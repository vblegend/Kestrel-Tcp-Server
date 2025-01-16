using Examples.Gateway;
using Light.Message;
using Light.Transmit;
using System.Buffers;
using System.Diagnostics;


namespace Examples.Services
{
    public class MessageClientService : MessageClient, IHostedService
    {
        private readonly ILogger<MessageClientService> logger;
        private readonly ApplicationOptions applicationOptions;
        private CancellationTokenSource? sendToken;
        private IConnectionSession? session;




        public MessageClientService(ILogger<MessageClientService> _logger, ApplicationOptions applicationOptions)
            : base(MessageResolvers.GatewayResolver)
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
                var message = MessageFactory.Create<GatewayPingMessage>();
                message.X = 19201080;

                int count = 0;
                while (!cancelToken.IsCancellationRequested && session != null)
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    try
                    {
                        for (int i = 0; !cancelToken.IsCancellationRequested && i < 10000; i++)
                        {
                            session?.Write(message);
                            count++;
                        }
                        if (session != null && !cancelToken.IsCancellationRequested) await session.FlushAsync();
                        if (count % 1000000 == 0)
                        {
                            //logger.LogInformation("Send Message {0}", count);
                        }
                    }
                    catch (Exception ex)
                    {
                        //Console.WriteLine(ex.StackTrace);
                    }
                    finally
                    {
                        stopwatch.Stop();
                    }
                    Thread.Sleep(1);
                }
                message.Return();
            });

            return cancelToken;
        }



        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await ConnectAsync(applicationOptions.ClientUri, cancellationToken);
            //var response = await this.Login("123456");
            //logger.LogInformation("CLIENT {0}, CODE = {1}", "身份验证成功", response.Code);

            sendToken = StartSendMessage();
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            sendToken?.Cancel();
            await Task.CompletedTask;
        }

        public override async ValueTask OnConnection(IConnectionSession session)
        {
            this.session = session;
            logger.LogInformation("CLIENT {0} {1}", "JOIN", applicationOptions.ClientUri);

            //await session.WriteFlushAsync(MessageFactory.ExampleMessage(251));
        }

        public override async ValueTask OnClose(IConnectionSession session)
        {
            this.session = null;
            if (authCompleted != null) authCompleted.SetException(new Exception("验证失败"));
            logger.LogInformation("CLIENT {0} {1}", "LEAVE", applicationOptions.ClientUri);
            await ValueTask.CompletedTask;
        }

        public override async ValueTask OnError(IConnectionSession session, Exception exception)
        {
            logger.LogInformation("客户端异常。 {0}", exception);
            await ValueTask.CompletedTask;
        }

        public override void OnReceive(IConnectionSession session, AbstractNetMessage message)
        {
            if (authCompleted != null && message.Kind == GatewayMessageKind.AuthResponse)
            {
                authCompleted.SetResult((GatewayAuthResponseMessage)message);
                return;
            }
            message.Return();

        }


        private TaskCompletionSource<GatewayAuthResponseMessage> authCompleted;


        public async Task<GatewayAuthResponseMessage> Login(String pwd)
        {
            authCompleted = new TaskCompletionSource<GatewayAuthResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            await this.session.WriteFlushAsync(MessageFactory.GatewayAuthRequestMessage(pwd));
            return await authCompleted.Task;
        }

    }
}
