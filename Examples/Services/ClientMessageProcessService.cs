using Examples.Client;
using Light.Message;
using Light.Transmit;
using System.Threading.Channels;

namespace Examples.Services
{
    public class ClientMessageProcessService : IMessageProcessor, IHostedService
    {
        public long count = 0;

        private readonly ILogger<ClientMessageProcessService> logger = LoggerProvider.CreateLogger<ClientMessageProcessService>();
        private readonly Channel<AbstractNetMessage> messageChannel = Channel.CreateUnbounded<AbstractNetMessage>(new UnboundedChannelOptions() { SingleReader = true, SingleWriter = true });
        private readonly AsyncMessageRouter msgRouter;
        public ChannelWriter<AbstractNetMessage> GetWriter => messageChannel.Writer;
        public ClientMessageProcessService()
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
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async ValueTask Process(ClientMessage message)
        {
            count++;
            if (count % 1000000 == 0)
            {
                logger.LogInformation("Received packet: {0}", count);
            }
            await ValueTask.CompletedTask;
        }




    }
}
