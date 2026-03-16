using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Manages monster instances — creation, leveling, fusion, evolution.
    /// Uses a callback for definition lookup to avoid coupling to a specific data store.
    /// </summary>
    public class MonsterManager
    {
        private readonly Func<int, Dictionary<string, object>> _defLookup;
        private int _nextInstanceId;

        /// <summary>
        /// Create a MonsterManager with a definition lookup callback.
        /// The callback takes a monster def ID and returns raw dictionary data, or null if not found.
        /// Passing null for the lookup is allowed — GetDef will return null.
        /// </summary>
        public MonsterManager(Func<int, Dictionary<string, object>> defLookup = null, int startingInstanceId = 1)
        {
            _defLookup = defLookup;
            _nextInstanceId = startingInstanceId;
        }

        /// <summary>
        /// Create a new monster instance from a definition ID.
        /// Always succeeds — does NOT validate the def_id.
        /// </summary>
        public MonsterInstance CreateInstance(int defId, int level = 1)
        {
            var instance = new MonsterInstance(_nextInstanceId, defId);
            instance.Level = level;
            _nextInstanceId++;
            return instance;
        }

        /// <summary>
        /// Get the MonsterDef for a definition ID via the lookup callback.
        /// Returns null if the definition is not found or lookup is null.
        /// </summary>
        public MonsterDef GetDef(int defId)
        {
            if (_defLookup == null)
                return null;

            var data = _defLookup(defId);
            if (data == null || data.Count == 0)
                return null;

            return new MonsterDef(data);
        }

        /// <summary>
        /// Calculate current stats for a monster instance.
        /// Returns zero-stats if the definition is not found.
        /// </summary>
        public MonsterStats GetStats(MonsterInstance instance)
        {
            var def = GetDef(instance.DefId);
            if (def == null)
                return new MonsterStats(0, 0, 0);

            return StatCalculator.Calculate(def, instance);
        }

        /// <summary>
        /// Add experience to a monster instance. Handles level-ups.
        /// Returns the number of levels gained.
        /// </summary>
        public int AddExp(MonsterInstance instance, int expAmount)
        {
            var def = GetDef(instance.DefId);
            if (def == null)
                return 0;

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
                else
                {
                    break;
                }
            }

            if (instance.Level >= def.MaxLevel)
            {
                instance.Exp = 0;
            }

            return levelsGained;
        }

        /// <summary>
        /// Fuse a fodder monster into a base monster (adds exp).
        /// Returns exp gained.
        /// </summary>
        public int Fuse(MonsterInstance baseMonster, MonsterInstance fodder)
        {
            var fodderDef = GetDef(fodder.DefId);
            if (fodderDef == null)
                return 0;

            // Exp from fusion = fodder level * fodder rarity * 50
            int expValue = fodder.Level * fodderDef.Rarity * 50;
            AddExp(baseMonster, expValue);
            return expValue;
        }

        /// <summary>
        /// Check if a monster can evolve.
        /// Requires max level and a valid evolve_to target.
        /// </summary>
        public bool CanEvolve(MonsterInstance instance)
        {
            var def = GetDef(instance.DefId);
            if (def == null || def.EvolveTo < 0)
                return false;

            if (instance.Level < def.MaxLevel)
                return false;

            return true;
        }

        /// <summary>
        /// Evolve a monster in place. Returns the new def_id, or -1 if evolution fails.
        /// Mutates the instance: changes DefId, resets level to 1 and exp to 0.
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
            return newDefId;
        }
    }
}
