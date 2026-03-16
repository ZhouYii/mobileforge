using System;
using System.Collections.Generic;

namespace MobileForge.Presentation
{
    /// <summary>
    /// Generic object pool for list items. Reduces allocation pressure
    /// when scrolling through long lists.
    /// </summary>
    public class ListItemPool<T> where T : class
    {
        private readonly Func<T> _factory;
        private readonly Action<T> _onAcquire;
        private readonly Action<T> _onRelease;
        private readonly Stack<T> _pool = new();

        /// <summary>
        /// Number of items currently available in the pool (not in use).
        /// </summary>
        public int PoolSize => _pool.Count;

        /// <summary>
        /// Create a pool with a factory for new items and optional acquire/release callbacks.
        /// </summary>
        /// <param name="factory">Creates a new item when the pool is empty.</param>
        /// <param name="onAcquire">Called when an item is acquired (e.g., enable visuals).</param>
        /// <param name="onRelease">Called when an item is released (e.g., disable visuals).</param>
        public ListItemPool(Func<T> factory, Action<T> onAcquire = null, Action<T> onRelease = null)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _onAcquire = onAcquire;
            _onRelease = onRelease;
        }

        /// <summary>
        /// Get an item from the pool, or create a new one if the pool is empty.
        /// </summary>
        public T Acquire()
        {
            T item;
            if (_pool.Count > 0)
            {
                item = _pool.Pop();
            }
            else
            {
                item = _factory.Invoke();
            }

            _onAcquire?.Invoke(item);
            return item;
        }

        /// <summary>
        /// Return an item to the pool for reuse.
        /// </summary>
        public void Release(T item)
        {
            if (item == null) return;
            _onRelease?.Invoke(item);
            _pool.Push(item);
        }

        /// <summary>
        /// Pre-warm the pool with a number of items.
        /// </summary>
        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var item = _factory.Invoke();
                _onRelease?.Invoke(item);
                _pool.Push(item);
            }
        }

        /// <summary>
        /// Clear the pool, discarding all pooled items.
        /// </summary>
        public void Clear()
        {
            _pool.Clear();
        }
    }
}
