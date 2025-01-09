using LightNet;
using LightNet.Message;
using System.Threading.Channels;

namespace Examples.Services
{
    public class TestMessageProcessService : IMessageProcessor, IHostedService
    {
        public long count = 0;
        private readonly AsyncMessageRouter msgRouter;
        private readonly ILogger<TestMessageProcessService> logger = LoggerProvider.CreateLogger<TestMessageProcessService>();
        private readonly Channel<AbstractNetMessage> messageChannel = Channel.CreateUnbounded<AbstractNetMessage>(new UnboundedChannelOptions() { SingleReader = true, SingleWriter = true });
        public TestMessageProcessService()
        {
            msgRouter = new AsyncMessageRouter(this);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            ThreadPool.QueueUserWorkItem(ProcessMessage, cancellationToken);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public async ValueTask WriteMessageAsync(AbstractNetMessage message)
        {
            await messageChannel.Writer.WriteAsync(message);
        }


        private async void ProcessMessage(object? state)
        {
            var cancellationToken = (CancellationToken)state!;
            while (!cancellationToken.IsCancellationRequested)
            {
                var msg = await messageChannel.Reader.ReadAsync(cancellationToken);
                await msgRouter.RouteAsync(msg);
                msg = null;
            }
        }



        public async ValueTask Process(ExampleMessage message)
        {
            count++;
            //await session.WriteFlushAsync(MessageFactory.ExampleMessage(count));
            if (count % 100000 == 0)
            {
                logger.LogInformation("Received packet: {0}", count);
            }
            await ValueTask.CompletedTask;
        }

        public async ValueTask Process(GatewayMessage message)
        {
            await ValueTask.CompletedTask;
        }


    }
}
