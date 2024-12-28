using System.Buffers;
using System.Threading.Tasks;
using System;

using System.Text;
using KestrelServer.Tcp;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Net;
using KestrelServer.Message;
using Microsoft.Extensions.Logging;


namespace KestrelServer
{

    public class MySessionData : ISessionData
    {
        public String UserName = "";
    }

    public class TCPConnectionHandler : TcpListenerService, IHostedService
    {
        public Int64 count = 0;
        private readonly IPBlacklistTrie iPBlacklist;
        private readonly UTCTimeService timeService;
        private readonly GMessageParser messageParser;
        private readonly ILogger<TCPConnectionHandler> logger;


        public TCPConnectionHandler(IPBlacklistTrie iPBlacklist, UTCTimeService timeService, GMessageParser messageParser, ILogger<TCPConnectionHandler> _logger) : base(_logger, timeService)
        {
            this.MinimumPacketLength = GMessage.MinimumSize;
            this.timeService = timeService;
            this.iPBlacklist = iPBlacklist;
            this.messageParser = messageParser;
            this.logger = _logger;
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            this.Listen(IPAddress.Any, 50000, cancellationToken);
            logger.LogInformation($"TCP Server Pipeline Running On Port {50000}");
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            this.Stop();
            logger.LogInformation($"TCP Server Pipeline Stoped .");
            await Task.CompletedTask;
        }




        protected override async Task<Boolean> OnConnected(IConnectionSession connection)
        {
            if (connection.RemoteEndPoint is System.Net.IPEndPoint ipEndPoint)
            {
                var ipOfBytes = ipEndPoint.Address.GetAddressBytes();
                if (this.iPBlacklist.IsBlocked(ipEndPoint.Address))
                {
                    logger.LogInformation($"Blocked Client Connect: {ipEndPoint.Address}");
                    return false;
                }
            }
            connection.Data = new MySessionData()
            {
                UserName = "root",
            };
            logger.LogInformation($"Client connected: {connection.ConnectionId} {connection.RemoteEndPoint} {connection.ConnectTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")}");
            await connection.SendAsync(GMessage.Create(1001, [111, 222, 333, 444], Encoding.UTF8.GetBytes(timeService.Now().ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"))));
            return true;
        }


        protected override async Task OnClose(IConnectionSession connection)
        {
            logger.LogInformation($"Client closed: {connection.ConnectionId}, ClientIp: {connection.RemoteEndPoint}, Username: {connection.Data}");
            await Task.CompletedTask;
        }

        protected override async Task OnError(IConnectionSession connection, Exception ex)
        {
            logger.LogError(ex, "???");
            await Task.CompletedTask;
        }


        protected override async Task<UInt32> OnPacket(IConnectionSession session, ReadOnlySequence<Byte> sequence)
        {
            var len = GMessage.ReadLength(new SequenceReader<byte>(sequence));
            if (len == uint.MaxValue || len > 64 * 1024)
            {
                await OnError(session, new Exception("检测到非法封包，即将关闭连接！"));
                session.Close();
            }
            return len;
        }



        protected override async Task OnReceive(IConnectionSession connection, ReadOnlySequence<Byte> buffer)
        {
            var result = messageParser.Parse(new SequenceReader<byte>(buffer), out GMessage message);
            if (result == ParseResult.Illicit)
            {
                connection.Close();
                return;
            }
            if (result == ParseResult.Ok)
            {
                if (++count % 1000 == 0)
                {
                    var text = $"Received packet: {count}";
                    logger.LogInformation(text);
                    await connection.WriteAsync(Encoding.UTF8.GetBytes(text));
                }
            }
            message.Return();
        }


    }
}
