using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Builds and validates a team composition.
    /// Optionally uses MonsterManager to look up stats for validation.
    /// </summary>
    public class TeamBuilder
    {
        private readonly Team _team;
        private readonly MonsterManager _monsterManager;

        public TeamBuilder(int maxSlots = 6, MonsterManager monsterManager = null)
        {
            _team = new Team(maxSlots);
            _monsterManager = monsterManager;
        }

        /// <summary>
        /// Get the underlying team.
        /// </summary>
        public Team GetTeam()
        {
            return _team;
        }

        /// <summary>
        /// Set a monster instance into a slot. Returns true if valid index.
        /// </summary>
        public bool SetSlot(int index, MonsterInstance instance)
        {
            return _team.SetSlot(index, instance);
        }

        /// <summary>
        /// Clear a specific slot.
        /// </summary>
        public void ClearSlot(int index)
        {
            _team.SetSlot(index, null);
        }

        /// <summary>
        /// Clear all slots.
        /// </summary>
        public void ClearAll()
        {
            for (int i = 0; i < _team.MaxSlots; i++)
                _team.SetSlot(i, null);
        }

        /// <summary>
        /// Validate the team. Returns a list of error strings (empty = valid).
        /// Checks: at least one member, no duplicate instances, cost within budget.
        /// </summary>
        public List<string> Validate(int maxCost = int.MaxValue)
        {
            var errors = new List<string>();

            // Check at least one member
            if (_team.GetFilledCount() == 0)
            {
                errors.Add("Team must have at least one member");
                return errors;
            }

            // Check for duplicate instance IDs
            var seenIds = new HashSet<int>();
            for (int i = 0; i < _team.MaxSlots; i++)
            {
                var member = _team.GetSlot(i);
                if (member == null)
                    continue;

                if (member is MonsterInstance inst)
                {
                    if (!seenIds.Add(inst.InstanceId))
                    {
                        errors.Add($"Duplicate monster in slot {i}: instance {inst.InstanceId}");
                    }
                }
            }

            // Check total cost
            if (_monsterManager != null && maxCost < int.MaxValue)
            {
                var stats = GetTeamStats();
                if (stats.TotalCost > maxCost)
                {
                    errors.Add($"Team cost {stats.TotalCost} exceeds maximum {maxCost}");
                }
            }

            return errors;
        }

        /// <summary>
        /// Calculate aggregate team stats.
        /// Requires MonsterManager for definition/stat lookups.
        /// Returns zero-stats if MonsterManager is null.
        /// </summary>
        public TeamStats GetTeamStats()
        {
            var stats = new TeamStats();

            for (int i = 0; i < _team.MaxSlots; i++)
            {
                var member = _team.GetSlot(i);
                if (member == null)
                    continue;

                if (member is MonsterInstance inst)
                {
                    stats.MemberCount++;

                    if (_monsterManager != null)
                    {
                        var def = _monsterManager.GetDef(inst.DefId);
                        if (def != null)
                        {
                            stats.TotalCost += def.Cost;
                        }

                        var mStats = _monsterManager.GetStats(inst);
                        stats.TotalHp += mStats.Hp;
                        stats.TotalAtk += mStats.Atk;
                        stats.TotalRec += mStats.Rec;
                    }
                }
            }

            return stats;
        }
    }
}
