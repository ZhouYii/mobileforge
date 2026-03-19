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

        // Domain - Quest
        public const string QuestActivated = "quest_activated";
        public const string QuestProgress = "quest_progress";
        public const string QuestCompleted = "quest_completed";
        public const string QuestClaimed = "quest_claimed";

        // Domain - Inventory
        public const string InventoryChanged = "inventory_changed";
        public const string ItemEquipped = "item_equipped";
        public const string ItemUnequipped = "item_unequipped";

        // Domain - Buff
        public const string BuffRemoved = "buff_removed";
        public const string BuffStacked = "buff_stacked";
        public const string BuffCleansed = "buff_cleansed";

        // Domain - Equipment
        public const string EquipmentEnhanced = "equipment_enhanced";
        public const string EquipmentEquipped = "equipment_equipped";
        public const string SetBonusActivated = "set_bonus_activated";

        // Domain - GameEvent
        public const string EventStarted = "event_started";
        public const string EventEnded = "event_ended";
        public const string EventPointsEarned = "event_points_earned";
        public const string EventMilestoneClaimed = "event_milestone_claimed";

        // Domain - PlayerLevel
        public const string PlayerLevelUp = "player_level_up";

        // Domain - Social
        public const string FriendAdded = "friend_added";
        public const string FriendRemoved = "friend_removed";
        public const string SupportUsed = "support_used";

        // Domain - Arena
        public const string ArenaWin = "arena_win";
        public const string ArenaLoss = "arena_loss";
        public const string ArenaTierChanged = "arena_tier_changed";

        // Domain - Mail
        public const string MailReceived = "mail_received";
        public const string MailRead = "mail_read";
        public const string MailClaimed = "mail_claimed";

        // Infrastructure - Badge
        public const string BadgeChanged = "badge_changed";

        // Infrastructure - Preferences
        public const string PreferenceChanged = "preference_changed";

        // Presentation
        public const string ScreenChanged = "screen_changed";
        public const string PopupShown = "popup_shown";
        public const string PopupDismissed = "popup_dismissed";
    }
}
