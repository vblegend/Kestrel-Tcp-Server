using System;
using System.Collections.Concurrent;
using System.Threading;


namespace Light.Transmit.Pools
{
    public class ObjectPool<TObject> where TObject : class, new()
    {
        private int _maxCapacity = Environment.ProcessorCount * 2;
        private int _numItems;

        private readonly ConcurrentQueue<TObject> _items = new();
        private TObject _fastItem;
        private readonly Func<TObject> _createFunc;

        /// <summary>
        /// 设置消息池的最大容量，对象池中对象数量超过此值时Return将不会继续放回池子
        /// </summary>
        /// <param name="value">池子最大容量值</param>
        /// <param name="releaseNow">是否立即释放多余的对象</param>
        public void SetPoolMaxCapacity(int value, bool releaseNow)
        {
            if (value < 0) value = Environment.ProcessorCount * 2;
            _maxCapacity = value;
            while (_numItems > _maxCapacity && releaseNow)
            {
                var item = Get();
            }
        }

        internal ObjectPool(Func<TObject> _createFunc, int _poolCapacity)
        {
            this._createFunc = _createFunc;
            _maxCapacity = _poolCapacity;
        }



        /// <summary>
        /// 从池中取出一个消息对象，如果池中没有消息对象则新创建
        /// </summary>
        /// <returns></returns>

        public unsafe TObject Get()
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
            return item;
        }


        /// <summary>
        /// 将消息对象归还到池中
        /// </summary>
        /// <param name="obj"></param>
        public void Return(TObject obj)
        {
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

    }
}
