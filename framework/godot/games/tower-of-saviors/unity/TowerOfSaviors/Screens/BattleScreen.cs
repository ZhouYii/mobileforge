using System;
using System.Collections.Generic;
using MobileForge.Infrastructure;
using MobileForge.Domain;
using MobileForge.Presentation;

namespace TowerOfSaviors
{
    /// <summary>
    /// Main battle screen. Orchestrates the battle loop using framework modules.
    /// Mirrors battle_screen.gd — creates BoardLogic, CombatResolver, DungeonRunner,
    /// and handles turn resolution, win/loss checks, and navigation to result.
    /// </summary>
    public class BattleScreen : IScreen
    {
        private GameData _gameData;
        private MonsterManager _monsterManager;
        private SkillPipeline _skillPipeline;
        private Economy _economy;
        private EventBus _eventBus;
        private UIRouter _router;
        private Dictionary<string, object> _params;

        private BoardLogic _board;
        private CombatResolver _combat;
        private DungeonRunner _dungeonRunner;

        /// <summary>
        /// The board logic instance, exposed for UI rendering.
        /// </summary>
        public BoardLogic Board => _board;

        /// <summary>
        /// Current dungeon state snapshot, refreshed after each action.
        /// </summary>
        public DungeonState State { get; private set; }

        public void Setup(GameData gameData, MonsterManager monsterManager,
            SkillPipeline skillPipeline, Economy economy, EventBus eventBus,
            UIRouter router, Dictionary<string, object> parameters)
        {
            _gameData = gameData;
            _monsterManager = monsterManager;
            _skillPipeline = skillPipeline;
            _economy = economy;
            _eventBus = eventBus;
            _router = router;
            _params = parameters ?? new Dictionary<string, object>();
        }

        public void OnEnter(Dictionary<string, object> parameters)
        {
            // Spend stamina
            int staminaCost = GetIntParam("stamina_cost", 10);
            _economy.SpendStamina(staminaCost);

            // Create board (5 rows x 6 columns, matching ToS)
            var config = new BoardConfig(5, 6);
            _board = new BoardLogic(config);
            _board.InitBoard();

            // Create combat resolver with element chart
            var chart = new ElementChart();
            _combat = new CombatResolver(chart);

            // Create dungeon runner, wiring events through the EventBus
            _dungeonRunner = new DungeonRunner(
                _board,
                _combat,
                _skillPipeline,
                (eventName, data) => _eventBus.Emit(eventName, data)
            );

            // Load dungeon definition and start
            int stageId = GetIntParam("stage_id", 1);
            var stageDef = _gameData.GetDefinition("stages", stageId);
            if (stageDef != null)
            {
                var dungeonDef = new DungeonDef(stageDef.Raw());
                // Simplified: use fixed team HP values (game layer calculates from team)
                _dungeonRunner.Start(dungeonDef, 10000, 10000);
            }

            // Evaluate and apply team skills
            ApplyTeamSkills();

            RefreshState();
        }

        public void OnPause() { }
        public void OnResume() { }
        public void OnExit() { }

        /// <summary>
        /// Auto-resolve a turn: run cascade, execute player turn, execute enemy turn.
        /// Called when the player presses the resolve button or completes a gem move.
        /// Returns the TurnResult for the UI to animate.
        /// </summary>
        public TurnResult ResolveTurn()
        {
            // Run cascade on current board
            var cascadeSteps = CascadeResolver.Resolve(_board);

            if (cascadeSteps.Count == 0)
            {
                // No matches — shuffle board
                _board.InitBoard();
                RefreshState();
                return null;
            }

            // Execute player turn (simplified: empty team for now)
            var team = new List<object>();
            var teamStats = new List<object>();
            var result = _dungeonRunner.ExecutePlayerTurn(cascadeSteps, team, teamStats);

            // Execute enemy turn if battle continues and wave not cleared
            if (State.IsActive && !result.WaveCleared)
            {
                _dungeonRunner.ExecuteEnemyTurn();
            }

            RefreshState();

            // Check battle end
            if (!State.IsActive)
            {
                if (result.BattleWon)
                {
                    _router.Navigate("result", new Dictionary<string, object>
                    {
                        { "won", true },
                        { "rewards", result.Rewards }
                    });
                }
                else
                {
                    _router.Navigate("result", new Dictionary<string, object>
                    {
                        { "won", false }
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Get element array for rendering the board grid.
        /// Returns a flat int[] of size rows*cols with element IDs.
        /// </summary>
        public int[] GetBoardElements()
        {
            return _board?.ToElementArray() ?? Array.Empty<int>();
        }

        /// <summary>
        /// Evaluate team skill definitions and apply activated stat multipliers.
        /// </summary>
        private void ApplyTeamSkills()
        {
            // Get team IDs from params
            List<int> teamIds = null;
            if (_params.TryGetValue("team_ids", out var idsObj) && idsObj is List<int> ids)
                teamIds = ids;

            if (teamIds == null || teamIds.Count == 0)
                return;

            // Build team data for skill context
            var teamData = new List<object>();
            foreach (int mid in teamIds)
            {
                var def = _gameData.GetDefinition("monsters", mid);
                teamData.Add(def?.Raw() ?? new Dictionary<string, object>());
            }

            // Get all team skill definitions
            var teamSkillDefs = _gameData.GetAllDefinitions("team_skills");
            if (teamSkillDefs == null || teamSkillDefs.Count == 0)
                return;

            // Build context with team info
            var ctx = new SkillContext { Team = teamData };

            foreach (var tsDef in teamSkillDefs)
            {
                var raw = tsDef.Raw();
                var skillDef = _skillPipeline.LoadSkillDef(raw);
                var result = _skillPipeline.ActivateSkill(skillDef, ctx);

                // Buffs applied will be tracked in the skill result
                // The combat hooks registered by persistent outcomes will
                // automatically apply during damage resolution
            }
        }

        private void RefreshState()
        {
            State = _dungeonRunner?.GetState();
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
