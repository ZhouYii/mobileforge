using System;
using System.Collections.Generic;
using MobileForge.Domain;

namespace TowerOfSaviors
{
    /// <summary>
    /// Registers a POST_DEFENSE hook that reduces incoming damage for N turns.
    /// Params: "damage_reduction" (float 0.0-1.0), "duration_turns" (int)
    /// </summary>
    public class DefenseBuffOutcome : SkillOutcomeBase
    {
        private float _reduction;

        public DefenseBuffOutcome(Dictionary<string, object> parameters = null)
            : base(parameters) { }

        public override void Activate(SkillContext context, SkillResult result)
        {
            _reduction = GetParam<float>("damage_reduction", 0.5f);
            TurnsLeft = GetParam<int>("duration_turns", 2);

            result.BuffsApplied.Add(new Dictionary<string, object>
            {
                { "type", "defense_buff" },
                { "reduction", _reduction },
                { "turns", TurnsLeft }
            });
        }

        public override void Deactivate(SkillContext context)
        {
            // Hook cleanup handled by pipeline
        }
    }
}
