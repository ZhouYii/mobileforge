using System;
using System.Collections.Generic;
using NUnit.Framework;
using MobileForge.Domain;

namespace MobileForge.Tests.Domain
{
    [TestFixture]
    public class BattlePassTests
    {
        private BattlePass _battlePass;
        private BattlePassDef _def;
        private List<Dictionary<string, object>> _grantedRewards;
        private List<string> _emittedEvents;

        [SetUp]
        public void SetUp()
        {
            _grantedRewards = new List<Dictionary<string, object>>();
            _emittedEvents = new List<string>();

            var rewardPipeline = new RewardPipeline((evt, payload) => _emittedEvents.Add(evt));
            rewardPipeline.OnGrantCurrency = (id, count) => _grantedRewards.Add(new Dictionary<string, object> { ["type"] = "currency", ["id"] = id, ["count"] = count });

            _battlePass = new BattlePass(rewardPipeline, (evt, payload) => _emittedEvents.Add(evt));

            _def = new BattlePassDef
            {
                Id = "season_1",
                XpPerTier = 100,
                Tiers = new List<BattlePassTier>
                {
                    new() { Tier = 0, FreeRewards = new List<Dictionary<string, object>> { new() { ["type"] = "currency", ["id"] = "coins", ["count"] = 100 } }, PremiumRewards = new List<Dictionary<string, object>> { new() { ["type"] = "currency", ["id"] = "gems", ["count"] = 50 } } },
                    new() { Tier = 1, FreeRewards = new List<Dictionary<string, object>> { new() { ["type"] = "currency", ["id"] = "coins", ["count"] = 200 } }, PremiumRewards = new List<Dictionary<string, object>> { new() { ["type"] = "currency", ["id"] = "gems", ["count"] = 100 } } },
                    new() { Tier = 2, FreeRewards = new List<Dictionary<string, object>>(), PremiumRewards = new List<Dictionary<string, object>>() },
                },
            };
        }

        [Test]
        public void AddXp_IncreasesXp()
        {
            _battlePass.AddXp(_def, 50);
            Assert.AreEqual(50, _battlePass.Xp);
        }

        [Test]
        public void AddXp_ReturnsTiersGained()
        {
            int tiers = _battlePass.AddXp(_def, 250);
            Assert.AreEqual(2, tiers);
            Assert.AreEqual(250, _battlePass.Xp);
        }

        [Test]
        public void AddXp_FiresTierUpEvent()
        {
            _battlePass.AddXp(_def, 100);
            Assert.Contains("battle_pass_tier_up", _emittedEvents);
        }

        [Test]
        public void GetTier_CalculatesCorrectly()
        {
            _battlePass.AddXp(_def, 150);
            Assert.AreEqual(1, _battlePass.GetTier(_def));
        }

        [Test]
        public void GetTier_ClampedToMaxTier()
        {
            _battlePass.AddXp(_def, 500);
            Assert.AreEqual(2, _battlePass.GetTier(_def));
        }

        [Test]
        public void Claim_FreeTrack_GrantsRewards()
        {
            _battlePass.AddXp(_def, 100);

            var result = _battlePass.Claim(_def, 0, "free");
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual(1, _grantedRewards.Count);
            Assert.Contains("battle_pass_claimed", _emittedEvents);
        }

        [Test]
        public void Claim_PremiumTrack_RequiresPremium()
        {
            _battlePass.AddXp(_def, 100);

            var result = _battlePass.Claim(_def, 0, "premium");
            Assert.IsFalse((bool)result["success"]);
            Assert.AreEqual("premium_required", result["error"]);
        }

        [Test]
        public void Claim_PremiumTrack_WithPremium_GrantsRewards()
        {
            _battlePass.ActivatePremium();
            _battlePass.AddXp(_def, 100);

            var result = _battlePass.Claim(_def, 0, "premium");
            Assert.IsTrue((bool)result["success"]);
            Assert.AreEqual("gems", _grantedRewards[0]["id"]);
        }

        [Test]
        public void Claim_FailsForUnreachedTier()
        {
            var result = _battlePass.Claim(_def, 2, "free");
            Assert.IsFalse((bool)result["success"]);
            Assert.AreEqual("tier_not_reached", result["error"]);
        }

        [Test]
        public void Claim_FailsIfAlreadyClaimed()
        {
            _battlePass.AddXp(_def, 100);
            _battlePass.Claim(_def, 0, "free");

            var result = _battlePass.Claim(_def, 0, "free");
            Assert.IsFalse((bool)result["success"]);
            Assert.AreEqual("already_claimed", result["error"]);
        }

        [Test]
        public void ActivatePremium_SetsFlag()
        {
            Assert.IsFalse(_battlePass.IsPremium);
            _battlePass.ActivatePremium();
            Assert.IsTrue(_battlePass.IsPremium);
            Assert.Contains("battle_pass_premium_activated", _emittedEvents);
        }

        [Test]
        public void LoadState_RestoresState()
        {
            _battlePass.LoadState(350, true, new List<string> { "free_0", "premium_1" });

            Assert.AreEqual(350, _battlePass.Xp);
            Assert.IsTrue(_battlePass.IsPremium);

            var result = _battlePass.Claim(_def, 0, "free");
            Assert.IsFalse((bool)result["success"]);

            result = _battlePass.Claim(_def, 1, "premium");
            Assert.IsFalse((bool)result["success"]);
        }

        [Test]
        public void Claim_FailsForInvalidTier()
        {
            _battlePass.AddXp(_def, 1000);
            var result = _battlePass.Claim(_def, 10, "free");
            Assert.IsFalse((bool)result["success"]);
            Assert.AreEqual("invalid_tier", result["error"]);
        }
    }
}
