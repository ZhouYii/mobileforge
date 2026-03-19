using System;
using System.Collections.Generic;
using System.Linq;
using MobileForge.Infrastructure;
using MobileForge.Domain;
using MobileForge.Presentation;

namespace TowerOfSaviors
{
    /// <summary>
    /// Dungeon selection screen. Lists available stages filtered by difficulty,
    /// shows stamina, turn limits, floor effects, and board dimensions.
    /// Mirrors dungeon_select_screen.gd.
    /// </summary>
    public class DungeonSelectScreen : IScreen
    {
        private GameData _gameData;
        private Economy _economy;
        private UIRouter _router;
        private List<StageEntry> _allStages = new();
        private string _activeDifficulty = "normal";

        /// <summary>
        /// Stage info exposed for the UI layer to render.
        /// </summary>
        public class StageEntry
        {
            public int StageId { get; set; }
            public string Name { get; set; }
            public string Difficulty { get; set; }
            public int StaminaCost { get; set; }
            public bool CanAfford { get; set; }
            public int TurnLimit { get; set; }           // 0 = unlimited
            public int BoardRows { get; set; }            // default 5
            public int BoardCols { get; set; }            // default 6
            public bool IsDaily { get; set; }
            public int DayOfWeek { get; set; }            // -1 = any day
            public List<string> FloorEffects { get; set; } = new();
            public int WaveCount { get; set; }
        }

        /// <summary>Filtered stages for current difficulty tab.</summary>
        public List<StageEntry> Stages { get; private set; } = new();

        /// <summary>Available difficulty tiers present in stage data.</summary>
        public List<string> AvailableDifficulties { get; private set; } = new();

        /// <summary>Current player stamina.</summary>
        public int CurrentStamina => _economy.GetBalance("stamina");

        /// <summary>Max stamina.</summary>
        public int MaxStamina => 100;

        /// <summary>Active difficulty filter.</summary>
        public string ActiveDifficulty => _activeDifficulty;

        public void Setup(GameData gameData, Economy economy, UIRouter router)
        {
            _gameData = gameData;
            _economy = economy;
            _router = router;
        }

        public void OnEnter(Dictionary<string, object> parameters)
        {
            BuildAllStages();
            FilterByDifficulty(_activeDifficulty);
        }

        public void OnPause() { }
        public void OnResume()
        {
            RefreshAffordability();
        }
        public void OnExit() { }

        /// <summary>
        /// Switch difficulty tab and rebuild visible stage list.
        /// </summary>
        public void SetDifficulty(string difficulty)
        {
            _activeDifficulty = difficulty;
            FilterByDifficulty(difficulty);
        }

        /// <summary>
        /// Called when the player selects a stage.
        /// </summary>
        public bool SelectStage(int stageId, int staminaCost)
        {
            if (!_economy.CheckStamina(staminaCost))
                return false;

            _router.Push("team_select", new Dictionary<string, object>
            {
                { "stage_id", stageId },
                { "stamina_cost", staminaCost }
            });
            return true;
        }

        public void GoBack() => _router.Pop();

        private void BuildAllStages()
        {
            _allStages.Clear();
            var difficulties = new HashSet<string>();

            var definitions = _gameData.GetAllDefinitions("stages");
            foreach (var def in definitions)
            {
                string difficulty = def.GetString("difficulty", "normal");
                difficulties.Add(difficulty);
                int staminaCost = def.GetInt("stamina_cost", 10);

                var entry = new StageEntry
                {
                    StageId = def.Id,
                    Name = def.GetString("name", "Unknown Stage"),
                    Difficulty = difficulty,
                    StaminaCost = staminaCost,
                    CanAfford = _economy.CheckStamina(staminaCost),
                    TurnLimit = def.GetInt("turn_limit", 0),
                    BoardRows = def.GetInt("board_rows", 5),
                    BoardCols = def.GetInt("board_cols", 6),
                    IsDaily = def.GetInt("is_daily", 0) == 1,
                    DayOfWeek = def.GetInt("day_of_week", -1),
                };

                // Parse floor effects
                var effects = def.GetArray("floor_effects");
                if (effects != null)
                {
                    foreach (var e in effects)
                    {
                        if (e is string s) entry.FloorEffects.Add(s);
                        else if (e is Dictionary<string, object> d && d.ContainsKey("type"))
                            entry.FloorEffects.Add(d["type"].ToString());
                    }
                }

                // Count waves
                var waves = def.GetArray("waves");
                entry.WaveCount = waves?.Count ?? 0;

                _allStages.Add(entry);
            }

            var order = new[] { "normal", "expert", "mythical", "annihilation" };
            AvailableDifficulties = order.Where(d => difficulties.Contains(d)).ToList();
            if (AvailableDifficulties.Count == 0)
                AvailableDifficulties.Add("normal");
        }

        private void FilterByDifficulty(string difficulty)
        {
            Stages = _allStages.Where(s => s.Difficulty == difficulty).ToList();
        }

        private void RefreshAffordability()
        {
            foreach (var stage in _allStages)
                stage.CanAfford = _economy.CheckStamina(stage.StaminaCost);
            FilterByDifficulty(_activeDifficulty);
        }
    }
}
