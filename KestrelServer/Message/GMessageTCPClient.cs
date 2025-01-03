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
        ValueTask OnMessage(GMessageTCPClient client, AbstractNetMessage message);
    }




    public class GMessageTCPClient : TCPClient
    {
        private readonly IGMessageHandler messageHandler;
        private readonly GMessageParser messageParser = new GMessageParser();
        public GMessageTCPClient(IGMessageHandler clientAdapter) : base(clientAdapter, 5)
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
