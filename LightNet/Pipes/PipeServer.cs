using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using LightNet.Adapters;
using LightNet.Internals;
using LightNet.Network;
using System;
using System.IO.Pipelines;
using System.IO.Pipes;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;


namespace LightNet.Pipes
{
    public class PipeServer : IPacketServer
    {
        private String PipeName;
        private Int64 _currentConnectionCounter;
        private Int64 ConnectionIdSource;
        public UInt32 _minimumPacketLength = 1;
        public Int32 _maximumConnectionLimit = 65535;
        private readonly ILogger<TCPServer> logger = LoggerProvider.CreateLogger<TCPServer>();
        private readonly InternalSessionPool<InternalPipeSession> sessionPool;

        private Int32 readBufferSize = 8192;
        private Int32 writeBufferSize = 8192;
        private CancellationTokenSource listenCancelTokenSource = null;
        private TaskCompletionSource stopCompleted = null;

        public ServerHandlerAdapter handlerAdapter;



        public PipeServer()
        {
            this.sessionPool = new InternalSessionPool<InternalPipeSession>(Environment.ProcessorCount * 2);
        }

        public void SetAdapter(ServerHandlerAdapter handlerAdapter)
        {
            this.handlerAdapter = handlerAdapter;
        }

        public void Dispose()
        {
            this.StopAsync().Wait();
            this.sessionPool.Dispose();
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
            var querys = QueryHelpers.ParseQuery(uri.Query);
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
                System.IO.Pipes.PipeOptions.Asynchronous,  // 管道选项
                writeBufferSize,               // 输入缓冲区大小
                readBufferSize                // 输出缓冲区大小
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
            long minimumReadSize = _minimumPacketLength;
            try
            {
                Interlocked.Increment(ref _currentConnectionCounter);
                if (_currentConnectionCounter > _maximumConnectionLimit)
                {
                    throw new Exception("超出连接数");
                }
                var reader = PipeReader.Create(serverStream);
                session = sessionPool.Get();
                session.ConnectionId = Interlocked.Increment(ref ConnectionIdSource);
                session.ConnectTime = TimeService.Default.Now();
                session.Init(serverStream);
                var allowConnect = await handlerAdapter.OnConnected(session);
                if (allowConnect)
                {
                    while (!cancellationToken.IsCancellationRequested && serverStream.IsConnected)
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

                if (serverStream != null)
                {
                    await serverStream.DisposeAsync();
                    serverStream = null;
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
                return readBufferSize;
            }
            set
            {
                if (this.PipeName != null) throw new Exception("不支持运行时更改");
                readBufferSize = value;
            }
        }
        public int SendBufferSize
        {
            get
            {
                return writeBufferSize;
            }
            set
            {
                if (this.PipeName != null) throw new Exception("不支持运行时更改");
                writeBufferSize = value;
            }
        }
    }
}
