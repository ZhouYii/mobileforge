using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MobileForge.Domain;

namespace MobileForge.Tests.Domain
{
    /// <summary>
    /// Tests for LootTable -- guaranteed drops, non-guaranteed rolling,
    /// count ranges, and empty tables.
    /// </summary>
    [TestFixture]
    public class LootTableTests
    {
        private Random _rng;

        [SetUp]
        public void SetUp()
        {
            _rng = new Random(54321);
        }

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

        private static LootTableDef MakeTableWithGuaranteed()
        {
            var entries = new List<LootEntry>
            {
                new LootEntry("currency", 1, 10, 10, 0, true),
                new LootEntry("item", 2, 1, 1, 50, false),
                new LootEntry("item", 3, 1, 1, 50, false),
            };
            return new LootTableDef(1, entries);
        }

        private static LootTableDef MakeNonGuaranteedOnlyTable()
        {
            var entries = new List<LootEntry>
            {
                new LootEntry("item", 10, 1, 1, 50, false),
                new LootEntry("item", 11, 1, 1, 50, false),
            };
            return new LootTableDef(2, entries);
        }

        private static LootTableDef MakeCountRangeTable()
        {
            var entries = new List<LootEntry>
            {
                new LootEntry("currency", 1, 1, 5, 0, true),
            };
            return new LootTableDef(3, entries);
        }

        // -----------------------------------------------------------------
        // Tests
        // -----------------------------------------------------------------

        [Test]
        public void Guaranteed_Drops_Always_Present()
        {
            var table = MakeTableWithGuaranteed();

            for (int i = 0; i < 20; i++)
            {
                // With rollCount=0, only guaranteed drops should appear
                var drops = LootTable.RollDrops(table, _rng, 0);
                bool foundGuaranteed = drops.Any(d => d.ItemId == 1 && d.Type == "currency");
                Assert.IsTrue(foundGuaranteed,
                    "Guaranteed drop (ItemId=1) should always be present");
            }
        }

        [Test]
        public void Non_Guaranteed_May_Not_Drop()
        {
            var table = MakeNonGuaranteedOnlyTable();

            // With rollCount=0, no non-guaranteed drops should appear
            var drops = LootTable.RollDrops(table, _rng, 0);
            Assert.AreEqual(0, drops.Count,
                "Non-guaranteed table with rollCount=0 should return empty");
        }

        [Test]
        public void Roll_Count_Affects_Drops()
        {
            var table = MakeTableWithGuaranteed();

            // rollCount=3 should give guaranteed drops + up to 3 non-guaranteed drops
            var drops = LootTable.RollDrops(table, _rng, 3);

            // Should have at least 1 guaranteed drop
            Assert.GreaterOrEqual(drops.Count, 1,
                "Should have at least the guaranteed drop");
            // Should have at most 1 guaranteed + 3 non-guaranteed = 4
            Assert.LessOrEqual(drops.Count, 4,
                $"Should have at most 4 drops (1 guaranteed + 3 non-guaranteed), got {drops.Count}");
            // Verify the guaranteed one is present
            bool hasGuaranteed = drops.Any(d => d.ItemId == 1);
            Assert.IsTrue(hasGuaranteed, "Guaranteed drop should be present with rollCount=3");
        }

        [Test]
        public void Count_Range()
        {
            var table = MakeCountRangeTable();

            for (int i = 0; i < 50; i++)
            {
                var drops = LootTable.RollDrops(table, _rng, 0);
                Assert.AreEqual(1, drops.Count, "Should have exactly 1 guaranteed drop");
                var drop = drops[0];
                Assert.GreaterOrEqual(drop.Count, 1, "Drop count should be >= 1");
                Assert.LessOrEqual(drop.Count, 5,
                    $"Drop count should be <= 5, got {drop.Count}");
            }
        }

        [Test]
        public void Empty_Table_Returns_Empty()
        {
            var emptyTable = new LootTableDef(99, new List<LootEntry>());
            var drops = LootTable.RollDrops(emptyTable, _rng, 5);

            Assert.AreEqual(0, drops.Count, "Empty table should return no drops");
        }
    }
}
