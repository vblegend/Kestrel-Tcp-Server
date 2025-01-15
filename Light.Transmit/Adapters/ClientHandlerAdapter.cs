using System;
using System.Buffers;
using System.Threading.Tasks;

namespace Light.Transmit.Adapters
{
    /// <summary>
    /// 客户端适配器
    /// </summary>
    public abstract class ClientHandlerAdapter
    {

        /// <summary>
        /// 成功连接至服务器
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public abstract ValueTask OnConnection(IConnectionSession session);


        /// <summary>
        /// 客户端连接被关闭
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public abstract ValueTask OnClose(IConnectionSession session);


        /// <summary>
        /// 遇到无法恢复的错误
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public abstract ValueTask OnError(IConnectionSession session, Exception exception);

        /// <summary>
        /// 收到一个封包
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public abstract UnPacketResult OnPacket(IConnectionSession session, ReadOnlySequence<byte> buffer);

    }
}
