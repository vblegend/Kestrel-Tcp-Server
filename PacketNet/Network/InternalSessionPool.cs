using System;
using System.Collections.Concurrent;
using System.Threading;

namespace PacketNet.Network
{
    internal class InternalSessionPool : IDisposable
    {
        private readonly int _maxCapacity;
        private int _numItems;

        private protected readonly ConcurrentQueue<InternalSession> _items = new();
        private protected InternalSession _fastItem;


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
        public InternalSession Get()
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
                return new InternalSession();
            }

            return item;
        }

        /// <inheritdoc />
        public void Return(InternalSession obj)
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
