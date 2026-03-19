using System.Collections.Generic;
using MobileForge.Presentation;
using MobileForge.Infrastructure;

namespace TowerOfSaviors
{
    /// <summary>
    /// Settings/Preferences screen matching original Preferences_MainMenu_View.
    /// Provides options for sound, BGM, display, account management.
    /// </summary>
    public class SettingsScreen : IScreen
    {
        private UIRouter _router;
        private PlayerState _playerState;

        public bool SoundEnabled { get; set; } = true;
        public bool BgmEnabled { get; set; } = true;
        public bool NotificationsEnabled { get; set; } = true;
        public bool ScreenShakeEnabled { get; set; } = true;
        public string CurrentLanguage { get; set; } = "English";
        public string GameVersion => "1.0.0";
        public string PlayerName { get; private set; } = "Summoner";

        public List<SettingsEntry> Entries { get; } = new();

        public class SettingsEntry
        {
            public string Label { get; set; }
            public string Category { get; set; }
            public EntryType Type { get; set; }
            public bool ToggleValue { get; set; }
            public System.Action OnTap { get; set; }
            public System.Action<bool> OnToggle { get; set; }
        }

        public enum EntryType { Navigation, Toggle, Info }

        public SettingsScreen() { }

        public void Setup(UIRouter router, PlayerState playerState)
        {
            _router = router;
            _playerState = playerState;

            // Load saved preferences
            SoundEnabled = System.Convert.ToBoolean(
                _playerState?.GetValue("settings", "sound_enabled", true) ?? true);
            BgmEnabled = System.Convert.ToBoolean(
                _playerState?.GetValue("settings", "bgm_enabled", true) ?? true);
            NotificationsEnabled = System.Convert.ToBoolean(
                _playerState?.GetValue("settings", "notifications", true) ?? true);
            ScreenShakeEnabled = System.Convert.ToBoolean(
                _playerState?.GetValue("settings", "screen_shake", true) ?? true);
            PlayerName = _playerState?.GetValue("progress", "player_name", "Summoner")?.ToString() ?? "Summoner";

            BuildEntries();
        }

        private void BuildEntries()
        {
            Entries.Clear();

            // Sound section
            Entries.Add(new SettingsEntry
            {
                Label = "Sound Effects",
                Category = "Audio",
                Type = EntryType.Toggle,
                ToggleValue = SoundEnabled,
                OnToggle = v => { SoundEnabled = v; _playerState?.SetValue("settings", "sound_enabled", v); }
            });
            Entries.Add(new SettingsEntry
            {
                Label = "Background Music",
                Category = "Audio",
                Type = EntryType.Toggle,
                ToggleValue = BgmEnabled,
                OnToggle = v => { BgmEnabled = v; _playerState?.SetValue("settings", "bgm_enabled", v); }
            });

            // Display section
            Entries.Add(new SettingsEntry
            {
                Label = "Screen Shake",
                Category = "Display",
                Type = EntryType.Toggle,
                ToggleValue = ScreenShakeEnabled,
                OnToggle = v => { ScreenShakeEnabled = v; _playerState?.SetValue("settings", "screen_shake", v); }
            });
            Entries.Add(new SettingsEntry
            {
                Label = "Push Notifications",
                Category = "Notifications",
                Type = EntryType.Toggle,
                ToggleValue = NotificationsEnabled,
                OnToggle = v => { NotificationsEnabled = v; _playerState?.SetValue("settings", "notifications", v); }
            });

            // Account section
            Entries.Add(new SettingsEntry
            {
                Label = "Change Name",
                Category = "Account",
                Type = EntryType.Navigation,
                OnTap = () => { /* handled by page */ }
            });
            Entries.Add(new SettingsEntry
            {
                Label = "Game Manual",
                Category = "Info",
                Type = EntryType.Navigation,
                OnTap = () => ShowInfoPopup("Game Manual",
                    "Tower of Saviors is a puzzle RPG.\n\n" +
                    "Match 3+ gems of the same element to attack.\n" +
                    "Heart gems heal your team.\n" +
                    "Build combos for massive damage multipliers!\n\n" +
                    "Team building:\n" +
                    "- Leader skills provide multipliers\n" +
                    "- Match your team elements to deal more damage\n" +
                    "- Higher combos = higher damage")
            });
            Entries.Add(new SettingsEntry
            {
                Label = "Credits",
                Category = "Info",
                Type = EntryType.Navigation,
                OnTap = () => ShowInfoPopup("Credits",
                    "Tower of Saviors\nRe-implementation by MobileForge\n\n" +
                    "Original game by Mad Head Limited\n\n" +
                    "Framework: MobileForge v1.0\n" +
                    "Engine: Unity 6")
            });
            Entries.Add(new SettingsEntry
            {
                Label = "Privacy Policy",
                Category = "Info",
                Type = EntryType.Navigation,
                OnTap = () => ShowInfoPopup("Privacy Policy",
                    "This is a development build.\n\n" +
                    "No personal data is collected.\n" +
                    "All game data is stored locally on your device.")
            });
            Entries.Add(new SettingsEntry
            {
                Label = "Contact Support",
                Category = "Info",
                Type = EntryType.Navigation,
                OnTap = () => ShowInfoPopup("Contact Support",
                    "For support, please contact:\n\n" +
                    "Email: support@mobileforge.dev\n\n" +
                    "This is a development build.")
            });

            // Version info
            Entries.Add(new SettingsEntry
            {
                Label = $"Version {GameVersion}",
                Category = "Info",
                Type = EntryType.Info
            });
        }

        public void ChangeName(string newName)
        {
            if (!string.IsNullOrWhiteSpace(newName))
            {
                PlayerName = newName;
                _playerState?.SetValue("progress", "player_name", newName);
            }
        }

        /// <summary>
        /// Event raised when an info popup should be shown. UI layer subscribes to this.
        /// Parameters: (title, message)
        /// </summary>
        public event System.Action<string, string> OnShowInfo;

        private void ShowInfoPopup(string title, string message)
        {
            OnShowInfo?.Invoke(title, message);
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
