using System;
using System.IO;
using CASC.NET.Structures;

namespace CASC.NET.Games
{
    public abstract class GameProvider
    {
        public abstract bool LoadRoot(Stream fileStream);
        public abstract string ProgramCode { get; }

        public abstract event Action<RootRecord, uint> OnRootRecord;
    }
}
