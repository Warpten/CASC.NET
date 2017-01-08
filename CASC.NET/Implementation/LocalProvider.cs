using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CASC.NET.IO;
using CASC.NET.Structures;
using CASC.NET.Utils.Extensions;
using CASC.NET.Utils.IO;

namespace CASC.NET.Implementation
{
    public sealed class LocalProvider : Provider
    {
        private string ArchivePath { get; set; }

        private byte[] _encodingKey;
        private byte[] _rootKey;

        private Stream[] _archives { get; set; } = new Stream[46];

        public override bool Initialize(string installationPath)
        {
            ArchivePath = Path.Combine(installationPath, @"Data\data");

            #region Load .build.info and config
            var buildInfo = new TokenConfig();
            using (var buildInfoReader = new StreamReader(File.OpenRead(Path.Combine(installationPath, ".build.info"))))
                buildInfo.Load(buildInfoReader);

            var buildKey = buildInfo["Build Key"].FirstOrDefault();
            if (string.IsNullOrEmpty(buildKey))
                throw new InvalidOperationException(".build.info is missing a build key");

            var buildConfigPath = Path.Combine(installationPath, @"Data\config",
                buildKey.Substring(0, 2), buildKey.Substring(2, 2), buildKey);
            var buildConfig = new KeyValueConfig();
            using (var buildConfigReader = new StreamReader(File.OpenRead(buildConfigPath)))
                buildConfig.Load(buildConfigReader);
            #endregion

            _rootKey = buildConfig["root"].FirstOrDefault().ToByteArray();
            _encodingKey = buildConfig["encoding"].ElementAtOrDefault(1).ToByteArray(9);

            for (var i = 0; i < 0x10; ++i)
            {
                var indexFile = Directory.GetFiles(ArchivePath, $"{i:X2}*.idx").Last();
                using (var indexFileStream = File.OpenRead(indexFile))
                    LoadIndex(indexFileStream);
            }

            var encodingEntry = _indexEntries[_encodingKey];
            using (var encodingStream = new BLTE(GetArchive(encodingEntry.Item1), encodingEntry.Item2 + 30, encodingEntry.Item3 - 30, false))
                if (!LoadEncoding(encodingStream))
                    throw new InvalidOperationException("Unable to find encoding");

            // At this point, the only possibility for root to not load is
            // that it was parsed from encoding before being found in index
            // or that it was not loaded when found in encoding
            if (_rootEntries.Count == 0)
            {
                if (!_encodingEntries.ContainsKey(_rootKey))
                    throw new InvalidOperationException("Root entry not found in encoding!");

                foreach (var rootEncodingEntry in _encodingEntries[_rootKey])
                {
                    Tuple<int, int, int> indexEntry;
                    if (!_indexEntries.TryGetValue(rootEncodingEntry.Item2, out indexEntry))
                        continue;

                    using (var rootStream = new BLTE(GetArchive(indexEntry.Item1), indexEntry.Item2 + 30, indexEntry.Item3 - 30))
                        if (LoadRoot(rootStream))
                            break;
                }
            }

            return _rootEntries.Count != 0;
        }

        #region Provider implementation
        private bool LoadEncoding(Stream fileStream)
        {
            using (var reader = new EndianBinaryReader(EndianBitConverter.Little, fileStream))
            {
                if (reader.ReadInt16() != 0x4E45) // EN
                    return false;

                reader.Seek(1 + 1 + 1 + 2 + 2, SeekOrigin.Current); // Skip unk, checksum sizes, flagsA, flagsB

                reader.BitConverter = EndianBitConverter.Big;
                var tableEntryCount = reader.ReadInt32();
                reader.Seek(4 + 1, SeekOrigin.Current); // skip entrycountB, unk
                var stringBlockSize = reader.ReadInt32(); // String block size (which we won't use)

                // Skip string block and hash headers
                reader.Seek(stringBlockSize + (16 + 16) * tableEntryCount, SeekOrigin.Current);

                reader.BitConverter = EndianBitConverter.Little;

                var chunkStart = reader.BaseStream.Position;

                for (var i = 0; i < tableEntryCount; ++i)
                {
                    ushort keyCount;
                    while ((keyCount = reader.ReadUInt16()) != 0)
                    {
                        reader.BitConverter = EndianBitConverter.Big;
                        var fileSize = reader.ReadUInt32();
                        var hash = reader.ReadBytes(16);

                        for (var j = 0; j < keyCount; ++j)
                        {
                            // Local only uses 9 bytes - for reasons I can't fathom.
                            var key = reader.ReadBytes(9);
                            reader.BaseStream.Position += 16 - 9;

                            if (!_encodingEntries.ContainsKey(hash))
                                _encodingEntries[hash] = new List<Tuple<uint, byte[]>>(15);

                            _encodingEntries[hash].Add(Tuple.Create(fileSize, key));

                            OnEncodingRecord?.Invoke(hash, fileSize, key);
                        }

                        reader.BitConverter = EndianBitConverter.Little;
                    }

                    const int CHUNK_SIZE = 4096;
                    reader.Seek(CHUNK_SIZE - (int)((reader.BaseStream.Position - chunkStart) % CHUNK_SIZE), SeekOrigin.Current);
                }
            }

            return true;
        }

        private bool LoadIndex(Stream fileStream)
        {
            using (var reader = new EndianBinaryReader(EndianBitConverter.Little, fileStream))
            {
                reader.BaseStream.Position = (8 + reader.ReadInt32() + 0x0F) & 0xFFFFFFF0;

                var dataLength = reader.ReadInt32();
                var blockCount = dataLength / 18;
                reader.BaseStream.Position += 4; // data check

                for (var i = 0; i < blockCount; ++i)
                {
                    var key = reader.ReadBytes(9);

                    var indexHigh = reader.ReadByte();
                    reader.BitConverter = EndianBitConverter.Big;
                    var indexLow = reader.ReadUInt32();
                    reader.BitConverter = EndianBitConverter.Little;

                    var archiveIndex = indexHigh << 2 | (byte) ((indexLow & 0xC0000000) >> 30);
                    var offset = (int) (indexLow & 0x3FFFFFFF);
                    var size = reader.ReadInt32();

                    _indexEntries[key] = Tuple.Create(archiveIndex, offset, size);
                    OnIndexRecord?.Invoke(key, archiveIndex, offset, size);
                }
            }

            return true;
        }

        /// <summary>
        /// Opens a file from its root hash.
        /// </summary>
        /// <param name="fileHash"></param>
        /// <returns></returns>
        public override BLTE OpenFile(ulong fileHash)
        {
            List<Tuple<byte[], uint>> rootEntries;
            if (!_rootEntries.TryGetValue(fileHash, out rootEntries))
                return null;

            foreach (var rootEntry in rootEntries)
            {
                List<Tuple<uint, byte[]>> encodingEntries;
                if (!_encodingEntries.TryGetValue(rootEntry.Item1, out encodingEntries))
                    continue;

                foreach (var encodingEntry in encodingEntries)
                {
                    Tuple<int, int, int> indexEntry;
                    if (_indexEntries.TryGetValue(encodingEntry.Item2.Take(9).ToArray(), out indexEntry))
                        return new BLTE(GetArchive(indexEntry.Item1), indexEntry.Item2 + 30, indexEntry.Item3 - 30);
                }
            }

            return null;
        }

        /// <summary>
        /// Open a file from its data ID.
        /// </summary>
        /// <param name="fileDataID"></param>
        /// <returns></returns>
        public override BLTE OpenFile(uint fileDataID)
        {
            foreach (var rootEntry in _rootEntries.Values.Flatten(item => item.Item2 == fileDataID))
            {
                List<Tuple<uint, byte[]>> encodingEntries;
                if (!_encodingEntries.TryGetValue(rootEntry.Item1, out encodingEntries))
                    continue;

                foreach (var encodingEntry in encodingEntries)
                {
                    Tuple<int, int, int> indexEntry;
                    if (_indexEntries.TryGetValue(encodingEntry.Item2, out indexEntry))
                        return new BLTE(GetArchive(indexEntry.Item1), indexEntry.Item2 + 30, indexEntry.Item3 - 30);
                }
            }

            return null;
        }

        private Stream GetArchive(int archiveIndex)
        {
            return _archives[archiveIndex] ??
                   (_archives[archiveIndex] = File.OpenRead(Path.Combine(ArchivePath, $"data.{archiveIndex:D3}")));
        }
        #endregion

        #region Events
        public event Action<byte[] /* hash */, int /* archiveIndex */, int /* offset */, int /* size */> OnIndexRecord;
        public event Action<RootRecord, uint /* fileDataID */> OnRootRecord
        {
            add { Game.OnRootRecord += value; }
            remove { Game.OnRootRecord -= value; }
        }
        public event Action<byte[] /* hash */, uint /* fileSize */, byte[] /* key */> OnEncodingRecord;
        #endregion

        #region IDisposable implementation
        public override void Dispose()
        {
            if (_archives != null)
            {
                foreach (var file in _archives)
                    file?.Dispose();
                _archives = null;
            }
        }
        #endregion
    }
}
