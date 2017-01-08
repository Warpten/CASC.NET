using System;
using System.IO;
using CASC.NET.Structures;
using CASC.NET.Utils.Extensions;
using CASC.NET.Utils.IO;

namespace CASC.NET.Games
{
    public sealed class Wow : GameProvider
    {
        public Wow()
        {
            ProgramCode = "wow";
        }

        public Wow(string programCode = "wow")
        {
            ProgramCode = programCode;
        }

        public override bool LoadRoot(Stream fileStream)
        {
            if (OnRootRecord == null)
                return true;

            using (var fileReader = new EndianBinaryReader(EndianBitConverter.Little, fileStream))
            {
                try
                {
                    while (true)
                    {
                        var recordCount = fileReader.ReadInt32();
                        if (fileStream.CanSeek)
                            fileReader.Seek(8, SeekOrigin.Current);
                        else
                            fileReader.ReadBytes(8);

                        var fileDataIndex = 0u;

                        var fileDataIndices = fileReader.ReadArray<uint>(recordCount);
                        for (var i = 0; i < recordCount; ++i)
                        {
                            fileDataIndices[i] += fileDataIndex;
                            fileDataIndex = fileDataIndices[i] + 1;
                        }

                        var fastRecords = fileReader.ReadArray<RootRecord>(recordCount);
                        for (var i = 0; i < recordCount; ++i)
                            OnRootRecord(fastRecords[i], fileDataIndices[i]);
                    }
                }
                catch (EndOfStreamException)
                {
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public override string ProgramCode { get; }
        public override event Action<RootRecord, uint> OnRootRecord;
    }
}
