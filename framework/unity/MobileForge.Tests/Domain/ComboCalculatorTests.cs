using NUnit.Framework;
using MobileForge.Domain;

namespace MobileForge.Tests.Domain
{
    [TestFixture]
    public class ComboCalculatorTests
    {
        [Test]
        public void ComboMultiplier_OneCombo_Returns1()
        {
            float result = ComboCalculator.Calculate(1);
            Assert.AreEqual(1.0f, result, 0.001f, "1 combo = 1.0x multiplier");
        }

        [Test]
        public void ComboMultiplier_FiveCombos_Returns2()
        {
            float result = ComboCalculator.Calculate(5);
            Assert.AreEqual(2.0f, result, 0.001f, "5 combos = 1 + (5-1) * 0.25 = 2.0x");
        }

        [Test]
        public void ComboMultiplier_TenCombos_Returns3_25()
        {
            float result = ComboCalculator.Calculate(10);
            Assert.AreEqual(3.25f, result, 0.001f, "10 combos = 1 + (10-1) * 0.25 = 3.25x");
        }

        [Test]
        public void GemDamage_BaseFormula()
        {
            float atk = 1000f;

            float dmg3 = ComboCalculator.GemDamage(atk, 3);
            Assert.AreEqual(1000f, dmg3, 0.001f, "3 gems = base damage (1.0x)");

            float dmg4 = ComboCalculator.GemDamage(atk, 4);
            Assert.AreEqual(1250f, dmg4, 0.001f, "4 gems = 1.25x");

            float dmg5 = ComboCalculator.GemDamage(atk, 5);
            Assert.AreEqual(1500f, dmg5, 0.001f, "5 gems = 1.5x");

            float dmg6 = ComboCalculator.GemDamage(atk, 6);
            Assert.AreEqual(1750f, dmg6, 0.001f, "6 gems = 1.75x");
        }

        [Test]
        public void GemDamage_MinimumThreeGems()
        {
            float atk = 1000f;

            float dmg3 = ComboCalculator.GemDamage(atk, 3);
            Assert.AreEqual(1000f, dmg3, 0.001f, "3 gems = base multiplier of 1.0");

            float dmg2 = ComboCalculator.GemDamage(atk, 2);
            Assert.AreEqual(1000f, dmg2, 0.001f, "2 gems = still base (formula max(0, gems-3))");

            float dmg1 = ComboCalculator.GemDamage(atk, 1);
            Assert.AreEqual(1000f, dmg1, 0.001f, "1 gem = still base");
        }

        [Test]
        public void ComboMultiplier_ZeroCombo_Returns0()
        {
            float result = ComboCalculator.Calculate(0);
            Assert.AreEqual(0.0f, result, 0.001f, "0 combos = 0.0x (no damage)");
        }

        [Test]
        public void ComboMultiplier_NegativeCombo_Returns0()
        {
            float result = ComboCalculator.Calculate(-1);
            Assert.AreEqual(0.0f, result, 0.001f, "Negative combo = 0.0x");
        }

        [Test]
        public void GemDamage_ZeroGems_Returns0()
        {
            float result = ComboCalculator.GemDamage(1000f, 0);
            Assert.AreEqual(0.0f, result, 0.001f, "0 gems = 0 damage");
        }
    }
}
