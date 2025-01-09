using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LightNet;

namespace Microsoft.Extensions.Hosting
{
    public static class HostExtensions
    {


        public static IHost PacketLogger(this IHost host)
        {
            LoggerProvider.Initialize(host.Services.GetRequiredService<ILoggerFactory>());
            return host;
        }

    }
}
