﻿using Examples.Gateway;
using Light.Message;
using Light.Transmit;
using System.Threading.Channels;

namespace Examples.Services
{
    public class GatewayMessageProcessService : IMessageProcessor, IHostedService
    {
        public long count = 0;
        private readonly ILogger<ClientMessageProcessService> logger = LoggerProvider.CreateLogger<ClientMessageProcessService>();
        private readonly Channel<AbstractNetMessage> messageChannel = Channel.CreateUnbounded<AbstractNetMessage>(new UnboundedChannelOptions() { SingleReader = true, SingleWriter = true });
        private readonly AsyncMessageRouter msgRouter;
        public ChannelWriter<AbstractNetMessage> GetWriter => messageChannel.Writer;
        public GatewayMessageProcessService()
        {
            msgRouter = new AsyncMessageRouter(this, messageChannel.Reader);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await msgRouter.StartAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await msgRouter.StopAsync(cancellationToken);
        }

        /// <summary>
        /// 处理网关ping消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async ValueTask Process(GatewayPingMessage message)
        {
            count++;
            if (count % 1000000 == 0)
            {
                logger.LogInformation("Received packet: {0}", count);
            }
            await ValueTask.CompletedTask;
        }


        /// <summary>
        /// 处理网关pong消息
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async ValueTask Process(GatewayPongMessage message)
        {
            count++;
            if (count % 1000000 == 0)
            {
                logger.LogInformation("Received packet: {0}", count);
            }
            await ValueTask.CompletedTask;
        }


    }
}
