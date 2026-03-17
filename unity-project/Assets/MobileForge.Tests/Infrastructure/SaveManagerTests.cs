using System.Collections.Generic;
using NUnit.Framework;
using MobileForge.Infrastructure;

namespace MobileForge.Tests.Infrastructure
{
    [TestFixture]
    public class SaveManagerTests
    {
        private SaveMigrator _migrator;
        private SaveManager _manager;

        [SetUp]
        public void SetUp()
        {
            _migrator = new SaveMigrator();
            _manager = new SaveManager();
        }

        // -----------------------------------------------------------------
        // Tests: SaveFormat
        // -----------------------------------------------------------------

        [Test]
        public void CreateEnvelope_Has_Required_Fields()
        {
            var data = new Dictionary<string, object>
            {
                { "progress", new Dictionary<string, object> { { "level", 5 } } },
                { "inventory", new Dictionary<string, object> { { "gold", 100 } } }
            };

            var envelope = SaveFormat.CreateEnvelope(1, data);

            Assert.IsTrue(envelope.ContainsKey("version"), "Envelope should have version field");
            Assert.IsTrue(envelope.ContainsKey("timestamp"), "Envelope should have timestamp field");
            Assert.IsTrue(envelope.ContainsKey("checksum"), "Envelope should have checksum field");
            Assert.IsTrue(envelope.ContainsKey("data"), "Envelope should have data field");
            Assert.AreEqual(1, envelope["version"], "Version should be 1");
            Assert.IsTrue((long)envelope["timestamp"] > 0, "Timestamp should be positive");
            Assert.IsInstanceOf<string>(envelope["checksum"], "Checksum should be a string");

            var envelopeData = (Dictionary<string, object>)envelope["data"];
            var progress = (Dictionary<string, object>)envelopeData["progress"];
            Assert.AreEqual(5, progress["level"], "Data should be preserved in envelope");
        }

        [Test]
        public void Validate_Returns_True_For_Untampered_Envelope()
        {
            var data = new Dictionary<string, object>
            {
                { "settings", new Dictionary<string, object> { { "volume", 0.8 } } }
            };

            var envelope = SaveFormat.CreateEnvelope(1, data);
            bool isValid = SaveFormat.ValidateEnvelope(envelope);

            Assert.IsTrue(isValid, "Freshly created envelope should validate successfully");
        }

        [Test]
        public void Validate_Returns_False_For_Tampered_Envelope()
        {
            var data = new Dictionary<string, object>
            {
                { "settings", new Dictionary<string, object> { { "volume", 0.8 } } }
            };

            var envelope = SaveFormat.CreateEnvelope(1, data);

            // Tamper with the data after envelope creation
            var envelopeData = (Dictionary<string, object>)envelope["data"];
            var settings = (Dictionary<string, object>)envelopeData["settings"];
            settings["volume"] = 0.0;

            bool isValid = SaveFormat.ValidateEnvelope(envelope);

            Assert.IsFalse(isValid, "Tampered envelope should fail validation");
        }

        // -----------------------------------------------------------------
        // Tests: SaveMigrator
        // -----------------------------------------------------------------

        [Test]
        public void Migrate_Chains_Multiple_Steps()
        {
            // Register v1->v2: rename "coins" to "gold"
            _migrator.Register(1, 2, data =>
            {
                if (data.ContainsKey("coins"))
                {
                    data["gold"] = data["coins"];
                    data.Remove("coins");
                }
                return data;
            });

            // Register v2->v3: add "gems" field defaulting to 0
            _migrator.Register(2, 3, data =>
            {
                if (!data.ContainsKey("gems"))
                    data["gems"] = 0;
                return data;
            });

            var oldData = new Dictionary<string, object> { { "coins", 500 } };
            var migrated = _migrator.Migrate(oldData, 1, 3);

            Assert.IsFalse(migrated.ContainsKey("coins"), "coins should be removed after v1->v2 migration");
            Assert.IsTrue(migrated.ContainsKey("gold"), "gold should exist after v1->v2 migration");
            Assert.AreEqual(500, migrated["gold"], "gold value should be preserved from coins");
            Assert.IsTrue(migrated.ContainsKey("gems"), "gems should exist after v2->v3 migration");
            Assert.AreEqual(0, migrated["gems"], "gems should default to 0");
        }

        // -----------------------------------------------------------------
        // Tests: ISaveable round-trip
        // -----------------------------------------------------------------

        [Test]
        public void Saveable_RoundTrip_Preserves_Data()
        {
            var saveable = new MockSaveable { Name = "TestHero", Level = 10 };

            // Save
            var saved = saveable.SaveToDict();
            Assert.IsTrue(saved.ContainsKey("name"));
            Assert.IsTrue(saved.ContainsKey("level"));
            Assert.AreEqual("TestHero", saved["name"]);
            Assert.AreEqual(10, saved["level"]);

            // Restore into a new instance
            var restored = new MockSaveable();
            restored.LoadFromDict(saved);
            Assert.AreEqual("TestHero", restored.Name, "Name should be restored");
            Assert.AreEqual(10, restored.Level, "Level should be restored");
        }

        // -----------------------------------------------------------------
        // Tests: SaveManager
        // -----------------------------------------------------------------

        [Test]
        public void HasSave_Returns_False_Initially()
        {
            Assert.IsFalse(_manager.HasSave("slot_0"), "Initially there should be no save");
        }

        // -----------------------------------------------------------------
        // Mock saveable
        // -----------------------------------------------------------------

        private class MockSaveable
        {
            public string Name { get; set; } = "";
            public int Level { get; set; } = 0;

            public Dictionary<string, object> SaveToDict()
            {
                return new Dictionary<string, object>
                {
                    { "name", Name },
                    { "level", Level }
                };
            }

            public void LoadFromDict(Dictionary<string, object> data)
            {
                if (data.ContainsKey("name")) Name = (string)data["name"];
                if (data.ContainsKey("level")) Level = (int)data["level"];
            }
        }
    }
}
