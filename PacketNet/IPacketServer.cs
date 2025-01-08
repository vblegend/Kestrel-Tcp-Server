using System;
using System.Threading.Tasks;

namespace PacketNet
{
    /// <summary>
    /// 封包服务器
    /// </summary>
    public interface IPacketServer
    {

        /// <summary>
        /// 设置事件适配器
        /// </summary>
        /// <param name="handlerAdapter"></param>
        void SetAdapter(ServerHandlerAdapter handlerAdapter);


        /// <summary>
        /// 监听指定Uri
        /// </summary>
        /// <param name="uri"></param>
        void Listen(Uri uri);

        /// <summary>
        /// 停止服务
        /// </summary>
        /// <returns></returns>
        Task StopAsync();


        /// <summary>
        /// 获取/设置 触发OnPacket事件的最小封包长度
        /// </summary>
        UInt32 MinimumPacketLength { get; set; }

    }
}
