using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Light.Transmit.Internals
{
    using Light.Transmit.Network;
    using Light.Transmit.Pools;
    using System;
    using System.Buffers;
    using System.IO.Pipelines;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using static Light.Transmit.Network.HighPerformanceTcpServer;

    internal class SocketBufferWriter : PipeWriter
    {
        private readonly Socket _socket;
        private readonly HighPerformanceTcpServer _server;
        private readonly InternalNetSession _session;
        private readonly ObjectPool<SocketAsyncEventArgs> _writePool;
        private readonly int _bufferSize;
        private byte[] _buffer;
        private int _position;

        public SocketBufferWriter(HighPerformanceTcpServer server, Socket socket, InternalNetSession session, ObjectPool<SocketAsyncEventArgs> writePool, int bufferSize)
        {
            _socket = socket;
            _server = server;
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _writePool = writePool ?? throw new ArgumentNullException(nameof(writePool));
            _bufferSize = bufferSize > 0 ? bufferSize : throw new ArgumentOutOfRangeException(nameof(bufferSize));
            _buffer = new byte[_bufferSize];
            _position = 0;
        }

        public override void Advance(int count)
        {
            if (_position + count > _bufferSize)
            {
                throw new InvalidOperationException("Cannot advance beyond buffer size.");
            }

            _position += count;

            // 自动刷新缓冲区
            if (_position >= _bufferSize)
            {
                FlushAsync(CancellationToken.None).GetAwaiter().GetResult(); // 同步调用异步刷新
            }
        }

        public override Memory<byte> GetMemory(int sizeHint = 0)
        {
            if (sizeHint > _bufferSize - _position)
            {
                FlushAsync(CancellationToken.None).GetAwaiter().GetResult(); // 同步调用异步刷新
            }
            EnsureCapacity(sizeHint);
            return _buffer.AsMemory(_position);
        }

        public override Span<byte> GetSpan(int sizeHint = 0)
        {
            if (sizeHint > _bufferSize - _position)
            {
                FlushAsync(CancellationToken.None).GetAwaiter().GetResult(); // 同步调用异步刷新
            }
            EnsureCapacity(sizeHint);
            return _buffer.AsSpan(_position);
        }

        public override async ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken)
        {
            if (_position == 0)
            {
                return new FlushResult(false, true); // No data to send
            }

            var sendBuffer = _buffer.AsMemory(0, _position);
            _position = 0; // Reset position for the next write
            await SendDataAsync(_session, sendBuffer);
            return new FlushResult(false, true);
        }

        private void EnsureCapacity(int sizeHint)
        {
            if (sizeHint > _bufferSize - _position)
            {
                throw new InvalidOperationException("Buffer is full. Consider flushing before requesting more memory.");
            }
        }

        private async Task SendDataAsync(InternalNetSession session, Memory<byte> buffer)
        {
            var sendArgs = _writePool.Get();
            sendArgs.AcceptSocket = _socket;
            sendArgs.SetBuffer(buffer);

            var context = new SendEventContext
            {
                Session = session,
                TaskSource = new TaskCompletionSource()
            };
            sendArgs.UserToken = context;
            _server.DoSendEventArgs(sendArgs);
            await context.TaskSource.Task;
        }


        public override void CancelPendingFlush()
        {

        }

        public override void Complete(Exception exception = null)
        {

        }
    }

}
