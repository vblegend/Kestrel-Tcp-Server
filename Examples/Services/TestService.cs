﻿using Examples.Client;
using Light.Message;
using Light.Message.Pools;
using Light.Transmit;
using Light.Transmit.Extensions;
using System.Buffers;
using System.Diagnostics;

namespace Examples.Services
{
    public class TestService : IHostedService
    {
        private readonly ILogger logger;

        public TestService(ILogger<MessageClientService> _logger, TimeService timeService)
        {
            logger = _logger;
            SnowflakeId.UtcNowFunc = timeService.UtcNow;
        }



        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // ==========================================================================================
            // 消息序列化测试
            // ==========================================================================================
            using (var stream = StreamPool.GetStream())
            {
                using (var writer = new MessageWriter(stream))
                {
                    writer.Write(MessageFactory.ExampleMessage(long.MaxValue));
                }
                var span = stream.GetReadOnlySequence().Slice(0, stream.Length);
                logger.LogInformation("消息HEX: {0}", span.ToHex());
                var reader = new SequenceReader<byte>(span);
                MessageResolvers.CSResolver.TryReadMessage(ref reader, out var msg, out var len);
                logger.LogInformation("消息Kind：{0}, 读取长度：{1}", msg.Kind, len);
            }

            Stopwatch stopwatch = null;
            int testCount = 10000000;
            // ==========================================================================================
            // 消息原生创建测试
            // ==========================================================================================
            stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < testCount; i++)
            {
                var obj1 = new ClientMessage();
            }
            stopwatch.Stop();
            logger.LogInformation("创建{0}条消息[  New ] Use {1}ms", testCount, stopwatch.ElapsedMilliseconds);


            // ==========================================================================================
            // 消息工厂创建测试
            // ==========================================================================================
            stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < testCount; i++)
            {
                var obj2 = MFactory<ClientMessage>.GetMessage();
            }
            stopwatch.Stop();
            logger.LogInformation("创建{0}条消息[ Pool ] Use {1}ms", testCount, stopwatch.ElapsedMilliseconds);


            // ==========================================================================================
            //  消息分发测试
            // ==========================================================================================
            var processor = new ClientMessageProcessService();
            var msgRouter = new AsyncMessageRouter(processor, null);
            var obj = MFactory<ClientMessage>.GetMessage();
            stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < testCount; i++)
            {
                await msgRouter.RouteAsync(obj);
            }
            stopwatch.Stop();
            logger.LogInformation("分发{0}条消息[Router] Use {1}ms", testCount, stopwatch.ElapsedMilliseconds);



        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
    }
}
