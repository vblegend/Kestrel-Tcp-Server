using Examples.Client;
using Examples.Gateway;
using LightNet;
using LightNet.Message;
using System.Buffers;
using System.Threading.Channels;

namespace Examples.Services
{
    public class ServerService : MessageServer, IHostedService
    {
        private readonly ILogger<ServerService> logger;
        private readonly ApplicationOptions applicationOptions;
        private readonly ChannelWriter<AbstractNetMessage> channelWriter;
        public ServerService(ILogger<ServerService> _logger, ClientProcessService _messageProcessor, ApplicationOptions applicationOptions, MessageResolvers resolvers)
            : base(resolvers.CSResolver)
        {
            channelWriter = _messageProcessor.GetWriter;
            this.applicationOptions = applicationOptions;
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
            await session.WriteFlushAsync(MessageFactory.Create<ClientMessage>());
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

        public override void OnReceive(AbstractNetMessage message)
        {
            channelWriter.WriteAsync(message);//.AsTask().Wait();
            //channelWriter.TryWrite(message);
        }
    }
}
