using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Light.Transmit.Internals
{
    using Light.Transmit.Pools;
    using System;
    using System.Buffers;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using static Light.Transmit.Network.HighPerformanceTcpServer;

    internal class SocketBufferWriter : IBufferWriter<byte>
    {
        private readonly InternalEventSession _session;
        private readonly ObjectPool<SocketAsyncEventArgs> _writePool;
        private readonly int _bufferSize;
        private byte[] _buffer;
        private int _position;

        public SocketBufferWriter(InternalEventSession session, ObjectPool<SocketAsyncEventArgs> writePool, int bufferSize)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _writePool = writePool ?? throw new ArgumentNullException(nameof(writePool));
            _bufferSize = bufferSize > 0 ? bufferSize : throw new ArgumentOutOfRangeException(nameof(bufferSize));
            _buffer = new byte[_bufferSize];
            _position = 0;
        }

        public void Advance(int count)
        {
            if (_position + count > _bufferSize)
            {
                throw new InvalidOperationException("Cannot advance beyond buffer size.");
            }

            _position += count;

            // 自动刷新缓冲区
            if (_position >= _bufferSize)
            {
                FlushAsync().GetAwaiter().GetResult(); // 同步调用异步刷新
            }
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            if (sizeHint > _bufferSize - _position)
            {
                FlushAsync().GetAwaiter().GetResult(); // 同步调用异步刷新
            }
            EnsureCapacity(sizeHint);
            return _buffer.AsMemory(_position);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            if (sizeHint > _bufferSize - _position)
            {
                FlushAsync().GetAwaiter().GetResult(); // 同步调用异步刷新
            }
            EnsureCapacity(sizeHint);
            return _buffer.AsSpan(_position);
        }

        public async Task FlushAsync()
        {
            if (_position == 0)
            {
                return; // No data to send
            }

            var sendBuffer = _buffer.AsMemory(0, _position);
            _position = 0; // Reset position for the next write
            await SendDataAsync(_session, sendBuffer);
        }

        private void EnsureCapacity(int sizeHint)
        {
            if (sizeHint > _bufferSize - _position)
            {
                throw new InvalidOperationException("Buffer is full. Consider flushing before requesting more memory.");
            }
        }

        private async Task SendDataAsync(InternalEventSession session, Memory<byte> buffer)
        {
            var socket = session._socket;
            var sendArgs = _writePool.Get();
            sendArgs.AcceptSocket = socket;
            sendArgs.SetBuffer(buffer);

            var context = new SendEventContext
            {
                Session = session,
                TaskSource = new TaskCompletionSource()
            };

            sendArgs.UserToken = context;

            try
            {
                DoSendEventArgs(sendArgs);
                await context.TaskSource.Task;
            }
            finally
            {
                _writePool.Return(sendArgs);
            }
        }

        private void DoSendEventArgs(SocketAsyncEventArgs sendArgs)
        {
            if (!sendArgs.AcceptSocket.SendAsync(sendArgs))
            {
                OnSendCompleted(sendArgs);
            }
        }

        private void OnSendCompleted(SocketAsyncEventArgs sendArgs)
        {
            var context = (SendEventContext)sendArgs.UserToken;
            if (sendArgs.SocketError == SocketError.Success)
            {
                context.TaskSource.SetResult();
            }
            else
            {
                context.TaskSource.SetException(new SocketException((int)sendArgs.SocketError));
            }
        }
    }

}
