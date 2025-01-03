using Microsoft.Extensions.Hosting;
using System.Buffers;
using System;
using System.Threading;
using System.Threading.Tasks;
using KestrelServer.Message;
using KestrelServer.Pools;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Diagnostics;
using KestrelServer.Network;
using System.Threading.Tasks.Sources;
using System.Reflection;
using System.Numerics;


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
        }

        public async ValueTask OnClose(TCPClient client2)
        {
            logger.LogInformation("客户端关闭。");
            await ValueTask.CompletedTask;
        }

        public async ValueTask OnConnection(TCPClient client2)
        {
            logger.LogInformation("客户端与服务器连接成功。");
            await client.WriteFlushAsync(new ExampleMessage(251));
        }

        public async ValueTask OnError(Exception exception)
        {
            logger.LogInformation("客户端异常。 {0}", exception);
            await ValueTask.CompletedTask;
        }

        public async ValueTask OnMessage(GMessageTCPClient client, INetMessage message)
        {
            //logger.LogInformation("客户端收到消息。 {0} {1}", message.Action, message.Payload);
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
                            client.Write(new ExampleMessage(2048));
                        }
                        await client.FlushAsync();
                    }
                    catch (Exception _)
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


        private void HeapTest(ObjectHeap<Object> heap)
        {
            for (int i = 0; i < 1000; i++)
            {
                var index = heap!.Put(i);
                if (i % 5 == 0)
                {
                    heap.Take(index);
                }
            }

        }





        CancellationTokenSource sendToken;



        static int GetEffectiveBytes(int number)
        {
            if (number == 0) return 1; // 0 使用1字节
            int absValue = Math.Abs(number);
            if ((absValue & 0xFFFFFF00) == 0) return 1; // 1 字节
            if ((absValue & 0xFFFF0000) == 0) return 2; // 2 字节
            if ((absValue & 0xFF000000) == 0) return 3; // 3 字节
            return 4; // 4 字节
        }



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


            var v1 = Pool<ExampleMessage>.Shared.Get();
            var v2 = Pool<GatewayMessage>.Shared.Get();

            Console.WriteLine(v1);

            //Console.WriteLine(heap);

            sendToken = StartSendMessage();

            //timer = new Timer(sendMessage, null, 0, 10);


            using (var stream = StreamPool.GetStream())
            {
                MessageBuilder.WriteTo(new ExampleMessage(Int64.MaxValue), stream);

                var span = stream.GetBuffer();
                var len = INetMessage.ReadFullLength(new SequenceReader<byte>(stream.GetReadOnlySequence()));


                var array = span.Take((Int32)stream.Length).Select((e) => e.ToString("X2"));
                var line = String.Join("", array);
                logger.LogInformation(line);



                //var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(stream.ToArray()));
                //messageParser.Parse(reader, out var msg);
                //msg.Return();

            }
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            sendToken.Cancel();
            logger.LogInformation("TestService.StopAsync()");
            await Task.CompletedTask;
        }
    }
}
