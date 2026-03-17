using System;
using System.Collections.Generic;

namespace MobileForge.Presentation
{
    /// <summary>
    /// Recycling 1D scroll list data model. Manages a pool of visible items
    /// and binds them to data as the visible range changes.
    /// Pure C# — actual scrolling and rendering are engine-specific.
    /// </summary>
    public class VirtualList<T> where T : class
    {
        private Func<T> _itemFactory;
        private Action<T, object, int> _bindCallback;
        private float _itemHeight;

        private List<object> _data = new();
        private readonly ListItemPool<T> _pool;
        private readonly Dictionary<int, T> _visibleItems = new();

        private int _lastFirstIndex = -1;
        private int _lastLastIndex = -1;

        /// <summary>
        /// Total number of data items.
        /// </summary>
        public int DataCount => _data.Count;

        /// <summary>
        /// Total content height based on data count and item height.
        /// </summary>
        public float TotalHeight => _data.Count * _itemHeight;

        /// <summary>
        /// Number of currently visible (active) items.
        /// </summary>
        public int VisibleCount => _visibleItems.Count;

        public VirtualList()
        {
            _pool = new ListItemPool<T>(() => _itemFactory.Invoke());
        }

        /// <summary>
        /// Configure the list with a factory, bind callback, and item height.
        /// </summary>
        /// <param name="itemFactory">Creates a new list item view.</param>
        /// <param name="bindCallback">Called to bind data to an item: (item, data, index).</param>
        /// <param name="itemHeight">Height of each item in the list.</param>
        public void Setup(Func<T> itemFactory, Action<T, object, int> bindCallback, float itemHeight)
        {
            _itemFactory = itemFactory ?? throw new ArgumentNullException(nameof(itemFactory));
            _bindCallback = bindCallback ?? throw new ArgumentNullException(nameof(bindCallback));
            _itemHeight = itemHeight > 0 ? itemHeight : throw new ArgumentException("Item height must be positive.");
        }

        /// <summary>
        /// Set the data source. Resets visible items.
        /// </summary>
        public void SetData(List<object> data)
        {
            _data = data ?? new List<object>();
            ReleaseAll();
            _lastFirstIndex = -1;
            _lastLastIndex = -1;
        }

        /// <summary>
        /// Calculate the visible index range for a given scroll offset and viewport height.
        /// Returns (firstVisibleIndex, lastVisibleIndex) inclusive.
        /// </summary>
        public (int firstIndex, int lastIndex) GetVisibleRange(float scrollOffset, float viewportHeight)
        {
            if (_data.Count == 0 || _itemHeight <= 0f)
                return (0, -1);

            int firstIndex = Math.Max(0, (int)(scrollOffset / _itemHeight));
            int lastIndex = Math.Min(_data.Count - 1, (int)((scrollOffset + viewportHeight) / _itemHeight));

            return (firstIndex, lastIndex);
        }

        /// <summary>
        /// Update the visible items based on scroll position. Releases items that scrolled
        /// out of view, acquires and binds items that scrolled into view.
        /// </summary>
        public void UpdateVisible(float scrollOffset, float viewportHeight)
        {
            var (firstIndex, lastIndex) = GetVisibleRange(scrollOffset, viewportHeight);

            // Skip if range hasn't changed
            if (firstIndex == _lastFirstIndex && lastIndex == _lastLastIndex)
                return;

            // Release items that are no longer visible
            var toRelease = new List<int>();
            foreach (var kvp in _visibleItems)
            {
                if (kvp.Key < firstIndex || kvp.Key > lastIndex)
                    toRelease.Add(kvp.Key);
            }

            foreach (var idx in toRelease)
            {
                _pool.Release(_visibleItems[idx]);
                _visibleItems.Remove(idx);
            }

            // Acquire and bind items that are newly visible
            for (int i = firstIndex; i <= lastIndex; i++)
            {
                if (!_visibleItems.ContainsKey(i))
                {
                    var item = _pool.Acquire();
                    _visibleItems[i] = item;
                    _bindCallback.Invoke(item, _data[i], i);
                }
            }

            _lastFirstIndex = firstIndex;
            _lastLastIndex = lastIndex;
        }

        /// <summary>
        /// Get the visible item at a data index, or null if not currently visible.
        /// </summary>
        public T GetVisibleItem(int dataIndex)
        {
            return _visibleItems.TryGetValue(dataIndex, out var item) ? item : null;
        }

        /// <summary>
        /// Release all visible items back to the pool.
        /// </summary>
        public void ReleaseAll()
        {
            foreach (var kvp in _visibleItems)
                _pool.Release(kvp.Value);

            _visibleItems.Clear();
        }

        /// <summary>
        /// Clear everything — data, visible items, and the pool.
        /// </summary>
        public void Clear()
        {
            ReleaseAll();
            _data.Clear();
            _pool.Clear();
            _lastFirstIndex = -1;
            _lastLastIndex = -1;
        }
    }
}
