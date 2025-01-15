using Light.Transmit;
using Light.Transmit.Adapters;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;



namespace Light.Message
{
    /// <summary>
    /// 消息解析器
    /// </summary>
    public sealed class MessageResolver
    {
        private readonly ILogger<MessageResolver> logger = LoggerProvider.CreateLogger<MessageResolver>();
        private readonly Dictionary<Int16, IntPtr> Keys = new();

        private MessageResolver(Type baseMessageType)
        {
            var list = MessageLoader.InitializedTypes.ToArray();
            foreach (var item in list.Where(e => e.Type.IsSubclassOf(baseMessageType)))
            {
                if (Keys.TryGetValue(item.Kind, out var pointer))
                {
                    if (pointer == item.FuncPointer) continue;
                    logger.LogWarning("MessageResolver<{0}> kind:{1} 已存在", baseMessageType.Name, item.Kind);
                    continue;
                }
                Keys.Add(item.Kind, item.FuncPointer);
            }
        }

        private static Dictionary<Type, MessageResolver> typeCache = new Dictionary<Type, MessageResolver>();

        /// <summary>
        /// 创建实现类型基类的消息解析器
        /// </summary>
        /// <typeparam name="TMessage"></typeparam>
        /// <returns></returns>
        public static MessageResolver Create<TMessage>()
        {
            var type = typeof(TMessage);
            if (typeCache.TryGetValue(typeof(TMessage), out var resolver))
            {
                return resolver;
            }
            resolver = new MessageResolver(type);
            typeCache.Add(type, resolver);
            return resolver;
        }

        /// <summary>
        /// 根据kind解析消息对象
        /// </summary>
        /// <param name="kind"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe AbstractNetMessage Resolver(Int16 kind)
        {
            if (Keys.TryGetValue(kind, out var p))
            {
                delegate*<AbstractNetMessage> getter = (delegate*<AbstractNetMessage>)p;
                return getter();
            }
            throw new InvalidOperationException($"MessageResolver: Resolver cannot find message bound to Kind[{kind}].");
        }


        public Boolean TryAddType<TMessage>()
        {
            return TryAddType(typeof(TMessage));
        }


        public Boolean TryAddType(Type type)
        {
            var list = MessageLoader.InitializedTypes.ToArray();
            var first = list.Where(e => e.Type == type).FirstOrDefault();
            if (first == null) return false;
            if (Keys.TryGetValue(first.Kind, out var sss))
            {
                return sss == first.FuncPointer;
            }
            Keys.Add(first.Kind, first.FuncPointer);
            return true;
        }



        /// <summary>
        /// 尝试从字节流中解析消息
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="message"></param>
        /// <param name="readLength"></param>
        /// <returns></returns>
        public ParseResult TryReadMessage(ref SequenceReader<byte> reader, out AbstractNetMessage message, out UInt16 readLength)
        {
            message = default;
            readLength = 0;
            reader.TryRead<ushort>(out var header);
            if (header != AbstractNetMessage.Header) return ParseResult.Illicit;
            reader.TryRead<MessageFlags>(out var flags);
            reader.TryRead<UInt16>(out readLength);
            if (reader.Remaining < readLength - 5) return ParseResult.Partial;
            var kl = GetKindLen(flags);
            reader.TryRead(kl, out Int16 kind);
            reader.TryRead(out UInt64 time);
            message = Resolver(kind);
            message.Read(ref reader);
            return ParseResult.Ok;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Byte GetKindLen(MessageFlags flags)
        {
            if ((flags & MessageFlags.Kind2) == MessageFlags.Kind2) return 2;
            return 1;
        }
        // 474D 00 1300 02 00000000 08 FFFFFFFFFFFFFF7F


    }
}
