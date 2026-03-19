using System;
using System.Collections.Generic;
using System.Linq;

namespace MobileForge.Domain
{
    /// <summary>
    /// Manages PvP arena battles, trophy progression, tier placement,
    /// daily attempt tracking, defense teams, and season rewards.
    /// Pure domain logic — uses a date provider for testability.
    /// </summary>
    public class ArenaManager
    {
        private readonly Func<DateTime> _dateProvider;
        private ArenaDef _def;
        private readonly ArenaRecord _record = new();

        /// <summary>Fired after a win. Payload: trophy count after win.</summary>
        public event Action<int> ArenaWin;

        /// <summary>Fired after a loss. Payload: trophy count after loss.</summary>
        public event Action<int> ArenaLoss;

        /// <summary>Fired when the player crosses a tier boundary. Payload: oldTierId, newTierId.</summary>
        public event Action<string, string> ArenaTierChanged;

        private string Today => _dateProvider().ToString("yyyy-MM-dd");

        public ArenaManager(Func<DateTime> dateProvider = null, ArenaDef def = null)
        {
            _dateProvider = dateProvider ?? (() => DateTime.Now);
            _def = def;
        }

        /// <summary>Load or replace the arena definition at runtime.</summary>
        public void LoadDef(ArenaDef def)
        {
            _def = def;
        }

        /// <summary>Set the player's defense team (monster instance IDs).</summary>
        public void SetDefenseTeam(List<int> monsterInstanceIds)
        {
            _record.DefenseTeam = new List<int>(monsterInstanceIds);
        }

        /// <summary>Get the player's current defense team.</summary>
        public List<int> GetDefenseTeam()
        {
            return new List<int>(_record.DefenseTeam);
        }

        /// <summary>Returns true if the player has attempts remaining today.</summary>
        public bool CanBattle()
        {
            CheckDailyReset();
            if (_def == null) return false;
            return _record.AttemptsToday < _def.AttemptsPerDay;
        }

        /// <summary>
        /// Record a win. Increments wins, adds trophies from current tier,
        /// updates highest trophies, checks for tier change, fires events.
        /// Returns the new trophy count.
        /// </summary>
        public int RecordWin()
        {
            CheckDailyReset();

            string oldTierId = GetCurrentTierId();
            var tier = GetCurrentTier();
            int trophiesGained = tier?.WinTrophies ?? 0;

            _record.Wins++;
            _record.AttemptsToday++;
            _record.Trophies += trophiesGained;

            if (_record.Trophies > _record.HighestTrophies)
                _record.HighestTrophies = _record.Trophies;

            string newTierId = GetCurrentTierId();
            if (oldTierId != newTierId)
                ArenaTierChanged?.Invoke(oldTierId, newTierId);

            ArenaWin?.Invoke(_record.Trophies);
            return _record.Trophies;
        }

        /// <summary>
        /// Record a loss. Increments losses, subtracts trophies (floor at 0),
        /// checks for tier change, fires events.
        /// Returns the new trophy count.
        /// </summary>
        public int RecordLoss()
        {
            CheckDailyReset();

            string oldTierId = GetCurrentTierId();
            var tier = GetCurrentTier();
            int trophiesLost = tier?.LoseTrophies ?? 0;

            _record.Losses++;
            _record.AttemptsToday++;
            _record.Trophies = Math.Max(0, _record.Trophies - trophiesLost);

            string newTierId = GetCurrentTierId();
            if (oldTierId != newTierId)
                ArenaTierChanged?.Invoke(oldTierId, newTierId);

            ArenaLoss?.Invoke(_record.Trophies);
            return _record.Trophies;
        }

        /// <summary>
        /// Returns the ArenaTierDef matching the current trophy count.
        /// Optionally accepts an override def.
        /// </summary>
        public ArenaTierDef GetCurrentTier(ArenaDef def = null)
        {
            var d = def ?? _def;
            if (d == null || d.Tiers.Count == 0) return null;

            ArenaTierDef matched = d.Tiers[0];
            foreach (var t in d.Tiers)
            {
                if (_record.Trophies >= t.MinTrophies &&
                    (t.MaxTrophies == -1 || _record.Trophies <= t.MaxTrophies))
                {
                    matched = t;
                }
            }
            return matched;
        }

        /// <summary>Returns the current tier's ID string.</summary>
        public string GetCurrentTierId()
        {
            var tier = GetCurrentTier();
            return tier?.Id ?? "";
        }

        /// <summary>Resets daily attempts and updates the last reset date.</summary>
        public void DailyReset()
        {
            _record.AttemptsToday = 0;
            _record.LastResetDate = Today;
        }

        /// <summary>If today's date differs from LastResetDate, performs a daily reset.</summary>
        public void CheckDailyReset()
        {
            if (Today != _record.LastResetDate)
                DailyReset();
        }

        /// <summary>Returns the current ArenaRecord.</summary>
        public ArenaRecord GetRecord()
        {
            return _record;
        }

        /// <summary>Returns the number of remaining attempts today.</summary>
        public int GetRemainingAttempts()
        {
            CheckDailyReset();
            if (_def == null) return 0;
            return Math.Max(0, _def.AttemptsPerDay - _record.AttemptsToday);
        }

        /// <summary>
        /// Returns season rewards if the season has ended (based on SeasonDurationDays),
        /// or null if the season is still active.
        /// </summary>
        public List<Dictionary<string, object>> ClaimSeasonRewards()
        {
            if (_def == null) return null;
            if (_def.SeasonRewards == null || _def.SeasonRewards.Count == 0) return null;
            // Season is considered ended when SeasonDurationDays have elapsed since season start.
            // For simplicity, we check if SeasonId > 0 (season has been started) and return rewards.
            // In a real implementation, a season-start timestamp would be tracked.
            if (_record.SeasonId <= 0) return null;
            return new List<Dictionary<string, object>>(_def.SeasonRewards);
        }

        // ── Persistence ──

        public Dictionary<string, object> ToSaveDict()
        {
            return new Dictionary<string, object>
            {
                ["trophies"] = _record.Trophies,
                ["wins"] = _record.Wins,
                ["losses"] = _record.Losses,
                ["attempts_today"] = _record.AttemptsToday,
                ["highest_trophies"] = _record.HighestTrophies,
                ["last_reset_date"] = _record.LastResetDate,
                ["season_id"] = _record.SeasonId,
                ["defense_team"] = new List<object>(_record.DefenseTeam.ConvertAll(x => (object)x)),
            };
        }

        public void FromSaveDict(Dictionary<string, object> data)
        {
            if (data == null) return;
            _record.Trophies = data.TryGetValue("trophies", out var t) ? Convert.ToInt32(t) : 0;
            _record.Wins = data.TryGetValue("wins", out var w) ? Convert.ToInt32(w) : 0;
            _record.Losses = data.TryGetValue("losses", out var l) ? Convert.ToInt32(l) : 0;
            _record.AttemptsToday = data.TryGetValue("attempts_today", out var at) ? Convert.ToInt32(at) : 0;
            _record.HighestTrophies = data.TryGetValue("highest_trophies", out var ht) ? Convert.ToInt32(ht) : 0;
            _record.LastResetDate = data.TryGetValue("last_reset_date", out var lrd) ? Convert.ToString(lrd) : "";
            _record.SeasonId = data.TryGetValue("season_id", out var sid) ? Convert.ToInt32(sid) : 0;

            _record.DefenseTeam.Clear();
            if (data.TryGetValue("defense_team", out var dtObj) && dtObj is List<object> dtList)
            {
                foreach (var item in dtList)
                    _record.DefenseTeam.Add(Convert.ToInt32(item));
            }
        }
    }
}
