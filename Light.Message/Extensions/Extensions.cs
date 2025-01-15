using Light.Message;
using System.Runtime.CompilerServices;

namespace System
{
    internal static class Extensions
    {


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Split(uint combineValue, out MessageFlags flags, out uint length)
        {
            flags = (MessageFlags)(combineValue & 0xFF);
            length = combineValue >> 8;
        }


    }
}
