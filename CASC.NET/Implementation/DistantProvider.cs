using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CASC.NET.Games;
using CASC.NET.IO;
using CASC.NET.Structures;
using CASC.NET.Utils.Extensions;
using CASC.NET.Utils.IO;
using CASC.NET.Utils.Net;

namespace CASC.NET.Implementation
{
    public sealed class DistantProvider : Provider
    {
        private byte[] _rootKey;
        private byte[] _encodingKey;

        private CDNs.Record ServerInfo;
        private BuildConfiguration BuildConfig;
        private ContentConfiguration ContentConfig;

        public override bool Initialize(string region)
        {
            var versions = new Versions(Game.ProgramCode);
            var cdns = new CDNs(Game.ProgramCode);

            if (!cdns.Records.TryGetValue(region, out ServerInfo))
                return false;

            Versions.Record versionInfo;
            if (!versions.Records.TryGetValue(region, out versionInfo))
                return false;

            BuildConfig = new BuildConfiguration(ServerInfo.Hosts[0], versionInfo.BuildConfig);
            ContentConfig = new ContentConfiguration(ServerInfo.Hosts[0], versionInfo.CDNConfig);

            _rootKey = BuildConfig.Root.ToByteArray();
            _encodingKey = BuildConfig.Encoding[1].ToByteArray();

            using (var encodingClient = new AsyncClient(ServerInfo.Hosts[0]))
            {
                encodingClient.Send($"/{ServerInfo.Path}/data/{_encodingKey[0]:x2}/{_encodingKey[1]:x2}/{_encodingKey}");
                if (!encodingClient.Failed)
                    using (var encodingPack = new BLTE(encodingClient.Stream, 0, encodingClient.ContentLength))
                        if (!LoadEncoding(encodingPack))
                            throw new InvalidOperationException("Unable to find encoding!");
            }

            for (var i = 0; i < ContentConfig.Archives.Length; ++i)
            {
                var archiveHash = ContentConfig.Archives[i];

                using (var archiveClient = new AsyncClient(ServerInfo.Hosts[0]))
                {
                    archiveClient.Send($"/{ServerInfo.Path}/data/{archiveHash[0]:x2}/{archiveHash[1]:x2}/{archiveHash.ToHexString()}.index");
                    if (!archiveClient.Failed)
                        LoadIndex(archiveClient.Stream, archiveClient.ContentLength, i);
                }
            }

            if (!_encodingEntries.ContainsKey(_rootKey))
                throw new InvalidOperationException("Root entry not found in encoding!");

            var encodingEntry = _encodingEntries[_rootKey];

            foreach (var rootEncodingEntry in encodingEntry)
            {
                using (var rootClient = new AsyncClient(ServerInfo.Hosts[0]))
                {
                    rootClient.Send($"/{ServerInfo.Path}/data/{rootEncodingEntry.Item2[0]:x2}/{rootEncodingEntry.Item2[1]:x2}/{rootEncodingEntry.Item2.ToHexString()}");
                    if (!rootClient.Failed)
                        using (var rootBlte = new BLTE(rootClient.Stream, 0, rootClient.ContentLength))
                            if (LoadRoot(rootBlte))
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
                            var key = reader.ReadBytes(16);

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

        private bool LoadIndex(Stream remoteStream, int contentLength, int archiveIndex)
        {
            using (var memoryStream = new EndianBinaryReader(EndianBitConverter.Little, remoteStream))
            {
                //! TODO: Avoid reading the whole file before parsing it
                using (var chunkStream = new MemoryStream(memoryStream.ReadBytes(contentLength), false))
                using (var chunkReader = new EndianBinaryReader(EndianBitConverter.Big, chunkStream))
                {
                    chunkReader.Seek(-12, SeekOrigin.End);
                    var recordCount = chunkReader.ReadInt32().EndianReverse();
                    chunkReader.Seek(0, SeekOrigin.Begin);

                    while (recordCount != 0)
                    {
                        var hash = chunkReader.ReadBytes(9);
                        chunkReader.BaseStream.Position += 16 - 9;
                        if (hash.All(b => b == 0))
                        {
                            if (chunkStream.Position % 4096 == 0)
                                continue;

                            var chunkPosition = (chunkStream.Position / 4096) * 4096;
                            chunkStream.Position = chunkPosition + 4096; // Skip to next chunk
                        }
                        else
                        {
                            var size = chunkReader.ReadInt32();
                            var offset = chunkReader.ReadInt32();

                            _indexEntries[hash] = Tuple.Create(archiveIndex, offset, size);
                            OnIndexRecord?.Invoke(hash, archiveIndex, offset, size);
                            --recordCount;
                        }
                    }
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
                    if (!_indexEntries.TryGetValue(encodingEntry.Item2.Take(9).ToArray(), out indexEntry))
                        continue;

                    using (var asyncClient = new AsyncClient(ServerInfo.Hosts[0]))
                    {
                        var archiveHash = ContentConfig.Archives[indexEntry.Item1];

                        asyncClient.RequestHeaders.Add("Range", $"bytes={indexEntry.Item2}-{indexEntry.Item2 + indexEntry.Item3 - 1}");
                        asyncClient.Send($"/{ServerInfo.Path}/data/{archiveHash[0]:x2}/{archiveHash[1]:x2}/{archiveHash.ToHexString()}");
                        if (!asyncClient.Failed)
                            return new BLTE(asyncClient.Stream, 0, indexEntry.Item3);
                    }
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
                    if (!_indexEntries.TryGetValue(encodingEntry.Item2, out indexEntry))
                        continue;

                    using (var asyncClient = new AsyncClient(ServerInfo.Hosts[0]))
                    {
                        var archiveHash = ContentConfig.Archives[indexEntry.Item1];

                        asyncClient.RequestHeaders.Add("Range", $"bytes={indexEntry.Item2}-{indexEntry.Item3}");
                        asyncClient.Send($"/{ServerInfo.Path}/data/{archiveHash[0]:x2}/{archiveHash[1]:x2}/{archiveHash.ToHexString()}");
                        if (!asyncClient.Failed)
                            return new BLTE(asyncClient.Stream, 0, indexEntry.Item3);
                    }
                }
            }

            return null;
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

        public override void Dispose()
        {
            
        }
    }
}
