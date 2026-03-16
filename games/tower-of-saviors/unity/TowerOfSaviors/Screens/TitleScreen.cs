using System.Collections.Generic;
using MobileForge.Presentation;

namespace TowerOfSaviors
{
    /// <summary>
    /// ToS Title Screen. Main menu with navigation to all game screens.
    /// Mirrors title_screen.gd — offers Dungeon Select, Gacha, Monster Box, Shop.
    /// </summary>
    public class TitleScreen : IScreen
    {
        private readonly UIRouter _router;

        public string Title => "Tower of Saviors";

        /// <summary>
        /// Navigation entries exposed for the UI layer to render as buttons.
        /// </summary>
        public List<NavEntry> NavEntries { get; } = new();

        public class NavEntry
        {
            public string Label { get; set; }
            public string ScreenId { get; set; }
        }

        public TitleScreen(UIRouter router)
        {
            _router = router;

            NavEntries.Add(new NavEntry { Label = "Dungeon Select", ScreenId = "dungeon_select" });
            NavEntries.Add(new NavEntry { Label = "Gacha", ScreenId = "gacha" });
            NavEntries.Add(new NavEntry { Label = "Monster Box", ScreenId = "monster_box" });
            NavEntries.Add(new NavEntry { Label = "Shop", ScreenId = "shop" });
        }

        public void OnEnter(Dictionary<string, object> parameters)
        {
            // Title screen is ready — UI layer renders Title and NavEntries
        }

        public void OnPause() { }
        public void OnResume() { }
        public void OnExit() { }

        /// <summary>
        /// Called when the player presses a navigation button.
        /// Pushes the target screen onto the stack so Back returns here.
        /// </summary>
        public void OnNavSelected(string screenId)
        {
            _router.Push(screenId);
        }

        /// <summary>
        /// Convenience for the original "Start Game" flow (dungeon select).
        /// </summary>
        public void OnStartPressed()
        {
            _router.Push("dungeon_select");
        }
    }
}
