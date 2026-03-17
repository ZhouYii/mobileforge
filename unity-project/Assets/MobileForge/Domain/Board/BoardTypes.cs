using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Element IDs matching ToS: WATER=1, FIRE=2, GRASS=3, LIGHT=4, DARK=5, HEART=6
    /// </summary>
    public enum Element
    {
        None = 0,
        Water = 1,
        Fire = 2,
        Grass = 3,
        Light = 4,
        Dark = 5,
        Heart = 6,
    }

    /// <summary>
    /// Gem status types from ToS (14 types).
    /// </summary>
    public enum GemStatus
    {
        None = 0,
        Frozen = 1,         // Cannot be moved
        Locked = 2,         // Cannot be matched (but can be moved)
        Weathered = 3,      // Breaks if not matched within N turns
        Burning = 4,        // Deals damage to player each turn
        Sticky = 5,         // Swaps with adjacent when moved
        Poisoned = 6,       // Converts to poison element on match
        Enchanted = 7,      // Counts as two elements
        Petrified = 8,      // Cannot be moved or matched
        Hidden = 9,         // Element hidden until moved
        Marked = 10,        // Extra damage when matched
        Chained = 11,       // Requires multiple matches to free
        Transmuted = 12,    // Changed element
        Shielded = 13,      // Absorbs one modification
    }

    /// <summary>
    /// Represents one gem on the board.
    /// </summary>
    public class GemState
    {
        public int ElementId;
        public int Position;
        public List<GemStatusEntry> Statuses = new();

        public GemState(int element, int position)
        {
            ElementId = element;
            Position = position;
        }

        public bool HasStatus(GemStatus type)
        {
            return Statuses.Exists(s => s.Type == type);
        }

        /// <summary>
        /// Add a status, replacing an existing status of the same type.
        /// </summary>
        public void AddStatus(GemStatus type, int turns = -1, object data = null)
        {
            for (int i = 0; i < Statuses.Count; i++)
            {
                if (Statuses[i].Type == type)
                {
                    Statuses[i] = new GemStatusEntry(type, turns, data);
                    return;
                }
            }
            Statuses.Add(new GemStatusEntry(type, turns, data));
        }

        public void RemoveStatus(GemStatus type)
        {
            for (int i = Statuses.Count - 1; i >= 0; i--)
            {
                if (Statuses[i].Type == type)
                {
                    Statuses.RemoveAt(i);
                    return;
                }
            }
        }

        public GemState Duplicate()
        {
            var copy = new GemState(ElementId, Position);
            foreach (var s in Statuses)
                copy.Statuses.Add(new GemStatusEntry(s.Type, s.Turns, s.Data));
            return copy;
        }
    }

    /// <summary>
    /// A single status entry on a gem.
    /// </summary>
    public class GemStatusEntry
    {
        public GemStatus Type;
        public int Turns;
        public object Data;

        public GemStatusEntry(GemStatus type, int turns, object data)
        {
            Type = type;
            Turns = turns;
            Data = data;
        }
    }

    /// <summary>
    /// A group of matched gems.
    /// </summary>
    public class MatchResult
    {
        public int ElementId;
        public List<int> Positions;
        public int ComboIndex;

        public int GemCount => Positions.Count;

        public MatchResult(int element, List<int> positions, int comboIndex = 0)
        {
            ElementId = element;
            Positions = positions;
            ComboIndex = comboIndex;
        }
    }

    /// <summary>
    /// One step of the cascade resolution.
    /// </summary>
    public class CascadeStep
    {
        public List<MatchResult> Matches = new();
        public List<int> RemovedPositions = new();
        public List<GemDrop> Drops = new();
        public List<GemSpawn> Spawned = new();
        public int StepIndex;
    }

    /// <summary>
    /// Records a gem dropping from one position to another during gravity.
    /// </summary>
    public class GemDrop
    {
        public int From;
        public int To;
    }

    /// <summary>
    /// Records a newly spawned gem.
    /// </summary>
    public class GemSpawn
    {
        public int Position;
        public int ElementId;
    }
}
