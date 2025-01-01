using System.Buffers;
using System;


namespace KestrelServer.Message
{
    /// <summary>
    /// 封包解析返回值
    /// </summary>
    public enum ParseResult
    {
        /// <summary>
        /// 非法封包数据
        /// </summary>
        Illicit = 0,
        /// <summary>
        /// 部分封包，粘包、不完整的
        /// </summary>
        Partial = 1,
        /// <summary>
        /// 一个完整的封包
        /// </summary>
        Ok = 2,
    }


    public class GMessageParser
    {
        private readonly GMPayloadResolver resolver;
        public GMessageParser(GMPayloadResolver resolver)
        {
            this.resolver = resolver;
            if (resolver == null) this.resolver = new GMPayloadResolver();
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
                    return ParseResult.Illicit;
                }
                var payload = resolver.Resolver(message.Action);
                payload.Read(new SequenceReader<byte>(reader.UnreadSequence));
                message.Payload = payload;
            }
            return ParseResult.Ok;
        }



    }
}
