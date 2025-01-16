using Light.Transmit;
using Light.Transmit.Adapters;
using Light.Transmit.Network;
using System.Buffers;

namespace Examples
{
    public class NoneClientHandlerAdapter : ClientHandlerAdapter
    {

        private readonly ILogger<NoneClientHandlerAdapter> logger = LoggerProvider.CreateLogger<NoneClientHandlerAdapter>();

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
