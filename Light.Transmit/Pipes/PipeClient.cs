using Light.Transmit.Adapters;
using Light.Transmit.Internals;
using Light.Transmit.Network;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace Light.Transmit.Pipes
{
    public class PipeClient : IPacketClient
    {
        private readonly ILogger<TCPServer> logger = LoggerProvider.CreateLogger<TCPServer>();
        private CancellationTokenSource cancelTokenSource = null;
        private NamedPipeClientStream stream = null;
        private ClientHandlerAdapter handlerAdapter = null;
        private TaskCompletionSource stopCompleted = null;
        private readonly InternalPipeSession session = new InternalPipeSession();



        private Int32 receiveBufferSize = 8192;

        private Int32 sendBufferSize = 8192;

        public int ReceiveBufferSize
        {
            get
            {
                return receiveBufferSize;
            }
            set
            {
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
                sendBufferSize = value;
            }
        }


        public void SetAdapter(ClientHandlerAdapter handlerAdapter)
        {
            this.handlerAdapter = handlerAdapter;
        }

        public Boolean Connected
        {
            get
            {
                return stream.IsConnected;
            }
        }

        public async ValueTask ConnectAsync(Uri remoteUri, CancellationToken cancellationToken)
        {
            var querys = remoteUri.ParseQuery();
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
            await ConnectAsync(remoteUri.Host, uname, cancellationToken);
        }


        public async ValueTask ConnectAsync(String serverName, String pipeName, CancellationToken cancellationToken)
        {
            if (cancelTokenSource != null) throw new Exception("不能重复连接");
            cancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var stream = new NamedPipeClientStream(serverName, pipeName, PipeDirection.InOut, PipeComponent.DEFAULT_PIPE_OPTIONS);
            await stream.ConnectAsync(cancellationToken);
            _ = OnConnectedAsync(stream, cancelTokenSource.Token);
        }

        public async Task CloseAsync()
        {
            cancelTokenSource?.Cancel();
            cancelTokenSource = null;
            if (stopCompleted != null) await stopCompleted.Task;
        }


        private async Task OnConnectedAsync(NamedPipeClientStream stream, CancellationToken cancellationToken)
        {
            this.stream = stream;
            stopCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Int32 minimumReadSize = 1;

            try
            {
                session.ConnectionId = 0;
                session.ConnectTime = TimeService.Default.LocalNow();
                session.Init(stream, sendBufferSize);
                var reader = PipeReader.Create(stream, new StreamPipeReaderOptions(bufferSize: receiveBufferSize));
                await handlerAdapter.OnConnection(session);
                while (!cancellationToken.IsCancellationRequested && stream.IsConnected)
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
                session.Close(SessionShutdownCause.SHUTTING_DOWN);
            }
            catch (Exception ex)
            {
                await handlerAdapter.OnError(session, ex);
                session.Close(SessionShutdownCause.ERROR);
            }
            finally
            {
                await handlerAdapter.OnClose(session);
                if (stream != null) await stream.DisposeAsync();
                cancelTokenSource = null;
                stream = null;
                session.Clean();
                stopCompleted?.TrySetResult();
            }
        }


        /// <summary>
        /// 收到任意封包，进行自定义解析
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns>一个有效封包的长度</returns>
        protected virtual ValueTask<UInt32> OnPacket(ReadOnlySequence<Byte> buffer)
        {
            return new ValueTask<UInt32>((UInt32)buffer.Length);
        }

        public void Dispose()
        {
            CloseAsync().Wait();
        }

    }
}
