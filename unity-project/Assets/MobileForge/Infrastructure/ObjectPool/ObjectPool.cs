using System;
using System.Collections.Generic;

namespace MobileForge.Infrastructure
{
    /// <summary>
    /// Generic object pool. Replaces ad-hoc ListItemPool/GridCellPool patterns.
    /// Supports factory, acquire/release callbacks, prewarm, and max capacity.
    /// </summary>
    public class ObjectPool<T> where T : class
    {
        private readonly Func<T> _factory;
        private readonly Action<T> _onAcquire;
        private readonly Action<T> _onRelease;
        private readonly Stack<T> _pool = new();
        private readonly int _maxCapacity;

        public int PoolSize => _pool.Count;

        /// <param name="factory">Creates a new item when the pool is empty.</param>
        /// <param name="onAcquire">Called when an item is acquired.</param>
        /// <param name="onRelease">Called when an item is released back to pool.</param>
        /// <param name="maxCapacity">Max pooled items (0 = unlimited).</param>
        public ObjectPool(Func<T> factory, Action<T> onAcquire = null, Action<T> onRelease = null, int maxCapacity = 0)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _onAcquire = onAcquire;
            _onRelease = onRelease;
            _maxCapacity = maxCapacity;
        }

        public T Acquire()
        {
            T item = _pool.Count > 0 ? _pool.Pop() : _factory.Invoke();
            _onAcquire?.Invoke(item);
            return item;
        }

        public void Release(T item)
        {
            if (item == null) return;
            if (_maxCapacity > 0 && _pool.Count >= _maxCapacity) return;
            _onRelease?.Invoke(item);
            _pool.Push(item);
        }

        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (_maxCapacity > 0 && _pool.Count >= _maxCapacity) break;
                var item = _factory.Invoke();
                _onRelease?.Invoke(item);
                _pool.Push(item);
            }
        }

        public void Clear() => _pool.Clear();
    }
}
