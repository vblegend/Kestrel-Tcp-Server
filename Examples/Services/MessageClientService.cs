﻿using Examples.Client;
using LightNet;
using LightNet.Message;
using LightNet.Pipes;
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
            : base(MessageResolvers.CSResolver)
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
                var message = MessageFactory.Create<ClientMessage>();
                message.X = 19201080;

                var s = this.packetClient as PipeClient;

                int count = 0;
                while (!cancelToken.IsCancellationRequested && session != null)
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    try
                    {
                        for (int i = 0; !cancelToken.IsCancellationRequested && i < 100000; i++)
                        {
                            session?.Write(message);
                            count++;
                        }
                        if (session != null && !cancelToken.IsCancellationRequested) await session.FlushAsync();
                        if (count % 1000000 == 0)
                        {
                            logger.LogInformation("Send Message {0}", count);
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
            this.session = null;
            logger.LogInformation("客户端关闭, 原因：{0}", session.CloseCause);
            await ValueTask.CompletedTask;
        }

        public override async ValueTask OnError(IConnectionSession session, Exception exception)
        {
            logger.LogInformation("客户端异常。 {0}", exception);
            await ValueTask.CompletedTask;
        }

        public override void OnReceive(IConnectionSession session, AbstractNetMessage message)
        {
            message.Return();
            logger.LogInformation("客户端收到消息。 {0}", message.Kind);
        }
    }
}