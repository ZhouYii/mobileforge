using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Applies and manages gem statuses. Pure functions.
    /// Handles the 14 status types from ToS + exclusion groups.
    /// </summary>
    public static class GemModifier
    {
        /// <summary>
        /// Exclusion group: statuses that block movement cannot coexist.
        /// </summary>
        private static readonly GemStatus[] MovementBlock = { GemStatus.Frozen, GemStatus.Petrified };

        /// <summary>
        /// Exclusion group: statuses that modify element cannot coexist.
        /// </summary>
        private static readonly GemStatus[] ElementModify = { GemStatus.Enchanted, GemStatus.Transmuted };

        private static readonly GemStatus[][] ExclusionGroups = { MovementBlock, ElementModify };

        /// <summary>
        /// Apply a status to a gem, respecting exclusion rules.
        /// Returns true if the status was applied.
        /// </summary>
        public static bool ApplyStatus(GemState gem, GemStatus statusType, int turns = -1, object data = null)
        {
            if (gem == null)
                return false;

            // Shielded gems absorb one modification
            if (gem.HasStatus(GemStatus.Shielded) && statusType != GemStatus.Shielded)
            {
                gem.RemoveStatus(GemStatus.Shielded);
                return false;
            }

            // Check exclusion groups
            foreach (var group in ExclusionGroups)
            {
                if (Contains(group, statusType))
                {
                    foreach (var existingType in group)
                    {
                        if (existingType != statusType)
                            gem.RemoveStatus(existingType);
                    }
                }
            }

            gem.AddStatus(statusType, turns, data);
            return true;
        }

        /// <summary>
        /// Tick all statuses on a gem (decrement turns). Remove expired ones.
        /// Returns list of expired status types.
        /// </summary>
        public static List<GemStatus> TickStatuses(GemState gem)
        {
            if (gem == null)
                return new List<GemStatus>();

            var expired = new List<GemStatus>();
            var toRemove = new List<GemStatus>();

            for (int i = gem.Statuses.Count - 1; i >= 0; i--)
            {
                var s = gem.Statuses[i];
                if (s.Turns > 0)
                {
                    s.Turns--;
                    if (s.Turns == 0)
                    {
                        expired.Add(s.Type);
                        toRemove.Add(s.Type);
                    }
                }
            }

            foreach (var t in toRemove)
                gem.RemoveStatus(t);

            return expired;
        }

        /// <summary>
        /// Check if a gem can be moved (not frozen or petrified).
        /// </summary>
        public static bool CanMove(GemState gem)
        {
            if (gem == null)
                return false;
            return !gem.HasStatus(GemStatus.Frozen) && !gem.HasStatus(GemStatus.Petrified);
        }

        /// <summary>
        /// Check if a gem can be matched (not locked or petrified).
        /// </summary>
        public static bool CanMatch(GemState gem)
        {
            if (gem == null)
                return false;
            return !gem.HasStatus(GemStatus.Locked) && !gem.HasStatus(GemStatus.Petrified);
        }

        private static bool Contains(GemStatus[] array, GemStatus value)
        {
            foreach (var item in array)
            {
                if (item == value)
                    return true;
            }
            return false;
        }
    }
}
