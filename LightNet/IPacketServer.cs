using LightNet.Adapters;
using System;
using System.Threading.Tasks;

namespace LightNet
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


        /// <summary>
        /// 获取/设置 最大连接限制
        /// </summary>
        Int32 MaximumConnectionLimit { get; set; }

        /// <summary>
        /// 获取当前客户端连接数
        /// </summary>
        Int32 CurrentConnections { get; }

        /// <summary>
        /// 接收缓冲区大小
        /// </summary>
        Int32 ReceiveBufferSize { get; set; }

        /// <summary>
        /// 发送缓冲区大小
        /// </summary>
        Int32 SendBufferSize { get; set; }
    }
}
