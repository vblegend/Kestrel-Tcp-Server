using Light.Transmit;
using Light.Transmit.Adapters;
using System;
using System.Buffers;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Examples
{
    internal class NoneServerHandlerAdapter : ServerHandlerAdapter
    {
        public override bool OnAccept(Socket socket)
        {
            return true;
        }

        public override ValueTask OnClose(IConnectionSession session)
        {
            return ValueTask.CompletedTask;
        }

        public override ValueTask OnConnected(IConnectionSession session)
        {
            return ValueTask.CompletedTask;
        }

        public override ValueTask OnError(IConnectionSession session, Exception ex)
        {
            return ValueTask.CompletedTask;
        }

        public override UnPacketResult OnPacket(IConnectionSession session, ReadOnlySequence<byte> buffer)
        {
            return new UnPacketResult((Int32)buffer.Length);
        }
    }
}
