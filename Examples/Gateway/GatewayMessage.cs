using LightNet.Message;
using System.Buffers;

namespace Examples.Gateway
{
    public abstract class GatewayMessage : AbstractNetMessage { }



    [Message(GatewayMessageKind.Ping)]
    public class GatewayPingMessage : GatewayMessage
    {
        public Int64 X = 123;
        public override void Read(ref SequenceReader<byte> reader)
        {
            reader.TryRead<Int64>(out X);
        }
        public override void Write(IBufferWriter<byte> writer)
        {
            writer.Write(X);
        }
    }



    [Message(GatewayMessageKind.Pong)]
    public class GatewayPongMessage : GatewayMessage
    {
        public Int64 X = 123;
        public override void Read(ref SequenceReader<byte> reader)
        {
            reader.TryRead<Int64>(out X);
        }
        public override void Write(IBufferWriter<byte> writer)
        {
            writer.Write(X);
        }
    }

}
