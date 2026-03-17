using System;
using System.Collections.Generic;
using MobileForge.Domain;

namespace TowerOfSaviors
{
    /// <summary>
    /// Returns true if team HP is at or below a percentage of max HP.
    /// Params: "percent" (float, default 0.5)
    /// </summary>
    public class HpBelowCondition : SkillConditionBase
    {
        public HpBelowCondition(Dictionary<string, object> parameters = null)
            : base(parameters) { }

        public override bool IsValid(SkillContext context)
        {
            float pct = GetParam<float>("percent", 0.5f);
            if (context.MaxHp <= 0)
                return false;
            return (float)context.TeamHp / (float)context.MaxHp <= pct;
        }
    }
}
