using System;
using System.Collections.Generic;

namespace MobileForge.Presentation
{
    /// <summary>
    /// Generic object pool for grid cells. Identical in behavior to ListItemPool
    /// but semantically distinct for grid usage.
    /// </summary>
    public class GridCellPool<T> where T : class
    {
        private readonly Func<T> _factory;
        private readonly Action<T> _onAcquire;
        private readonly Action<T> _onRelease;
        private readonly Stack<T> _pool = new();

        /// <summary>
        /// Number of cells currently available in the pool (not in use).
        /// </summary>
        public int PoolSize => _pool.Count;

        /// <summary>
        /// Create a pool with a factory for new cells and optional acquire/release callbacks.
        /// </summary>
        public GridCellPool(Func<T> factory, Action<T> onAcquire = null, Action<T> onRelease = null)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _onAcquire = onAcquire;
            _onRelease = onRelease;
        }

        /// <summary>
        /// Get a cell from the pool, or create a new one if the pool is empty.
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
        /// Return a cell to the pool for reuse.
        /// </summary>
        public void Release(T item)
        {
            if (item == null) return;
            _onRelease?.Invoke(item);
            _pool.Push(item);
        }

        /// <summary>
        /// Pre-warm the pool with a number of cells.
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
        /// Clear the pool, discarding all pooled cells.
        /// </summary>
        public void Clear()
        {
            _pool.Clear();
        }
    }
}
