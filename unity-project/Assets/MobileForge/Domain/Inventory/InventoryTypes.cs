using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>Item type constants.</summary>
    public static class ItemType
    {
        public const string Material = "material";
        public const string Equipment = "equipment";
        public const string Consumable = "consumable";
        public const string KeyItem = "key_item";
    }

    /// <summary>
    /// Item definition — type, max stack, tags, extra data.
    /// </summary>
    public class ItemDef
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } = ItemType.Material;
        public int MaxStack { get; set; } = 9999;
        public List<string> Tags { get; set; } = new();
        public Dictionary<string, object> Data { get; set; } = new();
    }

    /// <summary>
    /// Runtime inventory stack — count and optional instance data for unique items.
    /// </summary>
    public class InventoryStack
    {
        public int Count { get; set; }
        public Dictionary<string, object> InstanceData { get; set; }
    }
}
