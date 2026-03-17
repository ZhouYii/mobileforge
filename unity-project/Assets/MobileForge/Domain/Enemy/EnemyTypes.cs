using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Mutable enemy state during dungeon combat.
    /// </summary>
    public class EnemyState
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Element { get; set; }
        public int Hp { get; set; }
        public int MaxHp { get; set; }
        public int Atk { get; set; }
        public int Defense { get; set; }
        public int Countdown { get; set; }
        public int MaxCountdown { get; set; }
        public string Behavior { get; set; }
        public bool HasUsedBuff { get; set; }
        public int AttackCount { get; set; }
        public List<Dictionary<string, object>> StatusEffects { get; set; }

        public bool IsAlive => Hp > 0;

        public EnemyState(Dictionary<string, object> data)
        {
            Id = GetInt(data, "id", 0);
            Name = GetString(data, "name", "");
            Element = GetInt(data, "element", 0);
            Hp = GetInt(data, "hp", 1);
            MaxHp = GetInt(data, "max_hp", Hp);
            Atk = GetInt(data, "atk", 0);
            Defense = GetInt(data, "defense", 0);
            Countdown = GetInt(data, "countdown", 1);
            MaxCountdown = GetInt(data, "max_countdown", Countdown);
            Behavior = GetString(data, "behavior", "normal");
            HasUsedBuff = false;
            AttackCount = 0;
            StatusEffects = new List<Dictionary<string, object>>();
            if (data.TryGetValue("status_effects", out var seObj) && seObj is List<object> seList)
            {
                foreach (var se in seList)
                {
                    if (se is Dictionary<string, object> seDict)
                        StatusEffects.Add(seDict);
                }
            }
        }

        /// <summary>
        /// Apply damage to this enemy. Returns actual damage dealt (clamped to remaining HP).
        /// </summary>
        public int TakeDamage(int amount)
        {
            int actual = Math.Min(amount, Hp);
            Hp -= actual;
            return actual;
        }

        /// <summary>
        /// Heal this enemy. HP is clamped to MaxHp.
        /// </summary>
        public void Heal(int amount)
        {
            Hp = Math.Min(Hp + amount, MaxHp);
        }

        /// <summary>
        /// Check if enemy has a status effect of the given type.
        /// </summary>
        public bool HasStatus(string statusType)
        {
            foreach (var se in StatusEffects)
            {
                if (se.TryGetValue("type", out var typeObj) && Convert.ToString(typeObj) == statusType)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Add a status effect. Replaces existing status of the same type.
        /// </summary>
        public void AddStatus(string statusType, int turns = -1, Dictionary<string, object> extra = null)
        {
            // Remove existing status of same type first
            RemoveStatus(statusType);

            var status = new Dictionary<string, object>
            {
                ["type"] = statusType,
                ["turns"] = turns,
            };
            if (extra != null)
            {
                foreach (var kv in extra)
                    status[kv.Key] = kv.Value;
            }
            StatusEffects.Add(status);
        }

        /// <summary>
        /// Remove a status effect by type.
        /// </summary>
        public void RemoveStatus(string statusType)
        {
            for (int i = StatusEffects.Count - 1; i >= 0; i--)
            {
                if (StatusEffects[i].TryGetValue("type", out var typeObj)
                    && Convert.ToString(typeObj) == statusType)
                {
                    StatusEffects.RemoveAt(i);
                    return;
                }
            }
        }

        private static int GetInt(Dictionary<string, object> data, string key, int defaultValue)
        {
            return data.TryGetValue(key, out var val) ? Convert.ToInt32(val) : defaultValue;
        }

        private static string GetString(Dictionary<string, object> data, string key, string defaultValue)
        {
            return data.TryGetValue(key, out var val) ? Convert.ToString(val) : defaultValue;
        }
    }

    /// <summary>
    /// An action that an enemy will perform.
    /// </summary>
    public class EnemyAction
    {
        public string Type { get; set; }
        public int Damage { get; set; }
        public string Target { get; set; }
        public Dictionary<string, object> Extra { get; set; }

        public EnemyAction(string type = "attack", int damage = 0, string target = "team", Dictionary<string, object> extra = null)
        {
            Type = type;
            Damage = damage;
            Target = target;
            Extra = extra ?? new Dictionary<string, object>();
        }
    }
}
