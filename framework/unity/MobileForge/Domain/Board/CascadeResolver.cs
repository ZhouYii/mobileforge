using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Resolves the full cascade after gems are moved.
    /// Loop: detect matches -> process statuses -> remove -> gravity -> spawn -> repeat.
    /// Returns List of CascadeStep — complete history for animation.
    ///
    /// Gem status handling:
    /// - Chained: requires multiple matches to remove (reduces chain count)
    /// - Marked: tracked for bonus damage multiplier
    /// - Weathered: ticked each cascade step; destroyed if timer expires
    /// - Burning: tracked for per-turn damage (processed externally)
    /// </summary>
    public static class CascadeResolver
    {
        public static List<CascadeStep> Resolve(BoardLogic board)
        {
            var steps = new List<CascadeStep>();
            int stepIndex = 0;

            while (true)
            {
                // 1. Detect matches
                var matches = MatchDetector.FindMatches(board);
                if (matches.Count == 0)
                    break;

                // 2. Collect all matched positions
                var removedSet = new HashSet<int>();
                foreach (var matchResult in matches)
                {
                    foreach (int pos in matchResult.Positions)
                        removedSet.Add(pos);
                }

                // 3. Process gem statuses for matched gems
                var chainedPositions = new HashSet<int>();
                foreach (int pos in removedSet)
                {
                    var gem = board.GetGem(pos);
                    if (gem == null) continue;

                    // Chained gems: reduce chain counter, don't remove if still chained
                    if (gem.HasStatus(GemStatus.Chained))
                    {
                        var chainEntry = gem.Statuses.Find(s => s.Type == GemStatus.Chained);
                        if (chainEntry != null && chainEntry.Turns > 1)
                        {
                            chainEntry.Turns--;
                            chainedPositions.Add(pos); // Keep this gem
                        }
                        else
                        {
                            gem.RemoveStatus(GemStatus.Chained); // Freed!
                        }
                    }
                }

                // Remove chained positions from the removal set
                foreach (int pos in chainedPositions)
                    removedSet.Remove(pos);

                var removedPositions = new List<int>(removedSet);
                removedPositions.Sort();

                // 4. Remove matched gems (except still-chained ones)
                foreach (int pos in removedPositions)
                    board.SetGem(pos, null);

                // 5. Gravity: drop gems down to fill gaps (column by column, bottom to top)
                var drops = new List<GemDrop>();
                var config = board.Config;
                for (int col = 0; col < config.Cols; col++)
                {
                    int writeRow = config.Rows - 1;
                    for (int readRow = config.Rows - 1; readRow >= 0; readRow--)
                    {
                        int pos = config.RcToPos(readRow, col);
                        var gem = board.GetGem(pos);
                        if (gem != null)
                        {
                            int targetPos = config.RcToPos(writeRow, col);
                            if (pos != targetPos)
                            {
                                drops.Add(new GemDrop { From = pos, To = targetPos });
                                board.SetGem(targetPos, gem);
                                board.SetGem(pos, null);
                            }
                            writeRow--;
                        }
                    }
                }

                // 6. Spawn new gems in empty top slots
                var spawned = new List<GemSpawn>();
                for (int col = 0; col < config.Cols; col++)
                {
                    for (int row = 0; row < config.Rows; row++)
                    {
                        int pos = config.RcToPos(row, col);
                        if (board.GetGem(pos) == null)
                        {
                            int element = config.Elements[board.Rng.Next(config.Elements.Length)];
                            var gem = new GemState(element, pos);
                            board.SetGem(pos, gem);
                            spawned.Add(new GemSpawn { Position = pos, ElementId = element });
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                // 7. Record this cascade step
                var step = new CascadeStep
                {
                    Matches = matches,
                    RemovedPositions = removedPositions,
                    Drops = drops,
                    Spawned = spawned,
                    StepIndex = stepIndex,
                };

                // Set combo_index on each match
                int comboOffset = 0;
                if (steps.Count > 0)
                {
                    var prevStep = steps[steps.Count - 1];
                    comboOffset = prevStep.Matches[prevStep.Matches.Count - 1].ComboIndex + prevStep.Matches.Count;
                }
                for (int i = 0; i < matches.Count; i++)
                {
                    matches[i].ComboIndex = comboOffset + i;
                }

                steps.Add(step);
                stepIndex++;
            }

            return steps;
        }

        /// <summary>
        /// Tick gem statuses on the entire board after a turn.
        /// Returns a GemStatusTickResult with information about expired/triggered statuses.
        /// Call this once per turn after cascade resolution.
        /// </summary>
        public static GemStatusTickResult TickBoardStatuses(BoardLogic board)
        {
            var result = new GemStatusTickResult();
            var config = board.Config;

            for (int pos = 0; pos < config.TotalCells; pos++)
            {
                var gem = board.GetGem(pos);
                if (gem == null) continue;

                // Burning gems deal damage each turn
                if (gem.HasStatus(GemStatus.Burning))
                {
                    var entry = gem.Statuses.Find(s => s.Type == GemStatus.Burning);
                    int burnDamage = entry?.Data is int dmg ? dmg : 100;
                    result.BurnDamage += burnDamage;
                    result.BurningPositions.Add(pos);
                }

                // Tick all statuses (decrement turns, remove expired)
                var expired = GemModifier.TickStatuses(gem);

                // Weathered gems that expire are destroyed
                if (expired.Contains(GemStatus.Weathered))
                {
                    board.SetGem(pos, null);
                    result.WeatheredDestroyed.Add(pos);
                }
            }

            return result;
        }

        /// <summary>
        /// Count Marked gems in a set of positions (for bonus damage calculation).
        /// </summary>
        public static int CountMarkedGems(BoardLogic board, List<int> positions)
        {
            int count = 0;
            foreach (int pos in positions)
            {
                var gem = board.GetGem(pos);
                if (gem != null && gem.HasStatus(GemStatus.Marked))
                    count++;
            }
            return count;
        }
    }

    /// <summary>
    /// Result of ticking gem statuses on the board after a turn.
    /// </summary>
    public class GemStatusTickResult
    {
        public int BurnDamage { get; set; }
        public List<int> BurningPositions { get; set; } = new();
        public List<int> WeatheredDestroyed { get; set; } = new();
    }
}
