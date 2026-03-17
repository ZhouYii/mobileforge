using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using MobileForge.Domain;

namespace MobileForge.Tests.Domain
{
    [TestFixture]
    public class SkillPipelineVectorTests
    {
        private SkillPipeline _pipeline;

        [SetUp]
        public void SetUp()
        {
            _pipeline = new SkillPipeline();
            RegisterConditions();
            RegisterEffects();
        }

        private void RegisterConditions()
        {
            _pipeline.ConditionRegistry.Register("always_true", p => new AlwaysTrueConditionTest(p));
            _pipeline.ConditionRegistry.Register("combo_above", p => new ComboAboveConditionTest(p));
            _pipeline.ConditionRegistry.Register("hp_below", p => new HpBelowConditionTest(p));
            _pipeline.ConditionRegistry.Register("elements_matched", p => new ElementsMatchedConditionTest(p));
        }

        private void RegisterEffects()
        {
            _pipeline.EffectRegistry.Register("heal_flat", (p, ctx, result) =>
            {
                result.Healing += Convert.ToInt32(p.GetValueOrDefault("amount", 0));
            });
            _pipeline.EffectRegistry.Register("heal_percent", (p, ctx, result) =>
            {
                float pct = Convert.ToSingle(p.GetValueOrDefault("percent", 0f));
                result.Healing += (int)(ctx.MaxHp * pct);
            });
            _pipeline.EffectRegistry.Register("area_damage", (p, ctx, result) =>
            {
                float mult = Convert.ToSingle(p.GetValueOrDefault("multiplier", 1.0f));
                float atk = 0f;
                if (ctx.TeamStats != null && ctx.TeamStats.Count > 0 && ctx.TeamStats[0] is MonsterStats stats)
                    atk = stats.Atk;
                for (int i = 0; i < ctx.Enemies.Count; i++)
                {
                    if (ctx.Enemies[i] is EnemyState enemy && enemy.IsAlive)
                        result.DamageDealt[i] = (int)(atk * mult);
                }
            });
            _pipeline.EffectRegistry.Register("delay_enemies", (p, ctx, result) =>
            {
                int turns = Convert.ToInt32(p.GetValueOrDefault("turns", 1));
                foreach (var enemyObj in ctx.Enemies)
                {
                    if (enemyObj is EnemyState enemy && enemy.IsAlive)
                        enemy.Countdown += turns;
                }
            });
        }

        [Test]
        public void RunSkillPipelineTestVectors()
        {
            var jsonPath = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "..", "..", "..", "..", "..", "..",
                "framework", "shared", "test_vectors", "skill_pipeline_cases.json");

            if (!File.Exists(jsonPath))
            {
                Assert.Ignore($"Test vector file not found: {jsonPath}");
                return;
            }

            var json = File.ReadAllText(jsonPath);
            var doc = JObject.Parse(json);
            var cases = (JArray)doc["cases"];

            foreach (var testCase in cases)
            {
                string name = testCase["name"].Value<string>();
                var skillDefData = ParseSkillDef((JObject)testCase["skill_def"]);
                var contextData = ParseContext((JObject)testCase["context"]);
                var expected = testCase["expected"];

                var skillDef = new SkillDef(skillDefData);
                var context = BuildContext(contextData);
                var result = _pipeline.ActivateSkill(skillDef, context);

                bool expectedSuccess = expected["success"].Value<bool>();
                if (expectedSuccess)
                {
                    if (expected["healing"] != null)
                    {
                        int expectedHealing = expected["healing"].Value<int>();
                        Assert.AreEqual(expectedHealing, result.Healing,
                            $"{name}: healing should be {expectedHealing}");
                    }
                }
                else
                {
                    Assert.AreEqual(0, result.Healing, $"{name}: should not heal when conditions fail");
                }

                TestContext.WriteLine($"[PASS] {name}");
            }
        }

        private Dictionary<string, object> ParseSkillDef(JObject def)
        {
            var result = new Dictionary<string, object>();
            result["id"] = def["id"].Value<int>();

            var rules = new List<Dictionary<string, object>>();
            foreach (var rule in (JArray)def["rules"])
            {
                var ruleDict = new Dictionary<string, object>();
                var conditions = new List<Dictionary<string, object>>();
                foreach (var cond in (JArray)rule["conditions"])
                {
                    conditions.Add(ParseParams((JObject)cond));
                }
                ruleDict["conditions"] = conditions;

                var outcomes = new List<Dictionary<string, object>>();
                foreach (var outcome in (JArray)rule["outcomes"])
                {
                    outcomes.Add(ParseParams((JObject)outcome));
                }
                ruleDict["outcomes"] = outcomes;
                rules.Add(ruleDict);
            }
            result["rules"] = rules;

            return result;
        }

        private Dictionary<string, object> ParseParams(JObject elem)
        {
            var result = new Dictionary<string, object>();
            result["type"] = elem["type"].Value<string>();
            var paramsToken = elem["params"];
            if (paramsToken != null && paramsToken is JObject paramsObj)
            {
                foreach (var prop in paramsObj.Properties())
                {
                    if (prop.Value.Type == JTokenType.Integer)
                        result[prop.Name] = prop.Value.Value<int>();
                    else if (prop.Value.Type == JTokenType.Float)
                        result[prop.Name] = prop.Value.Value<double>();
                    else
                        result[prop.Name] = prop.Value.Value<string>();
                }
            }
            return result;
        }

        private Dictionary<string, object> ParseContext(JObject ctx)
        {
            var result = new Dictionary<string, object>();
            result["team_hp"] = ctx["team_hp"].Value<int>();
            result["max_hp"] = ctx["max_hp"].Value<int>();
            result["combo_count"] = ctx["combo_count"].Value<int>();

            var elementsMatched = new Dictionary<int, int>();
            var elems = ctx["elements_matched"];
            if (elems != null && elems is JObject elemsObj)
            {
                foreach (var prop in elemsObj.Properties())
                {
                    elementsMatched[int.Parse(prop.Name)] = prop.Value.Value<int>();
                }
            }
            result["elements_matched"] = elementsMatched;

            return result;
        }

        private SkillContext BuildContext(Dictionary<string, object> data)
        {
            var ctx = new SkillContext
            {
                TeamHp = Convert.ToInt32(data["team_hp"]),
                MaxHp = Convert.ToInt32(data["max_hp"]),
                ComboCount = Convert.ToInt32(data["combo_count"]),
                ElementsMatched = data.TryGetValue("elements_matched", out var em)
                    ? (Dictionary<int, int>)em : new Dictionary<int, int>(),
                Enemies = new List<object>(),
                TeamStats = new List<object>(),
            };
            return ctx;
        }
    }

    internal class AlwaysTrueConditionTest : SkillConditionBase
    {
        public AlwaysTrueConditionTest(Dictionary<string, object> p) : base(p) { }
        public override bool IsValid(SkillContext ctx) => true;
    }

    internal class ComboAboveConditionTest : SkillConditionBase
    {
        public ComboAboveConditionTest(Dictionary<string, object> p) : base(p) { }
        public override bool IsValid(SkillContext ctx) => ctx.ComboCount >= GetParamInt("threshold", 1);

        protected int GetParamInt(string key, int defaultValue)
        {
            if (_params.TryGetValue(key, out var val))
            {
                try { return Convert.ToInt32(val); }
                catch { return defaultValue; }
            }
            return defaultValue;
        }
    }

    internal class HpBelowConditionTest : SkillConditionBase
    {
        public HpBelowConditionTest(Dictionary<string, object> p) : base(p) { }
        public override bool IsValid(SkillContext ctx)
        {
            if (ctx.MaxHp <= 0) return false;
            return (float)ctx.TeamHp / ctx.MaxHp <= GetParamFloat("percent", 0.5f);
        }

        protected float GetParamFloat(string key, float defaultValue)
        {
            if (_params.TryGetValue(key, out var val))
            {
                try { return Convert.ToSingle(val); }
                catch { return defaultValue; }
            }
            return defaultValue;
        }
    }

    internal class ElementsMatchedConditionTest : SkillConditionBase
    {
        public ElementsMatchedConditionTest(Dictionary<string, object> p) : base(p) { }
        public override bool IsValid(SkillContext ctx)
        {
            int elem = GetParamInt("element", 0);
            int minCount = GetParamInt("min_count", 1);
            return ctx.ElementsMatched.TryGetValue(elem, out int count) && count >= minCount;
        }

        protected int GetParamInt(string key, int defaultValue)
        {
            if (_params.TryGetValue(key, out var val))
            {
                try { return Convert.ToInt32(val); }
                catch { return defaultValue; }
            }
            return defaultValue;
        }
    }
}
