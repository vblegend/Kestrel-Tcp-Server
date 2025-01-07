using Microsoft.IO;
using System.Buffers.Binary;
using System.Text;

namespace System.Buffers
{
    public static class IBufferWriterExtensions
    {

        public static void Write(this IBufferWriter<byte> writer, UInt32 value, Byte length)
        {
            // 根据指定的 length 字节数来写入值
            if (length == 1)
            {
                // 1字节：只写入最低的8位
                var span = writer.GetSpan(1);  // 获取1个字节的空间
                span[0] = (byte)(value & 0xFF);  // 保留低8位
                writer.Advance(1);  // 更新 writer 状态，表示已写入1个字节
            }
            else if (length == 2)
            {
                // 2字节：写入低16位
                var span = writer.GetSpan(2);  // 获取2个字节的空间
                span[0] = (byte)(value & 0xFF);        // 保留低8位
                span[1] = (byte)((value >> 8) & 0xFF); // 保留高8位
                writer.Advance(2);  // 更新 writer 状态，表示已写入2个字节
            }
            else if (length == 3)
            {
                // 3字节：写入低24位
                var span = writer.GetSpan(3);  // 获取3个字节的空间
                span[0] = (byte)(value & 0xFF);          // 低8位
                span[1] = (byte)((value >> 8) & 0xFF);   // 中间8位
                span[2] = (byte)((value >> 16) & 0xFF);  // 高8位
                writer.Advance(3);  // 更新 writer 状态，表示已写入3个字节
            }
            else if (length == 4)
            {
                // 4字节：写入整个32位值
                var span = writer.GetSpan(4);  // 获取4个字节的空间
                span[0] = (byte)(value & 0xFF);          // 低8位
                span[1] = (byte)((value >> 8) & 0xFF);   // 次低8位
                span[2] = (byte)((value >> 16) & 0xFF);  // 次高8位
                span[3] = (byte)((value >> 24) & 0xFF);  // 高8位
                writer.Advance(4);  // 更新 writer 状态，表示已写入4个字节
            }
            else
            {
                throw new ArgumentException("Length must be 1, 2, 3, or 4.", nameof(length));
            }
        }


        public static void Write(this IBufferWriter<byte> writer, Int16 value, Byte length)
        {
            // 根据指定的 length 字节数来写入值
            if (length == 1)
            {
                // 1字节：只写入最低的8位
                var span = writer.GetSpan(1);  // 获取1个字节的空间
                span[0] = (byte)(value & 0xFF);  // 保留低8位
                writer.Advance(1);  // 更新 writer 状态，表示已写入1个字节
            }
            else if (length == 2)
            {
                // 2字节：写入低16位
                var span = writer.GetSpan(2);  // 获取2个字节的空间
                span[0] = (byte)(value & 0xFF);        // 保留低8位
                span[1] = (byte)((value >> 8) & 0xFF); // 保留高8位
                writer.Advance(2);  // 更新 writer 状态，表示已写入2个字节
            }
            else
            {
                throw new ArgumentException("Length must be 1, 2, 3, or 4.", nameof(length));
            }
        }


        public static void Write(this IBufferWriter<byte> writer, Int32 value, Byte length)
        {
            // 根据指定的 length 字节数来写入值
            if (length == 1)
            {
                // 1字节：只写入最低的8位
                var span = writer.GetSpan(1);  // 获取1个字节的空间
                span[0] = (byte)(value & 0xFF);  // 保留低8位
                writer.Advance(1);  // 更新 writer 状态，表示已写入1个字节
            }
            else if (length == 2)
            {
                // 2字节：写入低16位
                var span = writer.GetSpan(2);  // 获取2个字节的空间
                span[0] = (byte)(value & 0xFF);        // 保留低8位
                span[1] = (byte)((value >> 8) & 0xFF); // 保留高8位
                writer.Advance(2);  // 更新 writer 状态，表示已写入2个字节
            }
            else if (length == 3)
            {
                // 3字节：写入低24位
                var span = writer.GetSpan(3);  // 获取3个字节的空间
                span[0] = (byte)(value & 0xFF);          // 低8位
                span[1] = (byte)((value >> 8) & 0xFF);   // 中间8位
                span[2] = (byte)((value >> 16) & 0xFF);  // 高8位
                writer.Advance(3);  // 更新 writer 状态，表示已写入3个字节
            }
            else if (length == 4)
            {
                // 4字节：写入整个32位值
                var span = writer.GetSpan(4);  // 获取4个字节的空间
                span[0] = (byte)(value & 0xFF);          // 低8位
                span[1] = (byte)((value >> 8) & 0xFF);   // 次低8位
                span[2] = (byte)((value >> 16) & 0xFF);  // 次高8位
                span[3] = (byte)((value >> 24) & 0xFF);  // 高8位
                writer.Advance(4);  // 更新 writer 状态，表示已写入4个字节
            }
            else
            {
                throw new ArgumentException("Length must be 1, 2, 3, or 4.", nameof(length));
            }
        }

        public static void Write(this IBufferWriter<byte> writer, byte value)
        {
            writer.Write([value]);
        }
        public static void Write(this IBufferWriter<byte> writer, char value)
        {
            writer.Write([(byte)value]);
        }

        public static void Write(this IBufferWriter<byte> writer, bool value)
        {
            writer.Write([(byte)(value ? 1 : 0)]);
        }

        public static void Write(this IBufferWriter<byte> writer, short value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(short)];
            BinaryPrimitives.WriteInt16LittleEndian(buffer, value);
            writer.Write(buffer);
        }

        public static void Write(this IBufferWriter<byte> writer, ushort value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
            writer.Write(buffer);
        }

        public static void Write(this IBufferWriter<byte> writer, int value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
            writer.Write(buffer);
        }

        public static void Write(this IBufferWriter<byte> writer, uint value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
            writer.Write(buffer);
        }


        public static void Write(this IBufferWriter<byte> writer, long value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            BinaryPrimitives.WriteInt64LittleEndian(buffer, value);
            writer.Write(buffer);
        }

        public static void Write(this IBufferWriter<byte> writer, ulong value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ulong)];
            BinaryPrimitives.WriteUInt64LittleEndian(buffer, value);
            writer.Write(buffer);
        }

        public static void Write(this IBufferWriter<byte> writer, DateTime value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            BinaryPrimitives.WriteInt64LittleEndian(buffer, value.Ticks);
            writer.Write(buffer);
        }



        public static void Write(this IBufferWriter<byte> writer, float value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(float)];
            BinaryPrimitives.WriteSingleLittleEndian(buffer, value);
            writer.Write(buffer);
        }

        public static void Write(this IBufferWriter<byte> writer, double value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(double)];
            BinaryPrimitives.WriteDoubleLittleEndian(buffer, value);
            writer.Write(buffer);
        }


        public static void Write(this IBufferWriter<byte> writer, string value, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;
            int byteCount = encoding.GetByteCount(value);
            writer.Write(byteCount);
            Span<byte> buffer = writer.GetSpan(byteCount);
            encoding.GetBytes(value, buffer);
            writer.Advance(byteCount);
        }




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
