using System;
using System.Collections.Generic;
using NUnit.Framework;
using MobileForge.Domain;

namespace MobileForge.Tests.Domain
{
    [TestFixture]
    public class InventoryTests
    {
        private Inventory _inventory;

        [SetUp]
        public void SetUp()
        {
            _inventory = new Inventory();
        }

        [Test]
        public void AddItem_IncreasesCount()
        {
            int added = _inventory.AddItem("sword", 5);
            Assert.AreEqual(5, added);
            Assert.AreEqual(5, _inventory.GetCount("sword"));
        }

        [Test]
        public void AddItem_ReturnsZeroForNegative()
        {
            int added = _inventory.AddItem("sword", -1);
            Assert.AreEqual(0, added);
            Assert.AreEqual(0, _inventory.GetCount("sword"));
        }

        [Test]
        public void AddItem_ReturnsZeroForZero()
        {
            int added = _inventory.AddItem("sword", 0);
            Assert.AreEqual(0, added);
        }

        [Test]
        public void AddItem_RespectsMaxStack()
        {
            _inventory.RegisterItemDefs(new[] { new ItemDef { Id = "sword", MaxStack = 10 } });
            int added = _inventory.AddItem("sword", 15);
            Assert.AreEqual(10, added);
            Assert.AreEqual(10, _inventory.GetCount("sword"));
        }

        [Test]
        public void AddItem_RespectsMaxSlots()
        {
            _inventory.SetMaxSlots(2);
            _inventory.AddItem("sword", 1);
            _inventory.AddItem("shield", 1);
            int added = _inventory.AddItem("potion", 1);
            Assert.AreEqual(0, added);
            Assert.AreEqual(0, _inventory.GetCount("potion"));
        }

        [Test]
        public void AddItem_FiresEvent()
        {
            string eventItemId = null;
            int eventOld = -1, eventNew = -1;
            _inventory.OnInventoryChanged += (id, old, neu) => { eventItemId = id; eventOld = old; eventNew = neu; };

            _inventory.AddItem("sword", 5);
            Assert.AreEqual("sword", eventItemId);
            Assert.AreEqual(0, eventOld);
            Assert.AreEqual(5, eventNew);
        }

        [Test]
        public void RemoveItem_DecreasesCount()
        {
            _inventory.AddItem("sword", 10);
            int removed = _inventory.RemoveItem("sword", 3);
            Assert.AreEqual(3, removed);
            Assert.AreEqual(7, _inventory.GetCount("sword"));
        }

        [Test]
        public void RemoveItem_RemovesStackWhenEmpty()
        {
            _inventory.AddItem("sword", 5);
            _inventory.RemoveItem("sword", 5);
            Assert.IsFalse(_inventory.HasItem("sword"));
            Assert.AreEqual(0, _inventory.SlotCount);
        }

        [Test]
        public void RemoveItem_ReturnsZeroForMissing()
        {
            int removed = _inventory.RemoveItem("nonexistent", 5);
            Assert.AreEqual(0, removed);
        }

        [Test]
        public void HasItem_TrueWhenEnough()
        {
            _inventory.AddItem("sword", 10);
            Assert.IsTrue(_inventory.HasItem("sword", 5));
        }

        [Test]
        public void HasItem_FalseWhenNotEnough()
        {
            _inventory.AddItem("sword", 3);
            Assert.IsFalse(_inventory.HasItem("sword", 5));
        }

        [Test]
        public void GetByType_FiltersCorrectly()
        {
            _inventory.RegisterItemDefs(new[]
            {
                new ItemDef { Id = "sword", Type = "weapon" },
                new ItemDef { Id = "shield", Type = "armor" },
                new ItemDef { Id = "axe", Type = "weapon" },
            });
            _inventory.AddItem("sword", 1);
            _inventory.AddItem("shield", 1);
            _inventory.AddItem("axe", 1);

            var weapons = _inventory.GetByType("weapon");
            Assert.AreEqual(2, weapons.Count);
            Assert.Contains("sword", weapons);
            Assert.Contains("axe", weapons);
        }

        [Test]
        public void GetByTag_FiltersCorrectly()
        {
            _inventory.RegisterItemDefs(new[]
            {
                new ItemDef { Id = "fire_sword", Tags = new List<string> { "fire", "legendary" } },
                new ItemDef { Id = "ice_wand", Tags = new List<string> { "ice", "legendary" } },
                new ItemDef { Id = "wood_club", Tags = new List<string> { "common" } },
            });
            _inventory.AddItem("fire_sword", 1);
            _inventory.AddItem("ice_wand", 1);
            _inventory.AddItem("wood_club", 1);

            var legendary = _inventory.GetByTag("legendary");
            Assert.AreEqual(2, legendary.Count);
        }

        [Test]
        public void Equip_ReturnsPrevious()
        {
            _inventory.AddItem("sword1", 1);
            _inventory.AddItem("sword2", 1);

            var prev = _inventory.Equip("weapon", "sword1");
            Assert.IsNull(prev);

            prev = _inventory.Equip("weapon", "sword2");
            Assert.AreEqual("sword1", prev);
            Assert.AreEqual("sword2", _inventory.GetEquipped("weapon"));
        }

        [Test]
        public void Equip_ReturnsNullForMissing()
        {
            var prev = _inventory.Equip("weapon", "nonexistent");
            Assert.IsNull(prev);
            Assert.IsNull(_inventory.GetEquipped("weapon"));
        }

        [Test]
        public void Unequip_RemovesAndReturns()
        {
            _inventory.AddItem("sword", 1);
            _inventory.Equip("weapon", "sword");

            var unequipped = _inventory.Unequip("weapon");
            Assert.AreEqual("sword", unequipped);
            Assert.IsNull(_inventory.GetEquipped("weapon"));
        }

        [Test]
        public void Unequip_FiresEvent()
        {
            string unequippedSlot = null, unequippedItem = null;
            _inventory.OnItemUnequipped += (slot, item) => { unequippedSlot = slot; unequippedItem = item; };
            _inventory.AddItem("sword", 1);
            _inventory.Equip("weapon", "sword");
            _inventory.Unequip("weapon");

            Assert.AreEqual("weapon", unequippedSlot);
            Assert.AreEqual("sword", unequippedItem);
        }

        [Test]
        public void IsFull_TrueWhenAtCapacity()
        {
            _inventory.SetMaxSlots(1);
            _inventory.AddItem("sword", 1);
            Assert.IsTrue(_inventory.IsFull);
        }

        [Test]
        public void IsFull_FalseWhenUnlimited()
        {
            _inventory.AddItem("sword", 1);
            Assert.IsFalse(_inventory.IsFull);
        }

        [Test]
        public void ToSaveDict_RoundTrip()
        {
            _inventory.AddItem("sword", 10);
            _inventory.AddItem("potion", 5);
            _inventory.Equip("weapon", "sword");

            var saved = _inventory.ToSaveDict();
            var newInventory = new Inventory();
            newInventory.FromSaveDict(saved);

            Assert.AreEqual(10, newInventory.GetCount("sword"));
            Assert.AreEqual(5, newInventory.GetCount("potion"));
            Assert.AreEqual("sword", newInventory.GetEquipped("weapon"));
        }

        [Test]
        public void FromSaveDict_ClearsExisting()
        {
            _inventory.AddItem("old_item", 10);
            _inventory.FromSaveDict(new Dictionary<string, object>
            {
                ["stacks"] = new Dictionary<string, object>(),
                ["equipped"] = new Dictionary<string, object>(),
            });
            Assert.AreEqual(0, _inventory.SlotCount);
        }
    }
}
