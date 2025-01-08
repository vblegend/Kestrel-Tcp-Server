
using System;
using System.Buffers;
using System.Threading.Tasks;

namespace PacketNet.Network
{
    public interface IClientHandler
    {
        ValueTask OnConnection(TCPClient client);
        ValueTask OnClose(TCPClient client);
        ValueTask OnError(Exception exception);
        async ValueTask OnReceive(ReadOnlySequence<Byte> data)
        {
            await ValueTask.CompletedTask;
        }
    }
}
