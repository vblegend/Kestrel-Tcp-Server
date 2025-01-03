using KestrelServer.Message;
using KestrelServer.Network;
using KestrelServer.Pools;
using System.Threading.Tasks;


namespace System.Buffers
{
    public static class SessionExtensions
    {
        public static void Write(this IConnectionSession context, INetMessage message)
        {
            MessageBuilder.WriteTo(message, context.Writer);
        }


        public static async ValueTask WriteFlushAsync(this IConnectionSession context, INetMessage message)
        {
            MessageBuilder.WriteTo(message, context.Writer);
            await context.FlushAsync();
        }
    }
}
