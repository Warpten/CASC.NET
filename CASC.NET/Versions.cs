using System.Collections.Generic;
using System.IO;
using CASC.NET.Utils.Net;

namespace CASC.NET
{
    internal sealed class Versions : AsyncClient
    {
        public Dictionary<string, Record> Records { get; } = new Dictionary<string, Record>();

        public Versions(string channel) : base("eu.patch.battle.net", 1119)
        {
            LogRequest = false;
            Send($"/{channel}/versions");

            using (var reader = new StreamReader(Stream))
            {
                // Skip header
                // ReSharper disable once RedundantAssignment
                var line = reader.ReadLine();
                while ((line = reader.ReadLine()) != null)
                {
                    var lineTokens = line.Split('|');

                    Records[lineTokens[0]] = new Record
                    {
                        Region = lineTokens[0],
                        BuildConfig = lineTokens[1],
                        CDNConfig = lineTokens[2],
                        // KeyRing = BuildHash(lineTokens[3]),
                        BuildID = int.Parse(lineTokens[4]),
                        VersionsName = lineTokens[5],
                        ProductConfig = lineTokens[6],

                        Channel = channel
                    };
                }
            }
        }

        public struct Record
        {
            public string Region { get; set; }
            public string BuildConfig { get; set; }
            public string CDNConfig { get; set; }
            public byte[] KeyRing { get; set; }
            public int BuildID { get; set; }
            public string VersionsName { get; set; }
            public string ProductConfig { get; set; }

            public string Channel { get; set; }

            public string GetName(string channel) => $"{Channel}-{BuildID}patch{VersionsName.Substring(0, 5)}_{channel}";
        }
    }
}
