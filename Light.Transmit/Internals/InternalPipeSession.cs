using System;
using System.Buffers;
using System.IO.Pipelines;
using System.IO.Pipes;
using System.Net;
using System.Threading.Tasks;

namespace Light.Transmit.Internals
{
    internal class InternalPipeSession : IConnectionSession
    {
        private PipeWriter writer;
        private PipeStream stream;

        public long ConnectionId { get; set; }
        public object[] Datas { get; } = [null, null, null, null, null];
        public EndPoint RemoteEndPoint { get; set; }
        public DateTime ConnectTime { get; set; }

        public SessionShutdownCause CloseCause { get; private set; }

        public IBufferWriter<byte> Writer => writer;

        public bool IsConnected => stream.IsConnected;

        public void Close(SessionShutdownCause cause)
        {
            stream?.Close();
            writer = null;
            stream = null;
            CloseCause = cause;
        }


        internal void Init(PipeStream stream, Int32 writeBufferSize)
        {
            this.stream = stream;
            writer = PipeWriter.Create(stream, new StreamPipeWriterOptions(minimumBufferSize: writeBufferSize));

        }


        public void Clean()
        {
            if (stream != null) Close(SessionShutdownCause.NONE);
            stream = null;
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
