using System;
using System.Collections.Generic;
using CASC.NET.Games;
using CASC.NET.Implementation;
using CASC.NET.IO;

namespace CASC.NET
{
    public sealed class CASC<T, U> where T : Provider, new() where U : GameProvider, new()
    {
        private Provider _provider;

        public CASC()
        {
            _provider = new T();
            _provider.SetProvider(new U());
        }

        public bool Initialize(string installationPath) => _provider.Initialize(installationPath);

        public BLTE OpenFile(string fileName) => _provider.OpenFile(fileName);
        public BLTE OpenFile(uint fileDataID) => _provider.OpenFile(fileDataID);
        public BLTE OpenFile(ulong fileHash) => _provider.OpenFile(fileHash);
    }
}
