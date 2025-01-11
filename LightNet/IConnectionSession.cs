using System;
using System.Buffers;
using System.Net;
using System.Threading.Tasks;

namespace LightNet
{
    /// <summary>
    /// 会话关闭原因
    /// </summary>
    public enum SessionShutdownCause
    {
        /// <summary>
        /// 未设置关闭原因，或会话未关闭
        /// </summary>
        NONE = 0,

        /// <summary>
        /// 协商断开
        /// </summary>
        GRACEFUL = 1,

        /// <summary>
        /// 意外断开
        /// </summary>
        UNEXPECTED_DISCONNECTED = 2,

        /// <summary>
        /// 服务关闭
        /// </summary>
        SHUTTING_DOWN = 3,

        /// <summary>
        /// 非法数据
        /// </summary>
        ILLEGAL_DATA = 4,


        /// <summary>
        /// 错误
        /// </summary>
        ERROR = 5,


        /// <summary>
        /// 连接被拒绝
        /// </summary>
        CONNECTION_DENIAL = 6

    }


    /// <summary>
    /// 连接上下文会话
    /// </summary>
    public interface IConnectionSession
    {
        /// <summary>
        /// 断开原因
        /// </summary>
        public SessionShutdownCause CloseCause { get; }
        /// <summary>
        /// 连接ID，递增，本次启动期间不会重复，客户端始终为0
        /// </summary>
        public long ConnectionId { get; }

        /// <summary>
        /// session连接是否可用
        /// </summary>
        public Boolean IsConnected { get; }

        /// <summary>
        /// 客户端IP、端口
        /// </summary>
        public EndPoint RemoteEndPoint { get; }

        /// <summary>
        /// 用户自定义数据
        /// </summary>
        public object[] Datas { get; }

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

        /// <summary>
        /// 清除session 所有数据
        /// </summary>
        void Clean();

    }
}
