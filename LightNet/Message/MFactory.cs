using System;
using System.Runtime.CompilerServices;

namespace LightNet.Message
{

    internal delegate void MFactoryInitialMethod(Int16 kind, Int32 poolCapacity);


    /// <summary>
    /// 泛型消息工厂,提供基于池的消息创建和复用
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public static class MFactory<TMessage> where TMessage : AbstractNetMessage, new()
    {
        /// <summary>
        /// 消息池实例
        /// </summary>
        private static MessagePool<TMessage> _shared;

        /// <summary>
        /// 获取一个消息对象，如果消息使用额池则从池中获取
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TMessage GetMessage()
        {
            return _shared?.Get() ?? CreateMessageRawInternal();
        }

        /// <summary>
        /// 消息对应的Kind
        /// </summary>
        public static readonly Int16 Kind;

        /// <summary>
        /// 原始的消息对象创建
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe static TMessage CreateMessageRawInternal()
        {
            var msg = new TMessage();
            fixed (Int16* ptr = &msg.Kind) *ptr = Kind;
            return msg;
        }


        private static Boolean isInited = false;


        internal unsafe static void InitialFactory(Int16 kind, Int32 poolCapacity)
        {
            if (isInited) return;
            fixed (Int16* ptr = &Kind) *ptr = kind;
            if (poolCapacity >= 0)
            {
                if (poolCapacity == 0) poolCapacity = Environment.ProcessorCount * 2;
                _shared = new MessagePool<TMessage>(CreateMessageRawInternal, poolCapacity);
            }
            isInited = true;
        }
    }
}
