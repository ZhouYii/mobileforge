using System;
using System.Collections.Generic;
using NUnit.Framework;
using MobileForge.Domain;

namespace MobileForge.Tests.Domain
{
    [TestFixture]
    public class PlayerLevelTests
    {
        private PlayerLevel _player;
        private PlayerLevelDef _def;

        [SetUp]
        public void SetUp()
        {
            _player = new PlayerLevel();
            _def = new PlayerLevelDef
            {
                MaxLevel = 100,
                ExpCurve = "standard",
                LevelRewards = new List<LevelUpRewardDef>
                {
                    new LevelUpRewardDef
                    {
                        Level = 2,
                        StaminaCapIncrease = 3,
                        TeamCostIncrease = 2,
                        StorageIncrease = 5,
                        FriendSlotsIncrease = 1,
                        BonusRewards = new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object> { ["type"] = "gem", ["amount"] = 1 }
                        }
                    },
                    new LevelUpRewardDef
                    {
                        Level = 5,
                        StaminaCapIncrease = 5,
                        TeamCostIncrease = 3,
                        StorageIncrease = 10,
                        FriendSlotsIncrease = 2,
                    },
                    new LevelUpRewardDef
                    {
                        Level = 10,
                        StaminaCapIncrease = 10,
                        TeamCostIncrease = 5,
                        StorageIncrease = 20,
                        FriendSlotsIncrease = 5,
                    },
                }
            };
        }

        // ── ExpForLevel ──

        [Test]
        public void ExpForLevel_Standard_Returns_Correct_Values()
        {
            // Level 1: Ceil(1^2 * 1.0 * 50) = 50
            Assert.AreEqual(50, PlayerLevel.ExpForLevel(1, "standard"));

            // Level 2: Ceil(2^2 * 1.0 * 50) = 200
            Assert.AreEqual(200, PlayerLevel.ExpForLevel(2, "standard"));

            // Level 10: Ceil(10^2 * 1.0 * 50) = 5000
            Assert.AreEqual(5000, PlayerLevel.ExpForLevel(10, "standard"));
        }

        [Test]
        public void ExpForLevel_Slow_Returns_Higher_Than_Standard()
        {
            int standard = PlayerLevel.ExpForLevel(5, "standard");
            int slow = PlayerLevel.ExpForLevel(5, "slow");
            Assert.Greater(slow, standard, "Slow curve should require more EXP than standard");
        }

        [Test]
        public void ExpForLevel_Fast_Returns_Lower_Than_Standard()
        {
            int standard = PlayerLevel.ExpForLevel(5, "standard");
            int fast = PlayerLevel.ExpForLevel(5, "fast");
            Assert.Less(fast, standard, "Fast curve should require less EXP than standard");
        }

        // ── AddExp: Single Level ──

        [Test]
        public void AddExp_Gains_Single_Level()
        {
            // Need 50 exp to go from level 1 to 2
            int levels = _player.AddExp(_def, 50);

            Assert.AreEqual(1, levels, "Should gain exactly 1 level");
            Assert.AreEqual(2, _player.Level, "Should be level 2");
            Assert.AreEqual(0, _player.Exp, "Leftover exp should be 0");
        }

        // ── AddExp: Multiple Levels ──

        [Test]
        public void AddExp_Gains_Multiple_Levels()
        {
            // Level 1->2 needs 50, level 2->3 needs 200 => total 250 for 2 levels
            int levels = _player.AddExp(_def, 250);

            Assert.AreEqual(2, levels, "Should gain 2 levels");
            Assert.AreEqual(3, _player.Level, "Should be level 3");
            Assert.AreEqual(0, _player.Exp, "Leftover exp should be 0");
        }

        [Test]
        public void AddExp_Partial_Exp_Carries_Over()
        {
            // Give 60 exp: 50 to level up, 10 leftover
            int levels = _player.AddExp(_def, 60);

            Assert.AreEqual(1, levels, "Should gain 1 level");
            Assert.AreEqual(2, _player.Level, "Should be level 2");
            Assert.AreEqual(10, _player.Exp, "Should have 10 leftover exp");
        }

        // ── AddExp: Max Level Cap ──

        [Test]
        public void AddExp_Caps_At_Max_Level()
        {
            var smallDef = new PlayerLevelDef { MaxLevel = 3, ExpCurve = "standard" };
            // Level 1->2 = 50, Level 2->3 = 200, total = 250
            // Give way more than needed
            int levels = _player.AddExp(smallDef, 999999);

            Assert.AreEqual(2, levels, "Should gain exactly 2 levels (max is 3)");
            Assert.AreEqual(3, _player.Level, "Should be at max level 3");
            Assert.AreEqual(0, _player.Exp, "Exp should be zeroed at max level");
        }

        [Test]
        public void AddExp_At_Max_Level_Returns_Zero()
        {
            var smallDef = new PlayerLevelDef { MaxLevel = 3, ExpCurve = "standard" };
            _player.AddExp(smallDef, 999999); // reach max
            int levels = _player.AddExp(smallDef, 1000);

            Assert.AreEqual(0, levels, "Should gain no levels when already at max");
            Assert.AreEqual(3, _player.Level, "Should remain at max level");
        }

        // ── TotalExp Tracking ──

        [Test]
        public void AddExp_Tracks_TotalExp()
        {
            _player.AddExp(_def, 100);
            _player.AddExp(_def, 200);

            Assert.AreEqual(300, _player.TotalExp, "TotalExp should accumulate across calls");
        }

        // ── Stat Caps (Cumulative Rewards) ──

        [Test]
        public void GetStaminaCap_Returns_Base_At_Level_1()
        {
            Assert.AreEqual(50, _player.GetStaminaCap(_def),
                "Stamina cap at level 1 should be base 50");
        }

        [Test]
        public void GetStaminaCap_Accumulates_Rewards()
        {
            // Get to level 5: need 50 + 200 + 450 + 800 = 1500
            _player.AddExp(_def, 1500);
            Assert.AreEqual(5, _player.Level);

            // Base 50 + level 2 reward (3) + level 5 reward (5) = 58
            Assert.AreEqual(58, _player.GetStaminaCap(_def));
        }

        [Test]
        public void GetTeamCostLimit_Accumulates_Rewards()
        {
            _player.AddExp(_def, 1500); // reach level 5
            // Base 50 + level 2 reward (2) + level 5 reward (3) = 55
            Assert.AreEqual(55, _player.GetTeamCostLimit(_def));
        }

        [Test]
        public void GetStorageCapacity_Accumulates_Rewards()
        {
            _player.AddExp(_def, 1500); // reach level 5
            // Base 50 + level 2 reward (5) + level 5 reward (10) = 65
            Assert.AreEqual(65, _player.GetStorageCapacity(_def));
        }

        [Test]
        public void GetFriendSlots_Accumulates_Rewards()
        {
            _player.AddExp(_def, 1500); // reach level 5
            // Base 10 + level 2 reward (1) + level 5 reward (2) = 13
            Assert.AreEqual(13, _player.GetFriendSlots(_def));
        }

        // ── GetLevelRewards ──

        [Test]
        public void GetLevelRewards_Returns_Reward_For_Level()
        {
            var reward = _player.GetLevelRewards(_def, 5);

            Assert.IsNotNull(reward, "Should find reward for level 5");
            Assert.AreEqual(5, reward.Level);
            Assert.AreEqual(5, reward.StaminaCapIncrease);
            Assert.AreEqual(3, reward.TeamCostIncrease);
        }

        [Test]
        public void GetLevelRewards_Returns_Null_For_Missing_Level()
        {
            var reward = _player.GetLevelRewards(_def, 7);
            Assert.IsNull(reward, "Should return null for level with no reward");
        }

        [Test]
        public void GetLevelRewards_BonusRewards_Present()
        {
            var reward = _player.GetLevelRewards(_def, 2);

            Assert.IsNotNull(reward);
            Assert.AreEqual(1, reward.BonusRewards.Count);
            Assert.AreEqual("gem", reward.BonusRewards[0]["type"]);
            Assert.AreEqual(1, reward.BonusRewards[0]["amount"]);
        }

        // ── ExpToNextLevel ──

        [Test]
        public void ExpToNextLevel_At_Level_1_With_No_Exp()
        {
            int needed = _player.ExpToNextLevel(_def);
            // Level 1 needs 50 exp, player has 0
            Assert.AreEqual(50, needed);
        }

        [Test]
        public void ExpToNextLevel_With_Partial_Exp()
        {
            _player.AddExp(_def, 30); // not enough to level
            Assert.AreEqual(1, _player.Level);
            // Level 1 needs 50, has 30, so 20 remaining
            Assert.AreEqual(20, _player.ExpToNextLevel(_def));
        }

        [Test]
        public void ExpToNextLevel_At_Max_Level_Returns_Zero()
        {
            var smallDef = new PlayerLevelDef { MaxLevel = 2, ExpCurve = "standard" };
            _player.AddExp(smallDef, 50); // level up to 2 (max)
            Assert.AreEqual(0, _player.ExpToNextLevel(smallDef));
        }

        // ── ExpProgress ──

        [Test]
        public void ExpProgress_At_Zero_Returns_Zero()
        {
            float progress = _player.ExpProgress(_def);
            Assert.AreEqual(0f, progress, 0.001f);
        }

        [Test]
        public void ExpProgress_Halfway()
        {
            // Level 1 needs 50 exp; give 25 (half)
            _player.AddExp(_def, 25);
            Assert.AreEqual(1, _player.Level);
            Assert.AreEqual(0.5f, _player.ExpProgress(_def), 0.001f);
        }

        [Test]
        public void ExpProgress_At_Max_Level_Returns_One()
        {
            var smallDef = new PlayerLevelDef { MaxLevel = 2, ExpCurve = "standard" };
            _player.AddExp(smallDef, 50);
            Assert.AreEqual(1f, _player.ExpProgress(smallDef), 0.001f);
        }

        // ── LeveledUp Event ──

        [Test]
        public void LeveledUp_Event_Fires_With_Correct_Levels()
        {
            var events = new List<(int oldLevel, int newLevel)>();
            _player.LeveledUp += (old, @new) => events.Add((old, @new));

            _player.AddExp(_def, 50); // level 1 -> 2

            Assert.AreEqual(1, events.Count, "Should fire once for single level gain");
            Assert.AreEqual(1, events[0].oldLevel);
            Assert.AreEqual(2, events[0].newLevel);
        }

        [Test]
        public void LeveledUp_Event_Fires_For_Each_Level_Gained()
        {
            var events = new List<(int oldLevel, int newLevel)>();
            _player.LeveledUp += (old, @new) => events.Add((old, @new));

            // Level 1->2 = 50, level 2->3 = 200 => 250 for 2 levels
            _player.AddExp(_def, 250);

            Assert.AreEqual(2, events.Count, "Should fire twice for two level gains");
            Assert.AreEqual(1, events[0].oldLevel);
            Assert.AreEqual(2, events[0].newLevel);
            Assert.AreEqual(2, events[1].oldLevel);
            Assert.AreEqual(3, events[1].newLevel);
        }

        [Test]
        public void LeveledUp_Event_Does_Not_Fire_Without_Level_Gain()
        {
            int fireCount = 0;
            _player.LeveledUp += (_, _) => fireCount++;

            _player.AddExp(_def, 10); // not enough to level up

            Assert.AreEqual(0, fireCount, "Should not fire when no level gained");
        }

        // ── Persistence ──

        [Test]
        public void ToSaveDict_Contains_All_Fields()
        {
            _player.AddExp(_def, 60); // level 2, 10 exp leftover, 60 total

            var dict = _player.ToSaveDict();

            Assert.AreEqual(2, dict["level"]);
            Assert.AreEqual(10, dict["exp"]);
            Assert.AreEqual(60, dict["totalExp"]);
        }

        [Test]
        public void FromSaveDict_Restores_State()
        {
            _player.AddExp(_def, 60);
            var dict = _player.ToSaveDict();

            var restored = PlayerLevel.FromSaveDict(dict);

            Assert.AreEqual(_player.Level, restored.Level);
            Assert.AreEqual(_player.Exp, restored.Exp);
            Assert.AreEqual(_player.TotalExp, restored.TotalExp);
        }

        [Test]
        public void FromSaveDict_RoundTrip_Preserves_State()
        {
            // Level up multiple times, save, restore, verify
            _player.AddExp(_def, 1500); // level 5
            _player.AddExp(_def, 30);   // some leftover exp at level 5

            var dict = _player.ToSaveDict();
            var restored = PlayerLevel.FromSaveDict(dict);

            Assert.AreEqual(_player.Level, restored.Level, "Level should match");
            Assert.AreEqual(_player.Exp, restored.Exp, "Exp should match");
            Assert.AreEqual(_player.TotalExp, restored.TotalExp, "TotalExp should match");

            // Verify restored instance behaves correctly
            Assert.AreEqual(_player.GetStaminaCap(_def), restored.GetStaminaCap(_def),
                "Stat caps should be identical after restore");
        }

        [Test]
        public void FromSaveDict_Handles_Defaults()
        {
            var emptyDict = new Dictionary<string, object>();
            var restored = PlayerLevel.FromSaveDict(emptyDict);

            Assert.AreEqual(1, restored.Level, "Default level should be 1");
            Assert.AreEqual(0, restored.Exp, "Default exp should be 0");
            Assert.AreEqual(0, restored.TotalExp, "Default totalExp should be 0");
        }

        // ── Edge Cases ──

        [Test]
        public void AddExp_Zero_Amount_Does_Nothing()
        {
            int levels = _player.AddExp(_def, 0);
            Assert.AreEqual(0, levels);
            Assert.AreEqual(1, _player.Level);
            Assert.AreEqual(0, _player.Exp);
        }

        [Test]
        public void AddExp_Negative_Amount_Does_Nothing()
        {
            int levels = _player.AddExp(_def, -100);
            Assert.AreEqual(0, levels);
            Assert.AreEqual(1, _player.Level);
        }

        [Test]
        public void Stat_Caps_With_No_Rewards()
        {
            var emptyDef = new PlayerLevelDef { MaxLevel = 100, ExpCurve = "standard" };

            Assert.AreEqual(50, _player.GetStaminaCap(emptyDef));
            Assert.AreEqual(50, _player.GetTeamCostLimit(emptyDef));
            Assert.AreEqual(50, _player.GetStorageCapacity(emptyDef));
            Assert.AreEqual(10, _player.GetFriendSlots(emptyDef));
        }

        [Test]
        public void All_Stat_Caps_Include_All_Rewards_At_High_Level()
        {
            // Reach level 10+ to include all rewards
            // Levels 1->10: sum of Ceil(n^2 * 50) for n=1..9
            // = 50 + 200 + 450 + 800 + 1250 + 1800 + 2450 + 3200 + 4050 = 14250
            _player.AddExp(_def, 14250);
            Assert.AreEqual(10, _player.Level);

            // StaminaCap: 50 + 3 + 5 + 10 = 68
            Assert.AreEqual(68, _player.GetStaminaCap(_def));
            // TeamCostLimit: 50 + 2 + 3 + 5 = 60
            Assert.AreEqual(60, _player.GetTeamCostLimit(_def));
            // StorageCapacity: 50 + 5 + 10 + 20 = 85
            Assert.AreEqual(85, _player.GetStorageCapacity(_def));
            // FriendSlots: 10 + 1 + 2 + 5 = 18
            Assert.AreEqual(18, _player.GetFriendSlots(_def));
        }
    }
}
