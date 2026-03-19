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

        /// <summary>
        /// Roll a pull guaranteed to be at least a minimum rarity.
        /// Used by step-up banners and guaranteed-rarity mechanics.
        /// </summary>
        public static GachaResult RollMinRarity(GachaPool pool, int minRarity, Random rng)
        {
            var eligible = new List<GachaEntry>();
            int eligibleWeight = 0;
            foreach (var entry in pool.Entries)
            {
                if (entry.Rarity >= minRarity)
                {
                    eligible.Add(entry);
                    eligibleWeight += entry.Weight;
                }
            }

            if (eligible.Count == 0)
                return RollTopRarity(pool, rng, false); // fallback to top rarity

            int rollValue = rng.Next(eligibleWeight);
            int cumulative = 0;
            foreach (var entry in eligible)
            {
                cumulative += entry.Weight;
                if (rollValue < cumulative)
                    return new GachaResult(entry.MonsterId, entry.Rarity, false, entry.IsFeatured);
            }

            var last = eligible[eligible.Count - 1];
            return new GachaResult(last.MonsterId, last.Rarity, false, last.IsFeatured);
        }

        /// <summary>
        /// Roll a pull guaranteed to be a featured monster.
        /// Used by step-up final steps.
        /// </summary>
        public static GachaResult RollFeatured(GachaPool pool, Random rng)
        {
            var featured = new List<GachaEntry>();
            int featuredWeight = 0;
            foreach (var entry in pool.Entries)
            {
                if (entry.IsFeatured)
                {
                    featured.Add(entry);
                    featuredWeight += entry.Weight;
                }
            }

            if (featured.Count == 0)
                return Roll(pool, 0, rng); // fallback to normal roll

            int rollValue = rng.Next(featuredWeight);
            int cumulative = 0;
            foreach (var entry in featured)
            {
                cumulative += entry.Weight;
                if (rollValue < cumulative)
                    return new GachaResult(entry.MonsterId, entry.Rarity, false, true);
            }

            var last = featured[featured.Count - 1];
            return new GachaResult(last.MonsterId, last.Rarity, false, true);
        }

        /// <summary>
        /// Roll multi with a guaranteed minimum rarity on the last pull.
        /// Common pattern: "10-pull with guaranteed 5★ on last".
        /// </summary>
        public static List<GachaResult> RollMultiWithGuarantee(GachaPool pool, int count,
            int pityCount, int guaranteedMinRarity, Random rng)
        {
            var results = new List<GachaResult>();
            int currentPity = pityCount;

            for (int i = 0; i < count; i++)
            {
                GachaResult result;
                if (i == count - 1 && guaranteedMinRarity > 0)
                {
                    // Check if we already got the guaranteed rarity
                    bool alreadyGot = false;
                    foreach (var r in results)
                    {
                        if (r.Rarity >= guaranteedMinRarity)
                        {
                            alreadyGot = true;
                            break;
                        }
                    }
                    result = alreadyGot
                        ? Roll(pool, currentPity, rng)
                        : RollMinRarity(pool, guaranteedMinRarity, rng);
                }
                else
                {
                    result = Roll(pool, currentPity, rng);
                }

                results.Add(result);
                if (result.IsPity || IsTopRarity(pool, result.Rarity))
                    currentPity = 0;
                else
                    currentPity += 1;
            }
            return results;
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
