using LightNet;
using LightNet.Message;
using System.Buffers;

namespace Examples.Services
{
    public class TestServerService : MessageServer, IHostedService
    {
        private readonly ILogger<TestServerService> logger;
        private readonly TestMessageProcessService messageProcessor;
        private readonly ApplicationOptions applicationOptions;

        public TestServerService(ILogger<TestServerService> _logger, TestMessageProcessService _messageProcessor, ApplicationOptions applicationOptions)
        {
            this.applicationOptions = applicationOptions;
            messageProcessor = _messageProcessor;
            logger = _logger;
        }



        public async Task StartAsync(CancellationToken cancellationToken)
        {
            Listen(applicationOptions.ServerUri);
            logger.LogInformation("TCP Server Listen: {0}", applicationOptions.ServerUri);
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await StopAsync();
            logger.LogInformation($"TCP Server Stoped.");
        }


        public override async ValueTask<bool> OnConnected(IConnectionSession session)
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


        public override async ValueTask OnClose(IConnectionSession session)
        {
            logger.LogInformation("Client    Closed: {0}, ClientIp: {1}", session.ConnectionId, session.RemoteEndPoint);
            await ValueTask.CompletedTask;
        }

        public override async ValueTask OnError(IConnectionSession session, Exception ex)
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
