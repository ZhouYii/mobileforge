using System;
using System.Collections.Generic;
using MobileForge.Infrastructure;
using MobileForge.Domain;
using MobileForge.Presentation;

namespace TowerOfSaviors
{
    /// <summary>
    /// Team selection screen before entering a dungeon.
    /// Mirrors team_select_screen.gd — placeholder for team slot UI.
    /// </summary>
    public class TeamSelectScreen : IScreen
    {
        private MonsterManager _monsterManager;
        private PlayerState _playerState;
        private UIRouter _router;
        private Dictionary<string, object> _params;

        public int StageId { get; private set; }
        public int StaminaCost { get; private set; }

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
            // Team selection screen is ready
            // TODO: populate team slot UI with player's monsters
        }

        public void OnPause() { }
        public void OnResume() { }
        public void OnExit() { }

        /// <summary>
        /// Called when the player presses "Enter Dungeon".
        /// Navigates to the battle screen with the selected stage parameters.
        /// </summary>
        public void OnStartBattle()
        {
            _router.Navigate("battle", new Dictionary<string, object>(_params));
        }

        /// <summary>
        /// Called when the player presses "Back".
        /// Pops back to the dungeon select screen.
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
