using KestrelServer.Network;
using KestrelServer.Pools;
using System;
using System.Buffers;
using System.IO;
using System.Threading.Tasks;

namespace KestrelServer.Message
{
    public interface IGMessageHandler : IClientHandler
    {
        Task OnMessage(GMessageTCPClient client, GMessage message);
    }




    public class GMessageTCPClient : TCPClient
    {
        private readonly IGMessageHandler messageHandler;
        private readonly GMessageParser messageParser = new GMessageParser(null);
        public GMessageTCPClient(IGMessageHandler clientAdapter) : base(clientAdapter)
        {
            this.messageHandler = clientAdapter;
        }


        protected override async Task<uint> OnPacket(ReadOnlySequence<byte> buffer)
        {
            var len = GMessage.ReadLength(new SequenceReader<byte>(buffer));
            if (len == uint.MaxValue || len > 64 * 1024)
            {
                await messageHandler.OnError(new Exception("检测到非法封包，即将关闭连接！"));
                Close();
            }
            return len;
        }

        protected override async Task OnReceive(ReadOnlySequence<Byte> data)
        {
            var result = messageParser.Parse(new SequenceReader<byte>(data), out GMessage message);
            if (result == ParseResult.Illicit)
            {
                Close();
                await messageHandler.OnError(new Exception("检测到非法封包，即将关闭连接！"));
                //session.Close(SessionShutdownCause.CLIENT_ILLEGAL_DATA);
                return;
            }
            if (result == ParseResult.Ok)
            {
                await messageHandler.OnMessage(this, message);
            }
        }



        public async Task WriteAsync(GMessage message)
        {
            using (var stream = StreamPool.GetStream())
            {
                await message.WriteToAsync(stream/* , context.Items["timeService"] */);
                message.Return();
                var sequence = stream.GetReadOnlySequence();
                foreach (var item in sequence)
                {
                    Write(item.Span);
                }
            }
        }


        public async Task WriteFlushAsync(GMessage message)
        {
            using (var stream = StreamPool.GetStream())
            {
                await message.WriteToAsync(stream/* , context.Items["timeService"] */);
                message.Return();
                var sequence = stream.GetReadOnlySequence();
                foreach (var item in sequence)
                {
                    Write(item.Span);
                }
                await FlushAsync();
            }
        }

    }
}
