
using PacketNet.Pipes;
using PacketNet;
using PacketNet.Network;
using System.Buffers;

namespace Examples
{
    public class TestPipeSevice : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var uri1 = new Uri("tcp://0.0.0.0:5000");
            var uri2 = new Uri("pipe://Hell-oHdls.ssss:123?f=1&b=2");
            var options = new ServerOptions();
            options.OnConnected = OnConnected;
            options.OnPacket = OnPacket;
            options.OnReceive = OnReceive;
            options.OnError = OnError;
            options.OnClose = OnClose;
            var ps = new PipeServer();
            ps.Options(options);
            ps.Listen(uri2);
        }

        async ValueTask OnClose(IConnectionSession session)
        {

        }
        async ValueTask OnError(IConnectionSession session, Exception ex)
        {

        }

        async ValueTask<UInt32> OnPacket(IConnectionSession session, ReadOnlySequence<Byte> sequence)
        {
            return 11;

        }

        async ValueTask<bool> OnConnected(IConnectionSession session)
        {
            return true;
        }

        async ValueTask OnReceive(IConnectionSession session, ReadOnlySequence<byte> sequence)
        {

        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {

        }
    }
}
