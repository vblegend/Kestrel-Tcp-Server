using Light.Transmit.Adapters;
using Light.Transmit.Internals;
using Microsoft.Extensions.Logging;
using System;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;




namespace Light.Transmit.Network
{
    public class TCPServer : IPV4Socket, IPacketServer
    {
        private Int64 _currentConnectionCounter;
        private Int64 ConnectionIdSource;
        public Int32 _maximumConnectionLimit = 65535;
        private readonly ILogger<TCPServer> logger = LoggerProvider.CreateLogger<TCPServer>();
        private CancelCompletionSignal cancelCompletionSignal = new CancelCompletionSignal(true);
        private ServerHandlerAdapter handlerAdapter = null;

        private Int32 receiveBufferSize = 8192;
        private Int32 sendBufferSize = 8192;




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




        public TCPServer() : base()
        {
        }


        public void SetAdapter(ServerHandlerAdapter handlerAdapter)
        {
            this.handlerAdapter = handlerAdapter;
        }


        public override void Dispose()
        {
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


        /// <summary>
        /// 监听IP地址及端口，此方法不会阻塞
        /// </summary>
        /// <param name="localAddress"></param>
        /// <param name="localPort"></param>
        /// <param name="cancellationToken"></param>
        public void Listen(IPAddress localAddress, Int32 localPort)
        {
            if (!cancelCompletionSignal.IsComplete) throw new Exception("服务已经启动");
            cancelCompletionSignal.Reset();
            socket.Bind(new IPEndPoint(localAddress, localPort));
            socket.Listen(Int32.MaxValue);
            socket.BeginAccept(new AsyncCallback(HandleAccepted), cancelCompletionSignal.Token);
            logger.LogDebug("Listen TCP Server: {0}:{1}", localAddress, localPort);
        }

        private async void HandleAccepted(IAsyncResult result)
        {
            try
            {
                var cancelToken = (CancellationToken)result.AsyncState;
                if (cancelToken.IsCancellationRequested) return;
                Socket clientSocket = socket.EndAccept(result);
                _ = Task.Run(() => OnConnectedAsync(clientSocket, cancelToken));
                if (cancelToken.IsCancellationRequested) return;
                socket.BeginAccept(new AsyncCallback(HandleAccepted), cancelToken);

                await socket.AcceptAsync();


            }
            catch (ObjectDisposedException)
            {
                logger.LogDebug("Listener closed.");
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, $"Listener Error {ex.GetType().FullName}.");
            }
            finally
            {

            }
        }


        /// <summary>
        /// 停止监听端口并断开所有客户端连接
        /// </summary>
        public async Task StopAsync()
        {   // dispose socket
            base.Dispose();
            // 标志为取消状态
            cancelCompletionSignal.Cancel();
            var count = Interlocked.Read(ref _currentConnectionCounter);
            // 等待所有客户端释放
            if (count > 0) await cancelCompletionSignal.CancelAsync();
        }

        private async Task OnConnectedAsync(Socket socket, CancellationToken cancellationToken)
        {
            NetworkStream networkStream = null;
            InternalNetSession session = null;
            Int32 minimumReadSize = 1;
            try
            {
                // 连接数限制
                if (_currentConnectionCounter >= _maximumConnectionLimit) return;
                // 服务器同意
                if (!handlerAdapter.OnAccept(socket)) return;
                Interlocked.Increment(ref _currentConnectionCounter);
                socket.ReceiveBufferSize = this.ReceiveBufferSize;
                socket.SendBufferSize = this.SendBufferSize;
                networkStream = new NetworkStream(socket, ownsSocket: true);
                session = new InternalNetSession();
                session.ConnectionId = Interlocked.Increment(ref ConnectionIdSource);
                session.ConnectTime = TimeService.Default.LocalNow();
                var writer = PipeWriter.Create(networkStream, new StreamPipeWriterOptions(minimumBufferSize: sendBufferSize));
                var reader = PipeReader.Create(networkStream, new StreamPipeReaderOptions(bufferSize: receiveBufferSize));
                session.Init(socket, writer);
                await handlerAdapter.OnConnected(session);


                while (!cancellationToken.IsCancellationRequested && socket.Connected)
                {
                    var result = await reader.ReadAtLeastAsync(minimumReadSize, cancellationToken);
                    if (result.IsCompleted) break;
                    var RESULT = handlerAdapter.OnPacket(session, result.Buffer);
                    reader.AdvanceTo(result.Buffer.GetPosition(RESULT.ReadLength));
                    minimumReadSize = RESULT.NextReadLength;
                }
                if (cancellationToken.IsCancellationRequested)
                {
                    session.Close(SessionShutdownCause.SHUTTING_DOWN);
                }
                else
                {
                    session.Close(SessionShutdownCause.UNEXPECTED_DISCONNECTED);
                }
            }
            catch (OperationCanceledException)
            {
                session?.Close(SessionShutdownCause.SHUTTING_DOWN);
            }
            catch (Exception ex)
            {
                if (ex.InnerException is SocketException socketEx)
                {
                    if (socketEx.SocketErrorCode == SocketError.ConnectionReset || socketEx.SocketErrorCode == SocketError.ConnectionAborted)
                    {
                        session?.Close(SessionShutdownCause.UNEXPECTED_DISCONNECTED);
                    }
                    else
                    {
                        await handlerAdapter.OnError(session, ex);
                    }
                }
                else
                {
                    await handlerAdapter.OnError(session, ex);
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
                    
                }
                if (Interlocked.Decrement(ref _currentConnectionCounter) == 0)
                {
                    if (cancelCompletionSignal.IsCancellationRequested)
                    {
                        cancelCompletionSignal.Complete();
                    }
                }

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
