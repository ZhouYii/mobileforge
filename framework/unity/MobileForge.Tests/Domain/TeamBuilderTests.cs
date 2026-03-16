using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MobileForge.Domain;

namespace MobileForge.Tests.Domain
{
    /// <summary>
    /// Tests for TeamBuilder -- party composition validation and stats aggregation.
    /// TeamBuilder does not yet exist as a source file; these tests define
    /// the expected API contract (test-first development).
    /// </summary>
    [TestFixture]
    public class TeamBuilderTests
    {
        private MonsterManager _manager;
        private TeamBuilder _builder;

        // -----------------------------------------------------------------
        // Test monster definitions
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
                    ["evolve_to"] = -1, ["exp_curve"] = "standard",
                }
            },
            {
                2, new Dictionary<string, object>
                {
                    ["id"] = 2, ["name"] = "Water Golem", ["element"] = 1,
                    ["rarity"] = 3, ["max_level"] = 50, ["cost"] = 15,
                    ["base_hp"] = 800f, ["base_atk"] = 100f, ["base_rec"] = 50f,
                    ["max_hp"] = 4000f, ["max_atk"] = 600f, ["max_rec"] = 200f,
                    ["active_skill_id"] = -1, ["leader_skill_id"] = -1,
                    ["evolve_to"] = -1, ["exp_curve"] = "standard",
                }
            },
            {
                3, new Dictionary<string, object>
                {
                    ["id"] = 3, ["name"] = "Fire Phoenix", ["element"] = 2,
                    ["rarity"] = 5, ["max_level"] = 99, ["cost"] = 30,
                    ["base_hp"] = 400f, ["base_atk"] = 300f, ["base_rec"] = 80f,
                    ["max_hp"] = 2500f, ["max_atk"] = 2000f, ["max_rec"] = 350f,
                    ["active_skill_id"] = 102, ["leader_skill_id"] = 202,
                    ["evolve_to"] = -1, ["exp_curve"] = "standard",
                }
            },
        };

        private static Dictionary<string, object> MockLookup(int defId)
        {
            return TestDefData.TryGetValue(defId, out var data) ? data : null;
        }

        // -----------------------------------------------------------------
        // Setup
        // -----------------------------------------------------------------

        [SetUp]
        public void SetUp()
        {
            _manager = new MonsterManager(MockLookup);
            // TeamBuilder constructor: (int maxSlots, MonsterManager manager)
            _builder = new TeamBuilder(6, _manager);
        }

        // -----------------------------------------------------------------
        // Tests
        // -----------------------------------------------------------------

        [Test]
        public void Set_Slot()
        {
            var inst = _manager.CreateInstance(1);
            bool ok = _builder.SetSlot(0, inst);

            Assert.IsTrue(ok, "SetSlot(0) should succeed");
            var team = _builder.GetTeam();
            Assert.IsNotNull(team.GetSlot(0), "Slot 0 should be populated");
            Assert.AreEqual(1, team.GetSlot(0).DefId, "Slot 0 def_id should match");
        }

        [Test]
        public void Set_Slot_Out_Of_Range()
        {
            var inst = _manager.CreateInstance(1);

            bool ok1 = _builder.SetSlot(-1, inst);
            Assert.IsFalse(ok1, "SetSlot(-1) should fail");

            bool ok2 = _builder.SetSlot(6, inst);
            Assert.IsFalse(ok2, "SetSlot(6) should fail for maxSlots=6");
        }

        [Test]
        public void Clear_Slot()
        {
            var inst = _manager.CreateInstance(1);
            _builder.SetSlot(0, inst);
            _builder.ClearSlot(0);

            var team = _builder.GetTeam();
            Assert.IsNull(team.GetSlot(0), "Slot 0 should be null after clear");
        }

        [Test]
        public void Validate_Empty_Team()
        {
            var errors = _builder.Validate();

            Assert.Greater(errors.Count, 0, "Empty team should produce errors");
            bool foundEmptyError = errors.Any(e =>
                e.ToLower().Contains("at least 1") || e.ToLower().Contains("empty"));
            Assert.IsTrue(foundEmptyError,
                "Errors should mention needing at least 1 member");
        }

        [Test]
        public void Validate_Valid_Team()
        {
            var inst = _manager.CreateInstance(1);
            _builder.SetSlot(0, inst);

            var errors = _builder.Validate();

            Assert.AreEqual(0, errors.Count, "Team with 1 monster should have no errors");
        }

        [Test]
        public void Validate_Duplicate_Monster()
        {
            var inst = _manager.CreateInstance(1);
            _builder.SetSlot(0, inst);
            _builder.SetSlot(1, inst); // same instance in 2 slots

            var errors = _builder.Validate();

            Assert.Greater(errors.Count, 0, "Duplicate monster should produce errors");
            bool foundDupError = errors.Any(e => e.ToLower().Contains("duplicate"));
            Assert.IsTrue(foundDupError, "Errors should mention duplicates");
        }

        [Test]
        public void Get_Team_Stats()
        {
            // Place 2 monsters and verify aggregated stats
            var instA = _manager.CreateInstance(1); // Water Dragon: base hp=500, atk=200, rec=100
            var instB = _manager.CreateInstance(2); // Water Golem: base hp=800, atk=100, rec=50
            _builder.SetSlot(0, instA);
            _builder.SetSlot(1, instB);

            var stats = _builder.GetTeamStats();

            // At level 1, stats = base stats
            Assert.AreEqual(500 + 800, stats.TotalHp, "TotalHp should be sum of base HPs");
            Assert.AreEqual(200 + 100, stats.TotalAtk, "TotalAtk should be sum of base ATKs");
            Assert.AreEqual(100 + 50, stats.TotalRec, "TotalRec should be sum of base RECs");
        }

        [Test]
        public void Team_Skill_Checker_All_Same_Element()
        {
            // 2 water monsters -> AllSameElement should be true
            var instA = _manager.CreateInstance(1); // Water, element=1
            var instB = _manager.CreateInstance(2); // Water, element=1
            _builder.SetSlot(0, instA);
            _builder.SetSlot(1, instB);

            Assert.IsTrue(_builder.AllSameElement(),
                "2 water monsters should satisfy AllSameElement");

            // Add a fire monster -> should break the check
            var instC = _manager.CreateInstance(3); // Fire, element=2
            _builder.SetSlot(2, instC);

            Assert.IsFalse(_builder.AllSameElement(),
                "Mixed elements should not satisfy AllSameElement");
        }
    }
}
