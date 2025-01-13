using LightNet.Adapters;
using LightNet.Message;
using LightNet.Network;
using LightNet.Pipes;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Threading.Tasks;



namespace LightNet
{

    public abstract class MessageServer : ServerHandlerAdapter
    {
        private const Int32 MINIMUM_PACKET_LENGTH = 5;
        private readonly ILogger<TCPServer> logger = LoggerProvider.CreateLogger<TCPServer>();
        private IPacketServer _packetServer;
        public readonly MessageResolver messageResolver;

        private MessageMiddleware[] middlewares = [];

        protected MessageServer(MessageResolver resolver)
        {
            messageResolver = resolver;
        }


        /// <summary>
        /// 注册中间件
        /// </summary>
        public void Use(MessageMiddleware middleware)
        {
            var newMiddlewares = new MessageMiddleware[middlewares.Length +1];
            Array.Copy(middlewares, newMiddlewares, middlewares.Length);
            newMiddlewares[middlewares.Length] = middleware;
            middlewares = newMiddlewares;
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
            _packetServer.SetAdapter(this);
            _packetServer.Listen(uri);
        }

        public async Task StopAsync()
        {
            if (_packetServer != null) await _packetServer.StopAsync();
            _packetServer = null;
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
                    for (int i = 0; i < middlewares.Length; i++)
                    {
                        Boolean r = middlewares[i].OnMessage(session, message);
                        if (!r) break;
                    }
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

        public abstract void OnReceive(IConnectionSession session, AbstractNetMessage message);

    }
}
