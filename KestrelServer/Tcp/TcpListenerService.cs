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
        protected UInt32 MinimumPacketLength = 0;
        private TcpListener? listener;
        private CancellationTokenSource listenCancelTokenSource = new CancellationTokenSource();
        private CancellationTokenSource linkedCts;
        private readonly ILogger<TcpListenerService> logger;
        private readonly UTCTimeService timeService;

        protected TcpListenerService(ILogger<TcpListenerService> _logger, UTCTimeService _timeService)
        {
            this.logger = _logger;
            this.timeService = _timeService;
        }



        /// <summary>
        /// 监听IP地址及端口，此方法不会阻塞
        /// </summary>
        /// <param name="localAddress"></param>
        /// <param name="localPort"></param>
        /// <param name="cancellationToken"></param>
        public void Listen(IPAddress localAddress, Int32 localPort, CancellationToken cancellationToken = default)
        {
            linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, listenCancelTokenSource.Token);
            listener = new TcpListener(localAddress, localPort);
            listener.Start();
            listener.BeginAcceptSocket(new AsyncCallback(HandleAccepted), linkedCts.Token);
            logger.LogDebug("Listen TCP Server: {0}:{1}", localAddress, localPort);
        }

        private void HandleAccepted(IAsyncResult result)
        {
            try
            {
                var linkedToken = (CancellationToken)result.AsyncState!;
                if (linkedToken.IsCancellationRequested || listener == null) return;
                Socket clientSocket = listener.EndAcceptSocket(result);
                listener.BeginAcceptSocket(new AsyncCallback(HandleAccepted), linkedToken);
                _ = OnConnectedAsync(clientSocket, linkedCts.Token);
            }
            catch (ObjectDisposedException)
            {
                logger.LogDebug("Listener closed.");
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Error in OnClientConnected.");
            }

        }


        /// <summary>
        /// 停止监听端口并断开所有客户端连接
        /// </summary>
        public void Stop()
        {
            listenCancelTokenSource.Cancel();
            if (listener != null)
            {
                listener.Dispose();
                listener = null;
            }
        }

        private async Task OnConnectedAsync(Socket socket, CancellationToken cancellationToken)
        {
            long minimumReadSize = MinimumPacketLength;
            var networkStream = new NetworkStream(socket, ownsSocket: true);
            var reader = PipeReader.Create(networkStream);
            var writer = PipeWriter.Create(networkStream);
            var session = new InternalSession();
            session.Init(socket, writer, timeService.Now());
            try
            {
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
                // 处理取消操作的异常逻辑 
                logger.LogDebug("Listener Operation Canceled.");
            }
            catch (Exception ex)
            {
                if (ex.InnerException is SocketException socketEx && socketEx.SocketErrorCode == SocketError.ConnectionReset)
                {
                    // 客户端主动关闭
                }
                else
                {
                    await this.OnError(session, ex);
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
                await this.OnClose(session);
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
