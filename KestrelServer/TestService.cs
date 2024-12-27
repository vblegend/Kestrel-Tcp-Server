using Microsoft.Extensions.Hosting;
using System.Buffers;
using System;
using System.Threading;
using System.Threading.Tasks;


namespace KestrelServer
{
    public class TestService : IHostedService
    {
        private readonly GMessageParser messageParser;
        public TestService(GMessageParser messageParser)
        {
            this.messageParser = messageParser;
        }


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            GMessage gMessage = GMessage.Create(12345678, new TestClass());
            using (var stream = StreamPool.GetStream())
            {
                await gMessage.WriteToAsync(stream);
                gMessage.Return();
                var span = stream.GetBuffer();
                for (int i = 0; i < stream.Length; i++)
                {
                    Console.Write(span[i].ToString("X2"));
                }
                Console.WriteLine();
                var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(stream.ToArray()));
                messageParser.Parse(reader, out var msg);
                msg.Return();

            }
            await Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("TestService.StopAsync");
            await Task.CompletedTask;
        }
    }
}
