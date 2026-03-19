using System;
using System.Collections.Generic;
using NUnit.Framework;
using MobileForge.Domain;

namespace MobileForge.Tests.Domain
{
    [TestFixture]
    public class ArenaManagerTests
    {
        private ArenaManager _arena;
        private ArenaDef _def;
        private DateTime _now;

        [SetUp]
        public void SetUp()
        {
            _now = new DateTime(2026, 3, 18);

            _def = new ArenaDef
            {
                AttemptsPerDay = 5,
                SeasonDurationDays = 14,
                Tiers = new List<ArenaTierDef>
                {
                    new() { Id = "bronze", Name = "Bronze", MinTrophies = 0, MaxTrophies = 99, WinTrophies = 30, LoseTrophies = 10 },
                    new() { Id = "silver", Name = "Silver", MinTrophies = 100, MaxTrophies = 199, WinTrophies = 25, LoseTrophies = 15 },
                    new() { Id = "gold", Name = "Gold", MinTrophies = 200, MaxTrophies = -1, WinTrophies = 20, LoseTrophies = 20 },
                },
                SeasonRewards = new List<Dictionary<string, object>>
                {
                    new() { ["type"] = "currency", ["id"] = "gems", ["count"] = 500 },
                },
            };

            _arena = new ArenaManager(() => _now, _def);
        }

        // ── RecordWin ──

        [Test]
        public void RecordWin_AddsTrophies_AndIncrementsWins()
        {
            int trophies = _arena.RecordWin();

            Assert.AreEqual(30, trophies);
            Assert.AreEqual(1, _arena.GetRecord().Wins);
        }

        [Test]
        public void RecordWin_FiresArenaWinEvent()
        {
            int eventTrophies = -1;
            _arena.ArenaWin += t => eventTrophies = t;

            _arena.RecordWin();

            Assert.AreEqual(30, eventTrophies);
        }

        // ── RecordLoss ──

        [Test]
        public void RecordLoss_SubtractsTrophies_AndIncrementsLosses()
        {
            // Start with some trophies
            _arena.RecordWin(); // +30
            _arena.RecordWin(); // +30 = 60

            int trophies = _arena.RecordLoss(); // -10 = 50

            Assert.AreEqual(50, trophies);
            Assert.AreEqual(1, _arena.GetRecord().Losses);
        }

        [Test]
        public void RecordLoss_FloorAtZero()
        {
            int trophies = _arena.RecordLoss();

            Assert.AreEqual(0, trophies);
            Assert.AreEqual(1, _arena.GetRecord().Losses);
        }

        [Test]
        public void RecordLoss_FiresArenaLossEvent()
        {
            int eventTrophies = -1;
            _arena.ArenaLoss += t => eventTrophies = t;

            _arena.RecordLoss();

            Assert.AreEqual(0, eventTrophies);
        }

        // ── Attempts ──

        [Test]
        public void RecordWin_ConsumesAttempt()
        {
            _arena.RecordWin();

            Assert.AreEqual(4, _arena.GetRemainingAttempts());
        }

        [Test]
        public void RecordLoss_ConsumesAttempt()
        {
            _arena.RecordLoss();

            Assert.AreEqual(4, _arena.GetRemainingAttempts());
        }

        [Test]
        public void CanBattle_ReturnsFalse_WhenNoAttemptsRemain()
        {
            for (int i = 0; i < 5; i++)
                _arena.RecordWin();

            Assert.IsFalse(_arena.CanBattle());
        }

        [Test]
        public void CanBattle_ReturnsTrue_WhenAttemptsRemain()
        {
            _arena.RecordWin();

            Assert.IsTrue(_arena.CanBattle());
        }

        [Test]
        public void GetRemainingAttempts_Accurate()
        {
            Assert.AreEqual(5, _arena.GetRemainingAttempts());

            _arena.RecordWin();
            _arena.RecordLoss();

            Assert.AreEqual(3, _arena.GetRemainingAttempts());
        }

        // ── DailyReset ──

        [Test]
        public void DailyReset_RestoresAttempts()
        {
            for (int i = 0; i < 5; i++)
                _arena.RecordWin();

            Assert.IsFalse(_arena.CanBattle());

            _arena.DailyReset();

            Assert.IsTrue(_arena.CanBattle());
            Assert.AreEqual(5, _arena.GetRemainingAttempts());
        }

        [Test]
        public void CheckDailyReset_AutoResets_OnNewDay()
        {
            for (int i = 0; i < 5; i++)
                _arena.RecordWin();

            Assert.IsFalse(_arena.CanBattle());

            // Advance the date by one day
            _now = _now.AddDays(1);

            Assert.IsTrue(_arena.CanBattle());
            Assert.AreEqual(5, _arena.GetRemainingAttempts());
        }

        // ── GetCurrentTier ──

        [Test]
        public void GetCurrentTier_ReturnsBronze_AtZeroTrophies()
        {
            var tier = _arena.GetCurrentTier();

            Assert.AreEqual("bronze", tier.Id);
        }

        [Test]
        public void GetCurrentTier_ReturnsSilver_At100Trophies()
        {
            // Win enough to reach 100+ trophies (4 wins = 120)
            for (int i = 0; i < 4; i++)
                _arena.RecordWin();

            var tier = _arena.GetCurrentTier();

            Assert.AreEqual("silver", tier.Id);
        }

        [Test]
        public void GetCurrentTier_ReturnsGold_At200Trophies()
        {
            // Need 200+ trophies. Bronze gives 30 each.
            // 4 wins in bronze = 120 (now silver, gives 25 each)
            // 4 more wins in silver = 120 + 100 = 220 (now gold)
            for (int i = 0; i < 10; i++)
            {
                // Refresh attempts by advancing the day when needed
                if (!_arena.CanBattle())
                {
                    _now = _now.AddDays(1);
                }
                _arena.RecordWin();
            }

            Assert.IsTrue(_arena.GetRecord().Trophies >= 200);
            Assert.AreEqual("gold", _arena.GetCurrentTier().Id);
        }

        // ── Tier Change Event ──

        [Test]
        public void TierChange_FiresEvent_WhenCrossingBoundary()
        {
            string oldTier = null;
            string newTier = null;
            _arena.ArenaTierChanged += (o, n) => { oldTier = o; newTier = n; };

            // Win until crossing from bronze to silver (bronze max = 99, win trophies = 30)
            // 3 wins = 90 (still bronze), 4th win = 120 (silver)
            for (int i = 0; i < 4; i++)
                _arena.RecordWin();

            Assert.AreEqual("bronze", oldTier);
            Assert.AreEqual("silver", newTier);
        }

        [Test]
        public void TierChange_FiresEvent_OnLoss_WhenDroppingTier()
        {
            string oldTier = null;
            string newTier = null;

            // Get to silver first (4 wins = 120 trophies)
            for (int i = 0; i < 4; i++)
                _arena.RecordWin();

            Assert.AreEqual("silver", _arena.GetCurrentTierId());

            _arena.ArenaTierChanged += (o, n) => { oldTier = o; newTier = n; };

            // Lose until dropping back to bronze (silver loses 15 each)
            // 120 - 15 = 105 (silver), then advance day for more attempts
            _now = _now.AddDays(1);
            // 105 - 15 = 90 (bronze!)
            _arena.RecordLoss(); // 105
            _arena.RecordLoss(); // 90

            Assert.AreEqual("silver", oldTier);
            Assert.AreEqual("bronze", newTier);
        }

        // ── Defense Team ──

        [Test]
        public void SetDefenseTeam_GetDefenseTeam_RoundTrip()
        {
            var team = new List<int> { 101, 202, 303 };
            _arena.SetDefenseTeam(team);

            var result = _arena.GetDefenseTeam();

            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(101, result[0]);
            Assert.AreEqual(202, result[1]);
            Assert.AreEqual(303, result[2]);
        }

        [Test]
        public void SetDefenseTeam_IsCopied_NotReferenced()
        {
            var team = new List<int> { 1, 2, 3 };
            _arena.SetDefenseTeam(team);
            team.Add(4); // mutate original

            var result = _arena.GetDefenseTeam();
            Assert.AreEqual(3, result.Count);
        }

        // ── HighestTrophies ──

        [Test]
        public void HighestTrophies_TracksAllTimeHigh()
        {
            _arena.RecordWin(); // 30
            _arena.RecordWin(); // 60
            _arena.RecordWin(); // 90

            Assert.AreEqual(90, _arena.GetRecord().HighestTrophies);

            _now = _now.AddDays(1);
            _arena.RecordLoss(); // 80
            _arena.RecordLoss(); // 70

            Assert.AreEqual(90, _arena.GetRecord().HighestTrophies);
        }

        // ── Season Rewards ──

        [Test]
        public void ClaimSeasonRewards_ReturnsNull_WhenNoSeasonActive()
        {
            var rewards = _arena.ClaimSeasonRewards();

            Assert.IsNull(rewards);
        }

        [Test]
        public void ClaimSeasonRewards_ReturnsRewards_WhenSeasonEnded()
        {
            _arena.GetRecord().SeasonId = 1;

            var rewards = _arena.ClaimSeasonRewards();

            Assert.IsNotNull(rewards);
            Assert.AreEqual(1, rewards.Count);
            Assert.AreEqual("gems", rewards[0]["id"]);
        }

        // ── Persistence ──

        [Test]
        public void ToSaveDict_FromSaveDict_RoundTrip()
        {
            _arena.SetDefenseTeam(new List<int> { 10, 20, 30 });
            _arena.RecordWin();  // 30 trophies, 1 win
            _arena.RecordWin();  // 60 trophies, 2 wins
            _arena.RecordLoss(); // 50 trophies, 1 loss
            _arena.GetRecord().SeasonId = 3;

            var saved = _arena.ToSaveDict();

            // Create a fresh manager and restore
            var arena2 = new ArenaManager(() => _now, _def);
            arena2.FromSaveDict(saved);

            var record = arena2.GetRecord();
            Assert.AreEqual(50, record.Trophies);
            Assert.AreEqual(2, record.Wins);
            Assert.AreEqual(1, record.Losses);
            Assert.AreEqual(3, record.AttemptsToday);
            Assert.AreEqual(60, record.HighestTrophies);
            Assert.AreEqual(3, record.SeasonId);
            Assert.AreEqual(3, record.DefenseTeam.Count);
            Assert.AreEqual(10, record.DefenseTeam[0]);
            Assert.AreEqual(20, record.DefenseTeam[1]);
            Assert.AreEqual(30, record.DefenseTeam[2]);
        }

        [Test]
        public void FromSaveDict_HandlesNullGracefully()
        {
            _arena.FromSaveDict(null);

            Assert.AreEqual(0, _arena.GetRecord().Trophies);
        }

        [Test]
        public void FromSaveDict_HandlesEmptyDict()
        {
            _arena.FromSaveDict(new Dictionary<string, object>());

            Assert.AreEqual(0, _arena.GetRecord().Trophies);
            Assert.AreEqual(0, _arena.GetRecord().Wins);
        }

        // ── Events fire correctly ──

        [Test]
        public void Events_MultipleWins_FireEachTime()
        {
            int fireCount = 0;
            _arena.ArenaWin += _ => fireCount++;

            _arena.RecordWin();
            _arena.RecordWin();
            _arena.RecordWin();

            Assert.AreEqual(3, fireCount);
        }

        [Test]
        public void Events_MultipleLosses_FireEachTime()
        {
            int fireCount = 0;
            _arena.ArenaLoss += _ => fireCount++;

            _arena.RecordLoss();
            _arena.RecordLoss();

            Assert.AreEqual(2, fireCount);
        }

        // ── LoadDef ──

        [Test]
        public void LoadDef_ReplacesDef()
        {
            var newDef = new ArenaDef
            {
                AttemptsPerDay = 10,
                Tiers = new List<ArenaTierDef>
                {
                    new() { Id = "starter", Name = "Starter", MinTrophies = 0, MaxTrophies = -1, WinTrophies = 50, LoseTrophies = 5 },
                },
            };

            _arena.LoadDef(newDef);

            Assert.AreEqual(10, _arena.GetRemainingAttempts());
            int trophies = _arena.RecordWin();
            Assert.AreEqual(50, trophies);
        }

        // ── GetCurrentTierId ──

        [Test]
        public void GetCurrentTierId_ReturnsCorrectId()
        {
            Assert.AreEqual("bronze", _arena.GetCurrentTierId());

            for (int i = 0; i < 4; i++)
                _arena.RecordWin();

            Assert.AreEqual("silver", _arena.GetCurrentTierId());
        }
    }
}
