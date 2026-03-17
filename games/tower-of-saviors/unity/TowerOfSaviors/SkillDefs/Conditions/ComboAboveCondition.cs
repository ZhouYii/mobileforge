using System;
using System.Collections.Generic;
using MobileForge.Domain;

namespace TowerOfSaviors
{
    /// <summary>
    /// Returns true if combo count is at or above the threshold.
    /// Params: "threshold" (int, default 1)
    /// </summary>
    public class ComboAboveCondition : SkillConditionBase
    {
        public ComboAboveCondition(Dictionary<string, object> parameters = null)
            : base(parameters) { }

        public override bool IsValid(SkillContext context)
        {
            int threshold = GetParam<int>("threshold", 1);
            return context.ComboCount >= threshold;
        }
    }
}
