using System;
using System.Collections.Generic;
using System.Linq;

namespace MobileForge.Domain
{
    /// <summary>
    /// Generic inventory system supporting stackable items and unique equipment.
    /// Pure C# — no MonoBehaviour dependency.
    /// </summary>
    public class Inventory
    {
        private readonly Dictionary<string, InventoryStack> _stacks = new();
        private readonly Dictionary<string, string> _equipped = new(); // slotType -> itemId
        private readonly Dictionary<string, ItemDef> _itemDefs = new();
        private int _maxSlots; // 0 = unlimited

        /// <summary>Fired when item count changes. Args: itemId, oldCount, newCount.</summary>
        public event Action<string, int, int> OnInventoryChanged;
        /// <summary>Fired when an item is equipped. Args: slotType, itemId.</summary>
        public event Action<string, string> OnItemEquipped;
        /// <summary>Fired when an item is unequipped. Args: slotType, itemId.</summary>
        public event Action<string, string> OnItemUnequipped;

        /// <summary>Set max inventory slots (0 = unlimited).</summary>
        public void SetMaxSlots(int maxSlots) => _maxSlots = maxSlots;

        /// <summary>Register item definitions for type/tag lookups.</summary>
        public void RegisterItemDefs(IEnumerable<ItemDef> defs)
        {
            foreach (var def in defs)
                _itemDefs[def.Id] = def;
        }

        /// <summary>Add items. Returns actual amount added.</summary>
        public int AddItem(string id, int count = 1, Dictionary<string, object> instanceData = null)
        {
            if (count <= 0) return 0;
            if (_maxSlots > 0 && !_stacks.ContainsKey(id) && _stacks.Count >= _maxSlots) return 0;

            int oldCount = _stacks.TryGetValue(id, out var existing) ? existing.Count : 0;
            int maxStack = GetMaxStack(id);
            int newCount = Math.Min(oldCount + count, maxStack);
            int added = newCount - oldCount;
            if (added <= 0) return 0;

            if (existing != null)
            {
                existing.Count = newCount;
                if (instanceData != null && instanceData.Count > 0)
                    existing.InstanceData = instanceData;
            }
            else
            {
                _stacks[id] = new InventoryStack
                {
                    Count = newCount,
                    InstanceData = instanceData != null && instanceData.Count > 0 ? instanceData : null,
                };
            }

            OnInventoryChanged?.Invoke(id, oldCount, newCount);
            return added;
        }

        /// <summary>Remove items. Returns actual amount removed.</summary>
        public int RemoveItem(string id, int count = 1)
        {
            if (!_stacks.TryGetValue(id, out var stack) || count <= 0) return 0;
            int oldCount = stack.Count;
            int removed = Math.Min(count, oldCount);
            int newCount = oldCount - removed;

            if (newCount <= 0)
                _stacks.Remove(id);
            else
                stack.Count = newCount;

            OnInventoryChanged?.Invoke(id, oldCount, newCount);
            return removed;
        }

        /// <summary>Check if inventory has at least count of item.</summary>
        public bool HasItem(string id, int count = 1) =>
            _stacks.TryGetValue(id, out var s) && s.Count >= count;

        /// <summary>Get count of an item (0 if absent).</summary>
        public int GetCount(string id) =>
            _stacks.TryGetValue(id, out var s) ? s.Count : 0;

        /// <summary>Get a stack entry, or null if absent.</summary>
        public InventoryStack GetStack(string id) =>
            _stacks.TryGetValue(id, out var s) ? s : null;

        /// <summary>Get all stacks.</summary>
        public Dictionary<string, InventoryStack> GetAllStacks() => new(_stacks);

        /// <summary>Get item IDs filtered by type.</summary>
        public List<string> GetByType(string type) =>
            _stacks.Keys.Where(id => _itemDefs.TryGetValue(id, out var def) && def.Type == type).ToList();

        /// <summary>Get item IDs filtered by tag.</summary>
        public List<string> GetByTag(string tag) =>
            _stacks.Keys.Where(id => _itemDefs.TryGetValue(id, out var def) && def.Tags.Contains(tag)).ToList();

        /// <summary>Whether inventory is full (only meaningful when max slots > 0).</summary>
        public bool IsFull => _maxSlots > 0 && _stacks.Count >= _maxSlots;

        /// <summary>Number of occupied slots.</summary>
        public int SlotCount => _stacks.Count;

        // ── Equipment ──

        /// <summary>Equip an item. Returns previously equipped item ID or null.</summary>
        public string Equip(string slotType, string itemId)
        {
            if (!_stacks.ContainsKey(itemId)) return null;
            _equipped.TryGetValue(slotType, out var prev);
            _equipped[slotType] = itemId;
            if (prev != null) OnItemUnequipped?.Invoke(slotType, prev);
            OnItemEquipped?.Invoke(slotType, itemId);
            return prev;
        }

        /// <summary>Unequip from a slot. Returns item ID or null.</summary>
        public string Unequip(string slotType)
        {
            if (!_equipped.Remove(slotType, out var itemId)) return null;
            OnItemUnequipped?.Invoke(slotType, itemId);
            return itemId;
        }

        /// <summary>Get equipped item ID for a slot, or null.</summary>
        public string GetEquipped(string slotType) =>
            _equipped.TryGetValue(slotType, out var id) ? id : null;

        /// <summary>Get all equipped slots.</summary>
        public Dictionary<string, string> GetAllEquipped() => new(_equipped);

        // ── Persistence ──

        public Dictionary<string, object> ToSaveDict()
        {
            var stacksData = new Dictionary<string, object>();
            foreach (var kv in _stacks)
            {
                stacksData[kv.Key] = new Dictionary<string, object>
                {
                    ["count"] = kv.Value.Count,
                    ["instance_data"] = kv.Value.InstanceData,
                };
            }
            return new Dictionary<string, object>
            {
                ["stacks"] = stacksData,
                ["equipped"] = new Dictionary<string, object>(_equipped.Select(kv =>
                    new KeyValuePair<string, object>(kv.Key, kv.Value))),
            };
        }

        public void FromSaveDict(Dictionary<string, object> data)
        {
            _stacks.Clear();
            _equipped.Clear();

            if (data.TryGetValue("stacks", out var sObj) && sObj is Dictionary<string, object> sData)
            {
                foreach (var kv in sData)
                {
                    if (kv.Value is Dictionary<string, object> entry)
                    {
                        _stacks[kv.Key] = new InventoryStack
                        {
                            Count = Convert.ToInt32(entry.GetValueOrDefault("count", 0)),
                            InstanceData = entry.GetValueOrDefault("instance_data") as Dictionary<string, object>,
                        };
                    }
                }
            }

            if (data.TryGetValue("equipped", out var eObj) && eObj is Dictionary<string, object> eData)
            {
                foreach (var kv in eData)
                    _equipped[kv.Key] = kv.Value?.ToString();
            }
        }

        private int GetMaxStack(string id) =>
            _itemDefs.TryGetValue(id, out var def) ? def.MaxStack : 9999;
    }
}
