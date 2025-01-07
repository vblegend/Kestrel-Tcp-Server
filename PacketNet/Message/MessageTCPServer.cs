using PacketNet.Network;
using System;
using System.Buffers;
using System.Threading.Tasks;


namespace PacketNet.Message
{

    public abstract class MessageTCPServer : TCPServer
    {
        private readonly GMessageParser messageParser = new GMessageParser(MessageResolver.Default);
        public MessageTCPServer() : base()
        {
            this.MinimumPacketLength = 5;
        }

        protected override async ValueTask<uint> OnPacket(IConnectionSession session, ReadOnlySequence<byte> sequence)
        {
            var len = messageParser.ReadFullLength(new SequenceReader<byte>(sequence));
            if (len == uint.MaxValue || len > 64 * 1024)
            {
                await OnError(session, new Exception("检测到非法封包，即将关闭连接！"));
                session.Close(SessionShutdownCause.CLIENT_ILLEGAL_DATA);
            }
            return len;
        }

        protected override async ValueTask OnReceive(IConnectionSession session, ReadOnlySequence<byte> buffer)
        {
            var result = messageParser.Parse(new SequenceReader<byte>(buffer), out AbstractNetMessage message);
            if (result == ParseResult.Illicit)
            {
                await OnError(session, new Exception("检测到非法封包，即将关闭连接！"));
                session.Close(SessionShutdownCause.CLIENT_ILLEGAL_DATA);
                return;
            }
            if (result == ParseResult.Ok)
            {
                message.Session = session;
                await OnReceive(message);
            }
        }


        public abstract ValueTask OnReceive(AbstractNetMessage message);

    }
}
