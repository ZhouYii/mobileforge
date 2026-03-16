using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Gacha pool definition loaded from JSON.
    /// </summary>
    public class GachaPool
    {
        public int Id { get; }
        public string Name { get; }
        public string CostCurrency { get; }
        public int CostAmount { get; }
        public List<GachaEntry> Entries { get; }
        public int PityThreshold { get; }
        public int[] FeaturedIds { get; }

        public GachaPool(Dictionary<string, object> data)
        {
            Id = GetInt(data, "id", 0);
            Name = GetString(data, "name", "");
            CostCurrency = GetString(data, "cost_currency", "gems");
            CostAmount = GetInt(data, "cost_amount", 5);
            PityThreshold = GetInt(data, "pity_threshold", 0);
            FeaturedIds = GetIntArray(data, "featured_ids");

            Entries = new List<GachaEntry>();
            if (data.TryGetValue("entries", out var rawEntries) && rawEntries is List<object> entryList)
            {
                foreach (var e in entryList)
                {
                    if (e is Dictionary<string, object> entryDict)
                        Entries.Add(new GachaEntry(entryDict));
                }
            }
        }

        public GachaPool(int id, string name, string costCurrency, int costAmount,
            List<GachaEntry> entries, int pityThreshold, int[] featuredIds)
        {
            Id = id;
            Name = name;
            CostCurrency = costCurrency;
            CostAmount = costAmount;
            Entries = entries ?? new List<GachaEntry>();
            PityThreshold = pityThreshold;
            FeaturedIds = featuredIds ?? Array.Empty<int>();
        }

        private static int GetInt(Dictionary<string, object> data, string key, int defaultValue)
        {
            return data.TryGetValue(key, out var val) ? Convert.ToInt32(val) : defaultValue;
        }

        private static string GetString(Dictionary<string, object> data, string key, string defaultValue)
        {
            return data.TryGetValue(key, out var val) ? Convert.ToString(val) : defaultValue;
        }

        private static int[] GetIntArray(Dictionary<string, object> data, string key)
        {
            if (!data.TryGetValue(key, out var val))
                return Array.Empty<int>();

            if (val is List<object> list)
            {
                var result = new int[list.Count];
                for (int i = 0; i < list.Count; i++)
                    result[i] = Convert.ToInt32(list[i]);
                return result;
            }

            if (val is int[] intArr)
                return intArr;

            return Array.Empty<int>();
        }
    }

    /// <summary>
    /// Single entry in a gacha pool with weight for weighted random selection.
    /// </summary>
    public class GachaEntry
    {
        public int MonsterId { get; }
        public int Rarity { get; }
        public int Weight { get; }
        public bool IsFeatured { get; }

        public GachaEntry(Dictionary<string, object> data)
        {
            MonsterId = data.TryGetValue("monster_id", out var mid) ? Convert.ToInt32(mid) : 0;
            Rarity = data.TryGetValue("rarity", out var r) ? Convert.ToInt32(r) : 1;
            Weight = data.TryGetValue("weight", out var w) ? Convert.ToInt32(w) : 100;
            IsFeatured = data.TryGetValue("is_featured", out var f) && Convert.ToBoolean(f);
        }

        public GachaEntry(int monsterId, int rarity, int weight, bool isFeatured)
        {
            MonsterId = monsterId;
            Rarity = rarity;
            Weight = weight;
            IsFeatured = isFeatured;
        }
    }

    /// <summary>
    /// Result of a single gacha pull.
    /// </summary>
    public class GachaResult
    {
        public int MonsterId { get; }
        public int Rarity { get; }
        public bool IsPity { get; }
        public bool IsFeatured { get; }

        public GachaResult(int monsterId = 0, int rarity = 1, bool isPity = false, bool isFeatured = false)
        {
            MonsterId = monsterId;
            Rarity = rarity;
            IsPity = isPity;
            IsFeatured = isFeatured;
        }
    }
}
