﻿using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using LightNet.Adapters;
using LightNet.Network;
using System;
using System.Buffers;
using System.IO.Pipelines;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using LightNet.Internals;

namespace LightNet.Pipes
{
    public class PipeClient : IPacketClient
    {
        private readonly ILogger<TCPServer> logger = LoggerProvider.CreateLogger<TCPServer>();
        private UInt32 _minimumPacketLength = 1;
        private CancellationTokenSource cancelTokenSource = null;
        private NamedPipeClientStream stream = null;
        private ClientHandlerAdapter handlerAdapter = null;

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
            var querys = QueryHelpers.ParseQuery(remoteUri.Query);
            if (!querys.ContainsKey("name")) throw new Exception("缺少参数 name");
            var uname = querys["name"];
            await ConnectAsync(remoteUri.Host, uname, cancellationToken);
        }


        public async ValueTask ConnectAsync(String serverName, String pipeName, CancellationToken cancellationToken)
        {
            if (cancelTokenSource != null) throw new Exception("不能重复连接");
            cancelTokenSource = new CancellationTokenSource();
            stream = new NamedPipeClientStream(serverName, pipeName, PipeDirection.InOut, System.IO.Pipes.PipeOptions.Asynchronous);

            await stream.ConnectAsync(cancellationToken);
            _ = OnConnectedAsync(cancelTokenSource.Token);
        }

        public void Close()
        {
            cancelTokenSource?.Cancel();
            stream.Dispose();
            cancelTokenSource = null;
        }


        private async Task OnConnectedAsync(CancellationToken cancellationToken)
        {
            long minimumReadSize = _minimumPacketLength;
            var session = new InternalPipeSession();
            try
            {
                session.ConnectionId = 0;
                session.ConnectTime = TimeService.Default.Now();
                session.Init(stream);
                var reader = PipeReader.Create(stream);
                await handlerAdapter.OnConnection(session);
                while (!cancellationToken.IsCancellationRequested && stream.IsConnected)
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
                if (stream != null) await stream.DisposeAsync();
                cancelTokenSource = null;
                stream = null;
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

        public void Dispose()
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

        public int ReceiveBufferSize
        {
            get
            {
                return 0;
            }
            set
            {
            }
        }
        public int SendBufferSize
        {
            get
            {
                return 0;
            }
            set
            {
            }
        }
    }
}