#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using MobileForge.Domain;
using TowerOfSaviors;

namespace TowerOfSaviors.Tests
{
    [TestFixture]
    public class TosNewSkillTests
    {
        private SkillPipeline _pipeline;

        [SetUp]
        public void SetUp()
        {
            _pipeline = new SkillPipeline();
            TosSkillRegistration.Register(_pipeline);
        }

        [Test]
        public void AllSkillJsonTypes_AreRegistered()
        {
            var jsonPath = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "..", "..", "..", "..", "..", "..",
                "games", "tower-of-saviors", "shared", "data", "skills.json");

            if (!File.Exists(jsonPath))
            {
                Assert.Ignore($"skills.json not found: {jsonPath}");
                return;
            }

            var json = File.ReadAllText(jsonPath);
            var skills = JsonConvert.DeserializeObject<JArray>(json);

            var conditionTypes = new HashSet<string>();
            var effectTypes = new HashSet<string>();

            if (skills != null)
            {
                foreach (var skill in skills)
                {
                    var rules = skill["rules"] as JArray;
                    if (rules == null) continue;
                    foreach (var rule in rules)
                    {
                        var conditions = rule["conditions"] as JArray;
                        if (conditions != null)
                        {
                            foreach (var cond in conditions)
                            {
                                var type = cond["type"]?.ToString();
                                if (type != null) conditionTypes.Add(type);
                            }
                        }
                        var outcomes = rule["outcomes"] as JArray;
                        if (outcomes != null)
                        {
                            foreach (var outcome in outcomes)
                            {
                                var type = outcome["type"]?.ToString();
                                if (type != null) effectTypes.Add(type);
                            }
                        }
                    }
                }
            }

            foreach (var condType in conditionTypes)
            {
                Assert.IsTrue(_pipeline.ConditionRegistry.HasType(condType),
                    $"Condition '{condType}' should be registered");
            }

            foreach (var effectType in effectTypes)
            {
                Assert.IsTrue(_pipeline.EffectRegistry.HasEffect(effectType) ||
                    _pipeline.OutcomeRegistry.HasType(effectType),
                    $"Effect/Outcome '{effectType}' should be registered");
            }
        }

        [Test]
        public void AllTeamSkillJsonTypes_AreRegistered()
        {
            var jsonPath = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "..", "..", "..", "..", "..", "..",
                "games", "tower-of-saviors", "shared", "data", "team_skills.json");

            if (!File.Exists(jsonPath))
            {
                Assert.Ignore($"team_skills.json not found: {jsonPath}");
                return;
            }

            var json = File.ReadAllText(jsonPath);
            var teamSkills = JsonConvert.DeserializeObject<JArray>(json);

            var conditionTypes = new HashSet<string>();

            if (teamSkills != null)
            {
                foreach (var skill in teamSkills)
                {
                    var rules = skill["rules"] as JArray;
                    if (rules == null) continue;
                    foreach (var rule in rules)
                    {
                        var conditions = rule["conditions"] as JArray;
                        if (conditions != null)
                        {
                            foreach (var cond in conditions)
                            {
                                var type = cond["type"]?.ToString();
                                if (type != null) conditionTypes.Add(type);
                            }
                        }
                    }
                }
            }

            foreach (var condType in conditionTypes)
            {
                Assert.IsTrue(_pipeline.ConditionRegistry.HasType(condType),
                    $"Team skill condition '{condType}' should be registered");
            }
        }

        [Test]
        public void EnemyAI_HeavyAttack_DoublesEvery3rd()
        {
            var enemy = new EnemyState(new Dictionary<string, object>
            {
                { "name", "Heavy Attacker" },
                { "hp", 1000 },
                { "atk", 100 },
                { "countdown", 1 },
                { "element", 1 },
                { "behavior", "heavy_attack" },
            });

            var action1 = EnemyAI.DecideAction(enemy);
            Assert.AreEqual("attack", action1.Type);
            Assert.AreEqual(100, action1.Damage);

            var action2 = EnemyAI.DecideAction(enemy);
            Assert.AreEqual(100, action2.Damage);

            var action3 = EnemyAI.DecideAction(enemy);
            Assert.AreEqual(200, action3.Damage, "3rd attack should be 2x");

            var action4 = EnemyAI.DecideAction(enemy);
            Assert.AreEqual(100, action4.Damage, "4th attack should be normal");
        }

        [Test]
        public void EnemyAI_HealSelf_Below30Percent()
        {
            var enemy = new EnemyState(new Dictionary<string, object>
            {
                { "name", "Healer" },
                { "hp", 300 },
                { "max_hp", 1000 },
                { "atk", 100 },
                { "countdown", 1 },
                { "element", 1 },
                { "behavior", "heal_self" },
            });

            var action = EnemyAI.DecideAction(enemy);

            Assert.AreEqual("heal", action.Type, "Should heal when HP < 30%");
            Assert.AreEqual(200, action.Extra["heal_amount"], "Should heal 20% of max HP");
        }

        [Test]
        public void EnemyAI_BuffAllies_OnceOnly()
        {
            var enemy = new EnemyState(new Dictionary<string, object>
            {
                { "name", "Buffer" },
                { "hp", 1000 },
                { "atk", 100 },
                { "countdown", 1 },
                { "element", 1 },
                { "behavior", "buff_allies" },
            });

            Assert.IsFalse(enemy.HasUsedBuff, "Should start with has_used_buff = false");

            var action1 = EnemyAI.DecideAction(enemy);
            Assert.AreEqual("buff", action1.Type, "First action should be buff");
            Assert.IsTrue(enemy.HasUsedBuff, "Should mark buff as used");

            var action2 = EnemyAI.DecideAction(enemy);
            Assert.AreEqual("attack", action2.Type, "Subsequent actions should be attacks");

            var action3 = EnemyAI.DecideAction(enemy);
            Assert.AreEqual("attack", action3.Type);
        }
    }
}
#endif
