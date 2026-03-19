using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MobileForge.Domain;

namespace MobileForge.Tests.Domain
{
    [TestFixture]
    public class EquipmentManagerTests
    {
        private EquipmentManager _manager;
        private float _nextRandom;

        private EquipmentDef _swordDef;
        private EquipmentDef _armorDef;
        private SetBonusDef _setBonusDef;

        [SetUp]
        public void SetUp()
        {
            _nextRandom = 0.5f;
            _manager = new EquipmentManager(() => _nextRandom);

            _swordDef = new EquipmentDef
            {
                Id = "sword_01",
                Name = "Iron Sword",
                SlotType = EquipSlot.Weapon,
                Rarity = 3,
                MaxLevel = 20,
                BaseStats = new Dictionary<string, float> { ["atk"] = 10f, ["hp"] = 100f },
                MaxStats = new Dictionary<string, float> { ["atk"] = 100f, ["hp"] = 500f },
                SetIds = new List<string> { "iron_set" },
                SubstatSlots = 4,
                ExpCurve = "standard",
            };

            _armorDef = new EquipmentDef
            {
                Id = "armor_01",
                Name = "Iron Armor",
                SlotType = EquipSlot.Armor,
                Rarity = 3,
                MaxLevel = 20,
                BaseStats = new Dictionary<string, float> { ["def"] = 20f, ["hp"] = 200f },
                MaxStats = new Dictionary<string, float> { ["def"] = 80f, ["hp"] = 800f },
                SetIds = new List<string> { "iron_set" },
                SubstatSlots = 2,
            };

            _setBonusDef = new SetBonusDef
            {
                Id = "iron_set",
                Name = "Iron Set",
                RequiredPieces = 2,
                BonusStats = new Dictionary<string, float> { ["def"] = 15f },
            };

            _manager.LoadDefs(new[] { _swordDef, _armorDef });
            _manager.LoadSetDefs(new[] { _setBonusDef });
        }

        private EquipmentInstance MakeSword(int level = 1, int exp = 0)
        {
            return new EquipmentInstance
            {
                InstanceId = Guid.NewGuid().ToString(),
                DefId = "sword_01",
                Level = level,
                Exp = exp,
            };
        }

        private EquipmentInstance MakeArmor(int level = 1, int exp = 0)
        {
            return new EquipmentInstance
            {
                InstanceId = Guid.NewGuid().ToString(),
                DefId = "armor_01",
                Level = level,
                Exp = exp,
            };
        }

        // ── Enhance: basic EXP + level-up ──

        [Test]
        public void Enhance_AddsExpAndLevelsUp()
        {
            var sword = MakeSword();
            // EXP for level 2 = 2*2*100 = 400
            _manager.Enhance(sword, 400);

            Assert.AreEqual(2, sword.Level);
            Assert.AreEqual(0, sword.Exp);
        }

        [Test]
        public void Enhance_AddsExpWithoutLevelUpWhenInsufficient()
        {
            var sword = MakeSword();
            _manager.Enhance(sword, 100);

            Assert.AreEqual(1, sword.Level);
            Assert.AreEqual(100, sword.Exp);
        }

        // ── Enhance: multi-level ──

        [Test]
        public void Enhance_MultiLevelUpInSingleCall()
        {
            var sword = MakeSword();
            // Level 2: 400, Level 3: 900, Level 4: 1600 → total = 2900
            _manager.Enhance(sword, 2900);

            Assert.AreEqual(4, sword.Level);
            Assert.AreEqual(0, sword.Exp);
        }

        [Test]
        public void Enhance_MultiLevelUpWithLeftoverExp()
        {
            var sword = MakeSword();
            // Level 2: 400, Level 3: 900 → total = 1300; feed 1500
            _manager.Enhance(sword, 1500);

            Assert.AreEqual(3, sword.Level);
            Assert.AreEqual(200, sword.Exp);
        }

        // ── Enhance: caps at max level ──

        [Test]
        public void Enhance_CapsAtMaxLevel()
        {
            var sword = MakeSword(level: 19);
            // Level 20: 20*20*100 = 40000
            _manager.Enhance(sword, 999999);

            Assert.AreEqual(20, sword.Level);
            Assert.AreEqual(0, sword.Exp);
        }

        [Test]
        public void Enhance_NoEffectWhenAlreadyMaxLevel()
        {
            var sword = MakeSword(level: 20);
            _manager.Enhance(sword, 5000);

            Assert.AreEqual(20, sword.Level);
            Assert.AreEqual(0, sword.Exp);
        }

        // ── RollSubstat: new when slots available ──

        [Test]
        public void RollSubstat_AddsNewWhenSlotsAvailable()
        {
            var sword = MakeSword();
            Assert.AreEqual(0, sword.Substats.Count);

            _manager.RollSubstat(sword);

            Assert.AreEqual(1, sword.Substats.Count);
            Assert.AreEqual(1, sword.Substats[0].RollCount);
        }

        [Test]
        public void RollSubstat_NewSubstatHasPositiveValue()
        {
            var sword = MakeSword();
            _manager.RollSubstat(sword);

            Assert.Greater(sword.Substats[0].Value, 0f);
        }

        // ── RollSubstat: upgrades existing when full ──

        [Test]
        public void RollSubstat_UpgradesExistingWhenFull()
        {
            var sword = MakeSword();
            // Fill all 4 substat slots
            for (int i = 0; i < 4; i++)
            {
                sword.Substats.Add(new SubstatEntry
                {
                    StatId = new[] { "atk", "hp", "def", "spd" }[i],
                    Value = 10f,
                    RollCount = 1,
                });
            }

            float totalBefore = sword.Substats.Sum(s => s.Value);
            int rollsBefore = sword.Substats.Sum(s => s.RollCount);

            _manager.RollSubstat(sword);

            Assert.AreEqual(4, sword.Substats.Count);
            float totalAfter = sword.Substats.Sum(s => s.Value);
            int rollsAfter = sword.Substats.Sum(s => s.RollCount);

            Assert.Greater(totalAfter, totalBefore);
            Assert.AreEqual(rollsBefore + 1, rollsAfter);
        }

        // ── Enhance triggers substat roll at level 5 ──

        [Test]
        public void Enhance_RollsSubstatAtLevelDivisibleBy5()
        {
            var sword = MakeSword(level: 4);
            // Level 5: 5*5*100 = 2500
            _manager.Enhance(sword, 2500);

            Assert.AreEqual(5, sword.Level);
            Assert.AreEqual(1, sword.Substats.Count);
        }

        // ── CalculateEquipmentStats ──

        [Test]
        public void CalculateEquipmentStats_Level1_ReturnsBaseStats()
        {
            var sword = MakeSword();
            var stats = _manager.CalculateEquipmentStats(sword);

            Assert.AreEqual(10f, stats["atk"], 0.001f);
            Assert.AreEqual(100f, stats["hp"], 0.001f);
        }

        [Test]
        public void CalculateEquipmentStats_MaxLevel_ReturnsMaxStats()
        {
            var sword = MakeSword(level: 20);
            var stats = _manager.CalculateEquipmentStats(sword);

            Assert.AreEqual(100f, stats["atk"], 0.001f);
            Assert.AreEqual(500f, stats["hp"], 0.001f);
        }

        [Test]
        public void CalculateEquipmentStats_MidLevel_InterpolatesCorrectly()
        {
            // Level 10/20 → t = 9/19
            var sword = MakeSword(level: 10);
            var stats = _manager.CalculateEquipmentStats(sword);

            float t = 9f / 19f;
            float expectedAtk = 10f + (100f - 10f) * t;
            float expectedHp = 100f + (500f - 100f) * t;

            Assert.AreEqual(expectedAtk, stats["atk"], 0.01f);
            Assert.AreEqual(expectedHp, stats["hp"], 0.01f);
        }

        [Test]
        public void CalculateEquipmentStats_IncludesSubstats()
        {
            var sword = MakeSword();
            sword.Substats.Add(new SubstatEntry { StatId = "atk", Value = 15f });
            sword.Substats.Add(new SubstatEntry { StatId = "crit_rate", Value = 0.05f });

            var stats = _manager.CalculateEquipmentStats(sword);

            // Base atk (10) + substat (15)
            Assert.AreEqual(25f, stats["atk"], 0.001f);
            Assert.AreEqual(0.05f, stats["crit_rate"], 0.001f);
        }

        // ── CalculateAllEquippedStats ──

        [Test]
        public void CalculateAllEquippedStats_SumsMultipleItems()
        {
            var sword = MakeSword();
            var armor = MakeArmor();
            var equipped = new[] { sword, armor };

            var stats = _manager.CalculateAllEquippedStats(equipped);

            // sword base: atk=10, hp=100; armor base: def=20, hp=200
            Assert.AreEqual(10f, stats["atk"], 0.001f);
            Assert.AreEqual(300f, stats["hp"], 0.001f);
            Assert.AreEqual(20f, stats["def"], 0.001f);
        }

        // ── GetActiveSetBonuses ──

        [Test]
        public void GetActiveSetBonuses_DetectsCompleteSet()
        {
            var sword = MakeSword();
            var armor = MakeArmor();

            var bonuses = _manager.GetActiveSetBonuses(new[] { sword, armor });

            Assert.AreEqual(1, bonuses.Count);
            Assert.AreEqual("iron_set", bonuses[0].Id);
        }

        [Test]
        public void GetActiveSetBonuses_IgnoresIncompleteSet()
        {
            var sword = MakeSword();

            var bonuses = _manager.GetActiveSetBonuses(new[] { sword });

            Assert.AreEqual(0, bonuses.Count);
        }

        // ── CompareStats ──

        [Test]
        public void CompareStats_ShowsDifferencesCorrectly()
        {
            var current = MakeSword(level: 1);
            var candidate = MakeSword(level: 10);

            var diff = _manager.CompareStats(current, candidate);

            // Candidate at level 10 has higher atk and hp
            Assert.Greater(diff["atk"], 0f);
            Assert.Greater(diff["hp"], 0f);
        }

        [Test]
        public void CompareStats_NegativeWhenCurrentIsBetter()
        {
            var current = MakeSword(level: 20);
            var candidate = MakeSword(level: 1);

            var diff = _manager.CompareStats(current, candidate);

            Assert.Less(diff["atk"], 0f);
            Assert.Less(diff["hp"], 0f);
        }

        // ── GetExpToNextLevel ──

        [Test]
        public void GetExpToNextLevel_IsAccurate()
        {
            var sword = MakeSword(level: 1, exp: 100);
            // Level 2 threshold = 400; remaining = 400 - 100 = 300
            int remaining = _manager.GetExpToNextLevel(sword);

            Assert.AreEqual(300, remaining);
        }

        [Test]
        public void GetExpToNextLevel_ZeroAtMaxLevel()
        {
            var sword = MakeSword(level: 20);
            Assert.AreEqual(0, _manager.GetExpToNextLevel(sword));
        }

        // ── IsMaxLevel ──

        [Test]
        public void IsMaxLevel_TrueAtMax()
        {
            var sword = MakeSword(level: 20);
            Assert.IsTrue(_manager.IsMaxLevel(sword));
        }

        [Test]
        public void IsMaxLevel_FalseBeforeMax()
        {
            var sword = MakeSword(level: 19);
            Assert.IsFalse(_manager.IsMaxLevel(sword));
        }

        // ── Events ──

        [Test]
        public void Enhance_FiresEquipmentEnhancedEvent()
        {
            EquipmentInstance eventInstance = null;
            int eventLevels = 0;
            _manager.EquipmentEnhanced += (inst, levels) =>
            {
                eventInstance = inst;
                eventLevels = levels;
            };

            var sword = MakeSword();
            _manager.Enhance(sword, 400);

            Assert.IsNotNull(eventInstance);
            Assert.AreEqual(sword, eventInstance);
            Assert.AreEqual(1, eventLevels);
        }

        [Test]
        public void Enhance_EventReportsCorrectMultiLevelGain()
        {
            int eventLevels = 0;
            _manager.EquipmentEnhanced += (_, levels) => eventLevels = levels;

            var sword = MakeSword();
            // Level 2: 400, Level 3: 900, Level 4: 1600 → 2900
            _manager.Enhance(sword, 2900);

            Assert.AreEqual(3, eventLevels);
        }

        [Test]
        public void Enhance_DoesNotFireEventWhenNoLevelGained()
        {
            bool fired = false;
            _manager.EquipmentEnhanced += (_, _) => fired = true;

            var sword = MakeSword();
            _manager.Enhance(sword, 10);

            Assert.IsFalse(fired);
        }

        // ── Persistence round-trip ──

        [Test]
        public void EquipmentInstance_ToSaveDict_FromSaveDict_RoundTrip()
        {
            var original = MakeSword(level: 15, exp: 250);
            original.IsLocked = true;
            original.Substats.Add(new SubstatEntry { StatId = "atk", Value = 12.5f, RollCount = 2 });
            original.Substats.Add(new SubstatEntry { StatId = "crit_rate", Value = 0.03f, RollCount = 1 });

            var dict = original.ToSaveDict();
            var restored = EquipmentInstance.FromSaveDict(dict);

            Assert.AreEqual(original.InstanceId, restored.InstanceId);
            Assert.AreEqual(original.DefId, restored.DefId);
            Assert.AreEqual(original.Level, restored.Level);
            Assert.AreEqual(original.Exp, restored.Exp);
            Assert.AreEqual(original.IsLocked, restored.IsLocked);
            Assert.AreEqual(original.Substats.Count, restored.Substats.Count);

            for (int i = 0; i < original.Substats.Count; i++)
            {
                Assert.AreEqual(original.Substats[i].StatId, restored.Substats[i].StatId);
                Assert.AreEqual(original.Substats[i].Value, restored.Substats[i].Value, 0.001f);
                Assert.AreEqual(original.Substats[i].RollCount, restored.Substats[i].RollCount);
            }
        }
    }
}
