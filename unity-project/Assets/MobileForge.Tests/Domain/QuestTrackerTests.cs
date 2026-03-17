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
    }
}
