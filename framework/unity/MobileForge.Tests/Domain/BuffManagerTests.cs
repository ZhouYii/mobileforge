using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MobileForge.Domain;

namespace MobileForge.Tests.Domain
{
    [TestFixture]
    public class BuffManagerTests
    {
        private BuffManager _manager;

        [SetUp]
        public void SetUp()
        {
            _manager = new BuffManager();
        }

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

        private BuffDef MakeDef(
            string id,
            string stackRule = BuffStackRule.Replace,
            int maxStacks = 1,
            int baseDuration = 3,
            bool isDebuff = false,
            bool dispellable = true,
            string category = BuffCategory.Offensive,
            Dictionary<string, float> statMods = null,
            Dictionary<string, float> flatMods = null,
            Dictionary<string, object> parms = null)
        {
            return new BuffDef
            {
                Id = id,
                Name = id,
                Category = category,
                StackRule = stackRule,
                MaxStacks = maxStacks,
                BaseDuration = baseDuration,
                Dispellable = dispellable,
                IsDebuff = isDebuff,
                StatModifiers = statMods ?? new Dictionary<string, float>(),
                FlatModifiers = flatMods ?? new Dictionary<string, float>(),
                Params = parms ?? new Dictionary<string, object>(),
            };
        }

        // -----------------------------------------------------------------
        // Apply buff — basic
        // -----------------------------------------------------------------

        [Test]
        public void ApplyBuff_Adds_New_Buff()
        {
            var def = MakeDef("atk_up");
            _manager.ApplyBuff(def, "hero1");

            Assert.IsTrue(_manager.HasBuff("atk_up"));
            Assert.AreEqual(1, _manager.ActiveCount);
        }

        [Test]
        public void ApplyBuff_Sets_Correct_Properties()
        {
            var def = MakeDef("atk_up", baseDuration: 5);
            _manager.ApplyBuff(def, "hero1", 10f);

            var all = _manager.GetAllActive();
            Assert.AreEqual(1, all.Count);
            Assert.AreEqual("atk_up", all[0].DefId);
            Assert.AreEqual("hero1", all[0].SourceId);
            Assert.AreEqual(5, all[0].RemainingDuration);
            Assert.AreEqual(1, all[0].Stacks);
            Assert.AreEqual(10f, all[0].Value, 0.001f);
        }

        // -----------------------------------------------------------------
        // Stacking rules
        // -----------------------------------------------------------------

        [Test]
        public void ApplyBuff_Replace_Replaces_Existing()
        {
            var def = MakeDef("atk_up", BuffStackRule.Replace, baseDuration: 3);
            _manager.ApplyBuff(def, "hero1", 5f);
            _manager.ApplyBuff(def, "hero2", 10f);

            Assert.AreEqual(1, _manager.ActiveCount);
            var active = _manager.GetAllActive();
            Assert.AreEqual("hero2", active[0].SourceId);
            Assert.AreEqual(10f, active[0].Value, 0.001f);
        }

        [Test]
        public void ApplyBuff_Extend_Adds_Duration()
        {
            var def = MakeDef("shield", BuffStackRule.Extend, baseDuration: 3);
            _manager.ApplyBuff(def, "hero1");
            _manager.ApplyBuff(def, "hero1");

            Assert.AreEqual(1, _manager.ActiveCount);
            var active = _manager.GetAllActive();
            Assert.AreEqual(6, active[0].RemainingDuration, "Duration should be 3 + 3 = 6");
        }

        [Test]
        public void ApplyBuff_Stack_Increments_Stacks_Up_To_Max()
        {
            var def = MakeDef("poison", BuffStackRule.Stack, maxStacks: 3, baseDuration: 5);
            _manager.ApplyBuff(def, "enemy1");
            _manager.ApplyBuff(def, "enemy1");
            _manager.ApplyBuff(def, "enemy1");
            _manager.ApplyBuff(def, "enemy1"); // Should not increase beyond 3

            Assert.AreEqual(1, _manager.ActiveCount);
            var active = _manager.GetAllActive();
            Assert.AreEqual(3, active[0].Stacks, "Stacks should cap at MaxStacks=3");
        }

        [Test]
        public void ApplyBuff_Stack_Refreshes_Duration()
        {
            var def = MakeDef("poison", BuffStackRule.Stack, maxStacks: 3, baseDuration: 5);
            _manager.ApplyBuff(def, "enemy1");

            // Tick once to reduce duration
            _manager.TickTurnEnd();
            var active = _manager.GetAllActive();
            Assert.AreEqual(4, active[0].RemainingDuration);

            // Stack again should refresh duration
            _manager.ApplyBuff(def, "enemy1");
            active = _manager.GetAllActive();
            Assert.AreEqual(5, active[0].RemainingDuration, "Duration should reset on stack");
            Assert.AreEqual(2, active[0].Stacks);
        }

        [Test]
        public void ApplyBuff_Highest_Keeps_Higher_Value()
        {
            var def = MakeDef("atk_up", BuffStackRule.Highest, baseDuration: 3);
            _manager.ApplyBuff(def, "hero1", 10f);
            _manager.ApplyBuff(def, "hero2", 20f);

            var active = _manager.GetAllActive();
            Assert.AreEqual(1, active.Count);
            Assert.AreEqual(20f, active[0].Value, 0.001f, "Higher value should win");
            Assert.AreEqual("hero2", active[0].SourceId);
        }

        [Test]
        public void ApplyBuff_Highest_Keeps_Existing_When_Lower()
        {
            var def = MakeDef("atk_up", BuffStackRule.Highest, baseDuration: 3);
            _manager.ApplyBuff(def, "hero1", 20f);
            _manager.ApplyBuff(def, "hero2", 5f);

            var active = _manager.GetAllActive();
            Assert.AreEqual(1, active.Count);
            Assert.AreEqual(20f, active[0].Value, 0.001f, "Existing higher value should stay");
            Assert.AreEqual("hero1", active[0].SourceId);
        }

        [Test]
        public void ApplyBuff_Refresh_Resets_Duration()
        {
            var def = MakeDef("regen", BuffStackRule.Refresh, baseDuration: 4);
            _manager.ApplyBuff(def, "hero1");

            // Tick twice
            _manager.TickTurnEnd();
            _manager.TickTurnEnd();
            var active = _manager.GetAllActive();
            Assert.AreEqual(2, active[0].RemainingDuration);

            // Refresh
            _manager.ApplyBuff(def, "hero1");
            active = _manager.GetAllActive();
            Assert.AreEqual(4, active[0].RemainingDuration, "Duration should reset to BaseDuration");
            Assert.AreEqual(1, _manager.ActiveCount, "Should still be one instance");
        }

        // -----------------------------------------------------------------
        // Remove
        // -----------------------------------------------------------------

        [Test]
        public void RemoveBuff_Removes_By_DefId()
        {
            var def1 = MakeDef("atk_up");
            var def2 = MakeDef("def_up");
            _manager.ApplyBuff(def1, "hero1");
            _manager.ApplyBuff(def2, "hero1");

            bool removed = _manager.RemoveBuff("atk_up");

            Assert.IsTrue(removed);
            Assert.IsFalse(_manager.HasBuff("atk_up"));
            Assert.IsTrue(_manager.HasBuff("def_up"));
            Assert.AreEqual(1, _manager.ActiveCount);
        }

        [Test]
        public void RemoveBuff_Returns_False_When_Not_Found()
        {
            Assert.IsFalse(_manager.RemoveBuff("nonexistent"));
        }

        [Test]
        public void RemoveAll_Removes_All_Instances()
        {
            // Use Replace rule but add two different buffs, then use RemoveAll on one
            var def1 = MakeDef("atk_up");
            var def2 = MakeDef("def_up");
            _manager.ApplyBuff(def1, "hero1");
            _manager.ApplyBuff(def2, "hero1");

            int count = _manager.RemoveAll("atk_up");
            Assert.AreEqual(1, count);
            Assert.IsFalse(_manager.HasBuff("atk_up"));
        }

        // -----------------------------------------------------------------
        // Cleanse
        // -----------------------------------------------------------------

        [Test]
        public void Cleanse_DebuffsOnly_Removes_Debuffs()
        {
            var buff = MakeDef("atk_up", isDebuff: false);
            var debuff = MakeDef("poison", isDebuff: true);
            _manager.LoadDefs(new[] { buff, debuff });
            _manager.ApplyBuff(buff, "hero1");
            _manager.ApplyBuff(debuff, "enemy1");

            int cleansed = _manager.Cleanse(debuffsOnly: true);

            Assert.AreEqual(1, cleansed);
            Assert.IsTrue(_manager.HasBuff("atk_up"), "Non-debuff should remain");
            Assert.IsFalse(_manager.HasBuff("poison"), "Debuff should be cleansed");
        }

        [Test]
        public void Cleanse_All_Removes_All_Dispellable()
        {
            var dispellable = MakeDef("atk_up", dispellable: true);
            var undispellable = MakeDef("passive", dispellable: false);
            _manager.LoadDefs(new[] { dispellable, undispellable });
            _manager.ApplyBuff(dispellable, "hero1");
            _manager.ApplyBuff(undispellable, "hero1");

            int cleansed = _manager.Cleanse(debuffsOnly: false);

            Assert.AreEqual(1, cleansed);
            Assert.IsFalse(_manager.HasBuff("atk_up"), "Dispellable buff should be cleansed");
            Assert.IsTrue(_manager.HasBuff("passive"), "Undispellable buff should remain");
        }

        // -----------------------------------------------------------------
        // Tick
        // -----------------------------------------------------------------

        [Test]
        public void TickTurnEnd_Decrements_Duration_And_Removes_Expired()
        {
            var def = MakeDef("short_buff", baseDuration: 2);
            _manager.ApplyBuff(def, "hero1");

            _manager.TickTurnEnd();
            Assert.AreEqual(1, _manager.ActiveCount, "Should still be active after 1 tick");
            Assert.AreEqual(1, _manager.GetAllActive()[0].RemainingDuration);

            _manager.TickTurnEnd();
            Assert.AreEqual(0, _manager.ActiveCount, "Should be removed after 2 ticks");
        }

        [Test]
        public void TickTurnStart_Decrements_Duration()
        {
            var def = MakeDef("buff", baseDuration: 3);
            _manager.ApplyBuff(def, "hero1");

            _manager.TickTurnStart();
            Assert.AreEqual(2, _manager.GetAllActive()[0].RemainingDuration);
        }

        [Test]
        public void Tick_Fires_BuffRemoved_On_Expiry()
        {
            var def = MakeDef("short_buff", baseDuration: 1);
            _manager.ApplyBuff(def, "hero1");

            BuffInstance removedBuff = null;
            _manager.BuffRemoved += b => removedBuff = b;

            _manager.TickTurnEnd();

            Assert.IsNotNull(removedBuff, "BuffRemoved should fire on expiry");
            Assert.AreEqual("short_buff", removedBuff.DefId);
        }

        // -----------------------------------------------------------------
        // Stat modifiers
        // -----------------------------------------------------------------

        [Test]
        public void GetStatModifier_Returns_Combined_Multiplicative()
        {
            var def1 = MakeDef("atk_up", statMods: new Dictionary<string, float> { { "atk", 1.5f } });
            var def2 = MakeDef("atk_up2", statMods: new Dictionary<string, float> { { "atk", 1.2f } });
            _manager.ApplyBuff(def1, "hero1");
            _manager.ApplyBuff(def2, "hero2");

            float mod = _manager.GetStatModifier("atk");
            Assert.AreEqual(1.8f, mod, 0.001f, "1.5 * 1.2 = 1.8");
        }

        [Test]
        public void GetStatModifier_Returns_1_When_No_Buffs()
        {
            Assert.AreEqual(1f, _manager.GetStatModifier("atk"), 0.001f);
        }

        [Test]
        public void GetStatModifier_Applies_Per_Stack()
        {
            var def = MakeDef("atk_up", BuffStackRule.Stack, maxStacks: 3,
                statMods: new Dictionary<string, float> { { "atk", 1.1f } });
            _manager.ApplyBuff(def, "hero1");
            _manager.ApplyBuff(def, "hero1");
            _manager.ApplyBuff(def, "hero1");

            float mod = _manager.GetStatModifier("atk");
            // 1.1 * 1.1 * 1.1 = 1.331
            Assert.AreEqual(1.331f, mod, 0.01f, "3 stacks of 1.1x should be ~1.331");
        }

        [Test]
        public void GetFlatModifier_Returns_Combined_Additive()
        {
            var def1 = MakeDef("flat_atk", flatMods: new Dictionary<string, float> { { "atk", 50f } });
            var def2 = MakeDef("flat_atk2", flatMods: new Dictionary<string, float> { { "atk", 30f } });
            _manager.ApplyBuff(def1, "hero1");
            _manager.ApplyBuff(def2, "hero2");

            float mod = _manager.GetFlatModifier("atk");
            Assert.AreEqual(80f, mod, 0.001f, "50 + 30 = 80");
        }

        [Test]
        public void GetFlatModifier_Returns_0_When_No_Buffs()
        {
            Assert.AreEqual(0f, _manager.GetFlatModifier("atk"), 0.001f);
        }

        [Test]
        public void GetFlatModifier_Scales_With_Stacks()
        {
            var def = MakeDef("flat_atk", BuffStackRule.Stack, maxStacks: 3,
                flatMods: new Dictionary<string, float> { { "atk", 25f } });
            _manager.ApplyBuff(def, "hero1");
            _manager.ApplyBuff(def, "hero1");

            float mod = _manager.GetFlatModifier("atk");
            Assert.AreEqual(50f, mod, 0.001f, "2 stacks * 25 = 50");
        }

        // -----------------------------------------------------------------
        // Query helpers
        // -----------------------------------------------------------------

        [Test]
        public void HasBuff_Returns_False_When_Not_Present()
        {
            Assert.IsFalse(_manager.HasBuff("nonexistent"));
        }

        [Test]
        public void GetByCategory_Filters_Correctly()
        {
            var offensive = MakeDef("atk_up", category: BuffCategory.Offensive);
            var defensive = MakeDef("def_up", category: BuffCategory.Defensive);
            var utility = MakeDef("speed_up", category: BuffCategory.Utility);
            _manager.ApplyBuff(offensive, "hero1");
            _manager.ApplyBuff(defensive, "hero1");
            _manager.ApplyBuff(utility, "hero1");

            var offList = _manager.GetByCategory(BuffCategory.Offensive);
            Assert.AreEqual(1, offList.Count);
            Assert.AreEqual("atk_up", offList[0].DefId);

            var defList = _manager.GetByCategory(BuffCategory.Defensive);
            Assert.AreEqual(1, defList.Count);
            Assert.AreEqual("def_up", defList[0].DefId);
        }

        [Test]
        public void GetAllActive_Returns_Copy()
        {
            var def = MakeDef("atk_up");
            _manager.ApplyBuff(def, "hero1");

            var list1 = _manager.GetAllActive();
            var list2 = _manager.GetAllActive();
            Assert.AreNotSame(list1, list2, "GetAllActive should return a new list each time");
        }

        // -----------------------------------------------------------------
        // Persistence
        // -----------------------------------------------------------------

        [Test]
        public void ToSaveDict_FromSaveDict_Roundtrip()
        {
            var def1 = MakeDef("atk_up", baseDuration: 5);
            var def2 = MakeDef("poison", BuffStackRule.Stack, maxStacks: 3,
                baseDuration: 4, isDebuff: true);
            _manager.LoadDefs(new[] { def1, def2 });
            _manager.ApplyBuff(def1, "hero1", 10f);
            _manager.ApplyBuff(def2, "enemy1", 5f);
            _manager.ApplyBuff(def2, "enemy1"); // 2 stacks

            var saveData = _manager.ToSaveDict();

            var restored = new BuffManager();
            restored.LoadDefs(new[] { def1, def2 });
            restored.FromSaveDict(saveData);

            Assert.AreEqual(2, restored.ActiveCount);
            Assert.IsTrue(restored.HasBuff("atk_up"));
            Assert.IsTrue(restored.HasBuff("poison"));

            var allActive = restored.GetAllActive();
            var atkBuff = allActive.First(b => b.DefId == "atk_up");
            Assert.AreEqual("hero1", atkBuff.SourceId);
            Assert.AreEqual(5, atkBuff.RemainingDuration);
            Assert.AreEqual(10f, atkBuff.Value, 0.001f);

            var poisonBuff = allActive.First(b => b.DefId == "poison");
            Assert.AreEqual(2, poisonBuff.Stacks);
        }

        // -----------------------------------------------------------------
        // Events
        // -----------------------------------------------------------------

        [Test]
        public void BuffApplied_Event_Fires()
        {
            BuffInstance applied = null;
            _manager.BuffApplied += b => applied = b;

            var def = MakeDef("atk_up");
            _manager.ApplyBuff(def, "hero1");

            Assert.IsNotNull(applied);
            Assert.AreEqual("atk_up", applied.DefId);
        }

        [Test]
        public void BuffRemoved_Event_Fires_On_Remove()
        {
            BuffInstance removed = null;
            var def = MakeDef("atk_up");
            _manager.ApplyBuff(def, "hero1");
            _manager.BuffRemoved += b => removed = b;

            _manager.RemoveBuff("atk_up");

            Assert.IsNotNull(removed);
            Assert.AreEqual("atk_up", removed.DefId);
        }

        [Test]
        public void BuffStacked_Event_Fires_On_Stack()
        {
            BuffInstance stacked = null;
            _manager.BuffStacked += b => stacked = b;

            var def = MakeDef("poison", BuffStackRule.Stack, maxStacks: 3);
            _manager.ApplyBuff(def, "enemy1");
            _manager.ApplyBuff(def, "enemy1");

            Assert.IsNotNull(stacked);
            Assert.AreEqual("poison", stacked.DefId);
            Assert.AreEqual(2, stacked.Stacks);
        }

        [Test]
        public void BuffCleansed_Event_Fires_On_Cleanse()
        {
            var cleansedList = new List<BuffInstance>();
            _manager.BuffCleansed += b => cleansedList.Add(b);

            var debuff = MakeDef("poison", isDebuff: true);
            _manager.LoadDefs(new[] { debuff });
            _manager.ApplyBuff(debuff, "enemy1");

            _manager.Cleanse(debuffsOnly: true);

            Assert.AreEqual(1, cleansedList.Count);
            Assert.AreEqual("poison", cleansedList[0].DefId);
        }

        [Test]
        public void BuffRemoved_Fires_On_Replace()
        {
            var removedList = new List<BuffInstance>();
            _manager.BuffRemoved += b => removedList.Add(b);

            var def = MakeDef("atk_up", BuffStackRule.Replace);
            _manager.ApplyBuff(def, "hero1", 5f);
            _manager.ApplyBuff(def, "hero2", 10f);

            // First apply: no removal event. Second apply: removal of first.
            Assert.AreEqual(1, removedList.Count);
            Assert.AreEqual("hero1", removedList[0].SourceId);
        }

        // -----------------------------------------------------------------
        // BuffCalculator static helpers
        // -----------------------------------------------------------------

        [Test]
        public void BuffCalculator_AggregateStatModifiers()
        {
            var defs = new[]
            {
                MakeDef("atk_up", statMods: new Dictionary<string, float> { { "atk", 1.5f } }),
                MakeDef("atk_up2", statMods: new Dictionary<string, float> { { "atk", 1.2f } }),
            };
            var buffs = new[]
            {
                new BuffInstance { DefId = "atk_up", Stacks = 1 },
                new BuffInstance { DefId = "atk_up2", Stacks = 1 },
            };

            float result = BuffCalculator.AggregateStatModifiers(buffs, defs, "atk");
            Assert.AreEqual(1.8f, result, 0.001f);
        }

        [Test]
        public void BuffCalculator_AggregateFlatModifiers()
        {
            var defs = new[]
            {
                MakeDef("flat1", flatMods: new Dictionary<string, float> { { "hp", 100f } }),
                MakeDef("flat2", flatMods: new Dictionary<string, float> { { "hp", 50f } }),
            };
            var buffs = new[]
            {
                new BuffInstance { DefId = "flat1", Stacks = 2 },
                new BuffInstance { DefId = "flat2", Stacks = 1 },
            };

            float result = BuffCalculator.AggregateFlatModifiers(buffs, defs, "hp");
            Assert.AreEqual(250f, result, 0.001f, "100*2 + 50*1 = 250");
        }

        [Test]
        public void BuffCalculator_CheckImmunity_Grants_Immunity()
        {
            var shieldDef = MakeDef("immune_shield",
                parms: new Dictionary<string, object> { { "grants_immunity", BuffCategory.Debuff } });
            var defs = new[] { shieldDef };
            var active = new[] { new BuffInstance { DefId = "immune_shield", Stacks = 1 } };

            var incoming = MakeDef("poison", category: BuffCategory.Debuff, isDebuff: true);

            bool immune = BuffCalculator.CheckImmunity(active, defs, incoming);
            Assert.IsTrue(immune, "Shield should grant immunity to debuff category");
        }

        [Test]
        public void BuffCalculator_CheckImmunity_No_Immunity()
        {
            var def = MakeDef("atk_up");
            var defs = new[] { def };
            var active = new[] { new BuffInstance { DefId = "atk_up", Stacks = 1 } };

            var incoming = MakeDef("poison", category: BuffCategory.Debuff, isDebuff: true);

            bool immune = BuffCalculator.CheckImmunity(active, defs, incoming);
            Assert.IsFalse(immune, "No immunity buff should not block");
        }

        [Test]
        public void BuffCalculator_ResolveStackConflict_Replace_Returns_True()
        {
            var def = MakeDef("atk_up", BuffStackRule.Replace);
            var existing = new BuffInstance { DefId = "atk_up", Stacks = 1, Value = 5f };
            Assert.IsTrue(BuffCalculator.ResolveStackConflict(existing, def, 10f));
        }

        [Test]
        public void BuffCalculator_ResolveStackConflict_Highest_Value_Comparison()
        {
            var def = MakeDef("atk_up", BuffStackRule.Highest);
            var existing = new BuffInstance { DefId = "atk_up", Stacks = 1, Value = 20f };

            Assert.IsFalse(BuffCalculator.ResolveStackConflict(existing, def, 10f),
                "Lower value should not replace");
            Assert.IsTrue(BuffCalculator.ResolveStackConflict(existing, def, 30f),
                "Higher value should replace");
        }

        [Test]
        public void BuffCalculator_ResolveStackConflict_Stack_Under_Max()
        {
            var def = MakeDef("poison", BuffStackRule.Stack, maxStacks: 3);
            var existing = new BuffInstance { DefId = "poison", Stacks = 2 };
            Assert.IsTrue(BuffCalculator.ResolveStackConflict(existing, def, 0f),
                "Under max stacks should allow stacking");
        }

        [Test]
        public void BuffCalculator_ResolveStackConflict_Stack_At_Max()
        {
            var def = MakeDef("poison", BuffStackRule.Stack, maxStacks: 3);
            var existing = new BuffInstance { DefId = "poison", Stacks = 3 };
            Assert.IsFalse(BuffCalculator.ResolveStackConflict(existing, def, 0f),
                "At max stacks should not allow more stacking");
        }
    }
}
