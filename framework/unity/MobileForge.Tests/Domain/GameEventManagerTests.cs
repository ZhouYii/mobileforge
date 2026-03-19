using System;
using System.Collections.Generic;
using NUnit.Framework;
using MobileForge.Domain;

namespace MobileForge.Tests.Domain
{
    [TestFixture]
    public class GameEventManagerTests
    {
        private GameEventManager _manager;
        private long _currentTime;
        private GameEventDef _eventDef;
        private List<string> _startedEvents;
        private List<string> _endedEvents;
        private List<(string eventId, int totalPoints)> _pointsEarned;
        private List<(string eventId, int milestoneIndex)> _milestonesClaimed;

        [SetUp]
        public void SetUp()
        {
            _currentTime = 1000;
            _manager = new GameEventManager(() => _currentTime);

            _startedEvents = new List<string>();
            _endedEvents = new List<string>();
            _pointsEarned = new List<(string, int)>();
            _milestonesClaimed = new List<(string, int)>();

            _manager.EventStarted += id => _startedEvents.Add(id);
            _manager.EventEnded += id => _endedEvents.Add(id);
            _manager.EventPointsEarned += (id, pts) => _pointsEarned.Add((id, pts));
            _manager.EventMilestoneClaimed += (id, idx) => _milestonesClaimed.Add((id, idx));

            _eventDef = new GameEventDef
            {
                Id = "summer_event",
                Name = "Summer Festival",
                StartTime = 500,
                EndTime = 2000,
                GraceEndTime = 2500,
                PointCurrencyId = "summer_pts",
                EventShopSectionId = "summer_shop",
                Milestones = new List<EventMilestoneDef>
                {
                    new() { PointsRequired = 100, Rewards = new List<Dictionary<string, object>> { new() { ["type"] = "currency", ["id"] = "coins", ["count"] = 500 } } },
                    new() { PointsRequired = 300, Rewards = new List<Dictionary<string, object>> { new() { ["type"] = "currency", ["id"] = "gems", ["count"] = 50 } } },
                    new() { PointsRequired = 500, Rewards = new List<Dictionary<string, object>> { new() { ["type"] = "item", ["id"] = "summer_skin" } } },
                    new() { PointsRequired = 100, IsLoop = true, Rewards = new List<Dictionary<string, object>> { new() { ["type"] = "currency", ["id"] = "coins", ["count"] = 200 } } },
                },
                Bonuses = new List<EventBonusDef>
                {
                    new() { MatchType = EventBonusMatchType.MonsterId, MatchValue = "mon_summer_01", PointMultiplier = 2.0f },
                    new() { MatchType = EventBonusMatchType.Rarity, MatchValue = "SSR", PointMultiplier = 1.5f },
                    new() { MatchType = EventBonusMatchType.Element, MatchValue = "fire", PointMultiplier = 1.3f },
                    new() { MatchType = EventBonusMatchType.Tag, MatchValue = "summer", PointMultiplier = 1.2f },
                },
            };
        }

        // --- GetActiveEvents ---

        [Test]
        public void GetActiveEvents_ReturnsOnlyEventsWithinTimeWindow()
        {
            var futureEvent = new GameEventDef
            {
                Id = "future_event",
                Name = "Future",
                StartTime = 3000,
                EndTime = 4000,
                GraceEndTime = 4500,
            };
            var pastEvent = new GameEventDef
            {
                Id = "past_event",
                Name = "Past",
                StartTime = 100,
                EndTime = 200,
                GraceEndTime = 300,
            };

            _manager.LoadEvents(new[] { _eventDef, futureEvent, pastEvent });

            var active = _manager.GetActiveEvents();
            Assert.AreEqual(1, active.Count);
            Assert.AreEqual("summer_event", active[0].Id);
        }

        [Test]
        public void GetActiveEvents_ReturnsEmpty_WhenNoEventsActive()
        {
            _currentTime = 5000;
            _manager.LoadEvents(new[] { _eventDef });

            var active = _manager.GetActiveEvents();
            Assert.AreEqual(0, active.Count);
        }

        // --- GetStatus ---

        [Test]
        public void GetStatus_ReturnsUpcoming_BeforeStartTime()
        {
            _currentTime = 100;
            _manager.LoadEvents(new[] { _eventDef });

            Assert.AreEqual(EventStatus.Upcoming, _manager.GetStatus("summer_event"));
        }

        [Test]
        public void GetStatus_ReturnsActive_DuringEvent()
        {
            _currentTime = 1000;
            _manager.LoadEvents(new[] { _eventDef });

            Assert.AreEqual(EventStatus.Active, _manager.GetStatus("summer_event"));
        }

        [Test]
        public void GetStatus_ReturnsGracePeriod_AfterEndBeforeGraceEnd()
        {
            _currentTime = 2100;
            _manager.LoadEvents(new[] { _eventDef });

            Assert.AreEqual(EventStatus.GracePeriod, _manager.GetStatus("summer_event"));
        }

        [Test]
        public void GetStatus_ReturnsEnded_AfterGraceEndTime()
        {
            _currentTime = 3000;
            _manager.LoadEvents(new[] { _eventDef });

            Assert.AreEqual(EventStatus.Ended, _manager.GetStatus("summer_event"));
        }

        [Test]
        public void GetStatus_ReturnsEnded_ForUnknownEvent()
        {
            Assert.AreEqual(EventStatus.Ended, _manager.GetStatus("nonexistent"));
        }

        // --- AddPoints ---

        [Test]
        public void AddPoints_AccumulatesCorrectly()
        {
            _manager.LoadEvents(new[] { _eventDef });

            _manager.AddPoints("summer_event", 50);
            Assert.AreEqual(50, _manager.GetPoints("summer_event"));

            _manager.AddPoints("summer_event", 75);
            Assert.AreEqual(125, _manager.GetPoints("summer_event"));
        }

        [Test]
        public void AddPoints_ReturnsZero_WhenEventNotActive()
        {
            _currentTime = 3000;
            _manager.LoadEvents(new[] { _eventDef });

            int earned = _manager.AddPoints("summer_event", 100);
            Assert.AreEqual(0, earned);
            Assert.AreEqual(0, _manager.GetPoints("summer_event"));
        }

        [Test]
        public void AddPoints_AppliesBonusMultiplier_FromTeamComposition()
        {
            _manager.LoadEvents(new[] { _eventDef });

            var team = new List<Dictionary<string, object>>
            {
                new() { ["id"] = "mon_summer_01", ["rarity"] = "SR", ["element"] = "water" },
            };

            // mon_summer_01 matches MonsterId bonus (2.0f), so multiplier = 1.0 + (2.0 - 1.0) = 2.0
            int earned = _manager.AddPoints("summer_event", 100, team);
            Assert.AreEqual(200, earned);
            Assert.AreEqual(200, _manager.GetPoints("summer_event"));
        }

        [Test]
        public void AddPoints_AppliesMultipleBonuses()
        {
            _manager.LoadEvents(new[] { _eventDef });

            var team = new List<Dictionary<string, object>>
            {
                new() { ["id"] = "mon_summer_01", ["rarity"] = "SSR", ["element"] = "fire", ["tags"] = new List<object> { "summer" } },
            };

            // All 4 bonuses match: 1.0 + (2.0-1.0) + (1.5-1.0) + (1.3-1.0) + (1.2-1.0) = 3.0
            int earned = _manager.AddPoints("summer_event", 100, team);
            Assert.AreEqual(300, earned);
        }

        // --- CalculateBonus ---

        [Test]
        public void CalculateBonus_MatchesByMonsterId()
        {
            _manager.LoadEvents(new[] { _eventDef });

            var team = new List<Dictionary<string, object>>
            {
                new() { ["id"] = "mon_summer_01", ["rarity"] = "R", ["element"] = "water" },
            };

            float multiplier = _manager.CalculateBonus("summer_event", team);
            Assert.AreEqual(2.0f, multiplier, 0.001f);
        }

        [Test]
        public void CalculateBonus_MatchesByRarity()
        {
            _manager.LoadEvents(new[] { _eventDef });

            var team = new List<Dictionary<string, object>>
            {
                new() { ["id"] = "mon_generic", ["rarity"] = "SSR", ["element"] = "water" },
            };

            float multiplier = _manager.CalculateBonus("summer_event", team);
            Assert.AreEqual(1.5f, multiplier, 0.001f);
        }

        [Test]
        public void CalculateBonus_MatchesByElement()
        {
            _manager.LoadEvents(new[] { _eventDef });

            var team = new List<Dictionary<string, object>>
            {
                new() { ["id"] = "mon_generic", ["rarity"] = "R", ["element"] = "fire" },
            };

            float multiplier = _manager.CalculateBonus("summer_event", team);
            Assert.AreEqual(1.3f, multiplier, 0.001f);
        }

        [Test]
        public void CalculateBonus_MatchesByTag()
        {
            _manager.LoadEvents(new[] { _eventDef });

            var team = new List<Dictionary<string, object>>
            {
                new() { ["id"] = "mon_generic", ["rarity"] = "R", ["element"] = "water", ["tags"] = new List<object> { "summer", "beach" } },
            };

            float multiplier = _manager.CalculateBonus("summer_event", team);
            Assert.AreEqual(1.2f, multiplier, 0.001f);
        }

        [Test]
        public void CalculateBonus_ReturnsBaseMultiplier_WhenNoMatch()
        {
            _manager.LoadEvents(new[] { _eventDef });

            var team = new List<Dictionary<string, object>>
            {
                new() { ["id"] = "mon_generic", ["rarity"] = "R", ["element"] = "water" },
            };

            float multiplier = _manager.CalculateBonus("summer_event", team);
            Assert.AreEqual(1.0f, multiplier, 0.001f);
        }

        [Test]
        public void CalculateBonus_EachBonusAppliesAtMostOncePerTeam()
        {
            _manager.LoadEvents(new[] { _eventDef });

            // Two SSR monsters — Rarity bonus should still only apply once
            var team = new List<Dictionary<string, object>>
            {
                new() { ["id"] = "mon_a", ["rarity"] = "SSR", ["element"] = "water" },
                new() { ["id"] = "mon_b", ["rarity"] = "SSR", ["element"] = "water" },
            };

            float multiplier = _manager.CalculateBonus("summer_event", team);
            Assert.AreEqual(1.5f, multiplier, 0.001f);
        }

        // --- ClaimMilestone ---

        [Test]
        public void ClaimMilestone_Succeeds_WhenEnoughPoints()
        {
            _manager.LoadEvents(new[] { _eventDef });
            _manager.AddPoints("summer_event", 150);

            var result = _manager.ClaimMilestone("summer_event", 0);
            Assert.IsTrue((bool)result["success"]);
            Assert.IsNotNull(result["rewards"]);
        }

        [Test]
        public void ClaimMilestone_Fails_WhenInsufficientPoints()
        {
            _manager.LoadEvents(new[] { _eventDef });
            _manager.AddPoints("summer_event", 50);

            var result = _manager.ClaimMilestone("summer_event", 0);
            Assert.IsFalse((bool)result["success"]);
            Assert.AreEqual("insufficient_points", result["error"]);
        }

        [Test]
        public void ClaimMilestone_Fails_WhenAlreadyClaimed()
        {
            _manager.LoadEvents(new[] { _eventDef });
            _manager.AddPoints("summer_event", 150);

            _manager.ClaimMilestone("summer_event", 0);
            var result = _manager.ClaimMilestone("summer_event", 0);

            Assert.IsFalse((bool)result["success"]);
            Assert.AreEqual("already_claimed", result["error"]);
        }

        [Test]
        public void ClaimMilestone_AllowedDuringGracePeriod()
        {
            _manager.LoadEvents(new[] { _eventDef });
            _manager.AddPoints("summer_event", 150);

            _currentTime = 2100; // grace period
            var result = _manager.ClaimMilestone("summer_event", 0);
            Assert.IsTrue((bool)result["success"]);
        }

        [Test]
        public void ClaimMilestone_FailsWhenEnded()
        {
            _manager.LoadEvents(new[] { _eventDef });
            _manager.AddPoints("summer_event", 150);

            _currentTime = 3000; // ended
            var result = _manager.ClaimMilestone("summer_event", 0);
            Assert.IsFalse((bool)result["success"]);
            Assert.AreEqual("event_not_active", result["error"]);
        }

        // --- Loop milestones ---

        [Test]
        public void LoopMilestone_CanBeClaimedRepeatedly()
        {
            _manager.LoadEvents(new[] { _eventDef });
            _manager.AddPoints("summer_event", 500);

            // Milestone index 3 is the loop milestone (100 pts required)
            // First claim: requires 100 * (0+1) = 100
            var result1 = _manager.ClaimMilestone("summer_event", 3);
            Assert.IsTrue((bool)result1["success"]);
            Assert.AreEqual(1, result1["loop_count"]);

            // Second claim: requires 100 * (1+1) = 200
            var result2 = _manager.ClaimMilestone("summer_event", 3);
            Assert.IsTrue((bool)result2["success"]);
            Assert.AreEqual(2, result2["loop_count"]);
        }

        [Test]
        public void LoopMilestone_FailsWhenInsufficientPointsForNextLoop()
        {
            _manager.LoadEvents(new[] { _eventDef });
            _manager.AddPoints("summer_event", 150);

            // First claim: requires 100 * 1 = 100, passes
            _manager.ClaimMilestone("summer_event", 3);

            // Second claim: requires 100 * 2 = 200, fails (only 150 points)
            var result = _manager.ClaimMilestone("summer_event", 3);
            Assert.IsFalse((bool)result["success"]);
            Assert.AreEqual("insufficient_points", result["error"]);
        }

        // --- GetNextMilestoneIndex ---

        [Test]
        public void GetNextMilestoneIndex_ReturnsFirstUnclaimed()
        {
            _manager.LoadEvents(new[] { _eventDef });

            Assert.AreEqual(0, _manager.GetNextMilestoneIndex("summer_event"));
        }

        [Test]
        public void GetNextMilestoneIndex_AdvancesAfterClaim()
        {
            _manager.LoadEvents(new[] { _eventDef });
            _manager.AddPoints("summer_event", 500);

            _manager.ClaimMilestone("summer_event", 0);
            Assert.AreEqual(1, _manager.GetNextMilestoneIndex("summer_event"));

            _manager.ClaimMilestone("summer_event", 1);
            Assert.AreEqual(2, _manager.GetNextMilestoneIndex("summer_event"));
        }

        [Test]
        public void GetNextMilestoneIndex_SkipsLoopMilestones()
        {
            _manager.LoadEvents(new[] { _eventDef });
            _manager.AddPoints("summer_event", 600);

            _manager.ClaimMilestone("summer_event", 0);
            _manager.ClaimMilestone("summer_event", 1);
            _manager.ClaimMilestone("summer_event", 2);

            // All non-loop milestones claimed; loop milestone at index 3 is skipped
            Assert.AreEqual(-1, _manager.GetNextMilestoneIndex("summer_event"));
        }

        [Test]
        public void GetNextMilestoneIndex_ReturnsNegativeOne_ForUnknownEvent()
        {
            Assert.AreEqual(-1, _manager.GetNextMilestoneIndex("nonexistent"));
        }

        // --- GetClaimableMilestones ---

        [Test]
        public void GetClaimableMilestones_ReturnsCorrectIndices()
        {
            _manager.LoadEvents(new[] { _eventDef });
            _manager.AddPoints("summer_event", 350);

            var claimable = _manager.GetClaimableMilestones("summer_event");
            // 350 >= 100 (index 0), 350 >= 300 (index 1), 350 < 500 (index 2), loop: 350 >= 100*1 (index 3)
            Assert.Contains(0, claimable);
            Assert.Contains(1, claimable);
            Assert.IsFalse(claimable.Contains(2));
            Assert.Contains(3, claimable);
        }

        [Test]
        public void GetClaimableMilestones_ExcludesAlreadyClaimed()
        {
            _manager.LoadEvents(new[] { _eventDef });
            _manager.AddPoints("summer_event", 350);

            _manager.ClaimMilestone("summer_event", 0);

            var claimable = _manager.GetClaimableMilestones("summer_event");
            Assert.IsFalse(claimable.Contains(0));
            Assert.Contains(1, claimable);
        }

        [Test]
        public void GetClaimableMilestones_ReturnsEmpty_WhenNoPoints()
        {
            _manager.LoadEvents(new[] { _eventDef });

            var claimable = _manager.GetClaimableMilestones("summer_event");
            Assert.AreEqual(0, claimable.Count);
        }

        // --- Events fire correctly ---

        [Test]
        public void EventStarted_FiresWhenLoadingActiveEvent()
        {
            _manager.LoadEvents(new[] { _eventDef });
            Assert.Contains("summer_event", _startedEvents);
        }

        [Test]
        public void EventStarted_DoesNotFire_ForUpcomingEvent()
        {
            _currentTime = 100;
            _manager.LoadEvents(new[] { _eventDef });
            Assert.AreEqual(0, _startedEvents.Count);
        }

        [Test]
        public void EventPointsEarned_FiresOnAddPoints()
        {
            _manager.LoadEvents(new[] { _eventDef });
            _manager.AddPoints("summer_event", 50);

            Assert.AreEqual(1, _pointsEarned.Count);
            Assert.AreEqual("summer_event", _pointsEarned[0].eventId);
            Assert.AreEqual(50, _pointsEarned[0].totalPoints);
        }

        [Test]
        public void EventMilestoneClaimed_FiresOnClaim()
        {
            _manager.LoadEvents(new[] { _eventDef });
            _manager.AddPoints("summer_event", 150);
            _manager.ClaimMilestone("summer_event", 0);

            Assert.AreEqual(1, _milestonesClaimed.Count);
            Assert.AreEqual("summer_event", _milestonesClaimed[0].eventId);
            Assert.AreEqual(0, _milestonesClaimed[0].milestoneIndex);
        }

        // --- ToSaveDict / FromSaveDict round-trip ---

        [Test]
        public void ToSaveDict_FromSaveDict_RoundTrip()
        {
            _manager.LoadEvents(new[] { _eventDef });
            _manager.AddPoints("summer_event", 350);
            _manager.ClaimMilestone("summer_event", 0);
            _manager.ClaimMilestone("summer_event", 1);
            _manager.ClaimMilestone("summer_event", 3); // loop milestone

            var saveData = _manager.ToSaveDict();

            // Create a new manager and restore
            var newManager = new GameEventManager(() => _currentTime);
            newManager.LoadEvents(new[] { _eventDef });
            newManager.FromSaveDict(saveData);

            Assert.AreEqual(350, newManager.GetPoints("summer_event"));

            // Previously claimed non-loop milestones should be claimed
            var result0 = newManager.ClaimMilestone("summer_event", 0);
            Assert.IsFalse((bool)result0["success"]);
            Assert.AreEqual("already_claimed", result0["error"]);

            var result1 = newManager.ClaimMilestone("summer_event", 1);
            Assert.IsFalse((bool)result1["success"]);
            Assert.AreEqual("already_claimed", result1["error"]);

            // Milestone index 2 should still be claimable (not enough points though)
            Assert.AreEqual(2, newManager.GetNextMilestoneIndex("summer_event"));

            // Loop milestone should have loop_count = 1 restored
            // Next loop claim requires 100 * (1+1) = 200, and we have 350 points
            var resultLoop = newManager.ClaimMilestone("summer_event", 3);
            Assert.IsTrue((bool)resultLoop["success"]);
            Assert.AreEqual(2, resultLoop["loop_count"]);
        }

        [Test]
        public void FromSaveDict_ClearsExistingState()
        {
            _manager.LoadEvents(new[] { _eventDef });
            _manager.AddPoints("summer_event", 500);

            var emptyData = new Dictionary<string, object>
            {
                ["states"] = new Dictionary<string, object>(),
            };

            _manager.FromSaveDict(emptyData);
            Assert.AreEqual(0, _manager.GetPoints("summer_event"));
        }

        // --- GetPoints ---

        [Test]
        public void GetPoints_ReturnsZero_ForUnknownEvent()
        {
            Assert.AreEqual(0, _manager.GetPoints("nonexistent"));
        }
    }
}
