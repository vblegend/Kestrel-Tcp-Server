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


namespace KestrelServer
{
    public class TestService : IHostedService
    {
        private readonly GMessageParser messageParser;
        private readonly ILogger<TestService> logger;
        private readonly TimeService timeService;

        public TestService(GMessageParser messageParser ,ILogger<TestService> _logger,TimeService timeService  )
        {
            this.messageParser = messageParser;
            this.logger = _logger;
            this.timeService = timeService;
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
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
