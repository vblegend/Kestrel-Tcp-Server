using System.Net.Sockets;
using System.Net;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Buffers;
using System.IO.Pipelines;
using Microsoft.Extensions.Logging;


namespace KestrelServer.Tcp
{
    public abstract class TcpListenerService
    {
        private Int64 _currentConnectionCounter;
        private Int64 ConnectionIdSource;
        private UInt32 MinimumPacketLength = 0;
        private TcpListener? listener;
        private readonly ILogger<TcpListenerService> logger;
        private readonly TimeService timeService;

        private CancellationTokenSource? listenCancelTokenSource = null;
        private TaskCompletionSource? stopCompleted = null;


        protected TcpListenerService(ILogger<TcpListenerService> _logger, TimeService _timeService, UInt32 minimumPacketLength = 0)
        {
            this.logger = _logger;
            this.timeService = _timeService;
            this.MinimumPacketLength = minimumPacketLength;
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
            listener = new TcpListener(localAddress, localPort);
            listener.Start();
            listener.BeginAcceptSocket(new AsyncCallback(HandleAccepted), listenCancelTokenSource.Token);
            logger.LogTrace("Listen TCP Server: {0}:{1}", localAddress, localPort);
        }

        private void HandleAccepted(IAsyncResult result)
        {
            try
            {
                var cancelToken = (CancellationToken)result.AsyncState!;
                if (cancelToken.IsCancellationRequested) return;
                Socket clientSocket = listener!.EndAcceptSocket(result);
                _ = OnConnectedAsync(clientSocket, cancelToken);
                if (cancelToken.IsCancellationRequested) return;
                listener?.BeginAcceptSocket(new AsyncCallback(HandleAccepted), cancelToken);
                //logger.LogDebug("Accepted Next");
            }
            catch (ObjectDisposedException)
            {
                logger.LogDebug("Listener closed.");
            }
            catch (Exception ex)
            {

            }

        }


        /// <summary>
        /// 停止监听端口并断开所有客户端连接
        /// </summary>
        public async Task StopAsync()
        {
            listenCancelTokenSource?.Cancel();
            if (listener != null)
            {
                listener.Dispose();
                listener = null;
            }
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
            NetworkStream? networkStream = null;
            InternalSession? session = null;
            try
            {
                Interlocked.Increment(ref _currentConnectionCounter);
                long minimumReadSize = MinimumPacketLength;
                networkStream = new NetworkStream(socket, ownsSocket: true);
                var reader = PipeReader.Create(networkStream);
                session = SessionPool.Pool.Get();
                session.ConnectionId = Interlocked.Increment(ref ConnectionIdSource);
                session.ConnectTime = timeService.Now();
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
                            logger.LogDebug("Receive Partial Packet: {0}/{1}", result.Buffer.Length, len);
                            continue;
                        }
                        var packetData = result.Buffer.Slice(0, len);
                        await OnReceive(session, packetData);
                        reader.AdvanceTo(result.Buffer.GetPosition(len));
                        minimumReadSize = MinimumPacketLength;
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (ex.InnerException is SocketException socketEx)
                {
                    if (socketEx.SocketErrorCode == SocketError.ConnectionReset || socketEx.SocketErrorCode == SocketError.ConnectionAborted)
                    {

                    }
                    else
                    {
                        if (session != null)
                        {
                            await this.OnError(session, ex);
                        }
                    }
                    // 客户端主动关闭
                }
                else
                {
                    if(session != null)
                    {
                        await this.OnError(session, ex);
                    }
             
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
                    SessionPool.Pool.Return(session);
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
        protected virtual async Task<bool> OnConnected(IConnectionSession session)
        {
            return await Task.FromResult(true);
        }

        /// <summary>
        /// 客户端连接关闭
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        protected virtual async Task OnClose(IConnectionSession session)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// Socket 不可恢复的异常
        /// </summary>
        /// <param name="session"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        protected virtual async Task OnError(IConnectionSession session, Exception ex)
        {
            await Task.CompletedTask;
        }



        /// <summary>
        /// 收到封包，验证封包并返回本次要读取的长度
        /// </summary>
        /// <param name="session"></param>
        /// <param name="sequence"></param>
        /// <returns></returns>
        protected virtual async Task<UInt32> OnPacket(IConnectionSession session, ReadOnlySequence<Byte> sequence)
        {
            return await Task.FromResult((UInt32)sequence.Length);
        }

        /// <summary>
        /// 收到封包，经过OnPacket验证的合法封包
        /// </summary>
        /// <param name="session"></param>
        /// <param name="sequence"></param>
        /// <returns></returns>
        protected virtual async Task OnReceive(IConnectionSession session, ReadOnlySequence<byte> sequence)
        {
            await Task.CompletedTask;
        }





    }
}
