using System;
using System.Collections.Generic;
using MobileForge.Domain;

namespace TowerOfSaviors
{
    /// <summary>
    /// Returns true if combo count >= min_combo.
    /// Params: "min_combo" (int, default 1)
    /// </summary>
    public class ComboGteCondition : SkillConditionBase
    {
        public ComboGteCondition(Dictionary<string, object> parameters = null)
            : base(parameters) { }

        public override bool IsValid(SkillContext context)
        {
            int minCombo = GetParam<int>("min_combo", 1);
            return context.ComboCount >= minCombo;
        }
    }
}
