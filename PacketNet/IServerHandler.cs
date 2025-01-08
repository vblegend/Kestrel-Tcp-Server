using PacketNet.Network;
using System;
using System.Buffers;
using System.Threading.Tasks;

namespace PacketNet
{
    public delegate ValueTask<bool> OnConnectedHandler(IConnectionSession session);
    public delegate ValueTask OnCloseHandler(IConnectionSession session);
    public delegate ValueTask OnErrorHandler(IConnectionSession session, Exception ex);
    public delegate ValueTask<UInt32> OnPacketHandler(IConnectionSession session, ReadOnlySequence<Byte> sequence);
    public delegate ValueTask OnReceiveHandler(IConnectionSession session, ReadOnlySequence<byte> sequence);


    public class ServerOptions
    {
        public OnConnectedHandler OnConnected;
        public OnCloseHandler OnClose;
        public OnErrorHandler OnError;
        public OnPacketHandler OnPacket;
        public OnReceiveHandler OnReceive;
    }



    public interface IServerHandler
    {
        ValueTask<bool> OnConnected(IConnectionSession session);

        ValueTask OnClose(IConnectionSession session);

        ValueTask OnError(IConnectionSession session, Exception ex);

        ValueTask<UInt32> OnPacket(IConnectionSession session, ReadOnlySequence<Byte> sequence);

        ValueTask OnReceive(IConnectionSession session, ReadOnlySequence<byte> sequence);

    }
}
