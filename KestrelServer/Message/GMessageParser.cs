using System.Buffers;
using System;
using System.Runtime.CompilerServices;


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
        public GMessageParser(GMPayloadResolver? resolver = null)
        {
            this.resolver = resolver ?? GMPayloadResolver.Default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Byte GetKindLen(GMFlags flags)
        {
            if ((flags & GMFlags.Kind4) == GMFlags.Kind4) return 4;
            if ((flags & GMFlags.Kind3) == GMFlags.Kind3) return 3;
            if ((flags & GMFlags.Kind2) == GMFlags.Kind2) return 2;
            return 1;
        }

        public ParseResult Parse(SequenceReader<byte> reader, out INetMessage message)
        {
            message = default;
            reader.TryRead<ushort>(out var header);
            if (header != INetMessage.Header) return ParseResult.Illicit;
            reader.TryRead<GMFlags>(out var flags);
            reader.TryRead<UInt16>(out var packetLen);
            if (reader.Length < packetLen) return ParseResult.Partial;
            
            var kl = GetKindLen(flags);
            reader.TryRead(kl, out Int32 kind);
            reader.TryRead(out Int32 time);
            reader.TryRead<byte>(out var dataLen);
            if (dataLen != reader.UnreadSequence.Length % 255) return ParseResult.Illicit;
            message = resolver.Resolver(kind);
            message.Read(new SequenceReader<byte>(reader.UnreadSequence));
            return ParseResult.Ok;
        }



    }
}
