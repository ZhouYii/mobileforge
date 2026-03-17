using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Resolves the full cascade after gems are moved.
    /// Loop: detect matches -> remove -> gravity drop -> spawn -> repeat until no matches.
    /// Returns List of CascadeStep — complete history for animation.
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
                var removedPositions = new List<int>(removedSet);
                removedPositions.Sort();

                // 3. Remove matched gems
                foreach (int pos in removedPositions)
                    board.SetGem(pos, null);

                // 4. Gravity: drop gems down to fill gaps (column by column, bottom to top)
                var drops = new List<GemDrop>();
                var config = board.Config;
                for (int col = 0; col < config.Cols; col++)
                {
                    int writeRow = config.Rows - 1; // Bottom of column
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

                // 5. Spawn new gems in empty top slots
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
                            break; // No more empty above a filled cell
                        }
                    }
                }

                // 6. Record this cascade step
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
    }
}
