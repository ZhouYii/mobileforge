using System;
using System.Collections.Generic;
using NUnit.Framework;
using MobileForge.Presentation;

namespace MobileForge.Tests.Presentation
{
    [TestFixture]
    public class PopupStackTests
    {
        private PopupStack _stack;

        [SetUp]
        public void SetUp()
        {
            _stack = new PopupStack();
        }

        private static Func<IPopup> MakePopupFactory()
        {
            return () => new StubPopup();
        }

        // -----------------------------------------------------------------
        // Tests
        // -----------------------------------------------------------------

        [Test]
        public void Show_Queues_Popup()
        {
            _stack.Show("alert", MakePopupFactory(), new Dictionary<string, object> { { "title", "Hello" } }, 0);

            Assert.AreEqual(1, _stack.PopupCount);
        }

        [Test]
        public void Dismiss_Removes_Popup()
        {
            _stack.Show("alert", MakePopupFactory(), new Dictionary<string, object> { { "title", "Hello" } }, 0);
            _stack.Dismiss();

            Assert.AreEqual(0, _stack.PopupCount);
        }

        [Test]
        public void Priority_Ordering_Higher_Shows_First()
        {
            _stack.Show("low", MakePopupFactory(), new Dictionary<string, object> { { "title", "Low" } }, 1);
            _stack.Show("high", MakePopupFactory(), new Dictionary<string, object> { { "title", "High" } }, 5);

            Assert.AreEqual("high", _stack.CurrentPopupId, "Higher priority popup should show first");
        }

        [Test]
        public void Dismiss_Shows_Next_Popup()
        {
            _stack.Show("first", MakePopupFactory(), new Dictionary<string, object> { { "title", "1st" } }, 5);
            _stack.Show("second", MakePopupFactory(), new Dictionary<string, object> { { "title", "2nd" } }, 1);

            Assert.AreEqual(2, _stack.PopupCount);

            _stack.Dismiss();

            Assert.AreEqual(1, _stack.PopupCount);
            Assert.AreEqual("second", _stack.CurrentPopupId, "Second popup should now be showing");
        }

        [Test]
        public void DismissAll_Clears_All_Popups()
        {
            _stack.Show("a", MakePopupFactory(), new Dictionary<string, object>(), 1);
            _stack.Show("b", MakePopupFactory(), new Dictionary<string, object>(), 2);
            _stack.Show("c", MakePopupFactory(), new Dictionary<string, object>(), 3);

            Assert.AreEqual(3, _stack.PopupCount);

            _stack.DismissAll();

            Assert.AreEqual(0, _stack.PopupCount);
        }

        [Test]
        public void IsShowing_Tracks_State()
        {
            Assert.IsFalse(_stack.IsShowing, "Should not be showing initially");

            _stack.Show("notice", MakePopupFactory(), new Dictionary<string, object>(), 0);
            Assert.IsTrue(_stack.IsShowing, "Should be showing after Show");

            _stack.Dismiss();
            Assert.IsFalse(_stack.IsShowing, "Should not be showing after dismiss");
        }

        [Test]
        public void OnDismiss_Callback_Receives_Result()
        {
            object receivedResult = null;

            _stack.Show("confirm", MakePopupFactory(), new Dictionary<string, object> { { "title", "OK?" } }, 0,
                result => receivedResult = result);

            _stack.Dismiss(new Dictionary<string, object> { { "accepted", true } });

            Assert.IsNotNull(receivedResult, "Dismiss callback should be called");
            var resultDict = receivedResult as Dictionary<string, object>;
            Assert.IsNotNull(resultDict, "Result should be a Dictionary<string, object>");
            Assert.AreEqual(true, resultDict["accepted"], "Callback should receive dismiss result");
        }

        [Test]
        public void Empty_Dismiss_Does_Not_Throw()
        {
            Assert.DoesNotThrow(() =>
            {
                _stack.Dismiss();
            }, "Dismiss on empty stack should not throw");

            Assert.AreEqual(0, _stack.PopupCount);
        }

        /// <summary>
        /// Minimal IPopup stub for testing PopupStack.
        /// </summary>
        private class StubPopup : IPopup
        {
            public string PopupId { get; set; }

            public event Action<object> Dismissed;

            public void OnShow(Dictionary<string, object> parameters) { }

            public void Dismiss(object result = null)
            {
                Dismissed?.Invoke(result);
            }
        }
    }
}
