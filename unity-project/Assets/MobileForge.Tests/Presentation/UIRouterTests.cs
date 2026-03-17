using NUnit.Framework;
using System.Collections.Generic;
using MobileForge.Presentation;

namespace MobileForge.Tests.Presentation
{
    [TestFixture]
    public class UIRouterTests
    {
        private ScreenRegistry _registry;
        private UIRouter _router;

        [SetUp]
        public void SetUp()
        {
            _registry = new ScreenRegistry();
            _router = new UIRouter(_registry);
            _router.Register("home", _ => new object());
            _router.Register("shop", _ => new object());
            _router.Register("inventory", _ => new object());
            _router.Register("settings", _ => new object());
        }

        // -----------------------------------------------------------------
        // Tests
        // -----------------------------------------------------------------

        [Test]
        public void RegisterScreen_Makes_Screen_Available()
        {
            Assert.IsTrue(_registry.HasScreen("home"), "home screen should be registered");
            Assert.IsTrue(_registry.HasScreen("shop"), "shop screen should be registered");
            Assert.IsFalse(_registry.HasScreen("nonexistent"), "nonexistent screen should not be registered");
        }

        [Test]
        public void Navigate_Sets_Current_Screen()
        {
            _router.Navigate("home");
            Assert.AreEqual("home", _router.CurrentScreenId);

            _router.Navigate("shop");
            Assert.AreEqual("shop", _router.CurrentScreenId, "Current screen should update after navigate");
        }

        [Test]
        public void Push_Increases_Stack_Depth()
        {
            _router.Navigate("home");
            int initialDepth = _router.StackDepth;

            _router.Push("shop");

            Assert.AreEqual(initialDepth + 1, _router.StackDepth, "Push should increase stack depth by 1");
        }

        [Test]
        public void Pop_Decreases_Stack_Depth()
        {
            _router.Navigate("home");
            _router.Push("shop");
            _router.Push("inventory");
            int depthBefore = _router.StackDepth;

            _router.Pop();

            Assert.AreEqual(depthBefore - 1, _router.StackDepth, "Pop should decrease stack depth by 1");
            Assert.AreEqual("shop", _router.CurrentScreenId, "Current screen should be shop after popping inventory");
        }

        [Test]
        public void Pop_Single_Screen_Is_NoOp()
        {
            _router.Navigate("home");
            Assert.AreEqual(1, _router.StackDepth);

            _router.Pop();

            Assert.AreEqual(1, _router.StackDepth, "Stack depth should still be 1 after pop on single screen");
            Assert.AreEqual("home", _router.CurrentScreenId, "Current screen should still be home");
        }

        [Test]
        public void Replace_Keeps_Same_Stack_Depth()
        {
            _router.Navigate("home");
            _router.Push("shop");
            int depthBefore = _router.StackDepth;

            _router.Replace("inventory");

            Assert.AreEqual(depthBefore, _router.StackDepth, "Replace should not change stack depth");
            Assert.AreEqual("inventory", _router.CurrentScreenId, "Current screen should be inventory after replace");
        }

        [Test]
        public void Navigate_Clears_Stack()
        {
            _router.Navigate("home");
            _router.Push("shop");
            _router.Push("inventory");
            Assert.AreEqual(3, _router.StackDepth, "Stack depth should be 3 after pushing 3 screens");

            _router.Navigate("settings");

            Assert.AreEqual(1, _router.StackDepth, "Navigate should reset stack depth to 1");
            Assert.AreEqual("settings", _router.CurrentScreenId, "Current screen should be settings");
        }
    }
}
