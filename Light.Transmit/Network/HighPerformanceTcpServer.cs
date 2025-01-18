using Light.Transmit.Adapters;
using Light.Transmit.Internals;
using Light.Transmit.Pools;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Light.Transmit.Network
{



    public class HighPerformanceTcpServer : IPV4Socket, IPacketServer
    {





        private readonly SocketAsyncEventArgs acceptEventArgs;
        internal readonly ObjectPool<SocketAsyncEventArgs> writePool;
        private readonly ObjectPool<SocketAsyncEventArgs> readArgsPool;
        private readonly ConcurrentDictionary<Int64, InternalNetSession> connections = new ConcurrentDictionary<Int64, InternalNetSession>();
        private ServerHandlerAdapter handlerAdapter = null;
        private Int32 receiveBufferSize = 8192;
        private Int32 sendBufferSize = 8192;
        private Int32 maximumConnectionLimit = 100;
        private Int64 ConnectionIdSource;

        private SocketAsyncEventArgs CreateSendEventArgs()
        {
            var args = new SocketAsyncEventArgs();
            args.Completed += IO_Completed;
            return args;
        }

        private SocketAsyncEventArgs CreateReceiveEventArgs()
        {
            var args = new SocketAsyncEventArgs();
            args.SetBuffer(new byte[receiveBufferSize], 0, receiveBufferSize);
            args.Completed += IO_Completed;
            return args;
        }


        public void SetAdapter(ServerHandlerAdapter handlerAdapter)
        {
            this.handlerAdapter = handlerAdapter;
        }
        public HighPerformanceTcpServer()
        {
            writePool = new ObjectPool<SocketAsyncEventArgs>(this.CreateSendEventArgs, 1024);
            readArgsPool = new ObjectPool<SocketAsyncEventArgs>(this.CreateReceiveEventArgs, 1024);
            acceptEventArgs = new SocketAsyncEventArgs();
            acceptEventArgs.Completed += IO_Completed;
        }

        public void Listen(Uri uri)
        {
            if (uri == null) throw new Exception("参数不能为空");
            if (uri.Scheme != "tcp") throw new Exception("不支持的协议");
            var querys = uri.ParseQuery();
            if (querys.TryGetValue("readBuffer", out var readBufferSize))
            {
                this.ReceiveBufferSize = Int32.Parse(readBufferSize);
            }
            if (querys.TryGetValue("writeBuffer", out var writeBufferSize))
            {
                this.SendBufferSize = Int32.Parse(writeBufferSize);
            }
            Listen(IPAddress.Parse(uri.Host), uri.Port);
        }

        public void Listen(IPAddress ipAddress, int port)
        {
            // 初始化监听Socket
            socket.Bind(new IPEndPoint(ipAddress, port));
            socket.Listen(100);
            Console.WriteLine("Server started. Listening for connections...");
            // 开始接受连接
            // 重置
            acceptEventArgs.AcceptSocket = null;
            // 异步接受连接
            if (!socket.AcceptAsync(acceptEventArgs)) ProcessAccept(acceptEventArgs);
        }

        public Task StopAsync()
        {
            socket.Close();
            return Task.CompletedTask;
        }


        private void ProcessAccept(SocketAsyncEventArgs acceptArgs)
        {
            while (true)
            {
                Socket clientSocket = acceptArgs.AcceptSocket;
                if (acceptArgs.SocketError == SocketError.Success)
                {
                    if (!handlerAdapter.OnAccept(clientSocket))
                    {
                        clientSocket.Close();
                        break;
                    }
                    clientSocket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
                    clientSocket.SendBufferSize = sendBufferSize;
                    clientSocket.ReceiveBufferSize = receiveBufferSize;
                    SocketAsyncEventArgs receiveArgs = readArgsPool.Get();
                    //
                    var session = new InternalNetSession();
                    session.ConnectionId = Interlocked.Increment(ref ConnectionIdSource);
                    session.ConnectTime = TimeService.Default.LocalNow();
                    //session.Init(clientSocket);
                    var writer = new SocketBufferWriter(this, clientSocket, session, writePool, sendBufferSize);


                    //var writer = PipeWriter.Create(new NetworkStream(clientSocket), new StreamPipeWriterOptions(minimumBufferSize:sendBufferSize));



                    session.Init(clientSocket, writer);
                    receiveArgs.UserToken = session;
                    receiveArgs.AcceptSocket = clientSocket;
                    AddSession(session);
                    //Console.WriteLine("Join");
                    //
                    // OnConnection()
                    //
                    handlerAdapter.OnConnected(session).AsTask().Wait();
                    //send100000(session).Wait();
                    if (!clientSocket.ReceiveAsync(receiveArgs)) ProcessReceive(receiveArgs);
                    acceptArgs.AcceptSocket = null;
                    if (socket.AcceptAsync(acceptArgs)) break;
                }
                else
                {
                    // shutdown
                    KickdAllSessions();
                    break;
                }


            }
            // 继续接受下一个连接
            //StartAccept(acceptArgs);
        }



        private void KickdAllSessions()
        {
            var sessions = connections.Values.ToList();
            foreach (var session in sessions)
            {
                session.Close(SessionShutdownCause.SHUTTING_DOWN);
            }
        }

        private void AddSession(InternalNetSession session)
        {
            connections.TryAdd(session.ConnectionId, session);
        }
        private void RemoveSession(InternalNetSession session)
        {
            connections.Remove(session.ConnectionId, out var _);
        }



        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                case SocketAsyncOperation.Accept:
                    ProcessAccept(e);
                    break;
            }
        }


        private unsafe void ProcessReceive(SocketAsyncEventArgs eventArgs)
        {
            Socket clientSocket = eventArgs.AcceptSocket;
            InternalNetSession session = eventArgs.UserToken as InternalNetSession;
            do
            {
                try
                {
                    if (eventArgs.BytesTransferred > 0 && eventArgs.SocketError == SocketError.Success)
                    {
                        var length = eventArgs.Offset + eventArgs.BytesTransferred;
                        // 使用正确的参数来构造 ReadOnlySequence，确保不丢失粘包部分
                        ReadOnlySequence<byte> readOnlyMemory = new ReadOnlySequence<byte>(eventArgs.Buffer, 0, length);
                        // 处理数据包（handlerAdapter 会处理粘包和返回有效字节的数量）
                        var result = handlerAdapter.OnPacket(session, readOnlyMemory);
                        if (result.ReadLength < eventArgs.Buffer.Length)
                        {
                            // 处理剩余数据，将有效数据移到缓冲区的前面
                            var buffer = eventArgs.MemoryBuffer;
                            var remaining = length - result.ReadLength;
                            // 将剩余数据复制到缓冲区的前面
                            var source = buffer.Slice(result.ReadLength, remaining);
                            var destination = buffer.Slice(0, remaining);
                            source.CopyTo(destination);
                            // 更新 eventArgs 的缓冲区和偏移量
                            var bufferSize = eventArgs.Buffer.Length - remaining;
                            eventArgs.SetBuffer(remaining, bufferSize);
                        }
                        else
                        {
                            // 如果已读取完全部数据，则重置缓冲区
                            eventArgs.SetBuffer(0, eventArgs.Buffer.Length);
                        }
                    }
                    else
                    {
                        // 连接关闭或发生错误，关闭连接
                        HandleSessionClose(session, eventArgs);
                        readArgsPool.Return(eventArgs);
                        break;
                    }
                }
                catch (Exception ex)
                {
                    // 处理异常
                    handlerAdapter.OnError(session, ex);
                    HandleSessionClose(session, eventArgs);
                    readArgsPool.Return(eventArgs);
                    break;
                }

            } while (!clientSocket.ReceiveAsync(eventArgs));  // 继续接收数据
        }




        internal void DoSendEventArgs(SocketAsyncEventArgs sendArgs)
        {
            if (!sendArgs.AcceptSocket.SendAsync(sendArgs)) ProcessSend(sendArgs);
        }



        private void ProcessSend(SocketAsyncEventArgs eventArgs)
        {
            Socket clientSocket = eventArgs.AcceptSocket;
            SendEventContext eventContext = eventArgs.UserToken as SendEventContext;
            if (eventArgs.SocketError == SocketError.Success)
            {
                var length = eventArgs.Count - eventArgs.Offset;
                if (eventArgs.BytesTransferred != length)
                {
                    // 测试过程中没发现有没发送完成的情况
                    Console.WriteLine("未发送完成, 测试过程中没发现有没发送完成的情况");
                }
                eventContext.TaskSource.SetResult();
            }
            else
            {
                // 发送异常，连接关闭
                HandleSessionClose(eventContext.Session, eventArgs);
                eventContext.TaskSource.SetException(new Exception("连接已关闭"));
            }
            eventContext = null;
            eventArgs.AcceptSocket = null;
            eventArgs.UserToken = null;
            eventArgs.SetBuffer(null, 0, 0);
            writePool.Return(eventArgs);
        }



        private void HandleSessionClose(InternalNetSession session, SocketAsyncEventArgs eventArgs)
        {
            session.Close(SessionShutdownCause.UNEXPECTED_DISCONNECTED);
            handlerAdapter.OnClose(session);
            eventArgs.UserToken = null;
            eventArgs.AcceptSocket = null;
            RemoveSession(session);
        }



        public int MaximumConnectionLimit
        {
            get
            {
                return maximumConnectionLimit;
            }
            set
            {
                maximumConnectionLimit = value;
            }
        }

        public int CurrentConnections => 0;


        public override int ReceiveBufferSize
        {
            get
            {
                return receiveBufferSize;
            }
            set
            {
                receiveBufferSize = value;
                base.ReceiveBufferSize = value;
            }
        }

        public override int SendBufferSize
        {
            get
            {
                return sendBufferSize;
            }
            set
            {
                sendBufferSize = value;
                base.SendBufferSize = value;
            }
        }



    }
}
