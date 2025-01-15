using Examples.Client;
using Examples.Gateway;
using Light.Message;

namespace Examples
{
    public class MessageResolvers
    {
        /// <summary>
        /// 网关消息解析器
        /// </summary>
        public static readonly MessageResolver GatewayResolver = MessageResolver.Create<GatewayMessage>();

        /// <summary>
        /// 客户端消息解析器
        /// </summary>
        public static readonly MessageResolver CSResolver = MessageResolver.Create<CSMessage>();

    }
}
