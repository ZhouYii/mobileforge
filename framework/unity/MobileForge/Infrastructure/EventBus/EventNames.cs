namespace MobileForge.Infrastructure
{
    /// <summary>
    /// String constants for all framework events.
    /// Game-specific events should be defined in game code.
    /// </summary>
    public static class EventNames
    {
        // Infrastructure
        public const string StateLoaded = "state_loaded";
        public const string StateChanged = "state_changed";
        public const string SaveCompleted = "save_completed";
        public const string SaveFailed = "save_failed";

        // Domain - Board
        public const string BoardInitialized = "board_initialized";
        public const string CascadeResolved = "cascade_resolved";
        public const string ComboHit = "combo_hit";

        // Domain - Combat
        public const string DamageDealt = "damage_dealt";
        public const string EnemyKilled = "enemy_killed";
        public const string HealingApplied = "healing_applied";

        // Domain - Dungeon
        public const string WaveStarted = "wave_started";
        public const string WaveCleared = "wave_cleared";
        public const string BattleWon = "battle_won";
        public const string BattleLost = "battle_lost";
        public const string EnemyAttacked = "enemy_attacked";

        // Domain - Skill
        public const string SkillActivated = "skill_activated";
        public const string SkillDeactivated = "skill_deactivated";
        public const string BuffApplied = "buff_applied";
        public const string BuffExpired = "buff_expired";

        // Domain - Economy
        public const string CurrencyChanged = "currency_changed";
        public const string StaminaChanged = "stamina_changed";

        // Domain - Monster
        public const string MonsterAdded = "monster_added";
        public const string MonsterLeveled = "monster_leveled";
        public const string MonsterEvolved = "monster_evolved";
        public const string MonsterFused = "monster_fused";

        // Presentation
        public const string ScreenChanged = "screen_changed";
        public const string PopupShown = "popup_shown";
        public const string PopupDismissed = "popup_dismissed";
    }
}
