using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Detects matches on a board. Pure functions — takes board state, returns matches.
    /// Scans rows then columns for runs of 3+ same-element gems.
    /// Overlapping matches are merged (e.g., T-shape or L-shape = one match).
    /// </summary>
    public static class MatchDetector
    {
        /// <summary>
        /// Find all matches on the board.
        /// </summary>
        public static List<MatchResult> FindMatches(BoardLogic board)
        {
            var config = board.Config;
            var horizontal = ScanHorizontal(board, config);
            var vertical = ScanVertical(board, config);
            horizontal.AddRange(vertical);
            return MergeMatches(horizontal);
        }

        /// <summary>
        /// Scan rows for horizontal runs of min_match+ same element.
        /// </summary>
        private static List<MatchResult> ScanHorizontal(BoardLogic board, BoardConfig config)
        {
            var matches = new List<MatchResult>();
            for (int row = 0; row < config.Rows; row++)
            {
                int runElement = 0;
                var runPositions = new List<int>();
                for (int col = 0; col < config.Cols; col++)
                {
                    int pos = config.RcToPos(row, col);
                    var gem = board.GetGem(pos);
                    if (gem == null || gem.ElementId == (int)Element.None || !GemModifier.CanMatch(gem))
                    {
                        FlushRun(runPositions, runElement, config.MinMatch, matches);
                        runPositions = new List<int>();
                        runElement = 0;
                        continue;
                    }
                    if (gem.ElementId == runElement)
                    {
                        runPositions.Add(pos);
                    }
                    else
                    {
                        FlushRun(runPositions, runElement, config.MinMatch, matches);
                        runElement = gem.ElementId;
                        runPositions = new List<int> { pos };
                    }
                }
                FlushRun(runPositions, runElement, config.MinMatch, matches);
            }
            return matches;
        }

        /// <summary>
        /// Scan columns for vertical runs.
        /// </summary>
        private static List<MatchResult> ScanVertical(BoardLogic board, BoardConfig config)
        {
            var matches = new List<MatchResult>();
            for (int col = 0; col < config.Cols; col++)
            {
                int runElement = 0;
                var runPositions = new List<int>();
                for (int row = 0; row < config.Rows; row++)
                {
                    int pos = config.RcToPos(row, col);
                    var gem = board.GetGem(pos);
                    if (gem == null || gem.ElementId == (int)Element.None || !GemModifier.CanMatch(gem))
                    {
                        FlushRun(runPositions, runElement, config.MinMatch, matches);
                        runPositions = new List<int>();
                        runElement = 0;
                        continue;
                    }
                    if (gem.ElementId == runElement)
                    {
                        runPositions.Add(pos);
                    }
                    else
                    {
                        FlushRun(runPositions, runElement, config.MinMatch, matches);
                        runElement = gem.ElementId;
                        runPositions = new List<int> { pos };
                    }
                }
                FlushRun(runPositions, runElement, config.MinMatch, matches);
            }
            return matches;
        }

        private static void FlushRun(List<int> positions, int element, int minMatch, List<MatchResult> output)
        {
            if (positions.Count >= minMatch && element != 0)
            {
                output.Add(new MatchResult(element, new List<int>(positions)));
            }
        }

        /// <summary>
        /// Merge overlapping matches of the same element into single matches.
        /// Two matches overlap if they share any position AND have the same element.
        /// Uses a union-find style approach.
        /// </summary>
        private static List<MatchResult> MergeMatches(List<MatchResult> matches)
        {
            if (matches.Count == 0)
                return new List<MatchResult>();

            var merged = new List<MatchResult>();
            var used = new bool[matches.Count];

            for (int i = 0; i < matches.Count; i++)
            {
                if (used[i])
                    continue;

                int groupElement = matches[i].ElementId;
                var groupPositions = new HashSet<int>(matches[i].Positions);

                // Find all matches that overlap with this group
                bool changed = true;
                while (changed)
                {
                    changed = false;
                    for (int j = 0; j < matches.Count; j++)
                    {
                        if (used[j] || i == j)
                            continue;
                        if (matches[j].ElementId != groupElement)
                            continue;

                        // Check if any position overlaps
                        bool overlaps = false;
                        foreach (int p in matches[j].Positions)
                        {
                            if (groupPositions.Contains(p))
                            {
                                overlaps = true;
                                break;
                            }
                        }

                        if (overlaps)
                        {
                            foreach (int p in matches[j].Positions)
                                groupPositions.Add(p);
                            used[j] = true;
                            changed = true;
                        }
                    }
                }

                used[i] = true;
                var positionsList = new List<int>(groupPositions);
                positionsList.Sort();
                merged.Add(new MatchResult(groupElement, positionsList));
            }

            return merged;
        }
    }
}
