using System;
using System.Collections.Generic;
using NUnit.Framework;
using MobileForge.Domain;
using MobileForge.Infrastructure;

namespace MobileForge.Tests.Domain
{
    /// <summary>
    /// Tests for Economy -- currency management, spending, and stamina refill.
    /// Creates fresh PlayerState and EventBus per test for isolation.
    /// </summary>
    [TestFixture]
    public class EconomyTests
    {
        private EventBus _bus;
        private PlayerState _ps;
        private Economy _economy;

        // Event tracking
        private List<Dictionary<string, object>> _currencyEvents;

        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus();
            _ps = new PlayerState(_bus);
            // Register a currencies section with initial balances
            _ps.RegisterSection("currencies", new Dictionary<string, object>
            {
                { "coins", 0 },
                { "gems", 0 },
                { "stamina", 50 },
            });
            _economy = new Economy(_ps, _bus);
            _currencyEvents = new List<Dictionary<string, object>>();
        }

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

        private void OnCurrencyChanged(Dictionary<string, object> payload)
        {
            _currencyEvents.Add(payload);
        }

        // -----------------------------------------------------------------
        // Tests
        // -----------------------------------------------------------------

        [Test]
        public void Earn_Increases_Balance()
        {
            _economy.Earn("coins", 100);
            Assert.AreEqual(100, _economy.GetBalance("coins"),
                "Balance should be 100 after earning 100 coins");
        }

        [Test]
        public void Spend_Decreases_Balance()
        {
            _economy.Earn("coins", 100);
            bool ok = _economy.Spend("coins", 30);

            Assert.IsTrue(ok, "Spend should succeed when balance is sufficient");
            Assert.AreEqual(70, _economy.GetBalance("coins"),
                "Balance should be 70 after earning 100 and spending 30");
        }

        [Test]
        public void Can_Afford_True()
        {
            _economy.Earn("coins", 100);
            Assert.IsTrue(_economy.CanAfford("coins", 50),
                "Should be able to afford 50 when balance is 100");
        }

        [Test]
        public void Can_Afford_False()
        {
            _economy.Earn("coins", 100);
            Assert.IsFalse(_economy.CanAfford("coins", 200),
                "Should not be able to afford 200 when balance is 100");
        }

        [Test]
        public void Spend_Fails_When_Insufficient()
        {
            _economy.Earn("coins", 100);
            bool ok = _economy.Spend("coins", 200);

            Assert.IsFalse(ok, "Spend should return false when balance is insufficient");
            Assert.AreEqual(100, _economy.GetBalance("coins"),
                "Balance should remain 100 after failed spend");
        }

        [Test]
        public void Stamina_Timer_Refill()
        {
            // Configure stamina: refill every 300 seconds
            var config = new StaminaConfig(100, 300.0, "gems", 1);
            var timer = new StaminaTimer(config);

            // First call initializes the last_update_time
            double t0 = 1000.0;
            var result0 = timer.CalculateRefill(50, t0);
            Assert.AreEqual(50, result0["stamina"], "Initial call should return current stamina");

            // Advance 600 seconds -> 2 refill ticks at 300s each
            double t1 = t0 + 600.0;
            var result1 = timer.CalculateRefill(50, t1);
            Assert.AreEqual(52, result1["stamina"],
                "After 600s with 300s/point, should gain 2 points: 50 + 2 = 52");
        }

        [Test]
        public void Currency_Changed_Event()
        {
            _bus.Subscribe(EventNames.CurrencyChanged, OnCurrencyChanged);
            _economy.Earn("coins", 100);
            _economy.Spend("coins", 30);

            Assert.AreEqual(2, _currencyEvents.Count,
                "Should have 2 currency_changed events (earn + spend)");

            // Verify the spend event payload
            var spendEvt = _currencyEvents[1];
            Assert.AreEqual("coins", spendEvt["currency_type"],
                "Event currency_type should be 'coins'");
            Assert.AreEqual(100, Convert.ToInt32(spendEvt["old_value"]),
                "old_value should be 100 before spend");
            Assert.AreEqual(70, Convert.ToInt32(spendEvt["new_value"]),
                "new_value should be 70 after spend of 30");
        }
    }
}
