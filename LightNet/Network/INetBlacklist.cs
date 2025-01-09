using System.Net;

namespace LightNet.Network
{
    public interface INetBlacklist
    {
        public void Add(string ip);
        public void Add(IPAddress ip);
        public void Add(string ip, byte mask = 32);
        public void Add(IPAddress ip, byte mask = 32);
        public bool IsBlocked(IPAddress ip);

    }
}
