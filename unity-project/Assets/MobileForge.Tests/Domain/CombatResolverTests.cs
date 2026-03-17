using System;
using System.Collections.Generic;
using NUnit.Framework;
using MobileForge.Domain;

namespace MobileForge.Tests.Domain
{
    [TestFixture]
    public class CombatResolverTests
    {
        private ElementChart _chart;
        private CombatResolver _resolver;

        [SetUp]
        public void SetUp()
        {
            _chart = new ElementChart();
            _resolver = new CombatResolver(_chart);
        }

        // -----------------------------------------------------------------
        // Helper
        // -----------------------------------------------------------------

        private DamageContext MakeCtx(float atk, int atkElem, int defElem, float defense,
            int gems, int comboCount, int comboIndex = 0)
        {
            return new DamageContext
            {
                AttackerAtk = atk,
                AttackerElement = atkElem,
                DefenderElement = defElem,
                DefenderDefense = defense,
                GemsMatched = gems,
                ComboCount = comboCount,
                ComboIndex = comboIndex,
            };
        }

        // Hook call tracking
        private List<string> _hookCallOrder = new();

        private void HookDoubleDamage(DamageContext ctx)
        {
            ctx.Damage *= 2.0f;
        }

        private void HookAdd100(DamageContext ctx)
        {
            ctx.Damage += 100.0f;
        }

        private void HookTrackFirst(DamageContext ctx)
        {
            _hookCallOrder.Add("first");
        }

        private void HookTrackSecond(DamageContext ctx)
        {
            _hookCallOrder.Add("second");
        }

        // -----------------------------------------------------------------
        // Tests -- damage_calc_cases test vectors
        // -----------------------------------------------------------------

        [Test]
        public void Basic_3_Gem_Single_Combo()
        {
            // 1000 ATK, 3 water gems, 1 combo, water vs water (neutral)
            var ctx = MakeCtx(1000f, 1, 1, 0f, 3, 1);
            var result = _resolver.ResolvePlayerAttack(ctx);

            Assert.AreEqual(1000, result.FinalDamage, "Basic 3-gem single combo should deal 1000 damage");
            Assert.AreEqual(1.0f, result.ComboMultiplier, 0.001f, "Combo multiplier should be 1.0");
            Assert.AreEqual(1.0f, result.ElementMultiplier, 0.001f, "Element multiplier should be 1.0");
        }

        [Test]
        public void Five_Gems_3_Combos_Advantage()
        {
            // 1000 ATK, 5 gems, 3 combos, water vs fire (1.5x advantage)
            var ctx = MakeCtx(1000f, 1, 2, 0f, 5, 3, 2);
            var result = _resolver.ResolvePlayerAttack(ctx);

            Assert.AreEqual(3375, result.FinalDamage, "5 gems, 3 combos, advantage should deal 3375");
            Assert.AreEqual(1.5f, result.ElementMultiplier, 0.001f, "Element multiplier should be 1.5");
            Assert.AreEqual(1.5f, result.ComboMultiplier, 0.001f, "Combo multiplier should be 1.5");
        }

        [Test]
        public void Disadvantage_With_Defense()
        {
            // 2000 ATK, 4 gems, 2 combos, fire vs water (0.5x), 500 defense
            var ctx = MakeCtx(2000f, 2, 1, 500f, 4, 2, 1);
            var result = _resolver.ResolvePlayerAttack(ctx);

            Assert.AreEqual(1063, result.FinalDamage, "Disadvantage with defense should deal 1063");
            Assert.AreEqual(0.5f, result.ElementMultiplier, 0.001f, "Element multiplier should be 0.5");
            Assert.AreEqual(1.25f, result.ComboMultiplier, 0.001f, "Combo multiplier should be 1.25");
        }

        [Test]
        public void Minimum_Damage_1()
        {
            // Very high defense still gives minimum 1
            var ctx = MakeCtx(100f, 3, 3, 99999f, 3, 1);
            var result = _resolver.ResolvePlayerAttack(ctx);

            Assert.AreEqual(1, result.FinalDamage, "Minimum damage should be 1");
        }

        [Test]
        public void Light_Dark_Mutual()
        {
            // Light vs Dark = 1.5x
            var ctx = MakeCtx(1000f, 4, 5, 0f, 3, 1);
            var result = _resolver.ResolvePlayerAttack(ctx);
            Assert.AreEqual(1500, result.FinalDamage, "Light vs dark should deal 1500");
            Assert.AreEqual(1.5f, result.ElementMultiplier, 0.001f);

            // Dark vs Light = also 1.5x
            var ctx2 = MakeCtx(1000f, 5, 4, 0f, 3, 1);
            var result2 = _resolver.ResolvePlayerAttack(ctx2);
            Assert.AreEqual(1500, result2.FinalDamage, "Dark vs light should also deal 1500");
            Assert.AreEqual(1.5f, result2.ElementMultiplier, 0.001f);
        }

        [Test]
        public void Heart_No_Advantage()
        {
            // Heart vs anything = 1.0x (neutral)
            var ctx = MakeCtx(1000f, 6, 1, 0f, 3, 1);
            var result = _resolver.ResolvePlayerAttack(ctx);

            Assert.AreEqual(1000, result.FinalDamage, "Heart should deal neutral damage");
            Assert.AreEqual(1.0f, result.ElementMultiplier, 0.001f);
        }

        [Test]
        public void Ten_Combo_High_Gems()
        {
            // 500 ATK, 8 gems, 10 combos, water vs fire, 100 defense
            var ctx = MakeCtx(500f, 1, 2, 100f, 8, 10, 9);
            var result = _resolver.ResolvePlayerAttack(ctx);

            Assert.AreEqual(5384, result.FinalDamage, "10 combo high gems should deal 5384");
            Assert.AreEqual(3.25f, result.ComboMultiplier, 0.001f, "Combo multiplier should be 3.25");
            Assert.AreEqual(1.5f, result.ElementMultiplier, 0.001f, "Element multiplier should be 1.5");
        }

        // -----------------------------------------------------------------
        // Tests -- hooks
        // -----------------------------------------------------------------

        [Test]
        public void Hook_Modifies_Damage()
        {
            // Register a MAIN hook that doubles damage
            _resolver.RegisterHook(DamageHook.Main, HookDoubleDamage, 100, "double");
            var ctx = MakeCtx(1000f, 1, 1, 0f, 3, 1);
            var result = _resolver.ResolvePlayerAttack(ctx);

            // Base = 1000, combo = 1.0, elem = 1.0, MAIN doubles -> 2000, no defense
            Assert.AreEqual(2000, result.FinalDamage, "Hook should double the damage");
            Assert.Contains("double", result.HooksApplied, "Hook name should be in HooksApplied");
        }

        [Test]
        public void Hook_Unregister()
        {
            // Register then unregister hook
            _resolver.RegisterHook(DamageHook.Main, HookDoubleDamage, 100, "double");
            _resolver.UnregisterHook(DamageHook.Main, HookDoubleDamage);

            var ctx = MakeCtx(1000f, 1, 1, 0f, 3, 1);
            var result = _resolver.ResolvePlayerAttack(ctx);

            Assert.AreEqual(1000, result.FinalDamage, "Unregistered hook should not affect damage");
            Assert.IsFalse(result.HooksApplied.Contains("double"), "Hook name should not be in HooksApplied");
        }

        [Test]
        public void Hook_Priority()
        {
            // Two hooks with different priorities fire in correct order
            // Lower priority number fires first
            _hookCallOrder.Clear();
            _resolver.RegisterHook(DamageHook.Main, HookTrackSecond, 200, "second");
            _resolver.RegisterHook(DamageHook.Main, HookTrackFirst, 50, "first");

            var ctx = MakeCtx(1000f, 1, 1, 0f, 3, 1);
            _resolver.ResolvePlayerAttack(ctx);

            Assert.AreEqual(2, _hookCallOrder.Count, "Both hooks should fire");
            Assert.AreEqual("first", _hookCallOrder[0], "Lower priority hook should fire first");
            Assert.AreEqual("second", _hookCallOrder[1], "Higher priority hook should fire second");
        }
    }
}
