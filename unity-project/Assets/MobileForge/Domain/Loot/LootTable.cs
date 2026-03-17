using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Pure function loot roller.
    /// </summary>
    public static class LootTable
    {
        public static List<LootDrop> RollDrops(LootTableDef tableDef, Random rng, int rollCount = 1)
        {
            var drops = new List<LootDrop>();

            // Always include guaranteed drops
            foreach (var entry in tableDef.Entries)
            {
                if (entry.Guaranteed)
                {
                    int count = rng.Next(entry.CountMin, entry.CountMax + 1);
                    drops.Add(new LootDrop(entry.Type, entry.ItemId, count));
                }
            }

            // Roll for non-guaranteed drops
            var nonGuaranteed = new List<LootEntry>();
            int totalWeight = 0;
            foreach (var entry in tableDef.Entries)
            {
                if (!entry.Guaranteed)
                {
                    nonGuaranteed.Add(entry);
                    totalWeight += entry.Weight;
                }
            }

            if (totalWeight > 0)
            {
                for (int i = 0; i < rollCount; i++)
                {
                    int rollValue = rng.Next(totalWeight);
                    int cumulative = 0;
                    foreach (var entry in nonGuaranteed)
                    {
                        cumulative += entry.Weight;
                        if (rollValue < cumulative)
                        {
                            int count = rng.Next(entry.CountMin, entry.CountMax + 1);
                            drops.Add(new LootDrop(entry.Type, entry.ItemId, count));
                            break;
                        }
                    }
                }
            }

            return drops;
        }
    }
}
