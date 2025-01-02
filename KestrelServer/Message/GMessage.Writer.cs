using KestrelServer.Pools;
using Microsoft.IO;
using System;
using System.Buffers;
using System.Threading.Tasks;

namespace KestrelServer.Message
{
    public partial class GMessage
    {







        /// <summary>
        /// 0x02 - HEADER
        /// ====================================================
        /// 0x01 - Flags (HasParams | HasData | Compressed | HasTimestamp | LargePacket)
        /// 0x03 - LENGTH(完整包长度)
        /// ====================================================
        /// ↓↓↓↓↓可压缩部分↓↓↓↓↓
        /// ====================================================
        /// 0x04 - SerialNumber
        /// 0x04 - Action
        /// 0x04 - Timestamp 可选
        /// ====================================================
        /// 0x01 - ParamCount 可选
        /// 0x03 - Data Length 可选
        /// ====================================================
        /// 0x?? - Params[ParamCount] 可选 (ParamCount * 4)
        /// ====================================================
        /// 0x?? - Data 可选
        /// ====================================================
        /// </summary>
        public void WriteTo(IBufferWriter<byte> writer)
        {
            //   BinaryWriter
            var flags = GMFlags.None;
            var packetLength = totalLength();

            RecyclableMemoryStream payloadStream = null;
            if (Payload != null)
            {
                payloadStream = StreamPool.GetStream();
                Payload.Write(payloadStream);
                packetLength = packetLength + (UInt32)payloadStream.Length + 1;
            }
            writer.Write(Header);
            writer.Write(Combine(packetLength, (Byte)flags));
            writer.Write(Action);
            if (GMessage.UseTimestamp) writer.Write(99999999);
            if (Parameters.Length > 0)
            {
                writer.Write((Byte)Parameters.Length);
                for (int i = 0; i < Parameters.Length; i++)
                {
                    writer.Write(Parameters.Data[i]);
                }
            }
            if (payloadStream != null)
            {
                writer.Write((Byte)(payloadStream.Length % 255));
                payloadStream.Position = 0;
                payloadStream.CopyTo((RecyclableMemoryStream)writer);
                payloadStream.Dispose();
            }
        }


        private UInt32 totalLength()
        {
            UInt32 size = 0;
            size += sizeof(UInt16);  //HEADER
            size += sizeof(UInt32);  // FLAGES + TOTALLength
            size += sizeof(Int32);  // Action
            if (GMessage.UseTimestamp)
            {
                size += sizeof(UInt32);  // Timestamp
            }
            if (Parameters.Length > 0)
            {
                size += sizeof(Byte); // Params Length
                size += (UInt32)(sizeof(Int32) * Parameters.Length); // Params Data
            }
            return size;
        }






        internal static uint Combine(uint length, byte flags)
        {
            return (length << 8) | flags;
        }


    }
}
