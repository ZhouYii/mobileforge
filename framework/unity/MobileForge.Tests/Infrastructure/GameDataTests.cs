using System.Collections.Generic;
using NUnit.Framework;
using MobileForge.Infrastructure;

namespace MobileForge.Tests.Infrastructure
{
    [TestFixture]
    public class GameDataTests
    {
        private GameData _gameData;

        [SetUp]
        public void SetUp()
        {
            _gameData = new GameData();
        }

        // -----------------------------------------------------------------
        // Helper: build a list of definition dictionaries (simulates parsed JSON)
        // -----------------------------------------------------------------
        private List<Dictionary<string, object>> MakeSampleDefinitions()
        {
            return new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "id", 1 },
                    { "name", "Fireball" },
                    { "damage", 120 },
                    { "cooldown", 2.5f }
                },
                new Dictionary<string, object>
                {
                    { "id", 2 },
                    { "name", "Heal" },
                    { "damage", 0 },
                    { "cooldown", 5.0f }
                },
                new Dictionary<string, object>
                {
                    { "id", 3 },
                    { "name", "Shield" },
                    { "damage", 0 },
                    { "cooldown", 10.0f }
                }
            };
        }

        [Test]
        public void LoadDefinitions_Stores_By_Id()
        {
            var defs = MakeSampleDefinitions();
            _gameData.LoadDefinitions("skills", defs);

            Assert.AreEqual(3, _gameData.GetDefinitionCount("skills"));
        }

        [Test]
        public void GetDefinition_Returns_Correct_Definition()
        {
            var defs = MakeSampleDefinitions();
            _gameData.LoadDefinitions("skills", defs);

            var def = _gameData.GetDefinition("skills", 1);
            Assert.IsNotNull(def);
            Assert.AreEqual(1, def.Id);
            Assert.AreEqual("Fireball", def.GetString("name"));
        }

        [Test]
        public void GetDefinition_Returns_Null_For_Missing()
        {
            var defs = MakeSampleDefinitions();
            _gameData.LoadDefinitions("skills", defs);

            // Missing id within existing type
            Assert.IsNull(_gameData.GetDefinition("skills", 999));
            // Missing type entirely
            Assert.IsNull(_gameData.GetDefinition("items", 1));
        }

        [Test]
        public void GetAllDefinitions_Returns_All()
        {
            var defs = MakeSampleDefinitions();
            _gameData.LoadDefinitions("skills", defs);

            var all = _gameData.GetAllDefinitions("skills");
            Assert.AreEqual(3, all.Count);

            // Verify all ids are present
            var ids = new HashSet<int>();
            foreach (var d in all)
                ids.Add(d.Id);

            Assert.IsTrue(ids.Contains(1));
            Assert.IsTrue(ids.Contains(2));
            Assert.IsTrue(ids.Contains(3));
        }

        [Test]
        public void HasDefinition_Returns_True_When_Exists()
        {
            var defs = MakeSampleDefinitions();
            _gameData.LoadDefinitions("skills", defs);

            Assert.IsTrue(_gameData.HasDefinition("skills", 1));
            Assert.IsTrue(_gameData.HasDefinition("skills", 2));
            Assert.IsTrue(_gameData.HasDefinition("skills", 3));
        }

        [Test]
        public void HasDefinition_Returns_False_When_Missing()
        {
            var defs = MakeSampleDefinitions();
            _gameData.LoadDefinitions("skills", defs);

            Assert.IsFalse(_gameData.HasDefinition("skills", 999));
            Assert.IsFalse(_gameData.HasDefinition("items", 1));
        }

        [Test]
        public void GetDefinitionCount_Returns_Correct_Count()
        {
            Assert.AreEqual(0, _gameData.GetDefinitionCount("skills"));

            var defs = MakeSampleDefinitions();
            _gameData.LoadDefinitions("skills", defs);

            Assert.AreEqual(3, _gameData.GetDefinitionCount("skills"));
            Assert.AreEqual(0, _gameData.GetDefinitionCount("items"));
        }

        [Test]
        public void ClearType_Removes_Only_That_Type()
        {
            _gameData.LoadDefinitions("skills", MakeSampleDefinitions());
            _gameData.LoadDefinitions("items", new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { { "id", 100 }, { "name", "Potion" } }
            });

            Assert.AreEqual(3, _gameData.GetDefinitionCount("skills"));
            Assert.AreEqual(1, _gameData.GetDefinitionCount("items"));

            _gameData.ClearType("skills");

            Assert.AreEqual(0, _gameData.GetDefinitionCount("skills"));
            Assert.AreEqual(1, _gameData.GetDefinitionCount("items"));
        }

        [Test]
        public void ClearAll_Removes_Everything()
        {
            _gameData.LoadDefinitions("skills", MakeSampleDefinitions());
            _gameData.LoadDefinitions("items", new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { { "id", 100 }, { "name", "Potion" } }
            });

            _gameData.ClearAll();

            Assert.AreEqual(0, _gameData.GetDefinitionCount("skills"));
            Assert.AreEqual(0, _gameData.GetDefinitionCount("items"));
        }

        [Test]
        public void Definition_GetInt_Returns_Correct_Value()
        {
            _gameData.LoadDefinitions("skills", MakeSampleDefinitions());

            var def = _gameData.GetDefinition("skills", 1);
            Assert.AreEqual(120, def.GetInt("damage"));
            // Default for missing key
            Assert.AreEqual(-1, def.GetInt("missing_key", -1));
        }

        [Test]
        public void Definition_GetFloat_Returns_Correct_Value()
        {
            _gameData.LoadDefinitions("skills", MakeSampleDefinitions());

            var def = _gameData.GetDefinition("skills", 1);
            Assert.AreEqual(2.5f, def.GetFloat("cooldown"), 0.001f);
            // Default for missing key
            Assert.AreEqual(99.9f, def.GetFloat("missing_key", 99.9f), 0.001f);
        }

        [Test]
        public void Definition_GetString_Returns_Correct_Value()
        {
            _gameData.LoadDefinitions("skills", MakeSampleDefinitions());

            var def = _gameData.GetDefinition("skills", 2);
            Assert.AreEqual("Heal", def.GetString("name"));
            // Default for missing key
            Assert.AreEqual("N/A", def.GetString("missing_key", "N/A"));
        }

        [Test]
        public void Definition_HasField_Works()
        {
            _gameData.LoadDefinitions("skills", MakeSampleDefinitions());

            var def = _gameData.GetDefinition("skills", 1);
            Assert.IsTrue(def.HasField("name"));
            Assert.IsTrue(def.HasField("damage"));
            Assert.IsTrue(def.HasField("cooldown"));
            Assert.IsFalse(def.HasField("nonexistent"));
        }
    }
}
