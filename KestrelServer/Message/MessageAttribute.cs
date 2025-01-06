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
        public readonly Int32 PoolCapacity;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="kind">定义消息的Kind</param>
        /// <param name="poolCapacity">池容量，如果为0则禁用池</param>
        public unsafe MessageAttribute(MessageKind kind, Int32 poolCapacity = 0)
        {
            PoolCapacity = poolCapacity;
            fixed (Int16* ptr = &MessagePool<TMessage>.Kind) *ptr = (Int16)kind;
            if (poolCapacity > 0)
            {
                // used pool
                Getter = MessagePool<TMessage>.Shared.Get;
                MessagePool<TMessage>.Shared.SetCapacity(poolCapacity);
            }
            else
            {
                // not used pool
                Getter = () =>
                {
                    var msg = new TMessage();
                    fixed (Int16* ptr = &msg.Kind) *ptr = (Int16)kind;
                    return msg;
                };
            }
        }
    }
}
