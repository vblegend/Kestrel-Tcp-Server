using System;
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
        public Int64 ConnectionId { get; }
        public EndPoint? RemoteEndPoint { get; }
        public Object? Data {  get; set; }
        public DateTime ConnectTime { get; }
        void Write(ReadOnlySpan<byte> buffer);
        void Write(ReadOnlyMemory<byte> buffer);
        void Write(ArraySegment<byte> buffer);
        Task WriteAsync(ArraySegment<byte> buffer);
        Task WriteAsync(ReadOnlyMemory<byte> buffer);
        Task FlushAsync();
        void Close(SessionShutdownCause cause = SessionShutdownCause.NONE);

    }
}
