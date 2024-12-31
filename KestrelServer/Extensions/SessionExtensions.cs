using KestrelServer.Message;
using KestrelServer.Network;
using KestrelServer.Pools;
using System.Threading.Tasks;


namespace System.Buffers
{
    public static class SessionExtensions
    {



        public static async Task SendFlushAsync(this IConnectionSession context, GMessage message)
        {
            using (var stream = StreamPool.GetStream())
            {
                await message.WriteToAsync(stream/* , context.Items["timeService"] */);
                message.Return();
                var sequence = stream.GetReadOnlySequence();
                foreach (var item in sequence)
                {
                    context.Write(item.Span);
                }
                await context.FlushAsync();
            }
        }

        public static async Task SendAsync(this IConnectionSession context, GMessage message)
        {
            using (var stream = StreamPool.GetStream())
            {
                await message.WriteToAsync(stream/* , context.Items["timeService"] */);
                message.Return();
                var sequence = stream.GetReadOnlySequence();
                foreach (var item in sequence)
                {
                    context.Write(item.Span);
                }
            }
        }
    }
}
