using System;
using System.Collections.Concurrent;
using System.Threading;

namespace LightNet.Internals
{
    internal class InternalSessionPool<TSession> : IDisposable where TSession : class, IConnectionSession, new()
    {
        private readonly int _maxCapacity;
        private int _numItems;

        private protected readonly ConcurrentQueue<TSession> _items = new();
        private protected TSession _fastItem;


        /// <summary>
        /// Creates an instance of <see cref="DefaultObjectPool{T}"/>.
        /// </summary>
        /// <param name="policy">The pooling policy to use.</param>
        /// <param name="maximumRetained">The maximum number of objects to retain in the pool.</param>
        public InternalSessionPool(int maximumRetained)
        {
            _maxCapacity = maximumRetained - 1;  // -1 to account for _fastItem
        }

        /// <inheritdoc />
        public TSession Get()
        {
            var item = _fastItem;
            if (item == null || Interlocked.CompareExchange(ref _fastItem, null, item) != item)
            {
                if (_items.TryDequeue(out item))
                {
                    Interlocked.Decrement(ref _numItems);
                    return item;
                }

                // no object available, so go get a brand new one
                return new TSession();
            }

            return item;
        }

        /// <inheritdoc />
        public void Return(TSession obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            obj.Clean();
            if (_fastItem != null || Interlocked.CompareExchange(ref _fastItem, obj, null) != null)
            {
                if (Interlocked.Increment(ref _numItems) <= _maxCapacity)
                {
                    _items.Enqueue(obj);
                    return;
                }
                // no room, clean up the count and drop the object on the floor
                Interlocked.Decrement(ref _numItems);
                return;
            }
            return;
        }

        public void Dispose()
        {
            _fastItem = null;
            _items.Clear();
        }

    }
}
