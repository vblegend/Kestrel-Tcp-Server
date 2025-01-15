using Examples.Client;
using Examples.Middleware;
using Light.Message;
using Light.Transmit;
using System.Buffers;
using System.Threading.Channels;

namespace Examples.Services
{
    public class MessageServerService : MessageServer, IHostedService
    {
        private readonly ILogger<MessageServerService> logger;
        private readonly ApplicationOptions applicationOptions;
        private readonly ChannelWriter<AbstractNetMessage> channelWriter;
        public MessageServerService(ILogger<MessageServerService> _logger, ClientMessageProcessService _messageProcessor, ApplicationOptions applicationOptions)
            : base(MessageResolvers.CSResolver)
        {
            channelWriter = _messageProcessor.GetWriter;
            this.applicationOptions = applicationOptions;
            logger = _logger;
        }



        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var querys = applicationOptions.ServerUri.ParseQuery();
            if (querys.TryGetValue("pwd", out var pwd))
            {
                this.Middlewares.Add(new GatewayAuthMiddleware(pwd.ToString()));
                logger.LogInformation("MessageServer Use AuthMiddleware [ authorization: {0} ]", pwd);
            }

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

            logger.LogInformation("SERVER {0}[{1}], ClientIp: {2}", "CONNECTED", session.ConnectionId, session.RemoteEndPoint);
            await session.WriteFlushAsync(MessageFactory.Create<ClientMessage>());
            return true;
        }


        public override async ValueTask OnClose(IConnectionSession session)
        {
            logger.LogInformation("SERVER {0}[{1}][{2}], ClientIp: {3}", "DISCONNECTED", session.ConnectionId, session.CloseCause, session.RemoteEndPoint);
            await ValueTask.CompletedTask;
        }

        public override async ValueTask OnError(IConnectionSession session, Exception ex)
        {
            logger.LogError("SERVER Error: {0}, {1}", session.ConnectionId, ex.Message);
            await ValueTask.CompletedTask;
        }

        public override void OnReceive(IConnectionSession session, AbstractNetMessage message)
        {
            _ = channelWriter.WriteAsync(message);
        }
    }
}
