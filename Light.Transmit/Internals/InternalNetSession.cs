using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Light.Transmit.Internals
{

    internal class InternalNetSession : IConnectionSession, IDisposable
    {
        private Socket _socket;

        private PipeWriter _writer;
        public long ConnectionId { get; set; }
        public object[] Datas { get; } = [null, null, null, null, null];
        public EndPoint RemoteEndPoint { get; set; }
        public DateTime ConnectTime { get; set; }
        public SessionShutdownCause CloseCause { get; private set; }
        public IBufferWriter<byte> Writer => _writer;
        public bool IsConnected => _socket.Connected;
        public void Close(SessionShutdownCause cause)
        {
            _socket?.Close();
            _socket = null;
            CloseCause = cause;
        }

        internal void Init(Socket socket, PipeWriter writer)
        {
            _socket = socket;
            _writer = writer;
            RemoteEndPoint = _socket.RemoteEndPoint;
        }

        internal void Init(NetworkStream networkStream, Int32 writeBufferSize)
        {
            _socket = networkStream.Socket;
            _writer = PipeWriter.Create(networkStream, new StreamPipeWriterOptions(minimumBufferSize: writeBufferSize));
            RemoteEndPoint = _socket.RemoteEndPoint;
        }

        public void Clean()
        {
            _socket = null;
            _writer = null;
            RemoteEndPoint = null;
            ConnectTime = default;
            Datas[0] = Datas[1] = Datas[2] = Datas[3] = Datas[4] = null;
            ConnectionId = 0;
        }


        public void Dispose()
        {

        }

        public void Write(ReadOnlySpan<byte> buffer)
        {
            _writer?.Write(buffer);
        }

        public void Write(ReadOnlyMemory<byte> buffer)
        {
            _writer?.Write(buffer.Span);
        }

        public void Write(ArraySegment<byte> buffer)
        {
            _writer?.Write(buffer);
        }

        public async ValueTask WriteAsync(ArraySegment<byte> buffer)
        {
            var w = _writer;
            if (w != null)
            {
                await w.WriteAsync(buffer);
            }
        }

        public async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer)
        {
            var w = _writer;
            if (w != null)
            {
                await w.WriteAsync(buffer);
            }
        }

        public async ValueTask FlushAsync()
        {
            var w = _writer;
            if (w != null)
            {
                await w.FlushAsync();
            }
        }


    }
}
