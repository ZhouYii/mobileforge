using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Static enemy AI logic: countdown ticking, action selection, status management.
    /// Supports behavior field for varied enemy patterns:
    ///   "normal" — always attacks
    ///   "heavy_attack" — 2x damage every 3rd attack
    ///   "heal_self" — heals 20% max HP when below 30% HP
    ///   "buff_allies" — boosts other enemies' ATK once, then attacks normally
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

            switch (behavior)
            {
                case "heavy_attack":
                    enemy.AttackCount += 1;
                    if (enemy.AttackCount % 3 == 0)
                        return new EnemyAction("attack", enemy.Atk * 2, "team");
                    return new EnemyAction("attack", enemy.Atk, "team");

                case "heal_self":
                    float hpRatio = enemy.MaxHp > 0
                        ? (float)enemy.Hp / enemy.MaxHp
                        : 1f;
                    if (hpRatio < 0.3f)
                    {
                        int healAmount = (int)(enemy.MaxHp * 0.2);
                        enemy.Heal(healAmount);
                        return new EnemyAction("heal", 0, "self",
                            new Dictionary<string, object> { { "heal_amount", healAmount } });
                    }
                    return new EnemyAction("attack", enemy.Atk, "team");

                case "buff_allies":
                    if (!enemy.HasUsedBuff)
                    {
                        enemy.HasUsedBuff = true;
                        return new EnemyAction("buff", 0, "allies",
                            new Dictionary<string, object> { { "atk_boost", 1.5 } });
                    }
                    return new EnemyAction("attack", enemy.Atk, "team");

                default: // "normal"
                    return new EnemyAction("attack", enemy.Atk, "team");
            }
        }

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
        /// Reset an enemy's countdown to its max value after attacking.
        /// </summary>
        public static void ResetCountdown(EnemyState enemy)
        {
            enemy.Countdown = enemy.MaxCountdown;
        }

        /// <summary>
        /// Check if an enemy can attack (alive and countdown <= 0).
        /// </summary>
        public static bool CanAttack(EnemyState enemy)
        {
            return enemy.IsAlive && enemy.Countdown <= 0;
        }

        /// <summary>
        /// Tick status effects for all enemies. Decrements turns and removes expired statuses.
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
                                enemy.StatusEffects.RemoveAt(i);
                            }
                        }
                    }
                }
            }
        }
    }
}
