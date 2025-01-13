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




        /// <summary>
        /// 登录验证结果
        /// </summary>
        [Kind]
        public const Int16 AuthResponse = 32766;

        /// <summary>
        /// 登录验证
        /// </summary>
        [Kind]
        public const Int16 AuthRequest = 32767;
    }
}
