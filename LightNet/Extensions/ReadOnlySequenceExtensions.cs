using System;
using System.Buffers;
using System.Linq;
namespace LightNet.Extensions
{
    public static class ReadOnlySequenceExtensions
    {
        public static String ToHex(this ReadOnlySequence<Byte> readOnlyMemories, String separator = "")
        {
            return string.Join(separator, readOnlyMemories.ToArray().Select(e => e.ToString("X2"))); ;
        }


    }
}
