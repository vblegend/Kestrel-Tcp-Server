using System.Net;

namespace KestrelServer.Network
{
    public interface INetBlacklistTrie
    {
        public void Add(string ip, byte mask = 32);
        public void Add(IPAddress ip, byte mask = 32);
        public bool IsBlocked(IPAddress ip);

    }
}
