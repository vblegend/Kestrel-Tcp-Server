using Microsoft.IO;
using System;
using System.Buffers;
using System.Threading.Tasks;

namespace KestrelServer
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
        public async Task WriteToAsync(IBufferWriter<byte> writer)
        {
            //   BinaryWriter
            var flags = BitConverter.IsLittleEndian ? GMFlags.LittleEndian : GMFlags.None;
            if (Parameters.Length > 0) flags |= GMFlags.HasParams;
            if (GMessage.UseTimestamp) flags |= GMFlags.HasTimestamp;
            if (Payload != null) flags |= GMFlags.HasData;

            var packetLength = totalLength();

            RecyclableMemoryStream payloadStream = null;
            if (Payload != null)
            {
                payloadStream = StreamPool.GetStream();
                Payload.Write(payloadStream);
                packetLength = packetLength +(UInt32)payloadStream.Length + 1;
            }

            writer.Write(Header);
            writer.Write(Combine((Byte)flags, packetLength));
            writer.Write((UInt32)Action);
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
                await payloadStream.CopyToAsync((RecyclableMemoryStream)writer);
                payloadStream.Dispose();
            }
            await Task.CompletedTask;
        }


        private UInt32 totalLength()
        {
            UInt32 size = 0;
            size += sizeof(UInt16);  //HEADER
            size += sizeof(UInt32);  // FLAGES + TOTALLength
            size += sizeof(UInt32);  // Action
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






        public static uint Combine(byte firstByte, uint remainingBytes)
        {
            // 限制剩余字节只占低 3 个字节
            remainingBytes &= 0x00FFFFFF;
            // 将第一个字节移至高 8 位，并与剩余字节合并
            return ((uint)firstByte << 24) | (remainingBytes ^ 0xFFFFFF);
        }


    }
}
