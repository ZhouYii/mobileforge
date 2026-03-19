using System.Collections.Generic;
using System.Linq;

namespace MobileForge.Domain
{
    /// <summary>Equipment slot type constants.</summary>
    public static class EquipSlot
    {
        public const string Weapon = "weapon";
        public const string Armor = "armor";
        public const string Accessory = "accessory";
        public const string CraftEssence = "craft_essence";
        public const string Topping1 = "topping_1";
        public const string Topping2 = "topping_2";
        public const string Topping3 = "topping_3";
    }

    /// <summary>
    /// Equipment definition — slot, rarity, stat curves, set membership.
    /// </summary>
    public class EquipmentDef
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string SlotType { get; set; }
        public int Rarity { get; set; }
        public int MaxLevel { get; set; } = 20;
        public Dictionary<string, float> BaseStats { get; set; } = new();
        public Dictionary<string, float> MaxStats { get; set; } = new();
        public List<string> SetIds { get; set; } = new();
        public int SubstatSlots { get; set; } = 0;
        public string ExpCurve { get; set; } = "standard";
    }

    /// <summary>
    /// Runtime equipment instance — mutable level, EXP, substats.
    /// </summary>
    public class EquipmentInstance
    {
        public string InstanceId { get; set; }
        public string DefId { get; set; }
        public int Level { get; set; } = 1;
        public int Exp { get; set; }
        public List<SubstatEntry> Substats { get; set; } = new();
        public bool IsLocked { get; set; }

        // ── Persistence ──

        public Dictionary<string, object> ToSaveDict()
        {
            var substatsData = Substats.Select(s => (object)new Dictionary<string, object>
            {
                ["stat_id"] = s.StatId,
                ["value"] = s.Value,
                ["roll_count"] = s.RollCount,
            }).ToList();

            return new Dictionary<string, object>
            {
                ["instance_id"] = InstanceId,
                ["def_id"] = DefId,
                ["level"] = Level,
                ["exp"] = Exp,
                ["substats"] = substatsData,
                ["is_locked"] = IsLocked,
            };
        }

        public static EquipmentInstance FromSaveDict(Dictionary<string, object> data)
        {
            var instance = new EquipmentInstance
            {
                InstanceId = (string)data["instance_id"],
                DefId = (string)data["def_id"],
                Level = System.Convert.ToInt32(data["level"]),
                Exp = System.Convert.ToInt32(data["exp"]),
                IsLocked = System.Convert.ToBoolean(data["is_locked"]),
            };

            if (data.TryGetValue("substats", out var ssObj) && ssObj is List<object> ssList)
            {
                foreach (var entry in ssList)
                {
                    if (entry is Dictionary<string, object> ssData)
                    {
                        instance.Substats.Add(new SubstatEntry
                        {
                            StatId = (string)ssData["stat_id"],
                            Value = System.Convert.ToSingle(ssData["value"]),
                            RollCount = System.Convert.ToInt32(ssData["roll_count"]),
                        });
                    }
                }
            }

            return instance;
        }
    }

    /// <summary>A single substat line on equipment.</summary>
    public class SubstatEntry
    {
        public string StatId { get; set; }
        public float Value { get; set; }
        public int RollCount { get; set; } = 1;
    }

    /// <summary>Set bonus definition — pieces required and bonus stats.</summary>
    public class SetBonusDef
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int RequiredPieces { get; set; }
        public Dictionary<string, float> BonusStats { get; set; } = new();
    }
}
