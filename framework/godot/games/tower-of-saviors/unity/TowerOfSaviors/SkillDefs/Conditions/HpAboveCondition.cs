using System.Collections.Generic;
using MobileForge.Domain;

namespace TowerOfSaviors
{
    /// <summary>
    /// True when team HP is above a threshold percentage.
    /// Complement of HpBelowCondition. Used by leader skills that
    /// require staying healthy (e.g. "ATK x3 when HP > 80%").
    /// Params: "percent" (float, default 0.5)
    /// </summary>
    public class HpAboveCondition : SkillConditionBase
    {
        public HpAboveCondition(Dictionary<string, object> parameters = null) : base(parameters) { }

        public override bool IsValid(SkillContext context)
        {
            float threshold = GetParam("percent", 0.5f);
            if (context.MaxHp <= 0) return false;
            return (float)context.TeamHp / context.MaxHp >= threshold;
        }
    }
}
