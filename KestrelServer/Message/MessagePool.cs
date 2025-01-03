using Microsoft.Extensions.ObjectPool;
using System;

namespace KestrelServer.Message
{
    public interface IMessagePoolProxy
    {
        /// <summary>
        /// 获取一个可复用的消息对象
        /// </summary>
        /// <returns></returns>
        public AbstractNetMessage Get();
        /// <summary>
        /// 将消息对象归还到池子里 (应使用 messaeg.Return();)
        /// </summary>
        /// <param name="message"></param>
        public void Return(AbstractNetMessage message);
    }

    public class MessagePool<T> where T : AbstractNetMessage, new()
    {

        internal class GMessagePooledObjectPolicy<T> : PooledObjectPolicy<T> where T : AbstractNetMessage, new()
        {
            public override T Create()
            {
                Console.WriteLine($"Create Message Object: {typeof(T).FullName}");
                return new T();
            }

            public override bool Return(T obj)
            {
                obj.ReturnFunc = null;
                return true;
            }
        }

        internal class MessagePoolProxy<TObject> : IMessagePoolProxy where TObject : AbstractNetMessage, new()
        {
            public AbstractNetMessage Get()
            {
                var s = MessagePool<TObject>.Shared.Get();
                s.ReturnFunc = Return;
                return s;
            }

            public void Return(AbstractNetMessage message)
            {
                MessagePool<TObject>.Shared.Return((TObject)message);
            }
        }

        /// <summary>
        /// 原始的内存池对象
        /// </summary>
        private static readonly ObjectPool<T> Shared = CreateObjectPool();

        /// <summary>
        /// 带有池Return注入的代理，用于工厂的对象创建
        /// </summary>
        public static readonly IMessagePoolProxy Proxy = new MessagePoolProxy<T>();


        /// <summary>
        /// 设置消息池的最大容量，对象池中对象数量超过此值时Return将不会继续放回池子
        /// </summary>
        /// <param name="value"></param>
        public static void SetPoolMaxCapacity(Int32 value)
        {
            if(value <= 0) throw new ArgumentOutOfRangeException("Parameter value must be greater than 0");
            var type = Shared.GetType();
            var field = type.GetField("_maxCapacity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            field.SetValue(Shared, value);
            type = null;
            field = null;
        }


        private static ObjectPool<T> CreateObjectPool()
        {
            Console.WriteLine($"Create Pool {typeof(T).FullName}");
            var provider = new DefaultObjectPoolProvider();
            return provider.Create(new GMessagePooledObjectPolicy<T>());
        }

    }
}
