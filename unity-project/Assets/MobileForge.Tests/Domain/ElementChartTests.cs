using NUnit.Framework;
using MobileForge.Domain;

namespace MobileForge.Tests.Domain
{
    [TestFixture]
    public class ElementChartTests
    {
        private ElementChart _chart;

        [SetUp]
        public void SetUp()
        {
            _chart = new ElementChart();
        }

        // -----------------------------------------------------------------
        // Tests -- element_advantage_cases test vectors
        // -----------------------------------------------------------------

        [Test]
        public void Water_Beats_Fire()
        {
            float mult = _chart.GetMultiplier((int)Element.Water, (int)Element.Fire);
            Assert.AreEqual(1.5f, mult, 0.001f, "Water vs Fire should be 1.5x");
        }

        [Test]
        public void Fire_Beats_Grass()
        {
            float mult = _chart.GetMultiplier((int)Element.Fire, (int)Element.Grass);
            Assert.AreEqual(1.5f, mult, 0.001f, "Fire vs Grass should be 1.5x");
        }

        [Test]
        public void Grass_Beats_Water()
        {
            float mult = _chart.GetMultiplier((int)Element.Grass, (int)Element.Water);
            Assert.AreEqual(1.5f, mult, 0.001f, "Grass vs Water should be 1.5x");
        }

        [Test]
        public void Light_Dark_Mutual()
        {
            float lightVsDark = _chart.GetMultiplier((int)Element.Light, (int)Element.Dark);
            Assert.AreEqual(1.5f, lightVsDark, 0.001f, "Light vs Dark should be 1.5x");

            float darkVsLight = _chart.GetMultiplier((int)Element.Dark, (int)Element.Light);
            Assert.AreEqual(1.5f, darkVsLight, 0.001f, "Dark vs Light should be 1.5x");
        }

        [Test]
        public void Water_Weak_To_Grass()
        {
            float mult = _chart.GetMultiplier((int)Element.Water, (int)Element.Grass);
            Assert.AreEqual(0.5f, mult, 0.001f, "Water vs Grass should be 0.5x");
        }

        [Test]
        public void Same_Element_Neutral()
        {
            float water = _chart.GetMultiplier((int)Element.Water, (int)Element.Water);
            Assert.AreEqual(1.0f, water, 0.001f, "Same element should be 1.0x (neutral)");

            float fire = _chart.GetMultiplier((int)Element.Fire, (int)Element.Fire);
            Assert.AreEqual(1.0f, fire, 0.001f, "Same element should be 1.0x (neutral)");
        }

        [Test]
        public void Heart_Neutral()
        {
            // Heart vs everything should be neutral
            Assert.AreEqual(1.0f, _chart.GetMultiplier((int)Element.Heart, (int)Element.Water), 0.001f,
                "Heart vs Water should be 1.0x");
            Assert.AreEqual(1.0f, _chart.GetMultiplier((int)Element.Heart, (int)Element.Fire), 0.001f,
                "Heart vs Fire should be 1.0x");
            Assert.AreEqual(1.0f, _chart.GetMultiplier((int)Element.Heart, (int)Element.Grass), 0.001f,
                "Heart vs Grass should be 1.0x");
            Assert.AreEqual(1.0f, _chart.GetMultiplier((int)Element.Heart, (int)Element.Light), 0.001f,
                "Heart vs Light should be 1.0x");
            Assert.AreEqual(1.0f, _chart.GetMultiplier((int)Element.Heart, (int)Element.Dark), 0.001f,
                "Heart vs Dark should be 1.0x");

            // Anything vs heart should also be neutral
            Assert.AreEqual(1.0f, _chart.GetMultiplier((int)Element.Water, (int)Element.Heart), 0.001f,
                "Water vs Heart should be 1.0x");
            Assert.AreEqual(1.0f, _chart.GetMultiplier((int)Element.Fire, (int)Element.Heart), 0.001f,
                "Fire vs Heart should be 1.0x");
        }
    }
}
