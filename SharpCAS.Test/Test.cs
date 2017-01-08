using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using CASC.NET.Games;
using CASC.NET.Implementation;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CASC.NET.Test
{
    [TestClass]
    public class Test
    {
        [TestMethod]
        [Description("Remote loading speed test")]
        public void RemoteLoadingSpeed()
        {
            var casc = new CASC<DistantProvider, Wow>();

            var stopWatch = Stopwatch.StartNew();
            casc.Initialize("eu");
            stopWatch.Stop();
            Trace.WriteLine($"Distant CASC loaded in {stopWatch.ElapsedMilliseconds} ms");
        }

        [TestMethod]
        [Description("Local loading speed test")]
        public void LocalLoadingSpeed()
        {
            var accumulatedTime = 0L;
            for (var i = 0; i < 10; ++i)
            {
                var casc = new CASC<LocalProvider, Wow>();

                var stopWatch = Stopwatch.StartNew();
                casc.Initialize(@"G:\Games\World of Warcraft - Retail");
                stopWatch.Stop();
                Console.WriteLine("[{1}] Local CASC loaded in {0} ms", stopWatch.ElapsedMilliseconds, i + 1);
                accumulatedTime += stopWatch.ElapsedMilliseconds;
            }

            Console.WriteLine("Average load speed: {0} ms", accumulatedTime / 10.0f);
        }

        [TestMethod]
        [Description("Test loading Map.db2")]
        public void OpenLocalFile()
        {
            var casc = new CASC<LocalProvider, Wow>();

            var stopWatch = Stopwatch.StartNew();
            casc.Initialize(@"G:\Games\World of Warcraft - Retail");
            stopWatch.Stop();
            Console.WriteLine("CASC.OpenLocal: {0} ms", stopWatch.ElapsedMilliseconds);

            stopWatch = Stopwatch.StartNew();
            var blteStream = casc.OpenFile("DBFilesClient/Map.db2");
            stopWatch.Stop();

            Console.WriteLine("CASC.OpenFile: {0} ms", stopWatch.ElapsedMilliseconds);
            Console.WriteLine();

            using (var mapFile = new BinaryReader(blteStream))
            {
                Console.WriteLine("Chunk information:");
                for (var i = 0; i < blteStream.Chunks.Length; ++i)
                {
                    Console.WriteLine($"[{i}] CompressedSize: {blteStream.Chunks[i].CompressedSize}");
                    Console.WriteLine($"[{i}] DecompressedSize: {blteStream.Chunks[i].DecompressedSize}");
                }

                Console.WriteLine("|-------------------------------------------------|---------------------------------|");
                Console.WriteLine("| 00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F | 0 1 2 3 4 5 6 7 8 9 A B C D E F |");
                Console.WriteLine("|-------------------------------------------------|---------------------------------|");

                for (var i = 0; i < mapFile.BaseStream.Length; i += 16)
                {
                    var lineBuffer = mapFile.ReadBytes(16);

                    var textBuffer = new StringBuilder();
                    var hexBuffer = new StringBuilder();

                    foreach (var lineChar in lineBuffer)
                    {
                        hexBuffer.Append($"{lineChar:X2} ");
                        if (lineChar >= 32 && lineChar < 127)
                            textBuffer.Append($"{(char)lineChar} ");
                        else
                            textBuffer.Append(". ");
                    }

                    if (lineBuffer.Length < 16)
                    {
                        for (var j = lineBuffer.Length; j < 16; ++j)
                        {
                            textBuffer.Append("  ");
                            hexBuffer.Append("   ");
                        }
                    }

                    Console.WriteLine($"| {hexBuffer}| {textBuffer}|");
                }

                Console.WriteLine("|-------------------------------------------------|---------------------------------|");

                blteStream.Dispose();
            }
        }

        [TestMethod]
        [Description("Test loading Map.db2")]
        public void OpenRemoteFile()
        {
            var casc = new CASC<DistantProvider, Wow>();

            var stopWatch = Stopwatch.StartNew();
            casc.Initialize("eu");
            stopWatch.Stop();
            Console.WriteLine("CASC.OpenLocal: {0} ms", stopWatch.ElapsedMilliseconds);

            stopWatch = Stopwatch.StartNew();
            var blteStream = casc.OpenFile("DBFilesClient/Map.db2");
            stopWatch.Stop();

            Console.WriteLine("CASC.OpenFile: {0} ms", stopWatch.ElapsedMilliseconds);
            Console.WriteLine();

            using (var mapFile = new BinaryReader(blteStream))
            {
                Console.WriteLine("Chunk information:");
                for (var i = 0; i < blteStream.Chunks.Length; ++i)
                {
                    Console.WriteLine($"[{i}] CompressedSize: {blteStream.Chunks[i].CompressedSize}");
                    Console.WriteLine($"[{i}] DecompressedSize: {blteStream.Chunks[i].DecompressedSize}");
                }

                Console.WriteLine("|-------------------------------------------------|---------------------------------|");
                Console.WriteLine("| 00 01 02 03 04 05 06 07 08 09 0A 0B 0C 0D 0E 0F | 0 1 2 3 4 5 6 7 8 9 A B C D E F |");
                Console.WriteLine("|-------------------------------------------------|---------------------------------|");

                for (var i = 0; i < mapFile.BaseStream.Length; i += 16)
                {
                    var lineBuffer = mapFile.ReadBytes(16);

                    var textBuffer = new StringBuilder();
                    var hexBuffer = new StringBuilder();

                    foreach (var lineChar in lineBuffer)
                    {
                        hexBuffer.Append($"{lineChar:X2} ");
                        if (lineChar >= 32 && lineChar < 127)
                            textBuffer.Append($"{(char)lineChar} ");
                        else
                            textBuffer.Append(". ");
                    }

                    if (lineBuffer.Length < 16)
                    {
                        for (var j = lineBuffer.Length; j < 16; ++j)
                        {
                            textBuffer.Append("  ");
                            hexBuffer.Append("   ");
                        }
                    }

                    Console.WriteLine($"| {hexBuffer}| {textBuffer}|");
                }

                Console.WriteLine("|-------------------------------------------------|---------------------------------|");

                blteStream.Dispose();
            }
        }
    }
}
