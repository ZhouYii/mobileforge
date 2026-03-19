using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Definition of a daily login reward for one day in the cycle.
    /// </summary>
    public class DailyRewardDef
    {
        public int Day { get; set; }          // 1-based day in cycle
        public string RewardType { get; set; } // "gems", "coins", "stamina", "item"
        public int Amount { get; set; }
        public int ItemId { get; set; }        // For item rewards
    }

    /// <summary>
    /// Milestone reward triggered at a specific cumulative login count.
    /// </summary>
    public class LoginMilestoneDef
    {
        public int RequiredDays { get; set; }
        public string RewardType { get; set; }
        public int Amount { get; set; }
    }

    /// <summary>
    /// Manages daily login tracking, streak counting, and reward distribution.
    /// Pure domain logic — uses a date provider for testability.
    ///
    /// ToS daily login pattern:
    /// - 7-day cycle with escalating rewards
    /// - Login streak tracking (resets if a day is missed)
    /// - Milestone rewards at cumulative thresholds (7, 14, 30, 60, 100 days)
    /// - One claim per calendar day
    /// </summary>
    public class DailyLoginTracker
    {
        private readonly Func<DateTime> _dateProvider;
        private List<DailyRewardDef> _cycleDefs = new();
        private List<LoginMilestoneDef> _milestoneDefs = new();

        // Persisted state
        private string _lastLoginDate = "";
        private string _lastClaimDate = "";
        private int _loginStreak;
        private int _totalLogins;
        private HashSet<int> _claimedMilestones = new();

        public int LoginStreak => _loginStreak;
        public int TotalLogins => _totalLogins;
        public int DayInCycle => _loginStreak > 0 ? ((_loginStreak - 1) % CycleLength) + 1 : 1;
        public int CycleLength => _cycleDefs.Count > 0 ? _cycleDefs.Count : 7;
        public bool ClaimedToday => _lastClaimDate == Today;

        private string Today => _dateProvider().ToString("yyyy-MM-dd");
        private string Yesterday => _dateProvider().AddDays(-1).ToString("yyyy-MM-dd");

        public DailyLoginTracker(Func<DateTime> dateProvider = null)
        {
            _dateProvider = dateProvider ?? (() => DateTime.Now);
        }

        /// <summary>
        /// Set reward definitions for the login cycle and milestones.
        /// </summary>
        public void SetRewardDefs(List<DailyRewardDef> cycleDefs, List<LoginMilestoneDef> milestoneDefs = null)
        {
            _cycleDefs = cycleDefs ?? new List<DailyRewardDef>();
            _milestoneDefs = milestoneDefs ?? new List<LoginMilestoneDef>();
        }

        /// <summary>
        /// Record a login. Updates streak and total count.
        /// Call once per app launch / session start.
        /// Returns true if this is the first login today.
        /// </summary>
        public bool RecordLogin()
        {
            string today = Today;
            if (_lastLoginDate == today)
                return false; // Already logged in today

            // Check if streak continues (logged in yesterday) or resets
            if (_lastLoginDate == Yesterday)
            {
                _loginStreak++;
            }
            else if (string.IsNullOrEmpty(_lastLoginDate))
            {
                _loginStreak = 1; // First ever login
            }
            else
            {
                _loginStreak = 1; // Streak broken
            }

            _totalLogins++;
            _lastLoginDate = today;
            return true;
        }

        /// <summary>
        /// Claim today's daily reward. Returns the reward, or null if already claimed.
        /// </summary>
        public DailyRewardDef ClaimDailyReward()
        {
            if (ClaimedToday) return null;
            if (_loginStreak <= 0) return null;

            _lastClaimDate = Today;

            // Find the reward for today's position in the cycle
            int dayIndex = DayInCycle - 1; // 0-based
            if (dayIndex < _cycleDefs.Count)
                return _cycleDefs[dayIndex];

            // Fallback: generic reward
            return new DailyRewardDef { Day = DayInCycle, RewardType = "coins", Amount = 1000 };
        }

        /// <summary>
        /// Get all milestone rewards that are newly claimable.
        /// </summary>
        public List<LoginMilestoneDef> GetClaimableMilestones()
        {
            var result = new List<LoginMilestoneDef>();
            foreach (var ms in _milestoneDefs)
            {
                if (_totalLogins >= ms.RequiredDays && !_claimedMilestones.Contains(ms.RequiredDays))
                    result.Add(ms);
            }
            return result;
        }

        /// <summary>
        /// Claim a milestone reward. Returns true if successfully claimed.
        /// </summary>
        public bool ClaimMilestone(int requiredDays)
        {
            if (_claimedMilestones.Contains(requiredDays)) return false;
            if (_totalLogins < requiredDays) return false;
            _claimedMilestones.Add(requiredDays);
            return true;
        }

        /// <summary>
        /// Get display info for all days in the current cycle.
        /// </summary>
        public List<DailyRewardDef> GetCycleRewards() => new(_cycleDefs);

        /// <summary>
        /// Distribute a daily reward to the economy.
        /// </summary>
        public static void DistributeReward(DailyRewardDef reward, Economy economy)
        {
            if (reward == null || economy == null) return;
            if (!string.IsNullOrEmpty(reward.RewardType) && reward.Amount > 0)
                economy.Earn(reward.RewardType, reward.Amount);
        }

        // ── Persistence ──

        public Dictionary<string, object> ToSaveDict()
        {
            return new Dictionary<string, object>
            {
                ["last_login_date"] = _lastLoginDate,
                ["last_claim_date"] = _lastClaimDate,
                ["login_streak"] = _loginStreak,
                ["total_logins"] = _totalLogins,
                ["claimed_milestones"] = new List<object>(_claimedMilestones.Count > 0
                    ? new List<int>(_claimedMilestones).ConvertAll(x => (object)x)
                    : new List<object>()),
            };
        }

        public void FromSaveDict(Dictionary<string, object> data)
        {
            if (data == null) return;
            _lastLoginDate = data.TryGetValue("last_login_date", out var lld) ? Convert.ToString(lld) : "";
            _lastClaimDate = data.TryGetValue("last_claim_date", out var lcd) ? Convert.ToString(lcd) : "";
            _loginStreak = data.TryGetValue("login_streak", out var ls) ? Convert.ToInt32(ls) : 0;
            _totalLogins = data.TryGetValue("total_logins", out var tl) ? Convert.ToInt32(tl) : 0;

            _claimedMilestones.Clear();
            if (data.TryGetValue("claimed_milestones", out var cmObj) && cmObj is List<object> cmList)
            {
                foreach (var item in cmList)
                    _claimedMilestones.Add(Convert.ToInt32(item));
            }
        }
    }
}
