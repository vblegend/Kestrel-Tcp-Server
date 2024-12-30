using KestrelServer.Message;
using Microsoft.Extensions.ObjectPool;

namespace KestrelServer.Tcp
{
    internal class SessionPool
    {
        private class SessionPooledObjectPolicy : PooledObjectPolicy<InternalSession>
        {
            public override InternalSession Create()
            {
                return new InternalSession();
            }

            public override bool Return(InternalSession obj)
            {
                obj.Clean();
                return true;
            }
        }


        internal static ObjectPool<InternalSession> Pool = CreateObjectPool();


        private static ObjectPool<InternalSession> CreateObjectPool()
        {
            var provider = new DefaultObjectPoolProvider();
            return provider.Create(new SessionPooledObjectPolicy());
        }

    }
}
