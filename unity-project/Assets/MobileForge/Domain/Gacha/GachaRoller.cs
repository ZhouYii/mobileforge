using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Pure function gacha roller. NO state, NO deps.
    /// All randomness comes from the passed-in RNG.
    /// </summary>
    public static class GachaRoller
    {
        /// <summary>
        /// Roll a single pull from a gacha pool.
        /// </summary>
        /// <param name="pool">The gacha pool to roll from.</param>
        /// <param name="pityCount">How many pulls since last top-rarity result.</param>
        /// <param name="rng">Random number generator.</param>
        public static GachaResult Roll(GachaPool pool, int pityCount, Random rng)
        {
            // Check pity
            if (pool.PityThreshold > 0 && pityCount >= pool.PityThreshold - 1)
                return RollTopRarity(pool, rng, true);

            // Weighted random from all entries
            int totalWeight = 0;
            foreach (var entry in pool.Entries)
                totalWeight += entry.Weight;

            if (totalWeight <= 0)
                return new GachaResult();

            int rollValue = rng.Next(totalWeight);
            int cumulative = 0;
            foreach (var entry in pool.Entries)
            {
                cumulative += entry.Weight;
                if (rollValue < cumulative)
                {
                    return new GachaResult(
                        entry.MonsterId,
                        entry.Rarity,
                        false,
                        entry.IsFeatured
                    );
                }
            }

            // Fallback (should not reach)
            var last = pool.Entries[pool.Entries.Count - 1];
            return new GachaResult(last.MonsterId, last.Rarity, false, last.IsFeatured);
        }

        /// <summary>
        /// Roll multiple pulls, tracking pity across the batch.
        /// </summary>
        public static List<GachaResult> RollMulti(GachaPool pool, int count, int pityCount, Random rng)
        {
            var results = new List<GachaResult>();
            int currentPity = pityCount;
            for (int i = 0; i < count; i++)
            {
                var result = Roll(pool, currentPity, rng);
                results.Add(result);
                if (result.IsPity || IsTopRarity(pool, result.Rarity))
                    currentPity = 0;
                else
                    currentPity += 1;
            }
            return results;
        }

        /// <summary>
        /// Get displayed rates for a pool (for UI). Returns rarity -> percentage.
        /// </summary>
        public static Dictionary<int, double> GetDisplayedRates(GachaPool pool)
        {
            int totalWeight = 0;
            var rarityWeights = new Dictionary<int, int>();
            foreach (var entry in pool.Entries)
            {
                totalWeight += entry.Weight;
                if (!rarityWeights.ContainsKey(entry.Rarity))
                    rarityWeights[entry.Rarity] = 0;
                rarityWeights[entry.Rarity] += entry.Weight;
            }

            var rates = new Dictionary<int, double>();
            foreach (var kvp in rarityWeights)
                rates[kvp.Key] = (double)kvp.Value / totalWeight * 100.0;
            return rates;
        }

        private static GachaResult RollTopRarity(GachaPool pool, Random rng, bool isPity)
        {
            int maxRarity = 0;
            foreach (var entry in pool.Entries)
            {
                if (entry.Rarity > maxRarity)
                    maxRarity = entry.Rarity;
            }

            var topEntries = new List<GachaEntry>();
            int topWeight = 0;
            foreach (var entry in pool.Entries)
            {
                if (entry.Rarity == maxRarity)
                {
                    topEntries.Add(entry);
                    topWeight += entry.Weight;
                }
            }

            if (topEntries.Count == 0)
                return new GachaResult();

            int rollValue = rng.Next(topWeight);
            int cumulative = 0;
            foreach (var entry in topEntries)
            {
                cumulative += entry.Weight;
                if (rollValue < cumulative)
                    return new GachaResult(entry.MonsterId, entry.Rarity, isPity, entry.IsFeatured);
            }

            var last = topEntries[topEntries.Count - 1];
            return new GachaResult(last.MonsterId, last.Rarity, isPity, last.IsFeatured);
        }

        private static bool IsTopRarity(GachaPool pool, int rarity)
        {
            int maxRarity = 0;
            foreach (var entry in pool.Entries)
            {
                if (entry.Rarity > maxRarity)
                    maxRarity = entry.Rarity;
            }
            return rarity == maxRarity;
        }
    }
}
