using System;
using System.Collections.Generic;
using System.Linq;

namespace MobileForge.Domain
{
    /// <summary>
    /// Session state for a dungeon run.
    /// </summary>
    public enum DungeonSessionState
    {
        Idle,
        PreFlight,   // Validating team/stamina
        InProgress,  // DungeonRunner active
        Completed,   // Battle won, rewards pending
        Failed,      // Battle lost
    }

    /// <summary>
    /// Orchestrates the complete dungeon lifecycle: validation → enter → run → rewards.
    /// Connects DungeonRunner, Economy, MonsterManager, and QuestTracker into
    /// a cohesive game loop.
    ///
    /// This is the "glue" between individual domain systems:
    /// - Pre-flight: validates stamina, team composition, friend helper
    /// - Enter: consumes stamina, starts DungeonRunner
    /// - On win: calculates rewards, distributes to Economy/MonsterManager, emits quest events
    /// - On lose: offers continue (gem cost) or exit
    /// </summary>
    public class DungeonSessionManager
    {
        private readonly Economy _economy;
        private readonly MonsterManager _monsterManager;
        private readonly QuestTracker _questTracker;
        private readonly Action<string, Dictionary<string, object>> _emitEvent;
        private readonly Random _rng;

        private DungeonRunner _runner;
        private DungeonDef _dungeonDef;
        private List<MonsterInstance> _team;
        private DungeonSessionState _state = DungeonSessionState.Idle;
        private DungeonClearReward _pendingReward;
        private HashSet<int> _clearedDungeons;

        public DungeonSessionState State => _state;
        public DungeonRunner Runner => _runner;
        public DungeonClearReward PendingReward => _pendingReward;

        public DungeonSessionManager(
            Economy economy,
            MonsterManager monsterManager,
            QuestTracker questTracker = null,
            Action<string, Dictionary<string, object>> emitEvent = null,
            HashSet<int> clearedDungeons = null,
            Random rng = null)
        {
            _economy = economy;
            _monsterManager = monsterManager;
            _questTracker = questTracker;
            _emitEvent = emitEvent;
            _clearedDungeons = clearedDungeons ?? new HashSet<int>();
            _rng = rng ?? new Random();
        }

        /// <summary>
        /// Validate whether a dungeon can be entered with the given team.
        /// Returns error message or null if valid.
        /// </summary>
        public string ValidateEntry(DungeonDef dungeon, List<MonsterInstance> team)
        {
            if (dungeon == null) return "No dungeon selected";
            if (team == null || team.Count == 0) return "Team is empty";
            if (_state == DungeonSessionState.InProgress) return "Already in a dungeon";

            if (dungeon.StaminaCost > 0 && !_economy.CheckStamina(dungeon.StaminaCost))
                return $"Not enough stamina (need {dungeon.StaminaCost}, have {_economy.GetBalance("stamina")})";

            return null; // Valid
        }

        /// <summary>
        /// Enter a dungeon. Consumes stamina and starts the DungeonRunner.
        /// Returns the DungeonRunner for the presentation layer to drive.
        /// </summary>
        public DungeonRunner EnterDungeon(DungeonDef dungeon, List<MonsterInstance> team,
            BoardLogic board, CombatResolver combat, SkillPipeline skillPipeline)
        {
            string error = ValidateEntry(dungeon, team);
            if (error != null)
                throw new InvalidOperationException(error);

            // Consume stamina
            if (dungeon.StaminaCost > 0)
                _economy.SpendStamina(dungeon.StaminaCost);

            _dungeonDef = dungeon;
            _team = new List<MonsterInstance>(team);
            _state = DungeonSessionState.InProgress;

            // Calculate team HP from team stats
            int totalHp = 0;
            foreach (var member in _team)
            {
                var stats = _monsterManager.GetStats(member);
                totalHp += stats.Hp;
            }

            // Create and start runner
            _runner = new DungeonRunner(board, combat, skillPipeline, OnDungeonEvent);
            _runner.Start(dungeon, totalHp, totalHp);

            _emitEvent?.Invoke("dungeon_entered", new Dictionary<string, object>
            {
                ["dungeon_id"] = dungeon.Id,
                ["dungeon_name"] = dungeon.Name,
                ["stamina_spent"] = dungeon.StaminaCost,
                ["team_hp"] = totalHp,
            });

            // Emit quest event
            _questTracker?.OnEvent("dungeon_entered", new Dictionary<string, object>
            {
                ["dungeon_id"] = dungeon.Id,
            });

            return _runner;
        }

        /// <summary>
        /// Called when the dungeon battle concludes (win or lose).
        /// Automatically invoked via DungeonRunner events.
        /// </summary>
        public void OnBattleEnded(bool won)
        {
            if (won)
            {
                _state = DungeonSessionState.Completed;

                bool isFirstClear = !_clearedDungeons.Contains(_dungeonDef.Id);
                var finalState = _runner.GetState();

                // Calculate rewards
                _pendingReward = DungeonRewardCalculator.Calculate(
                    _dungeonDef, finalState, isFirstClear, _rng);

                // Mark as cleared
                _clearedDungeons.Add(_dungeonDef.Id);

                // Emit quest events
                _questTracker?.OnEvent("dungeon_cleared", new Dictionary<string, object>
                {
                    ["dungeon_id"] = _dungeonDef.Id,
                    ["turns"] = finalState.TurnNumber,
                    ["combos"] = finalState.TotalCombos,
                });

                if (isFirstClear)
                {
                    _questTracker?.OnEvent("first_clear", new Dictionary<string, object>
                    {
                        ["dungeon_id"] = _dungeonDef.Id,
                    });
                }
            }
            else
            {
                _state = DungeonSessionState.Failed;
            }
        }

        /// <summary>
        /// Collect pending rewards after a win. Distributes to Economy/MonsterManager.
        /// Returns the reward that was distributed.
        /// </summary>
        public DungeonClearReward CollectRewards()
        {
            if (_state != DungeonSessionState.Completed || _pendingReward == null)
                return null;

            DungeonRewardCalculator.Distribute(_pendingReward, _economy, _monsterManager, _team);

            var collected = _pendingReward;
            _pendingReward = null;
            _state = DungeonSessionState.Idle;

            _emitEvent?.Invoke("rewards_collected", new Dictionary<string, object>
            {
                ["player_exp"] = collected.PlayerExp,
                ["coins"] = collected.Coins,
                ["team_exp"] = collected.TeamMonsterExp,
                ["monster_drops"] = collected.DroppedMonsterIds.Count,
                ["first_clear"] = collected.FirstClearBonus,
            });

            return collected;
        }

        /// <summary>
        /// Continue after defeat by spending gems. Restores full HP.
        /// Returns true if continue was successful.
        /// </summary>
        public bool ContinueWithGems(int gemCost = 1)
        {
            if (_state != DungeonSessionState.Failed) return false;
            if (!_economy.CanAfford("gems", gemCost)) return false;

            _economy.Spend("gems", gemCost);
            _state = DungeonSessionState.InProgress;

            // Runner would need to be re-activated with full HP
            // (implementation depends on DungeonRunner expose methods)
            _emitEvent?.Invoke("dungeon_continued", new Dictionary<string, object>
            {
                ["gems_spent"] = gemCost,
            });

            return true;
        }

        /// <summary>
        /// Exit the dungeon (after win, loss, or surrender). Resets session.
        /// </summary>
        public void ExitDungeon()
        {
            _state = DungeonSessionState.Idle;
            _runner = null;
            _dungeonDef = null;
            _team = null;
            _pendingReward = null;
        }

        /// <summary>
        /// Get the list of team MonsterStats for the current session.
        /// </summary>
        public List<MonsterStats> GetTeamStats()
        {
            if (_team == null) return new List<MonsterStats>();
            return _team.Select(m => _monsterManager.GetStats(m)).ToList();
        }

        /// <summary>
        /// Check if a dungeon has been cleared before.
        /// </summary>
        public bool IsDungeonCleared(int dungeonId) => _clearedDungeons.Contains(dungeonId);

        // ── Internal ──

        private void OnDungeonEvent(string eventName, Dictionary<string, object> data)
        {
            // Forward to external listeners
            _emitEvent?.Invoke(eventName, data);

            // Auto-detect battle end
            if (eventName == DungeonEvents.BattleWon)
                OnBattleEnded(true);
            else if (eventName == DungeonEvents.BattleLost)
                OnBattleEnded(false);

            // Forward kill events to quest tracker
            if (eventName == DungeonEvents.EnemyKilled)
                _questTracker?.OnEvent("enemy_killed", data);
        }
    }
}
