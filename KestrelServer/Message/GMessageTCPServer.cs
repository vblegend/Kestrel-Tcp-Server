using System.Buffers;
using System.Threading.Tasks;
using System;

using System.Text;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Net;
using Microsoft.Extensions.Logging;
using KestrelServer.Network;


namespace KestrelServer.Message
{

    public class GMessageTCPServer : TCPServer, IHostedService
    {
        public long count = 0;
        private readonly IPBlacklistTrie iPBlacklist;
        private readonly TimeService timeService;
        private readonly GMessageParser messageParser;
        private readonly ILogger<GMessageTCPServer> logger;


        public GMessageTCPServer(IPBlacklistTrie iPBlacklist, TimeService timeService, GMessageParser messageParser, ILogger<GMessageTCPServer> _logger) : base(_logger, timeService, 5)
        {
            this.timeService = timeService;
            this.iPBlacklist = iPBlacklist;
            this.messageParser = messageParser;
            logger = _logger;
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Listen(IPAddress.Any, 50000);
            //await this.StopAsync();
            //this.Listen(IPAddress.Any, 50000);


            logger.LogInformation("TCP Server Listen: {0}", $"tcp://{IPAddress.Any}:{50000}");
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await StopAsync();
            logger.LogInformation($"TCP Server Stoped.");
        }

        protected override async ValueTask<bool> OnConnected(IConnectionSession session)
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
            await session.WriteFlushAsync(MessageFactory.ExampleMessage(DateTime.UtcNow.Ticks));
            return true;
        }


        protected override async ValueTask OnClose(IConnectionSession session)
        {
            logger.LogInformation($"Client    Closed: {session.ConnectionId}, ClientIp: {session.RemoteEndPoint}");
            await ValueTask.CompletedTask;
        }

        protected override async ValueTask OnError(IConnectionSession session, Exception ex)
        {
            logger.LogError($"Client     Error: {session.ConnectionId}, {ex.Message}");
            await ValueTask.CompletedTask;
        }


        protected override async ValueTask<uint> OnPacket(IConnectionSession session, ReadOnlySequence<byte> sequence)
        {
            var len = messageParser.ReadFullLength(new SequenceReader<byte>(sequence));
            if (len == uint.MaxValue || len > 64 * 1024)
            {
                await OnError(session, new Exception("检测到非法封包，即将关闭连接！"));
                session.Close(SessionShutdownCause.CLIENT_ILLEGAL_DATA);
            }
            return len;
        }



        protected override async ValueTask OnReceive(IConnectionSession session, ReadOnlySequence<byte> buffer)
        {
            var result = messageParser.Parse(new SequenceReader<byte>(buffer), out AbstractNetMessage message);
            if (result == ParseResult.Illicit)
            {
                await OnError(session, new Exception("检测到非法封包，即将关闭连接！"));
                session.Close(SessionShutdownCause.CLIENT_ILLEGAL_DATA);
                return;
            }
            if (result == ParseResult.Ok)
            {
     
                count++;
                //await session.WriteFlushAsync(MessageFactory.ExampleMessage(count));

                if (count % 100000 == 0)
                {
                    logger.LogInformation("Received packet: {0}", count);
                }
                message.Return();
            }
       
        }


    }
}
