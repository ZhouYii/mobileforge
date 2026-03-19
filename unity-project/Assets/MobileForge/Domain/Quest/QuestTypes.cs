using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Type definitions for the quest system.
    /// </summary>
    public static class QuestCategory
    {
        public const string Daily = "daily";
        public const string Weekly = "weekly";
        public const string Achievement = "achievement";
        public const string Story = "story";
    }

    public static class QuestStatus
    {
        public const string Locked = "locked";
        public const string Active = "active";
        public const string Completed = "completed";
        public const string Claimed = "claimed";
    }

    /// <summary>
    /// Definition of a quest objective — which event triggers progress and how many times.
    /// </summary>
    public class QuestObjective
    {
        public string EventName { get; set; }
        public int TargetCount { get; set; } = 1;
        public Dictionary<string, object> Filter { get; set; } = new();
    }

    /// <summary>
    /// Definition of a quest — objectives, rewards, prerequisites, repeatability.
    /// </summary>
    public class QuestDef
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; } = QuestCategory.Daily;
        public List<QuestObjective> Objectives { get; set; } = new();
        public List<Dictionary<string, object>> Rewards { get; set; } = new();
        public List<string> Prerequisites { get; set; } = new();
        public bool Repeatable { get; set; }
        /// <summary>For achievements: the base achievement ID this quest belongs to.</summary>
        public string AchievementBaseId { get; set; }
        /// <summary>For achievements: the tier index (0, 1, 2...) within the achievement chain.</summary>
        public int AchievementTier { get; set; }
    }

    /// <summary>
    /// Runtime state of a quest — objective progress and current status.
    /// </summary>
    public class QuestState
    {
        public List<int> ObjectiveProgress { get; set; } = new();
        public string Status { get; set; } = QuestStatus.Active;
    }

    /// <summary>
    /// Defines a chain of progressive achievement tiers (e.g., kill 10/100/1000 monsters).
    /// Each tier references a QuestDef.Id.
    /// </summary>
    public class AchievementTierDef
    {
        public string AchievementId { get; set; }
        public int Tier { get; set; }
        public int TargetCount { get; set; }
        public List<Dictionary<string, object>> Rewards { get; set; } = new();
    }

    /// <summary>
    /// Configuration for daily/weekly mission auto-reset.
    /// </summary>
    public class MissionResetConfig
    {
        /// <summary>Category to reset (QuestCategory.Daily or Weekly).</summary>
        public string ResetCategory { get; set; } = QuestCategory.Daily;
        /// <summary>UTC hour at which daily reset occurs (0-23).</summary>
        public int ResetHourUtc { get; set; } = 4;
        /// <summary>For weekly: day of week (0=Sunday). Ignored for daily.</summary>
        public int ResetDayOfWeek { get; set; } = 1; // Monday
    }
}
