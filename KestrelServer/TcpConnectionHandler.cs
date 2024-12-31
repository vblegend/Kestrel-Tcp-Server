using System.Buffers;
using System.Threading.Tasks;
using System;

using System.Text;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Net;
using KestrelServer.Message;
using Microsoft.Extensions.Logging;
using KestrelServer.Network;


namespace KestrelServer
{

    public class TCPConnectionHandler : TCPServer, IHostedService
    {
        public Int64 count = 0;
        private readonly IPBlacklistTrie iPBlacklist;
        private readonly TimeService timeService;
        private readonly GMessageParser messageParser;
        private readonly ILogger<TCPConnectionHandler> logger;


        public TCPConnectionHandler(IPBlacklistTrie iPBlacklist, TimeService timeService, GMessageParser messageParser, ILogger<TCPConnectionHandler> _logger) : base(_logger, timeService, 1)
        {
            this.timeService = timeService;
            this.iPBlacklist = iPBlacklist;
            this.messageParser = messageParser;
            this.logger = _logger;
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            this.Listen(IPAddress.Any, 50000);
            //await this.StopAsync();
            //this.Listen(IPAddress.Any, 50000);


            logger.LogInformation("TCP Server Listen: {0}", $"tcp://{IPAddress.Any}:{50000}");
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await this.StopAsync();
            logger.LogInformation($"TCP Server Stoped.");
        }

        protected override async Task<Boolean> OnConnected(IConnectionSession session)
        {
            //if (connection.RemoteEndPoint is IPEndPoint ipEndPoint)
            //{
            //    if (this.iPBlacklist.IsBlocked(ipEndPoint.Address))
            //    {
            //        logger.LogInformation($"Blocked Client Connect: {ipEndPoint.Address}");
            //        return false;
            //    }
            //}

            logger.LogInformation($"Client Connected: {session.ConnectionId}, ClientIp: {session.RemoteEndPoint}");
            await session.SendFlushAsync(GMessage.Create(1920, new StringPayload(timeService.Now().ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"))));
            return true;
        }


        protected override async Task OnClose(IConnectionSession session)
        {
            logger.LogInformation($"Client    Closed: {session.ConnectionId}, ClientIp: {session.RemoteEndPoint}");
            await Task.CompletedTask;
        }

        protected override async Task OnError(IConnectionSession session, Exception ex)
        {
            logger.LogError($"Client     Error: {session.ConnectionId}, {ex.Message}");
            await Task.CompletedTask;
        }


        protected override async Task<UInt32> OnPacket(IConnectionSession session, ReadOnlySequence<Byte> sequence)
        {
            var len = GMessage.ReadLength(new SequenceReader<byte>(sequence));
            if (len == uint.MaxValue || len > 64 * 1024)
            {
                await OnError(session, new Exception("检测到非法封包，即将关闭连接！"));
                session.Close(SessionShutdownCause.CLIENT_ILLEGAL_DATA);
            }
            return len;
        }



        protected override async Task OnReceive(IConnectionSession session, ReadOnlySequence<Byte> buffer)
        {
            var result = messageParser.Parse(new SequenceReader<byte>(buffer), out GMessage message);
            if (result == ParseResult.Illicit)
            {
                await OnError(session, new Exception("检测到非法封包，即将关闭连接！"));
                session.Close(SessionShutdownCause.CLIENT_ILLEGAL_DATA);
                return;
            }
            if (result == ParseResult.Ok)
            {
                count++;
                var text = $"Received packet: {count}";
                await session.SendFlushAsync(GMessage.Create(1920, new StringPayload(text)));
                if (count % 1000 == 0)
                {
                    logger.LogInformation(text);
                    //await session.FlushAsync();
                }
            }
            message.Return();
        }


    }
}
