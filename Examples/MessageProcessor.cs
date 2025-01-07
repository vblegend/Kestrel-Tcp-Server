using PacketNet.Message;
using PacketNet.Network;
using Serilog;
using System.Collections.Concurrent;

namespace Examples
{
    public class MessageProcessor : IMessageProcessor, IHostedService
    {
        public long count = 0;
        private readonly ConcurrentQueue<AbstractNetMessage> msgQueue = new ConcurrentQueue<AbstractNetMessage>();
        private readonly AsyncMessageRouter msgRouter;
        private readonly Serilog.ILogger logger = Log.ForContext<TCPServer>();

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

        public void Enqueue(AbstractNetMessage message)
        {
            msgQueue.Enqueue(message);
        }


        private async void ProcessMessage(Object? state)
        {
            var cancellationToken = (CancellationToken)state;
            while (!cancellationToken.IsCancellationRequested)
            {
                if (msgQueue.TryDequeue(out var msg))
                {
                    await msgRouter.RouteAsync(msg);
                    msg = null;
                }
                else
                {
                    Thread.Sleep(1);
                };
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
