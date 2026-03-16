using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Static enemy AI logic: countdown ticking, action selection, status management.
    /// </summary>
    public static class EnemyAI
    {
        /// <summary>
        /// Tick countdowns for all enemies. Returns list of enemies whose countdown hit 0 (ready to attack).
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
        /// Decide what action an enemy will take.
        /// Default behavior: attack with base ATK.
        /// </summary>
        public static EnemyAction DecideAction(EnemyState enemy)
        {
            // Default behavior: basic attack
            return new EnemyAction("attack", enemy.Atk, "team");
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
                        // turns == -1 means infinite duration, do not remove
                    }
                }
            }
        }
    }
}
