using PacketNet.Network;
using System;
using System.Buffers;
using System.Threading.Tasks;

namespace PacketNet.Message
{
    public interface IGMessageHandler : IClientHandler
    {
        ValueTask OnMessage(MessageTCPClient client, AbstractNetMessage message);
    }




    public class MessageTCPClient : TCPClient
    {
        private readonly IGMessageHandler messageHandler;
        private readonly GMessageParser messageParser = new GMessageParser();
        public MessageTCPClient(IGMessageHandler clientAdapter) : base(clientAdapter, 5)
        {
            this.messageHandler = clientAdapter;
        }


        protected override async ValueTask<uint> OnPacket(ReadOnlySequence<byte> buffer)
        {
            var len = messageParser.ReadFullLength(new SequenceReader<byte>(buffer));
            if (len == uint.MaxValue || len > 64 * 1024)
            {
                await messageHandler.OnError(new Exception("检测到非法封包，即将关闭连接！"));
                Close();
            }
            return len;
        }

        protected override async ValueTask OnReceive(ReadOnlySequence<Byte> data)
        {
            var result = messageParser.Parse(new SequenceReader<byte>(data), out AbstractNetMessage message);
            if (result == ParseResult.Illicit)
            {
                await messageHandler.OnError(new Exception("检测到非法封包，即将关闭连接！"));
                Close();
                return;
            }
            if (result == ParseResult.Ok)
            {
                await messageHandler.OnMessage(this, message);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public void Write(AbstractNetMessage message)
        {
            if (streamWriter != null)
            {
                using (var writer = new MessageWriter(streamWriter))
                {
                    writer.Write(message);
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task WriteFlushAsync(AbstractNetMessage message)
        {
            if (streamWriter != null)
            {
                using (var writer = new MessageWriter(streamWriter))
                {
                    writer.Write(message);
                }
                await streamWriter.FlushAsync();

            }
        }

    }
}
