using System;
using System.Collections.Generic;
using System.Linq;

namespace MobileForge.Domain
{
    /// <summary>
    /// Manages equipment enhancement, substat rolling, stat calculation,
    /// and set bonus resolution. Pure C# — no Unity dependencies.
    /// </summary>
    public class EquipmentManager
    {
        private static readonly string[] SubstatPool =
            { "atk", "hp", "def", "spd", "crit_rate", "crit_dmg" };

        private readonly Dictionary<string, EquipmentDef> _defs = new();
        private readonly Dictionary<string, SetBonusDef> _setDefs = new();
        private readonly Func<float> _random;

        // ── Events ──

        /// <summary>Fires after enhancement with the instance and number of levels gained.</summary>
        public event Action<EquipmentInstance, int> EquipmentEnhanced;

        /// <summary>Fires when equipment is placed in a slot.</summary>
        public event Action<string, string> EquipmentEquipped;

        /// <summary>Fires when a set bonus becomes active.</summary>
        public event Action<string> SetBonusActivated;

        // ── Constructor ──

        public EquipmentManager(Func<float> randomProvider = null)
        {
            var rng = new Random();
            _random = randomProvider ?? (() => (float)rng.NextDouble());
        }

        // ── Definition Loading ──

        public void LoadDefs(IEnumerable<EquipmentDef> defs)
        {
            foreach (var def in defs)
                _defs[def.Id] = def;
        }

        public void LoadSetDefs(IEnumerable<SetBonusDef> setDefs)
        {
            foreach (var sd in setDefs)
                _setDefs[sd.Id] = sd;
        }

        // ── Enhancement ──

        /// <summary>
        /// Add EXP to equipment. Levels up as needed, rolling substats on
        /// levels divisible by 5. Caps at MaxLevel.
        /// </summary>
        public void Enhance(EquipmentInstance instance, int expAmount)
        {
            if (expAmount <= 0) return;
            if (!_defs.TryGetValue(instance.DefId, out var def)) return;
            if (instance.Level >= def.MaxLevel) return;

            int startLevel = instance.Level;
            instance.Exp += expAmount;

            while (instance.Level < def.MaxLevel)
            {
                int needed = ExpForLevel(instance.Level + 1);
                if (instance.Exp < needed) break;

                instance.Exp -= needed;
                instance.Level++;

                if (instance.Level % 5 == 0 && def.SubstatSlots > 0)
                    RollSubstat(instance);
            }

            // Cap at max level — discard overflow EXP
            if (instance.Level >= def.MaxLevel)
            {
                instance.Level = def.MaxLevel;
                instance.Exp = 0;
            }

            int levelsGained = instance.Level - startLevel;
            if (levelsGained > 0)
                EquipmentEnhanced?.Invoke(instance, levelsGained);
        }

        /// <summary>EXP required to advance FROM the given level TO it (threshold).</summary>
        public static int ExpForLevel(int level)
        {
            // Simple equipment curve: level * level * 100
            return level * level * 100;
        }

        // ── Substat Rolling ──

        /// <summary>
        /// Roll a new substat (if slots available) or upgrade an existing one.
        /// </summary>
        public void RollSubstat(EquipmentInstance instance)
        {
            if (!_defs.TryGetValue(instance.DefId, out var def)) return;

            float roll = _random();

            if (instance.Substats.Count < def.SubstatSlots)
            {
                // Pick a new stat not already present, if possible
                var available = SubstatPool
                    .Where(s => instance.Substats.All(ss => ss.StatId != s))
                    .ToArray();
                if (available.Length == 0) available = SubstatPool;

                int idx = (int)(roll * available.Length);
                if (idx >= available.Length) idx = available.Length - 1;
                string statId = available[idx];

                float value = GenerateSubstatValue(statId, roll);
                instance.Substats.Add(new SubstatEntry
                {
                    StatId = statId,
                    Value = value,
                    RollCount = 1,
                });
            }
            else
            {
                // Upgrade a random existing substat
                int idx = (int)(roll * instance.Substats.Count);
                if (idx >= instance.Substats.Count) idx = instance.Substats.Count - 1;

                var entry = instance.Substats[idx];
                float addedValue = GenerateSubstatValue(entry.StatId, roll);
                entry.Value += addedValue;
                entry.RollCount++;
            }
        }

        private static float GenerateSubstatValue(string statId, float roll)
        {
            // Flat values vary by stat type
            return statId switch
            {
                "hp" => 50f + roll * 200f,
                "atk" => 5f + roll * 20f,
                "def" => 5f + roll * 20f,
                "spd" => 1f + roll * 5f,
                "crit_rate" => 0.01f + roll * 0.05f,
                "crit_dmg" => 0.02f + roll * 0.10f,
                _ => 1f + roll * 10f,
            };
        }

        // ── Stat Calculation ──

        /// <summary>
        /// Calculate total stats for a single equipment instance: interpolated
        /// base→max stats plus all substat values.
        /// </summary>
        public Dictionary<string, float> CalculateEquipmentStats(EquipmentInstance instance)
        {
            var result = new Dictionary<string, float>();

            if (!_defs.TryGetValue(instance.DefId, out var def)) return result;

            float t = def.MaxLevel > 1
                ? (float)(instance.Level - 1) / (def.MaxLevel - 1)
                : 0f;

            foreach (var kv in def.BaseStats)
            {
                float baseVal = kv.Value;
                float maxVal = def.MaxStats.TryGetValue(kv.Key, out var mv) ? mv : baseVal;
                result[kv.Key] = baseVal + (maxVal - baseVal) * t;
            }

            // Add substats
            foreach (var ss in instance.Substats)
            {
                if (result.ContainsKey(ss.StatId))
                    result[ss.StatId] += ss.Value;
                else
                    result[ss.StatId] = ss.Value;
            }

            return result;
        }

        /// <summary>Sum stats across all equipped items.</summary>
        public Dictionary<string, float> CalculateAllEquippedStats(IEnumerable<EquipmentInstance> equipped)
        {
            var totals = new Dictionary<string, float>();

            foreach (var inst in equipped)
            {
                var stats = CalculateEquipmentStats(inst);
                foreach (var kv in stats)
                {
                    if (totals.ContainsKey(kv.Key))
                        totals[kv.Key] += kv.Value;
                    else
                        totals[kv.Key] = kv.Value;
                }
            }

            return totals;
        }

        // ── Set Bonuses ──

        /// <summary>
        /// Determine which set bonuses are active given equipped items.
        /// </summary>
        public List<SetBonusDef> GetActiveSetBonuses(IEnumerable<EquipmentInstance> equipped)
        {
            var setCounts = new Dictionary<string, int>();

            foreach (var inst in equipped)
            {
                if (!_defs.TryGetValue(inst.DefId, out var def)) continue;
                foreach (var setId in def.SetIds)
                {
                    if (!setCounts.ContainsKey(setId))
                        setCounts[setId] = 0;
                    setCounts[setId]++;
                }
            }

            var active = new List<SetBonusDef>();
            foreach (var kv in setCounts)
            {
                if (_setDefs.TryGetValue(kv.Key, out var sd) && kv.Value >= sd.RequiredPieces)
                    active.Add(sd);
            }

            return active;
        }

        // ── Comparison ──

        /// <summary>
        /// Compare two equipment instances. Positive values mean the candidate is better.
        /// </summary>
        public Dictionary<string, float> CompareStats(EquipmentInstance current, EquipmentInstance candidate)
        {
            var currentStats = CalculateEquipmentStats(current);
            var candidateStats = CalculateEquipmentStats(candidate);

            var diff = new Dictionary<string, float>();

            // All keys from both sides
            var allKeys = new HashSet<string>(currentStats.Keys);
            allKeys.UnionWith(candidateStats.Keys);

            foreach (var key in allKeys)
            {
                float cur = currentStats.TryGetValue(key, out var c) ? c : 0f;
                float cand = candidateStats.TryGetValue(key, out var ca) ? ca : 0f;
                diff[key] = cand - cur;
            }

            return diff;
        }

        // ── Queries ──

        /// <summary>Remaining EXP to reach the next level.</summary>
        public int GetExpToNextLevel(EquipmentInstance instance)
        {
            if (!_defs.TryGetValue(instance.DefId, out var def)) return 0;
            if (instance.Level >= def.MaxLevel) return 0;

            int needed = ExpForLevel(instance.Level + 1);
            return needed - instance.Exp;
        }

        /// <summary>Whether the instance has reached its definition's max level.</summary>
        public bool IsMaxLevel(EquipmentInstance instance)
        {
            if (!_defs.TryGetValue(instance.DefId, out var def)) return false;
            return instance.Level >= def.MaxLevel;
        }
    }
}
