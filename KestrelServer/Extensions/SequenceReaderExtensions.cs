using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using System.Text;


namespace System.Buffers
{
    public static class SequenceReaderExtensions
    {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static bool TryRead<T>(this ref SequenceReader<byte> reader, out T value) where T : unmanaged
        {
            ReadOnlySpan<byte> span = reader.UnreadSpan;
            if (span.Length < sizeof(T))
            {
                return TryReadMultisegment(ref reader, out value);
            }
            value = Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(span));
            reader.Advance(sizeof(T));
            return true;
        }

        private unsafe static bool TryReadMultisegment<T>(ref SequenceReader<byte> reader, out T value) where T : unmanaged
        {
            T buffer = default;
            Span<byte> tempSpan = new Span<byte>(&buffer, sizeof(T));
            if (!reader.TryCopyTo(tempSpan))
            {
                value = default;
                return false;
            }
            value = Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(tempSpan));
            reader.Advance(sizeof(T));
            return true;
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static bool TryReadString(this ref SequenceReader<byte> reader, out string value, Encoding? encoding = null)
        {
            encoding ??= Encoding.UTF8;
            reader.TryRead<int>(out var length);
            if (!reader.TryReadExact(length, out ReadOnlySequence<byte> span))
            {
                throw new InvalidOperationException("Insufficient data in buffer.");
            }
            value = encoding.GetString(span);
            return true;
        }




        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static bool TryReadDateTime(this ref SequenceReader<byte> reader, out DateTime value)
        {
            reader.TryRead<long>(out var ticks);
            value = new DateTime(ticks);
            return true;
        }



    }
}
