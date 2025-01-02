using KestrelServer.Message;
using System.Buffers;
using System.Text;
using System;
using System.Runtime.InteropServices;
using KestrelServer.Network;


namespace KestrelServer
{

    public enum GMKind
    {
        None = 0,
        Example = 1,
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public abstract class MessageKind : Attribute
    {
        public Int32 Value { get; private set; }
        public MessageKind(Int32 value)
        {
            Value = value;
        }
    }

    public class GMessageKind : MessageKind
    {
        public GMessageKind(GMKind value) 
            : base((Int32)value)
        {

        }
        public GMKind Kind => (GMKind)base.Value;
    }

    public interface IGMessageProcessor<TPayload> where TPayload :  IMessagePayload
    {
        void Process(IConnectionSession session,TPayload payload);
    }




    [GMessageKind(GMKind.Example)]
    public class ExampleProcessor : IGMessageProcessor<ExamplePlayload>
    {
        public void Process(IConnectionSession session, ExamplePlayload payload)
        {
            Console.WriteLine(payload.X);
        }
    }




    [GMessageKind(GMKind.Example)]
    public class ExampleProcessor2 : IGMessageProcessor<ExamplePlayload>
    {
        public void Process(IConnectionSession session, ExamplePlayload payload)
        {
            Console.WriteLine(payload.X);
        }
    }







    public struct ExamplePlayload : IMessagePayload
    {
        public Int64 X = 123;

        public ExamplePlayload(Int64 X)
        {
            this.X = X;
        }

        public void Read(SequenceReader<byte> reader)
        {
            reader.TryRead<Int64>(out X);
        }


        public void Write(IBufferWriter<byte> writer)
        {
            writer.Write(X);
        }
    }


    /// <summary>
    /// 
    /// </summary>
    public struct GatewayPlayload : IMessagePayload
    {
        public Int32 ChannalId;
        public Int32 Action;
        public IMessagePayload Payload;


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
    }

}
