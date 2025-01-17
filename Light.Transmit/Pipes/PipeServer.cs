using Light.Transmit.Adapters;
using Light.Transmit.Internals;
using Light.Transmit.Network;
using Microsoft.Extensions.Logging;
using System;
using System.IO.Pipelines;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;


namespace Light.Transmit.Pipes
{
    public class PipeServer : IPacketServer
    {
        private String PipeName;
        private Int64 _currentConnectionCounter;
        private Int64 ConnectionIdSource;
        public Int32 _maximumConnectionLimit = 65535;
        private readonly ILogger<TCPServer> logger = LoggerProvider.CreateLogger<TCPServer>();

        private Int32 receiveBufferSize = 8192;
        private Int32 sendBufferSize = 8192;
        private CancellationTokenSource listenCancelTokenSource = null;
        private TaskCompletionSource stopCompleted = null;

        public ServerHandlerAdapter handlerAdapter;




        public PipeServer()
        {

        }

        public void SetAdapter(ServerHandlerAdapter handlerAdapter)
        {
            this.handlerAdapter = handlerAdapter;
        }

        public void Dispose()
        {
            this.StopAsync().Wait();
        }

        /// <summary>
        /// use pipe://Named.xx:1
        /// </summary>
        /// <param name="uri"></param>
        public void Listen(Uri uri)
        {
            if (uri == null) throw new Exception("参数不能为空");
            if (uri.Scheme != "pipe") throw new Exception("不支持的协议");
            if (uri.Host != ".") throw new Exception("host 只能是.");
            var querys = uri.ParseQuery();
            if (!querys.ContainsKey("name")) throw new Exception("缺少参数 name");
            var uname = querys["name"];
            if (querys.TryGetValue("readBuffer", out var readBufferSize))
            {
                this.ReceiveBufferSize = Int32.Parse(readBufferSize);
            }
            if (querys.TryGetValue("writeBuffer", out var writeBufferSize))
            {
                this.SendBufferSize = Int32.Parse(writeBufferSize);
            }
            Listen(uname);
        }

        /// <summary>
        /// 监听IP地址及端口，此方法不会阻塞
        /// </summary>
        /// <param name="localAddress"></param>
        /// <param name="localPort"></param>
        /// <param name="cancellationToken"></param>
        public void Listen(String pipeName)
        {
            this.PipeName = pipeName;
            if (listenCancelTokenSource != null)
            {
                throw new Exception("The listener cannot work twice.");
            }
            listenCancelTokenSource = new CancellationTokenSource();
            stopCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            ThreadPool.QueueUserWorkItem(HandleAccepted, listenCancelTokenSource.Token);
            logger.LogDebug("Listen Pipe Server: {0}", pipeName);
        }


        private NamedPipeServerStream CreatePipeStream()
        {
            return new NamedPipeServerStream(
                this.PipeName,                // 管道名称
                PipeDirection.InOut,         // 管道方向（双向）
                NamedPipeServerStream.MaxAllowedServerInstances,                      // 最大实例数
                PipeTransmissionMode.Byte, // 传输模式
                PipeComponent.DEFAULT_PIPE_OPTIONS,  // 异步管道
                sendBufferSize,               // 输入缓冲区大小
                receiveBufferSize                // 输出缓冲区大小
            );
        }



        private async void HandleAccepted(Object state)
        {
            var cancelToken = (CancellationToken)state;
            try
            {
                while (!cancelToken.IsCancellationRequested)
                {
                    var server = CreatePipeStream();
                    await server.WaitForConnectionAsync(cancelToken);
                    _ = OnConnectedAsync(server, cancelToken);
                }
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
                listenCancelTokenSource?.Cancel();
            }

        }


        /// <summary>
        /// 停止监听端口并断开所有客户端连接
        /// </summary>
        public async Task StopAsync()
        {
            listenCancelTokenSource?.Cancel();
            var count = Interlocked.Read(ref _currentConnectionCounter);
            if (stopCompleted != null && count > 0)
            {
                await stopCompleted.Task;
                stopCompleted = null;
            }
            listenCancelTokenSource = null;
        }

        private async Task OnConnectedAsync(NamedPipeServerStream serverStream, CancellationToken cancellationToken)
        {
            InternalPipeSession session = null;
            Int32 minimumReadSize = 1;
            try
            {
                // 连接数限制
                if (_currentConnectionCounter >= _maximumConnectionLimit) return;
                // 服务器同意
                //if (!handlerAdapter.OnAccept(socket)) return;
                Interlocked.Increment(ref _currentConnectionCounter);
                // 连接数限制
                var reader = PipeReader.Create(serverStream, new StreamPipeReaderOptions(bufferSize: receiveBufferSize));
                session = new InternalPipeSession();
                session.ConnectionId = Interlocked.Increment(ref ConnectionIdSource);
                session.ConnectTime = TimeService.Default.LocalNow();
                session.Init(serverStream, sendBufferSize);
                await handlerAdapter.OnConnected(session);
                while (!cancellationToken.IsCancellationRequested && serverStream.IsConnected)
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
                        // 客户端主动关闭
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

                if (serverStream != null)
                {
                    await serverStream.DisposeAsync();
                    serverStream = null;
                }
                if (session != null)
                {
                    await handlerAdapter.OnClose(session);
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

        public Int32 MaximumConnectionLimit
        {
            get
            {
                return _maximumConnectionLimit;
            }
            set
            {
                if (value < 1 || value > 254)
                {
                    throw new Exception("The value should be between 1 and 254");
                }
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


        public int ReceiveBufferSize
        {
            get
            {
                return receiveBufferSize;
            }
            set
            {
                if (this.PipeName != null) throw new Exception("不支持运行时更改");
                receiveBufferSize = value;
            }
        }
        public int SendBufferSize
        {
            get
            {
                return sendBufferSize;
            }
            set
            {
                if (this.PipeName != null) throw new Exception("不支持运行时更改");
                sendBufferSize = value;
            }
        }
    }
}
