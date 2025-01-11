using Examples.Gateway;
using LightNet;
using LightNet.Message;
using System.Threading.Channels;

namespace Examples.Services
{
    public class GatewayMessageProcessService : IMessageProcessor, IHostedService
    {

        private readonly ILogger<ClientMessageProcessService> logger = LoggerProvider.CreateLogger<ClientMessageProcessService>();
        private readonly Channel<AbstractNetMessage> messageChannel = Channel.CreateUnbounded<AbstractNetMessage>(new UnboundedChannelOptions() { SingleReader = true, SingleWriter = true });
        private readonly AsyncMessageRouter msgRouter;
        public ChannelWriter<AbstractNetMessage> GetWriter => messageChannel.Writer;
        public GatewayMessageProcessService()
        {
            msgRouter = new AsyncMessageRouter(this, messageChannel.Reader);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await msgRouter.StartAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await msgRouter.StopAsync(cancellationToken);
        }

        /// <summary>
        /// 处理网关ping消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async ValueTask Process(GatewayPingMessage message)
        {
            await ValueTask.CompletedTask;
        }


        /// <summary>
        /// 处理网关pong消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async ValueTask Process(GatewayPongMessage message)
        {
            await ValueTask.CompletedTask;
        }


    }
}
