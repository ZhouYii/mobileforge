using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Pure math for monster stat calculation.
    /// Base formula: stat = base + (max - base) * ((level-1) / (maxLevel-1)) ^ curveExp
    /// Plus stats added raw (1:1) after curve.
    /// Limit break adds +5% per level to all stats.
    /// Full awakening applies the def's AwakenBonus multiplier (default 1.1x).
    ///
    /// EXP curve uses actual ToS formula: Ceil((level-1)^2 * expType * 52.06164)
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

        private const float ExpConstant = 52.06164f;
        private const float LimitBreakBonusPerLevel = 0.05f; // +5% per LB level

        /// <summary>
        /// Calculate stats for a monster at a given level, including plus stats,
        /// limit break bonus, and awakening bonus.
        /// </summary>
        public static MonsterStats Calculate(MonsterDef def, MonsterInstance instance)
        {
            float curveExp = GetCurveExponent(def.ExpCurve);
            float levelRatio = 0f;
            if (def.MaxLevel > 1)
                levelRatio = (float)(instance.Level - 1) / (def.MaxLevel - 1);

            float ratioCurved = (float)Math.Pow(levelRatio, curveExp);

            int hp = (int)(def.BaseHp + (def.MaxHp - def.BaseHp) * ratioCurved) + instance.PlusHp;
            int atk = (int)(def.BaseAtk + (def.MaxAtk - def.BaseAtk) * ratioCurved) + instance.PlusAtk;
            int rec = (int)(def.BaseRec + (def.MaxRec - def.BaseRec) * ratioCurved) + instance.PlusRec;

            // Limit break: +5% per level
            if (instance.LimitBreakLevel > 0)
            {
                float lbMult = 1f + instance.LimitBreakLevel * LimitBreakBonusPerLevel;
                hp = (int)(hp * lbMult);
                atk = (int)(atk * lbMult);
                rec = (int)(rec * lbMult);
            }

            // Awakening: apply multiplier when all slots are awakened
            if (IsFullyAwakened(instance))
            {
                float awakenMult = def.AwakenBonus;
                hp = (int)(hp * awakenMult);
                atk = (int)(atk * awakenMult);
                rec = (int)(rec * awakenMult);
            }

            return new MonsterStats(hp, atk, rec);
        }

        /// <summary>
        /// Experience needed to advance from a given level.
        /// Uses ToS formula: Ceil((level-1)^2 * expType * 52.06164)
        /// Falls back to 100 for level 1 (since (1-1)^2 = 0).
        /// </summary>
        public static int ExpForLevel(int level, string curve = "standard")
        {
            if (level <= 1) return 100;
            float expType = GetCurveExponent(curve);
            return (int)Math.Ceiling((level - 1) * (level - 1) * expType * ExpConstant);
        }

        /// <summary>
        /// Total cumulative EXP from level 1 to a target level.
        /// </summary>
        public static int TotalExpToLevel(int targetLevel, string curve = "standard")
        {
            int total = 0;
            for (int lv = 1; lv < targetLevel; lv++)
                total += ExpForLevel(lv, curve);
            return total;
        }

        /// <summary>
        /// Calculate the merge (fusion) EXP a monster provides when used as fodder.
        /// Uses ToS formula: (baseMergeExp + incMergeExp * (level-1)) * bonusMultiplier
        /// bonusMultiplier = 1.0 * (1.5 if same element) * (1.5 if same group)
        /// Falls back to rarity * level * 50 if baseMergeExp is not defined.
        /// </summary>
        public static int MergeExp(MonsterDef fodderDef, MonsterInstance fodder,
            MonsterDef targetDef)
        {
            int baseExp;
            if (fodderDef.BaseMergeExp >= 0)
                baseExp = fodderDef.BaseMergeExp + fodderDef.IncMergeExp * (fodder.Level - 1);
            else
                baseExp = fodder.Level * fodderDef.Rarity * 50;

            float multiplier = 1.0f;
            if (targetDef != null)
            {
                if (fodderDef.Element == targetDef.Element && fodderDef.Element > 0)
                    multiplier *= 1.5f;
                if (fodderDef.Group == targetDef.Group && fodderDef.Group > 0)
                    multiplier *= 1.5f;
            }

            return (int)(baseExp * multiplier);
        }

        /// <summary>
        /// Coin cost for fusing N fodder into a target.
        /// ToS formula: target.mergeCoin * count + bonusTotal * 2000
        /// Simplified: baseCost scales with target level and rarity.
        /// </summary>
        public static int FusionCoinCost(MonsterDef targetDef, MonsterInstance target, int fodderCount)
        {
            int baseCost = target.Level * targetDef.Rarity * 100;
            return baseCost * fodderCount;
        }

        /// <summary>
        /// Coin cost for evolution.
        /// ToS formula: maxLevel * 100 * materialCount
        /// </summary>
        public static int EvolutionCoinCost(MonsterDef def)
        {
            int materialCount = def.EvolveMaterials?.Length ?? 0;
            if (materialCount == 0) materialCount = 1;
            return def.MaxLevel * 100 * materialCount;
        }

        public static float GetCurveExponent(string curveName)
        {
            return CurveExponents.TryGetValue(curveName, out float exp) ? exp : 1.0f;
        }

        public static bool IsFullyAwakened(MonsterInstance instance)
        {
            if (instance.Awakenings == null || instance.Awakenings.Count == 0)
                return false;
            for (int i = 0; i < instance.Awakenings.Count; i++)
                if (!instance.Awakenings[i]) return false;
            return true;
        }
    }
}
