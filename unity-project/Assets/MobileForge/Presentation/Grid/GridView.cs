using System;
using System.Collections.Generic;

namespace MobileForge.Presentation
{
    /// <summary>
    /// Recycling 2D grid data model. Manages a pool of visible cells
    /// and binds them to data as the visible range changes.
    /// Pure C# — actual scrolling and rendering are engine-specific.
    /// </summary>
    public class GridView<T> where T : class
    {
        private Func<T> _cellFactory;
        private Action<T, object, int> _bindCallback;
        private int _columns;
        private float _cellSize;

        private List<object> _data = new();
        private readonly GridCellPool<T> _pool;
        private readonly Dictionary<int, T> _visibleCells = new();

        private int _lastFirstIndex = -1;
        private int _lastLastIndex = -1;

        /// <summary>
        /// Total number of data items.
        /// </summary>
        public int DataCount => _data.Count;

        /// <summary>
        /// Number of rows needed to display all data.
        /// </summary>
        public int RowCount => _columns > 0 ? (int)Math.Ceiling((double)_data.Count / _columns) : 0;

        /// <summary>
        /// Total content height based on row count and cell size.
        /// </summary>
        public float TotalHeight => RowCount * _cellSize;

        /// <summary>
        /// Number of currently visible (active) cells.
        /// </summary>
        public int VisibleCount => _visibleCells.Count;

        /// <summary>
        /// Configured column count.
        /// </summary>
        public int Columns => _columns;

        public GridView()
        {
            _pool = new GridCellPool<T>(() => _cellFactory.Invoke());
        }

        /// <summary>
        /// Configure the grid with a factory, bind callback, column count, and cell size.
        /// </summary>
        /// <param name="cellFactory">Creates a new grid cell view.</param>
        /// <param name="bindCallback">Called to bind data to a cell: (cell, data, index).</param>
        /// <param name="columns">Number of columns in the grid.</param>
        /// <param name="cellSize">Height (and width) of each cell. Cells are assumed square for layout.</param>
        public void Setup(Func<T> cellFactory, Action<T, object, int> bindCallback, int columns, float cellSize)
        {
            _cellFactory = cellFactory ?? throw new ArgumentNullException(nameof(cellFactory));
            _bindCallback = bindCallback ?? throw new ArgumentNullException(nameof(bindCallback));
            _columns = columns > 0 ? columns : throw new ArgumentException("Columns must be positive.");
            _cellSize = cellSize > 0 ? cellSize : throw new ArgumentException("Cell size must be positive.");
        }

        /// <summary>
        /// Set the data source. Resets visible cells.
        /// </summary>
        public void SetData(List<object> data)
        {
            _data = data ?? new List<object>();
            ReleaseAll();
            _lastFirstIndex = -1;
            _lastLastIndex = -1;
        }

        /// <summary>
        /// Calculate the visible data index range for a given scroll offset and viewport height.
        /// Returns (firstVisibleIndex, lastVisibleIndex) inclusive, accounting for grid columns.
        /// </summary>
        public (int firstIndex, int lastIndex) GetVisibleRange(float scrollOffset, float viewportHeight)
        {
            if (_data.Count == 0 || _cellSize <= 0f || _columns <= 0)
                return (0, -1);

            int firstRow = Math.Max(0, (int)(scrollOffset / _cellSize));
            int lastRow = (int)((scrollOffset + viewportHeight) / _cellSize);

            int firstIndex = firstRow * _columns;
            int lastIndex = Math.Min(_data.Count - 1, (lastRow + 1) * _columns - 1);

            if (firstIndex >= _data.Count)
                return (0, -1);

            return (firstIndex, lastIndex);
        }

        /// <summary>
        /// Update the visible cells based on scroll position. Releases cells that scrolled
        /// out of view, acquires and binds cells that scrolled into view.
        /// </summary>
        public void UpdateVisible(float scrollOffset, float viewportHeight)
        {
            var (firstIndex, lastIndex) = GetVisibleRange(scrollOffset, viewportHeight);

            // Skip if range hasn't changed
            if (firstIndex == _lastFirstIndex && lastIndex == _lastLastIndex)
                return;

            // Release cells that are no longer visible
            var toRelease = new List<int>();
            foreach (var kvp in _visibleCells)
            {
                if (kvp.Key < firstIndex || kvp.Key > lastIndex)
                    toRelease.Add(kvp.Key);
            }

            foreach (var idx in toRelease)
            {
                _pool.Release(_visibleCells[idx]);
                _visibleCells.Remove(idx);
            }

            // Acquire and bind cells that are newly visible
            for (int i = firstIndex; i <= lastIndex; i++)
            {
                if (!_visibleCells.ContainsKey(i))
                {
                    var cell = _pool.Acquire();
                    _visibleCells[i] = cell;
                    _bindCallback.Invoke(cell, _data[i], i);
                }
            }

            _lastFirstIndex = firstIndex;
            _lastLastIndex = lastIndex;
        }

        /// <summary>
        /// Get the grid position (row, col) for a data index.
        /// </summary>
        public (int row, int col) GetGridPosition(int dataIndex)
        {
            if (_columns <= 0) return (0, 0);
            return (dataIndex / _columns, dataIndex % _columns);
        }

        /// <summary>
        /// Get the visible cell at a data index, or null if not currently visible.
        /// </summary>
        public T GetVisibleCell(int dataIndex)
        {
            return _visibleCells.TryGetValue(dataIndex, out var cell) ? cell : null;
        }

        /// <summary>
        /// Release all visible cells back to the pool.
        /// </summary>
        public void ReleaseAll()
        {
            foreach (var kvp in _visibleCells)
                _pool.Release(kvp.Value);

            _visibleCells.Clear();
        }

        /// <summary>
        /// Clear everything — data, visible cells, and the pool.
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
