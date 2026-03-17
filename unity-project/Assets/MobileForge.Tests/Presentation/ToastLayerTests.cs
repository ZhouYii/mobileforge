using System.Collections.Generic;
using NUnit.Framework;
using MobileForge.Presentation;

namespace MobileForge.Tests.Presentation
{
    [TestFixture]
    public class ToastLayerTests
    {
        private ToastLayer _toastLayer;
        private List<int> _shownIds;
        private List<int> _removedIds;

        [SetUp]
        public void SetUp()
        {
            _toastLayer = new ToastLayer();
            _shownIds = new List<int>();
            _removedIds = new List<int>();

            _toastLayer.OnToastShown += (text, duration, id) => _shownIds.Add(id);
            _toastLayer.OnToastRemoved += (id) => _removedIds.Add(id);
        }

        [Test]
        public void ShowToast_CreatesToastItem()
        {
            var id = _toastLayer.ShowToast("Test message", 2.0f);

            Assert.GreaterOrEqual(id, 0, "ShowToast should return valid ID");
            Assert.AreEqual(1, _toastLayer.ActiveCount, "Should have 1 active toast");
            Assert.AreEqual(1, _shownIds.Count, "OnToastShown should fire once");
        }

        [Test]
        public void AutoDismiss_RemovesAfterDuration()
        {
            _toastLayer.ShowToast("Test", 1.0f);

            Assert.AreEqual(1, _toastLayer.ActiveCount);

            _toastLayer.Update(0.5f);
            Assert.AreEqual(1, _toastLayer.ActiveCount, "Should still be visible at 0.5s");

            _toastLayer.Update(0.6f);
            Assert.AreEqual(0, _toastLayer.ActiveCount, "Should be dismissed after duration");
            Assert.AreEqual(1, _removedIds.Count, "OnToastRemoved should fire once");
        }

        [Test]
        public void MultipleToasts_StackVertically()
        {
            _toastLayer.ShowToast("First", 2.0f);
            _toastLayer.ShowToast("Second", 2.0f);
            _toastLayer.ShowToast("Third", 2.0f);

            Assert.AreEqual(3, _toastLayer.ActiveCount);

            var texts = _toastLayer.GetActiveTexts();
            Assert.AreEqual(3, texts.Count);
            Assert.Contains("First", texts);
            Assert.Contains("Second", texts);
            Assert.Contains("Third", texts);
        }

        [Test]
        public void ShowToast_NullText_ReturnsInvalid()
        {
            var id = _toastLayer.ShowToast(null, 2.0f);

            Assert.AreEqual(-1, id, "Null text should return -1");
            Assert.AreEqual(0, _toastLayer.ActiveCount);
        }

        [Test]
        public void ShowToast_EmptyText_ReturnsInvalid()
        {
            var id = _toastLayer.ShowToast("", 2.0f);

            Assert.AreEqual(-1, id, "Empty text should return -1");
            Assert.AreEqual(0, _toastLayer.ActiveCount);
        }

        [Test]
        public void ShowToast_NegativeDuration_UsesDefault()
        {
            var id = _toastLayer.ShowToast("Test", -1.0f);

            Assert.GreaterOrEqual(id, 0, "Should accept negative duration with default");
        }

        [Test]
        public void ClearAll_RemovesAllToasts()
        {
            _toastLayer.ShowToast("A", 5.0f);
            _toastLayer.ShowToast("B", 5.0f);
            _toastLayer.ShowToast("C", 5.0f);

            Assert.AreEqual(3, _toastLayer.ActiveCount);

            _toastLayer.ClearAll();

            Assert.AreEqual(0, _toastLayer.ActiveCount, "ClearAll should remove all toasts");
            Assert.AreEqual(3, _removedIds.Count, "OnToastRemoved should fire for each");
        }

        [Test]
        public void Update_RemovesMultipleExpiredToasts()
        {
            _toastLayer.ShowToast("A", 1.0f);
            _toastLayer.ShowToast("B", 1.0f);
            _toastLayer.ShowToast("C", 2.0f);

            _toastLayer.Update(1.5f);

            Assert.AreEqual(1, _toastLayer.ActiveCount, "Only C should remain");
            Assert.AreEqual(2, _removedIds.Count, "A and B should be removed");
        }

        [Test]
        public void SequentialIds_AreUnique()
        {
            var ids = new HashSet<int>();
            for (int i = 0; i < 100; i++)
            {
                var id = _toastLayer.ShowToast($"Toast {i}", 5.0f);
                Assert.IsFalse(ids.Contains(id), $"ID {id} should be unique");
                ids.Add(id);
            }
        }
    }
}
