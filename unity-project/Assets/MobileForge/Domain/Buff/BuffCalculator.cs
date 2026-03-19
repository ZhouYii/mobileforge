using System.Collections.Generic;
using System.Linq;

namespace MobileForge.Domain
{
    /// <summary>
    /// Static helpers for buff aggregation, immunity checks, and stack conflict resolution.
    /// </summary>
    public static class BuffCalculator
    {
        /// <summary>
        /// Returns the combined multiplicative stat modifier across all buff instances.
        /// Each stack of each buff multiplies independently. Returns 1.0 when no modifiers apply.
        /// </summary>
        public static float AggregateStatModifiers(
            IEnumerable<BuffInstance> buffs, IEnumerable<BuffDef> defs, string stat)
        {
            var defLookup = defs.ToDictionary(d => d.Id);
            float product = 1f;

            foreach (var inst in buffs)
            {
                if (defLookup.TryGetValue(inst.DefId, out var def) &&
                    def.StatModifiers.TryGetValue(stat, out float mod))
                {
                    for (int i = 0; i < inst.Stacks; i++)
                        product *= mod;
                }
            }
            return product;
        }

        /// <summary>
        /// Returns the combined additive flat modifier across all buff instances.
        /// Each stack of each buff adds independently. Returns 0 when no modifiers apply.
        /// </summary>
        public static float AggregateFlatModifiers(
            IEnumerable<BuffInstance> buffs, IEnumerable<BuffDef> defs, string stat)
        {
            var defLookup = defs.ToDictionary(d => d.Id);
            float sum = 0f;

            foreach (var inst in buffs)
            {
                if (defLookup.TryGetValue(inst.DefId, out var def) &&
                    def.FlatModifiers.TryGetValue(stat, out float mod))
                {
                    sum += mod * inst.Stacks;
                }
            }
            return sum;
        }

        /// <summary>
        /// Check if any active buff grants immunity to the incoming buff's category.
        /// A buff grants immunity if it has a "grants_immunity" param containing the category string.
        /// </summary>
        public static bool CheckImmunity(
            IEnumerable<BuffInstance> activeBuffs, IEnumerable<BuffDef> defs, BuffDef incomingBuff)
        {
            var defLookup = defs.ToDictionary(d => d.Id);

            foreach (var inst in activeBuffs)
            {
                if (defLookup.TryGetValue(inst.DefId, out var def) &&
                    def.Params.TryGetValue("grants_immunity", out var immunityObj))
                {
                    if (immunityObj is string immunityCategory &&
                        immunityCategory == incomingBuff.Category)
                    {
                        return true;
                    }

                    if (immunityObj is List<object> immunityList &&
                        immunityList.Any(c => c?.ToString() == incomingBuff.Category))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Resolve a stack conflict between an existing buff instance and an incoming buff
        /// based on the definition's StackRule.
        /// Returns true if the new buff should win (replace/override), false if existing stays.
        /// </summary>
        public static bool ResolveStackConflict(BuffInstance existing, BuffDef def, float newValue)
        {
            switch (def.StackRule)
            {
                case BuffStackRule.Replace:
                    return true;

                case BuffStackRule.Extend:
                    return false; // Existing stays but gains duration — handled by manager

                case BuffStackRule.Stack:
                    return existing.Stacks < def.MaxStacks;

                case BuffStackRule.Highest:
                    return newValue > existing.Value;

                case BuffStackRule.Refresh:
                    return false; // Existing stays but duration resets — handled by manager

                default:
                    return true;
            }
        }
    }
}
