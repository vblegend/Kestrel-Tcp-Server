using KestrelServer.Message;
using KestrelServer.Network;
using KestrelServer.Pools;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace KestrelServer
{
    public class TestService : IHostedService, IGMessageHandler
    {
        private readonly GMessageParser messageParser;
        private readonly ILogger<TestService> logger;
        private readonly TimeService timeService;
        private readonly GMessageTCPClient client;


        public TestService(GMessageParser messageParser, ILogger<TestService> _logger, TimeService timeService, GMessageTCPServer tCP)
        {
            this.messageParser = messageParser;
            this.logger = _logger;
            this.timeService = timeService;
            client = new GMessageTCPClient(this);
            SnowflakeId.UtcNowFunc = timeService.UtcNow;
        }

        async ValueTask IGMessageHandler.OnMessage(GMessageTCPClient client, AbstractNetMessage message)
        {
            message.Return();
            //logger.LogInformation("客户端收到消息。 {0} {1}", message.Action, message.Payload);
            await ValueTask.CompletedTask;
        }

        async ValueTask IClientHandler.OnConnection(TCPClient client2)
        {
            logger.LogInformation("客户端与服务器连接成功。");
            await client.WriteFlushAsync(MessageFactory.ExampleMessage(251));
        }

        async ValueTask IClientHandler.OnClose(TCPClient client)
        {
            logger.LogInformation("客户端关闭。");
            await ValueTask.CompletedTask;
        }

        async ValueTask IClientHandler.OnError(Exception exception)
        {
            logger.LogInformation("客户端异常。 {0}", exception);
            await ValueTask.CompletedTask;
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
                            client.Write(message);
                            message.Return();
                        }
                        await client.FlushAsync();
                    }
                    catch (Exception)
                    {

                    }
                    finally
                    {
                        stopwatch.Stop();
                    }
                    Thread.Sleep(10);
                }
            });

            return cancelToken;
        }

        CancellationTokenSource sendToken;


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await client.ConnectAsync("127.0.0.1", 50000, cancellationToken);

            //var s = new TaskCompletionSource<Int64>();
            //s.SetResult(123);
            //new ValueTask()
            //var heap = new ObjectHeap<Object>();
            //var sw = Stopwatch.StartNew();
            //HeapTest(heap);
            //sw.Stop();
            //Console.WriteLine(heap);

            sendToken = StartSendMessage();

            //timer = new Timer(sendMessage, null, 0, 10);


            using (var stream = StreamPool.GetStream())
            {
                using (var writer = new MessageWriter(stream))
                {
                    writer.Write(MessageFactory.ExampleMessage(Int64.MaxValue));
                }
                var span = stream.GetBuffer();
                var len = messageParser.ReadFullLength(new SequenceReader<byte>(stream.GetReadOnlySequence()));
                var array = span.Take((Int32)stream.Length).Select((e) => e.ToString("X2"));
                var line = String.Join("", array);
                logger.LogInformation(line);



                var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(stream.ToArray()));
                messageParser.Parse(reader, out var msg);


            }
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            sendToken?.Cancel();
            logger.LogInformation("TestService.StopAsync()");
            await Task.CompletedTask;
        }
    }
}
