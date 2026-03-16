using System;
using System.Collections.Generic;
using NUnit.Framework;
using MobileForge.Domain;

namespace MobileForge.Tests.Domain
{
    [TestFixture]
    public class MonsterManagerTests
    {
        private MonsterManager _manager;

        // -----------------------------------------------------------------
        // Test monster definition data (as raw dictionaries, matching JSON)
        // -----------------------------------------------------------------

        private static readonly Dictionary<int, Dictionary<string, object>> TestDefData = new()
        {
            {
                1, new Dictionary<string, object>
                {
                    ["id"] = 1, ["name"] = "Water Dragon", ["element"] = 1,
                    ["rarity"] = 5, ["max_level"] = 99, ["cost"] = 25,
                    ["base_hp"] = 500f, ["base_atk"] = 200f, ["base_rec"] = 100f,
                    ["max_hp"] = 3000f, ["max_atk"] = 1500f, ["max_rec"] = 400f,
                    ["active_skill_id"] = 101, ["leader_skill_id"] = 201,
                    ["evolve_to"] = 2, ["exp_curve"] = "standard",
                }
            },
            {
                2, new Dictionary<string, object>
                {
                    ["id"] = 2, ["name"] = "Water Dragon Evolved", ["element"] = 1,
                    ["rarity"] = 6, ["max_level"] = 99, ["cost"] = 50,
                    ["base_hp"] = 1000f, ["base_atk"] = 500f, ["base_rec"] = 200f,
                    ["max_hp"] = 5000f, ["max_atk"] = 2500f, ["max_rec"] = 800f,
                    ["active_skill_id"] = 102, ["leader_skill_id"] = 202,
                    ["evolve_to"] = -1, ["exp_curve"] = "standard",
                }
            },
            {
                3, new Dictionary<string, object>
                {
                    ["id"] = 3, ["name"] = "Fire Slime", ["element"] = 2,
                    ["rarity"] = 1, ["max_level"] = 10, ["cost"] = 5,
                    ["base_hp"] = 100f, ["base_atk"] = 50f, ["base_rec"] = 20f,
                    ["max_hp"] = 500f, ["max_atk"] = 200f, ["max_rec"] = 80f,
                    ["active_skill_id"] = -1, ["leader_skill_id"] = -1,
                    ["evolve_to"] = -1, ["exp_curve"] = "fast",
                }
            },
            {
                4, new Dictionary<string, object>
                {
                    ["id"] = 4, ["name"] = "Slow Growth Monster", ["element"] = 3,
                    ["rarity"] = 3, ["max_level"] = 50, ["cost"] = 15,
                    ["base_hp"] = 300f, ["base_atk"] = 100f, ["base_rec"] = 50f,
                    ["max_hp"] = 2000f, ["max_atk"] = 800f, ["max_rec"] = 300f,
                    ["active_skill_id"] = -1, ["leader_skill_id"] = -1,
                    ["evolve_to"] = -1, ["exp_curve"] = "slow",
                }
            },
        };

        private static Dictionary<string, object> MockLookup(int defId)
        {
            return TestDefData.TryGetValue(defId, out var data) ? data : null;
        }

        [SetUp]
        public void SetUp()
        {
            _manager = new MonsterManager(MockLookup);
        }

        // -----------------------------------------------------------------
        // Tests
        // -----------------------------------------------------------------

        [Test]
        public void CreateInstance_Returns_Valid_Instance()
        {
            var inst = _manager.CreateInstance(1);

            Assert.IsNotNull(inst, "Instance should not be null");
            Assert.AreEqual(1, inst.DefId, "Instance def_id should match");
            Assert.AreEqual(1, inst.Level, "New instance should be level 1");
            Assert.AreEqual(0, inst.Exp, "New instance should have 0 exp");
        }

        [Test]
        public void GetStats_Level_1_Equals_Base_Stats()
        {
            var inst = _manager.CreateInstance(1);
            var stats = _manager.GetStats(inst);

            Assert.IsNotNull(stats, "Stats should not be null");
            Assert.AreEqual(500, stats.Hp, "HP at level 1 should equal base_hp");
            Assert.AreEqual(200, stats.Atk, "ATK at level 1 should equal base_atk");
            Assert.AreEqual(100, stats.Rec, "REC at level 1 should equal base_rec");
        }

        [Test]
        public void GetStats_Max_Level_Equals_Max_Stats()
        {
            var inst = _manager.CreateInstance(1);
            inst.Level = 99;

            var stats = _manager.GetStats(inst);

            Assert.IsNotNull(stats, "Stats should not be null");
            Assert.AreEqual(3000, stats.Hp, "HP at max level should equal max_hp");
            Assert.AreEqual(1500, stats.Atk, "ATK at max level should equal max_atk");
            Assert.AreEqual(400, stats.Rec, "REC at max level should equal max_rec");
        }

        [Test]
        public void AddExp_Levels_Up()
        {
            var inst = _manager.CreateInstance(1);
            Assert.AreEqual(1, inst.Level);

            // For standard curve: exp_for_level(1) = 100 * 1 * 1.0 = 100
            int levelsGained = _manager.AddExp(inst, 100);

            Assert.Greater(levelsGained, 0, "Should gain at least 1 level");
            Assert.Greater(inst.Level, 1, "Level should increase above 1");
        }

        [Test]
        public void Fuse_Adds_Exp()
        {
            var baseMonster = _manager.CreateInstance(1);
            var fodder = _manager.CreateInstance(3); // Fire Slime, rarity 1

            int expGained = _manager.Fuse(baseMonster, fodder);

            // Exp from fusion = fodder.level * fodder_def.rarity * 50 = 1 * 1 * 50 = 50
            Assert.AreEqual(50, expGained, "Fuse should return 50 exp for level 1 rarity 1 fodder");
        }

        [Test]
        public void CanEvolve_At_Max_Level()
        {
            var inst = _manager.CreateInstance(1);
            inst.Level = 99;

            Assert.IsTrue(_manager.CanEvolve(inst), "Monster at max level with evolve_to should be able to evolve");
        }

        [Test]
        public void Cannot_Evolve_Low_Level()
        {
            var inst = _manager.CreateInstance(1);

            Assert.IsFalse(_manager.CanEvolve(inst), "Monster below max level should not be able to evolve");
        }

        [Test]
        public void Stat_Curves_Differ()
        {
            var fastInst = _manager.CreateInstance(3);
            fastInst.Level = 5;

            var slowInst = _manager.CreateInstance(4);
            slowInst.Level = 5;

            var fastStats = _manager.GetStats(fastInst);
            var slowStats = _manager.GetStats(slowInst);

            // Both should be between base and max at intermediate level
            Assert.Greater(fastStats.Hp, 100, "Fast curve HP should be above base at level 5");
            Assert.Greater(500, fastStats.Hp, "Fast curve HP should be below max at level 5");
            Assert.Greater(slowStats.Hp, 300, "Slow curve HP should be above base at level 5");
            Assert.Greater(2000, slowStats.Hp, "Slow curve HP should be below max at level 5");

            // Fast curve at level 5/10 produces higher ratio than slow curve at 5/50
            float fastRatio = (fastStats.Hp - 100f) / (500f - 100f);
            float slowRatio = (slowStats.Hp - 300f) / (2000f - 300f);
            Assert.Greater(fastRatio, slowRatio,
                "Fast curve should produce higher stat ratio than slow curve at same level");
        }
    }
}
