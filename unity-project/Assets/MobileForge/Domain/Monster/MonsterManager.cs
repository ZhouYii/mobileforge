using System;
using System.Collections.Generic;
using System.Linq;

namespace MobileForge.Domain
{
    /// <summary>
    /// Result of a fusion (merge / power-up) operation.
    /// </summary>
    public class FusionResult
    {
        public int ExpGained { get; set; }
        public int LevelsGained { get; set; }
        public int CoinCost { get; set; }
        public bool SkillLeveledUp { get; set; }
        public bool SameElementBonus { get; set; }
        public bool SameGroupBonus { get; set; }
    }

    /// <summary>
    /// Manages monster instances — creation, leveling, fusion, evolution,
    /// skill-up, awakening, limit break, and skill inheritance.
    /// Uses a callback for definition lookup to avoid coupling to a specific data store.
    ///
    /// Formulas match the original Tower of Saviors where available:
    /// - Fusion EXP: baseMergeExp + incMergeExp*(level-1), ×1.5 same element, ×1.5 same group
    /// - Level EXP: Ceil((level-1)^2 * expType * 52.06164)
    /// - Evolution cost: maxLevel * 100 * materialCount
    /// - Skill-up: same active skill ID → 100% success, same card series → scaling rate
    /// - Limit break: +5% stats per level (max 5 = +25%)
    /// - Awakening: stat multiplier (def.AwakenBonus, default 1.1) when all slots filled
    /// </summary>
    public class MonsterManager
    {
        private readonly Func<int, Dictionary<string, object>> _defLookup;
        private int _nextInstanceId;
        private readonly Random _rng;

        public MonsterManager(Func<int, Dictionary<string, object>> defLookup = null,
            int startingInstanceId = 1, Random rng = null)
        {
            _defLookup = defLookup;
            _nextInstanceId = startingInstanceId;
            _rng = rng ?? new Random();
        }

        // ── Creation & Lookup ──

        public MonsterInstance CreateInstance(int defId, int level = 1)
        {
            var instance = new MonsterInstance(_nextInstanceId, defId);
            instance.Level = level;
            _nextInstanceId++;
            return instance;
        }

        public MonsterDef GetDef(int defId)
        {
            if (_defLookup == null) return null;
            var data = _defLookup(defId);
            if (data == null || data.Count == 0) return null;
            return new MonsterDef(data);
        }

        public MonsterStats GetStats(MonsterInstance instance)
        {
            var def = GetDef(instance.DefId);
            if (def == null) return new MonsterStats(0, 0, 0);
            return StatCalculator.Calculate(def, instance);
        }

        // ── Leveling ──

        public int AddExp(MonsterInstance instance, int expAmount)
        {
            var def = GetDef(instance.DefId);
            if (def == null) return 0;

            int levelsGained = 0;
            instance.Exp += expAmount;

            while (instance.Level < def.MaxLevel)
            {
                int needed = StatCalculator.ExpForLevel(instance.Level, def.ExpCurve);
                if (instance.Exp >= needed)
                {
                    instance.Exp -= needed;
                    instance.Level++;
                    levelsGained++;
                }
                else break;
            }

            if (instance.Level >= def.MaxLevel)
                instance.Exp = 0;

            return levelsGained;
        }

        // ── Fusion (Merge / Power-Up) ──

        /// <summary>
        /// Simple fusion — adds exp from fodder to base. Returns exp gained.
        /// </summary>
        public int Fuse(MonsterInstance baseMonster, MonsterInstance fodder)
        {
            return FuseWithDetails(baseMonster, fodder).ExpGained;
        }

        /// <summary>
        /// Detailed fusion with ToS-accurate EXP formula, element/group bonuses,
        /// coin cost calculation, and automatic skill-up check.
        /// </summary>
        public FusionResult FuseWithDetails(MonsterInstance baseMonster, MonsterInstance fodder)
        {
            var result = new FusionResult();

            var baseDef = GetDef(baseMonster.DefId);
            var fodderDef = GetDef(fodder.DefId);
            if (fodderDef == null)
                return result;

            // Calculate EXP with element/group bonuses
            int exp = StatCalculator.MergeExp(fodderDef, fodder, baseDef);
            result.ExpGained = exp;
            result.SameElementBonus = baseDef != null && fodderDef.Element == baseDef.Element && fodderDef.Element > 0;
            result.SameGroupBonus = baseDef != null && fodderDef.Group == baseDef.Group && fodderDef.Group > 0;

            // Coin cost
            if (baseDef != null)
                result.CoinCost = StatCalculator.FusionCoinCost(baseDef, baseMonster, 1);

            // Apply EXP
            result.LevelsGained = AddExp(baseMonster, exp);

            // Skill-up check: if fodder has same active skill as base
            if (baseDef != null && fodderDef.ActiveSkillId >= 0
                && fodderDef.ActiveSkillId == baseDef.ActiveSkillId)
            {
                result.SkillLeveledUp = TrySkillUp(baseMonster, baseDef);
            }

            return result;
        }

        /// <summary>
        /// Batch fusion: fuse multiple fodder into one base. Returns aggregate result.
        /// </summary>
        public FusionResult FuseBatch(MonsterInstance baseMonster, IList<MonsterInstance> fodderList)
        {
            var aggregate = new FusionResult();
            foreach (var fodder in fodderList)
            {
                var r = FuseWithDetails(baseMonster, fodder);
                aggregate.ExpGained += r.ExpGained;
                aggregate.LevelsGained += r.LevelsGained;
                aggregate.CoinCost += r.CoinCost;
                if (r.SkillLeveledUp) aggregate.SkillLeveledUp = true;
                if (r.SameElementBonus) aggregate.SameElementBonus = true;
                if (r.SameGroupBonus) aggregate.SameGroupBonus = true;
            }
            return aggregate;
        }

        // ── Skill Level Up ──

        /// <summary>
        /// Attempt to level up the monster's skill. Returns true if successful.
        /// In ToS, using a card with the same active skill gives 100% success rate.
        /// Called automatically during fusion when fodder has matching skill.
        /// </summary>
        public bool TrySkillUp(MonsterInstance instance, MonsterDef def = null)
        {
            def ??= GetDef(instance.DefId);
            if (def == null) return false;
            if (instance.SkillLevel >= def.MaxSkillLevel) return false;

            // 100% success for same-skill fodder (ToS behavior)
            instance.SkillLevel++;
            return true;
        }

        /// <summary>
        /// Attempt skill-up with a custom success rate (0.0 to 1.0).
        /// Used for skill-up items or non-identical cards.
        /// </summary>
        public bool TrySkillUp(MonsterInstance instance, float successRate)
        {
            var def = GetDef(instance.DefId);
            if (def == null) return false;
            if (instance.SkillLevel >= def.MaxSkillLevel) return false;

            if (_rng.NextDouble() < successRate)
            {
                instance.SkillLevel++;
                return true;
            }
            return false;
        }

        // ── Plus Stats (Power-Up Fusing) ──

        /// <summary>
        /// Add plus stats from a fodder monster. Each stat caps at +99 (ToS standard).
        /// </summary>
        public void AddPlusStats(MonsterInstance target, MonsterInstance fodder,
            int maxPerStat = 99)
        {
            target.PlusHp = Math.Min(target.PlusHp + fodder.PlusHp, maxPerStat);
            target.PlusAtk = Math.Min(target.PlusAtk + fodder.PlusAtk, maxPerStat);
            target.PlusRec = Math.Min(target.PlusRec + fodder.PlusRec, maxPerStat);
        }

        // ── Awakening ──

        /// <summary>
        /// Check if the monster can awaken the next slot.
        /// Requires max level and at least one non-awakened slot.
        /// </summary>
        public bool CanAwaken(MonsterInstance instance)
        {
            var def = GetDef(instance.DefId);
            if (def == null) return false;
            if (instance.Level < def.MaxLevel) return false;
            return instance.Awakenings.Any(a => !a);
        }

        /// <summary>
        /// Awaken the next available slot. Returns the index awakened, or -1 if failed.
        /// In ToS, awakening requires max level + specific materials (simplified here).
        /// </summary>
        public int Awaken(MonsterInstance instance)
        {
            if (!CanAwaken(instance)) return -1;

            for (int i = 0; i < instance.Awakenings.Count; i++)
            {
                if (!instance.Awakenings[i])
                {
                    instance.Awakenings[i] = true;
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Check if monster is fully awakened (all slots active).
        /// </summary>
        public bool IsFullyAwakened(MonsterInstance instance)
        {
            return StatCalculator.IsFullyAwakened(instance);
        }

        // ── Limit Break ──

        /// <summary>
        /// Check if the monster can limit break further.
        /// Requires max level and below max limit break.
        /// </summary>
        public bool CanLimitBreak(MonsterInstance instance)
        {
            var def = GetDef(instance.DefId);
            if (def == null) return false;
            if (instance.Level < def.MaxLevel) return false;
            return instance.LimitBreakLevel < def.MaxLimitBreak;
        }

        /// <summary>
        /// Increase limit break by 1. Returns new LB level, or -1 if failed.
        /// Each level grants +5% to all stats (applied in StatCalculator).
        /// </summary>
        public int LimitBreak(MonsterInstance instance)
        {
            if (!CanLimitBreak(instance)) return -1;
            instance.LimitBreakLevel++;
            return instance.LimitBreakLevel;
        }

        // ── Skill Inheritance ──

        /// <summary>
        /// Check if a donor monster's active skill can be inherited by the target.
        /// Requires: donor has an active skill, target doesn't already have the same skill.
        /// </summary>
        public bool CanInheritSkill(MonsterInstance target, MonsterInstance donor)
        {
            var donorDef = GetDef(donor.DefId);
            if (donorDef == null || donorDef.ActiveSkillId < 0) return false;

            var targetDef = GetDef(target.DefId);
            if (targetDef == null) return false;

            // Can't inherit the same skill the monster already has natively
            if (donorDef.ActiveSkillId == targetDef.ActiveSkillId) return false;

            return true;
        }

        /// <summary>
        /// Transfer the donor's active skill to the target as an inherited skill.
        /// The donor is consumed. Returns the inherited skill ID, or -1 if failed.
        /// </summary>
        public int InheritSkill(MonsterInstance target, MonsterInstance donor)
        {
            if (!CanInheritSkill(target, donor)) return -1;

            var donorDef = GetDef(donor.DefId);
            target.InheritedSkillId = donorDef.ActiveSkillId;
            return target.InheritedSkillId;
        }

        /// <summary>
        /// Remove the inherited skill from a monster. Returns the removed skill ID.
        /// </summary>
        public int RemoveInheritedSkill(MonsterInstance instance)
        {
            int removed = instance.InheritedSkillId;
            instance.InheritedSkillId = -1;
            return removed;
        }

        // ── Evolution ──

        /// <summary>
        /// Check if a monster can evolve. Requires max level, valid evolve_to target,
        /// and all required materials present in the provided inventory.
        /// </summary>
        public bool CanEvolve(MonsterInstance instance, IList<MonsterInstance> ownedMonsters = null)
        {
            var def = GetDef(instance.DefId);
            if (def == null || def.EvolveTo < 0) return false;
            if (instance.Level < def.MaxLevel) return false;

            // Check materials if provided
            if (ownedMonsters != null && def.EvolveMaterials.Length > 0)
            {
                var available = ownedMonsters
                    .Where(m => m.InstanceId != instance.InstanceId)
                    .Select(m => m.DefId)
                    .ToList();

                foreach (int matId in def.EvolveMaterials)
                {
                    int idx = available.IndexOf(matId);
                    if (idx < 0) return false;
                    available.RemoveAt(idx); // Each material consumed once
                }
            }

            return true;
        }

        /// <summary>
        /// Get the coin cost for evolving this monster.
        /// </summary>
        public int GetEvolutionCost(MonsterInstance instance)
        {
            var def = GetDef(instance.DefId);
            if (def == null) return 0;
            return StatCalculator.EvolutionCoinCost(def);
        }

        /// <summary>
        /// Evolve a monster in place. Returns the new def_id, or -1 if evolution fails.
        /// Mutates the instance: changes DefId, resets level to 1 and exp to 0.
        /// Plus stats, awakenings, skill level, and limit break are preserved.
        /// </summary>
        public int Evolve(MonsterInstance instance)
        {
            var def = GetDef(instance.DefId);
            if (def == null || !CanEvolve(instance))
                return -1;

            int newDefId = def.EvolveTo;
            instance.DefId = newDefId;
            instance.Level = 1;
            instance.Exp = 0;
            // Plus stats, skill level, awakenings, LB, inherited skill all preserved
            return newDefId;
        }
    }
}
