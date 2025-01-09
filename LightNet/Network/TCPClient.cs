using Microsoft.Extensions.Logging;
using LightNet.Adapters;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using LightNet.Internals;

namespace LightNet.Network
{
    public class TCPClient : IPV4Socket, IPacketClient
    {
        private readonly ILogger<TCPServer> logger = LoggerProvider.CreateLogger<TCPServer>();
        private UInt32 _minimumPacketLength = 1;
        private CancellationTokenSource cancelTokenSource = null;
        private NetworkStream networkStream = null;
        private ClientHandlerAdapter handlerAdapter = null;

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
            await ConnectAsync(new IPEndPoint(IPAddress.Parse(remoteUri.Host), remoteUri.Port), cancellationToken);
        }


        public async ValueTask ConnectAsync(EndPoint remoteEP, CancellationToken cancellationToken)
        {
            if (cancelTokenSource != null) throw new Exception("不能重复连接");
            cancelTokenSource = new CancellationTokenSource();
            await socket.ConnectAsync(remoteEP, cancelTokenSource.Token);
            _ = OnConnectedAsync(socket, cancelTokenSource.Token);
        }

        public void Close()
        {
            cancelTokenSource?.Cancel();
            socket.Close();
            cancelTokenSource = null;
        }


        private async Task OnConnectedAsync(Socket socket, CancellationToken cancellationToken)
        {
            long minimumReadSize = _minimumPacketLength;
            var session = new InternalNetSession();
            try
            {
                networkStream = new NetworkStream(socket, true);
                session.Init(networkStream);
                session.ConnectionId = 0;
                session.ConnectTime = TimeService.Default.Now();
                var reader = PipeReader.Create(networkStream);
                await handlerAdapter.OnConnection(session);
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
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                await handlerAdapter.OnError(session, ex);
            }
            finally
            {
                if (networkStream != null) await networkStream.DisposeAsync();
                networkStream = null;
                await handlerAdapter.OnClose(session);
                session.Clean();
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

        public override void Dispose()
        {
            Close();
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
    }
}
