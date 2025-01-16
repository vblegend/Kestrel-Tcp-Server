using Light.Transmit;
using Light.Transmit.Adapters;
using System.Buffers;

namespace Examples
{
    public class NoneClientHandlerAdapter : ClientHandlerAdapter
    {
        public override ValueTask OnClose(IConnectionSession session)
        {
            return ValueTask.CompletedTask;
        }

        public override ValueTask OnConnection(IConnectionSession session)
        {
            return ValueTask.CompletedTask;
        }

        public override ValueTask OnError(IConnectionSession session, Exception exception)
        {
            return ValueTask.CompletedTask;
        }

        public override UnPacketResult OnPacket(IConnectionSession session, ReadOnlySequence<byte> buffer)
        {
            return new UnPacketResult((Int32)buffer.Length);
        }
    }
}
