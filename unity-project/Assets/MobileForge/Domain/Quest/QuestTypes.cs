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
    }

    /// <summary>
    /// Runtime state of a quest — objective progress and current status.
    /// </summary>
    public class QuestState
    {
        public List<int> ObjectiveProgress { get; set; } = new();
        public string Status { get; set; } = QuestStatus.Active;
    }
}
