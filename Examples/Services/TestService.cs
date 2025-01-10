using LightNet;
using LightNet.Message;
using LightNet.Pools;
using System.Buffers;

namespace Examples.Services
{
    public class TestService : IHostedService
    {
        private readonly MessageResolvers messageDescriptor;
        private readonly ILogger logger;
        public TestService(MessageResolvers messageDescriptor, ILogger<ClientService> _logger, TimeService timeService)
        {
            logger = _logger;
            this.messageDescriptor = messageDescriptor;
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
                var parser = new MessageParser();
                var len = parser.ReadFullLength(new SequenceReader<byte>(stream.GetReadOnlySequence()));
                var array = span.Take((int)stream.Length).Select((e) => e.ToString("X2"));
                var line = string.Join("", array);
                logger.LogInformation(line);
                var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(stream.ToArray()));
                parser.Parse(reader, messageDescriptor.CSResolver, out var msg);
            }
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
    }
}
