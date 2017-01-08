using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using CASC.NET.Utils.Extensions;
using CASC.NET.Utils.IO;

namespace CASC.NET.Utils.Net
{
    internal class AsyncClient : IDisposable
    {
        private TcpClient _client;
        public NonClosingStream Stream { get; private set; }
        private string _host;
        private int _port;

        public int StatusCode { get; private set; }
        public int ContentLength => int.Parse(ResponseHeaders.Get("Content-Length"));
        public WebHeaderCollection RequestHeaders { get; } = new WebHeaderCollection();
        public WebHeaderCollection ResponseHeaders { get; } = new WebHeaderCollection();

        public bool LogRequest { get; set; } = true;

        public bool Failed => StatusCode == 404;

        public AsyncClient(string host, int port = 80)
        {
            _port = port;
            _host = host;
        }

        public void Send(string queryString)
        {
            _client = new TcpClient(_host, _port)
            {
                ReceiveTimeout = 5000,
                NoDelay = true
            };

            var headerString = new StringBuilder();
            headerString.AppendFormat("GET {0} HTTP/1.1\nHost: {1}\n", queryString, _host);

            RequestHeaders["Connection"] = "close";
            foreach (var headerName in RequestHeaders.AllKeys)
                headerString.AppendFormat("{0}: {1}\n", headerName, RequestHeaders.Get(headerName));

            Stream = new NonClosingStream(_client.GetStream());

            var queryHeaders = Encoding.ASCII.GetBytes(headerString.ToString() + '\n');
            Stream.Write(queryHeaders, 0, queryHeaders.Length);

            // No using statement, we don't want the stream to be disposed
            var textReader = new UnbufferedStreamReader(Stream);
            foreach (var line in
                textReader.ReadUntil("\r\n\r\n")
                    .Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries)
                    .Where(line => !string.IsNullOrEmpty(line)))
            {
                if (line.Contains(':'))
                    ResponseHeaders.Add(line);
                else if (line.IndexOf("HTTP/", StringComparison.Ordinal) == 0)
                    StatusCode = int.Parse(line.Substring(9, 3));
            }

            if (!LogRequest)
                return;

            Console.WriteLine("{2}: http://{0}{1}", _host, queryString, StatusCode);
            Console.WriteLine(ResponseHeaders.ToString());
        }

        #region IDisposable Support
        private bool disposedValue; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (disposedValue)
                return;

            if (disposing)
            {
                Stream?.BaseStream.Close();
                Stream = null;

                _client?.Close();
                _client = null;
            }

            disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(false);
        }
        #endregion
    }
}
