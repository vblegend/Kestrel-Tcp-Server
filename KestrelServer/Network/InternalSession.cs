﻿using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.ObjectPool;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace KestrelServer.Network
{

    internal class SessionPool
    {
        private class SessionPooledObjectPolicy : PooledObjectPolicy<InternalSession>
        {
            public override InternalSession Create()
            {
                return new InternalSession();
            }

            public override bool Return(InternalSession obj)
            {
                obj.Clean();
                return true;
            }
        }


        internal static ObjectPool<InternalSession> Pool = CreateObjectPool();

        private static ObjectPool<InternalSession> CreateObjectPool()
        {
            //return new DisposableObjectPool<InternalSession>(new SessionPooledObjectPolicy(), MaximumRetained);
            return new DefaultObjectPool<InternalSession>(new SessionPooledObjectPolicy(), 1024);
        }

    }

    internal class InternalSession : IConnectionSession
    {
        private Socket? _socket;
        private PipeWriter? writer;

        public Int64 ConnectionId { get; set; }
        public Object?[] Datas { get; } = [null, null, null, null, null];
        public EndPoint? RemoteEndPoint { get; set; }
        public DateTime ConnectTime { get; set; }

        public SessionShutdownCause CloseCause { get; private set; }

        public IBufferWriter<byte> Writer => writer;

        public void Close(SessionShutdownCause cause)
        {
            _socket?.Close();
            _socket = null;
            CloseCause = cause;
        }


        internal void Init(NetworkStream networkStream)
        {
            this._socket = networkStream.Socket;
            this.writer = PipeWriter.Create(networkStream);
            this.RemoteEndPoint = _socket.RemoteEndPoint;
        }

        internal void Clean()
        {
            this._socket = null;
            this.writer = null;
            this.RemoteEndPoint = null;
            this.ConnectTime = default;
            this.Datas[0] = this.Datas[1] = this.Datas[2] = this.Datas[3] = this.Datas[4] = null;
            this.ConnectionId = 0;
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
            if (writer != null)
            {
                await writer.WriteAsync(buffer);
            }
        }

        public async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer)
        {
            if (writer != null)
            {
                await writer.WriteAsync(buffer);
            }
        }

        public async ValueTask FlushAsync()
        {
            if (writer != null)
            {
                await writer.FlushAsync();
            }
        }
    }
}
