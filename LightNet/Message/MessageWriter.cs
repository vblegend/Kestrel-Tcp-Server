using LightNet.Pools;
using Microsoft.IO;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace LightNet.Message
{
    public ref struct MessageWriter : IDisposable
    {
        public static readonly UInt16 Header = 0x4D47;

        private static MessageFlags[] KindFlags = [default, MessageFlags.None, MessageFlags.Flag2];

        private IBufferWriter<byte> _writer;

        public MessageWriter(IBufferWriter<byte> writer)
        {
            this._writer = writer;
        }

        public void Write(AbstractNetMessage message)
        {
            var timeTicks = TimeService.Default.UtcTicks;
            using (RecyclableMemoryStream payloadStream = StreamPool.GetStream())
            {
                MessageFlags flags = MessageFlags.None;
                Int32 packetLength = 5 + 8;
                message.Write(payloadStream);
                packetLength += (Int32)payloadStream.Length;

                Int16 kindValue = message.Kind;
                Byte kl = GetEffectiveBytes(kindValue);
                flags |= KindFlags[kl];
                packetLength += kl;
                // 474D 00 0C00 02 FFE0F505 01 FF
                // ==================================================
                _writer.Write(Header);                                 // 2 
                _writer.Write((Byte)(flags));                          // 1
                _writer.Write((UInt16)packetLength);                   // 2
                _writer.Write(message.Kind, kl);                      // kl
                _writer.Write(timeTicks);                            // 8
                _writer.Write(payloadStream);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Byte GetEffectiveBytes(Int16 number)
        {
            if (number == 0) return 1; // 0 使用1字节
            int absValue = Math.Abs(number);
            if ((absValue & 0xFF00) == 0) return 1; // 1 字节
            return 2; // 4 字节
        }


        internal static uint Combine(uint length, byte flags)
        {
            return (length << 8) | flags;
        }

        public void Dispose()
        {
            this._writer = null;
        }
    }
}
