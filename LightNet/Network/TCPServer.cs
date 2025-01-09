using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using LightNet.Adapters;
using LightNet.Internals;
using System;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;




namespace LightNet.Network
{
    public class TCPServer : IPV4Socket, IPacketServer
    {
        private Int64 _currentConnectionCounter;
        private Int64 ConnectionIdSource;
        public UInt32 _minimumPacketLength = 1;
        public Int32 _maximumConnectionLimit = 65535;
        private readonly ILogger<TCPServer> logger = LoggerProvider.CreateLogger<TCPServer>();
        private readonly InternalSessionPool<InternalNetSession> sessionPool;

        private CancellationTokenSource listenCancelTokenSource = null;
        private TaskCompletionSource stopCompleted = null;
        private ServerHandlerAdapter handlerAdapter = null;
        public TCPServer() : base()
        {
            this.sessionPool = new InternalSessionPool<InternalNetSession>(Environment.ProcessorCount * 2);
        }


        public void SetAdapter(ServerHandlerAdapter handlerAdapter)
        {
            this.handlerAdapter = handlerAdapter;
        }


        public override void Dispose()
        {
            this.sessionPool.Dispose();
            base.Dispose();
        }


        /// <summary>
        /// use tcp://0.0.0.0:5000
        /// </summary>
        /// <param name="uri"></param>
        public void Listen(Uri uri)
        {
            if (uri == null) throw new Exception("参数不能为空");
            if (uri.Scheme != "tcp") throw new Exception("不支持的协议");
            var querys = QueryHelpers.ParseQuery(uri.Query);
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


        /// <summary>
        /// 监听IP地址及端口，此方法不会阻塞
        /// </summary>
        /// <param name="localAddress"></param>
        /// <param name="localPort"></param>
        /// <param name="cancellationToken"></param>
        public void Listen(IPAddress localAddress, Int32 localPort)
        {
            if (listenCancelTokenSource != null)
            {
                throw new Exception("The listener cannot work twice.");
            }
            listenCancelTokenSource = new CancellationTokenSource();
            stopCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            socket.Bind(new IPEndPoint(localAddress, localPort));
            socket.Listen(Int32.MaxValue);
            socket.BeginAccept(new AsyncCallback(HandleAccepted), listenCancelTokenSource.Token);
            logger.LogDebug("Listen TCP Server: {0}:{1}", localAddress, localPort);
        }

        private void HandleAccepted(IAsyncResult result)
        {
            try
            {
                var cancelToken = (CancellationToken)result.AsyncState!;
                if (cancelToken.IsCancellationRequested) return;
                Socket clientSocket = socket.EndAccept(result);
                _ = OnConnectedAsync(clientSocket, cancelToken);
                if (cancelToken.IsCancellationRequested) return;
                socket.BeginAccept(new AsyncCallback(HandleAccepted), cancelToken);
            }
            catch (ObjectDisposedException)
            {
                logger.LogDebug("Listener closed.");
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, $"Listener Error {ex.GetType().FullName}.");
            }

        }


        /// <summary>
        /// 停止监听端口并断开所有客户端连接
        /// </summary>
        public async Task StopAsync()
        {
            listenCancelTokenSource?.Cancel();
            base.Dispose();
            var count = Interlocked.Read(ref _currentConnectionCounter);
            if (stopCompleted != null && count > 0)
            {
                await stopCompleted.Task;
                stopCompleted = null;
            }
            listenCancelTokenSource = null;
        }

        private async Task OnConnectedAsync(Socket socket, CancellationToken cancellationToken)
        {
            NetworkStream networkStream = null;
            InternalNetSession session = null;
            long minimumReadSize = _minimumPacketLength;
            try
            {
                Interlocked.Increment(ref _currentConnectionCounter);
                if (_currentConnectionCounter > _maximumConnectionLimit)
                {
                    throw new Exception("超出连接数");
                }
                networkStream = new NetworkStream(socket, ownsSocket: true);
                var reader = PipeReader.Create(networkStream);
                session = sessionPool.Get();
                session.ConnectionId = Interlocked.Increment(ref ConnectionIdSource);
                session.ConnectTime = TimeService.Default.Now();
                session.Init(networkStream);
                var allowConnect = await handlerAdapter.OnConnected(session);
                if (allowConnect)
                {
                    while (!cancellationToken.IsCancellationRequested && socket.Connected)
                    {
                        var result = await reader.ReadAtLeastAsync((int)minimumReadSize, cancellationToken);
                        if (result.IsCompleted) break;
                        var parseResult = await handlerAdapter.OnPacket(session, result.Buffer);
                        if (parseResult.IsCompleted)
                        {
                            reader.AdvanceTo(result.Buffer.GetPosition(parseResult.Length));
                            minimumReadSize = _minimumPacketLength;
                        }
                        else
                        {
                            minimumReadSize = parseResult.Length;
                            reader.AdvanceTo(result.Buffer.Start);
                            logger.LogDebug("Receive Partial Packet: {0}/{1}", result.Buffer.Length, minimumReadSize);
                        }
                    }
                }
                else
                {
                    session.Close(SessionShutdownCause.NONE);
                }
            }
            catch (OperationCanceledException)
            {
                session?.Close(SessionShutdownCause.SERVER_SHUTTING_DOWN);
            }
            catch (Exception ex)
            {
                if (ex.InnerException is SocketException socketEx)
                {
                    if (socketEx.SocketErrorCode == SocketError.ConnectionReset || socketEx.SocketErrorCode == SocketError.ConnectionAborted)
                    {
                        session?.Close(SessionShutdownCause.CLIENT_DISCONNECTED);
                        // 客户端主动关闭
                    }
                    else
                    {
                        if (session != null) await handlerAdapter.OnError(session, ex);
                    }
                }
                else
                {
                    if (session != null) await handlerAdapter.OnError(session, ex);
                }
            }
            finally
            {
                socket.Close();
                if (networkStream != null)
                {
                    await networkStream.DisposeAsync();
                    networkStream = null;
                }
                if (session != null)
                {

                    await handlerAdapter.OnClose(session);
                    sessionPool.Return(session);
                }
                if (Interlocked.Decrement(ref _currentConnectionCounter) == 0)
                {
                    if (this.listenCancelTokenSource != null && this.listenCancelTokenSource.IsCancellationRequested)
                    {
                        stopCompleted?.TrySetResult();
                    }
                }

            }

        }

        public UInt32 MinimumPacketLength
        {
            get
            {
                return _minimumPacketLength;
            }
            set
            {
                _minimumPacketLength = value;
            }
        }

        public Int32 MaximumConnectionLimit
        {
            get
            {
                return _maximumConnectionLimit;
            }
            set
            {
                _maximumConnectionLimit = value;
            }
        }

        public Int32 CurrentConnections
        {
            get
            {
                return (Int32)_currentConnectionCounter;
            }
        }
    }
}
