using System;
using System.Buffers;
using System.Runtime.CompilerServices;


namespace PacketNet.Message
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
            reader.TryRead<byte>(out var dataLen);
            if (dataLen != reader.UnreadSequence.Length % 255) return ParseResult.Illicit;
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
