using System.Buffers;
using System;


namespace KestrelServer.Message
{
    public enum ParseResult
    {
        Illicit = 0,
        Partial = 1,
        Ok = 2,
    }

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
            reader.TryRead<ushort>(out var header);
            if (header != GMessage.Header) return ParseResult.Illicit;
            reader.TryRead<uint>(out var combineValue);
            GMessage.Split(combineValue, out GMFlags _flags, out var packetLen);
            if (reader.Length < packetLen) return ParseResult.Partial;
            message = GMessage.Create();

            reader.TryRead(out message.Action);
            if ((_flags & GMFlags.HasTimestamp) == GMFlags.HasTimestamp)
            {
                reader.TryRead(out message.Timestamp);
            }
            if ((_flags & GMFlags.HasParams) == GMFlags.HasParams)
            {
                reader.TryRead<byte>(out var paramsLen);
                message.Parameters.Alloc(paramsLen);
                for (int i = 0; i < paramsLen; i++)
                {
                    reader.TryRead(out message.Parameters.Data[i]);
                }
            }
            if ((_flags & GMFlags.HasData) == GMFlags.HasData)
            {
                reader.TryRead<byte>(out var dataLen);
                if (dataLen != reader.UnreadSequence.Length % 255)
                {
                    throw new Exception("损坏的数据包。");
                }
                var payload = resolver.Resolver(message.Action);
                payload.Read(new SequenceReader<byte>(reader.UnreadSequence));
                message.Payload = payload;
            }
            return ParseResult.Ok;
        }



    }
}
