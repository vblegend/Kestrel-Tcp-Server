using System.Buffers.Binary;
using System;
using System.Buffers;

namespace KestrelServer
{
    public static class BufferExtends
    {


        // Writes a two-byte unsigned integer to this stream. The current position
        // of the stream is advanced by two.
        //
        public static void Write(this IBufferWriter<byte> writer, ushort value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(ushort)];
            BinaryPrimitives.WriteUInt16LittleEndian(buffer, value);
            writer.Write(buffer);
        }

        // Writes a four-byte signed integer to this stream. The current position
        // of the stream is advanced by four.
        //
        public static void Write(this IBufferWriter<byte> writer, int value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, value);
            writer.Write(buffer);
        }

        // Writes a four-byte unsigned integer to this stream. The current position
        // of the stream is advanced by four.
        //

        public static void Write(this IBufferWriter<byte> writer, uint value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(uint)];
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, value);
            writer.Write(buffer);
        }

        // Writes a float to this stream. The current position of the stream is
        // advanced by four.
        //
        public static void Write(this IBufferWriter<byte> writer, float value)
        {
            Span<byte> buffer = stackalloc byte[sizeof(float)];
            BinaryPrimitives.WriteSingleLittleEndian(buffer, value);
            writer.Write(buffer);
        }







    }
}
