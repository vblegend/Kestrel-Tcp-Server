using Microsoft.Extensions.ObjectPool;

namespace KestrelServer.Message
{
    public class Pool<T> where T : class, INetMessage, new()
    {


        private class GMessagePooledObjectPolicy<T> : PooledObjectPolicy<T> where T : INetMessage, new()
        {
            public override T Create()
            {
                return new T();
            }

            public override bool Return(T obj)
            {
                return true;
            }
        }


        public static readonly ObjectPool<T> Shared = CreateObjectPool();


        private static ObjectPool<T> CreateObjectPool()
        {
            var provider = new DefaultObjectPoolProvider();
            return provider.Create(new GMessagePooledObjectPolicy<T>());
        }
    }
}
