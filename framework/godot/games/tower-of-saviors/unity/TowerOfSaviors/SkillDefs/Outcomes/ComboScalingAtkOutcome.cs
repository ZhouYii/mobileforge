using System;
using System.Collections.Generic;
using MobileForge.Domain;

namespace TowerOfSaviors
{
    /// <summary>
    /// Registers a MAIN hook that scales ATK with combo count for N turns.
    /// Each combo above 1 adds bonus_per_combo to multiplier.
    /// Params: "bonus_per_combo" (float), "duration_turns" (int)
    /// </summary>
    public class ComboScalingAtkOutcome : SkillOutcomeBase
    {
        private float _bonusPerCombo;

        public ComboScalingAtkOutcome(Dictionary<string, object> parameters = null)
            : base(parameters) { }

        public override void Activate(SkillContext context, SkillResult result)
        {
            _bonusPerCombo = GetParam<float>("bonus_per_combo", 0.5f);
            TurnsLeft = GetParam<int>("duration_turns", 1);

            result.BuffsApplied.Add(new Dictionary<string, object>
            {
                { "type", "combo_scaling_atk" },
                { "bonus_per_combo", _bonusPerCombo },
                { "turns", TurnsLeft }
            });
        }

        public override void Deactivate(SkillContext context)
        {
            // Hook cleanup handled by pipeline
        }
    }
}
