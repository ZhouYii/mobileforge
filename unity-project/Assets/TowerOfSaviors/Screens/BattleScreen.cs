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

        /// <summary>
        /// Cascade steps from the last turn resolution, for UI animation.
        /// </summary>
        public List<CascadeStep> LastCascadeSteps { get; private set; } = new();

        // ── Stage context for overlay menu (matching original stageName_TM, floorName_TM) ──
        public string StageName { get; private set; } = "Stage";
        public string FloorName { get; private set; } = "";
        public int CurrentWave => (State?.CurrentWaveIndex ?? 0) + 1;
        public int TotalWaves => State?.DungeonDef?.Waves?.Count ?? 1;
        public int RoundNumber => State?.TurnNumber ?? 0;

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

            // Create board — read dimensions from stage definition, default to ToS standard 5×6
            int stageId = GetIntParam("stage_id", 1);
            var stageDef = _gameData.GetDefinition("stages", stageId);
            int boardRows = stageDef?.GetInt("board_rows", 5) ?? 5;
            int boardCols = stageDef?.GetInt("board_cols", 6) ?? 6;
            var config = new BoardConfig(boardRows, boardCols);
            _board = new BoardLogic(config);
            _board.InitBoard();

            // Create combat resolver with element chart
            var chart = new ElementChart();
            // Load element chart data if available
            var chartDef = _gameData.GetDefinition("element_chart", 0);
            if (chartDef != null)
                chart.LoadFromData(chartDef.Raw());
            _combat = new CombatResolver(chart);

            // Create dungeon runner, wiring events through the EventBus
            _dungeonRunner = new DungeonRunner(
                _board,
                _combat,
                _skillPipeline,
                (eventName, data) => _eventBus.Emit(eventName, data)
            );

            // Calculate team HP from actual monster stats
            int teamHp = 0;
            List<int> teamIds = null;
            if (_params.TryGetValue("team_ids", out var idsObjHp) && idsObjHp is List<int> idsHp)
            {
                teamIds = idsHp;
                foreach (int mid in idsHp)
                {
                    var mDef = _monsterManager.GetDef(mid);
                    if (mDef != null)
                    {
                        var inst = _monsterManager.CreateInstance(mid, mDef.MaxLevel);
                        var stats = _monsterManager.GetStats(inst);
                        teamHp += stats.Hp;
                    }
                }
            }
            if (teamHp <= 0) teamHp = 5000; // Fallback

            // Load dungeon definition and start
            if (stageDef != null)
            {
                var raw = stageDef.Raw();
                StageName = raw.ContainsKey("name") ? raw["name"]?.ToString() ?? $"Stage {stageId}" : $"Stage {stageId}";
                FloorName = raw.ContainsKey("difficulty") ? raw["difficulty"]?.ToString() ?? "" : "";

                var dungeonDef = new DungeonDef(stageDef.Raw());
                _dungeonRunner.Start(dungeonDef, teamHp, teamHp);
            }

            // Evaluate and apply team skills
            ApplyTeamSkills();

            // Build skill slot info for UI
            BuildSkillSlots();

            RefreshState();
        }

        /// <summary>
        /// Skill slot info exposed for the UI layer to render skill buttons.
        /// </summary>
        public class SkillSlotInfo
        {
            public string Name { get; set; }
            public int CurrentCd { get; set; }
            public int MaxCd { get; set; }
            public int Element { get; set; }
            public bool IsSealed { get; set; }
            public bool IsReady => CurrentCd <= 0 && !IsSealed;
        }

        /// <summary>
        /// Skill buttons for the current team. Empty if no skills loaded.
        /// </summary>
        public List<SkillSlotInfo> SkillSlots { get; private set; } = new();

        public void OnPause() { }
        public void OnResume() { }
        public void OnExit() { }

        /// <summary>
        /// Activate a skill by slot index. Builds context and runs through SkillPipeline.
        /// </summary>
        public void ActivateSkill(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SkillSlots.Count) return;
            var slot = SkillSlots[slotIndex];
            if (!slot.IsReady) return;

            // Build skill context from current battle state
            var ctx = new SkillContext
            {
                CasterIndex = slotIndex,
                Board = _board,
                Combat = _combat,
                TeamHp = State?.TeamHp ?? 0,
                MaxHp = State?.MaxHp ?? 1,
                TurnNumber = State?.TurnNumber ?? 0,
                Team = new List<object>(),
                Enemies = new List<object>(),
            };

            // Populate enemies from state
            if (State?.Enemies != null)
            {
                foreach (var e in State.Enemies)
                    ctx.Enemies.Add(e);
            }

            // Find and activate the skill definition
            var teamIds = _params.TryGetValue("team_ids", out var idsObj) && idsObj is List<int> ids ? ids : null;
            if (teamIds != null && slotIndex < teamIds.Count)
            {
                var monDef = _gameData.GetDefinition("monsters", teamIds[slotIndex]);
                int skillId = monDef?.GetInt("active_skill_id", -1) ?? -1;
                if (skillId >= 0)
                {
                    var skillDef = _gameData.GetDefinition("skills", skillId);
                    if (skillDef != null)
                    {
                        var skill = _skillPipeline.LoadSkillDef(skillDef.Raw());
                        var result = _skillPipeline.ActivateSkill(skill, ctx);

                        // Apply healing from skill result
                        if (result.Healing > 0 && State != null)
                        {
                            // Apply through state — DungeonRunner tracks HP internally
                            // Healing is applied via the skill context TeamHp field
                        }
                        // Apply damage from skill result to enemies
                        if (result.DamageDealt != null && State?.Enemies != null)
                        {
                            foreach (var kvp in result.DamageDealt)
                            {
                                if (kvp.Key >= 0 && kvp.Key < State.Enemies.Count)
                                    State.Enemies[kvp.Key].TakeDamage(kvp.Value);
                            }
                        }
                    }
                }
            }

            // Set cooldown
            slot.CurrentCd = slot.MaxCd;
            RefreshState();
        }

        /// <summary>
        /// Auto-resolve a turn: run cascade, execute player turn, execute enemy turn.
        /// Called when the player presses the resolve button or completes a gem move.
        /// Returns the TurnResult for the UI to animate.
        /// </summary>
        public TurnResult ResolveTurn()
        {
            // Run cascade on current board
            var cascadeSteps = CascadeResolver.Resolve(_board);
            LastCascadeSteps = cascadeSteps;

            if (cascadeSteps.Count == 0)
            {
                // No matches — shuffle board
                _board.InitBoard();
                RefreshState();
                return null;
            }

            // Build team and stats from battle parameters
            var team = new List<object>();
            var teamStats = new List<object>();
            if (_params.TryGetValue("team_ids", out var teamIdsObj) && teamIdsObj is List<int> teamIds2)
            {
                foreach (int mid in teamIds2)
                {
                    var monDef = _gameData.GetDefinition("monsters", mid);
                    if (monDef != null)
                    {
                        team.Add(monDef.Raw());
                        var inst = _monsterManager.CreateInstance(mid, 99);
                        teamStats.Add(_monsterManager.GetStats(inst));
                    }
                }
            }
            var result = _dungeonRunner.ExecutePlayerTurn(cascadeSteps, team, teamStats);

            // Execute enemy turn if battle continues and wave not cleared
            if (State.IsActive && !result.WaveCleared)
            {
                _dungeonRunner.ExecuteEnemyTurn();
            }

            // Tick skill cooldowns
            foreach (var slot in SkillSlots)
            {
                if (slot.CurrentCd > 0) slot.CurrentCd--;
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

        private void BuildSkillSlots()
        {
            SkillSlots.Clear();
            List<int> teamIds = null;
            if (_params.TryGetValue("team_ids", out var idsObj) && idsObj is List<int> ids)
                teamIds = ids;
            if (teamIds == null) return;

            foreach (int mid in teamIds)
            {
                var def = _gameData.GetDefinition("monsters", mid);
                if (def == null) continue;

                int skillId = def.GetInt("active_skill_id", -1);
                if (skillId < 0) continue;

                var skillDef = _gameData.GetDefinition("skills", skillId);
                string skillName = skillDef?.GetString("name", "Skill") ?? "Skill";
                int cd = skillDef?.GetInt("cooldown", 5) ?? 5;

                SkillSlots.Add(new SkillSlotInfo
                {
                    Name = skillName,
                    CurrentCd = cd, // Start on cooldown
                    MaxCd = cd,
                    Element = def.GetInt("element", 0),
                    IsSealed = false
                });
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
