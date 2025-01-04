using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace KestrelServer.Network
{
    public class TCPClient : IPV4Socket
    {
        private readonly IClientHandler clientHandler;
        private Int32 MinimumPacketLength = 0;
        private CancellationTokenSource cancelTokenSource = null;
        private NetworkStream networkStream = null;
        protected PipeWriter streamWriter = null;
        public TCPClient(IClientHandler clientAdapter, Int32 minimumPacketLength) : base()
        {
            this.clientHandler = clientAdapter;
            this.MinimumPacketLength = minimumPacketLength;
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
            catch (OperationCanceledException) { }
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

        /// <summary>
        /// 将要发送的数据写入发送缓冲区
        /// </summary>
        /// <param name="buffer"></param>
        public void Write(ReadOnlySpan<byte> buffer)
        {
            streamWriter?.Write(buffer);
        }

        /// <summary>
        /// 将要发送的数据写入发送缓冲区
        /// </summary>
        /// <param name="buffer"></param>
        public void Write(ReadOnlyMemory<byte> buffer)
        {
            streamWriter?.Write(buffer.Span);
        }

        /// <summary>
        /// 将要发送的数据写入发送缓冲区
        /// </summary>
        /// <param name="buffer"></param>
        public void Write(ArraySegment<byte> buffer)
        {
            streamWriter?.Write(buffer);
        }

        /// <summary>
        /// 将要发送的数据写入发送缓冲区并立即提交
        /// </summary>
        /// <param name="buffer"></param>
        public async ValueTask WriteAsync(ArraySegment<byte> buffer)
        {
            if (streamWriter != null) await streamWriter.WriteAsync(buffer);
        }

        /// <summary>
        /// 将要发送的数据写入发送缓冲区并立即提交
        /// </summary>
        /// <param name="buffer"></param>
        public async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer)
        {
            if (streamWriter != null) await streamWriter.WriteAsync(buffer);
        }

        /// <summary>
        /// 将发送缓冲区数据立即提交
        /// </summary>
        /// <param name="buffer"></param>
        public async ValueTask FlushAsync()
        {
            if (streamWriter != null) await streamWriter.FlushAsync();
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

        /// <summary>
        /// 收到一个完整封包
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected virtual ValueTask OnReceive(ReadOnlySequence<Byte> data)
        {
            return clientHandler.OnReceive(this, data);
        }

        public override void Dispose()
        {
            Close();
        }
    }
}
