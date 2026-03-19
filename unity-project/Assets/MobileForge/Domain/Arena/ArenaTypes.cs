using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Static definition for the PvP arena system.
    /// Defines tiers, attempt limits, season duration, and season rewards.
    /// </summary>
    public class ArenaDef
    {
        public List<ArenaTierDef> Tiers { get; set; } = new();
        public int AttemptsPerDay { get; set; } = 5;
        public int SeasonDurationDays { get; set; } = 14;
        public List<Dictionary<string, object>> SeasonRewards { get; set; } = new();
    }

    /// <summary>
    /// Definition for a single arena tier (e.g. Bronze, Silver, Gold).
    /// MaxTrophies of -1 indicates no upper bound.
    /// </summary>
    public class ArenaTierDef
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int MinTrophies { get; set; }
        public int MaxTrophies { get; set; } = -1;
        public int WinTrophies { get; set; }
        public int LoseTrophies { get; set; }
    }

    /// <summary>
    /// Mutable player record for arena state.
    /// </summary>
    public class ArenaRecord
    {
        public int Trophies { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int AttemptsToday { get; set; }
        public int HighestTrophies { get; set; }
        public string LastResetDate { get; set; } = "";
        public int SeasonId { get; set; }
        public List<int> DefenseTeam { get; set; } = new();
    }
}
