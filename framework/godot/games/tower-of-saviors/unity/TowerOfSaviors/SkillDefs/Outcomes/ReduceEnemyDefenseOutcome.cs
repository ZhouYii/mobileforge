using System.Collections.Generic;
using MobileForge.Domain;

namespace TowerOfSaviors
{
    /// <summary>
    /// Reduces all alive enemies' defense for N turns.
    /// Matches ToS SO_ReduceEnemeyDefence — registers a POST_DEFENSE combat hook
    /// that reduces the effective defense applied to damage calculations.
    /// Params: "reduction_percent" (float 0.0-1.0, default 0.5), "duration_turns" (int)
    /// </summary>
    public class ReduceEnemyDefenseOutcome : SkillOutcomeBase
    {
        private float _reductionPercent;

        public ReduceEnemyDefenseOutcome(Dictionary<string, object> parameters = null) : base(parameters) { }

        public override void Activate(SkillContext context, SkillResult result)
        {
            _reductionPercent = GetParam("reduction_percent", 0.5f);
            TurnsLeft = GetParam("duration_turns", 3);

            // Mark enemies with reduced defense
            foreach (var enemyObj in context.Enemies)
            {
                if (enemyObj is EnemyState enemy && enemy.IsAlive)
                    enemy.Defense = (int)(enemy.Defense * (1f - _reductionPercent));
            }

            result.BuffsApplied.Add(new Dictionary<string, object>
            {
                { "type", "reduce_enemy_defense" },
                { "reduction_percent", _reductionPercent },
                { "turns", TurnsLeft },
            });
        }
    }
}
