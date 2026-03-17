using System;
using System.Collections.Generic;
using NUnit.Framework;
using MobileForge.Domain;

namespace MobileForge.Tests.Domain
{
    [TestFixture]
    public class SkillPipelineTests
    {
        private SkillPipeline _pipeline;

        // -----------------------------------------------------------------
        // Test condition/outcome subclasses
        // -----------------------------------------------------------------

        /// <summary>Always-true condition: IsValid returns true unconditionally.</summary>
        private class AlwaysTrueCondition : SkillConditionBase
        {
            public AlwaysTrueCondition(Dictionary<string, object> p = null) : base(p) { }
            public override bool IsValid(SkillContext context) => true;
        }

        /// <summary>Checks context.ComboCount >= params["threshold"].</summary>
        private class ComboAboveCondition : SkillConditionBase
        {
            public ComboAboveCondition(Dictionary<string, object> p = null) : base(p) { }
            public override bool IsValid(SkillContext context)
            {
                int threshold = GetParam<int>("threshold", 0);
                return context.ComboCount >= threshold;
            }
        }

        /// <summary>Persistent test outcome: tracks activation in result.BuffsApplied.</summary>
        private class DamageBuff : SkillOutcomeBase
        {
            public DamageBuff(Dictionary<string, object> p = null) : base(p) { }
            public override void Activate(SkillContext context, SkillResult result)
            {
                result.BuffsApplied.Add(new Dictionary<string, object>
                {
                    ["type"] = "damage_buff",
                    ["turns"] = TurnsLeft,
                    ["target"] = "team",
                });
            }
        }

        // -----------------------------------------------------------------
        // Simple effect callback
        // -----------------------------------------------------------------

        private static void HealFlatEffect(Dictionary<string, object> parms, SkillContext ctx, SkillResult result)
        {
            int amount = parms.TryGetValue("amount", out var v) ? Convert.ToInt32(v) : 0;
            result.Healing += amount;
        }

        // -----------------------------------------------------------------
        // Setup
        // -----------------------------------------------------------------

        [SetUp]
        public void SetUp()
        {
            _pipeline = new SkillPipeline();

            // Register test condition types
            _pipeline.ConditionRegistry.Register("always_true",
                p => new AlwaysTrueCondition(p));
            _pipeline.ConditionRegistry.Register("combo_above",
                p => new ComboAboveCondition(p));

            // Register test simple effect
            _pipeline.EffectRegistry.Register("heal_flat", HealFlatEffect);

            // Register test outcome type
            _pipeline.OutcomeRegistry.Register("damage_buff",
                p => new DamageBuff(p));
        }

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

        private static SkillContext MakeContext(int combo = 0)
        {
            return new SkillContext { ComboCount = combo };
        }

        private static SkillDef MakeSkillDef(List<object> rulesData)
        {
            return new SkillDef(new Dictionary<string, object>
            {
                ["id"] = 1,
                ["name"] = "Test Skill",
                ["type"] = "active",
                ["max_cd"] = 10,
                ["min_cd"] = 5,
                ["max_level"] = 5,
                ["rules"] = rulesData,
            });
        }

        private static Dictionary<string, object> MakeRule(
            List<object> conditions, List<object> outcomes)
        {
            return new Dictionary<string, object>
            {
                ["conditions"] = conditions,
                ["outcomes"] = outcomes,
            };
        }

        // -----------------------------------------------------------------
        // Tests -- registration
        // -----------------------------------------------------------------

        [Test]
        public void Register_Condition_Type()
        {
            Assert.IsTrue(_pipeline.ConditionRegistry.HasType("always_true"),
                "always_true condition should be registered");
            Assert.IsTrue(_pipeline.ConditionRegistry.HasType("combo_above"),
                "combo_above condition should be registered");
            Assert.IsFalse(_pipeline.ConditionRegistry.HasType("nonexistent"),
                "nonexistent type should not be registered");
        }

        [Test]
        public void Register_Outcome_Type()
        {
            Assert.IsTrue(_pipeline.OutcomeRegistry.HasType("damage_buff"),
                "damage_buff outcome should be registered");
            Assert.IsFalse(_pipeline.OutcomeRegistry.HasType("nonexistent"),
                "nonexistent outcome should not be registered");
        }

        [Test]
        public void Register_Simple_Effect()
        {
            Assert.IsTrue(_pipeline.EffectRegistry.HasEffect("heal_flat"),
                "heal_flat effect should be registered");
            Assert.IsFalse(_pipeline.EffectRegistry.HasEffect("nonexistent"),
                "nonexistent effect should not be registered");
        }

        // -----------------------------------------------------------------
        // Tests -- activation
        // -----------------------------------------------------------------

        [Test]
        public void Activate_Skill_Simple_Effect()
        {
            var def = MakeSkillDef(new List<object>
            {
                MakeRule(
                    new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["type"] = "always_true",
                            ["params"] = new Dictionary<string, object>()
                        }
                    },
                    new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["type"] = "heal_flat",
                            ["params"] = new Dictionary<string, object> { ["amount"] = 500 }
                        }
                    })
            });

            var ctx = MakeContext();
            var result = _pipeline.ActivateSkill(def, ctx);

            Assert.AreEqual(500, result.Healing, "heal_flat should set healing to 500");
        }

        [Test]
        public void Activate_Skill_Condition_Not_Met()
        {
            var def = MakeSkillDef(new List<object>
            {
                MakeRule(
                    new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["type"] = "combo_above",
                            ["params"] = new Dictionary<string, object> { ["threshold"] = 5 }
                        }
                    },
                    new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["type"] = "heal_flat",
                            ["params"] = new Dictionary<string, object> { ["amount"] = 999 }
                        }
                    })
            });

            var ctx = MakeContext(3);
            var result = _pipeline.ActivateSkill(def, ctx);

            Assert.AreEqual(0, result.Healing, "Condition not met -> no healing");
        }

        [Test]
        public void Activate_Skill_Condition_Met()
        {
            var def = MakeSkillDef(new List<object>
            {
                MakeRule(
                    new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["type"] = "combo_above",
                            ["params"] = new Dictionary<string, object> { ["threshold"] = 5 }
                        }
                    },
                    new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["type"] = "heal_flat",
                            ["params"] = new Dictionary<string, object> { ["amount"] = 750 }
                        }
                    })
            });

            var ctx = MakeContext(5);
            var result = _pipeline.ActivateSkill(def, ctx);

            Assert.AreEqual(750, result.Healing, "Condition met -> healing should be 750");
        }

        // -----------------------------------------------------------------
        // Tests -- persistent outcomes
        // -----------------------------------------------------------------

        [Test]
        public void Persistent_Outcome_Tracked()
        {
            var def = MakeSkillDef(new List<object>
            {
                MakeRule(
                    new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["type"] = "always_true",
                            ["params"] = new Dictionary<string, object>()
                        }
                    },
                    new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["type"] = "damage_buff",
                            ["params"] = new Dictionary<string, object>(),
                            ["duration"] = 3
                        }
                    })
            });

            var ctx = MakeContext();
            _pipeline.ActivateSkill(def, ctx);

            Assert.AreEqual(1, _pipeline.ActiveOutcomeCount,
                "Persistent outcome should be tracked");
        }

        [Test]
        public void Process_Turn_End_Ticks_Duration()
        {
            var def = MakeSkillDef(new List<object>
            {
                MakeRule(
                    new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["type"] = "always_true",
                            ["params"] = new Dictionary<string, object>()
                        }
                    },
                    new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["type"] = "damage_buff",
                            ["params"] = new Dictionary<string, object>(),
                            ["duration"] = 3
                        }
                    })
            });

            var ctx = MakeContext();
            _pipeline.ActivateSkill(def, ctx);
            Assert.AreEqual(1, _pipeline.ActiveOutcomeCount, "Start with 1 active outcome");

            _pipeline.ProcessTurnEnd(ctx);
            Assert.AreEqual(1, _pipeline.ActiveOutcomeCount, "After 1 turn, still active (2 left)");

            _pipeline.ProcessTurnEnd(ctx);
            Assert.AreEqual(1, _pipeline.ActiveOutcomeCount, "After 2 turns, still active (1 left)");

            _pipeline.ProcessTurnEnd(ctx);
            Assert.AreEqual(0, _pipeline.ActiveOutcomeCount, "After 3 turns, outcome should expire");
        }

        [Test]
        public void Deactivate_Skill()
        {
            var def = MakeSkillDef(new List<object>
            {
                MakeRule(
                    new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["type"] = "always_true",
                            ["params"] = new Dictionary<string, object>()
                        }
                    },
                    new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["type"] = "damage_buff",
                            ["params"] = new Dictionary<string, object>(),
                            ["duration"] = 5
                        }
                    })
            });

            var ctx = MakeContext();
            _pipeline.ActivateSkill(def, ctx);
            Assert.AreEqual(1, _pipeline.ActiveOutcomeCount, "Should have 1 active outcome");

            _pipeline.DeactivateSkill(1, ctx);
            Assert.AreEqual(0, _pipeline.ActiveOutcomeCount, "After deactivate, should be 0");
        }

        [Test]
        public void Clear_Active_Outcomes()
        {
            var defA = new SkillDef(new Dictionary<string, object>
            {
                ["id"] = 10, ["name"] = "A",
                ["rules"] = new List<object>
                {
                    MakeRule(
                        new List<object>
                        {
                            new Dictionary<string, object>
                            {
                                ["type"] = "always_true",
                                ["params"] = new Dictionary<string, object>()
                            }
                        },
                        new List<object>
                        {
                            new Dictionary<string, object>
                            {
                                ["type"] = "damage_buff",
                                ["params"] = new Dictionary<string, object>(),
                                ["duration"] = 5
                            }
                        })
                }
            });

            var defB = new SkillDef(new Dictionary<string, object>
            {
                ["id"] = 20, ["name"] = "B",
                ["rules"] = new List<object>
                {
                    MakeRule(
                        new List<object>
                        {
                            new Dictionary<string, object>
                            {
                                ["type"] = "always_true",
                                ["params"] = new Dictionary<string, object>()
                            }
                        },
                        new List<object>
                        {
                            new Dictionary<string, object>
                            {
                                ["type"] = "damage_buff",
                                ["params"] = new Dictionary<string, object>(),
                                ["duration"] = 5
                            }
                        })
                }
            });

            var ctx = MakeContext();
            _pipeline.ActivateSkill(defA, ctx);
            _pipeline.ActivateSkill(defB, ctx);
            Assert.AreEqual(2, _pipeline.ActiveOutcomeCount, "Should have 2 active outcomes");

            _pipeline.ClearActiveOutcomes(ctx);
            Assert.AreEqual(0, _pipeline.ActiveOutcomeCount, "After clear, should be 0");
        }

        // -----------------------------------------------------------------
        // Tests -- cooldown
        // -----------------------------------------------------------------

        [Test]
        public void Skill_Cooldown_Calculate()
        {
            // calculate_cd(max_cd=10, min_cd=5, skill_level=1) = max(10+1-1, 5) = 10
            Assert.AreEqual(10, SkillCooldown.CalculateCd(10, 5, 1),
                "skill_level=1: cd should be 10");
            // calculate_cd(max_cd=10, min_cd=5, skill_level=6) = max(10+1-6, 5) = 5
            Assert.AreEqual(5, SkillCooldown.CalculateCd(10, 5, 6),
                "skill_level=6: cd should be min_cd=5");
            // calculate_cd(max_cd=10, min_cd=5, skill_level=10) = max(10+1-10, 5) = 5
            Assert.AreEqual(5, SkillCooldown.CalculateCd(10, 5, 10),
                "skill_level=10: cd should clamp to min_cd=5");
        }

        [Test]
        public void Skill_Cooldown_Tick()
        {
            Assert.AreEqual(2, SkillCooldown.Tick(3), "tick(3) should return 2");
            Assert.AreEqual(0, SkillCooldown.Tick(1), "tick(1) should return 0");
            Assert.AreEqual(0, SkillCooldown.Tick(0), "tick(0) should return 0 (floor)");
        }
    }
}
