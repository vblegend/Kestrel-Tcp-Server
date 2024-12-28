using System.Buffers.Binary;
using Microsoft.IO;
using System.Threading.Tasks;
using System.Text;

namespace System.Buffers
{
    public static class IBufferWriterExtensions
    {
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


        public static void Write(this IBufferWriter<byte> writer, string value, Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;
            int byteCount = encoding.GetByteCount(value);
            writer.Write(byteCount);
            Span<byte> buffer = writer.GetSpan(byteCount);
            encoding.GetBytes(value, buffer);
            writer.Advance(byteCount);
        }



    }
}
