using PacketNet.Message;
using System;
using System.Runtime.CompilerServices;

namespace PacketNet.Extensions
{
    public class NumberExtensions
    {


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Combine(uint length, byte flags)
        {
            return (length << 8) | flags;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Split(uint combineValue, out GMFlags flags, out uint length)
        {
            flags = (GMFlags)(combineValue & 0xFF);
            length = combineValue >> 8;
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

    }
}
