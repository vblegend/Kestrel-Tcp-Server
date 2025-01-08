using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace PacketNet
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
        void Close();

        /// <summary>
        /// 写入数据到缓冲区中但并不立即发送
        /// </summary>
        /// <param name="buffer"></param>
        void Write(ReadOnlySpan<byte> buffer);

        /// <summary>
        /// 写入数据到缓冲区中但并不立即发送
        /// </summary>
        /// <param name="buffer"></param>
        void Write(ReadOnlyMemory<byte> buffer);

        /// <summary>
        /// 写入数据到缓冲区中但并不立即发送
        /// </summary>
        /// <param name="buffer"></param>
        void Write(ArraySegment<byte> buffer);

        /// <summary>
        /// 写入数据到缓冲区中并立即发送
        /// </summary>
        /// <param name="buffer"></param>
        ValueTask WriteAsync(ArraySegment<byte> buffer);

        /// <summary>
        /// 写入数据到缓冲区中并立即发送
        /// </summary>
        /// <param name="buffer"></param>
        ValueTask WriteAsync(ReadOnlyMemory<byte> buffer);

        /// <summary>
        /// 将缓冲区数据立即发送
        /// </summary>
        /// <returns></returns>
        ValueTask FlushAsync();


        /// <summary>
        /// 获取/设置 触发OnPacket事件的最小封包长度
        /// </summary>
        UInt32 MinimumPacketLength { get; set; }

        /// <summary>
        /// 获取客户端流写入对象
        /// </summary>
        /// <returns></returns>
        IBufferWriter<byte> GetWriter();

    }
}
