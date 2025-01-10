using Examples.Gateway;
using LightNet;
using LightNet.Message;
using System.Threading.Channels;

namespace Examples.Services
{
    public class GatewayProcessService : IMessageProcessor, IHostedService
    {

        private readonly ILogger<ClientProcessService> logger = LoggerProvider.CreateLogger<ClientProcessService>();
        private readonly Channel<AbstractNetMessage> messageChannel = Channel.CreateUnbounded<AbstractNetMessage>(new UnboundedChannelOptions() { SingleReader = true, SingleWriter = true });
        private readonly AsyncMessageRouter msgRouter;
        public ChannelWriter<AbstractNetMessage> GetWriter => messageChannel.Writer;
        public GatewayProcessService()
        {
            msgRouter = new AsyncMessageRouter(messageChannel.Reader, this, true);
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
