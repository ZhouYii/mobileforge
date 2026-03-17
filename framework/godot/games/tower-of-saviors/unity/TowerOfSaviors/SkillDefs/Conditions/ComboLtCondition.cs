using System;
using System.Collections.Generic;
using MobileForge.Domain;

namespace TowerOfSaviors
{
    /// <summary>
    /// Returns true if combo count is less than max_combo.
    /// Params: "max_combo" (int, default 1)
    /// </summary>
    public class ComboLtCondition : SkillConditionBase
    {
        public ComboLtCondition(Dictionary<string, object> parameters = null)
            : base(parameters) { }

        public override bool IsValid(SkillContext context)
        {
            int maxCombo = GetParam<int>("max_combo", 1);
            return context.ComboCount < maxCombo;
        }
    }
}
