using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace KestrelServer.Tcp
{
    public class InternalSession : IConnectionSession
    {
        private Socket? _socket;
        private PipeWriter? writer;
        public String ConnectionId { get; set; }

        public EndPoint? RemoteEndPoint { get; set; }

        public ISessionData Data { get; set; }
        public DateTime ConnectTime { get; set; }

        public void Close()
        {
            _socket?.Close();
            _socket = null;
        }


        internal void Init(Socket socket, PipeWriter writer, DateTime now)
        {
            this._socket = socket;
            this.writer = writer;
            this.RemoteEndPoint = _socket?.RemoteEndPoint;
            this.ConnectTime = now;
        }

        public void Write(ReadOnlySpan<byte> buffer)
        {
            writer?.Write(buffer);
        }

        public void Write(ReadOnlyMemory<byte> buffer)
        {
            writer?.Write(buffer.Span);
        }

        public void Write(ArraySegment<byte> buffer)
        {
            writer?.Write(buffer);
        }

        public async Task WriteAsync(ArraySegment<byte> buffer)
        {
            if (writer != null)
            {
                await writer.WriteAsync(buffer);
            }
        }

        public async Task WriteAsync(ReadOnlyMemory<byte> buffer)
        {
            if (writer != null)
            {
                await writer.WriteAsync(buffer);
            }
        }

        public async Task FlushAsync()
        {
            if (writer != null)
            {
                await writer.FlushAsync();
            }
        }
    }
}
