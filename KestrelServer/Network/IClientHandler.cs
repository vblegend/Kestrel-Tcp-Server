
using System.Buffers;
using System;
using System.Threading.Tasks;

namespace KestrelServer.Network
{
    public interface IClientHandler
    {
        Task OnConnection(TCPClient client);
        Task OnClose(TCPClient client);
        Task OnError(Exception exception);
        async Task OnReceive(TCPClient client, ReadOnlySequence<Byte> data)
        {
            await Task.CompletedTask;
        }
    }
}
