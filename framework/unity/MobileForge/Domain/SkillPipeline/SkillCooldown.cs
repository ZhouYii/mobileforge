using System;

namespace MobileForge.Domain
{
    /// <summary>
    /// Cooldown calculation for active skills.
    /// ToS formula: current_cd = max(max_cd + 1 - skill_level, min_cd)
    /// </summary>
    public static class SkillCooldown
    {
        public static int CalculateCd(int maxCd, int minCd, int skillLevel)
        {
            return Math.Max(maxCd + 1 - skillLevel, minCd);
        }

        /// <summary>
        /// Tick cooldown by 1 turn. Returns new cooldown value.
        /// </summary>
        public static int Tick(int currentCd)
        {
            return Math.Max(currentCd - 1, 0);
        }

        /// <summary>
        /// Check if skill is ready (cooldown == 0).
        /// </summary>
        public static bool IsReady(int currentCd)
        {
            return currentCd <= 0;
        }
    }
}
