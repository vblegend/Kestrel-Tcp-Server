using System.Threading.Tasks;

namespace Light.Transmit.Internals
{
    internal class SendEventContext
    {
        public InternalNetSession Session;
        public TaskCompletionSource TaskSource;
    }
}
