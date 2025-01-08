using PacketNet.Message;
using PacketNet.Network;
using PacketNet.Pipes;
using System;
using System.Buffers;
using System.Threading.Tasks;


namespace PacketNet
{

    public abstract class MessageServer : ServerHandlerAdapter
    {

        private IPacketServer _packetServer;

        private readonly GMessageParser messageParser = new GMessageParser(MessageResolver.Default);

        public void Listen(Uri uri)
        {
            if (uri == null) throw new ArgumentNullException("uri");
            switch (uri.Scheme.ToLower())
            {
                case "tcp":
                    {
                        _packetServer = new TCPServer();
                        break;
                    }
                case "pipe":
                    {
                        _packetServer = new PipeServer();
                        break;
                    }
                default:
                    throw new ArgumentNullException("uri");
            }
            _packetServer.MinimumPacketLength = 5;
            _packetServer.SetAdapter(this);
            _packetServer.Listen(uri);
        }

        public async Task StopAsync()
        {
            if (_packetServer != null) await _packetServer.StopAsync();
            _packetServer = null;
        }


        public override async ValueTask<UnPacketResult> OnPacket(IConnectionSession session, ReadOnlySequence<byte> sequence)
        {
            var result = messageParser.TryParse(new SequenceReader<byte>(sequence), out AbstractNetMessage message, out var length);
            if (message != null)
            {
                message.Session = session;
                await OnReceive(message);
            }
            if (result == ParseResult.Illicit) throw new Exception("Illegal packet detected. Connection to be closed.");
            return new UnPacketResult(result == ParseResult.Ok, length);
        }

        public abstract ValueTask OnReceive(AbstractNetMessage message);

    }
}
