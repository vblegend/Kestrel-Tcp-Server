using System;
using System.Buffers;
using System.Threading.Tasks;

namespace PacketNet
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
        public abstract ValueTask OnConnection(IPacketClient client);


        /// <summary>
        /// 客户端连接被关闭
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public abstract ValueTask OnClose(IPacketClient client);


        /// <summary>
        /// 遇到无法恢复的错误
        /// </summary>
        /// <param name="exception"></param>
        /// <returns></returns>
        public abstract ValueTask OnError(Exception exception);

        /// <summary>
        /// 收到一个封包
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns></returns>
        public abstract ValueTask<UnPacketResult> OnPacket(ReadOnlySequence<Byte> sequence);

    }
}
