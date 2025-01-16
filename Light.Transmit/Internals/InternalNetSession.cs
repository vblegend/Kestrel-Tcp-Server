using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Light.Transmit.Internals
{

    internal class InternalNetSession : IConnectionSession
    {
        private Socket _socket;

        private PipeWriter writer;
        public long ConnectionId { get; set; }
        public object[] Datas { get; } = [null, null, null, null, null];
        public EndPoint RemoteEndPoint { get; set; }
        public DateTime ConnectTime { get; set; }
        public SessionShutdownCause CloseCause { get; private set; }
        public IBufferWriter<byte> Writer => writer;
        public bool IsConnected => _socket.Connected;
        public void Close(SessionShutdownCause cause)
        {
            _socket?.Close();
            _socket = null;
            CloseCause = cause;
        }

        public IntPtr UserData { get; set; }



        internal void Init(Socket socket)
        {
            _socket = socket;
            RemoteEndPoint = _socket.RemoteEndPoint;
        }


        internal void Init(NetworkStream networkStream, Int32 writeBufferSize)
        {
            _socket = networkStream.Socket;
            writer = PipeWriter.Create(networkStream, new StreamPipeWriterOptions(minimumBufferSize: writeBufferSize));
            RemoteEndPoint = _socket.RemoteEndPoint;
        }

        public void Clean()
        {
            _socket = null;
            writer = null;
            RemoteEndPoint = null;
            ConnectTime = default;
            Datas[0] = Datas[1] = Datas[2] = Datas[3] = Datas[4] = null;
            ConnectionId = 0;
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

        public async ValueTask WriteAsync(ArraySegment<byte> buffer)
        {
            var w = writer;
            if (w != null)
            {
                await w.WriteAsync(buffer);
            }
        }

        public async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer)
        {
            var w = writer;
            if (w != null)
            {
                await w.WriteAsync(buffer);
            }
        }

        public async ValueTask FlushAsync()
        {
            var w = writer;
            if (w != null)
            {
                await w.FlushAsync();
            }
        }
    }
}
