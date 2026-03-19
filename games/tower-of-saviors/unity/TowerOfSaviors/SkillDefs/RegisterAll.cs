using System;
using System.Collections.Generic;
using MobileForge.Domain;

namespace TowerOfSaviors
{
    /// <summary>
    /// Registers all ToS-specific skill conditions and effects with the framework.
    /// 11 conditions + 16 simple effects + 10 persistent outcomes
    /// + 3 team skill effects. Formulas referenced from decompiled ToS source.
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
            pipeline.ConditionRegistry.Register("combo_gte",
                parameters => new ComboGteCondition(parameters));
            pipeline.ConditionRegistry.Register("combo_lt",
                parameters => new ComboLtCondition(parameters));

            // New conditions from ToS decompiled source
            pipeline.ConditionRegistry.Register("hp_above",
                parameters => new HpAboveCondition(parameters));
            pipeline.ConditionRegistry.Register("turn_number",
                parameters => new TurnNumberCondition(parameters));
            pipeline.ConditionRegistry.Register("enemies_alive",
                parameters => new EnemiesAliveCondition(parameters));
            pipeline.ConditionRegistry.Register("all_elements_matched",
                parameters => new AllElementsMatchedCondition(parameters));
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

            // New effects from ToS decompiled source
            pipeline.EffectRegistry.Register("absolute_damage", AbsoluteDamage);
            pipeline.EffectRegistry.Register("charge_skills", ChargeSkills);
            pipeline.EffectRegistry.Register("random_gem_change", RandomGemChange);
            pipeline.EffectRegistry.Register("shield", Shield);
            pipeline.EffectRegistry.Register("time_bomb", TimeBomb);
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

            // New persistent outcomes from ToS decompiled source
            pipeline.OutcomeRegistry.Register("reduce_enemy_defense",
                parameters => new ReduceEnemyDefenseOutcome(parameters));
            pipeline.OutcomeRegistry.Register("counter_attack",
                parameters => new CounterAttackOutcome(parameters));
            pipeline.OutcomeRegistry.Register("reduce_cooldown",
                parameters => new ReduceCooldownOutcome(parameters));
            pipeline.OutcomeRegistry.Register("element_shift",
                parameters => new ElementShiftOutcome(parameters));
            pipeline.OutcomeRegistry.Register("poison",
                parameters => new PoisonOutcome(parameters));
            pipeline.OutcomeRegistry.Register("force_drop_gem",
                parameters => new ForceDropGemOutcome(parameters));
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

        // ---- New Effect Implementations (from ToS decompiled source) ----

        /// <summary>
        /// Fixed damage ignoring defense. Matches ToS AllEnemyAbsoluteDamageSkill.
        /// Params: "damage" (int), "target" (string: "all"|"single", default "all")
        /// </summary>
        private static void AbsoluteDamage(Dictionary<string, object> parameters, SkillContext ctx, SkillResult result)
        {
            int damage = GetInt(parameters, "damage", 0);
            string target = GetString(parameters, "target", "all");

            if (target == "all")
            {
                for (int i = 0; i < ctx.Enemies.Count; i++)
                {
                    if (ctx.Enemies[i] is EnemyState enemy && enemy.IsAlive)
                        result.DamageDealt[i] = damage;
                }
            }
            else
            {
                // Single target — pick first alive enemy
                for (int i = 0; i < ctx.Enemies.Count; i++)
                {
                    if (ctx.Enemies[i] is EnemyState enemy && enemy.IsAlive)
                    {
                        result.DamageDealt[i] = damage;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Reset/reduce all team skill cooldowns. Matches ToS ChargePlayerSkill.
        /// Params: "amount" (int, default 99 = full reset)
        /// </summary>
        private static void ChargeSkills(Dictionary<string, object> parameters, SkillContext ctx, SkillResult result)
        {
            int amount = GetInt(parameters, "amount", 99);
            ctx.Extra["cd_reduction_pending"] = amount;
            result.BuffsApplied.Add(new Dictionary<string, object>
            {
                { "type", "charge_skills" }, { "amount", amount }
            });
        }

        /// <summary>
        /// Change N random gems to a specific element. Matches ToS ChangePuzzleElementActiveSkill.
        /// Params: "element" (int), "count" (int, default 5)
        /// </summary>
        private static void RandomGemChange(Dictionary<string, object> parameters, SkillContext ctx, SkillResult result)
        {
            int toElement = GetInt(parameters, "element", 1);
            int count = GetInt(parameters, "count", 5);

            if (ctx.Board is BoardLogic board)
            {
                var candidates = new List<int>();
                for (int pos = 0; pos < board.Config.TotalCells; pos++)
                {
                    var gem = board.GetGem(pos);
                    if (gem != null && gem.ElementId != toElement)
                        candidates.Add(pos);
                }

                // Shuffle and take first N
                var rng = new Random();
                for (int i = candidates.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    (candidates[i], candidates[j]) = (candidates[j], candidates[i]);
                }

                int changed = 0;
                foreach (int pos in candidates)
                {
                    if (changed >= count) break;
                    var gem = board.GetGem(pos);
                    if (gem != null)
                    {
                        int oldElem = gem.ElementId;
                        board.ChangeGemElement(pos, toElement);
                        result.BoardChanges.Add(new Dictionary<string, object>
                        {
                            { "pos", pos }, { "old_element", oldElem }, { "new_element", toElement }
                        });
                        changed++;
                    }
                }
            }
        }

        /// <summary>
        /// Create a damage shield that absorbs N damage before breaking.
        /// Matches ToS SO_BossShield concept applied to player.
        /// Params: "absorb_amount" (int)
        /// </summary>
        private static void Shield(Dictionary<string, object> parameters, SkillContext ctx, SkillResult result)
        {
            int amount = GetInt(parameters, "absorb_amount", 0);
            ctx.Extra["shield_remaining"] = amount;
            result.BuffsApplied.Add(new Dictionary<string, object>
            {
                { "type", "shield" }, { "absorb_amount", amount }
            });
        }

        /// <summary>
        /// Place a time bomb on all alive enemies that detonates after N turns.
        /// Params: "damage" (int), "delay_turns" (int, default 3)
        /// </summary>
        private static void TimeBomb(Dictionary<string, object> parameters, SkillContext ctx, SkillResult result)
        {
            int damage = GetInt(parameters, "damage", 0);
            int delay = GetInt(parameters, "delay_turns", 3);

            for (int i = 0; i < ctx.Enemies.Count; i++)
            {
                if (ctx.Enemies[i] is EnemyState enemy && enemy.IsAlive)
                {
                    enemy.AddStatus("time_bomb", delay, new Dictionary<string, object>
                    {
                        { "damage", damage }
                    });
                }
            }
            result.BuffsApplied.Add(new Dictionary<string, object>
            {
                { "type", "time_bomb" }, { "damage", damage }, { "delay_turns", delay }
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
