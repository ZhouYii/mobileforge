using System.Collections.Generic;
using MobileForge.Domain;

namespace TowerOfSaviors
{
    /// <summary>
    /// Always returns true. Used as the default/unconditional condition.
    /// </summary>
    public class AlwaysTrueCondition : SkillConditionBase
    {
        public AlwaysTrueCondition(Dictionary<string, object> parameters = null)
            : base(parameters) { }

        public override bool IsValid(SkillContext context)
        {
            return true;
        }
    }
}
