using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Immutable monster definition loaded from JSON.
    /// </summary>
    public class MonsterDef
    {
        public int Id { get; }
        public string Name { get; }
        public int Element { get; }
        public int Rarity { get; }
        public int MaxLevel { get; }
        public float BaseHp { get; }
        public float BaseAtk { get; }
        public float BaseRec { get; }
        public float MaxHp { get; }
        public float MaxAtk { get; }
        public float MaxRec { get; }
        public int Cost { get; }
        public int ActiveSkillId { get; }
        public int LeaderSkillId { get; }
        public int[] TeamSkillIds { get; }
        public int EvolveTo { get; }
        public int[] EvolveMaterials { get; }
        public string ExpCurve { get; }

        public MonsterDef(Dictionary<string, object> data)
        {
            Id = GetInt(data, "id", 0);
            Name = GetString(data, "name", "");
            Element = GetInt(data, "element", 0);
            Rarity = GetInt(data, "rarity", 1);
            MaxLevel = GetInt(data, "max_level", 1);
            BaseHp = GetFloat(data, "base_hp", 0f);
            BaseAtk = GetFloat(data, "base_atk", 0f);
            BaseRec = GetFloat(data, "base_rec", 0f);
            MaxHp = GetFloat(data, "max_hp", 0f);
            MaxAtk = GetFloat(data, "max_atk", 0f);
            MaxRec = GetFloat(data, "max_rec", 0f);
            Cost = GetInt(data, "cost", 1);
            ActiveSkillId = GetInt(data, "active_skill_id", -1);
            LeaderSkillId = GetInt(data, "leader_skill_id", -1);
            TeamSkillIds = GetIntArray(data, "team_skill_ids");
            EvolveTo = GetInt(data, "evolve_to", -1);
            EvolveMaterials = GetIntArray(data, "evolve_materials");
            ExpCurve = GetString(data, "exp_curve", "standard");
        }

        private static int GetInt(Dictionary<string, object> data, string key, int defaultValue)
        {
            return data.TryGetValue(key, out var val) ? Convert.ToInt32(val) : defaultValue;
        }

        private static float GetFloat(Dictionary<string, object> data, string key, float defaultValue)
        {
            return data.TryGetValue(key, out var val) ? Convert.ToSingle(val) : defaultValue;
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
    /// Mutable player-owned monster instance.
    /// </summary>
    public class MonsterInstance
    {
        public int InstanceId { get; }
        public int DefId { get; set; }  // Mutable for in-place evolution
        public int Level { get; set; }
        public int Exp { get; set; }
        public int SkillLevel { get; set; }
        public int PlusHp { get; set; }
        public int PlusAtk { get; set; }
        public int PlusRec { get; set; }
        public bool IsFavorite { get; set; }

        public MonsterInstance(int instanceId, int defId)
        {
            InstanceId = instanceId;
            DefId = defId;
            Level = 1;
            Exp = 0;
            SkillLevel = 1;
            PlusHp = 0;
            PlusAtk = 0;
            PlusRec = 0;
            IsFavorite = false;
        }

        public Dictionary<string, object> ToDict()
        {
            return new Dictionary<string, object>
            {
                ["instance_id"] = InstanceId,
                ["def_id"] = DefId,
                ["level"] = Level,
                ["exp"] = Exp,
                ["skill_level"] = SkillLevel,
                ["plus_hp"] = PlusHp,
                ["plus_atk"] = PlusAtk,
                ["plus_rec"] = PlusRec,
                ["is_favorite"] = IsFavorite,
            };
        }

        public static MonsterInstance FromDict(Dictionary<string, object> data)
        {
            var inst = new MonsterInstance(
                Convert.ToInt32(data.GetValueOrDefault("instance_id", 0)),
                Convert.ToInt32(data.GetValueOrDefault("def_id", 0))
            );
            inst.Level = Convert.ToInt32(data.GetValueOrDefault("level", 1));
            inst.Exp = Convert.ToInt32(data.GetValueOrDefault("exp", 0));
            inst.SkillLevel = Convert.ToInt32(data.GetValueOrDefault("skill_level", 1));
            inst.PlusHp = Convert.ToInt32(data.GetValueOrDefault("plus_hp", 0));
            inst.PlusAtk = Convert.ToInt32(data.GetValueOrDefault("plus_atk", 0));
            inst.PlusRec = Convert.ToInt32(data.GetValueOrDefault("plus_rec", 0));
            inst.IsFavorite = Convert.ToBoolean(data.GetValueOrDefault("is_favorite", false));
            return inst;
        }
    }

    /// <summary>
    /// Calculated stats at a specific level.
    /// </summary>
    public class MonsterStats
    {
        public int Hp { get; }
        public int Atk { get; }
        public int Rec { get; }

        public MonsterStats(int hp, int atk, int rec)
        {
            Hp = hp;
            Atk = atk;
            Rec = rec;
        }
    }
}
