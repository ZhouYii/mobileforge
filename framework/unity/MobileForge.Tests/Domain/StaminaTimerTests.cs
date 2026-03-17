using System.Collections.Generic;
using NUnit.Framework;
using MobileForge.Domain;

namespace MobileForge.Tests.Domain
{
    [TestFixture]
    public class StaminaTimerTests
    {
        private StaminaConfig _config;
        private StaminaTimer _timer;

        [SetUp]
        public void SetUp()
        {
            _config = new StaminaConfig(maxStamina: 100, refillRateSeconds: 300.0);
            _timer = new StaminaTimer(_config);
        }

        [Test]
        public void Refill_CalculatesCorrectTime()
        {
            _timer.SetLastUpdate(0.0);
            var result = _timer.CalculateRefill(50, 150.0);

            Assert.AreEqual(51, result["stamina"], "150s / 300s per point = 0.5, so should gain 0 but still have 50");
            Assert.AreEqual(150.0, result["remainder_seconds"], "Remainder should be 150");
        }

        [Test]
        public void Refill_AtMaxStamina_ReturnsZero()
        {
            _timer.SetLastUpdate(100.0);
            var result = _timer.CalculateRefill(100, 200.0);

            Assert.AreEqual(100, result["stamina"], "Should stay at max stamina");
        }

        [Test]
        public void Refill_PartialProgress_TracksCorrectly()
        {
            _timer.SetLastUpdate(0.0);

            var result1 = _timer.CalculateRefill(50, 100.0);
            Assert.AreEqual(50, result1["stamina"], "100s / 300s = 0.33, no full point yet");
            Assert.AreEqual(100.0, result1["remainder_seconds"]);

            var result2 = _timer.CalculateRefill(50, 350.0);
            Assert.AreEqual(51, result2["stamina"], "350s / 300s = 1.16, should gain 1 point");
            Assert.AreEqual(50.0, result2["remainder_seconds"]);
        }

        [Test]
        public void SecondsUntilFull_CalculatesFromCurrent()
        {
            _timer.SetLastUpdate(0.0);
            _timer.CalculateRefill(50, 0.0);

            double seconds = _timer.SecondsUntilNext(50, 0.0);
            Assert.AreEqual(300.0, seconds, 0.001, "Should be 300 seconds until next point");
        }

        [Test]
        public void SecondsUntilNext_AtMax_ReturnsZero()
        {
            _timer.SetLastUpdate(0.0);
            _timer.CalculateRefill(100, 0.0);

            double seconds = _timer.SecondsUntilNext(100, 0.0);
            Assert.AreEqual(0.0, seconds, 0.001, "At max stamina, should return 0");
        }

        [Test]
        public void Refill_MultipleCalls_AccumulatesCorrectly()
        {
            _timer.SetLastUpdate(0.0);

            _timer.CalculateRefill(0, 300.0);
            var result = _timer.CalculateRefill(1, 600.0);

            Assert.AreEqual(2, result["stamina"], "After 600 seconds at 1 stamina, should be at 2");
        }

        [Test]
        public void Refill_DoesNotExceedMax()
        {
            _timer.SetLastUpdate(0.0);

            var result = _timer.CalculateRefill(99, 1000.0);

            Assert.AreEqual(100, result["stamina"], "Should cap at max stamina");
        }
    }
}
