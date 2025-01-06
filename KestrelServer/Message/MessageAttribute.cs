using System;

namespace KestrelServer.Message
{


    public abstract class IPoolGetterAttribute : Attribute
    {
        public Func<AbstractNetMessage> Getter;
    }




    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class MessageAttribute<TMessage> : IPoolGetterAttribute where TMessage : AbstractNetMessage, new() 
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="kind">定义消息的Kind</param>
        /// <param name="poolCapacity">池容量，如果为0则禁用池</param>
        public unsafe MessageAttribute(MessageKind kind, Int32 poolCapacity = 0)
        {
            fixed (Int16* ptr = &MessagePool<TMessage>.Kind) *ptr = (Int16)kind;
            Getter = MessagePool<TMessage>.Shared.Get;
            if (poolCapacity > 0)
            {
                MessagePool<TMessage>.Shared.SetCapacity(poolCapacity);
            }
        }
    }
}
