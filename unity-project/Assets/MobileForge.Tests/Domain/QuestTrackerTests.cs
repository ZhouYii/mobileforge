using System;
using System.Collections.Generic;
using NUnit.Framework;
using MobileForge.Domain;

namespace MobileForge.Tests.Domain
{
    [TestFixture]
    public class QuestTrackerTests
    {
        private QuestTracker _tracker;

        [SetUp]
        public void SetUp()
        {
            _tracker = new QuestTracker();
        }

        [Test]
        public void ActivateQuest_ReturnsTrueAndFiresEvent()
        {
            _tracker.LoadQuestDefs(new[] { new QuestDef { Id = "q1", Name = "Test Quest" } });
            string activatedId = null;
            _tracker.QuestActivated += id => activatedId = id;

            bool result = _tracker.ActivateQuest("q1");
            Assert.IsTrue(result);
            Assert.AreEqual("q1", activatedId);
        }

        [Test]
        public void ActivateQuest_ReturnsFalseForMissingDef()
        {
            bool result = _tracker.ActivateQuest("nonexistent");
            Assert.IsFalse(result);
        }

        [Test]
        public void ActivateQuest_ReturnsFalseForAlreadyActive()
        {
            _tracker.LoadQuestDefs(new[] { new QuestDef { Id = "q1" } });
            _tracker.ActivateQuest("q1");

            bool result = _tracker.ActivateQuest("q1");
            Assert.IsFalse(result);
        }

        [Test]
        public void ActivateQuest_ReturnsFalseIfPrereqNotClaimed()
        {
            _tracker.LoadQuestDefs(new[]
            {
                new QuestDef { Id = "q1" },
                new QuestDef { Id = "q2", Prerequisites = new List<string> { "q1" } },
            });

            bool result = _tracker.ActivateQuest("q2");
            Assert.IsFalse(result);
        }

        [Test]
        public void ActivateQuest_ReturnsTrueIfPrereqClaimed()
        {
            _tracker.LoadQuestDefs(new[]
            {
                new QuestDef { Id = "q1" },
                new QuestDef { Id = "q2", Prerequisites = new List<string> { "q1" } },
            });
            _tracker.ActivateQuest("q1");
            _tracker.OnEvent("any");
            _tracker.ClaimQuest("q1");

            bool result = _tracker.ActivateQuest("q2");
            Assert.IsTrue(result);
        }

        [Test]
        public void ActivateQuest_ReturnsFalseForNonRepeatableClaimed()
        {
            _tracker.LoadQuestDefs(new[] { new QuestDef { Id = "q1", Repeatable = false } });
            _tracker.ActivateQuest("q1");
            _tracker.OnEvent("any");
            _tracker.ClaimQuest("q1");

            bool result = _tracker.ActivateQuest("q1");
            Assert.IsFalse(result);
        }

        [Test]
        public void ActivateQuest_ReturnsTrueForRepeatable()
        {
            _tracker.LoadQuestDefs(new[] { new QuestDef { Id = "q1", Repeatable = true } });
            _tracker.ActivateQuest("q1");
            _tracker.OnEvent("any");
            _tracker.ClaimQuest("q1");

            bool result = _tracker.ActivateQuest("q1");
            Assert.IsTrue(result);
        }

        [Test]
        public void OnEvent_AdvancesProgress()
        {
            _tracker.LoadQuestDefs(new[]
            {
                new QuestDef
                {
                    Id = "q1",
                    Objectives = new List<QuestObjective>
                    {
                        new QuestObjective { EventName = "kill_monster", TargetCount = 3 },
                    },
                },
            });
            _tracker.ActivateQuest("q1");

            int progressCurrent = -1, progressTarget = -1;
            _tracker.QuestProgress += (qid, idx, cur, tgt) => { progressCurrent = cur; progressTarget = tgt; };

            _tracker.OnEvent("kill_monster");
            Assert.AreEqual(1, progressCurrent);
            Assert.AreEqual(3, progressTarget);
        }

        [Test]
        public void OnEvent_CompletesQuest()
        {
            _tracker.LoadQuestDefs(new[]
            {
                new QuestDef
                {
                    Id = "q1",
                    Objectives = new List<QuestObjective>
                    {
                        new QuestObjective { EventName = "kill", TargetCount = 2 },
                    },
                },
            });
            _tracker.ActivateQuest("q1");

            string completedId = null;
            _tracker.QuestCompleted += id => completedId = id;

            _tracker.OnEvent("kill");
            Assert.IsNull(completedId);

            var completed = _tracker.OnEvent("kill");
            Assert.AreEqual("q1", completedId);
            Assert.Contains("q1", completed);
        }

        [Test]
        public void OnEvent_RespectsFilter()
        {
            _tracker.LoadQuestDefs(new[]
            {
                new QuestDef
                {
                    Id = "q1",
                    Objectives = new List<QuestObjective>
                    {
                        new QuestObjective
                        {
                            EventName = "kill",
                            Filter = new Dictionary<string, object> { ["monster_type"] = "dragon" },
                            TargetCount = 1,
                        },
                    },
                },
            });
            _tracker.ActivateQuest("q1");

            _tracker.OnEvent("kill", new Dictionary<string, object> { ["monster_type"] = "slime" });
            var state = _tracker.GetQuestState("q1");
            Assert.AreEqual(0, state.ObjectiveProgress[0]);

            _tracker.OnEvent("kill", new Dictionary<string, object> { ["monster_type"] = "dragon" });
            Assert.AreEqual(1, state.ObjectiveProgress[0]);
        }

        [Test]
        public void GetActiveQuests_ReturnsOnlyActive()
        {
            _tracker.LoadQuestDefs(new[]
            {
                new QuestDef { Id = "q1", Objectives = new List<QuestObjective> { new QuestObjective { EventName = "e", TargetCount = 1 } } },
                new QuestDef { Id = "q2", Objectives = new List<QuestObjective> { new QuestObjective { EventName = "e", TargetCount = 1 } } },
            });
            _tracker.ActivateQuest("q1");
            _tracker.ActivateQuest("q2");
            _tracker.OnEvent("e");
            _tracker.OnEvent("e");

            var active = _tracker.GetActiveQuests();
            Assert.AreEqual(0, active.Count);

            var completed = _tracker.GetCompletedUnclaimed();
            Assert.AreEqual(2, completed.Count);
        }

        [Test]
        public void ClaimQuest_ReturnsRewards()
        {
            _tracker.LoadQuestDefs(new[]
            {
                new QuestDef
                {
                    Id = "q1",
                    Objectives = new List<QuestObjective> { new QuestObjective { EventName = "e", TargetCount = 1 } },
                    Rewards = new List<Dictionary<string, object>> { new() { ["type"] = "currency", ["id"] = "coins", ["count"] = 100 } },
                },
            });
            _tracker.ActivateQuest("q1");
            _tracker.OnEvent("e");

            string claimedId = null;
            _tracker.QuestClaimed += id => claimedId = id;

            var rewards = _tracker.ClaimQuest("q1");
            Assert.IsNotNull(rewards);
            Assert.AreEqual(1, rewards.Count);
            Assert.AreEqual("q1", claimedId);
        }

        [Test]
        public void ClaimQuest_ReturnsNullIfNotComplete()
        {
            _tracker.LoadQuestDefs(new[]
            {
                new QuestDef
                {
                    Id = "q1",
                    Objectives = new List<QuestObjective> { new QuestObjective { EventName = "e", TargetCount = 2 } },
                },
            });
            _tracker.ActivateQuest("q1");
            _tracker.OnEvent("e");

            var rewards = _tracker.ClaimQuest("q1");
            Assert.IsNull(rewards);
        }

        [Test]
        public void ClaimQuest_ReturnsNullIfAlreadyClaimed()
        {
            _tracker.LoadQuestDefs(new[]
            {
                new QuestDef { Id = "q1", Objectives = new List<QuestObjective> { new QuestObjective { EventName = "e", TargetCount = 1 } } },
            });
            _tracker.ActivateQuest("q1");
            _tracker.OnEvent("e");
            _tracker.ClaimQuest("q1");

            var rewards = _tracker.ClaimQuest("q1");
            Assert.IsNull(rewards);
        }

        [Test]
        public void HasClaimable_WorksCorrectly()
        {
            _tracker.LoadQuestDefs(new[]
            {
                new QuestDef { Id = "q1", Objectives = new List<QuestObjective> { new QuestObjective { EventName = "e", TargetCount = 1 } } },
            });
            Assert.IsFalse(_tracker.HasClaimable());

            _tracker.ActivateQuest("q1");
            Assert.IsFalse(_tracker.HasClaimable());

            _tracker.OnEvent("e");
            Assert.IsTrue(_tracker.HasClaimable());

            _tracker.ClaimQuest("q1");
            Assert.IsFalse(_tracker.HasClaimable());
        }

        [Test]
        public void ToSaveDict_RoundTrip()
        {
            _tracker.LoadQuestDefs(new[]
            {
                new QuestDef
                {
                    Id = "q1",
                    Objectives = new List<QuestObjective> { new QuestObjective { EventName = "e", TargetCount = 3 } },
                },
                new QuestDef
                {
                    Id = "q2",
                    Objectives = new List<QuestObjective> { new QuestObjective { EventName = "f", TargetCount = 1 } },
                },
            });
            _tracker.ActivateQuest("q1");
            _tracker.ActivateQuest("q2");
            _tracker.OnEvent("e");
            _tracker.OnEvent("f");
            _tracker.ClaimQuest("q2");

            var saved = _tracker.ToSaveDict();
            var newTracker = new QuestTracker();
            newTracker.LoadQuestDefs(_tracker.GetQuestState("q1") == null ? Array.Empty<QuestDef>() : Array.Empty<QuestDef>());
            newTracker.FromSaveDict(saved);

            Assert.AreEqual(1, newTracker.GetActiveQuests().Count);
            Assert.AreEqual(1, newTracker.GetCompletedUnclaimed().Count);
        }

        // ── Daily/Weekly Reset Tests ──

        [Test]
        public void CheckAndResetCycle_ReactivatesDailyQuests()
        {
            var currentDate = new DateTime(2026, 3, 18, 10, 0, 0, DateTimeKind.Utc);
            var tracker = new QuestTracker(() => currentDate);
            tracker.LoadQuestDefs(new[]
            {
                new QuestDef
                {
                    Id = "daily1", Category = QuestCategory.Daily,
                    Objectives = new List<QuestObjective> { new QuestObjective { EventName = "e", TargetCount = 1 } },
                },
                new QuestDef
                {
                    Id = "daily2", Category = QuestCategory.Daily,
                    Objectives = new List<QuestObjective> { new QuestObjective { EventName = "f", TargetCount = 1 } },
                },
            });

            var config = new MissionResetConfig { ResetCategory = QuestCategory.Daily, ResetHourUtc = 4 };

            // First reset activates quests
            int count = tracker.CheckAndResetCycle(config);
            Assert.AreEqual(2, count);
            Assert.AreEqual(2, tracker.GetActiveQuests().Count);
        }

        [Test]
        public void CheckAndResetCycle_DoesNotDoubleReset()
        {
            var currentDate = new DateTime(2026, 3, 18, 10, 0, 0, DateTimeKind.Utc);
            var tracker = new QuestTracker(() => currentDate);
            tracker.LoadQuestDefs(new[]
            {
                new QuestDef
                {
                    Id = "daily1", Category = QuestCategory.Daily,
                    Objectives = new List<QuestObjective> { new QuestObjective { EventName = "e", TargetCount = 1 } },
                },
            });

            var config = new MissionResetConfig { ResetCategory = QuestCategory.Daily, ResetHourUtc = 4 };
            tracker.CheckAndResetCycle(config);

            // Complete and claim
            tracker.OnEvent("e");
            tracker.ClaimQuest("daily1");

            // Same day reset does nothing
            int count = tracker.CheckAndResetCycle(config);
            Assert.AreEqual(0, count);
        }

        [Test]
        public void CheckAndResetCycle_ResetsOnNewDay()
        {
            var currentDate = new DateTime(2026, 3, 18, 10, 0, 0, DateTimeKind.Utc);
            var tracker = new QuestTracker(() => currentDate);
            tracker.LoadQuestDefs(new[]
            {
                new QuestDef
                {
                    Id = "daily1", Category = QuestCategory.Daily,
                    Objectives = new List<QuestObjective> { new QuestObjective { EventName = "e", TargetCount = 1 } },
                },
            });

            var config = new MissionResetConfig { ResetCategory = QuestCategory.Daily, ResetHourUtc = 4 };
            tracker.CheckAndResetCycle(config);
            tracker.OnEvent("e");
            tracker.ClaimQuest("daily1");

            // Advance to next day (after reset hour)
            currentDate = new DateTime(2026, 3, 19, 10, 0, 0, DateTimeKind.Utc);
            var tracker2 = new QuestTracker(() => currentDate);
            tracker2.LoadQuestDefs(new[]
            {
                new QuestDef
                {
                    Id = "daily1", Category = QuestCategory.Daily,
                    Objectives = new List<QuestObjective> { new QuestObjective { EventName = "e", TargetCount = 1 } },
                },
            });
            // Restore state from previous day
            tracker2.FromSaveDict(tracker.ToSaveDict());
            int count = tracker2.CheckAndResetCycle(config);
            Assert.AreEqual(1, count);
            Assert.AreEqual(1, tracker2.GetActiveQuests().Count);
        }

        // ── Achievement Tier Tests ──

        [Test]
        public void ActivateNextTier_ActivatesSecondTier()
        {
            _tracker.LoadQuestDefs(new[]
            {
                new QuestDef
                {
                    Id = "ach_kill_t0", AchievementBaseId = "ach_kill", AchievementTier = 0,
                    Category = QuestCategory.Achievement,
                    Objectives = new List<QuestObjective> { new QuestObjective { EventName = "kill", TargetCount = 10 } },
                },
                new QuestDef
                {
                    Id = "ach_kill_t1", AchievementBaseId = "ach_kill", AchievementTier = 1,
                    Category = QuestCategory.Achievement,
                    Objectives = new List<QuestObjective> { new QuestObjective { EventName = "kill", TargetCount = 100 } },
                },
            });

            _tracker.ActivateQuest("ach_kill_t0");
            for (int i = 0; i < 10; i++) _tracker.OnEvent("kill");
            _tracker.ClaimQuest("ach_kill_t0");

            string nextId = _tracker.ActivateNextTier("ach_kill");
            Assert.AreEqual("ach_kill_t1", nextId);
            Assert.IsNotNull(_tracker.GetQuestState("ach_kill_t1"));
        }

        [Test]
        public void ActivateNextTier_ReturnsNullWhenNoMoreTiers()
        {
            _tracker.LoadQuestDefs(new[]
            {
                new QuestDef
                {
                    Id = "ach_t0", AchievementBaseId = "ach", AchievementTier = 0,
                    Category = QuestCategory.Achievement,
                    Objectives = new List<QuestObjective> { new QuestObjective { EventName = "e", TargetCount = 1 } },
                },
            });

            _tracker.ActivateQuest("ach_t0");
            _tracker.OnEvent("e");
            _tracker.ClaimQuest("ach_t0");

            string nextId = _tracker.ActivateNextTier("ach");
            Assert.IsNull(nextId);
        }

        // ── Category Helpers ──

        [Test]
        public void ActivateCategory_ActivatesAll()
        {
            _tracker.LoadQuestDefs(new[]
            {
                new QuestDef { Id = "d1", Category = QuestCategory.Daily },
                new QuestDef { Id = "d2", Category = QuestCategory.Daily },
                new QuestDef { Id = "s1", Category = QuestCategory.Story },
            });

            int count = _tracker.ActivateCategory(QuestCategory.Daily);
            Assert.AreEqual(2, count);
            Assert.AreEqual(2, _tracker.GetActiveQuests().Count);
        }

        [Test]
        public void GetQuestsByCategory_ReturnsCorrect()
        {
            _tracker.LoadQuestDefs(new[]
            {
                new QuestDef { Id = "d1", Category = QuestCategory.Daily },
                new QuestDef { Id = "d2", Category = QuestCategory.Daily },
                new QuestDef { Id = "w1", Category = QuestCategory.Weekly },
            });

            var daily = _tracker.GetQuestsByCategory(QuestCategory.Daily);
            Assert.AreEqual(2, daily.Count);

            var weekly = _tracker.GetQuestsByCategory(QuestCategory.Weekly);
            Assert.AreEqual(1, weekly.Count);
        }

        [Test]
        public void GetClaimableCountByCategory_FiltersCorrectly()
        {
            _tracker.LoadQuestDefs(new[]
            {
                new QuestDef
                {
                    Id = "d1", Category = QuestCategory.Daily,
                    Objectives = new List<QuestObjective> { new QuestObjective { EventName = "e", TargetCount = 1 } },
                },
                new QuestDef
                {
                    Id = "w1", Category = QuestCategory.Weekly,
                    Objectives = new List<QuestObjective> { new QuestObjective { EventName = "e", TargetCount = 1 } },
                },
            });
            _tracker.ActivateQuest("d1");
            _tracker.ActivateQuest("w1");
            _tracker.OnEvent("e");

            Assert.AreEqual(1, _tracker.GetClaimableCountByCategory(QuestCategory.Daily));
            Assert.AreEqual(1, _tracker.GetClaimableCountByCategory(QuestCategory.Weekly));
            Assert.AreEqual(0, _tracker.GetClaimableCountByCategory(QuestCategory.Story));
        }
    }
}
