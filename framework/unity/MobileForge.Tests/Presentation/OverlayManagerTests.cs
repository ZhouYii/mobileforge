using System.Collections.Generic;
using NUnit.Framework;
using MobileForge.Presentation;

namespace MobileForge.Tests.Presentation
{
    [TestFixture]
    public class OverlayManagerTests
    {
        private OverlayManager _manager;
        private List<string> _shownEvents;
        private List<string> _hiddenEvents;

        [SetUp]
        public void SetUp()
        {
            _manager = new OverlayManager();
            _shownEvents = new List<string>();
            _hiddenEvents = new List<string>();

            _manager.OnOverlayShown += (id, obj) => _shownEvents.Add(id);
            _manager.OnOverlayHidden += (id) => _hiddenEvents.Add(id);
        }

        [Test]
        public void Show_CreatesOverlay()
        {
            _manager.ShowOverlay("menu", () => new object());

            Assert.IsTrue(_manager.IsShowing("menu"), "Overlay should be showing");
            Assert.AreEqual(1, _manager.ActiveCount);
            Assert.Contains("menu", _shownEvents);
        }

        [Test]
        public void Hide_RemovesOverlay()
        {
            _manager.ShowOverlay("menu", () => new object());

            _manager.HideOverlay("menu");

            Assert.IsFalse(_manager.IsShowing("menu"), "Overlay should not be showing");
            Assert.AreEqual(0, _manager.ActiveCount);
            Assert.Contains("menu", _hiddenEvents);
        }

        [Test]
        public void HideAll_RemovesAllOverlays()
        {
            _manager.ShowOverlay("menu1", () => new object());
            _manager.ShowOverlay("menu2", () => new object());
            _manager.ShowOverlay("menu3", () => new object());

            Assert.AreEqual(3, _manager.ActiveCount);

            _manager.HideAll();

            Assert.AreEqual(0, _manager.ActiveCount, "All overlays should be removed");
            Assert.AreEqual(3, _hiddenEvents.Count, "Should fire 3 hidden events");
        }

        [Test]
        public void IsShowing_ReturnsTrueForActive()
        {
            _manager.ShowOverlay("menu", () => new object());

            Assert.IsTrue(_manager.IsShowing("menu"), "IsShowing should return true for active overlay");
            Assert.IsFalse(_manager.IsShowing("nonexistent"), "IsShowing should return false for inactive overlay");
        }

        [Test]
        public void Show_DuplicateId_NoOp()
        {
            int createCount = 0;

            _manager.ShowOverlay("menu", () => { createCount++; return new object(); });
            _manager.ShowOverlay("menu", () => { createCount++; return new object(); });

            Assert.AreEqual(1, createCount, "Factory should only be called once for duplicate ID");
            Assert.AreEqual(1, _manager.ActiveCount, "Should only have 1 overlay");
            Assert.AreEqual(1, _shownEvents.Count, "Should only fire 1 shown event");
        }

        [Test]
        public void GetOverlay_ReturnsCorrectObject()
        {
            var obj = new TestOverlay { Id = 123 };
            _manager.ShowOverlay("test", () => obj);

            var result = _manager.GetOverlay("test");

            Assert.AreSame(obj, result, "GetOverlay should return the same object");
        }

        [Test]
        public void GetOverlay_NonExistent_ReturnsNull()
        {
            var result = _manager.GetOverlay("nonexistent");

            Assert.IsNull(result, "GetOverlay should return null for non-existent overlay");
        }

        [Test]
        public void Show_NullId_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => _manager.ShowOverlay(null, () => new object()));
        }

        [Test]
        public void Show_NullFactory_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() => _manager.ShowOverlay("test", null));
        }

        private class TestOverlay
        {
            public int Id { get; set; }
        }
    }
}
