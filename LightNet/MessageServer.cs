using LightNet.Adapters;
using LightNet.Message;
using LightNet.Network;
using LightNet.Pipes;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using static System.Collections.Specialized.BitVector32;


namespace LightNet
{

    public abstract class MessageServer : ServerHandlerAdapter
    {
        private const UInt32 MinimumPacketLength = 5;
        private readonly ILogger<TCPServer> logger = LoggerProvider.CreateLogger<TCPServer>();
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
            _packetServer.MinimumPacketLength = MinimumPacketLength;
            _packetServer.SetAdapter(this);
            _packetServer.Listen(uri);
        }

        public async Task StopAsync()
        {
            if (_packetServer != null) await _packetServer.StopAsync();
            _packetServer = null;
        }

        Int64 count = 0;
        private Queue<AbstractNetMessage> cache = new Queue<AbstractNetMessage>();

        public override UnPacketResult OnPacket(IConnectionSession session, ReadOnlySequence<byte> buffer)
        {
            Int64 len = 0;
            var bufferReader = new SequenceReader<byte>(buffer);
            while (bufferReader.Remaining >= MinimumPacketLength)
            {
                var result = messageParser.TryParse(ref bufferReader, messageResolver, out AbstractNetMessage message, out var length);

                if (result == ParseResult.Ok)
                {
                    message.Session = session;
                    OnReceive(message);
                }
                else if (result == ParseResult.Partial)
                {
                    return new UnPacketResult(result == ParseResult.Ok, len, length);
                }
                else if (result == ParseResult.Illicit)
                {
                    throw new IllegalDataException("Illegal packet detected. Connection to be closed.");
                }
                len += length;
            }
            return new UnPacketResult(true, len, MinimumPacketLength);
        }





        public UnPacketResult OnPacket2(IConnectionSession session, ref SequenceReader<byte> reader)
        {
            var result = messageParser.TryParse(ref reader, messageResolver, out AbstractNetMessage message, out var length);
            if (message != null)
            {
                message.Session = session;
                count++;

                if (count % 1000000 == 0)
                {
                    logger.LogInformation("===> {0}", count);
                }

                // 待优化
                //Task.Run(() => OnReceive(message));
            }
            if (result == ParseResult.Illicit) throw new IllegalDataException("Illegal packet detected. Connection to be closed.");
            return new UnPacketResult(result == ParseResult.Ok, length);
        }

        public abstract void OnReceive(AbstractNetMessage message);

    }
}
