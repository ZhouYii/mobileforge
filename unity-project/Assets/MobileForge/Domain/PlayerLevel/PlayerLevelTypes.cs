using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Definition data for the player level system.
    /// Configures max level, EXP curve, and per-level rewards.
    /// </summary>
    public class PlayerLevelDef
    {
        public int MaxLevel { get; set; } = 999;
        public string ExpCurve { get; set; } = "standard";
        public List<LevelUpRewardDef> LevelRewards { get; set; } = new List<LevelUpRewardDef>();
    }

    /// <summary>
    /// Reward granted when the player reaches a specific level.
    /// Cumulative increases are summed from level 1 to current level.
    /// </summary>
    public class LevelUpRewardDef
    {
        public int Level { get; set; }
        public int StaminaCapIncrease { get; set; }
        public int TeamCostIncrease { get; set; }
        public int StorageIncrease { get; set; }
        public int FriendSlotsIncrease { get; set; }
        public List<Dictionary<string, object>> BonusRewards { get; set; } = new List<Dictionary<string, object>>();
    }
}
