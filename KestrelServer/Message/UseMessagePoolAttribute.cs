using System;

namespace KestrelServer.Message
{


    public abstract class UsePoolProxyAttribute : Attribute
    {
        public abstract IMessagePoolProxy Proxy();
    }

    /// <summary>
    ///  使用消息池，用于Resolver的消息对象创建/获取，对象池中对象数量超过此值时Return将不会继续放回池子
    /// </summary>
    /// <typeparam name="TObject"></typeparam>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class UseMessagePoolAttribute<TObject> : UsePoolProxyAttribute where TObject : AbstractNetMessage, new()
    {
        IMessagePoolProxy _proxy = MessagePool<TObject>.Proxy;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="_poolMaxCapacity"> 调整内存池的容量</param>
        public UseMessagePoolAttribute(Int32 _poolMaxCapacity  = 0)
        {
            if (_poolMaxCapacity > 0)
            {
                MessagePool<TObject>.SetPoolMaxCapacity(_poolMaxCapacity);
            }
        }



        public override IMessagePoolProxy Proxy()
        {
            return _proxy;
        }
    }


}
