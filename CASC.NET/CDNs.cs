using System.Collections.Generic;
using System.IO;
using CASC.NET.Utils.Net;

namespace CASC.NET
{
    internal sealed class CDNs : AsyncClient
    {
        public Dictionary<string, Record> Records { get; } = new Dictionary<string, Record>();

        public CDNs(string channel) : base("eu.patch.battle.net", 1119)
        {
            LogRequest = false;
            Send($"/{channel}/cdns");
            if (Failed)
                return;

            using (var reader = new StreamReader(Stream))
            {
                // Skip header
                // ReSharper disable once RedundantAssignment
                var line = reader.ReadLine();
                while ((line = reader.ReadLine()) != null)
                {
                    var lineTokens = line.Split('|');

                    Records[lineTokens[0]] = new Record()
                    {
                        Name = lineTokens[0],
                        Path = lineTokens[1],
                        Hosts = lineTokens[2].Split(' ')
                    };
                }
            }
        }

        public struct Record
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public string[] Hosts { get; set; }
            // public string ConfigPath { get; set; }
        }
    }
}
