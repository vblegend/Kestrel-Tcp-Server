using LightNet.Message;

namespace Examples
{


    /// <summary>
    /// 消息种类定义
    /// </summary>
    public static class MessageKind
    {
        /// <summary>
        /// 无效消息
        /// </summary>
        [Kind]
        public const Int16 None = 0;
        

        /// <summary>
        /// 网关消息
        /// </summary>
        [Kind]
        public const Int16 Gateway = 1;


        /// <summary>
        /// 测试消息
        /// </summary>
        [Kind]
        public const Int16 Example = 2;





        [Kind]
        public const Int16 MinKind = -32767;

        [Kind]
        public const Int16 MaxKind = 32767;
    }
}
