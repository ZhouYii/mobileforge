using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Core board state manager. Owns the grid array.
    /// Pure logic — no engine dependencies.
    ///
    /// The grid is a flat GemState[] of size rows*cols.
    /// Position = row * cols + col (row-major, origin top-left).
    /// </summary>
    public class BoardLogic
    {
        public BoardConfig Config { get; }

        private GemState[] _grid;
        internal Random Rng { get; }

        public BoardLogic(BoardConfig config, int seed = -1)
        {
            Config = config;
            Rng = seed >= 0 ? new Random(seed) : new Random();
            _grid = new GemState[config.TotalCells];
        }

        /// <summary>
        /// Initialize board with random gems, ensuring no initial matches.
        /// </summary>
        public void InitBoard()
        {
            for (int i = 0; i < Config.TotalCells; i++)
            {
                _grid[i] = SpawnGemNoMatch(i);
            }
        }

        /// <summary>
        /// Get gem at position (null if empty or invalid).
        /// </summary>
        public GemState GetGem(int pos)
        {
            if (!Config.IsValidPos(pos))
                return null;
            return _grid[pos];
        }

        /// <summary>
        /// Set gem at position.
        /// </summary>
        public void SetGem(int pos, GemState gem)
        {
            if (Config.IsValidPos(pos))
            {
                _grid[pos] = gem;
                if (gem != null)
                    gem.Position = pos;
            }
        }

        /// <summary>
        /// Swap two gems by position. Returns true if valid swap.
        /// Checks for frozen/petrified gems that cannot be moved.
        /// </summary>
        public bool SwapGems(int posA, int posB)
        {
            if (!Config.IsValidPos(posA) || !Config.IsValidPos(posB))
                return false;

            var gemA = _grid[posA];
            var gemB = _grid[posB];

            if (gemA != null && (gemA.HasStatus(GemStatus.Frozen) || gemA.HasStatus(GemStatus.Petrified)))
                return false;
            if (gemB != null && (gemB.HasStatus(GemStatus.Frozen) || gemB.HasStatus(GemStatus.Petrified)))
                return false;

            _grid[posA] = gemB;
            _grid[posB] = gemA;

            if (gemA != null)
                gemA.Position = posB;
            if (gemB != null)
                gemB.Position = posA;

            return true;
        }

        /// <summary>
        /// Move gem along a path (ToS drag mechanic — gem swaps with each cell along the path).
        /// </summary>
        public void MoveGemPath(int startPos, List<int> path)
        {
            int current = startPos;
            foreach (int nextPos in path)
            {
                SwapGems(current, nextPos);
                current = nextPos;
            }
        }

        /// <summary>
        /// Change the element of a gem at a position.
        /// </summary>
        public void ChangeGemElement(int pos, int newElement)
        {
            var gem = _grid[pos];
            if (gem != null)
                gem.ElementId = newElement;
        }

        /// <summary>
        /// Get a snapshot of the entire grid (deep copy).
        /// </summary>
        public GemState[] Snapshot()
        {
            var result = new GemState[_grid.Length];
            for (int i = 0; i < _grid.Length; i++)
            {
                result[i] = _grid[i]?.Duplicate();
            }
            return result;
        }

        /// <summary>
        /// Get flat grid as array of element ints (for quick comparison/testing).
        /// </summary>
        public int[] ToElementArray()
        {
            var result = new int[_grid.Length];
            for (int i = 0; i < _grid.Length; i++)
            {
                result[i] = _grid[i]?.ElementId ?? 0;
            }
            return result;
        }

        /// <summary>
        /// Set board from an array of element ints (for testing).
        /// </summary>
        public void FromElementArray(int[] elements)
        {
            int count = Math.Min(elements.Length, Config.TotalCells);
            for (int i = 0; i < count; i++)
            {
                if (elements[i] == 0)
                {
                    _grid[i] = null;
                }
                else
                {
                    _grid[i] = new GemState(elements[i], i);
                }
            }
        }

        /// <summary>
        /// Spawn a gem that won't create an immediate match at the given position.
        /// </summary>
        internal GemState SpawnGemNoMatch(int pos)
        {
            var available = new List<int>(Config.Elements);
            int row = Config.PosToRow(pos);
            int col = Config.PosToCol(pos);

            // Check left 2
            if (col >= 2)
            {
                var e1 = _grid[pos - 1];
                var e2 = _grid[pos - 2];
                if (e1 != null && e2 != null && e1.ElementId == e2.ElementId)
                    available.Remove(e1.ElementId);
            }

            // Check above 2
            if (row >= 2)
            {
                var e1 = _grid[pos - Config.Cols];
                var e2 = _grid[pos - 2 * Config.Cols];
                if (e1 != null && e2 != null && e1.ElementId == e2.ElementId)
                    available.Remove(e1.ElementId);
            }

            if (available.Count == 0)
                available = new List<int>(Config.Elements);

            int element = available[Rng.Next(available.Count)];
            return new GemState(element, pos);
        }
    }
}
