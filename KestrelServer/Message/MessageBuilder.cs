using KestrelServer.Pools;
using Microsoft.IO;
using System;
using System.Buffers;
using System.Reflection.PortableExecutable;

namespace KestrelServer.Message
{
    public ref struct MessageBuilder
    {
        public static readonly UInt16 Header = 0x4D47;

        public MessageBuilder()
        {




        }




        public static void WriteTo(INetMessage message, IBufferWriter<byte> writer)
        {
            using (RecyclableMemoryStream payloadStream = StreamPool.GetStream())
            {
                GMFlags flags = GMFlags.None;
                message.Write(payloadStream);
                var packetLength = (UInt32)payloadStream.Length + 11;
                Byte ll = 1;
                Int32 kindValue = (Int32)message.Kind;
                Byte kl = 1;
                if (kindValue > 0xFF)
                {
                    kl = 2;
                    flags |= GMFlags.Kind2;
                }
                else if (kindValue > 0xFFFF)
                {
                    kl = 3;
                    flags |= GMFlags.Kind3;
                }
                else if (kindValue > 0xFFFFFF)
                {
                    kl = 4;
                    flags |= GMFlags.Kind4;
                }

                if (packetLength > 250)
                {
                    ll = 2;
                    flags |= GMFlags.LargePacket;
                }
    
                // ==================================================
                writer.Write(Header);
                writer.Write((Byte)(flags));
                writer.Write(packetLength, ll);
                writer.Write((Int32)message.Kind, kl);
                writer.Write(99999999);
                writer.Write((Byte)(payloadStream.Length % 255));
                // 474D 00100000 02000000 FFE0F505 FF01
                //writer.Write(payloadStream.GetReadOnlySequence());
                WriteToBufferWriter(payloadStream, writer);
                // ==================================================
            }
        }

        public static void WriteToBufferWriter(RecyclableMemoryStream stream, IBufferWriter<byte> writer)
        {
            // 获取 RecyclableMemoryStream 的内容
            var buffer = stream.GetBuffer();
            var length = (int)stream.Length; // 获取流的实际内容长度

            // 确保目标缓冲区有足够的空间
            writer.Write(buffer.AsSpan(0, length));

            // Advance 来更新 writer 的状态
            //writer.Advance(length);
        }

        internal static uint Combine(uint length, byte flags)
        {
            return (length << 8) | flags;
        }

    }
}
