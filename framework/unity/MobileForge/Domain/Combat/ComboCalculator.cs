using System;

namespace MobileForge.Domain
{
    /// <summary>
    /// Pure math for combo multiplier calculation.
    /// ToS formula: combo_mult = 1 + (combo_count - 1) * 0.25
    /// e.g., 1 combo = 1.0x, 2 combos = 1.25x, 5 combos = 2.0x
    /// </summary>
    public static class ComboCalculator
    {
        public const float BaseMultiplier = 1.0f;
        public const float PerComboBonus = 0.25f;

        /// <summary>
        /// Calculate the combo multiplier for a given combo count.
        /// </summary>
        public static float Calculate(int comboCount)
        {
            if (comboCount <= 0)
                return 0.0f;
            return BaseMultiplier + (comboCount - 1) * PerComboBonus;
        }

        /// <summary>
        /// Calculate base gem damage: base_damage = atk * (1 + (gems - 3) * 0.25)
        /// Where gems = number of gems of this element matched in this combo.
        /// </summary>
        public static float GemDamage(float atk, int gemsMatched)
        {
            if (gemsMatched < 1)
                return 0.0f;
            // Minimum 3 gems for a match
            return atk * (1.0f + Math.Max(gemsMatched - 3, 0) * 0.25f);
        }
    }
}
