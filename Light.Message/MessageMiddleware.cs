using Light.Transmit;
using System;

namespace Light.Message
{
    public abstract class MessageMiddleware
    {
        /// <summary>
        /// 收到消息回调
        /// </summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        /// <returns>是否继续执行下一个中间件</returns>
        public abstract Boolean OnMessage(IConnectionSession session, AbstractNetMessage message);
    }



}
