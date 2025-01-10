using LightNet.Adapters;
using LightNet.Message;
using LightNet.Network;
using LightNet.Pipes;
using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace LightNet
{
    public abstract class MessageClient : ClientHandlerAdapter
    {
        private IPacketClient packetClient;

        public readonly MessageParser messageParser = new MessageParser();

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
            packetClient.MinimumPacketLength = 5;
            packetClient.SetAdapter(this);
            await packetClient.ConnectAsync(remoteUri, cancellationToken);
        }

        public async Task CloseAsync()
        {
            await packetClient.CloseAsync();
            packetClient = null;
        }







        public override UnPacketResult OnPacket(IConnectionSession session,ref SequenceReader<byte> reader)
        {
            var result = messageParser.TryParse(ref reader, messageResolver, out AbstractNetMessage message, out var length);
            if (message != null)
            {
                message.Session = session;
                // 待优化
                Task.Run(() => OnReceive(session, message));
            }
            if (result == ParseResult.Illicit) throw new Exception("Illegal packet detected. Connection to be closed.");
            return new UnPacketResult(result == ParseResult.Ok, length);
        }

        /// <summary>
        /// 收到消息处理 
        /// 使用消息池时，需要自行执行message.Return()处理消息归还
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public abstract ValueTask OnReceive(IConnectionSession session, AbstractNetMessage message);
    }
}
