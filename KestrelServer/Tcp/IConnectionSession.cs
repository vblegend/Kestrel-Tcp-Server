using System;
using System.Net;
using System.Threading.Tasks;

namespace KestrelServer.Tcp
{

    public interface ISessionData
    {

    }



    public interface IConnectionSession
    {
        public String ConnectionId { get; set; }
        public EndPoint? RemoteEndPoint { get; }
        public ISessionData Data {  get; set; }

        public DateTime ConnectTime { get; set; }
        void Write(ReadOnlySpan<byte> buffer);
        void Write(ReadOnlyMemory<byte> buffer);
        void Write(ArraySegment<byte> buffer);
        Task WriteAsync(ArraySegment<byte> buffer);
        Task WriteAsync(ReadOnlyMemory<byte> buffer);
        Task FlushAsync();
        void Close();

    }
}
