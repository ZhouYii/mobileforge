using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MobileForge.Domain;

namespace MobileForge.Tests.Domain
{
    /// <summary>
    /// Tests for GachaRoller and PityTracker -- weighted rolling, pity system,
    /// displayed rates, and pity tracker state management.
    /// </summary>
    [TestFixture]
    public class GachaRollerTests
    {
        private GachaPool _pool;
        private Random _rng;

        // -----------------------------------------------------------------
        // Test data: 3 entries -- rarity 5 (weight 5), rarity 4 (weight 20), rarity 3 (weight 75)
        // Pity threshold = 10
        // -----------------------------------------------------------------

        private static GachaPool MakeTestPool()
        {
            var entries = new List<GachaEntry>
            {
                new GachaEntry(1001, 5, 5, true),
                new GachaEntry(2001, 4, 20, false),
                new GachaEntry(3001, 3, 75, false),
            };
            return new GachaPool(1, "Test Banner", "gems", 5, entries, 10, new[] { 1001 });
        }

        [SetUp]
        public void SetUp()
        {
            _pool = MakeTestPool();
            _rng = new Random(12345);
        }

        // -----------------------------------------------------------------
        // Tests
        // -----------------------------------------------------------------

        [Test]
        public void Roll_Returns_Valid_Result()
        {
            var result = GachaRoller.Roll(_pool, 0, _rng);

            Assert.IsNotNull(result, "Roll should return a result");
            Assert.Greater(result.MonsterId, 0, "Result should have a valid MonsterId");
            Assert.IsTrue(result.Rarity >= 3 && result.Rarity <= 5,
                "Result rarity should be between 3 and 5");
        }

        [Test]
        public void Roll_Multi_Returns_Correct_Count()
        {
            var results = GachaRoller.RollMulti(_pool, 10, 0, _rng);

            Assert.AreEqual(10, results.Count, "RollMulti(10) should return 10 results");
            foreach (var r in results)
                Assert.Greater(r.MonsterId, 0, "Each result should have a valid MonsterId");
        }

        [Test]
        public void Pity_Triggers_Top_Rarity()
        {
            // Set pityCount = threshold - 1 = 9 so the next roll triggers pity
            var result = GachaRoller.Roll(_pool, 9, _rng);

            Assert.IsTrue(result.IsPity, "Roll at pity threshold should set IsPity = true");
            Assert.AreEqual(5, result.Rarity, "Pity roll should return top rarity (5)");
            Assert.AreEqual(1001, result.MonsterId, "Pity roll should return a top-rarity monster");
        }

        [Test]
        public void Pity_Resets_After_Top_Rarity()
        {
            // Start at pityCount = 8. After the first pull (pityCount becomes 9),
            // pity triggers. After that, pity should reset to 0.
            var results = GachaRoller.RollMulti(_pool, 12, 8, _rng);

            Assert.AreEqual(12, results.Count, "Should have 12 results");
            // The second result (pityCount=9) should be pity or top rarity
            Assert.IsTrue(results[1].IsPity || results[1].Rarity == 5,
                "Second result should be pity or top rarity");
            // After pity resets, subsequent pulls should not all be pity
            int nonPityCount = results.Skip(2).Count(r => !r.IsPity);
            Assert.Greater(nonPityCount, 0,
                "After pity reset, not all subsequent pulls should be pity");
        }

        [Test]
        public void Weighted_Distribution()
        {
            // Roll 1000 times, verify high-weight entries appear more often
            var rarityCounts = new Dictionary<int, int> { { 3, 0 }, { 4, 0 }, { 5, 0 } };
            for (int i = 0; i < 1000; i++)
            {
                var result = GachaRoller.Roll(_pool, 0, _rng);
                rarityCounts[result.Rarity]++;
            }

            // Rarity 3 has weight 75 (75%), rarity 4 has 20 (20%), rarity 5 has 5 (5%)
            Assert.Greater(rarityCounts[3], rarityCounts[4],
                "Rarity 3 (weight 75) should appear more than rarity 4 (weight 20)");
            Assert.Greater(rarityCounts[4], rarityCounts[5],
                "Rarity 4 (weight 20) should appear more than rarity 5 (weight 5)");
            // Sanity check: rarity 3 should be at least 50% of total
            Assert.Greater(rarityCounts[3], 500,
                "Rarity 3 should account for more than 50% of 1000 rolls");
        }

        [Test]
        public void Get_Displayed_Rates()
        {
            var rates = GachaRoller.GetDisplayedRates(_pool);

            // Total weight = 5 + 20 + 75 = 100
            double totalRate = rates.Values.Sum();
            Assert.AreEqual(100.0, totalRate, 0.01,
                "Displayed rates should sum to ~100%");
            Assert.AreEqual(5.0, rates[5], 0.01, "Rarity 5 rate should be ~5%");
            Assert.AreEqual(20.0, rates[4], 0.01, "Rarity 4 rate should be ~20%");
            Assert.AreEqual(75.0, rates[3], 0.01, "Rarity 3 rate should be ~75%");
        }

        [Test]
        public void Pity_Tracker_Increment_And_Reset()
        {
            var tracker = new PityTracker();

            Assert.AreEqual(0, tracker.GetPity(1), "Initial pity should be 0");
            tracker.Increment(1);
            tracker.Increment(1);
            tracker.Increment(1);
            Assert.AreEqual(3, tracker.GetPity(1), "Pity should be 3 after 3 increments");
            tracker.Reset(1);
            Assert.AreEqual(0, tracker.GetPity(1), "Pity should be 0 after reset");
        }

        [Test]
        public void Pity_Tracker_Serialization()
        {
            var tracker = new PityTracker();
            tracker.Increment(1);
            tracker.Increment(1);
            tracker.Increment(2);
            tracker.Increment(2);
            tracker.Increment(2);

            // Serialize
            var data = tracker.ToDict();
            Assert.AreEqual(2, data[1], "Pool 1 should have pity 2 in serialized data");
            Assert.AreEqual(3, data[2], "Pool 2 should have pity 3 in serialized data");

            // Deserialize into a new tracker
            var tracker2 = new PityTracker();
            tracker2.FromDict(data);
            Assert.AreEqual(2, tracker2.GetPity(1), "Restored pity for pool 1 should be 2");
            Assert.AreEqual(3, tracker2.GetPity(2), "Restored pity for pool 2 should be 3");
            Assert.AreEqual(0, tracker2.GetPity(99), "Unset pool should still return 0");
        }
    }
}
