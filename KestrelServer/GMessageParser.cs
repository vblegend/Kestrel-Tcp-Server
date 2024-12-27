using System.Buffers;
using System;


namespace KestrelServer
{
    public class GMessageParser
    {
        private readonly GMPayloadResolver resolver;
        public GMessageParser(GMPayloadResolver resolver)
        {
            this.resolver = resolver;
        }


        public ParseResult Parse(SequenceReader<byte> reader, out GMessage message)
        {
            message = default;
            reader.TryRead<UInt16>(out var header);
            if (header != GMessage.Header) return ParseResult.Illicit;
            reader.TryRead<UInt32>(out var combineValue);
            Split(combineValue, out GMFlags _flags, out var packetLen);
            if (reader.Length < packetLen) return ParseResult.Partial;
            message = GMessage.Create();

            reader.TryRead<UInt32>(out message.Action);
            if ((_flags & GMFlags.HasTimestamp) == GMFlags.HasTimestamp)
            {
                reader.TryRead<UInt32>(out message.Timestamp);
            }
            if ((_flags & GMFlags.HasParams) == GMFlags.HasParams)
            {
                reader.TryRead<Byte>(out var paramsLen);
                message.Parameters.Alloc(paramsLen);
                for (int i = 0; i < paramsLen; i++)
                {
                    reader.TryRead<Int32>(out message.Parameters.Data[i]);
                }
            }
            if ((_flags & GMFlags.HasData) == GMFlags.HasData)
            {
                reader.TryRead<Byte>(out var dataLen);
                if(dataLen != (reader.UnreadSequence.Length % 255) )
                {
                    throw new Exception("损坏的数据包。");
                }
                var payload = resolver.Resolver(message.Action);
                payload.Read(new SequenceReader<byte>(reader.UnreadSequence));
                message.Payload = payload;
            }
            return ParseResult.Ok;
        }


        private static void Split(uint combineValue, out GMFlags flags, out uint remainingBytes)
        {
            flags = (GMFlags)((combineValue >> 24) & 0xFF);
            remainingBytes = (combineValue & 0x00FFFFFF) ^ 0xFFFFFF;
        }

    }
}
