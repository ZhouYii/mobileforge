namespace MobileForge.Domain
{
    /// <summary>
    /// Dungeon-specific event name constants.
    /// Matches Godot's StringName constants for cross-platform event consistency.
    /// </summary>
    public static class DungeonEvents
    {
        public const string DungeonStarted = "dungeon_started";
        public const string WaveStarted = "dungeon_wave_started";
        public const string WaveCleared = "dungeon_wave_cleared";
        public const string TurnStarted = "dungeon_turn_started";
        public const string TurnEnded = "dungeon_turn_ended";
        public const string PlayerAttack = "dungeon_player_attack";
        public const string EnemyAttack = "dungeon_enemy_attack";
        public const string EnemyKilled = "dungeon_enemy_killed";
        public const string TeamHealed = "dungeon_team_healed";
        public const string TeamDamaged = "dungeon_team_damaged";
        public const string SkillActivated = "dungeon_skill_activated";
        public const string BattleWon = "dungeon_battle_won";
        public const string BattleLost = "dungeon_battle_lost";
        public const string DungeonCompleted = "dungeon_completed";
    }
}
