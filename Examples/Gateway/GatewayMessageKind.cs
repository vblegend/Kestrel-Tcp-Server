using LightNet.Message;

namespace Examples.Gateway
{
    public static class GatewayMessageKind
    {
        /// <summary>
        /// 网关消息
        /// </summary>
        [Kind]
        public const Int16 Ping = 998;

        /// <summary>
        /// 测试消息
        /// </summary>
        [Kind]
        public const Int16 Pong = 999;

    }
}
