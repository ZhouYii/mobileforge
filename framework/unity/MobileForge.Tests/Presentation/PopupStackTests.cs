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

        // -----------------------------------------------------------------
        // Tests
        // -----------------------------------------------------------------

        [Test]
        public void Show_Queues_Popup()
        {
            _stack.ShowPopup("alert", new Dictionary<string, object> { { "title", "Hello" } }, 0);

            Assert.AreEqual(1, _stack.PopupCount);
        }

        [Test]
        public void Dismiss_Removes_Popup()
        {
            _stack.ShowPopup("alert", new Dictionary<string, object> { { "title", "Hello" } }, 0);
            _stack.Dismiss();

            Assert.AreEqual(0, _stack.PopupCount);
        }

        [Test]
        public void Priority_Ordering_Higher_Shows_First()
        {
            _stack.ShowPopup("low", new Dictionary<string, object> { { "title", "Low" } }, 1);
            _stack.ShowPopup("high", new Dictionary<string, object> { { "title", "High" } }, 5);

            Assert.AreEqual("high", _stack.CurrentPopupId, "Higher priority popup should show first");
        }

        [Test]
        public void Dismiss_Shows_Next_Popup()
        {
            _stack.ShowPopup("first", new Dictionary<string, object> { { "title", "1st" } }, 5);
            _stack.ShowPopup("second", new Dictionary<string, object> { { "title", "2nd" } }, 1);

            Assert.AreEqual(2, _stack.PopupCount);

            _stack.Dismiss();

            Assert.AreEqual(1, _stack.PopupCount);
            Assert.AreEqual("second", _stack.CurrentPopupId, "Second popup should now be showing");
        }

        [Test]
        public void DismissAll_Clears_All_Popups()
        {
            _stack.ShowPopup("a", new Dictionary<string, object>(), 1);
            _stack.ShowPopup("b", new Dictionary<string, object>(), 2);
            _stack.ShowPopup("c", new Dictionary<string, object>(), 3);

            Assert.AreEqual(3, _stack.PopupCount);

            _stack.DismissAll();

            Assert.AreEqual(0, _stack.PopupCount);
        }

        [Test]
        public void IsShowing_Tracks_State()
        {
            Assert.IsFalse(_stack.IsShowing, "Should not be showing initially");

            _stack.ShowPopup("notice", new Dictionary<string, object>(), 0);
            Assert.IsTrue(_stack.IsShowing, "Should be showing after ShowPopup");

            _stack.Dismiss();
            Assert.IsFalse(_stack.IsShowing, "Should not be showing after dismiss");
        }

        [Test]
        public void OnDismiss_Callback_Receives_Result()
        {
            Dictionary<string, object> receivedResult = null;

            _stack.ShowPopup("confirm", new Dictionary<string, object> { { "title", "OK?" } }, 0,
                result => receivedResult = result);

            _stack.Dismiss(new Dictionary<string, object> { { "accepted", true } });

            Assert.IsNotNull(receivedResult, "Dismiss callback should be called");
            Assert.AreEqual(true, receivedResult["accepted"], "Callback should receive dismiss result");
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
    }
}
