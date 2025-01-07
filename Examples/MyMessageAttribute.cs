using PacketNet.Message;

namespace Examples
{
    public class MyMessageAttribute<TMessage> : PacketNet.Message.MessageAttribute<TMessage> where TMessage : AbstractNetMessage, new()
    {
        public MyMessageAttribute(MessageKind kind, int poolCapacity = 0) : base((Int16)kind, poolCapacity)
        {
        }
    }
}
