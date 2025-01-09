using LightNet;
using LightNet.Message;
using LightNet.Pools;
using System.Buffers;

namespace Examples.Services
{
    public class TestMessageService : IHostedService
    {
        private readonly MessageParser messageParser;
        private readonly ILogger logger;
        public TestMessageService(MessageParser messageParser, ILogger<TestClientService> _logger, TimeService timeService)
        {
            logger = _logger;
            this.messageParser = messageParser;
            SnowflakeId.UtcNowFunc = timeService.UtcNow;

        }



        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using (var stream = StreamPool.GetStream())
            {
                using (var writer = new MessageWriter(stream))
                {
                    writer.Write(MessageFactory.ExampleMessage(long.MaxValue));
                }
                var span = stream.GetBuffer();
                var len = messageParser.ReadFullLength(new SequenceReader<byte>(stream.GetReadOnlySequence()));
                var array = span.Take((int)stream.Length).Select((e) => e.ToString("X2"));
                var line = string.Join("", array);
                logger.LogInformation(line);
                var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(stream.ToArray()));
                messageParser.Parse(reader, out var msg);
            }
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
    }
}
