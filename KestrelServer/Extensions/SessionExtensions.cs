using KestrelServer.Message;
using KestrelServer.Network;
using KestrelServer.Pools;
using System.Threading.Tasks;


namespace System.Buffers
{
    public static class SessionExtensions
    {
        /// <summary>
        /// 将message数据写入发送缓冲区
        /// </summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        public static void Write(this IConnectionSession context, GMessage message)
        {
            using (var stream = StreamPool.GetStream())
            {
                message.WriteTo(stream);
                message.Return();
                var sequence = stream.GetReadOnlySequence();
                foreach (var item in sequence)
                {
                    context.Write(item.Span);
                }
            }
        }


        /// <summary>
        /// 将message数据写入发送缓冲区并立即提交
        /// </summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async ValueTask WriteFlushAsync(this IConnectionSession context, GMessage message)
        {
            using (var stream = StreamPool.GetStream())
            {
                message.WriteTo(stream);
                message.Return();
                var sequence = stream.GetReadOnlySequence();
                foreach (var item in sequence)
                {
                    context.Write(item.Span);
                }
                await context.FlushAsync();
            }
        }
    }
}
