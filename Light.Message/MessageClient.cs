using Light.Transmit;
using Light.Transmit.Adapters;
using Light.Transmit.Network;
using Light.Transmit.Pipes;
using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Light.Message
{
    public abstract class MessageClient : ClientHandlerAdapter
    {
        public IPacketClient packetClient;
        /// <summary>
        /// Message 最短可读的报文长度
        /// </summary>
        private const Int32 MINIMUM_PACKET_LENGTH = 5;

        public readonly MessageResolver messageResolver;

        protected MessageClient(MessageResolver resolver)
        {
            messageResolver = resolver;
        }



        /// <summary>
        /// 支持 tcp 和 pipe
        /// </summary>
        /// <param name="remoteUri"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public async ValueTask ConnectAsync(Uri remoteUri, CancellationToken cancellationToken)
        {
            if (remoteUri == null) throw new ArgumentNullException("uri");
            switch (remoteUri.Scheme.ToLower())
            {
                case "tcp":
                    {
                        packetClient = new TCPClient();
                        break;
                    }
                case "pipe":
                    {
                        packetClient = new PipeClient();
                        break;
                    }
                default:
                    throw new ArgumentNullException("uri");
            }
            packetClient.SetAdapter(this);
            await packetClient.ConnectAsync(remoteUri, cancellationToken);
        }

        public async Task CloseAsync()
        {
            await packetClient.CloseAsync();
            packetClient = null;
        }







        public override UnPacketResult OnPacket(IConnectionSession session, ReadOnlySequence<byte> buffer)
        {
            Int32 len = 0;
            var bufferReader = new SequenceReader<byte>(buffer);
            while (bufferReader.Remaining >= MINIMUM_PACKET_LENGTH)
            {
                var result = messageResolver.TryReadMessage(ref bufferReader, out AbstractNetMessage message, out var length);
                if (result == ParseResult.Ok)
                {
                    message.Session = session;
                    OnReceive(session, message);
                }
                else if (result == ParseResult.Partial)
                {
                    return new UnPacketResult(len, length);
                }
                else if (result == ParseResult.Illicit)
                {
                    throw new IllegalDataException("Illegal packet detected. Connection to be closed.");
                }
                len += length;
            }
            return new UnPacketResult(len, MINIMUM_PACKET_LENGTH);
        }

        /// <summary>
        /// 收到消息处理 
        /// 使用消息池时，需要自行执行message.Return()处理消息归还
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public abstract void OnReceive(IConnectionSession session, AbstractNetMessage message);
    }
}
