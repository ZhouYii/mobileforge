using System;
using System.Collections.Generic;
using MobileForge.Domain;

namespace TowerOfSaviors
{
    /// <summary>
    /// Registers a MAIN hook that multiplies damage for N turns.
    /// If element is 0, applies to all elements.
    /// Params: "element" (int), "multiplier" (float), "duration_turns" (int)
    /// </summary>
    public class AtkBuffOutcome : SkillOutcomeBase
    {
        private float _multiplier;
        private int _element;

        public AtkBuffOutcome(Dictionary<string, object> parameters = null)
            : base(parameters) { }

        public override void Activate(SkillContext context, SkillResult result)
        {
            _multiplier = GetParam<float>("multiplier", 1.5f);
            _element = GetParam<int>("element", 0);
            TurnsLeft = GetParam<int>("duration_turns", 1);

            result.BuffsApplied.Add(new Dictionary<string, object>
            {
                { "type", "atk_buff" },
                { "element", _element },
                { "multiplier", _multiplier },
                { "turns", TurnsLeft }
            });
        }

        public override void Deactivate(SkillContext context)
        {
            // Hook cleanup handled by pipeline
        }
    }
}
