using NUnit.Framework;
using System.Collections.Generic;
using MobileForge.Presentation;
using MobileForge.Domain;

namespace MobileForge.Tests.Presentation
{
    [TestFixture]
    public class CardViewTests
    {
        private CardView _cardView;
        private int _changeCount;

        [SetUp]
        public void SetUp()
        {
            _cardView = new CardView();
            _changeCount = 0;
            _cardView.OnChanged += () => _changeCount++;
        }

        private MonsterDef CreateTestDef(int id = 1, string name = "Test Monster", int element = 1, int rarity = 3)
        {
            return new MonsterDef(new Dictionary<string, object>
            {
                { "id", id },
                { "name", name },
                { "element", element },
                { "rarity", rarity },
                { "max_level", 99 },
                { "base_hp", 500f },
                { "base_atk", 200f },
                { "base_rec", 100f },
                { "max_hp", 3000f },
                { "max_atk", 1200f },
                { "max_rec", 600f },
                { "cost", 10 },
            });
        }

        private MonsterInstance CreateTestInstance(int instanceId = 1, int defId = 1)
        {
            return new MonsterInstance(instanceId, defId);
        }

        [Test]
        public void BindInstance_ShowsLevelAndStats()
        {
            var def = CreateTestDef();
            var instance = CreateTestInstance();
            instance.Level = 50;
            instance.PlusAtk = 99;
            instance.PlusHp = 99;
            instance.PlusRec = 99;

            _cardView.Bind(instance, def);

            Assert.AreEqual(50, _cardView.Level, "Should show instance level");
            Assert.AreEqual(99, _cardView.PlusAtk, "Should show plus ATK");
            Assert.AreEqual(99, _cardView.PlusHp, "Should show plus HP");
            Assert.AreEqual(99, _cardView.PlusRec, "Should show plus REC");
            Assert.IsTrue(_cardView.HasInstance, "Should have instance bound");
        }

        [Test]
        public void BindDef_ShowsBaseInfo()
        {
            var def = CreateTestDef(id: 5, name: "Fire Dragon", element: 2, rarity: 5);

            _cardView.BindDef(def);

            Assert.AreEqual("Fire Dragon", _cardView.Name, "Should show def name");
            Assert.AreEqual(2, _cardView.Element, "Should show element");
            Assert.AreEqual(5, _cardView.Rarity, "Should show rarity");
            Assert.AreEqual(0, _cardView.Level, "Should not have level when only def bound");
            Assert.IsFalse(_cardView.HasInstance, "Should not have instance");
        }

        [Test]
        public void BindNull_ClearsDisplay()
        {
            var def = CreateTestDef();
            var instance = CreateTestInstance();
            _cardView.Bind(instance, def);

            _cardView.Unbind();

            Assert.AreEqual("", _cardView.Name, "Name should be empty");
            Assert.AreEqual(0, _cardView.Level, "Level should be 0");
            Assert.AreEqual(0, _cardView.Element, "Element should be 0");
            Assert.IsFalse(_cardView.HasDef, "Should not have def");
            Assert.IsFalse(_cardView.HasInstance, "Should not have instance");
        }

        [Test]
        public void ElementColor_MapsCorrectly()
        {
            var def = CreateTestDef(element: 1);
            _cardView.BindDef(def);
            Assert.AreEqual(1, _cardView.Element, "Water element");

            def = CreateTestDef(element: 2);
            _cardView.BindDef(def);
            Assert.AreEqual(2, _cardView.Element, "Fire element");

            def = CreateTestDef(element: 3);
            _cardView.BindDef(def);
            Assert.AreEqual(3, _cardView.Element, "Grass element");

            def = CreateTestDef(element: 4);
            _cardView.BindDef(def);
            Assert.AreEqual(4, _cardView.Element, "Light element");

            def = CreateTestDef(element: 5);
            _cardView.BindDef(def);
            Assert.AreEqual(5, _cardView.Element, "Dark element");
        }

        [Test]
        public void OnChanged_FiresOnBind()
        {
            Assert.AreEqual(0, _changeCount);

            var def = CreateTestDef();
            _cardView.BindDef(def);

            Assert.AreEqual(1, _changeCount, "OnChanged should fire once on bind");
        }

        [Test]
        public void OnChanged_FiresOnUnbind()
        {
            var def = CreateTestDef();
            _cardView.BindDef(def);
            _changeCount = 0;

            _cardView.Unbind();

            Assert.AreEqual(1, _changeCount, "OnChanged should fire on unbind");
        }

        [Test]
        public void Refresh_FiresOnChangedWithoutDataChange()
        {
            var def = CreateTestDef();
            _cardView.BindDef(def);
            _changeCount = 0;

            _cardView.Refresh();

            Assert.AreEqual(1, _changeCount, "Refresh should fire OnChanged");
            Assert.AreEqual("Test Monster", _cardView.Name, "Data should remain the same");
        }

        [Test]
        public void IsFavorite_ReflectsInstance()
        {
            var def = CreateTestDef();
            var instance = CreateTestInstance();
            instance.IsFavorite = true;

            _cardView.Bind(instance, def);

            Assert.IsTrue(_cardView.IsFavorite, "Should show favorite status");
        }

        [Test]
        public void InstanceId_ReturnsCorrectValue()
        {
            var def = CreateTestDef();
            var instance = CreateTestInstance(instanceId: 42);

            _cardView.Bind(instance, def);

            Assert.AreEqual(42, _cardView.InstanceId, "Should show instance ID");
        }

        [Test]
        public void InstanceId_ReturnsNegativeWithoutInstance()
        {
            var def = CreateTestDef();
            _cardView.BindDef(def);

            Assert.AreEqual(-1, _cardView.InstanceId, "Should return -1 without instance");
        }
    }
}
