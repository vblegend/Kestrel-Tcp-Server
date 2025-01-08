using System;
using System.Threading.Tasks;

namespace PacketNet
{
    public interface IPacketServer
    {
        IPacketServer Options(ServerOptions options);

        void Listen(Uri uri);

        Task StopAsync();

    }
}
