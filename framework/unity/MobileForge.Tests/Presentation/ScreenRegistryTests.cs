using System;
using System.Collections.Generic;
using NUnit.Framework;
using MobileForge.Presentation;

namespace MobileForge.Tests.Presentation
{
    [TestFixture]
    public class ScreenRegistryTests
    {
        private ScreenRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            _registry = new ScreenRegistry();
        }

        [Test]
        public void Register_StoresFactory()
        {
            _registry.Register("home", (p) => new object());

            Assert.IsTrue(_registry.HasScreen("home"), "home screen should be registered");
        }

        [Test]
        public void Create_ReturnsNewInstance()
        {
            var callCount = 0;
            _registry.Register("test", (p) =>
            {
                callCount++;
                return new object();
            });

            var instance1 = _registry.Create("test");
            var instance2 = _registry.Create("test");

            Assert.AreEqual(2, callCount, "Factory should be called twice");
            Assert.AreNotSame(instance1, instance2, "Each create should return a new instance");
        }

        [Test]
        public void HasScreen_ReturnsTrueForRegistered()
        {
            _registry.Register("shop", (p) => new object());

            Assert.IsTrue(_registry.HasScreen("shop"), "HasScreen should return true for registered screen");
        }

        [Test]
        public void HasScreen_ReturnsFalseForUnknown()
        {
            Assert.IsFalse(_registry.HasScreen("nonexistent"), "HasScreen should return false for unknown screen");
        }

        [Test]
        public void Create_ThrowsForUnregistered()
        {
            var ex = Assert.Throws<KeyNotFoundException>(() => _registry.Create("nonexistent"));
            Assert.IsTrue(ex.Message.Contains("nonexistent"), "Exception message should mention the screen ID");
        }

        [Test]
        public void Unregister_RemovesScreen()
        {
            _registry.Register("settings", (p) => new object());
            Assert.IsTrue(_registry.HasScreen("settings"));

            _registry.Unregister("settings");

            Assert.IsFalse(_registry.HasScreen("settings"), "Unregistered screen should not be found");
        }

        [Test]
        public void Clear_RemovesAllScreens()
        {
            _registry.Register("home", (p) => new object());
            _registry.Register("shop", (p) => new object());
            _registry.Register("settings", (p) => new object());

            _registry.Clear();

            Assert.IsFalse(_registry.HasScreen("home"));
            Assert.IsFalse(_registry.HasScreen("shop"));
            Assert.IsFalse(_registry.HasScreen("settings"));
        }

        [Test]
        public void GetRegisteredIds_ReturnsAllIds()
        {
            _registry.Register("home", (p) => new object());
            _registry.Register("shop", (p) => new object());

            var ids = _registry.GetRegisteredIds();

            Assert.AreEqual(2, ids.Count, "Should have 2 registered IDs");
            Assert.IsTrue(ids.Contains("home"));
            Assert.IsTrue(ids.Contains("shop"));
        }

        [Test]
        public void Register_NullId_Throws()
        {
            Assert.Throws<ArgumentException>(() => _registry.Register(null, (p) => new object()));
        }

        [Test]
        public void Register_EmptyId_Throws()
        {
            Assert.Throws<ArgumentException>(() => _registry.Register("", (p) => new object()));
        }

        [Test]
        public void Register_NullFactory_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _registry.Register("test", null));
        }

        [Test]
        public void Create_PassesParameters()
        {
            Dictionary<string, object> receivedParams = null;
            _registry.Register("test", (p) =>
            {
                receivedParams = p;
                return new object();
            });

            var inputParams = new Dictionary<string, object> { { "key", "value" } };
            _registry.Create("test", inputParams);

            Assert.AreSame(inputParams, receivedParams, "Parameters should be passed to factory");
        }
    }
}
