using System;

namespace MobileForge.Domain
{
    /// <summary>
    /// Configuration for a board instance. Immutable after creation.
    /// </summary>
    public class BoardConfig
    {
        public int Rows { get; }
        public int Cols { get; }
        public int[] Elements { get; }
        public int MinMatch { get; }
        public bool AllowDiagonal { get; }

        public BoardConfig(int rows = 5, int cols = 6, int[] elements = null, int minMatch = 3)
        {
            Rows = rows;
            Cols = cols;
            Elements = elements ?? new[] { 1, 2, 3, 4, 5, 6 };
            MinMatch = minMatch;
            AllowDiagonal = false;
        }

        public int TotalCells => Rows * Cols;

        public int PosToRow(int pos) => pos / Cols;

        public int PosToCol(int pos) => pos % Cols;

        public int RcToPos(int row, int col) => row * Cols + col;

        public bool IsValidPos(int pos) => pos >= 0 && pos < TotalCells;

        public bool IsValidRc(int row, int col) => row >= 0 && row < Rows && col >= 0 && col < Cols;

        public bool AreAdjacent(int a, int b)
        {
            int r1 = PosToRow(a), c1 = PosToCol(a);
            int r2 = PosToRow(b), c2 = PosToCol(b);
            return Math.Abs(r1 - r2) + Math.Abs(c1 - c2) == 1;
        }
    }
}
