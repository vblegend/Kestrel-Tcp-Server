using System.Buffers;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;
using System.IO.Pipelines;

namespace KestrelServer.Network
{
    public class TCPClient : IPV4Socket
    {
        private readonly IClientHandler clientHandler;
        private Int32 MinimumPacketLength = 0;
        private CancellationTokenSource? cancelTokenSource = null;
        private NetworkStream? networkStream = null;
        private PipeWriter? streamWriter = null;
        public TCPClient(IClientHandler clientAdapter) : base()
        {
            this.clientHandler = clientAdapter;
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
            long minimumReadSize = MinimumPacketLength;
            try
            {
                networkStream = new NetworkStream(socket, true);
                streamWriter = PipeWriter.Create(networkStream);
                var reader = PipeReader.Create(networkStream);
                await clientHandler.OnConnection(this);
                while (!cancellationToken.IsCancellationRequested && socket.Connected)
                {
                    var result = await reader.ReadAtLeastAsync((int)minimumReadSize, cancellationToken);
                    if (result.IsCompleted) break;
                    var len = await OnPacket(result.Buffer);
                    if (result.Buffer.Length < len)
                    {
                        minimumReadSize = len;
                        reader.AdvanceTo(result.Buffer.Start);
                        continue;
                    }
                    var packetData = result.Buffer.Slice(0, len);
                    await OnReceive(packetData);
                    reader.AdvanceTo(result.Buffer.GetPosition(len));
                    minimumReadSize = MinimumPacketLength;
                }
            }
            catch (Exception ex)
            {
                await clientHandler.OnError(ex);
            }
            finally
            {
                streamWriter = null;
                if (networkStream != null) await networkStream.DisposeAsync();
                networkStream = null;
                await clientHandler.OnClose(this);
            }
        }

        public void Write(ReadOnlySpan<byte> buffer)
        {
            streamWriter?.Write(buffer);
        }
        public void Write(ReadOnlyMemory<byte> buffer)
        {
            streamWriter?.Write(buffer.Span);
        }
        public void Write(ArraySegment<byte> buffer)
        {
            streamWriter?.Write(buffer);
        }
        public async Task WriteAsync(ArraySegment<byte> buffer)
        {
            if (streamWriter != null) await streamWriter.WriteAsync(buffer);
        }
        public async Task WriteAsync(ReadOnlyMemory<byte> buffer)
        {
            if (streamWriter != null) await streamWriter.WriteAsync(buffer);
        }
        public async Task FlushAsync()
        {
            if (streamWriter != null) await streamWriter.FlushAsync();
        }

        protected virtual async Task<UInt32> OnPacket(ReadOnlySequence<Byte> buffer)
        {
            return await Task.FromResult((UInt32)buffer.Length);
        }

        protected virtual async Task OnReceive(ReadOnlySequence<Byte> data)
        {
            await clientHandler.OnReceive(this, data);
        }

        public override void Dispose()
        {
            Close();
        }
    }
}
