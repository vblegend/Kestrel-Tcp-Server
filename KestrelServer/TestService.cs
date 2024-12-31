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


namespace KestrelServer
{
    public class TestService : IHostedService, IGMessageHandler
    {
        private readonly GMessageParser messageParser;
        private readonly ILogger<TestService> logger;
        private readonly TimeService timeService;
        private readonly GMessageTCPClient client;


        public TestService(GMessageParser messageParser, ILogger<TestService> _logger, TimeService timeService, TCPConnectionHandler tCP)
        {
            this.messageParser = messageParser;
            this.logger = _logger;
            this.timeService = timeService;
            client = new GMessageTCPClient(this);
        }

        public async Task OnClose(TCPClient client2)
        {
            logger.LogInformation("客户端关闭。");
            await Task.CompletedTask;
        }

        public async Task OnConnection(TCPClient client2)
        {
            logger.LogInformation("客户端与服务器连接成功。");
            await client.WriteFlushAsync(GMessage.Create(1024, [1, 2, 3, 4]));
        }

        public async Task OnError(Exception exception)
        {
            logger.LogInformation("客户端异常。 {0}", exception);
            await Task.CompletedTask;
        }

        public async Task OnMessage(GMessageTCPClient client, GMessage message)
        {
            logger.LogInformation("客户端收到消息。 {0} {1}", message.Action, message.Payload);
            await Task.CompletedTask;
        }


        private async void sendMessage(Object? state)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                for (int i = 0; i < 1000; i++)
                {
                    await client.WriteAsync(GMessage.Create(1024, [1, 2, 3, 4]));
                }
                await client.FlushAsync();

            }
            catch (Exception e)
            {

            }
            finally
            {
                stopwatch.Stop();
                logger.LogInformation($"Send 1000 Used {stopwatch.ElapsedMilliseconds}ms");
            }
        }

        Timer timer;
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await client.ConnectAsync("127.0.0.1", 50000, cancellationToken);


            //timer = new Timer(sendMessage, null, 0, 100);
            sendMessage(0);
            GMessage gMessage = GMessage.Create(12345678, new TestClass());
            using (var stream = StreamPool.GetStream())
            {
                await gMessage.WriteToAsync(stream);
                gMessage.Return();
                var span = stream.GetBuffer();

                var array = span.Take((Int32)stream.Length).Select((e) => e.ToString("X2"));
                var line = String.Join("", array);
                logger.LogInformation(line);
                var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(stream.ToArray()));
                messageParser.Parse(reader, out var msg);
                msg.Return();

            }
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("TestService.StopAsync()");
            await Task.CompletedTask;
        }
    }
}
