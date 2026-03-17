using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Loaded from JSON, immutable after construction.
    /// </summary>
    public class SkillDef
    {
        public int Id { get; }
        public string Name { get; }
        public string Description { get; }
        public string Type { get; } // "active", "leader", "team"
        public int MaxCd { get; }   // max cooldown (active skills only)
        public int MinCd { get; }   // min cooldown after max skill level
        public int MaxLevel { get; }
        public List<SkillRule> Rules { get; }

        public SkillDef(Dictionary<string, object> data)
        {
            Id = GetInt(data, "id", 0);
            Name = GetString(data, "name", "");
            Description = GetString(data, "description", "");
            Type = GetString(data, "type", "active");
            MaxCd = GetInt(data, "max_cd", 0);
            MinCd = GetInt(data, "min_cd", 0);
            MaxLevel = GetInt(data, "max_level", 1);
            Rules = new List<SkillRule>();
            if (data.TryGetValue("rules", out var rulesObj) && rulesObj is List<object> rulesList)
            {
                foreach (var ruleData in rulesList)
                {
                    if (ruleData is Dictionary<string, object> ruleDict)
                        Rules.Add(new SkillRule(ruleDict));
                }
            }
        }

        private static int GetInt(Dictionary<string, object> data, string key, int defaultValue)
        {
            return data.TryGetValue(key, out var val) ? Convert.ToInt32(val) : defaultValue;
        }

        private static string GetString(Dictionary<string, object> data, string key, string defaultValue)
        {
            return data.TryGetValue(key, out var val) ? Convert.ToString(val) : defaultValue;
        }
    }

    /// <summary>
    /// One rule within a skill: conditions that must be met and outcomes to apply.
    /// </summary>
    public class SkillRule
    {
        public List<Dictionary<string, object>> Conditions { get; }
        public List<Dictionary<string, object>> Outcomes { get; }

        public SkillRule(Dictionary<string, object> data)
        {
            Conditions = new List<Dictionary<string, object>>();
            if (data.TryGetValue("conditions", out var condObj) && condObj is List<object> condList)
            {
                foreach (var c in condList)
                {
                    if (c is Dictionary<string, object> cDict)
                        Conditions.Add(cDict);
                }
            }

            Outcomes = new List<Dictionary<string, object>>();
            if (data.TryGetValue("outcomes", out var outObj) && outObj is List<object> outList)
            {
                foreach (var o in outList)
                {
                    if (o is Dictionary<string, object> oDict)
                        Outcomes.Add(oDict);
                }
            }
        }
    }

    /// <summary>
    /// Mutable context passed during skill activation.
    /// </summary>
    public class SkillContext
    {
        public int CasterIndex { get; set; }
        public List<object> Team { get; set; }
        public List<object> TeamStats { get; set; }
        public object Board { get; set; }
        public object Combat { get; set; }
        public List<object> Enemies { get; set; }
        public int TeamHp { get; set; }
        public int MaxHp { get; set; }
        public int ComboCount { get; set; }
        public Dictionary<int, int> ElementsMatched { get; set; }
        public int TurnNumber { get; set; }
        public Dictionary<string, object> Extra { get; set; }

        public SkillContext()
        {
            CasterIndex = 0;
            Team = new List<object>();
            TeamStats = new List<object>();
            Board = null;
            Combat = null;
            Enemies = new List<object>();
            TeamHp = 0;
            MaxHp = 0;
            ComboCount = 0;
            ElementsMatched = new Dictionary<int, int>();
            TurnNumber = 0;
            Extra = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Output from skill activation.
    /// </summary>
    public class SkillResult
    {
        public List<Dictionary<string, object>> BoardChanges { get; set; }
        public Dictionary<int, int> DamageDealt { get; set; }
        public int Healing { get; set; }
        public List<Dictionary<string, object>> BuffsApplied { get; set; }
        public List<int> EnemiesKilled { get; set; }

        public SkillResult()
        {
            BoardChanges = new List<Dictionary<string, object>>();
            DamageDealt = new Dictionary<int, int>();
            Healing = 0;
            BuffsApplied = new List<Dictionary<string, object>>();
            EnemiesKilled = new List<int>();
        }
    }
}
