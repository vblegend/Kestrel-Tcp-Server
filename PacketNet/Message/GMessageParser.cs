using System;
using System.Buffers;
using System.Runtime.CompilerServices;


namespace PacketNet.Message
{


    public class GMessageParser
    {
        private readonly MessageResolver resolver;
        public GMessageParser(MessageResolver resolver = null)
        {
            this.resolver = resolver ?? MessageResolver.Default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Byte GetKindLen(GMFlags flags)
        {
            if ((flags & GMFlags.Kind2) == GMFlags.Kind2) return 2;
            return 1;
        }
        // 474D 00 1300 02 00000000 08 FFFFFFFFFFFFFF7F


        public ParseResult TryParse(SequenceReader<byte> reader, out AbstractNetMessage message, out UInt16 needLength)
        {
            message = default;
            needLength = 0;
            reader.TryRead<ushort>(out var header);
            if (header != AbstractNetMessage.Header) return ParseResult.Illicit;
            reader.TryRead<GMFlags>(out var flags);
            reader.TryRead<UInt16>(out needLength);
            if (reader.Length < needLength) return ParseResult.Partial;
            var kl = GetKindLen(flags);
            reader.TryRead(kl, out Int16 kind);
            reader.TryRead(out UInt64 time);
            message = resolver.Resolver(kind);
            message.Read(new SequenceReader<byte>(reader.UnreadSequence));
            return ParseResult.Ok;
        }


        public ParseResult Parse(SequenceReader<byte> reader, out AbstractNetMessage message)
        {
            message = default;
            reader.TryRead<ushort>(out var header);
            if (header != AbstractNetMessage.Header) return ParseResult.Illicit;
            reader.TryRead<GMFlags>(out var flags);
            reader.TryRead<UInt16>(out var packetLen);
            if (reader.Length < packetLen) return ParseResult.Partial;
            var kl = GetKindLen(flags);
            reader.TryRead(kl, out Int16 kind);
            reader.TryRead(out UInt64 time);
            message = resolver.Resolver(kind);
            message.Read(new SequenceReader<byte>(reader.UnreadSequence));
            return ParseResult.Ok;
        }

        public UInt32 ReadFullLength(SequenceReader<byte> reader)
        {
            reader.TryRead<ushort>(out var header);
            if (header != AbstractNetMessage.Header) return UInt32.MaxValue;
            reader.TryRead<GMFlags>(out var flags);
            reader.TryRead<UInt16>(out var packetLen);
            return packetLen;
        }

    }
}
