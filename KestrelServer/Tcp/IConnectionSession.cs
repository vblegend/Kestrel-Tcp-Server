using System;
using System.Net;
using System.Threading.Tasks;

namespace KestrelServer.Tcp
{
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
        void Close();

    }
}
