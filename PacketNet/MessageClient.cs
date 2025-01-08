using PacketNet.Message;
using PacketNet.Network;
using PacketNet.Pipes;
using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace PacketNet
{
    public abstract class MessageClient : ClientHandlerAdapter
    {

        private IPacketClient packetClient;
        private readonly GMessageParser messageParser = new GMessageParser();
        private IBufferWriter<byte> streamWriter = null;


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
            streamWriter = packetClient.GetWriter();
        }






        public override async ValueTask<UnPacketResult> OnPacket(ReadOnlySequence<byte> sequence)
        {
            var result = messageParser.TryParse(new SequenceReader<byte>(sequence), out AbstractNetMessage message, out var length);
            if (message != null)
            {
                await OnReceive(message);
            }
            if (result == ParseResult.Illicit) throw new Exception("Illegal packet detected. Connection to be closed.");
            return new UnPacketResult(result == ParseResult.Ok, length);
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
                await packetClient.FlushAsync();

            }
        }

        public async ValueTask FlushAsync()
        {
            await packetClient.FlushAsync();
        }


        public abstract ValueTask OnReceive(AbstractNetMessage message);
    }
}
