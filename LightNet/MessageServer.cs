using LightNet.Adapters;
using LightNet.Message;
using LightNet.Network;
using LightNet.Pipes;
using System;
using System.Buffers;
using System.Threading.Tasks;


namespace LightNet
{

    public abstract class MessageServer : ServerHandlerAdapter
    {

        private IPacketServer _packetServer;

        public readonly MessageParser messageParser = new MessageParser();
        public readonly MessageResolver messageResolver;

        protected MessageServer(MessageResolver resolver)
        {
            messageResolver = resolver;
        }



        /// <summary>
        /// 支持 tcp 和 pipe
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <exception cref="ArgumentNullException"></exception>
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
            var result = messageParser.TryParse(new SequenceReader<byte>(sequence), messageResolver, out AbstractNetMessage message, out var length);
            if (message != null)
            {
                message.Session = session;
                await OnReceive(message);
            }
            if (result == ParseResult.Illicit) throw new IllegalDataException("Illegal packet detected. Connection to be closed.");
            return new UnPacketResult(result == ParseResult.Ok, length);
        }

        public abstract ValueTask OnReceive(AbstractNetMessage message);

    }
}
