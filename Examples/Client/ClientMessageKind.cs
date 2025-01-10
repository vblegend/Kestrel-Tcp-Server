using LightNet.Message;

namespace Examples.Client
{


    /// <summary>
    /// 消息种类定义
    /// </summary>
    public static class ClientMessageKind
    {
        /// <summary>
        /// 无效消息
        /// </summary>
        [Kind]
        public const short None = 0;


        /// <summary>
        /// 网关消息
        /// </summary>
        [Kind]
        public const short Gateway = 100;


        /// <summary>
        /// 测试消息
        /// </summary>
        [Kind]
        public const short Example = 200;





        [Kind]
        public const short MinKind = -32767;

        [Kind]
        public const short MaxKind = 32767;
    }
}
