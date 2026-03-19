using System.Collections.Generic;
using MobileForge.Domain;

namespace TowerOfSaviors
{
    /// <summary>
    /// Deals damage over time to all alive enemies each turn.
    /// Matches ToS poison/burn mechanics where enemies take recurring damage.
    /// Damage is based on a flat amount or percentage of enemy max HP.
    /// Params: "damage_per_turn" (int, flat damage), "hp_percent" (float, % of enemy max HP),
    ///         "duration_turns" (int)
    /// If both are set, the higher value is used per enemy.
    /// </summary>
    public class PoisonOutcome : SkillOutcomeBase
    {
        private int _flatDamage;
        private float _hpPercent;

        public PoisonOutcome(Dictionary<string, object> parameters = null) : base(parameters) { }

        public override void Activate(SkillContext context, SkillResult result)
        {
            _flatDamage = GetParam("damage_per_turn", 0);
            _hpPercent = GetParam("hp_percent", 0f);
            TurnsLeft = GetParam("duration_turns", 3);

            result.BuffsApplied.Add(new Dictionary<string, object>
            {
                { "type", "poison" },
                { "damage_per_turn", _flatDamage },
                { "hp_percent", _hpPercent },
                { "turns", TurnsLeft },
            });
        }

        public override void OnTurnStart(SkillContext context)
        {
            // Deal poison damage to all alive enemies
            for (int i = 0; i < context.Enemies.Count; i++)
            {
                if (context.Enemies[i] is EnemyState enemy && enemy.IsAlive)
                {
                    int dmg = _flatDamage;
                    if (_hpPercent > 0)
                    {
                        int pctDmg = (int)(enemy.MaxHp * _hpPercent);
                        if (pctDmg > dmg) dmg = pctDmg;
                    }
                    enemy.Hp -= dmg;
                    if (enemy.Hp <= 0)
                        enemy.Hp = 0;
                }
            }
        }
    }
}
