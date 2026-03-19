using System.Collections.Generic;
using MobileForge.Presentation;
using MobileForge.Infrastructure;

namespace TowerOfSaviors
{
    /// <summary>
    /// Social hub screen matching original Social_Menu_View.
    /// Provides navigation to Friends, Mail, Daily Check-in, Achievements, Rankings.
    /// </summary>
    public class SocialScreen : IScreen
    {
        private UIRouter _router;
        private PlayerState _playerState;

        public List<SocialMenuEntry> MenuEntries { get; } = new();

        public class SocialMenuEntry
        {
            public string Label { get; set; }
            public string ScreenId { get; set; }
            public string Icon { get; set; }
            public int BadgeCount { get; set; }
        }

        public SocialScreen() { }

        public void Setup(UIRouter router, PlayerState playerState)
        {
            _router = router;
            _playerState = playerState;

            int unreadMail = System.Convert.ToInt32(
                _playerState?.GetValue("social", "unread_mail", 3) ?? 3);
            bool checkedInToday = (_playerState?.GetValue("progress", "last_login_date", "")?.ToString() ?? "")
                == System.DateTime.Now.ToString("yyyy-MM-dd");
            int friendRequests = System.Convert.ToInt32(
                _playerState?.GetValue("social", "friend_requests", 2) ?? 2);

            MenuEntries.Add(new SocialMenuEntry
            {
                Label = "Friends",
                ScreenId = "friends",
                Icon = "\u263A", // smiley
                BadgeCount = friendRequests
            });
            MenuEntries.Add(new SocialMenuEntry
            {
                Label = "Mail",
                ScreenId = "mail",
                Icon = "\u2709", // envelope
                BadgeCount = unreadMail
            });
            MenuEntries.Add(new SocialMenuEntry
            {
                Label = "Daily Check-In",
                ScreenId = "daily_checkin",
                Icon = "\u2713", // checkmark
                BadgeCount = checkedInToday ? 0 : 1
            });
            MenuEntries.Add(new SocialMenuEntry
            {
                Label = "Achievements",
                ScreenId = "achievements",
                Icon = "\u2605", // star
                BadgeCount = 0
            });
            MenuEntries.Add(new SocialMenuEntry
            {
                Label = "Rankings",
                ScreenId = "rankings",
                Icon = "\u265B", // chess queen
                BadgeCount = 0
            });
            MenuEntries.Add(new SocialMenuEntry
            {
                Label = "ID & Profile",
                ScreenId = "profile",
                Icon = "\u2302", // house
                BadgeCount = 0
            });
        }

        public string PlayerId => _playerState?.GetValue("progress", "player_id", "000000")?.ToString() ?? "000000";
        public string PlayerName => _playerState?.GetValue("progress", "player_name", "Summoner")?.ToString() ?? "Summoner";
        public int PlayerRank => System.Convert.ToInt32(_playerState?.GetValue("progress", "rank", 1) ?? 1);

        public void OnEnter(Dictionary<string, object> parameters) { }
        public void OnPause() { }
        public void OnResume() { }
        public void OnExit() { }

        public void Navigate(string screenId)
        {
            _router?.Push(screenId);
        }

        public void GoBack()
        {
            _router?.Pop();
        }
    }
}
