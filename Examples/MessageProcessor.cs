using PacketNet.Message;
using PacketNet.Network;
using Serilog;
using System.Threading.Channels;

namespace Examples
{
    public class MessageProcessor : IMessageProcessor, IHostedService
    {
        public long count = 0;
        private readonly AsyncMessageRouter msgRouter;
        private readonly Serilog.ILogger logger = Log.ForContext<TCPServer>();
        private readonly Channel<AbstractNetMessage> messageChannel = Channel.CreateUnbounded<AbstractNetMessage>();
        public MessageProcessor()
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


        private async void ProcessMessage(Object? state)
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
                logger.Information("Received packet: {0}", count);
            }
            await ValueTask.CompletedTask;
        }

        public async ValueTask Process(GatewayMessage message)
        {
            await ValueTask.CompletedTask;
        }


    }
}
