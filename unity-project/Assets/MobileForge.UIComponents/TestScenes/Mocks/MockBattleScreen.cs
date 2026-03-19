using System;
using System.Reflection;
using System.Collections.Generic;
using MobileForge.Domain;
using TowerOfSaviors;
using UnityEngine;

namespace MobileForge.UIComponents.TestScenes.Mocks
{
    /// <summary>
    /// Mock BattleScreen for page preview.
    /// Uses reflection to initialize the private BoardLogic and DungeonState
    /// that the real GetBoardElements() and State property read from.
    /// </summary>
    public class MockBattleScreen : BattleScreen
    {
        public MockBattleScreen()
        {
            // Create a real board with sample data
            var config = new BoardConfig(5, 6);
            var board = new BoardLogic(config);
            board.InitBoard();

            // Set private _board field via reflection
            var boardField = typeof(BattleScreen).GetField("_board",
                BindingFlags.NonPublic | BindingFlags.Instance);
            boardField?.SetValue(this, board);

            // Create a mock DungeonState and set it
            var state = new DungeonState
            {
                CurrentWaveIndex = 0,
                TeamHp = 8000,
                MaxHp = 10000,
                IsActive = true,
                Enemies = new List<EnemyState>
                {
                    new EnemyState(new Dictionary<string, object>
                        { {"id", 1}, {"name", "Fire Dragon"}, {"hp", 5000}, {"max_hp", 5000}, {"element", 2} }),
                    new EnemyState(new Dictionary<string, object>
                        { {"id", 2}, {"name", "Dark Imp"}, {"hp", 2000}, {"max_hp", 3000}, {"element", 5} }),
                }
            };

            // Set the State auto-property via its backing field
            // Auto-property backing field is named "<State>k__BackingField"
            var stateField = typeof(BattleScreen).GetField("<State>k__BackingField",
                BindingFlags.NonPublic | BindingFlags.Instance);
            stateField?.SetValue(this, state);

            Debug.Log("[MockBattleScreen] Initialized with sample board and state");
        }
    }
}
