using Light.Message;
using System.Buffers;
using System.Text;

namespace Examples.Gateway
{
    public abstract class GatewayMessage : AbstractNetMessage { }


    /// <summary>
    /// 网关登录消息
    /// </summary>
    [Message(GatewayMessageKind.AuthRequest, PoolingOptions.Default)]
    public class GatewayAuthRequestMessage : GatewayMessage
    {
        public String Pwd = "";

        public override void Read(ref SequenceReader<byte> reader)
        {
            reader.TryRead<Boolean>(out var hasStr);
            if (hasStr) reader.TryReadString(out Pwd, Encoding.UTF8);
        }

        public override void Write(IBufferWriter<byte> writer)
        {
            writer.Write(Pwd != null);
            writer.Write(Pwd, Encoding.UTF8);
        }


        public override void Reset()
        {
            Pwd = "";
        }
    }

    /// <summary>
    /// 网关登录消息
    /// </summary>
    [Message(GatewayMessageKind.AuthResponse, PoolingOptions.Default)]
    public class GatewayAuthResponseMessage : GatewayMessage
    {
        public Int32 Code = 0;

        public override void Read(ref SequenceReader<byte> reader)
        {
            reader.TryRead<Int32>(out Code);
        }

        public override void Write(IBufferWriter<byte> writer)
        {
            writer.Write(Code);
        }

        public override void Reset()
        {
            Code = 0;
        }
    }




    [Message(GatewayMessageKind.Ping, PoolingOptions.Pooling)]
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
