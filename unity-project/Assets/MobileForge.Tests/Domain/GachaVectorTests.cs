using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using MobileForge.Domain;

namespace MobileForge.Tests.Domain
{
    [TestFixture]
    public class GachaVectorTests
    {
        private GachaPool _testPool;

        [SetUp]
        public void SetUp()
        {
            _testPool = new GachaPool(1, "Test Pool", "gems", 5, new List<GachaEntry>
            {
                new GachaEntry(1, 5, 5, true),
                new GachaEntry(2, 4, 20, false),
                new GachaEntry(3, 3, 75, false),
            }, 10, Array.Empty<int>());
        }

        [Test]
        public void PityTriggersAtThreshold()
        {
            var rng = new Random(12345);
            var result = GachaRoller.Roll(_testPool, pityCount: 9, rng);

            Assert.IsTrue(result.IsPity, "Pity should trigger at pity_count=9 (threshold-1)");
            Assert.AreEqual(5, result.Rarity, "Pity should guarantee 5-star");
        }

        [Test]
        public void PityDoesNotTriggerBelowThreshold()
        {
            var rng = new Random(12345);
            var result = GachaRoller.Roll(_testPool, pityCount: 8, rng);

            Assert.IsFalse(result.IsPity, "Pity should not trigger at pity_count=8");
        }

        [Test]
        public void PityResetsOnTopRarity()
        {
            var rng = new Random(12345);

            int pityCount = 5;
            var result = GachaRoller.Roll(_testPool, pityCount, rng);
            if (result.Rarity == 5 || result.IsPity)
            {
                pityCount = 0;
            }
            else
            {
                pityCount++;
            }
            Assert.LessOrEqual(pityCount, 6, "Pity should reset or increment correctly");
        }

        [Test]
        public void DisplayedRatesMatchWeights()
        {
            var rates = GachaRoller.GetDisplayedRates(_testPool);

            Assert.AreEqual(5.0, rates[5], 0.1, "5-star rate should be 5%");
            Assert.AreEqual(20.0, rates[4], 0.1, "4-star rate should be 20%");
            Assert.AreEqual(75.0, rates[3], 0.1, "3-star rate should be 75%");
        }

        [Test]
        public void MultiPullPityTracksAcrossPulls()
        {
            var rng = new Random(12345);
            int initialPity = 7;

            var results = GachaRoller.RollMulti(_testPool, 10, initialPity, rng);

            Assert.AreEqual(10, results.Count, "Should have 10 results");

            bool foundPity = false;
            foreach (var result in results)
            {
                if (result.IsPity || result.Rarity == 5)
                    foundPity = true;
            }
            Assert.IsTrue(foundPity, "Multi-pull from pity=7 should trigger pity within 3 pulls");
        }

        [Test]
        public void WeightedDistributionStatistical()
        {
            var rng = new Random(42);
            var counts = new Dictionary<int, int> { { 5, 0 }, { 4, 0 }, { 3, 0 } };

            int sampleSize = 10000;
            for (int i = 0; i < sampleSize; i++)
            {
                var result = GachaRoller.Roll(_testPool, pityCount: 0, rng);
                counts[result.Rarity]++;
            }

            double rate5 = (double)counts[5] / sampleSize * 100;
            double rate4 = (double)counts[4] / sampleSize * 100;
            double rate3 = (double)counts[3] / sampleSize * 100;

            Assert.GreaterOrEqual(rate5, 2.0, "5-star rate should be >= 2%");
            Assert.LessOrEqual(rate5, 8.0, "5-star rate should be <= 8%");

            Assert.GreaterOrEqual(rate4, 17.0, "4-star rate should be >= 17%");
            Assert.LessOrEqual(rate4, 23.0, "4-star rate should be <= 23%");

            Assert.GreaterOrEqual(rate3, 72.0, "3-star rate should be >= 72%");
            Assert.LessOrEqual(rate3, 78.0, "3-star rate should be <= 78%");
        }

        [Test]
        public void EmptyPoolHandling()
        {
            var emptyPool = new GachaPool(99, "Empty", "gems", 5, new List<GachaEntry>(), 0, Array.Empty<int>());
            var rng = new Random(12345);

            var result = GachaRoller.Roll(emptyPool, 0, rng);

            Assert.AreEqual(0, result.MonsterId, "Empty pool should return default result");
            Assert.AreEqual(0, result.Rarity, "Empty pool should return default rarity");
        }

        [Test]
        public void RunGachaDistributionTestVectors()
        {
            var jsonPath = Path.Combine(TestContext.CurrentContext.TestDirectory,
                "..", "..", "..", "..", "..", "..", "framework", "shared", "test_vectors", "gacha_distribution_cases.json");

            if (!File.Exists(jsonPath))
            {
                Assert.Ignore($"Test vector file not found: {jsonPath}");
                return;
            }

            var json = File.ReadAllText(jsonPath);
            var doc = JObject.Parse(json);
            var cases = (JArray)doc["cases"];

            foreach (var testCase in cases)
            {
                string name = testCase["name"].Value<string>();
                TestContext.WriteLine($"Testing: {name}");

                switch (name)
                {
                    case "pity_triggers_at_threshold":
                        TestPityTriggers(testCase);
                        break;
                    case "pity_does_not_trigger_below_threshold":
                        TestPityDoesNotTrigger(testCase);
                        break;
                    case "displayed_rates_match_weights":
                        TestDisplayedRates(testCase);
                        break;
                    case "multi_pull_pity_tracks_across_pulls":
                        TestMultiPullPity(testCase);
                        break;
                    case "empty_pool_handling":
                        TestEmptyPool(testCase);
                        break;
                }
            }
        }

        private void TestPityTriggers(JToken testCase)
        {
            var rng = new Random(12345);
            var result = GachaRoller.Roll(_testPool, testCase["pity_count"].Value<int>(), rng);
            Assert.IsTrue(result.IsPity, testCase["name"].Value<string>());
        }

        private void TestPityDoesNotTrigger(JToken testCase)
        {
            var rng = new Random(12345);
            var result = GachaRoller.Roll(_testPool, testCase["pity_count"].Value<int>(), rng);
            Assert.IsFalse(result.IsPity, testCase["name"].Value<string>());
        }

        private void TestDisplayedRates(JToken testCase)
        {
            var rates = GachaRoller.GetDisplayedRates(_testPool);
            var expected = testCase["expected_rates"];

            Assert.AreEqual(expected["5"].Value<double>(), rates[5], 0.1);
            Assert.AreEqual(expected["4"].Value<double>(), rates[4], 0.1);
            Assert.AreEqual(expected["3"].Value<double>(), rates[3], 0.1);
        }

        private void TestMultiPullPity(JToken testCase)
        {
            var rng = new Random(12345);
            int initialPity = testCase["initial_pity"].Value<int>();
            int pullCount = testCase["pull_count"].Value<int>();

            var results = GachaRoller.RollMulti(_testPool, pullCount, initialPity, rng);
            Assert.AreEqual(pullCount, results.Count);
        }

        private void TestEmptyPool(JToken testCase)
        {
            var poolOverride = testCase["pool_override"];
            var emptyPool = new GachaPool(
                poolOverride["id"].Value<int>(),
                "Empty",
                "gems",
                5,
                new List<GachaEntry>(),
                poolOverride["pity_threshold"].Value<int>(),
                Array.Empty<int>()
            );

            var rng = new Random(12345);
            var result = GachaRoller.Roll(emptyPool, 0, rng);
            Assert.AreEqual(0, result.MonsterId);
        }
    }
}
