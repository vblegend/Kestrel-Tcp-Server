using System;
using System.Collections.Concurrent;
using System.Threading;

namespace KestrelServer
{
    public class ObjectHeap<T> where T : class
    {
        private readonly ConcurrentDictionary<int, T> _store = new ConcurrentDictionary<int, T>();
        private readonly ConcurrentBag<int> _freeIndices = new ConcurrentBag<int>();
        private int _currentIndex = 0;


        /// <summary>
        /// 将对象放入存储，并返回一个可复用的索引。
        /// </summary>
        public int Put(T obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj), "Object cannot be null");
            if (!_freeIndices.TryTake(out var index))
            {
                index = Interlocked.Increment(ref _currentIndex) - 1;
            }
            _store[index] = obj;
            return index;
        }

        /// <summary>
        /// 根据索引获取对象。
        /// </summary>
        public T Index(int index)
        {
            _store.TryGetValue(index, out var obj);
            return obj;
        }

        /// <summary>
        /// 根据索引取出对象，并将索引归还到空闲池。
        /// </summary>
        public T Take(int index)
        {
            if (_store.TryRemove(index, out var _object))
            {
                _freeIndices.Add(index);
            }
            return _object;
        }

    }

}
