using KestrelServer.Message;
using System;
using System.Buffers;


namespace KestrelServer
{
    [Message<ExampleMessage>(MessageKind.Example)]
    public class ExampleMessage : AbstractNetMessage
    {
        public Int64 X = 123;

        public override void Read(SequenceReader<byte> reader)
        {
            reader.TryRead<Int64>(out X);
        }


        public override void Write(IBufferWriter<byte> writer)
        {
            writer.Write(X);
        }


    }


    /// <summary>
    /// 
    /// </summary>
    [Message<GatewayMessage>(MessageKind.Gateway)]
    public class GatewayMessage : AbstractNetMessage
    {
        public Int32 ChannalId;
        public Int16 Action;
        public AbstractNetMessage Payload;

        public GatewayMessage()
        {
            
        }

        public GatewayMessage(AbstractNetMessage payload)
        {
            Payload = payload;
        }


        public override void Read(SequenceReader<byte> reader)
        {
            reader.TryRead<Int32>(out ChannalId);
            reader.TryRead<Int16>(out Action);
            Payload = MessageResolver.Default.Resolver(Action);
            Payload.Read(reader);
        }


        public override void Write(IBufferWriter<byte> writer)
        {
            writer.Write(ChannalId);
            writer.Write(Action);
            Payload.Write(writer);
        }

        public override void Reset()
        {

        }
    }

}
