using PacketNet.Message;
using PacketNet.Network;
using System.Buffers;
using System.Net;

namespace Examples
{
    public class ExampleServer : MessageTCPServer, IHostedService
    {
        private readonly ILogger<ExampleServer> logger;
        private readonly MessageProcessor messageProcessor;

        public ExampleServer(ILogger<ExampleServer> _logger, MessageProcessor _messageProcessor)
        {
            messageProcessor = _messageProcessor;
            logger = _logger;
        }



        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Listen(IPAddress.Any, 50000);
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

            logger.LogInformation("Client Connected: {0}, ClientIp: {1}", session.ConnectionId, session.RemoteEndPoint);
            await session.WriteFlushAsync(MessageFactory.Create<ExampleMessage>());
            return true;
        }


        protected override async ValueTask OnClose(IConnectionSession session)
        {
            logger.LogInformation("Client    Closed: {0}, ClientIp: {1}", session.ConnectionId, session.RemoteEndPoint);
            await ValueTask.CompletedTask;
        }

        protected override async ValueTask OnError(IConnectionSession session, Exception ex)
        {
            logger.LogError($"Client     Error: {session.ConnectionId}, {ex.Message}");
            await ValueTask.CompletedTask;
        }

        public override async ValueTask OnReceive(AbstractNetMessage message)
        {
           await messageProcessor.WriteMessageAsync(message);
        }
    }
}
