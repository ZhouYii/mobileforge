using System;
using System.Collections.Generic;
using MobileForge.Domain;

namespace TowerOfSaviors
{
    /// <summary>
    /// Returns true if any monster on the team has the specified element.
    /// Params: "element" (int)
    /// </summary>
    public class TeamHasElementCondition : SkillConditionBase
    {
        public TeamHasElementCondition(Dictionary<string, object> parameters = null)
            : base(parameters) { }

        public override bool IsValid(SkillContext context)
        {
            int elem = GetParam<int>("element", 0);
            if (context.Team == null)
                return false;

            foreach (var monsterObj in context.Team)
            {
                if (monsterObj is Dictionary<string, object> monster
                    && monster.TryGetValue("element", out var elemObj))
                {
                    try
                    {
                        if (Convert.ToInt32(elemObj) == elem)
                            return true;
                    }
                    catch { /* skip invalid entries */ }
                }
            }
            return false;
        }
    }
}
