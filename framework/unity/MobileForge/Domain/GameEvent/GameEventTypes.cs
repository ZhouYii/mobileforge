using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Status constants for time-limited game events.
    /// </summary>
    public static class EventStatus
    {
        public const string Upcoming = "upcoming";
        public const string Active = "active";
        public const string GracePeriod = "grace_period";
        public const string Ended = "ended";
    }

    /// <summary>
    /// Match type constants for event bonus definitions.
    /// </summary>
    public static class EventBonusMatchType
    {
        public const string MonsterId = "monster_id";
        public const string Rarity = "rarity";
        public const string Element = "element";
        public const string Tag = "tag";
    }

    /// <summary>
    /// Definition of a time-limited game event — schedule, milestones, bonuses, shop.
    /// </summary>
    public class GameEventDef
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public long StartTime { get; set; }
        public long EndTime { get; set; }
        public long GraceEndTime { get; set; }
        public string PointCurrencyId { get; set; }
        public List<EventMilestoneDef> Milestones { get; set; } = new();
        public List<EventBonusDef> Bonuses { get; set; } = new();
        public string EventShopSectionId { get; set; }
    }

    /// <summary>
    /// Definition of an event milestone — points threshold and rewards.
    /// </summary>
    public class EventMilestoneDef
    {
        public int PointsRequired { get; set; }
        public List<Dictionary<string, object>> Rewards { get; set; } = new();
        public bool IsLoop { get; set; }
    }

    /// <summary>
    /// Definition of an event bonus — team composition multipliers.
    /// </summary>
    public class EventBonusDef
    {
        public string MatchType { get; set; }
        public string MatchValue { get; set; }
        public float PointMultiplier { get; set; } = 1.0f;
        public float DropMultiplier { get; set; } = 1.0f;
    }

    /// <summary>
    /// Mutable per-event runtime state — points, claimed milestones, loop count.
    /// </summary>
    public class GameEventState
    {
        public string EventId { get; set; }
        public int Points { get; set; }
        public HashSet<int> ClaimedMilestoneIndices { get; set; } = new();
        public int LoopCount { get; set; }
    }
}
