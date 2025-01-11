using Examples.Client;
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
        private AsyncMessageRouter msgRouter;
        public ChannelWriter<AbstractNetMessage> GetWriter => messageChannel.Writer;
        public ClientProcessService()
        {

        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            msgRouter = new AsyncMessageRouter(messageChannel.Reader, this, true);
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
            if (count % 1000000 == 0)
            {
                logger.LogInformation("Received packet: {0}", count);
            }
            await ValueTask.CompletedTask;
        }




    }
}
