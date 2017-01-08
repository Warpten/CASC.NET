using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CASC.NET.Utils.Extensions;
using CASC.NET.Utils.Net;

namespace CASC.NET
{
    internal sealed class ContentConfiguration : AsyncClient
    {
        public byte[][] Archives { get; private set; }
        // public string ArchiveGroup { get; private set; }
        // public string[] PatchArchives { get; private set; }
        // public string PatchArchiveGroup { get; private set; }

        public ContentConfiguration(string host, string contentHash) : base(host)
        {
            var queryString = $"/tpr/wow/config/{contentHash[0]}{contentHash[1]}/{contentHash[2]}{contentHash[3]}/{contentHash}";

            Send(queryString);
            if (Failed)
                return;

            using (var textReader = new StreamReader(Stream))
            {
                var line = textReader.ReadLine();
                if (line != "# CDN Configuration")
                    return;

                var elementList = new List<string>();
                var currentElement = string.Empty;

                while ((line = textReader.ReadLine()) != null)
                {
                    if (string.IsNullOrEmpty(line))
                        continue;

                    var lineTokens = line.Split(new[] { '=', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    var isIndexLine = line.Contains('=');
                    if (isIndexLine)
                    {
                        if (!string.IsNullOrEmpty(currentElement) && elementList.Count != 0)
                            StoreElement(currentElement, elementList);

                        currentElement = lineTokens[0];
                        elementList.Clear();
                    }

                    elementList.AddRange(isIndexLine ? lineTokens.Skip(1) : lineTokens);
                }
            }
        }

        private void StoreElement(string elementName, List<string> values)
        {
            switch (elementName)
            {
                case "archives":
                    Archives = values.Select(v => v.ToByteArray()).ToArray();
                    break;
                // case "archive-group":
                //     ArchiveGroup = values[0];
                //     break;
                // case "patch-archives":
                //     PatchArchives = values.ToArray();
                //     break;
                // case "patch-archive-group":
                //     PatchArchiveGroup = values[0];
                //     break;
            }
        }
    }
}
