using System.Collections.Generic;
using MobileForge.Domain;

namespace TowerOfSaviors
{
    /// <summary>
    /// Board state view model for rendering.
    /// Reads from BoardLogic and provides display-ready data.
    /// </summary>
    public class BoardViewModel
    {
        private readonly BoardLogic _board;

        public int Rows => _board?.Config?.Rows ?? 0;
        public int Cols => _board?.Config?.Cols ?? 0;
        public int TotalCells => Rows * Cols;

        /// <summary>
        /// Current drag state for rendering the dragged gem.
        /// </summary>
        public bool IsDragging { get; set; }
        public int DragSourcePosition { get; set; } = -1;
        public float DragX { get; set; }
        public float DragY { get; set; }
        public int DragElement { get; set; }

        /// <summary>
        /// Current cascade animation step index (-1 if not animating).
        /// </summary>
        public int CascadeAnimationStep { get; set; } = -1;
        public bool IsAnimating => CascadeAnimationStep >= 0;

        public BoardViewModel(BoardLogic board)
        {
            _board = board;
        }

        /// <summary>
        /// Get the element array for rendering the grid.
        /// Returns a flat int[] of element IDs.
        /// </summary>
        public int[] GetElements()
        {
            return _board?.ToElementArray() ?? System.Array.Empty<int>();
        }

        /// <summary>
        /// Get the element at a specific position.
        /// </summary>
        public int GetElementAt(int position)
        {
            var gem = _board?.GetGem(position);
            return gem?.ElementId ?? 0;
        }

        /// <summary>
        /// Convert grid position to row/col for layout.
        /// </summary>
        public (int row, int col) PositionToRowCol(int position)
        {
            int cols = Cols;
            if (cols <= 0) return (0, 0);
            return (position / cols, position % cols);
        }

        /// <summary>
        /// Check if a position is part of the current cascade match highlight.
        /// </summary>
        public bool IsHighlighted(int position, List<CascadeStep> steps)
        {
            if (CascadeAnimationStep < 0 || steps == null || CascadeAnimationStep >= steps.Count)
                return false;
            return steps[CascadeAnimationStep].RemovedPositions.Contains(position);
        }
    }
}
