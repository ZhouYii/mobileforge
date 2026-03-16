using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Loot table definition loaded from JSON.
    /// </summary>
    public class LootTableDef
    {
        public int Id { get; }
        public List<LootEntry> Entries { get; }

        public LootTableDef(Dictionary<string, object> data)
        {
            Id = data.TryGetValue("id", out var idVal) ? Convert.ToInt32(idVal) : 0;

            Entries = new List<LootEntry>();
            if (data.TryGetValue("entries", out var rawEntries) && rawEntries is List<object> entryList)
            {
                foreach (var e in entryList)
                {
                    if (e is Dictionary<string, object> entryDict)
                        Entries.Add(new LootEntry(entryDict));
                }
            }
        }

        public LootTableDef(int id, List<LootEntry> entries)
        {
            Id = id;
            Entries = entries ?? new List<LootEntry>();
        }
    }

    /// <summary>
    /// Single entry in a loot table.
    /// </summary>
    public class LootEntry
    {
        public string Type { get; } // "monster", "currency", "item"
        public int ItemId { get; }
        public int CountMin { get; }
        public int CountMax { get; }
        public int Weight { get; }
        public bool Guaranteed { get; } // Always drops

        public LootEntry(Dictionary<string, object> data)
        {
            Type = data.TryGetValue("type", out var t) ? Convert.ToString(t) : "item";
            ItemId = data.TryGetValue("item_id", out var iid) ? Convert.ToInt32(iid) : 0;
            CountMin = data.TryGetValue("count_min", out var cmin) ? Convert.ToInt32(cmin) : 1;
            CountMax = data.TryGetValue("count_max", out var cmax) ? Convert.ToInt32(cmax) : 1;
            Weight = data.TryGetValue("weight", out var w) ? Convert.ToInt32(w) : 100;
            Guaranteed = data.TryGetValue("guaranteed", out var g) && Convert.ToBoolean(g);
        }

        public LootEntry(string type, int itemId, int countMin, int countMax, int weight, bool guaranteed)
        {
            Type = type;
            ItemId = itemId;
            CountMin = countMin;
            CountMax = countMax;
            Weight = weight;
            Guaranteed = guaranteed;
        }
    }

    /// <summary>
    /// A single drop result from a loot table roll.
    /// </summary>
    public class LootDrop
    {
        public string Type { get; }
        public int ItemId { get; }
        public int Count { get; }

        public LootDrop(string type = "", int itemId = 0, int count = 1)
        {
            Type = type;
            ItemId = itemId;
            Count = count;
        }
    }
}
