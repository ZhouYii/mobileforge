using System.Collections.Generic;
using MobileForge.Domain;

namespace TowerOfSaviors
{
    /// <summary>
    /// Returns a percentage of enemy damage back to the attacker for N turns.
    /// Matches ToS SO_ReturnAttack / SO_Parry.
    /// Params: "return_percent" (float, default 1.0 = 100%), "duration_turns" (int)
    /// </summary>
    public class CounterAttackOutcome : SkillOutcomeBase
    {
        private float _returnPercent;

        public CounterAttackOutcome(Dictionary<string, object> parameters = null) : base(parameters) { }

        public override void Activate(SkillContext context, SkillResult result)
        {
            _returnPercent = GetParam("return_percent", 1.0f);
            TurnsLeft = GetParam("duration_turns", 3);

            result.BuffsApplied.Add(new Dictionary<string, object>
            {
                { "type", "counter_attack" },
                { "return_percent", _returnPercent },
                { "turns", TurnsLeft },
            });

            // Store in context for combat resolver to read during enemy attacks
            context.Extra["counter_attack_percent"] = _returnPercent;
        }

        public override void OnTurnStart(SkillContext context)
        {
            // Refresh the counter for this turn
            context.Extra["counter_attack_percent"] = _returnPercent;
        }

        public override void Deactivate(SkillContext context)
        {
            context.Extra.Remove("counter_attack_percent");
        }
    }
}
