using LightNet.Message;
using System.Buffers;


namespace Examples.Client
{
    public abstract class CSMessage: AbstractNetMessage { }




    [Message(ClientMessageKind.Example, 10000)]
    public class ClientMessage : CSMessage
    {
        public long X = 123;
        public override void Read(ref SequenceReader<byte> reader)
        {
            reader.TryRead(out X);
        }
        public override void Write(IBufferWriter<byte> writer)
        {
            writer.Write(X);
        }
    }

}
