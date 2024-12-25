using Microsoft.Extensions.ObjectPool;
using System;

namespace KestrelServer
{
    public partial class GMessage
    {
        private class GMessagePooledObjectPolicy : PooledObjectPolicy<GMessage>
        {
            public override GMessage Create()
            {
                var message = new GMessage();
                Console.WriteLine("Create GMessage.");
                return message;
            }

            public override bool Return(GMessage obj)
            {
                obj._isReturn = true;
                return true;
            }
        }


        private static ObjectPool<GMessage> Pool = ObjectPool.Create(new GMessagePooledObjectPolicy());

        /// <summary>
        /// Use GMessage.Create()
        /// </summary>
        public GMessage()
        {
            
        }


        /// <summary>
        /// 从封包对象池中取出一个封包对象
        /// </summary>
        /// <returns></returns>
        public static GMessage Create()
        {
            var message = GMessage.Pool.Get();
            message._isReturn = false;
            return message;
        }


        /// <summary>
        /// 将封包放入封包对象池中
        /// </summary>
        public void Return()
        {
            this.Payload.Clear();
            this.Parameters.Clear();
            this.Action = 0;
            this.Timestamp = 0;
            this.SerialNumber = 0;
            if (!_isReturn)
            {
                GMessage.Pool.Return(this);
            }
        
        }


    }
}
