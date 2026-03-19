using System.Collections.Generic;
using MobileForge.Domain;

namespace TowerOfSaviors
{
    /// <summary>
    /// True when all specified elements were matched this turn.
    /// Default: elements 1-5 (Water, Fire, Earth, Light, Dark) — the "rainbow" condition.
    /// Params: "elements" (List of int element IDs, default [1,2,3,4,5]),
    ///         "min_count" (int, minimum matches per element, default 1)
    /// </summary>
    public class AllElementsMatchedCondition : SkillConditionBase
    {
        public AllElementsMatchedCondition(Dictionary<string, object> parameters = null) : base(parameters) { }

        public override bool IsValid(SkillContext context)
        {
            int minCount = GetParam("min_count", 1);

            var requiredElements = GetElementList();
            if (context.ElementsMatched == null) return false;

            foreach (int elem in requiredElements)
            {
                if (!context.ElementsMatched.TryGetValue(elem, out int count) || count < minCount)
                    return false;
            }
            return true;
        }

        private List<int> GetElementList()
        {
            if (_params != null && _params.TryGetValue("elements", out var val))
            {
                if (val is List<object> list)
                {
                    var result = new List<int>();
                    foreach (var item in list)
                        result.Add(System.Convert.ToInt32(item));
                    return result;
                }
            }
            // Default: Water(1), Fire(2), Earth(3), Light(4), Dark(5)
            return new List<int> { 1, 2, 3, 4, 5 };
        }
    }
}
