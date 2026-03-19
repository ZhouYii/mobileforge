using System;
using System.Collections.Generic;
using System.Linq;

namespace MobileForge.Domain
{
    /// <summary>
    /// Mutable player level state. Tracks level, current EXP within level,
    /// and total lifetime EXP. Provides stat cap calculations based on
    /// cumulative level-up rewards.
    ///
    /// EXP curve formula: Ceil(level^2 * curveExponent * 50)
    /// Curve exponents: standard=1.0, slow=1.5, fast=0.7
    /// </summary>
    public class PlayerLevel
    {
        private static readonly Dictionary<string, float> CurveExponents = new()
        {
            ["standard"] = 1.0f,
            ["slow"] = 1.5f,
            ["fast"] = 0.7f,
        };

        // ── State ──

        public int Level { get; private set; } = 1;
        public int Exp { get; private set; }
        public int TotalExp { get; private set; }

        // ── Events ──

        /// <summary>
        /// Fired for each level gained. Parameters: oldLevel, newLevel.
        /// </summary>
        public event Action<int, int> LeveledUp;

        // ── Constructor ──

        public PlayerLevel() { }

        // ── EXP Curve ──

        /// <summary>
        /// EXP required to advance from the given level.
        /// Formula: Ceil(level^2 * curveExponent * 50)
        /// </summary>
        public static int ExpForLevel(int level, string curve = "standard")
        {
            float exponent = CurveExponents.TryGetValue(curve, out float e) ? e : 1.0f;
            return (int)Math.Ceiling(level * level * exponent * 50.0);
        }

        // ── Leveling ──

        /// <summary>
        /// Adds EXP and processes level-ups in a loop. Returns the number
        /// of levels gained. Caps at def.MaxLevel. Fires LeveledUp for
        /// each individual level gained.
        /// </summary>
        public int AddExp(PlayerLevelDef def, int amount)
        {
            if (amount <= 0) return 0;
            if (Level >= def.MaxLevel) return 0;

            Exp += amount;
            TotalExp += amount;

            int levelsGained = 0;

            while (Level < def.MaxLevel)
            {
                int needed = ExpForLevel(Level, def.ExpCurve);
                if (Exp >= needed)
                {
                    Exp -= needed;
                    int oldLevel = Level;
                    Level++;
                    levelsGained++;
                    LeveledUp?.Invoke(oldLevel, Level);
                }
                else
                {
                    break;
                }
            }

            // Cap: if at max level, zero out remaining EXP
            if (Level >= def.MaxLevel)
                Exp = 0;

            return levelsGained;
        }

        // ── Stat Caps ──

        /// <summary>
        /// Base 50 + sum of StaminaCapIncrease from all rewards up to current level.
        /// </summary>
        public int GetStaminaCap(PlayerLevelDef def)
        {
            return 50 + SumReward(def, r => r.StaminaCapIncrease);
        }

        /// <summary>
        /// Base 50 + sum of TeamCostIncrease from all rewards up to current level.
        /// </summary>
        public int GetTeamCostLimit(PlayerLevelDef def)
        {
            return 50 + SumReward(def, r => r.TeamCostIncrease);
        }

        /// <summary>
        /// Base 50 + sum of StorageIncrease from all rewards up to current level.
        /// </summary>
        public int GetStorageCapacity(PlayerLevelDef def)
        {
            return 50 + SumReward(def, r => r.StorageIncrease);
        }

        /// <summary>
        /// Base 10 + sum of FriendSlotsIncrease from all rewards up to current level.
        /// </summary>
        public int GetFriendSlots(PlayerLevelDef def)
        {
            return 10 + SumReward(def, r => r.FriendSlotsIncrease);
        }

        /// <summary>
        /// Returns the LevelUpRewardDef for the specified level, or null.
        /// </summary>
        public LevelUpRewardDef GetLevelRewards(PlayerLevelDef def, int level)
        {
            if (def.LevelRewards == null) return null;
            return def.LevelRewards.FirstOrDefault(r => r.Level == level);
        }

        // ── Progress Helpers ──

        /// <summary>
        /// Remaining EXP needed to reach the next level.
        /// Returns 0 if already at max level.
        /// </summary>
        public int ExpToNextLevel(PlayerLevelDef def)
        {
            if (Level >= def.MaxLevel) return 0;
            int needed = ExpForLevel(Level, def.ExpCurve);
            return needed - Exp;
        }

        /// <summary>
        /// Progress within the current level as a float from 0 to 1.
        /// Returns 1 if at max level.
        /// </summary>
        public float ExpProgress(PlayerLevelDef def)
        {
            if (Level >= def.MaxLevel) return 1f;
            int needed = ExpForLevel(Level, def.ExpCurve);
            if (needed <= 0) return 1f;
            return (float)Exp / needed;
        }

        // ── Persistence ──

        public Dictionary<string, object> ToSaveDict()
        {
            return new Dictionary<string, object>
            {
                ["level"] = Level,
                ["exp"] = Exp,
                ["totalExp"] = TotalExp,
            };
        }

        public static PlayerLevel FromSaveDict(Dictionary<string, object> data)
        {
            var pl = new PlayerLevel();
            pl.Level = Convert.ToInt32(data.GetValueOrDefault("level", 1));
            pl.Exp = Convert.ToInt32(data.GetValueOrDefault("exp", 0));
            pl.TotalExp = Convert.ToInt32(data.GetValueOrDefault("totalExp", 0));
            return pl;
        }

        // ── Private Helpers ──

        private int SumReward(PlayerLevelDef def, Func<LevelUpRewardDef, int> selector)
        {
            if (def.LevelRewards == null) return 0;
            return def.LevelRewards
                .Where(r => r.Level <= Level)
                .Sum(selector);
        }
    }
}
