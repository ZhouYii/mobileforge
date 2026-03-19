using System.Collections.Generic;
using UnityEngine;

namespace TowerOfSaviors
{
    /// <summary>
    /// ToS visual constants — mirrors tos_theme.gd.
    /// Centralizes element colors, animation timings, card states, layout zones,
    /// and helper methods for consistent styling across all game UI.
    /// </summary>
    public static class TosTheme
    {
        // ── Element Colors (from Element.cs) ──

        public static readonly Dictionary<int, Color> ElementColors = new()
        {
            { 0, new Color(0.5f, 0.5f, 0.5f) },        // None - grey
            { 1, new Color(0.251f, 1.0f, 1.0f) },       // Water - cyan
            { 2, new Color(1.0f, 0.251f, 0.251f) },     // Fire - red
            { 3, new Color(0.251f, 1.0f, 0.243f) },     // Earth - green
            { 4, new Color(1.0f, 1.0f, 0.251f) },       // Light - yellow
            { 5, new Color(1.0f, 0.251f, 1.0f) },       // Dark - magenta
            { 6, new Color(1.0f, 0.576f, 0.749f) },     // Heart - pink
            { 7, new Color(0.4f, 0.4f, 0.4f) },         // Jammer - dark grey
            { 8, new Color(0.9f, 0.1f, 0.1f) },         // Bomb - bright red
            { 9, new Color(0.3f, 0.0f, 0.4f) },         // Poison - dark purple
        };

        public static readonly Dictionary<int, string> ElementNames = new()
        {
            { 0, "None" }, { 1, "Water" }, { 2, "Fire" }, { 3, "Earth" },
            { 4, "Light" }, { 5, "Dark" }, { 6, "Heart" },
            { 7, "Jammer" }, { 8, "Bomb" }, { 9, "Poison" },
        };

        public static readonly Dictionary<int, string> ElementIcons = new()
        {
            { 0, "\u25cf" },  // ●
            { 1, "\u2248" },  // ≈ water waves
            { 2, "\u2668" },  // ♨ fire
            { 3, "\u2618" },  // ☘ earth
            { 4, "\u2600" },  // ☀ light
            { 5, "\u263d" },  // ☽ dark
            { 6, "\u2665" },  // ♥ heart
            { 7, "\u2716" },  // ✖ jammer
            { 8, "\u25c6" },  // ◆ bomb
            { 9, "\u2620" },  // ☠ poison
        };

        // ── Card States (from DataCardIcon.cs) ──

        public const float CardAlphaDisabled = 0.6f;
        public const float CardAlphaLocked = 0.55f;
        public static readonly Color CardFrameLocked = new(0.282f, 0.282f, 0.282f);
        public const float CardPopScale = 1.12f;

        // ── Animation Timings (from various source files) ──

        public const float AnimPanel = 0.5f;           // Top bar entrance
        public const float AnimHpBar = 0.4f;           // GamePlayBar default
        public const float AnimEnemyEnter = 1.0f;      // Enemy come/leave
        public const float AnimCardShine = 0.3f;       // Border shimmer
        public const float AnimCardPop = 0.3f;         // 1.0→1.12→1.0 with easeInOutBack
        public const float AnimGemMatch = 0.33f;       // spitAnimate duration
        public const float AnimGemEffect = 0.4f;       // White/yellow ball
        public const float AnimDamage = 0.46f;         // playerDamageTextJumpTime
        public const float AnimCombo = 0.3f;           // Combo particle
        public const float AnimTransition = 0.25f;     // Screen transition (ContainerObject)

        // ── Gem Drag (from FollowMouse.cs) ──

        public const float GemDragScale = 1.06f;
        public const float GemDragAlpha = 0.65f;

        // ── Board Layout ──

        public const float GemCellSize = 62f;
        public const float GemSpacing = 3f;
        public const int BoardCornerRadius = 6;

        // ── Layout Zones (from puzzle system) ──

        public const float ZoneEnemyHeight = 0.295f;   // 29.5% from top
        public const float ZonePuzzleStart = 0.44444f;  // 44.4% from top

        // ── Skill Button ──

        public static readonly Vector2 SkillBtnSize = new(80, 44);
        public const int SkillBtnFontSize = 12;
        public const float SkillActiveScale = 1.2f;
        public const float SkillInactiveScale = 1.0f;

        // ── HP Bar ──

        public const float HpBarHeight = 24f;

        // ── Combo Color Ramp ──

        private static readonly Dictionary<int, Color> ComboColors = new()
        {
            { 1, Color.white },
            { 3, new Color(1.0f, 1.0f, 0.3f) },      // Yellow
            { 5, new Color(1.0f, 0.6f, 0.1f) },       // Orange
            { 7, new Color(1.0f, 0.85f, 0.0f) },      // Gold
        };

        // ── Rarity ──

        public static readonly Dictionary<int, Color> RarityColors = new()
        {
            { 1, new Color(0.7f, 0.7f, 0.7f) },      // Grey
            { 2, new Color(0.6f, 0.8f, 0.6f) },       // Green-grey
            { 3, new Color(0.4f, 0.7f, 1.0f) },       // Blue
            { 4, new Color(0.7f, 0.5f, 1.0f) },       // Purple
            { 5, new Color(1.0f, 0.7f, 0.1f) },       // Gold-orange
            { 6, new Color(1.0f, 0.85f, 0.0f) },      // Gold
            { 7, new Color(1.0f, 0.3f, 0.3f) },       // Red (legendary)
        };

        // ── Difficulty Colors ──

        public static readonly Dictionary<string, Color> DifficultyColors = new()
        {
            { "normal", new Color(0.6f, 0.6f, 0.6f) },
            { "expert", new Color(0.3f, 0.5f, 1.0f) },
            { "mythical", new Color(1.0f, 0.6f, 0.1f) },
            { "annihilation", new Color(1.0f, 0.2f, 0.2f) },
        };

        // ── Panel / Background Colors ──

        public static readonly Color BgDark = new(0.05f, 0.05f, 0.08f);
        public static readonly Color BgPanel = new(0.1f, 0.1f, 0.14f, 0.92f);
        public static readonly Color BgPanelAccent = new(0.3f, 0.3f, 0.35f);
        public static readonly Color TextGold = new(1.0f, 0.85f, 0.0f);
        public static readonly Color TextMuted = new(0.6f, 0.6f, 0.6f);

        // ── Font Sizes ──

        public const int FontTitle = 36;
        public const int FontHeader = 24;
        public const int FontSubheader = 18;
        public const int FontBody = 14;
        public const int FontSmall = 12;
        public const int FontTiny = 10;

        // ── Card Sizes ──

        public static readonly Vector2 CardSize = new(72, 96);
        public static readonly Vector2 EnemyViewSize = new(130, 140);

        // ── Button Sizes ──

        public static readonly Vector2 BtnLarge = new(220, 50);
        public static readonly Vector2 BtnMedium = new(160, 48);
        public static readonly Vector2 BtnSmall = new(80, 44);
        public static readonly Vector2 BtnTab = new(100, 34);

        // ── Standardized Layout (matching original TOS MenuLayer) ──

        public const float HeaderHeight = 50f;          // MenuTitleBar height
        public const float BattleHeaderHeight = 40f;     // Compact battle HUD header
        public const float BackBtnWidth = 70f;           // Back button preferred width
        public const float BackBtnHeight = 36f;          // Back button preferred height
        public const int BackBtnFontSize = 14;
        public static readonly Color BackBtnBg = new(0.18f, 0.18f, 0.24f, 0.95f);
        public static readonly Color BackBtnText = new(0.8f, 0.8f, 0.85f);
        public static readonly Color HeaderBg = new(0.08f, 0.08f, 0.12f, 0.96f);
        public const float ContentPadH = 8f;             // Horizontal content padding
        public const float ContentTopOffset = 56f;       // Below header (header + gap)
        public const float ContentBottomNav = 62f;       // Above nav bar (nav + gap)

        // ── Navigation Bar ──

        public const float NavBarHeight = 56f;           // Bottom nav bar height
        public static readonly Color NavBarBg = new(0.06f, 0.06f, 0.1f, 0.96f);
        public static readonly Color NavBarActive = TextGold;
        public static readonly Color NavBarInactive = new(0.45f, 0.45f, 0.5f);

        // ── Stagger Timing ──

        public const float StaggerButton = 0.08f;      // Per-button delay
        public const float StaggerGrid = 0.03f;         // Per-grid-item delay

        // ── Social / Settings Colors ──

        public static readonly Color SocialAccent = new(0.4f, 0.8f, 1f);
        public static readonly Color SettingsAccent = new(0.7f, 0.7f, 0.8f);
        public static readonly Color DailyCheckInAccent = TextGold;
        public static readonly Color MailAccent = new(0.4f, 0.7f, 1f);
        public static readonly Color TeamAccent = new(0.3f, 0.8f, 0.6f);
        public static readonly Color BadgeRed = new(0.9f, 0.2f, 0.2f);

        // ── Gem Status Icons ──

        public static readonly Dictionary<int, string> GemStatusIcons = new()
        {
            { 1, "\u2744" },     // ❄ Frozen
            { 2, "\U0001f512" }, // 🔒 Locked
            { 3, "\u2620" },     // ☠ Poison
        };

        // ── Lookup Functions ──

        public static Color ElementColor(int element)
            => ElementColors.TryGetValue(element, out var c) ? c : new Color(0.5f, 0.5f, 0.5f);

        public static string ElementName(int element)
            => ElementNames.TryGetValue(element, out var n) ? n : "None";

        public static string ElementIcon(int element)
            => ElementIcons.TryGetValue(element, out var i) ? i : "\u25cf";

        public static Color RarityColor(int rarity)
            => RarityColors.TryGetValue(rarity, out var c) ? c : new Color(0.7f, 0.7f, 0.7f);

        public static string RarityStars(int rarity)
            => new string('\u2605', Mathf.Clamp(rarity, 0, 7));

        public static Color ComboColor(int comboCount)
        {
            if (comboCount >= 7) return ComboColors[7];
            if (comboCount >= 5) return ComboColors[5];
            if (comboCount >= 3) return ComboColors[3];
            return ComboColors[1];
        }

        public static Color DifficultyColor(string difficulty)
            => DifficultyColors.TryGetValue(difficulty?.ToLower() ?? "", out var c) ? c : new Color(0.6f, 0.6f, 0.6f);

        /// <summary>
        /// HP bar color based on remaining ratio: green > 50%, yellow > 20%, red below.
        /// </summary>
        public static Color HpBarColor(float ratio)
        {
            if (ratio > 0.5f) return new Color(0.2f, 0.8f, 0.2f);
            if (ratio > 0.2f) return new Color(0.9f, 0.9f, 0.1f);
            return new Color(0.9f, 0.2f, 0.2f);
        }

        /// <summary>
        /// Element-tinted background color for panels (12% element color).
        /// </summary>
        public static Color ElementPanelBg(int element)
        {
            var ec = ElementColor(element);
            return new Color(ec.r * 0.12f, ec.g * 0.12f, ec.b * 0.12f, 0.92f);
        }

        /// <summary>
        /// Element-tinted button color (25% element color).
        /// </summary>
        public static Color ElementButtonBg(int element)
        {
            var ec = ElementColor(element);
            return new Color(ec.r * 0.25f, ec.g * 0.25f, ec.b * 0.25f, 0.9f);
        }
    }
}
