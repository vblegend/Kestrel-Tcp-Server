using PacketNet;
using PacketNet.Message;
using PacketNet.Pools;
using System.Buffers;
using System.Diagnostics;


namespace Examples.Services
{
    public class TestClientService : MessageClient, IHostedService
    {
        private readonly ILogger<TestClientService> logger;
        private readonly ApplicationOptions applicationOptions;
        private CancellationTokenSource sendToken;

        public TestClientService(ILogger<TestClientService> _logger, ApplicationOptions applicationOptions)
        {
            logger = _logger;
            this.applicationOptions = applicationOptions;
        }


        private CancellationTokenSource StartSendMessage()
        {
            CancellationTokenSource cancelToken = new CancellationTokenSource();
            ThreadPool.QueueUserWorkItem(async (e) =>
            {
                while (!cancelToken.IsCancellationRequested)
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    try
                    {
                        for (int i = 0; i < 1000; i++)
                        {
                            var message = MessageFactory.Create<ExampleMessage>();
                            message.X = 19201080;
                            Write(message);
                            message.Return();
                        }
                        await FlushAsync();
                    }
                    catch (Exception)
                    {

                    }
                    finally
                    {
                        stopwatch.Stop();
                    }
                    Thread.Sleep(1);
                }
            });

            return cancelToken;
        }



        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await ConnectAsync(applicationOptions.ClientUri, cancellationToken);
            sendToken = StartSendMessage();
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            sendToken?.Cancel();
            logger.LogInformation("TestService.StopAsync()");
            await Task.CompletedTask;
        }

        public override async ValueTask OnConnection(IPacketClient client)
        {
            logger.LogInformation("客户端成功连接至: {0}", applicationOptions.ClientUri);
            await WriteFlushAsync(MessageFactory.ExampleMessage(251));
        }

        public override async ValueTask OnClose(IPacketClient client)
        {
            logger.LogInformation("客户端关闭。");
            await ValueTask.CompletedTask;
        }

        public override async ValueTask OnError(Exception exception)
        {
            logger.LogInformation("客户端异常。 {0}", exception);
            await ValueTask.CompletedTask;
        }

        public override async ValueTask OnReceive(AbstractNetMessage message)
        {
            message.Return();
            logger.LogInformation("客户端收到消息。 {0}", message.Kind);
            await ValueTask.CompletedTask;
        }
    }
}
