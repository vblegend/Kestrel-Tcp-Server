using System;
using System.Buffers;
using System.Net;
using System.Threading.Tasks;

namespace KestrelServer.Network
{

    public enum SessionShutdownCause
    {
        /// <summary>
        /// 没有任何原因的关闭
        /// </summary>
        NONE = 0,

        /// <summary>
        /// 客户端协商断开
        /// </summary>
        GRACEFUL = 1,

        /// <summary>
        /// 客户端意外断开
        /// </summary>
        CLIENT_DISCONNECTED = 2,

        /// <summary>
        /// 服务器关闭
        /// </summary>
        SERVER_SHUTTING_DOWN = 3,

        /// <summary>
        /// 客户端非法数据
        /// </summary>
        CLIENT_ILLEGAL_DATA = 4,

    }



    public interface IConnectionSession
    {

        /// <summary>
        /// 连接ID，递增，本次启动期间不会重复
        /// </summary>
        public Int64 ConnectionId { get; }

        /// <summary>
        /// 客户端IP、端口
        /// </summary>
        public EndPoint RemoteEndPoint { get; }

        /// <summary>
        /// 用户自定义数据
        /// </summary>
        public Object[] Datas { get; }

        /// <summary>
        /// 客户端连接的时间戳
        /// </summary>
        public DateTime ConnectTime { get; }

        /// <summary>
        /// 获取原始的数据写入对象
        /// </summary>
        public IBufferWriter<byte> Writer { get; }

        /// <summary>
        /// 将要发送的数据写入发送缓冲区
        /// </summary>
        /// <param name="buffer"></param>
        void Write(ReadOnlySpan<byte> buffer);

        /// <summary>
        /// 将要发送的数据写入发送缓冲区
        /// </summary>
        /// <param name="buffer"></param>
        void Write(ReadOnlyMemory<byte> buffer);

        /// <summary>
        /// 将要发送的数据写入发送缓冲区
        /// </summary>
        /// <param name="buffer"></param>
        void Write(ArraySegment<byte> buffer);

        /// <summary>
        /// 将要发送的数据写入发送缓冲区并立即提交
        /// </summary>
        /// <param name="buffer"></param>
        ValueTask WriteAsync(ArraySegment<byte> buffer);

        /// <summary>
        /// 将要发送的数据写入发送缓冲区并立即提交
        /// </summary>
        /// <param name="buffer"></param>
        ValueTask WriteAsync(ReadOnlyMemory<byte> buffer);

        /// <summary>
        /// 将发送缓冲区数据立即提交
        /// </summary>
        /// <returns></returns>
        ValueTask FlushAsync();

        /// <summary>
        /// 主动关闭Socket连接
        /// </summary>
        /// <param name="cause"></param>
        void Close(SessionShutdownCause cause = SessionShutdownCause.NONE);

    }
}
