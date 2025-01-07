using PacketNet.Message;
using System;
using System.Runtime.CompilerServices;

namespace PacketNet.Message
{

    public interface MessageGetter
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        AbstractNetMessage GetMessage();
    }

    public class FMessage<TMessage> : MessageGetter where TMessage : AbstractNetMessage, new()
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AbstractNetMessage GetMessage()
        {
            return MFactory<TMessage>.GetMessage();
        }
    }

    public interface IMessageAttribute
    {
        //  这里用接口反而降
        MessageGetter BuildGetter();

        Func<AbstractNetMessage> GetFunc();
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


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MessageGetter BuildGetter()
        {
            return new FMessage<TMessage>();
        }


        public Func<AbstractNetMessage> GetFunc()
        {
            return MFactory<TMessage>.GetMessage;
        }



    }
}
