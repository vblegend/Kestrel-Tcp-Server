using LightNet;
using LightNet.Message;
using System.Buffers;
using System.Diagnostics;


namespace Examples.Services
{
    public class ClientService : MessageClient, IHostedService
    {
        private readonly ILogger<ClientService> logger;
        private readonly ApplicationOptions applicationOptions;
        private CancellationTokenSource? sendToken;
        private IConnectionSession session;
        public ClientService(ILogger<ClientService> _logger, ApplicationOptions applicationOptions)
        {
            logger = _logger;
            sendToken = null;
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
                            session.Write(message);
                            message.Return();
                        }
                        await session.FlushAsync();
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

        public override async ValueTask OnConnection(IConnectionSession session)
        {
            this.session = session;
            logger.LogInformation("客户端成功连接至: {0}", applicationOptions.ClientUri);
            await session.WriteFlushAsync(MessageFactory.ExampleMessage(251));
        }

        public override async ValueTask OnClose(IConnectionSession session)
        {
            session = null;
            logger.LogInformation("客户端关闭。");
            await ValueTask.CompletedTask;
        }

        public override async ValueTask OnError(IConnectionSession session, Exception exception)
        {
            logger.LogInformation("客户端异常。 {0}", exception);
            await ValueTask.CompletedTask;
        }

        public override async ValueTask OnReceive(IConnectionSession session, AbstractNetMessage message)
        {
            message.Return();
            logger.LogInformation("客户端收到消息。 {0}", message.Kind);
            await ValueTask.CompletedTask;
        }
    }
}
