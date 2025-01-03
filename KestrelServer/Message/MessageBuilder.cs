using KestrelServer.Pools;
using Microsoft.IO;
using System;
using System.Buffers;
using System.Numerics;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace KestrelServer.Message
{
    public ref struct MessageBuilder
    {
        public static readonly UInt16 Header = 0x4D47;

        private static GMFlags[] KindFlags = [default, GMFlags.None, GMFlags.Flag2, GMFlags.Kind3, GMFlags.Kind4];

        public static void WriteTo(INetMessage message, IBufferWriter<byte> writer)
        {
            using (RecyclableMemoryStream payloadStream = StreamPool.GetStream())
            {
                GMFlags flags = GMFlags.None;
                Int32 packetLength = 5+5;
                message.Write(payloadStream);
                packetLength += (Int32)payloadStream.Length;

                Int32 kindValue = (Int32)message.Kind;
                Byte kl = GetEffectiveBytes(kindValue);
                flags |= KindFlags[kl];
                packetLength += kl;
                // 474D 00 0C00 02 FFE0F505 01 FF
                // ==================================================
                writer.Write(Header);                               // 2 
                writer.Write((Byte)(flags));                        // 1
                writer.Write((UInt16)packetLength);                 // 2
                writer.Write((Int32)message.Kind, kl);             // kl
                writer.Write(99999999);                          // 4
                writer.Write((Byte)(payloadStream.Length % 255));  // 1
                // 474D 00100000 02000000 FFE0F505 FF01
                //writer.Write(payloadStream.GetReadOnlySequence());
                WriteToBufferWriter(payloadStream, writer);
                // ==================================================
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Byte GetEffectiveBytes(int number)
        {
            if (number == 0) return 1; // 0 使用1字节
            int absValue = Math.Abs(number);
            if ((absValue & 0xFFFFFF00) == 0) return 1; // 1 字节
            if ((absValue & 0xFFFF0000) == 0) return 2; // 2 字节
            if ((absValue & 0xFF000000) == 0) return 3; // 3 字节
            return 4; // 4 字节
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
