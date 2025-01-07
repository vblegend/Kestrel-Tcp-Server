using Serilog;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;




namespace PacketNet.Network
{
    public abstract class TCPServer : IPV4Socket
    {
        private Int64 _currentConnectionCounter;
        private Int64 ConnectionIdSource;
        public UInt32 MinimumPacketLength = 1;
        private readonly ILogger logger = Log.ForContext<TCPServer>();
        private readonly InternalSessionPool sessionPool;

        private CancellationTokenSource listenCancelTokenSource = null;
        private TaskCompletionSource stopCompleted = null;

        protected TCPServer() : base()
        {
            this.sessionPool = new InternalSessionPool(Environment.ProcessorCount * 2);
        }


        public override void Dispose()
        {
            this.sessionPool.Dispose();
            base.Dispose();
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
            logger.Debug("Listen TCP Server: {0}:{1}", localAddress, localPort);
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
                logger.Debug("Listener closed.");
            }
            catch (Exception ex)
            {
                logger.Debug(ex, $"Listener Error {ex.GetType().FullName}.");
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
            InternalSession session = null;
            long minimumReadSize = MinimumPacketLength;
            try
            {
                Interlocked.Increment(ref _currentConnectionCounter);
                networkStream = new NetworkStream(socket, ownsSocket: true);
                var reader = PipeReader.Create(networkStream);
                session = sessionPool.Get();
                session.ConnectionId = Interlocked.Increment(ref ConnectionIdSource);
                session.ConnectTime = TimeService.Default.Now();
                session.Init(networkStream);
                var allowConnect = await this.OnConnected(session);
                if (allowConnect)
                {
                    while (!cancellationToken.IsCancellationRequested && socket.Connected)
                    {
                        var result = await reader.ReadAtLeastAsync((int)minimumReadSize, cancellationToken);
                        if (result.IsCompleted) break;
                        var len = await OnPacket(session, result.Buffer);
                        if (result.Buffer.Length < len)
                        {
                            minimumReadSize = len;
                            reader.AdvanceTo(result.Buffer.Start);
                            logger.Debug("Receive Partial Packet: {0}/{1}", result.Buffer.Length, len);
                            continue;
                        }
                        var packetData = result.Buffer.Slice(0, len);
                        await OnReceive(session, packetData);
                        reader.AdvanceTo(result.Buffer.GetPosition(len));
                        minimumReadSize = MinimumPacketLength;
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
                        if (session != null) await this.OnError(session, ex);
                    }
                }
                else
                {
                    if (session != null) await this.OnError(session, ex);
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

                    await this.OnClose(session);
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




        /// <summary>
        /// 新的客户端连接事件
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        protected virtual ValueTask<bool> OnConnected(IConnectionSession session)
        {
            return new ValueTask<bool>(true);
        }

        /// <summary>
        /// 客户端连接关闭
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        protected virtual ValueTask OnClose(IConnectionSession session)
        {
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Socket 不可恢复的异常
        /// </summary>
        /// <param name="session"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        protected virtual ValueTask OnError(IConnectionSession session, Exception ex)
        {
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// 收到任意封包，进行自定义解析
        /// </summary>
        /// <param name="session"></param>
        /// <param name="sequence"></param>
        /// <returns></returns>
        protected virtual ValueTask<UInt32> OnPacket(IConnectionSession session, ReadOnlySequence<Byte> sequence)
        {
            return new ValueTask<uint>((UInt32)sequence.Length);
        }

        /// <summary>
        /// 收到一个完整封包
        /// </summary>
        /// <param name="session"></param>
        /// <param name="sequence"></param>
        /// <returns></returns>
        protected virtual ValueTask OnReceive(IConnectionSession session, ReadOnlySequence<byte> sequence)
        {
            return ValueTask.CompletedTask;
        }
    }
}
