using Microsoft.IO;

namespace System.Buffers
{
    public static class IBufferWriterExtensions
    {
        public static void Write(this IBufferWriter<byte> writer, RecyclableMemoryStream stream)
        {
            if (stream.TryGetBuffer(out ArraySegment<byte> segment))
            {
                // 使用 ArraySegment 的方式直接写入
                writer.Write(segment.AsSpan());
            }
            else
            {
                // Fallback：如果 TryGetBuffer 不支持，手动操作
                writer.Write(stream.GetBuffer().AsSpan(0, (int)stream.Length));
            }
        }
    }
}
