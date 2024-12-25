using System;
using System.Buffers;

namespace KestrelServer
{
    [Flags]
    internal enum GMFlags : Byte
    {
        None = 0b00000000,            // 普通的包
        LittleEndian = 0b00000001,    // 小端序，否则大端序
        HasParams = 0b00000010,      // 包含Action参数
        HasData = 0b00000100,       // 包含数据区
        Compressed = 0b00001000,   // 数据区被压缩了
        HasTimestamp = 0b00010000,   // 包含时间戳
        LargePacket = 0b00100000,   // 大封包，超过1Kb
        Flag1 = 0b01000000,     // 64
        Flag2 = 0b10000000,    // 128
    }


    public enum GAction : UInt32
    {

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
            if (dataType == DataType.Pool && Length > 0)
            {
                ArrayPool<Type>.Shared.Return(Data, true);
            }
            this.Length = length;
            Data = ArrayPool<Type>.Shared.Rent(length);
            dataType = DataType.Pool;
        }

        /// <summary>
        /// 清理数据区域，如果数据区为池化数据则放回池子
        /// </summary>
        public void Clear()
        {
            if (dataType == DataType.Pool && Length > 0)
            {
                ArrayPool<Type>.Shared.Return(Data, true);
            }
            Length = 0;
            Data = Array.Empty<Type>();
        }

    }


    public sealed class GMParameters : PoolingData<Int32>
    {
    }
    public sealed class GMPayload : PoolingData<Byte>
    {
    }


    public sealed partial class GMessage
    {
        public static readonly UInt16 Header = 0x474D;
        public static Boolean UseTimestamp = false;
        private Boolean _isReturn = false;
        public UInt32 SerialNumber = 0;
        public UInt32 Action = 0;
        public UInt32 Timestamp = 0;
        public Int32[] Params = Array.Empty<Int32>();


        public readonly GMParameters Parameters = new GMParameters();
        public readonly GMPayload Payload = new GMPayload();






        public static GMessage Create(UInt32 action, Int32[] _params, Byte[] payload)
        {
            var message = GMessage.Create();
            message.Action = action;
            message.Parameters.SetData(_params);
            message.Payload.SetData(payload);
            return message;
        }



        public static GMessage Create(UInt32 action, params Byte[] payload)
        {
            var message = GMessage.Create();
            message.Action = action;
            message.Payload.SetData(payload);
            return message;
        }


        public static GMessage Create(UInt32 action, params Int32[] _params)
        {
            var message = GMessage.Create();
            message.Action = action;
            message.Parameters.SetData(_params);
            return message;
        }




        public static GMessage Create(UInt32 action)
        {
            var message = GMessage.Create();
            message.Action = action;
            return message;
        }



    }






}
