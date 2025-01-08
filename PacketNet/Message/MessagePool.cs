using System;
using System.Collections.Concurrent;
using System.Threading;

namespace PacketNet.Message
{
    internal interface IMessagePool
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


    /// <summary>
    /// 泛型的消息池实现将AbstractNetMessage消息池化
    /// </summary>
    /// <typeparam name="TMessage"></typeparam>
    public class MessagePool<TMessage> : IMessagePool where TMessage : AbstractNetMessage, new()
    {
        private int _maxCapacity = Environment.ProcessorCount * 2;
        private int _numItems;

        private readonly ConcurrentQueue<TMessage> _items = new();
        private TMessage _fastItem;
        private readonly Func<TMessage> _createFunc;

        /// <summary>
        /// 设置消息池的最大容量，对象池中对象数量超过此值时Return将不会继续放回池子
        /// </summary>
        /// <param name="value"></param>
        public void SetCapacity(Int32 value)
        {
            if (value <= 1) throw new ArgumentOutOfRangeException("Parameter value must be greater than 1");
            _maxCapacity = value;
            while (_numItems > _maxCapacity)
            {
                var item = Get();
                item._pool = null;
                item = null;
            }
        }

        internal MessagePool(Func<TMessage> _createFunc, Int32 _poolCapacity)
        {
            this._createFunc = _createFunc;
            this._maxCapacity = _poolCapacity;
        }

        /// <summary>
        /// 从池中取出一个消息对象，如果池中没有消息对象则新创建
        /// </summary>
        /// <returns></returns>

        public unsafe TMessage Get()
        {
            var item = _fastItem;
            if (item == null || Interlocked.CompareExchange(ref _fastItem, null, item) != item)
            {
                if (_items.TryDequeue(out item))
                {
                    Interlocked.Decrement(ref _numItems);
                }
                else
                {
                    // no object available, so go get a brand new one
                    item = _createFunc();
                }
            }
            item._pool = this;
            return item;
        }


        /// <summary>
        /// 将消息对象归还到池中
        /// </summary>
        /// <param name="obj"></param>
        public void Return(TMessage obj)
        {
            obj._pool = null;
            if (_fastItem != null || Interlocked.CompareExchange(ref _fastItem, obj, null) != null)
            {
                if (Interlocked.Increment(ref _numItems) <= _maxCapacity)
                {
                    _items.Enqueue(obj);
                    return;
                }
                // no room, clean up the count and drop the object on the floor
                Interlocked.Decrement(ref _numItems);
            }
        }


        AbstractNetMessage IMessagePool.Get()
        {
            return Get();
        }

        void IMessagePool.Return(AbstractNetMessage message)
        {
            Return((TMessage)message);
        }

    }
}
