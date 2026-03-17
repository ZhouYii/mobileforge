using System;
using System.Collections.Generic;
using MobileForge.Domain;

namespace TowerOfSaviors
{
    /// <summary>
    /// Returns true if a specific element was matched at least min_count times.
    /// Params: "element" (int), "min_count" (int, default 1)
    /// </summary>
    public class ElementsMatchedCondition : SkillConditionBase
    {
        public ElementsMatchedCondition(Dictionary<string, object> parameters = null)
            : base(parameters) { }

        public override bool IsValid(SkillContext context)
        {
            int elem = GetParam<int>("element", 0);
            int minCount = GetParam<int>("min_count", 1);
            int matched = 0;
            if (context.ElementsMatched != null && context.ElementsMatched.TryGetValue(elem, out var count))
                matched = count;
            return matched >= minCount;
        }
    }
}
