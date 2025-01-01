
using System.Buffers;
using System;
using System.Threading.Tasks;

namespace KestrelServer.Network
{
    public interface IClientHandler
    {
        ValueTask OnConnection(TCPClient client);
        ValueTask OnClose(TCPClient client);
        ValueTask OnError(Exception exception);
        async ValueTask OnReceive(TCPClient client, ReadOnlySequence<Byte> data)
        {
            await ValueTask.CompletedTask;
        }
    }
}
