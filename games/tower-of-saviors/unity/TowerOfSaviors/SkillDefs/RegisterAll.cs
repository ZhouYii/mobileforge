using System;
using System.Collections.Generic;
using MobileForge.Domain;

namespace TowerOfSaviors
{
    /// <summary>
    /// Registers all ToS-specific skill conditions and effects with the framework.
    /// Mirrors register_all.gd — 5 conditions + 5 effects.
    /// </summary>
    public static class TosSkillRegistration
    {
        public static void Register(SkillPipeline pipeline)
        {
            // Conditions
            pipeline.ConditionRegistry.Register("always_true",
                parameters => new AlwaysTrueCondition(parameters));
            pipeline.ConditionRegistry.Register("combo_above",
                parameters => new ComboAboveCondition(parameters));
            pipeline.ConditionRegistry.Register("hp_below",
                parameters => new HpBelowCondition(parameters));
            pipeline.ConditionRegistry.Register("elements_matched",
                parameters => new ElementsMatchedCondition(parameters));
            pipeline.ConditionRegistry.Register("team_has_element",
                parameters => new TeamHasElementCondition(parameters));

            // Simple effects
            pipeline.EffectRegistry.Register("area_damage", AreaDamage);
            pipeline.EffectRegistry.Register("heal_flat", HealFlat);
            pipeline.EffectRegistry.Register("heal_percent", HealPercent);
            pipeline.EffectRegistry.Register("change_gem_element", ChangeGemElement);
            pipeline.EffectRegistry.Register("delay_enemies", DelayEnemies);
        }

        // ---- Effect Implementations ----

        private static void AreaDamage(Dictionary<string, object> parameters, SkillContext ctx, SkillResult result)
        {
            float mult = GetFloat(parameters, "multiplier", 1.0f);
            float atk = 0f;
            if (ctx.TeamStats != null && ctx.TeamStats.Count > 0 && ctx.TeamStats[0] != null)
            {
                if (ctx.TeamStats[0] is MonsterStats stats)
                    atk = stats.Atk;
            }

            for (int i = 0; i < ctx.Enemies.Count; i++)
            {
                if (ctx.Enemies[i] is EnemyState enemy && enemy.IsAlive)
                {
                    result.DamageDealt[i] = (int)(atk * mult);
                }
            }
        }

        private static void HealFlat(Dictionary<string, object> parameters, SkillContext ctx, SkillResult result)
        {
            result.Healing += GetInt(parameters, "amount", 0);
        }

        private static void HealPercent(Dictionary<string, object> parameters, SkillContext ctx, SkillResult result)
        {
            float pct = GetFloat(parameters, "percent", 0f);
            result.Healing += (int)(ctx.MaxHp * pct);
        }

        private static void ChangeGemElement(Dictionary<string, object> parameters, SkillContext ctx, SkillResult result)
        {
            int fromElem = GetInt(parameters, "from", 0);
            int toElem = GetInt(parameters, "to", 0);

            if (ctx.Board == null)
                return;

            if (ctx.Board is BoardLogic board)
            {
                for (int pos = 0; pos < board.Config.TotalCells; pos++)
                {
                    var gem = board.GetGem(pos);
                    if (gem != null && gem.ElementId == fromElem)
                    {
                        board.ChangeGemElement(pos, toElem);
                        result.BoardChanges.Add(new Dictionary<string, object>
                        {
                            { "pos", pos },
                            { "old_element", fromElem },
                            { "new_element", toElem }
                        });
                    }
                }
            }
        }

        private static void DelayEnemies(Dictionary<string, object> parameters, SkillContext ctx, SkillResult result)
        {
            int turns = GetInt(parameters, "turns", 1);
            foreach (var enemyObj in ctx.Enemies)
            {
                if (enemyObj is EnemyState enemy && enemy.IsAlive)
                {
                    enemy.Countdown += turns;
                }
            }
        }

        // ---- Helper Methods ----

        private static int GetInt(Dictionary<string, object> data, string key, int defaultValue)
        {
            if (data.TryGetValue(key, out var val))
            {
                try { return Convert.ToInt32(val); }
                catch { return defaultValue; }
            }
            return defaultValue;
        }

        private static float GetFloat(Dictionary<string, object> data, string key, float defaultValue)
        {
            if (data.TryGetValue(key, out var val))
            {
                try { return Convert.ToSingle(val); }
                catch { return defaultValue; }
            }
            return defaultValue;
        }
    }

    // ---- Condition Classes ----

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

    /// <summary>
    /// Returns true if combo count is at or above the threshold.
    /// Params: "threshold" (int, default 1)
    /// </summary>
    public class ComboAboveCondition : SkillConditionBase
    {
        public ComboAboveCondition(Dictionary<string, object> parameters = null)
            : base(parameters) { }

        public override bool IsValid(SkillContext context)
        {
            int threshold = GetParam<int>("threshold", 1);
            return context.ComboCount >= threshold;
        }
    }

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
