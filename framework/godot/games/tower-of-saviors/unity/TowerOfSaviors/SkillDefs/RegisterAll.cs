using System;
using System.Collections.Generic;
using MobileForge.Domain;

namespace TowerOfSaviors
{
    /// <summary>
    /// Registers all ToS-specific skill conditions and effects with the framework.
    /// Mirrors register_all.gd — 7 conditions + 10 simple effects + 4 persistent outcomes
    /// + 3 team skill effects.
    /// </summary>
    public static class TosSkillRegistration
    {
        public static void Register(SkillPipeline pipeline)
        {
            RegisterConditions(pipeline);
            RegisterSimpleEffects(pipeline);
            RegisterPersistentOutcomes(pipeline);
        }

        private static void RegisterConditions(SkillPipeline pipeline)
        {
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
            // New conditions
            pipeline.ConditionRegistry.Register("combo_gte",
                parameters => new ComboGteCondition(parameters));
            pipeline.ConditionRegistry.Register("combo_lt",
                parameters => new ComboLtCondition(parameters));
        }

        private static void RegisterSimpleEffects(SkillPipeline pipeline)
        {
            // Existing effects
            pipeline.EffectRegistry.Register("area_damage", AreaDamage);
            pipeline.EffectRegistry.Register("heal_flat", HealFlat);
            pipeline.EffectRegistry.Register("heal_percent", HealPercent);
            pipeline.EffectRegistry.Register("change_gem_element", ChangeGemElement);
            pipeline.EffectRegistry.Register("delay_enemies", DelayEnemies);

            // New simple effects
            pipeline.EffectRegistry.Register("single_target_damage", SingleTargetDamage);
            pipeline.EffectRegistry.Register("gem_conversion", GemConversion);
            pipeline.EffectRegistry.Register("self_damage", SelfDamage);
            pipeline.EffectRegistry.Register("element_change", ElementChange);
            pipeline.EffectRegistry.Register("rec_buff", RecBuff);
            pipeline.EffectRegistry.Register("lifesteal", Lifesteal);

            // Team skill effects (passive stat multipliers)
            pipeline.EffectRegistry.Register("element_atk_mult", ElementAtkMult);
            pipeline.EffectRegistry.Register("element_hp_mult", ElementHpMult);
            pipeline.EffectRegistry.Register("element_rec_mult", ElementRecMult);
        }

        private static void RegisterPersistentOutcomes(SkillPipeline pipeline)
        {
            pipeline.OutcomeRegistry.Register("heal_over_time",
                parameters => new HealOverTimeOutcome(parameters));
            pipeline.OutcomeRegistry.Register("atk_buff",
                parameters => new AtkBuffOutcome(parameters));
            pipeline.OutcomeRegistry.Register("defense_buff",
                parameters => new DefenseBuffOutcome(parameters));
            pipeline.OutcomeRegistry.Register("combo_scaling_atk",
                parameters => new ComboScalingAtkOutcome(parameters));
        }

        // ---- Existing Effect Implementations ----

        private static void AreaDamage(Dictionary<string, object> parameters, SkillContext ctx, SkillResult result)
        {
            float mult = GetFloat(parameters, "multiplier", 1.0f);
            float atk = 0f;
            if (ctx.TeamStats != null && ctx.TeamStats.Count > 0 && ctx.TeamStats[0] is MonsterStats stats)
                atk = stats.Atk;

            for (int i = 0; i < ctx.Enemies.Count; i++)
            {
                if (ctx.Enemies[i] is EnemyState enemy && enemy.IsAlive)
                    result.DamageDealt[i] = (int)(atk * mult);
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
            if (ctx.Board == null) return;

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
                            { "pos", pos }, { "old_element", fromElem }, { "new_element", toElem }
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
                    enemy.Countdown += turns;
            }
        }

        // ---- New Simple Effect Implementations ----

        private static void SingleTargetDamage(Dictionary<string, object> parameters, SkillContext ctx, SkillResult result)
        {
            float mult = GetFloat(parameters, "multiplier", 1.0f);
            float atk = 0f;
            if (ctx.TeamStats != null && ctx.TeamStats.Count > 0 && ctx.TeamStats[0] is MonsterStats stats)
                atk = stats.Atk;

            string targetStrategy = GetString(parameters, "target", "highest_hp");
            int targetIdx = -1;
            int bestHp = -1;

            for (int i = 0; i < ctx.Enemies.Count; i++)
            {
                if (ctx.Enemies[i] is EnemyState enemy && enemy.IsAlive)
                {
                    if (targetStrategy == "highest_hp")
                    {
                        if (enemy.Hp > bestHp)
                        {
                            bestHp = enemy.Hp;
                            targetIdx = i;
                        }
                    }
                    else if (targetIdx < 0)
                    {
                        targetIdx = i;
                    }
                }
            }

            if (targetIdx >= 0)
                result.DamageDealt[targetIdx] = (int)(atk * mult);
        }

        private static void GemConversion(Dictionary<string, object> parameters, SkillContext ctx, SkillResult result)
        {
            int fromElem = GetIntAlt(parameters, "from_element", "from", 0);
            int toElem = GetIntAlt(parameters, "to_element", "to", 0);
            if (ctx.Board == null) return;

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
                            { "pos", pos }, { "old_element", fromElem }, { "new_element", toElem }
                        });
                    }
                }
            }
        }

        private static void SelfDamage(Dictionary<string, object> parameters, SkillContext ctx, SkillResult result)
        {
            float pct = GetFloat(parameters, "hp_percent", 0f);
            int dmg = (int)(ctx.MaxHp * pct);
            ctx.TeamHp = Math.Max(ctx.TeamHp - dmg, 1); // Don't kill yourself
        }

        private static void ElementChange(Dictionary<string, object> parameters, SkillContext ctx, SkillResult result)
        {
            int toElem = GetInt(parameters, "to_element", 0);
            int duration = GetInt(parameters, "duration_turns", 1);
            result.BuffsApplied.Add(new Dictionary<string, object>
            {
                { "type", "element_change" }, { "to_element", toElem }, { "turns", duration }
            });
        }

        private static void RecBuff(Dictionary<string, object> parameters, SkillContext ctx, SkillResult result)
        {
            float mult = GetFloat(parameters, "multiplier", 1.0f);
            int duration = GetInt(parameters, "duration_turns", 1);
            result.BuffsApplied.Add(new Dictionary<string, object>
            {
                { "type", "rec_buff" }, { "multiplier", mult }, { "turns", duration }
            });
        }

        private static void Lifesteal(Dictionary<string, object> parameters, SkillContext ctx, SkillResult result)
        {
            float pct = GetFloat(parameters, "percent_of_damage", 0.3f);
            int totalDmg = 0;
            foreach (var kvp in result.DamageDealt)
                totalDmg += kvp.Value;
            result.Healing += (int)(totalDmg * pct);
        }

        private static void ElementAtkMult(Dictionary<string, object> parameters, SkillContext ctx, SkillResult result)
        {
            result.BuffsApplied.Add(new Dictionary<string, object>
            {
                { "type", "element_atk_mult" },
                { "element", GetInt(parameters, "element", 0) },
                { "multiplier", GetFloat(parameters, "multiplier", 1.0f) },
            });
        }

        private static void ElementHpMult(Dictionary<string, object> parameters, SkillContext ctx, SkillResult result)
        {
            result.BuffsApplied.Add(new Dictionary<string, object>
            {
                { "type", "element_hp_mult" },
                { "element", GetInt(parameters, "element", 0) },
                { "multiplier", GetFloat(parameters, "multiplier", 1.0f) },
            });
        }

        private static void ElementRecMult(Dictionary<string, object> parameters, SkillContext ctx, SkillResult result)
        {
            result.BuffsApplied.Add(new Dictionary<string, object>
            {
                { "type", "element_rec_mult" },
                { "element", GetInt(parameters, "element", 0) },
                { "multiplier", GetFloat(parameters, "multiplier", 1.0f) },
            });
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

        private static int GetIntAlt(Dictionary<string, object> data, string key1, string key2, int defaultValue)
        {
            if (data.TryGetValue(key1, out var val1))
            {
                try { return Convert.ToInt32(val1); }
                catch { /* fall through */ }
            }
            if (data.TryGetValue(key2, out var val2))
            {
                try { return Convert.ToInt32(val2); }
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

        private static string GetString(Dictionary<string, object> data, string key, string defaultValue)
        {
            if (data.TryGetValue(key, out var val))
                return Convert.ToString(val) ?? defaultValue;
            return defaultValue;
        }
    }
}
