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
    public class TCPClient : IPV4Socket, IPacketClient
    {
        private readonly ILogger<TCPServer> logger = LoggerProvider.CreateLogger<TCPServer>();
        private CancellationTokenSource cancelTokenSource = null;
        private NetworkStream networkStream = null;
        private ClientHandlerAdapter handlerAdapter = null;
        private TaskCompletionSource stopCompleted = null;
        private readonly InternalNetSession session = new InternalNetSession();


        private Int32 receiveBufferSize = 8192;

        private Int32 sendBufferSize = 8192;

        public IConnectionSession Session => session;


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


        public void SetAdapter(ClientHandlerAdapter handlerAdapter)
        {
            this.handlerAdapter = handlerAdapter;
        }

        public Boolean Connected
        {
            get
            {
                return socket.Connected;
            }
        }

        public async ValueTask ConnectAsync(String address, int port, CancellationToken cancellationToken)
        {
            await ConnectAsync(IPAddress.Parse(address), port, cancellationToken);
        }

        public async ValueTask ConnectAsync(IPAddress address, int port, CancellationToken cancellationToken)
        {
            await ConnectAsync(new IPEndPoint(address, port), cancellationToken);
        }

        public async ValueTask ConnectAsync(Uri remoteUri, CancellationToken cancellationToken)
        {
            var querys = remoteUri.ParseQuery();
            if (querys.TryGetValue("readBuffer", out var readBufferSize))
            {
                this.ReceiveBufferSize = Int32.Parse(readBufferSize);
            }
            if (querys.TryGetValue("writeBuffer", out var writeBufferSize))
            {
                this.SendBufferSize = Int32.Parse(writeBufferSize);
            }
            await ConnectAsync(new IPEndPoint(IPAddress.Parse(remoteUri.Host), remoteUri.Port), cancellationToken);
        }


        public async ValueTask ConnectAsync(EndPoint remoteEP, CancellationToken cancellationToken)
        {
            if (cancelTokenSource != null) throw new Exception("不能重复连接");
            cancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await socket.ConnectAsync(remoteEP, cancelTokenSource.Token);
            _ = OnConnectedAsync(socket, cancelTokenSource.Token);
        }

        public async Task CloseAsync()
        {
            cancelTokenSource?.Cancel();
            cancelTokenSource = null;
            if (stopCompleted != null) await stopCompleted.Task;
        }


        private async Task OnConnectedAsync(Socket socket, CancellationToken cancellationToken)
        {
            stopCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Int32 minimumReadSize = 1;
            try
            {
                networkStream = new NetworkStream(socket, true);
                session.Init(networkStream, sendBufferSize);
                session.ConnectionId = 0;
                session.ConnectTime = TimeService.Default.LocalNow();
                var reader = PipeReader.Create(networkStream, new StreamPipeReaderOptions(bufferSize: receiveBufferSize));
                await handlerAdapter.OnConnection(session);
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
                session.Close(SessionShutdownCause.SHUTTING_DOWN);
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
                await handlerAdapter.OnClose(session);
                if (networkStream != null) await networkStream.DisposeAsync();
                networkStream = null;
                session.Clean();
                stopCompleted?.TrySetResult();
            }
        }

        public override void Dispose()
        {
            CloseAsync().Wait();
        }

    }
}
