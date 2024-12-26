using Microsoft.IO;
using System;
using System.Buffers;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text;

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

    public interface ISerializer
    {
        void Read(BinaryReader reader);
        void Write(BinaryWriter writer);


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
    public sealed class GMPayload 
    {
        private RecyclableMemoryStream stream;
        public void SetStream(RecyclableMemoryStream stream)
        {
            this.stream = stream;
        }


        public void SetData(ReadOnlySequence<Byte> sequence)
        {
            foreach (var item in sequence)
            {

            }
        }

        public ReadOnlySequence<byte> ReadOnlySequence()
        {
            return this.stream.GetReadOnlySequence();
        }


        public Int32 Length
        {
            get
            {
                return this.stream != null ? (Int32)this.stream.Length : 0;
            }
        }

        public void Release()
        {
            this.stream?.Dispose();
            this.stream = null;

        }

    }


    public sealed partial class GMessage
    {
        public static readonly UInt16 Header = 0x474D;
        public static Boolean UseTimestamp = false;
        private Boolean _isReturn = false;
        public UInt32 Action = 0;
        public UInt32 Timestamp = 0;
        public readonly GMParameters Parameters = new GMParameters();
        public readonly GMPayload Payload = new GMPayload();

        public static GMessage Create(UInt32 action, Int32[] _params, Byte[] payload)
        {
            var message = GMessage.Create();
            message.Action = action;
            message.Parameters.SetData(_params);
            //message.Payload.SetData(payload);
            return message;
        }



        public static GMessage Create<T>(UInt32 action, T @object) where T : ISerializer
        {
            var message = GMessage.Create();
            message.Action = action;
            var stream = StreamPool.GetStream();
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                @object.Write(writer);
            }
            stream.Position = 0;
            message.Payload.SetStream(stream);
            return message;
        }




        public static GMessage Create(UInt32 action, params Byte[] payload)
        {
            var message = GMessage.Create();
            message.Action = action;
            //message.Payload.SetData(payload);
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
