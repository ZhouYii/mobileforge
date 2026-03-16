using System;
using System.Collections.Generic;

namespace MobileForge.Domain
{
    /// <summary>
    /// Orchestrates dungeon combat: wave progression, player turns, enemy turns.
    /// Pure logic — no engine dependencies. Uses injected board/combat/skill references.
    /// </summary>
    public class DungeonRunner
    {
        private readonly BoardLogic _board;
        private readonly CombatResolver _combat;
        private readonly SkillPipeline _skillPipeline;
        private readonly Action<string, Dictionary<string, object>> _emitEvent;

        private DungeonDef _dungeonDef;
        private int _currentWaveIndex;
        private List<EnemyState> _enemies;
        private int _teamHp;
        private int _maxHp;
        private int _turnNumber;
        private int _totalCombos;
        private bool _isActive;

        public DungeonRunner(
            BoardLogic board,
            CombatResolver combat,
            SkillPipeline skillPipeline,
            Action<string, Dictionary<string, object>> emitEvent = null)
        {
            _board = board;
            _combat = combat;
            _skillPipeline = skillPipeline;
            _emitEvent = emitEvent;
        }

        /// <summary>
        /// Start a dungeon run.
        /// </summary>
        public void Start(DungeonDef dungeonDef, int teamHp, int maxHp)
        {
            _dungeonDef = dungeonDef;
            _teamHp = teamHp;
            _maxHp = maxHp;
            _turnNumber = 0;
            _totalCombos = 0;
            _currentWaveIndex = 0;
            _isActive = true;

            SpawnWave(_currentWaveIndex);

            EmitEvent(DungeonEvents.DungeonStarted, new Dictionary<string, object>
            {
                ["dungeon_id"] = _dungeonDef.Id,
                ["dungeon_name"] = _dungeonDef.Name,
            });
        }

        /// <summary>
        /// Execute the player's turn given cascade results from board resolution.
        /// </summary>
        public TurnResult ExecutePlayerTurn(List<CascadeStep> cascadeSteps, List<object> team, List<object> teamStats)
        {
            var result = new TurnResult();
            if (!_isActive)
                return result;

            _turnNumber++;

            // Build skill context
            var skillContext = new SkillContext
            {
                Team = team ?? new List<object>(),
                TeamStats = teamStats ?? new List<object>(),
                Board = _board,
                Combat = _combat,
                Enemies = new List<object>(),
                TeamHp = _teamHp,
                MaxHp = _maxHp,
                TurnNumber = _turnNumber,
            };
            foreach (var enemy in _enemies)
                skillContext.Enemies.Add(enemy);

            // Process turn start for persistent skill outcomes
            _skillPipeline.ProcessTurnStart(skillContext);

            EmitEvent(DungeonEvents.TurnStarted, new Dictionary<string, object>
            {
                ["turn_number"] = _turnNumber,
            });

            // Calculate total combo count and elements matched
            int totalCombos = 0;
            var elementsMatched = new Dictionary<int, int>();
            foreach (var step in cascadeSteps)
            {
                foreach (var match in step.Matches)
                {
                    totalCombos++;
                    if (elementsMatched.ContainsKey(match.ElementId))
                        elementsMatched[match.ElementId] += match.GemCount;
                    else
                        elementsMatched[match.ElementId] = match.GemCount;
                }
            }
            _totalCombos += totalCombos;

            skillContext.ComboCount = totalCombos;
            skillContext.ElementsMatched = elementsMatched;

            // Process damage from matches against each enemy
            int healingTotal = 0;
            foreach (var step in cascadeSteps)
            {
                foreach (var match in step.Matches)
                {
                    // Heart element heals instead of dealing damage
                    if (match.ElementId == (int)Element.Heart)
                    {
                        // Simple healing: gem_count * team_recovery (placeholder: use gem count * 100)
                        int healAmount = match.GemCount * 100;
                        healingTotal += healAmount;
                        continue;
                    }

                    // Damage each alive enemy
                    for (int ei = 0; ei < _enemies.Count; ei++)
                    {
                        var enemy = _enemies[ei];
                        if (!enemy.IsAlive)
                            continue;

                        var damageCtx = new DamageContext
                        {
                            AttackerElement = match.ElementId,
                            DefenderElement = enemy.Element,
                            GemsMatched = match.GemCount,
                            ComboCount = totalCombos,
                            ComboIndex = match.ComboIndex,
                            AttackerAtk = 100f, // Placeholder — game layer sets actual ATK
                            DefenderDefense = enemy.Defense,
                        };

                        var dmgResult = _combat.ResolvePlayerAttack(damageCtx);
                        int dealt = enemy.TakeDamage(dmgResult.FinalDamage);

                        if (result.DamagePerEnemy.ContainsKey(ei))
                            result.DamagePerEnemy[ei] += dealt;
                        else
                            result.DamagePerEnemy[ei] = dealt;

                        EmitEvent(DungeonEvents.PlayerAttack, new Dictionary<string, object>
                        {
                            ["enemy_index"] = ei,
                            ["damage"] = dealt,
                            ["element"] = match.ElementId,
                        });

                        if (!enemy.IsAlive && !result.EnemiesKilled.Contains(ei))
                        {
                            result.EnemiesKilled.Add(ei);
                            EmitEvent(DungeonEvents.EnemyKilled, new Dictionary<string, object>
                            {
                                ["enemy_index"] = ei,
                                ["enemy_name"] = enemy.Name,
                            });
                        }
                    }
                }
            }

            // Apply healing
            if (healingTotal > 0)
            {
                _teamHp = Math.Min(_teamHp + healingTotal, _maxHp);
                result.Healing = healingTotal;
                EmitEvent(DungeonEvents.TeamHealed, new Dictionary<string, object>
                {
                    ["amount"] = healingTotal,
                    ["team_hp"] = _teamHp,
                });
            }

            // Check if wave is cleared
            bool waveCleared = true;
            foreach (var enemy in _enemies)
            {
                if (enemy.IsAlive)
                {
                    waveCleared = false;
                    break;
                }
            }

            if (waveCleared)
            {
                result.WaveCleared = true;
                EmitEvent(DungeonEvents.WaveCleared, new Dictionary<string, object>
                {
                    ["wave_index"] = _currentWaveIndex,
                });

                _currentWaveIndex++;
                if (_currentWaveIndex >= _dungeonDef.Waves.Count)
                {
                    // Dungeon won
                    result.BattleEnded = true;
                    result.BattleWon = true;
                    result.Rewards = new Dictionary<string, object>(_dungeonDef.Rewards);
                    _isActive = false;
                    EmitEvent(DungeonEvents.BattleWon, new Dictionary<string, object>
                    {
                        ["rewards"] = _dungeonDef.Rewards,
                    });
                    EmitEvent(DungeonEvents.DungeonCompleted, new Dictionary<string, object>
                    {
                        ["dungeon_id"] = _dungeonDef.Id,
                        ["turns"] = _turnNumber,
                        ["total_combos"] = _totalCombos,
                    });
                }
                else
                {
                    SpawnWave(_currentWaveIndex);
                }
            }

            // Process turn end for persistent skill outcomes
            _skillPipeline.ProcessTurnEnd(skillContext);

            EmitEvent(DungeonEvents.TurnEnded, new Dictionary<string, object>
            {
                ["turn_number"] = _turnNumber,
            });

            return result;
        }

        /// <summary>
        /// Execute the enemy turn. Ticks countdowns, attacks with ready enemies.
        /// Returns a list of attack dictionaries.
        /// </summary>
        public List<Dictionary<string, object>> ExecuteEnemyTurn()
        {
            var attacks = new List<Dictionary<string, object>>();
            if (!_isActive)
                return attacks;

            var readyEnemies = EnemyAI.TickCountdowns(_enemies);

            foreach (var enemy in readyEnemies)
            {
                if (!EnemyAI.CanAttack(enemy))
                    continue;

                var action = EnemyAI.DecideAction(enemy);
                int damage = _combat.ResolveEnemyAttack(action.Damage);

                _teamHp = Math.Max(_teamHp - damage, 0);
                EnemyAI.ResetCountdown(enemy);

                var attackInfo = new Dictionary<string, object>
                {
                    ["enemy_name"] = enemy.Name,
                    ["type"] = action.Type,
                    ["damage"] = damage,
                    ["team_hp"] = _teamHp,
                };
                attacks.Add(attackInfo);

                EmitEvent(DungeonEvents.EnemyAttack, attackInfo);
                EmitEvent(DungeonEvents.TeamDamaged, new Dictionary<string, object>
                {
                    ["amount"] = damage,
                    ["team_hp"] = _teamHp,
                });

                if (_teamHp <= 0)
                {
                    _isActive = false;
                    EmitEvent(DungeonEvents.BattleLost, new Dictionary<string, object>
                    {
                        ["turn_number"] = _turnNumber,
                    });
                    break;
                }
            }

            // Tick enemy statuses
            EnemyAI.TickEnemyStatuses(_enemies);

            return attacks;
        }

        /// <summary>
        /// Activate a skill during the dungeon.
        /// </summary>
        public SkillResult ActivateSkill(SkillDef skillDef, SkillContext context)
        {
            var result = _skillPipeline.ActivateSkill(skillDef, context);
            EmitEvent(DungeonEvents.SkillActivated, new Dictionary<string, object>
            {
                ["skill_id"] = skillDef.Id,
                ["skill_name"] = skillDef.Name,
            });
            return result;
        }

        /// <summary>
        /// Get a snapshot of the current dungeon state.
        /// </summary>
        public DungeonState GetState()
        {
            return new DungeonState
            {
                DungeonDef = _dungeonDef,
                CurrentWaveIndex = _currentWaveIndex,
                Enemies = _enemies != null ? new List<EnemyState>(_enemies) : new List<EnemyState>(),
                TeamHp = _teamHp,
                MaxHp = _maxHp,
                TurnNumber = _turnNumber,
                TotalCombos = _totalCombos,
                IsActive = _isActive,
            };
        }

        private void SpawnWave(int waveIndex)
        {
            _enemies = new List<EnemyState>();
            if (_dungeonDef.Waves.Count > waveIndex)
            {
                var waveDef = _dungeonDef.Waves[waveIndex];
                foreach (var enemyData in waveDef.Enemies)
                {
                    _enemies.Add(new EnemyState(enemyData));
                }
            }

            EmitEvent(DungeonEvents.WaveStarted, new Dictionary<string, object>
            {
                ["wave_index"] = waveIndex,
                ["enemy_count"] = _enemies.Count,
            });
        }

        private void EmitEvent(string eventName, Dictionary<string, object> data)
        {
            _emitEvent?.Invoke(eventName, data);
        }
    }
}
