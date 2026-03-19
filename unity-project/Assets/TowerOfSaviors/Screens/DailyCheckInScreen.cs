using System;
using System.Collections.Generic;
using MobileForge.Presentation;
using MobileForge.Infrastructure;

namespace TowerOfSaviors
{
    /// <summary>
    /// Daily check-in screen matching original Social_DailyCheckIn_V13_View.
    /// Calendar grid of 7-day rewards with streak tracking and cumulative bonuses.
    /// </summary>
    public class DailyCheckInScreen : IScreen
    {
        private UIRouter _router;
        private PlayerState _playerState;

        public int LoginStreak { get; private set; }
        public int DayInCycle { get; private set; }
        public bool ClaimedToday { get; private set; }
        public List<DayReward> Rewards { get; } = new();

        public class DayReward
        {
            public int Day { get; set; }
            public string RewardType { get; set; }
            public int Amount { get; set; }
            public bool Claimed { get; set; }
            public bool IsToday { get; set; }
        }

        // Cumulative milestone rewards
        public List<MilestoneReward> Milestones { get; } = new();

        public class MilestoneReward
        {
            public int RequiredDays { get; set; }
            public string RewardType { get; set; }
            public int Amount { get; set; }
            public bool Claimed { get; set; }
        }

        public DailyCheckInScreen() { }

        public void Setup(UIRouter router, PlayerState playerState)
        {
            _router = router;
            _playerState = playerState;

            // Load streak data
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            string lastLogin = _playerState?.GetValue("progress", "last_login_date", "")?.ToString() ?? "";
            LoginStreak = Convert.ToInt32(_playerState?.GetValue("progress", "login_streak", 0) ?? 0);
            ClaimedToday = lastLogin == today;
            DayInCycle = ((LoginStreak - 1) % 7) + 1;
            if (DayInCycle < 1) DayInCycle = 1;

            // Build 7-day reward cycle
            var rewardTable = new[]
            {
                ("gems", 1), ("coins", 1000), ("gems", 1),
                ("coins", 2000), ("gems", 2), ("coins", 3000), ("gems", 5)
            };

            for (int i = 0; i < 7; i++)
            {
                bool isClaimed = ClaimedToday ? (i + 1) <= DayInCycle : (i + 1) < DayInCycle;
                Rewards.Add(new DayReward
                {
                    Day = i + 1,
                    RewardType = rewardTable[i].Item1,
                    Amount = rewardTable[i].Item2,
                    Claimed = isClaimed,
                    IsToday = (i + 1) == DayInCycle && !ClaimedToday
                });
            }

            // Cumulative milestones
            Milestones.Add(new MilestoneReward { RequiredDays = 7, RewardType = "gems", Amount = 5, Claimed = LoginStreak >= 7 && ClaimedToday });
            Milestones.Add(new MilestoneReward { RequiredDays = 14, RewardType = "gems", Amount = 10, Claimed = LoginStreak >= 14 && ClaimedToday });
            Milestones.Add(new MilestoneReward { RequiredDays = 30, RewardType = "gems", Amount = 20, Claimed = LoginStreak >= 30 && ClaimedToday });
        }

        /// <summary>Claim today's reward. Returns the reward description.</summary>
        public string ClaimToday()
        {
            if (ClaimedToday) return "Already claimed today!";

            string today = DateTime.Now.ToString("yyyy-MM-dd");
            LoginStreak++;
            _playerState?.SetValue("progress", "last_login_date", today);
            _playerState?.SetValue("progress", "login_streak", LoginStreak);
            DayInCycle = ((LoginStreak - 1) % 7) + 1;

            // Grant reward
            var reward = Rewards.Find(r => r.IsToday);
            if (reward != null)
            {
                reward.Claimed = true;
                reward.IsToday = false;
                int current = Convert.ToInt32(
                    _playerState?.GetValue("currencies", reward.RewardType, 0) ?? 0);
                _playerState?.SetValue("currencies", reward.RewardType, current + reward.Amount);
                ClaimedToday = true;
                return $"+{reward.Amount} {reward.RewardType}!";
            }

            ClaimedToday = true;
            return "Reward claimed!";
        }

        public void OnEnter(Dictionary<string, object> parameters) { }
        public void OnPause() { }
        public void OnResume() { }
        public void OnExit() { }

        public void GoBack()
        {
            _router?.Pop();
        }
    }
}
