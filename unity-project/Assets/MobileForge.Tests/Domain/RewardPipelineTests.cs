using System;
using System.Collections.Generic;
using NUnit.Framework;
using MobileForge.Domain;

namespace MobileForge.Tests.Domain
{
    [TestFixture]
    public class RewardPipelineTests
    {
        private RewardPipeline _pipeline;
        private List<string> _emittedEvents;
        private List<string> _grantedCurrencies;
        private List<int> _grantedMonsters;
        private List<(string id, int count)> _grantedItems;
        private int _grantedStamina;

        [SetUp]
        public void SetUp()
        {
            _emittedEvents = new List<string>();
            _grantedCurrencies = new List<string>();
            _grantedMonsters = new List<int>();
            _grantedItems = new List<(string, int)>();
            _grantedStamina = 0;

            _pipeline = new RewardPipeline((evt, payload) => _emittedEvents.Add(evt));
            _pipeline.OnGrantCurrency = (id, count) => _grantedCurrencies.Add($"{id}:{count}");
            _pipeline.OnGrantMonster = (id) => _grantedMonsters.Add(id);
            _pipeline.OnGrantItem = (id, count) => _grantedItems.Add((id, count));
            _pipeline.OnGrantStamina = (amount) => _grantedStamina += amount;
        }

        [Test]
        public void Grant_Currency_CallsHandler()
        {
            var rewards = new List<Dictionary<string, object>>
            {
                new() { ["type"] = "currency", ["currency"] = "coins", ["count"] = 100 },
            };

            var results = _pipeline.Grant(rewards, "test");

            Assert.AreEqual(1, results.Count);
            Assert.IsTrue((bool)results[0]["success"]);
            Assert.AreEqual("coins", results[0]["currency"]);
            Assert.Contains("coins:100", _grantedCurrencies);
        }

        [Test]
        public void Grant_Currency_WithIdField()
        {
            var rewards = new List<Dictionary<string, object>>
            {
                new() { ["type"] = "currency", ["id"] = "gems", ["count"] = 50 },
            };

            var results = _pipeline.Grant(rewards, "test");

            Assert.IsTrue((bool)results[0]["success"]);
            Assert.AreEqual("gems", results[0]["currency"]);
        }

        [Test]
        public void Grant_Monster_CallsHandler()
        {
            var rewards = new List<Dictionary<string, object>>
            {
                new() { ["type"] = "monster", ["id"] = 42 },
            };

            var results = _pipeline.Grant(rewards, "gacha");

            Assert.IsTrue((bool)results[0]["success"]);
            Assert.AreEqual(42, results[0]["monster_id"]);
            Assert.Contains(42, _grantedMonsters);
        }

        [Test]
        public void Grant_Item_CallsHandler()
        {
            var rewards = new List<Dictionary<string, object>>
            {
                new() { ["type"] = "item", ["id"] = "potion", ["count"] = 5 },
            };

            var results = _pipeline.Grant(rewards, "shop");

            Assert.IsTrue((bool)results[0]["success"]);
            Assert.AreEqual("potion", results[0]["item_id"]);
            Assert.Contains(("potion", 5), _grantedItems);
        }

        [Test]
        public void Grant_Stamina_CallsHandler()
        {
            var rewards = new List<Dictionary<string, object>>
            {
                new() { ["type"] = "stamina", ["count"] = 30 },
            };

            var results = _pipeline.Grant(rewards, "daily");

            Assert.IsTrue((bool)results[0]["success"]);
            Assert.AreEqual(30, _grantedStamina);
        }

        [Test]
        public void Grant_MultipleRewards_ProcessesAll()
        {
            var rewards = new List<Dictionary<string, object>>
            {
                new() { ["type"] = "currency", ["id"] = "coins", ["count"] = 100 },
                new() { ["type"] = "monster", ["id"] = 1 },
                new() { ["type"] = "item", ["id"] = "key", ["count"] = 1 },
            };

            var results = _pipeline.Grant(rewards, "quest");

            Assert.AreEqual(3, results.Count);
            Assert.AreEqual(3, _emittedEvents.Count);
        }

        [Test]
        public void Grant_UnknownType_StillSucceeds()
        {
            var rewards = new List<Dictionary<string, object>>
            {
                new() { ["type"] = "unknown", ["count"] = 1 },
            };

            var results = _pipeline.Grant(rewards, "test");

            Assert.IsTrue((bool)results[0]["success"]);
        }

        [Test]
        public void Grant_FiresEvent()
        {
            var rewards = new List<Dictionary<string, object>>
            {
                new() { ["type"] = "currency", ["id"] = "coins", ["count"] = 100 },
            };

            _pipeline.Grant(rewards, "daily_login");

            Assert.Contains("rewards_granted", _emittedEvents);
        }

        [Test]
        public void Grant_DefaultCount_IsOne()
        {
            var rewards = new List<Dictionary<string, object>>
            {
                new() { ["type"] = "currency", ["id"] = "coins" },
            };

            var results = _pipeline.Grant(rewards, "test");

            Assert.AreEqual(1, results[0]["count"]);
        }

        [Test]
        public void AddTransform_ModifiesReward()
        {
            _pipeline.AddTransform(reward =>
            {
                if (reward.TryGetValue("count", out var c))
                {
                    reward["count"] = Convert.ToInt32(c) * 2;
                }
                return reward;
            });

            var rewards = new List<Dictionary<string, object>>
            {
                new() { ["type"] = "currency", ["id"] = "coins", ["count"] = 100 },
            };

            var results = _pipeline.Grant(rewards, "bonus");

            Assert.AreEqual(200, results[0]["count"]);
            Assert.Contains("coins:200", _grantedCurrencies);
        }

        [Test]
        public void AddTransform_MultipleTransforms_Chain()
        {
            _pipeline.AddTransform(reward =>
            {
                reward["count"] = Convert.ToInt32(reward.GetValueOrDefault("count", 1)) * 2;
                return reward;
            });
            _pipeline.AddTransform(reward =>
            {
                reward["count"] = Convert.ToInt32(reward["count"]) + 10;
                return reward;
            });

            var rewards = new List<Dictionary<string, object>>
            {
                new() { ["type"] = "currency", ["id"] = "coins", ["count"] = 100 },
            };

            var results = _pipeline.Grant(rewards, "chain");

            Assert.AreEqual(210, results[0]["count"]);
        }

        [Test]
        public void Grant_NoHandlers_StillSucceeds()
        {
            var noHandlerPipeline = new RewardPipeline();
            var rewards = new List<Dictionary<string, object>>
            {
                new() { ["type"] = "currency", ["id"] = "coins", ["count"] = 100 },
            };

            var results = noHandlerPipeline.Grant(rewards, "test");

            Assert.IsTrue((bool)results[0]["success"]);
        }

        [Test]
        public void Result_ContainsSource()
        {
            var rewards = new List<Dictionary<string, object>>
            {
                new() { ["type"] = "currency", ["id"] = "coins", ["count"] = 100 },
            };

            var results = _pipeline.Grant(rewards, "special_event");

            Assert.AreEqual("special_event", results[0]["source"]);
        }
    }
}
