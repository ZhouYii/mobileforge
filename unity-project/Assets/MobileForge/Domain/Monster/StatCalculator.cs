using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Pure math for monster stat calculation.
    /// Formula: stat = base + (max - base) * ((level-1) / (maxLevel-1)) ^ curveExp
    /// Curves: standard=1.0, slow=1.5, fast=0.7, super_slow=2.0
    /// Plus stats are added raw (1:1) after curve calculation.
    /// </summary>
    public static class StatCalculator
    {
        private static readonly Dictionary<string, float> CurveExponents = new()
        {
            ["standard"] = 1.0f,
            ["slow"] = 1.5f,
            ["fast"] = 0.7f,
            ["super_slow"] = 2.0f,
        };

        /// <summary>
        /// Calculate stats for a monster at a given level.
        /// </summary>
        public static MonsterStats Calculate(MonsterDef def, MonsterInstance instance)
        {
            float curveExp = GetCurveExponent(def.ExpCurve);
            float levelRatio = 0f;
            if (def.MaxLevel > 1)
                levelRatio = (float)(instance.Level - 1) / (def.MaxLevel - 1);

            float ratioCurved = (float)Math.Pow(levelRatio, curveExp);

            // Truncate via (int) cast to match Godot's int() behavior
            int hp = (int)(def.BaseHp + (def.MaxHp - def.BaseHp) * ratioCurved) + instance.PlusHp;
            int atk = (int)(def.BaseAtk + (def.MaxAtk - def.BaseAtk) * ratioCurved) + instance.PlusAtk;
            int rec = (int)(def.BaseRec + (def.MaxRec - def.BaseRec) * ratioCurved) + instance.PlusRec;

            return new MonsterStats(hp, atk, rec);
        }

        /// <summary>
        /// Calculate experience needed for a given level.
        /// Formula: 100 * level * curve_exponent
        /// </summary>
        public static int ExpForLevel(int level, string curve = "standard")
        {
            float multiplier = GetCurveExponent(curve);
            return (int)(100 * level * multiplier);
        }

        /// <summary>
        /// Get the curve exponent for a named curve.
        /// </summary>
        public static float GetCurveExponent(string curveName)
        {
            return CurveExponents.TryGetValue(curveName, out float exp) ? exp : 1.0f;
        }
    }
}
