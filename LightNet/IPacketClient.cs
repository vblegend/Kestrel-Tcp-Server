using LightNet.Adapters;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LightNet
{
    /// <summary>
    /// 封包客户端
    /// </summary>
    public interface IPacketClient : IDisposable
    {

        /// <summary>
        /// 连接到指定Uri
        /// </summary>
        /// <param name="remoteUri"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask ConnectAsync(Uri remoteUri, CancellationToken cancellationToken);

        /// <summary>
        /// 设置事件适配器
        /// </summary>
        /// <param name="handlerAdapter"></param>
        void SetAdapter(ClientHandlerAdapter handlerAdapter);

        /// <summary>
        /// 当前客户端连接状态
        /// </summary>
        Boolean Connected { get; }

        /// <summary>
        /// 关闭客户端连接
        /// </summary>
        Task CloseAsync();

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
