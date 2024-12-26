using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System;
using Microsoft.AspNetCore.Connections;
using System.Threading.Tasks;
using System.IO;
using System.Text;

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
