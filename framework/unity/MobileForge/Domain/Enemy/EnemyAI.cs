using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Static enemy AI logic: countdown ticking, action selection, status management.
    /// Supports behavior field for varied enemy patterns matching ToS enemy types:
    ///
    ///   "normal"         — always attacks with base ATK
    ///   "heavy_attack"   — 2x damage every 3rd attack
    ///   "heal_self"      — heals 20% max HP when below 30% HP
    ///   "buff_allies"    — boosts other enemies' ATK once, then attacks normally
    ///   "preemptive"     — attacks immediately on first appearance (countdown=0)
    ///   "rage"           — enters rage mode below HP threshold: +50% ATK, -1 countdown
    ///   "multi_hit"      — hits N times per attack (lower damage per hit)
    ///   "status_attack"  — applies a debuff to the player (poison/bind/skill_lock)
    ///   "shield"         — activates damage shield below HP threshold
    ///   "revive"         — revives once after death with 30% HP
    ///   "enrage_timer"   — after N turns, deals massive damage (dungeon timer pressure)
    ///   "gravity"        — reduces player HP to a fixed percentage
    ///   "absorb_element" — absorbs damage of a specific element, heals instead
    /// </summary>
    public static class EnemyAI
    {
        /// <summary>
        /// Tick countdowns for all enemies. Returns list of enemies whose countdown hit 0.
        /// </summary>
        public static List<EnemyState> TickCountdowns(List<EnemyState> enemies)
        {
            var readyEnemies = new List<EnemyState>();
            foreach (var enemy in enemies)
            {
                if (!enemy.IsAlive)
                    continue;

                enemy.Countdown -= 1;
                if (enemy.Countdown <= 0)
                    readyEnemies.Add(enemy);
            }
            return readyEnemies;
        }

        /// <summary>
        /// Decide what action an enemy will take based on its Behavior field.
        /// </summary>
        public static EnemyAction DecideAction(EnemyState enemy)
        {
            string behavior = enemy.Behavior ?? "normal";

            // Check for rage transition first (any behavior can enter rage)
            if (behavior == "rage")
                return DecideRageAction(enemy);

            switch (behavior)
            {
                case "heavy_attack":
                    return DecideHeavyAttack(enemy);
                case "heal_self":
                    return DecideHealSelf(enemy);
                case "buff_allies":
                    return DecideBuffAllies(enemy);
                case "preemptive":
                    return DecidePreemptive(enemy);
                case "multi_hit":
                    return DecideMultiHit(enemy);
                case "status_attack":
                    return DecideStatusAttack(enemy);
                case "shield":
                    return DecideShield(enemy);
                case "revive":
                    return DecideRevive(enemy);
                case "enrage_timer":
                    return DecideEnrageTimer(enemy);
                case "gravity":
                    return DecideGravity(enemy);
                case "absorb_element":
                    return new EnemyAction("attack", enemy.Atk, "team");
                default: // "normal"
                    return new EnemyAction("attack", enemy.Atk, "team");
            }
        }

        // ── Behavior Implementations ──

        private static EnemyAction DecideHeavyAttack(EnemyState enemy)
        {
            enemy.AttackCount += 1;
            if (enemy.AttackCount % 3 == 0)
                return new EnemyAction("attack", enemy.Atk * 2, "team");
            return new EnemyAction("attack", enemy.Atk, "team");
        }

        private static EnemyAction DecideHealSelf(EnemyState enemy)
        {
            float hpRatio = enemy.MaxHp > 0 ? (float)enemy.Hp / enemy.MaxHp : 1f;
            if (hpRatio < 0.3f)
            {
                int healAmount = (int)(enemy.MaxHp * 0.2);
                enemy.Heal(healAmount);
                return new EnemyAction("heal", 0, "self",
                    new Dictionary<string, object> { { "heal_amount", healAmount } });
            }
            return new EnemyAction("attack", enemy.Atk, "team");
        }

        private static EnemyAction DecideBuffAllies(EnemyState enemy)
        {
            if (!enemy.HasUsedBuff)
            {
                enemy.HasUsedBuff = true;
                return new EnemyAction("buff", 0, "allies",
                    new Dictionary<string, object> { { "atk_boost", 1.5 } });
            }
            return new EnemyAction("attack", enemy.Atk, "team");
        }

        private static EnemyAction DecidePreemptive(EnemyState enemy)
        {
            // First action is a preemptive strike, then normal attacks
            if (enemy.AttackCount == 0)
            {
                enemy.AttackCount++;
                return new EnemyAction("preemptive", enemy.Atk, "team",
                    new Dictionary<string, object> { { "message", "Preemptive Strike!" } });
            }
            enemy.AttackCount++;
            return new EnemyAction("attack", enemy.Atk, "team");
        }

        private static EnemyAction DecideRageAction(EnemyState enemy)
        {
            // Check if rage is active (below 50% HP)
            float hpRatio = enemy.MaxHp > 0 ? (float)enemy.Hp / enemy.MaxHp : 1f;
            float rageThreshold = GetStatusFloat(enemy, "rage_threshold", 0.5f);

            if (hpRatio <= rageThreshold && !enemy.HasStatus("enraged"))
            {
                // Enter rage mode: boost ATK by 50%, reduce max countdown by 1
                enemy.AddStatus("enraged", -1); // permanent until death
                enemy.Atk = (int)(enemy.Atk * 1.5f);
                if (enemy.MaxCountdown > 1) enemy.MaxCountdown--;
                return new EnemyAction("rage_activate", 0, "self",
                    new Dictionary<string, object> { { "message", "Enraged!" } });
            }

            enemy.AttackCount++;
            return new EnemyAction("attack", enemy.Atk, "team");
        }

        private static EnemyAction DecideMultiHit(EnemyState enemy)
        {
            // Hit N times with reduced damage per hit (total ≈ normal damage * 1.2)
            int hits = GetStatusInt(enemy, "hit_count", 3);
            int damagePerHit = enemy.Atk / hits;
            int totalDamage = damagePerHit * hits; // slightly different from Atk due to rounding
            enemy.AttackCount++;

            return new EnemyAction("multi_attack", totalDamage, "team",
                new Dictionary<string, object>
                {
                    { "hits", hits },
                    { "damage_per_hit", damagePerHit },
                });
        }

        private static EnemyAction DecideStatusAttack(EnemyState enemy)
        {
            // Attack AND apply a debuff. Alternates: odd turns = attack+debuff, even = just attack
            enemy.AttackCount++;

            if (enemy.AttackCount % 2 == 1)
            {
                string debuffType = GetStatusString(enemy, "debuff_type", "skill_lock");
                int debuffTurns = GetStatusInt(enemy, "debuff_turns", 2);

                return new EnemyAction("status_attack", enemy.Atk, "team",
                    new Dictionary<string, object>
                    {
                        { "debuff_type", debuffType },
                        { "debuff_turns", debuffTurns },
                    });
            }

            return new EnemyAction("attack", enemy.Atk, "team");
        }

        private static EnemyAction DecideShield(EnemyState enemy)
        {
            float hpRatio = enemy.MaxHp > 0 ? (float)enemy.Hp / enemy.MaxHp : 1f;

            if (hpRatio < 0.5f && !enemy.HasStatus("shielded"))
            {
                int shieldAmount = enemy.MaxHp / 2;
                enemy.AddStatus("shielded", 3, new Dictionary<string, object>
                {
                    { "absorb_remaining", shieldAmount },
                });
                return new EnemyAction("shield", 0, "self",
                    new Dictionary<string, object>
                    {
                        { "shield_amount", shieldAmount },
                        { "message", "Shield activated!" },
                    });
            }

            return new EnemyAction("attack", enemy.Atk, "team");
        }

        private static EnemyAction DecideRevive(EnemyState enemy)
        {
            // Normal attack. Revive logic is handled externally when HP drops to 0
            // (check HasRevived status after damage application)
            return new EnemyAction("attack", enemy.Atk, "team");
        }

        private static EnemyAction DecideEnrageTimer(EnemyState enemy)
        {
            int enrageTurn = GetStatusInt(enemy, "enrage_after", 10);
            enemy.AttackCount++;

            if (enemy.AttackCount >= enrageTurn)
            {
                // Massive damage — dungeon timer ran out
                return new EnemyAction("enrage_attack", enemy.Atk * 50, "team",
                    new Dictionary<string, object> { { "message", "ENRAGE! Massive damage!" } });
            }

            // Normal attack, but warn player as timer approaches
            int turnsLeft = enrageTurn - enemy.AttackCount;
            return new EnemyAction("attack", enemy.Atk, "team",
                turnsLeft <= 3
                    ? new Dictionary<string, object> { { "warning_turns", turnsLeft } }
                    : null);
        }

        private static EnemyAction DecideGravity(EnemyState enemy)
        {
            // Gravity: reduce player HP to X% (doesn't kill)
            enemy.AttackCount++;

            // Use gravity every 3rd turn
            if (enemy.AttackCount % 3 == 0)
            {
                float gravityPercent = GetStatusFloat(enemy, "gravity_percent", 0.5f);
                return new EnemyAction("gravity", 0, "team",
                    new Dictionary<string, object>
                    {
                        { "hp_percent", gravityPercent },
                        { "message", $"Gravity! HP → {(int)(gravityPercent * 100)}%" },
                    });
            }

            return new EnemyAction("attack", enemy.Atk, "team");
        }

        // ── Revive System ──

        /// <summary>
        /// Check if an enemy that just died should revive. Call after applying damage.
        /// Returns true if the enemy was revived.
        /// </summary>
        public static bool CheckRevive(EnemyState enemy)
        {
            if (enemy.IsAlive) return false;
            if (enemy.Behavior != "revive") return false;
            if (enemy.HasStatus("has_revived")) return false;

            // Revive with 30% HP
            enemy.Hp = (int)(enemy.MaxHp * 0.3);
            enemy.AddStatus("has_revived", -1); // permanent marker, can't revive again
            return true;
        }

        /// <summary>
        /// Check if this enemy absorbs a given element (heals instead of taking damage).
        /// Returns true if the element is absorbed.
        /// </summary>
        public static bool AbsorbsElement(EnemyState enemy, int element)
        {
            if (enemy.Behavior != "absorb_element") return false;
            int absorbedElement = GetStatusInt(enemy, "absorb_element_id", -1);
            return absorbedElement == element;
        }

        // ── Buff System ──

        /// <summary>
        /// Apply buff_allies effect to other enemies in the wave.
        /// </summary>
        public static void ApplyBuffAllies(List<EnemyState> enemies, EnemyState buffer)
        {
            foreach (var enemy in enemies)
            {
                if (enemy != buffer && enemy.IsAlive)
                {
                    enemy.Atk = (int)(enemy.Atk * 1.5);
                    enemy.AddStatus("atk_boosted", 1,
                        new Dictionary<string, object> { { "original_atk", enemy.Atk / 1.5 } });
                }
            }
        }

        /// <summary>
        /// Apply gravity attack to player. Reduces TeamHp to a percentage of MaxHp.
        /// Returns damage dealt.
        /// </summary>
        public static int ApplyGravity(int teamHp, int maxHp, float targetPercent)
        {
            int targetHp = Math.Max((int)(maxHp * targetPercent), 1);
            if (teamHp <= targetHp) return 0;
            return teamHp - targetHp;
        }

        // ── Utility ──

        public static void ResetCountdown(EnemyState enemy)
        {
            enemy.Countdown = enemy.MaxCountdown;
        }

        public static bool CanAttack(EnemyState enemy)
        {
            return enemy.IsAlive && enemy.Countdown <= 0;
        }

        /// <summary>
        /// Tick status effects for all enemies. Decrements turns and removes expired statuses.
        /// Also processes time_bomb detonation.
        /// </summary>
        public static void TickEnemyStatuses(List<EnemyState> enemies)
        {
            foreach (var enemy in enemies)
            {
                if (!enemy.IsAlive)
                    continue;

                for (int i = enemy.StatusEffects.Count - 1; i >= 0; i--)
                {
                    var se = enemy.StatusEffects[i];
                    if (se.TryGetValue("turns", out var turnsObj))
                    {
                        int turns = Convert.ToInt32(turnsObj);
                        if (turns > 0)
                        {
                            turns -= 1;
                            se["turns"] = turns;
                            if (turns <= 0)
                            {
                                // Check for time bomb detonation
                                if (se.TryGetValue("type", out var typeObj)
                                    && Convert.ToString(typeObj) == "time_bomb"
                                    && se.TryGetValue("damage", out var dmgObj))
                                {
                                    enemy.TakeDamage(Convert.ToInt32(dmgObj));
                                }

                                enemy.StatusEffects.RemoveAt(i);
                            }
                        }
                    }
                }
            }
        }

        // ── Status Helpers ──

        private static int GetStatusInt(EnemyState enemy, string key, int defaultValue)
        {
            foreach (var se in enemy.StatusEffects)
            {
                if (se.TryGetValue(key, out var val))
                {
                    try { return Convert.ToInt32(val); }
                    catch { /* continue */ }
                }
            }
            return defaultValue;
        }

        private static float GetStatusFloat(EnemyState enemy, string key, float defaultValue)
        {
            foreach (var se in enemy.StatusEffects)
            {
                if (se.TryGetValue(key, out var val))
                {
                    try { return Convert.ToSingle(val); }
                    catch { /* continue */ }
                }
            }
            return defaultValue;
        }

        private static string GetStatusString(EnemyState enemy, string key, string defaultValue)
        {
            foreach (var se in enemy.StatusEffects)
            {
                if (se.TryGetValue(key, out var val))
                    return Convert.ToString(val) ?? defaultValue;
            }
            return defaultValue;
        }
    }
}
