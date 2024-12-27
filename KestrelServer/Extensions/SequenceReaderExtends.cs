using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Connections;
using System.Threading.Tasks;
using System.Text;
using KestrelServer;

namespace System.Buffers
{
    public static class SequenceReaderExtends
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



        public static async Task Send(this ConnectionContext context, GMessage message)
        {
            using (var stream = StreamPool.GetStream())
            {
                await message.WriteToAsync(stream/* , context.Items["timeService"] */);
                message.Return();
                var sequence = stream.GetReadOnlySequence();
                foreach (var item in sequence)
                {
                    context.Transport.Output.Write(item.Span);
                }
                await context.Transport.Output.FlushAsync();
            }
        }
    }
}
