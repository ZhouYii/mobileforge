using System;
using System.Collections.Generic;
using MobileForge.Presentation;
using MobileForge.Infrastructure;

namespace TowerOfSaviors
{
    /// <summary>
    /// ToS Title/Home Screen. Main landing page matching original Worldmap_WorldMap_View.
    /// Shows player info panel, quick-access feature buttons, and announcements.
    /// The bottom navigation bar (MainNavBar) handles tab-based navigation externally.
    /// </summary>
    public class TitleScreen : IScreen
    {
        private readonly UIRouter _router;

        public string Title => "Tower of Saviors";

        // Player info for the home card
        public string PlayerName { get; private set; } = "Summoner";
        public int PlayerRank { get; private set; } = 1;
        public int CurrentStamina { get; private set; } = 100;
        public int MaxStamina { get; private set; } = 100;
        public int Gems { get; private set; } = 50;
        public int Coins { get; private set; } = 10000;

        /// <summary>Quick-access feature buttons for the home screen center area.</summary>
        public List<FeatureButton> Features { get; } = new();

        /// <summary>Announcements to show on home screen.</summary>
        public List<string> Announcements { get; } = new();

        public class FeatureButton
        {
            public string Label { get; set; }
            public string Icon { get; set; }
            public string ScreenId { get; set; }
            public UnityEngine.Color AccentColor { get; set; }
        }

        public TitleScreen(UIRouter router)
        {
            _router = router;

            // Load player data
            var ps = PlayerState.Instance;
            PlayerName = ps?.GetValue("progress", "player_name", "Summoner")?.ToString() ?? "Summoner";
            PlayerRank = Convert.ToInt32(ps?.GetValue("progress", "rank", 1) ?? 1);
            CurrentStamina = Convert.ToInt32(ps?.GetValue("currencies", "stamina", 100) ?? 100);
            Gems = Convert.ToInt32(ps?.GetValue("currencies", "gems", 50) ?? 50);
            Coins = Convert.ToInt32(ps?.GetValue("currencies", "coins", 10000) ?? 10000);

            // Quick-access feature grid (matching original main menu shortcuts)
            Features.Add(new FeatureButton
            {
                Label = "Quest", Icon = "\u2694", ScreenId = "world_map",
                AccentColor = TosTheme.ElementColors[1]
            });
            Features.Add(new FeatureButton
            {
                Label = "Gacha", Icon = "\u2605", ScreenId = "gacha",
                AccentColor = TosTheme.TextGold
            });
            Features.Add(new FeatureButton
            {
                Label = "Team", Icon = "\u2638", ScreenId = "team_manage",
                AccentColor = TosTheme.ElementColors[3]
            });
            Features.Add(new FeatureButton
            {
                Label = "Box", Icon = "\u2726", ScreenId = "monster_box",
                AccentColor = TosTheme.ElementColors[4]
            });
            Features.Add(new FeatureButton
            {
                Label = "Shop", Icon = "\u2302", ScreenId = "shop",
                AccentColor = new UnityEngine.Color(0.7f, 0.5f, 1f)
            });
            Features.Add(new FeatureButton
            {
                Label = "Social", Icon = "\u263A", ScreenId = "social",
                AccentColor = new UnityEngine.Color(0.4f, 0.8f, 1f)
            });

            // Sample announcements
            Announcements.Add("Version 1.0 — Welcome to Tower of Saviors!");
            Announcements.Add("New event dungeon available: Forbidden Tower");
            Announcements.Add("Gacha: Limited-time Water Dragon banner!");
        }

        public void OnEnter(Dictionary<string, object> parameters)
        {
            // Refresh currency values on re-entry
            var ps = PlayerState.Instance;
            CurrentStamina = Convert.ToInt32(ps?.GetValue("currencies", "stamina", 100) ?? 100);
            Gems = Convert.ToInt32(ps?.GetValue("currencies", "gems", 50) ?? 50);
            Coins = Convert.ToInt32(ps?.GetValue("currencies", "coins", 10000) ?? 10000);
        }

        public void OnPause() { }
        public void OnResume() { }
        public void OnExit() { }

        /// <summary>Navigate to a feature screen.</summary>
        public void OnNavSelected(string screenId)
        {
            _router.Navigate(screenId);
        }

        /// <summary>Convenience: start the dungeon select flow.</summary>
        public void OnStartPressed()
        {
            _router.Navigate("dungeon_select");
        }
    }
}
