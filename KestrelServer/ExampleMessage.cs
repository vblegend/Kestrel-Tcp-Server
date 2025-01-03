using KestrelServer.Message;
using System.Buffers;
using KestrelServer.Network;
using System;


namespace KestrelServer
{



    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public class MessageProcessor : Attribute
    {
        public MessageProcessor(Type payloadType)
        {
            this.PayloadType = payloadType;
        }
        public Type PayloadType { get; private set; }
    }

    public interface IGMessageProcessor
    {
        void Process(IConnectionSession session, AbstractNetMessage payload);
    }

    [MessageProcessor(typeof(ExampleMessage))]
    public class ExampleProcessor : IGMessageProcessor
    {
        public void Process(IConnectionSession session, AbstractNetMessage payload)
        {
            var s = payload as ExampleMessage;
        }
    }


    [UseMessagePool<ExampleMessage>(128)]
    public class ExampleMessage : AbstractNetMessage
    {

        public Int64 X = 123;
        public ExampleMessage() : base(MessageKind.Example)
        {
        }


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
    [UseMessagePool<GatewayMessage>(128)]
    public class GatewayMessage : AbstractNetMessage
    {


        public Int32 ChannalId;
        public Int32 Action;
        public AbstractNetMessage Payload;

        public GatewayMessage() : base(MessageKind.Gateway)
        {
        }

        public override void Read(SequenceReader<byte> reader)
        {
            reader.TryRead<Int32>(out ChannalId);
            reader.TryRead<Int32>(out Action);
            // set Payload Ins
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
