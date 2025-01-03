using Microsoft.Extensions.ObjectPool;
using Microsoft.IO;
using System;
using System.Buffers;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text;

namespace KestrelServer.Message
{
    public interface INetMessage
    {
        public static readonly UInt16 Header = 0x4D47;
        public static UInt32 ReadFullLength(SequenceReader<byte> reader)
        {
            reader.TryRead<ushort>(out var header);
            if (header != Header) return UInt32.MaxValue;
            reader.TryRead<GMFlags>(out var flags);
            reader.TryRead<UInt16>(out var packetLen);
            return packetLen;
        }

        public void Read(SequenceReader<byte> reader);
        public void Write(IBufferWriter<byte> writer);
        public void Reset();
        public MessageKind Kind { get; }
    }






    //private class GMessagePooledObjectPolicy : PooledObjectPolicy<GMessage>
    //{
    //    public override GMessage Create()
    //    {
    //        return new GMessage();
    //    }

    //    public override bool Return(GMessage obj)
    //    {
    //        return true;
    //    }
    //}


    //private static ObjectPool<GMessage> Pool = CreateObjectPool();


    //private static ObjectPool<GMessage> CreateObjectPool()
    //{
    //    var provider = new DefaultObjectPoolProvider();
    //    return provider.Create(new GMessagePooledObjectPolicy());
    //}






}
