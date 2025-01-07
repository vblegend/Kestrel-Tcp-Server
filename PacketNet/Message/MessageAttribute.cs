using PacketNet.Message;
using System;
using System.Runtime.CompilerServices;

namespace PacketNet.Message
{
    public interface IMessageAttribute
    {
       unsafe delegate*<AbstractNetMessage> GetPointer();
    }



    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class MessageAttribute<TMessage> : Attribute, IMessageAttribute where TMessage : AbstractNetMessage, new()
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="kind">定义消息的Kind</param>
        /// <param name="poolCapacity">池容量，如果为0则禁用池</param>
        public unsafe MessageAttribute(Int16 kind, Int32 poolCapacity = 0)
        {
            MFactory<TMessage>.InitialFactory(kind, poolCapacity);
        }

        public unsafe delegate*<AbstractNetMessage> GetPointer()
        {
            return &MFactory<TMessage>.GetMessage;
        }

    }
}
