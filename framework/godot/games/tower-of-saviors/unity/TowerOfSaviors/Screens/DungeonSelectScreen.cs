using System;
using System.Collections.Generic;
using MobileForge.Infrastructure;
using MobileForge.Domain;
using MobileForge.Presentation;

namespace TowerOfSaviors
{
    /// <summary>
    /// Dungeon selection screen. Lists available stages and checks stamina affordability.
    /// Mirrors dungeon_select_screen.gd.
    /// </summary>
    public class DungeonSelectScreen : IScreen
    {
        private GameData _gameData;
        private Economy _economy;
        private UIRouter _router;

        /// <summary>
        /// Stage info exposed for the UI layer to render.
        /// </summary>
        public class StageEntry
        {
            public int StageId { get; set; }
            public string Name { get; set; }
            public int StaminaCost { get; set; }
            public bool CanAfford { get; set; }
        }

        /// <summary>
        /// The list of available stages, populated on OnEnter.
        /// </summary>
        public List<StageEntry> Stages { get; private set; } = new List<StageEntry>();

        public void Setup(GameData gameData, Economy economy, UIRouter router)
        {
            _gameData = gameData;
            _economy = economy;
            _router = router;
        }

        public void OnEnter(Dictionary<string, object> parameters)
        {
            BuildStageList();
        }

        public void OnPause() { }
        public void OnResume()
        {
            // Refresh affordability on return (stamina may have changed)
            BuildStageList();
        }
        public void OnExit() { }

        /// <summary>
        /// Called when the player selects a stage.
        /// Checks stamina affordability and pushes team_select if affordable.
        /// </summary>
        public bool SelectStage(int stageId, int staminaCost)
        {
            if (!_economy.CheckStamina(staminaCost))
            {
                // Insufficient stamina — UI layer should show popup
                return false;
            }

            _router.Push("team_select", new Dictionary<string, object>
            {
                { "stage_id", stageId },
                { "stamina_cost", staminaCost }
            });
            return true;
        }

        private void BuildStageList()
        {
            Stages.Clear();
            var definitions = _gameData.GetAllDefinitions("stages");
            foreach (var def in definitions)
            {
                int staminaCost = def.GetInt("stamina_cost", 10);
                Stages.Add(new StageEntry
                {
                    StageId = def.Id,
                    Name = def.GetString("name", "Unknown Stage"),
                    StaminaCost = staminaCost,
                    CanAfford = _economy.CheckStamina(staminaCost)
                });
            }
        }
    }
}
