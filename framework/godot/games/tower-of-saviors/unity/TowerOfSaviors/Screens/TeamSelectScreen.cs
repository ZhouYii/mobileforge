using System;
using System.Collections.Generic;
using MobileForge.Infrastructure;
using MobileForge.Domain;
using MobileForge.Presentation;

namespace TowerOfSaviors
{
    /// <summary>
    /// Team selection screen before entering a dungeon.
    /// Mirrors team_select_screen.gd — 5 team slots, monster collection grid,
    /// tap to assign/remove, "Enter Dungeon" enabled when at least 1 selected.
    /// </summary>
    public class TeamSelectScreen : IScreen
    {
        public const int MaxTeamSize = 5;

        private MonsterManager _monsterManager;
        private PlayerState _playerState;
        private UIRouter _router;
        private Dictionary<string, object> _params;

        private readonly List<int> _selectedIds = new();

        public int StageId { get; private set; }
        public int StaminaCost { get; private set; }
        public bool CanStart => _selectedIds.Count > 0;

        public void Setup(MonsterManager monsterManager, PlayerState playerState,
            UIRouter router, Dictionary<string, object> parameters)
        {
            _monsterManager = monsterManager;
            _playerState = playerState;
            _router = router;
            _params = parameters ?? new Dictionary<string, object>();

            StageId = GetIntParam("stage_id", 0);
            StaminaCost = GetIntParam("stamina_cost", 10);
        }

        public void OnEnter(Dictionary<string, object> parameters)
        {
            // Screen ready — UI layer populates slots from GetSelectedTeam()
        }

        public void OnPause() { }
        public void OnResume() { }
        public void OnExit() { }

        /// <summary>
        /// Add a monster to the team. Returns false if team is full or monster already selected.
        /// </summary>
        public bool SelectMonster(int monsterId)
        {
            if (_selectedIds.Count >= MaxTeamSize)
                return false;
            if (_selectedIds.Contains(monsterId))
                return false;
            _selectedIds.Add(monsterId);
            return true;
        }

        /// <summary>
        /// Remove a monster from a team slot by index.
        /// </summary>
        public bool RemoveSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _selectedIds.Count)
                return false;
            _selectedIds.RemoveAt(slotIndex);
            return true;
        }

        /// <summary>
        /// Get the currently selected monster IDs.
        /// </summary>
        public List<int> GetSelectedTeam()
        {
            return new List<int>(_selectedIds);
        }

        /// <summary>
        /// Get monster data for a slot index. Returns null if slot is empty.
        /// </summary>
        public Dictionary<string, object> GetSlotData(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _selectedIds.Count)
                return null;
            var def = _monsterManager.GetDef(_selectedIds[slotIndex]);
            if (def == null) return null;
            return new Dictionary<string, object>
            {
                { "id", def.Id },
                { "name", def.Name },
                { "element", def.Element },
                { "rarity", def.Rarity }
            };
        }

        /// <summary>
        /// Called when the player presses "Enter Dungeon".
        /// Navigates to the battle screen with the selected team.
        /// </summary>
        public void OnStartBattle()
        {
            if (_selectedIds.Count == 0)
                return;

            var battleParams = new Dictionary<string, object>(_params);
            battleParams["team_ids"] = new List<int>(_selectedIds);
            _router.Navigate("battle", battleParams);
        }

        /// <summary>
        /// Called when the player presses "Back".
        /// </summary>
        public void OnBack()
        {
            _router.Pop();
        }

        private int GetIntParam(string key, int defaultValue)
        {
            if (_params != null && _params.TryGetValue(key, out var val))
            {
                try { return Convert.ToInt32(val); }
                catch { return defaultValue; }
            }
            return defaultValue;
        }
    }
}
