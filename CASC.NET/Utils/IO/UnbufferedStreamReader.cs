using System;
using System.Collections.Generic;
using System.IO;

namespace CASC.NET.Utils.IO
{
    internal class UnbufferedStreamReader : TextReader
    {
        private Stream _stream;
        private System.Text.Encoding _encoding;

        public UnbufferedStreamReader(Stream stream)
        {
            _stream = stream;
            _encoding = System.Text.Encoding.UTF8;
        }

        public UnbufferedStreamReader(Stream stream, System.Text.Encoding encoding) : this(stream)
        {
            _encoding = encoding;
        }

        // This method assumes lines end with a line feed.
        // You may need to modify this method if your stream
        // follows the Windows convention of \r\n or some other 
        // convention that isn't just \n
        public override string ReadLine()
        {
            var bytes = new List<byte>();
            int current;
            while ((current = Read()) != -1 && current != '\n')
                bytes.Add((byte)current);
            return _encoding.GetString(bytes.ToArray());
        }

        // Read works differently than the `Read()` method of a 
        // TextReader. It reads the next BYTE rather than the next character
        public override int Read()
        {
            return _stream.ReadByte();
        }

        public override void Close()
        {
            _stream.Close();
        }
        protected override void Dispose(bool disposing)
        {
            _stream.Dispose();
        }

        public override int Peek()
        {
            throw new NotImplementedException();
        }

        public override int Read(char[] buffer, int index, int count)
        {
            throw new NotImplementedException();
        }

        public override int ReadBlock(char[] buffer, int index, int count)
        {
            throw new NotImplementedException();
        }

        public override string ReadToEnd()
        {
            throw new NotImplementedException();
        }
    }
}
