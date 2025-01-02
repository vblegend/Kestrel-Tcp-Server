using Microsoft.IO;
using System;
using System.Buffers;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text;

namespace KestrelServer.Message
{
    [Flags]
    public enum GMFlags : Byte
    {
        None = 0b00000000,            // 普通的包
        SSSSS = 0b00000001,    // 小端序，否则大端序
        Kind2 = 0b00000010,      // 包含Action参数
        Kind3 = 0b00000100,       // 包含数据区
        Kind4 = 0b00001000,   // 数据区被压缩了
        LargePacket = 0b00010000,   // 包含时间戳
        HasTimestamp = 0b00100000,   // 大封包，超过256字节
        Flag1 = 0b01000000,     // 64
        Flag2 = 0b10000000,    // 128
    }





    public interface INetMessage
    {

        public void Read(SequenceReader<byte> reader);
        public void Write(IBufferWriter<byte> writer);
        public void Reset();
        public MessageKind Kind { get; }
    }



    public abstract class PoolingData<Type>
    {
        internal enum DataType
        {
            Other, Pool
        }

        private DataType dataType = DataType.Other;
        public Int32 Length = 0;
        public Type[] Data = Array.Empty<Type>();

        /// <summary>
        /// 设置为外部数据，不适用池化
        /// </summary>
        /// <param name="data"></param>
        public void SetData(Type[] data)
        {
            Length = data.Length;
            Data = data;
            dataType = DataType.Other;
        }


        /// <summary>
        /// 分配指定长度的池化数据区域
        /// </summary>
        /// <param name="length"></param>
        public void Alloc(Int32 length)
        {
            this.Release();
            this.Length = length;
            Data = ArrayPool<Type>.Shared.Rent(length);
            dataType = DataType.Pool;
        }

        /// <summary>
        /// 清理数据区域，如果数据区为池化数据则放回池子
        /// </summary>
        public void Release()
        {
            if (dataType == DataType.Pool && Length > 0)
            {
                ArrayPool<Type>.Shared.Return(Data, true);
            }
            Length = 0;
            Data = [];
        }

    }


    public sealed class GMParameters : PoolingData<Int32>
    {
    }




    public sealed partial class GMessage
    {
        public static readonly UInt32 MinimumSize = CalcMinimumSize();
        public static readonly UInt16 Header = 0x4D47;
        public static Boolean UseTimestamp = true;
        private Boolean _isReturn = false;
        public Int32 Action = 0;
        public UInt32 Timestamp = 0;
        public readonly GMParameters Parameters = new GMParameters();
        public INetMessage? Payload = null;

        public static GMessage Create(MessageKind action, Int32[] _params, Byte[] payload)
        {
            var message = GMessage.Create();
            message.Action = (Int32)action;
            message.Parameters.SetData(_params);
            //message.Payload.SetData(payload);
            return message;
        }


        public static GMessage Create<T>(T @object) where T : INetMessage
        {
            var message = GMessage.Create();
            message.Payload = @object;
            return message;
        }

        public static GMessage Create<T>(MessageKind action, T @object) where T : INetMessage
        {
            var message = GMessage.Create();
            message.Action = (Int32)action;
            message.Payload = @object;
            return message;
        }


        public static GMessage Create(MessageKind action, params Int32[] _params)
        {
            var message = GMessage.Create();
            message.Action = (Int32)action;
            message.Parameters.SetData(_params);
            return message;
        }

        public static GMessage Create(MessageKind action)
        {
            var message = GMessage.Create();
            message.Action = (Int32)action;
            return message;
        }



        private static UInt32 CalcMinimumSize()
        {
            UInt32 size = 0;
            size += sizeof(UInt16);  //HEADER
            size += sizeof(UInt32);  // FLAGES + TOTALLength
            size += sizeof(UInt32);  // Action
            return size;
        }




        public static UInt32 ReadLength(SequenceReader<byte> reader)
        {
            if (reader.Length == 1)
            {
                reader.TryRead<Byte>(out var partHeader);
                return partHeader == 71 ? 6 : UInt32.MaxValue;
            }
            reader.TryRead<UInt16>(out var header);
            if (header != Header) return UInt32.MaxValue;
            if (reader.Length < 6) return 6; // 不够读取
            reader.TryRead<UInt32>(out var combineValue);
            GMessage.Split(combineValue, out GMFlags _, out var packetLen);
            return packetLen;
        }


        internal static void Split(uint combineValue, out GMFlags flags, out uint length)
        {
            flags = (GMFlags)(combineValue & 0xFF);
            length = combineValue >> 8;
        }


    }






}
