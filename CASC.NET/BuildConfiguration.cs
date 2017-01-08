using System.IO;
using System.Linq;
using CASC.NET.Utils.Net;

namespace CASC.NET
{
    internal sealed class BuildConfiguration : AsyncClient
    {
        public string Root { get; }
        public string Install { get; }
        // public int InstallSize { get; }
        // public byte[] Download { get; }
        // public int DownloadSize { get; }
        // public byte[] PartialPriority { get; }
        // public int PartialPrioritySize { get; }
        public string[] Encoding { get; }
        // public int[] EncodingSize { get; }
        // public byte[] Patch { get; set; }
        // public int PatchSize { get; set; }
        // public byte[] PatchConfig { get; set; }

        public BuildConfiguration(string host, string buildHash) : base(host)
        {
            var queryString = $"/tpr/wow/config/{buildHash[0]}{buildHash[1]}/{buildHash[2]}{buildHash[3]}/{buildHash}";

            Send(queryString);
            if (Failed)
                return;

            using (var textReader = new StreamReader(Stream))
            {
                var line = textReader.ReadLine();
                if (line != "# Build Configuration")
                    return;

                while ((line = textReader.ReadLine()) != null)
                {
                    if (string.IsNullOrEmpty(line))
                        continue;

                    var lineTokens = line.Split('=').Select(l => l.Trim()).ToArray();
                    if (lineTokens.Length != 2)
                        continue;

                    // ReSharper disable once SwitchStatementMissingSomeCases
                    switch (lineTokens[0])
                    {
                        case "root":
                            Root = lineTokens[1];
                            break;
                        case "install":
                            Install = lineTokens[1];
                            break;
                        // case "install-size":
                        //     InstallSize = int.Parse(lineTokens[1]);
                        //     break;
                        // case "download":
                        //     Download = lineTokens[1].ToByteArray();
                        //     break;
                        // case "download-size":
                        //     DownloadSize = int.Parse(lineTokens[1]);
                        //     break;
                        // case "partial-priority":
                        //     PartialPriority = lineTokens[1].ToByteArray();
                        //     break;
                        // case "partial-priority-size":
                        //     PartialPrioritySize = int.Parse(lineTokens[1]);
                        //     break;
                        case "encoding":
                        {
                            Encoding = new string[2];
                            var encodingTokens = lineTokens[1].Split(' ');
                            Encoding[0] = encodingTokens[0];
                            Encoding[1] = encodingTokens[1];
                            break;
                        }
                        // case "encoding-size":
                        // {
                        //     EncodingSize = new int[2];
                        //     var encodingTokens = lineTokens[1].Split(' ');
                        //     EncodingSize[0] = int.Parse(encodingTokens[0]);
                        //     EncodingSize[1] = int.Parse(encodingTokens[1]);
                        //     break;
                        // }
                        // case "patch":
                        //     Patch = lineTokens[1].ToByteArray();
                        //     break;
                        // case "patch-size":
                        //     PatchSize = int.Parse(lineTokens[1]);
                        //     break;
                        // case "patch-config":
                        //     PatchConfig = lineTokens[1].ToByteArray();
                        //     break;
                    }
                }
            }
        }
    }
}
