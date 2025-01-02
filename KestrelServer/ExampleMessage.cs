using KestrelServer.Message;
using System.Buffers;
using System.Text;
using System;
using System.Runtime.InteropServices;
using KestrelServer.Network;


namespace KestrelServer
{

    public enum MessageKind
    {
        None = 0,
        Gateway = 1,
        Example = 2,
    }




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

        void Process(IConnectionSession session, INetMessage payload);
    }





    [MessageProcessor(typeof(ExampleMessage))]
    public class ExampleProcessor : IGMessageProcessor
    {
        public void Process(IConnectionSession session, INetMessage payload)
        {
            var s = payload as ExampleMessage;
        }
    }


    public class ExampleMessage : INetMessage
    {



        public Byte X = 123;


        public ExampleMessage()
        {
            this.X = 0;
        }

        public ExampleMessage(Int64 X)
        {
            this.X = 255;
        }



        public void Read(SequenceReader<byte> reader)
        {
            reader.TryRead<Byte>(out X);
        }


        public void Write(IBufferWriter<byte> writer)
        {
            writer.Write(X);
        }

        public void Reset()
        {

        }

        public MessageKind Kind => MessageKind.Example;
    }


    /// <summary>
    /// 
    /// </summary>

    public class GatewayMessage : INetMessage
    {


        public Int32 ChannalId;
        public Int32 Action;
        public INetMessage Payload;


        public void Read(SequenceReader<byte> reader)
        {
            reader.TryRead<Int32>(out ChannalId);
            reader.TryRead<Int32>(out Action);
            // set Payload Ins
            Payload.Read(reader);
        }


        public void Write(IBufferWriter<byte> writer)
        {
            writer.Write(ChannalId);
            writer.Write(Action);
            Payload.Write(writer);
        }

        public void Reset()
        {

        }

        public MessageKind Kind => MessageKind.Gateway;
    }

}
