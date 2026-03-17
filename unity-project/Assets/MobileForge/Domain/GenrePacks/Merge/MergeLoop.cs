using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Grid-based merge game loop.
    /// Players place items on a grid and merge matching adjacent items to level them up.
    /// </summary>
    public class MergeLoop : IGameLoop
    {
        private Dictionary<string, object>[,] _grid;
        private int _rows, _cols;
        private bool _isActive;
        private int _mergeCount;
        private int _highestLevel;
        private readonly Action<string, Dictionary<string, object>> _emitEvent;

        public bool IsActive => _isActive;

        public MergeLoop(Action<string, Dictionary<string, object>> emitEvent = null)
        {
            _emitEvent = emitEvent;
        }

        public void Start(Dictionary<string, object> config)
        {
            _rows = config.TryGetValue("rows", out var r) ? Convert.ToInt32(r) : 5;
            _cols = config.TryGetValue("cols", out var c) ? Convert.ToInt32(c) : 5;
            _grid = new Dictionary<string, object>[_rows, _cols];
            _isActive = true;
            _mergeCount = 0;
            _highestLevel = 0;
            _emitEvent?.Invoke("merge_started", new Dictionary<string, object>
            {
                ["rows"] = _rows, ["cols"] = _cols,
            });
        }

        public PhaseResult ProcessInput(Dictionary<string, object> input)
        {
            var action = input.TryGetValue("action", out var a) ? a as string ?? "" : "";
            var result = new PhaseResult { PhaseName = action, Completed = true };

            switch (action)
            {
                case "place":
                    int pr = input.TryGetValue("row", out var prv) ? Convert.ToInt32(prv) : -1;
                    int pc = input.TryGetValue("col", out var pcv) ? Convert.ToInt32(pcv) : -1;
                    var itemId = input.TryGetValue("item_id", out var iid) ? iid as string ?? "" : "";
                    result.Data = PlaceItem(pr, pc, itemId);
                    break;
                case "merge":
                    int r1 = input.TryGetValue("row", out var r1v) ? Convert.ToInt32(r1v) : -1;
                    int c1 = input.TryGetValue("col", out var c1v) ? Convert.ToInt32(c1v) : -1;
                    int r2 = input.TryGetValue("target_row", out var r2v) ? Convert.ToInt32(r2v) : -1;
                    int c2 = input.TryGetValue("target_col", out var c2v) ? Convert.ToInt32(c2v) : -1;
                    result.Data = MergeItems(r1, c1, r2, c2);
                    break;
                default:
                    result.Completed = false;
                    break;
            }
            return result;
        }

        public PhaseResult Tick(float delta) => new() { PhaseName = "idle", Completed = false };

        public GameLoopState GetState()
        {
            return new GameLoopState
            {
                Phase = _isActive ? "playing" : "inactive",
                IsActive = _isActive,
                Custom = new Dictionary<string, object>
                {
                    ["merge_count"] = _mergeCount, ["highest_level"] = _highestLevel,
                },
            };
        }

        private Dictionary<string, object> PlaceItem(int row, int col, string itemId)
        {
            if (!InBounds(row, col))
                return new Dictionary<string, object> { ["success"] = false, ["error"] = "out_of_bounds" };
            if (_grid[row, col] != null)
                return new Dictionary<string, object> { ["success"] = false, ["error"] = "cell_occupied" };
            _grid[row, col] = new Dictionary<string, object> { ["item_id"] = itemId, ["level"] = 1 };
            return new Dictionary<string, object> { ["success"] = true };
        }

        private Dictionary<string, object> MergeItems(int r1, int c1, int r2, int c2)
        {
            if (!InBounds(r1, c1) || !InBounds(r2, c2))
                return new Dictionary<string, object> { ["success"] = false, ["error"] = "out_of_bounds" };
            var a = _grid[r1, c1];
            var b = _grid[r2, c2];
            if (a == null || b == null)
                return new Dictionary<string, object> { ["success"] = false, ["error"] = "empty_cell" };

            var aId = a["item_id"] as string;
            var bId = b["item_id"] as string;
            int aLvl = Convert.ToInt32(a["level"]);
            int bLvl = Convert.ToInt32(b["level"]);
            if (aId != bId || aLvl != bLvl)
                return new Dictionary<string, object> { ["success"] = false, ["error"] = "not_mergeable" };

            int newLevel = aLvl + 1;
            _grid[r2, c2] = new Dictionary<string, object> { ["item_id"] = aId, ["level"] = newLevel };
            _grid[r1, c1] = null;
            _mergeCount++;
            if (newLevel > _highestLevel) _highestLevel = newLevel;

            _emitEvent?.Invoke("items_merged", new Dictionary<string, object>
            {
                ["item_id"] = aId, ["new_level"] = newLevel,
            });
            return new Dictionary<string, object> { ["success"] = true, ["new_level"] = newLevel };
        }

        private bool InBounds(int row, int col) => row >= 0 && row < _rows && col >= 0 && col < _cols;
    }
}
