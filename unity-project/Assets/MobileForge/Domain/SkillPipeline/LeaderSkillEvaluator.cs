using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Evaluates leader and friend leader skills each turn, registering damage hooks
    /// on the CombatResolver for stat multipliers.
    ///
    /// In ToS, leader skills are THE most impactful mechanic — providing massive
    /// conditional multipliers (e.g., "ATK x4 at 6+ combos for Water monsters").
    /// Both your leader (slot 0) and friend helper (last slot) contribute.
    ///
    /// Supported leader skill rule types (via SkillPipeline condition/outcome system):
    /// - Flat multipliers: always active (e.g., "HP x2 for all Water")
    /// - Combo-based: "ATK x3 at 5+ combos"
    /// - HP-based: "ATK x4 when HP > 80%"
    /// - Element match: "ATK x3 when matching Water+Fire+Earth"
    /// - Rainbow: "ATK x5 when all 5 elements matched"
    /// - Kill bonus: applied on overkill (via damage hooks)
    ///
    /// Usage:
    ///   evaluator.SetLeaderSkills(leaderSkillDef, friendSkillDef);
    ///   evaluator.OnTurnStart(context, combat);  // registers hooks
    ///   // ... combat resolution happens with hooks active ...
    ///   evaluator.OnTurnEnd(combat);  // cleans up hooks
    /// </summary>
    public class LeaderSkillEvaluator
    {
        private SkillDef _leaderSkill;
        private SkillDef _friendSkill;
        private readonly SkillPipeline _pipeline;
        private readonly List<RegisteredHook> _activeHooks = new();

        public LeaderSkillEvaluator(SkillPipeline pipeline)
        {
            _pipeline = pipeline;
        }

        /// <summary>
        /// Set the leader and friend leader skill definitions.
        /// Call once when entering a dungeon.
        /// </summary>
        public void SetLeaderSkills(SkillDef leaderSkill, SkillDef friendSkill = null)
        {
            _leaderSkill = leaderSkill;
            _friendSkill = friendSkill;
        }

        /// <summary>
        /// Evaluate leader skills for this turn and register appropriate damage hooks.
        /// Call at the start of each turn BEFORE damage resolution.
        /// </summary>
        public void OnTurnStart(SkillContext context, CombatResolver combat)
        {
            // Clean up any leftover hooks from previous turn
            CleanupHooks(combat);

            // Evaluate and apply leader skill
            if (_leaderSkill != null)
                EvaluateAndApply(_leaderSkill, context, combat, "leader");

            // Evaluate and apply friend leader skill
            if (_friendSkill != null)
                EvaluateAndApply(_friendSkill, context, combat, "friend_leader");
        }

        /// <summary>
        /// Clean up hooks at end of turn.
        /// </summary>
        public void OnTurnEnd(CombatResolver combat)
        {
            CleanupHooks(combat);
        }

        private void EvaluateAndApply(SkillDef skillDef, SkillContext context, CombatResolver combat, string source)
        {
            if (skillDef.Rules == null) return;

            foreach (var rule in skillDef.Rules)
            {
                // Check all conditions
                bool conditionsMet = true;
                if (rule.Conditions != null)
                {
                    foreach (var condData in rule.Conditions)
                    {
                        string condType = condData.TryGetValue("type", out var t)
                            ? Convert.ToString(t) : "always_true";
                        var condition = _pipeline.ConditionRegistry.Create(condType, condData);
                        if (condition != null && !condition.IsValid(context))
                        {
                            conditionsMet = false;
                            break;
                        }
                    }
                }

                if (!conditionsMet) continue;

                // Apply outcomes as damage hooks
                if (rule.Outcomes != null)
                {
                    foreach (var outcomeData in rule.Outcomes)
                    {
                        ApplyLeaderOutcome(outcomeData, context, combat, source);
                    }
                }
            }
        }

        private void ApplyLeaderOutcome(Dictionary<string, object> outcomeData,
            SkillContext context, CombatResolver combat, string source)
        {
            string type = outcomeData.TryGetValue("type", out var t) ? Convert.ToString(t) : "";

            switch (type)
            {
                case "atk_mult":
                {
                    float mult = GetFloat(outcomeData, "multiplier", 1f);
                    int element = GetInt(outcomeData, "element", 0); // 0 = all elements
                    RegisterMainHook(combat, source + "_atk_mult", ctx =>
                    {
                        if (element == 0 || ctx.AttackerElement == element)
                            ctx.Damage *= mult;
                    });
                    break;
                }

                case "hp_mult":
                {
                    float mult = GetFloat(outcomeData, "multiplier", 1f);
                    int element = GetInt(outcomeData, "element", 0);
                    // HP multiplier is applied externally (team HP calc), store in context
                    string key = $"{source}_hp_mult_{element}";
                    context.Extra[key] = mult;
                    break;
                }

                case "rec_mult":
                {
                    float mult = GetFloat(outcomeData, "multiplier", 1f);
                    int element = GetInt(outcomeData, "element", 0);
                    string key = $"{source}_rec_mult_{element}";
                    context.Extra[key] = mult;
                    break;
                }

                case "combo_atk_scale":
                {
                    // ATK scales with combo count: base_mult + (combos - threshold) * per_combo
                    float baseMult = GetFloat(outcomeData, "base_multiplier", 1f);
                    float perCombo = GetFloat(outcomeData, "per_combo", 0.5f);
                    int threshold = GetInt(outcomeData, "threshold", 1);
                    int comboCount = context.ComboCount;

                    if (comboCount >= threshold)
                    {
                        float totalMult = baseMult + (comboCount - threshold) * perCombo;
                        RegisterMainHook(combat, source + "_combo_atk", ctx =>
                        {
                            ctx.Damage *= totalMult;
                        });
                    }
                    break;
                }

                case "element_match_atk":
                {
                    // ATK bonus when specific elements are matched this turn
                    float mult = GetFloat(outcomeData, "multiplier", 1f);
                    var requiredElements = GetIntList(outcomeData, "elements");

                    if (requiredElements.Count > 0 && context.ElementsMatched != null)
                    {
                        bool allMatched = true;
                        foreach (int elem in requiredElements)
                        {
                            if (!context.ElementsMatched.ContainsKey(elem)
                                || context.ElementsMatched[elem] <= 0)
                            {
                                allMatched = false;
                                break;
                            }
                        }
                        if (allMatched)
                        {
                            RegisterMainHook(combat, source + "_elem_match_atk", ctx =>
                            {
                                ctx.Damage *= mult;
                            });
                        }
                    }
                    break;
                }

                case "damage_reduction":
                {
                    float reduction = GetFloat(outcomeData, "percent", 0f);
                    RegisterHook(combat, DamageHook.PostDefense, source + "_dmg_red", ctx =>
                    {
                        ctx.Damage *= (1f - reduction);
                    });
                    break;
                }
            }
        }

        private void RegisterMainHook(CombatResolver combat, string name, Action<DamageContext> callback)
        {
            RegisterHook(combat, DamageHook.Main, name, callback);
        }

        private void RegisterHook(CombatResolver combat, DamageHook hookType, string name, Action<DamageContext> callback)
        {
            combat.RegisterHook(hookType, callback, 50, name);
            _activeHooks.Add(new RegisteredHook(hookType, callback));
        }

        private void CleanupHooks(CombatResolver combat)
        {
            foreach (var hook in _activeHooks)
                combat.UnregisterHook(hook.HookType, hook.Callback);
            _activeHooks.Clear();
        }

        // ── Helpers ──

        private static int GetInt(Dictionary<string, object> data, string key, int def)
        {
            return data.TryGetValue(key, out var v) ? Convert.ToInt32(v) : def;
        }

        private static float GetFloat(Dictionary<string, object> data, string key, float def)
        {
            return data.TryGetValue(key, out var v) ? Convert.ToSingle(v) : def;
        }

        private static List<int> GetIntList(Dictionary<string, object> data, string key)
        {
            var result = new List<int>();
            if (data.TryGetValue(key, out var v) && v is List<object> list)
            {
                foreach (var item in list)
                    result.Add(Convert.ToInt32(item));
            }
            return result;
        }

        private struct RegisteredHook
        {
            public DamageHook HookType;
            public Action<DamageContext> Callback;

            public RegisteredHook(DamageHook hookType, Action<DamageContext> callback)
            {
                HookType = hookType;
                Callback = callback;
            }
        }
    }
}
