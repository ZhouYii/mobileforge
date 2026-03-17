using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Static utility for checking team skill conditions against team composition.
    /// </summary>
    public static class TeamSkillChecker
    {
        /// <summary>
        /// Check all conditions against the team definitions.
        /// Returns true if ALL conditions are met.
        /// Each condition is a dictionary with a "type" key and additional params.
        /// Supported types: all_same_element, has_element, min_rarity, unique_elements, contains_monster.
        /// </summary>
        public static bool CheckConditions(List<Dictionary<string, object>> conditions, List<MonsterDef> teamDefs)
        {
            if (conditions == null || conditions.Count == 0)
                return true;

            foreach (var condition in conditions)
            {
                if (!condition.TryGetValue("type", out var typeObj))
                    continue;

                string condType = Convert.ToString(typeObj);
                if (!EvaluateCondition(condType, condition, teamDefs))
                    return false;
            }

            return true;
        }

        private static bool EvaluateCondition(string condType, Dictionary<string, object> condition, List<MonsterDef> teamDefs)
        {
            switch (condType)
            {
                case "all_same_element":
                    return CheckAllSameElement(teamDefs);

                case "has_element":
                    return CheckHasElement(condition, teamDefs);

                case "min_rarity":
                    return CheckMinRarity(condition, teamDefs);

                case "unique_elements":
                    return CheckUniqueElements(condition, teamDefs);

                case "contains_monster":
                    return CheckContainsMonster(condition, teamDefs);

                default:
                    // Unknown condition type — treat as not met
                    return false;
            }
        }

        /// <summary>
        /// All members must have the same element.
        /// </summary>
        private static bool CheckAllSameElement(List<MonsterDef> teamDefs)
        {
            if (teamDefs.Count == 0)
                return false;

            int element = -1;
            foreach (var def in teamDefs)
            {
                if (def == null)
                    continue;
                if (element == -1)
                    element = def.Element;
                else if (def.Element != element)
                    return false;
            }
            return element != -1;
        }

        /// <summary>
        /// Team must contain at least one member with the specified element.
        /// Condition params: "element" (int).
        /// </summary>
        private static bool CheckHasElement(Dictionary<string, object> condition, List<MonsterDef> teamDefs)
        {
            if (!condition.TryGetValue("element", out var elemObj))
                return false;

            int requiredElement = Convert.ToInt32(elemObj);
            foreach (var def in teamDefs)
            {
                if (def != null && def.Element == requiredElement)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// All members must have at least the specified rarity.
        /// Condition params: "rarity" (int).
        /// </summary>
        private static bool CheckMinRarity(Dictionary<string, object> condition, List<MonsterDef> teamDefs)
        {
            if (!condition.TryGetValue("rarity", out var rarityObj))
                return false;

            int minRarity = Convert.ToInt32(rarityObj);
            foreach (var def in teamDefs)
            {
                if (def == null)
                    continue;
                if (def.Rarity < minRarity)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Team must have at least N unique elements.
        /// Condition params: "count" (int).
        /// </summary>
        private static bool CheckUniqueElements(Dictionary<string, object> condition, List<MonsterDef> teamDefs)
        {
            if (!condition.TryGetValue("count", out var countObj))
                return false;

            int requiredCount = Convert.ToInt32(countObj);
            var elements = new HashSet<int>();
            foreach (var def in teamDefs)
            {
                if (def != null)
                    elements.Add(def.Element);
            }
            return elements.Count >= requiredCount;
        }

        /// <summary>
        /// Team must contain a monster with the specified ID.
        /// Condition params: "monster_id" (int).
        /// </summary>
        private static bool CheckContainsMonster(Dictionary<string, object> condition, List<MonsterDef> teamDefs)
        {
            if (!condition.TryGetValue("monster_id", out var idObj))
                return false;

            int monsterId = Convert.ToInt32(idObj);
            foreach (var def in teamDefs)
            {
                if (def != null && def.Id == monsterId)
                    return true;
            }
            return false;
        }
    }
}
