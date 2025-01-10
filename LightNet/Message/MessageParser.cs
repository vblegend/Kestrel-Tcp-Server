using LightNet.Adapters;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;


namespace LightNet.Message
{


    public class MessageParser
    {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Byte GetKindLen(MessageFlags flags)
        {
            if ((flags & MessageFlags.Kind2) == MessageFlags.Kind2) return 2;
            return 1;
        }
        // 474D 00 1300 02 00000000 08 FFFFFFFFFFFFFF7F


        public ParseResult TryParse(ref SequenceReader<byte> reader, MessageResolver messageResolver, out AbstractNetMessage message, out UInt16 needLength)
        {
            message = default;
            needLength = 0;
            reader.TryRead<ushort>(out var header);
            if (header != AbstractNetMessage.Header) return ParseResult.Illicit;
            reader.TryRead<MessageFlags>(out var flags);
            reader.TryRead<UInt16>(out needLength);


            if (reader.Remaining < needLength - 5) 
                return ParseResult.Partial;
            var kl = GetKindLen(flags);
            reader.TryRead(kl, out Int16 kind);
            reader.TryRead(out UInt64 time);
            message = messageResolver.Resolver(kind);
            message.Read(ref reader);
            return ParseResult.Ok;
        }


        public ParseResult Parse(SequenceReader<byte> reader, MessageResolver messageResolver, out AbstractNetMessage message)
        {
            message = default;
            reader.TryRead<ushort>(out var header);
            if (header != AbstractNetMessage.Header) return ParseResult.Illicit;
            reader.TryRead<MessageFlags>(out var flags);
            reader.TryRead<UInt16>(out var packetLen);
            if (reader.Length < packetLen) return ParseResult.Partial;
            var kl = GetKindLen(flags);
            reader.TryRead(kl, out Int16 kind);
            reader.TryRead(out UInt64 time);
            message = messageResolver.Resolver(kind);
            message.Read(ref reader);
            return ParseResult.Ok;
        }

        public UInt32 ReadFullLength(SequenceReader<byte> reader)
        {
            reader.TryRead<ushort>(out var header);
            if (header != AbstractNetMessage.Header) return UInt32.MaxValue;
            reader.TryRead<MessageFlags>(out var flags);
            reader.TryRead<UInt16>(out var packetLen);
            return packetLen;
        }

    }
}
