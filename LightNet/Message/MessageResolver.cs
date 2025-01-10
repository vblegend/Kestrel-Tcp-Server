using LightNet.Internals;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;



namespace LightNet.Message
{
    public class MessageResolver
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
        /// 创建指定类型的
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

        public unsafe AbstractNetMessage Resolver(Int16 kind)
        {
            if (Keys.TryGetValue(kind, out var p))
            {
                delegate*<AbstractNetMessage> getter = (delegate*<AbstractNetMessage>)p;
                return getter();
            }
            throw new InvalidOperationException();
        }



    }
}
