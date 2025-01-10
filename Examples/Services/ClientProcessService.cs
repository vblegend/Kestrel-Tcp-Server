using Examples.Client;
using Examples.Gateway;
using LightNet;
using LightNet;
using LightNet.Message;
using System.Threading.Channels;

namespace Examples.Services
{
    public class ClientProcessService : IMessageProcessor, IHostedService
    {
        public long count = 0;

        private readonly ILogger<ClientProcessService> logger = LoggerProvider.CreateLogger<ClientProcessService>();
        private readonly Channel<AbstractNetMessage> messageChannel = Channel.CreateUnbounded<AbstractNetMessage>(new UnboundedChannelOptions() { SingleReader = true, SingleWriter = true });
        private readonly AsyncMessageRouter msgRouter;
        public ChannelWriter<AbstractNetMessage> GetWriter => messageChannel.Writer;
        public ClientProcessService()
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
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async ValueTask Process(ClientMessage message)
        {
            count++;
            //await session.WriteFlushAsync(MessageFactory.ExampleMessage(count));
            if (count % 100000 == 0)
            {
                logger.LogInformation("Received packet: {0}", count);
            }
            await ValueTask.CompletedTask;
        }




    }
}
