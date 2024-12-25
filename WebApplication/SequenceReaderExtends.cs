using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System;

namespace KestrelServer
{
    public static class SequenceReaderExtends
    {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static bool TryRead<T>(this ref SequenceReader<byte> reader, out T value) where T : unmanaged
        {
            ReadOnlySpan<byte> span = reader.UnreadSpan;
            if (span.Length < sizeof(T))
            {
                return TryReadMultisegment<T>(ref reader, out value);
            }
            value = Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(span));
            reader.Advance(sizeof(T));
            return true;
        }

        private unsafe static bool TryReadMultisegment<T>(ref SequenceReader<byte> reader, out T value) where T : unmanaged
        {
            T buffer = default(T);
            Span<byte> tempSpan = new Span<byte>(&buffer, sizeof(T));
            if (!reader.TryCopyTo(tempSpan))
            {
                value = default(T);
                return false;
            }
            value = Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(tempSpan));
            reader.Advance(sizeof(T));
            return true;
        }

    }
}
