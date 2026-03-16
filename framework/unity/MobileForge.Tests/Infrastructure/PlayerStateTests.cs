using System.Collections.Generic;
using NUnit.Framework;
using MobileForge.Infrastructure;

namespace MobileForge.Tests.Infrastructure
{
    [TestFixture]
    public class PlayerStateTests
    {
        private EventBus _bus;
        private PlayerState _state;

        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus();
            _state = new PlayerState(_bus);
        }

        [Test]
        public void RegisterSection_Creates_Section()
        {
            var section = _state.RegisterSection("inventory");
            Assert.IsNotNull(section);
            Assert.IsTrue(_state.HasSection("inventory"));
        }

        [Test]
        public void GetSection_Returns_Null_For_Missing()
        {
            Assert.IsNull(_state.GetSection("nonexistent"));
        }

        [Test]
        public void HasSection_Works()
        {
            Assert.IsFalse(_state.HasSection("currencies"));
            _state.RegisterSection("currencies");
            Assert.IsTrue(_state.HasSection("currencies"));
        }

        [Test]
        public void SetValue_And_GetValue()
        {
            _state.RegisterSection("currencies");

            _state.SetValue("currencies", "gold", 500);
            var result = _state.GetValue("currencies", "gold");

            Assert.AreEqual(500, result);
        }

        [Test]
        public void SetValue_Emits_StateChanged_Event()
        {
            _state.RegisterSection("currencies");

            int eventCount = 0;
            _bus.Subscribe(EventNames.StateChanged, _ => eventCount++);

            _state.SetValue("currencies", "gold", 100);

            Assert.AreEqual(1, eventCount);
        }

        [Test]
        public void SetValue_Event_Contains_Old_And_New_Values()
        {
            _state.RegisterSection("currencies");
            _state.SetValue("currencies", "gold", 100);

            Dictionary<string, object> capturedPayload = null;
            _bus.Subscribe(EventNames.StateChanged, payload => capturedPayload = payload);

            _state.SetValue("currencies", "gold", 250);

            Assert.IsNotNull(capturedPayload);
            Assert.AreEqual("currencies", capturedPayload["section"]);
            Assert.AreEqual("gold", capturedPayload["key"]);
            Assert.AreEqual(100, capturedPayload["old_value"]);
            Assert.AreEqual(250, capturedPayload["new_value"]);
        }

        [Test]
        public void GetValue_Returns_Default_For_Missing_Section()
        {
            var result = _state.GetValue("nonexistent", "key", "fallback");
            Assert.AreEqual("fallback", result);
        }

        [Test]
        public void GetValue_Returns_Default_For_Missing_Key()
        {
            _state.RegisterSection("currencies");

            var result = _state.GetValue("currencies", "gems", 0);
            Assert.AreEqual(0, result);
        }

        [Test]
        public void ToSaveDict_Serializes_All_Sections()
        {
            _state.RegisterSection("currencies");
            _state.SetValue("currencies", "gold", 500);
            _state.SetValue("currencies", "gems", 30);

            _state.RegisterSection("settings");
            _state.SetValue("settings", "music_on", true);

            var saveDict = _state.ToSaveDict();

            Assert.IsTrue(saveDict.ContainsKey("currencies"));
            Assert.IsTrue(saveDict.ContainsKey("settings"));

            var currencies = (Dictionary<string, object>)saveDict["currencies"];
            Assert.AreEqual(500, currencies["gold"]);
            Assert.AreEqual(30, currencies["gems"]);

            var settings = (Dictionary<string, object>)saveDict["settings"];
            Assert.AreEqual(true, settings["music_on"]);
        }

        [Test]
        public void FromSaveDict_Restores_State()
        {
            _state.RegisterSection("currencies");

            var saveData = new Dictionary<string, Dictionary<string, object>>
            {
                {
                    "currencies", new Dictionary<string, object>
                    {
                        { "gold", 999 },
                        { "gems", 50 }
                    }
                }
            };

            _state.FromSaveDict(saveData);

            Assert.AreEqual(999, _state.GetValue("currencies", "gold"));
            Assert.AreEqual(50, _state.GetValue("currencies", "gems"));
        }

        [Test]
        public void FromSaveDict_Emits_StateLoaded()
        {
            _state.RegisterSection("currencies");

            int loadedCount = 0;
            _bus.Subscribe(EventNames.StateLoaded, _ => loadedCount++);

            var saveData = new Dictionary<string, Dictionary<string, object>>
            {
                {
                    "currencies", new Dictionary<string, object>
                    {
                        { "gold", 100 }
                    }
                }
            };

            _state.FromSaveDict(saveData);

            Assert.AreEqual(1, loadedCount);
        }

        [Test]
        public void FromSaveDict_Creates_Missing_Sections()
        {
            // Do NOT register "new_section" before loading
            Assert.IsFalse(_state.HasSection("new_section"));

            var saveData = new Dictionary<string, Dictionary<string, object>>
            {
                {
                    "new_section", new Dictionary<string, object>
                    {
                        { "key1", "value1" }
                    }
                }
            };

            _state.FromSaveDict(saveData);

            Assert.IsTrue(_state.HasSection("new_section"));
            Assert.AreEqual("value1", _state.GetValue("new_section", "key1"));
        }

        [Test]
        public void ClearAll_Removes_All_Sections()
        {
            _state.RegisterSection("currencies");
            _state.RegisterSection("settings");

            _state.ClearAll();

            Assert.IsFalse(_state.HasSection("currencies"));
            Assert.IsFalse(_state.HasSection("settings"));
        }

        [Test]
        public void Multiple_Sections_Independent()
        {
            _state.RegisterSection("currencies");
            _state.RegisterSection("settings");

            _state.SetValue("currencies", "gold", 100);
            _state.SetValue("settings", "volume", 0.8f);

            // Each section has its own values
            Assert.AreEqual(100, _state.GetValue("currencies", "gold"));
            Assert.AreEqual(0.8f, _state.GetValue("settings", "volume"));

            // Cross-section keys don't leak
            Assert.IsNull(_state.GetValue("currencies", "volume"));
            Assert.IsNull(_state.GetValue("settings", "gold"));
        }

        [Test]
        public void RegisterSection_With_InitialData()
        {
            var initialData = new Dictionary<string, object>
            {
                { "gold", 100 },
                { "gems", 10 }
            };

            _state.RegisterSection("currencies", initialData);

            Assert.AreEqual(100, _state.GetValue("currencies", "gold"));
            Assert.AreEqual(10, _state.GetValue("currencies", "gems"));
        }
    }
}
