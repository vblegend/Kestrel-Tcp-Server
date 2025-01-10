using Examples.Client;
using Examples.Gateway;
using LightNet.Message;

namespace Examples
{
    public class MessageResolvers
    {
        public readonly MessageResolver GatewayResolver = MessageResolver.Create<GatewayMessage>();
        public readonly MessageResolver CSResolver = MessageResolver.Create<CSMessage>();

    }
}
