using System;
using System.Collections.Generic;
using System.IO;
using CASC.NET.Games;
using CASC.NET.IO;
using CASC.NET.Utils;

namespace CASC.NET.Implementation
{
    public abstract class Provider : IDisposable
    {
        protected GameProvider Game { get; private set; }

        public unsafe void SetProvider(GameProvider provider)
        {
            Game = provider;
            Game.OnRootRecord +=
                (rootRecord, fileDataID) =>
                {
                    if (!_rootEntries.ContainsKey(rootRecord.Hash))
                        _rootEntries[rootRecord.Hash] = new List<Tuple<byte[], uint>>(15);

                    _rootEntries[rootRecord.Hash].Add(Tuple.Create(new IntPtr(rootRecord.MD5).CopyToManaged<byte>(16),
                        fileDataID));
                };
        }

        public abstract bool Initialize(string argument);

        public abstract void Dispose();

        public BLTE OpenFile(string fileName) => OpenFile(JenkinsHashing.Instance.ComputeHash(fileName));

        public abstract BLTE OpenFile(ulong fileHash);
        public abstract BLTE OpenFile(uint fileDataID);

        protected bool LoadRoot(Stream fileStream)
        {
            return Game.LoadRoot(fileStream);
        }

        #region Data containers
        protected Dictionary<byte[], List<Tuple<uint, byte[]>>> _encodingEntries =
            new Dictionary<byte[], List<Tuple<uint, byte[]>>>(50000, ByteArrayComparer.Instance);
        protected Dictionary<ulong, List<Tuple<byte[], uint>>> _rootEntries =
            new Dictionary<ulong, List<Tuple<byte[], uint>>>(25000);
        protected Dictionary<byte[], Tuple<int, int, int>> _indexEntries =
            new Dictionary<byte[], Tuple<int, int, int>>(20000, ByteArrayComparer.Instance);
        #endregion

    }
}
