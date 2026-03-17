using System.Collections.Generic;
using NUnit.Framework;
using MobileForge.Presentation;
using MobileForge.Infrastructure;

namespace MobileForge.Tests.Presentation
{
    [TestFixture]
    public class CurrencyBarTests
    {
        private CurrencyBar _bar;
        private EventBus _eventBus;
        private List<long> _displayChanges;

        [SetUp]
        public void SetUp()
        {
            _bar = new CurrencyBar();
            _eventBus = new EventBus();
            _displayChanges = new List<long>();
            _bar.OnDisplayChanged += (value) => _displayChanges.Add(value);
        }

        [TearDown]
        public void TearDown()
        {
            _bar?.Unbind();
            _eventBus?.ClearAll();
        }

        [Test]
        public void Bind_SubscribesToEventBus()
        {
            int initialCount = _eventBus.SubscriberCount(EventNames.StateChanged);

            _bar.Bind("gems", _eventBus);

            Assert.AreEqual(initialCount + 1, _eventBus.SubscriberCount(EventNames.StateChanged),
                "Bind should subscribe to StateChanged events");
        }

        [Test]
        public void CurrencyChanged_UpdatesDisplay()
        {
            _bar.Bind("gems", _eventBus);
            _bar.SetImmediate(100);

            _eventBus.Emit(EventNames.StateChanged, new Dictionary<string, object>
            {
                { "section", "currency" },
                { "key", "gems" },
                { "new_value", 200L }
            });

            Assert.AreEqual(200, _bar.TargetValue, "Target should update from event");
        }

        [Test]
        public void AnimatedCount_InterpolatesOverTime()
        {
            _bar.Bind("gems", _eventBus);
            _bar.AnimationSpeed = 100f;
            _bar.SetImmediate(0);

            _bar.SetValue(500);

            Assert.IsTrue(_bar.IsAnimating, "Should be animating");

            _bar.Update(1.0f);
            Assert.AreEqual(100, _bar.CurrentDisplay, "After 1s at 100/s, should be at 100");

            _bar.Update(2.0f);
            Assert.AreEqual(300, _bar.CurrentDisplay, "After 3s total at 100/s, should be at 300");

            _bar.Update(3.0f);
            Assert.AreEqual(500, _bar.CurrentDisplay, "Should reach target");
            Assert.IsFalse(_bar.IsAnimating, "Should stop animating when target reached");
        }

        [Test]
        public void Unbind_UnsubscribesFromEventBus()
        {
            _bar.Bind("gems", _eventBus);
            int subscribedCount = _eventBus.SubscriberCount(EventNames.StateChanged);

            _bar.Unbind();

            Assert.AreEqual(subscribedCount - 1, _eventBus.SubscriberCount(EventNames.StateChanged),
                "Unbind should unsubscribe from events");
        }

        [Test]
        public void SetImmediate_SetsBothTargetAndDisplay()
        {
            _bar.Bind("gems", _eventBus);

            _bar.SetImmediate(500);

            Assert.AreEqual(500, _bar.TargetValue);
            Assert.AreEqual(500, _bar.CurrentDisplay);
            Assert.IsFalse(_bar.IsAnimating, "Should not be animating after SetImmediate");
        }

        [Test]
        public void SetValue_OnlySetsTarget()
        {
            _bar.Bind("gems", _eventBus);
            _bar.SetImmediate(0);

            _bar.SetValue(500);

            Assert.AreEqual(500, _bar.TargetValue);
            Assert.AreEqual(0, _bar.CurrentDisplay, "Display should still be 0");
            Assert.IsTrue(_bar.IsAnimating, "Should be animating");
        }

        [Test]
        public void CurrencyChanged_IgnoresOtherCurrencies()
        {
            _bar.Bind("gems", _eventBus);
            _bar.SetImmediate(100);

            _eventBus.Emit(EventNames.StateChanged, new Dictionary<string, object>
            {
                { "section", "currency" },
                { "key", "coins" },
                { "new_value", 5000L }
            });

            Assert.AreEqual(100, _bar.TargetValue, "Should not update for different currency");
        }

        [Test]
        public void Update_FiresOnDisplayChanged()
        {
            _bar.Bind("gems", _eventBus);
            _bar.AnimationSpeed = 100f;
            _bar.SetImmediate(0);
            _displayChanges.Clear();

            _bar.SetValue(50);
            _bar.Update(0.5f);

            Assert.Greater(_displayChanges.Count, 0, "Update should fire OnDisplayChanged");
        }
    }
}
