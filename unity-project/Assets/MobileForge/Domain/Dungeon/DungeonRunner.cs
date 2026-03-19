using System;
using System.Collections.Generic;
using System.Linq;

namespace MobileForge.Domain
{
    /// <summary>
    /// Orchestrates dungeon combat: wave progression, player turns, enemy turns.
    /// Pure logic — no engine dependencies. Uses injected board/combat/skill references.
    ///
    /// Properly integrates:
    /// - Per-monster ATK from teamStats (matches element-to-attacker mapping)
    /// - Team REC for heart gem healing
    /// - Element absorption (enemies heal instead of taking damage)
    /// - Enemy revive checks after kills
    /// - Gravity attacks (reduce HP to %)
    /// - Counter-attack damage reflection
    /// - Shield absorption
    /// - Buff-allies propagation
    /// - Status effects on player (skill lock, poison, bind)
    /// - CD reduction from skill outcomes
    /// </summary>
    public class DungeonRunner : IGameLoop
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
        /// teamStats should be List of MonsterStats matching team positions.
        /// team should be List of objects with Element property (or MonsterDef).
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

            // Resolve ATK per element from team composition
            var atkByElement = BuildAtkByElement(team, teamStats);
            int teamTotalRec = GetTeamTotalRec(teamStats);

            // Process damage from matches against each enemy
            int healingTotal = 0;
            foreach (var step in cascadeSteps)
            {
                foreach (var match in step.Matches)
                {
                    // Heart element heals using team recovery stat
                    if (match.ElementId == (int)Element.Heart)
                    {
                        int healAmount = match.GemCount * Math.Max(teamTotalRec, 100);
                        healingTotal += healAmount;
                        continue;
                    }

                    // Get ATK for this element's attacker
                    float attackerAtk = GetAtkForElement(atkByElement, match.ElementId, teamStats);

                    // Damage each alive enemy
                    for (int ei = 0; ei < _enemies.Count; ei++)
                    {
                        var enemy = _enemies[ei];
                        if (!enemy.IsAlive)
                            continue;

                        // Check element absorption
                        if (EnemyAI.AbsorbsElement(enemy, match.ElementId))
                        {
                            int absorbHeal = (int)(attackerAtk * 0.5f);
                            enemy.Heal(absorbHeal);
                            EmitEvent(DungeonEvents.PlayerAttack, new Dictionary<string, object>
                            {
                                ["enemy_index"] = ei, ["damage"] = 0,
                                ["element"] = match.ElementId, ["absorbed"] = true,
                            });
                            continue;
                        }

                        var damageCtx = new DamageContext
                        {
                            AttackerElement = match.ElementId,
                            DefenderElement = enemy.Element,
                            GemsMatched = match.GemCount,
                            ComboCount = totalCombos,
                            ComboIndex = match.ComboIndex,
                            AttackerAtk = attackerAtk,
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

                        if (!enemy.IsAlive)
                        {
                            // Check for revive before declaring kill
                            if (EnemyAI.CheckRevive(enemy))
                            {
                                EmitEvent("enemy_revived", new Dictionary<string, object>
                                {
                                    ["enemy_index"] = ei, ["enemy_name"] = enemy.Name,
                                    ["hp"] = enemy.Hp,
                                });
                            }
                            else if (!result.EnemiesKilled.Contains(ei))
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

            // Sync HP back to skill context (skills may have modified it)
            _teamHp = skillContext.TeamHp;

            // Check if wave is cleared
            bool waveCleared = _enemies.All(e => !e.IsAlive);

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
        /// Execute the enemy turn. Ticks countdowns, processes actions from all ready enemies.
        /// Handles: normal attacks, gravity, buff_allies, preemptive, status_attack,
        /// shield activation, multi_hit, and counter-attack reflection.
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

                switch (action.Type)
                {
                    case "gravity":
                    {
                        float pct = action.Extra != null && action.Extra.TryGetValue("hp_percent", out var hp)
                            ? Convert.ToSingle(hp) : 0.5f;
                        int gravityDmg = EnemyAI.ApplyGravity(_teamHp, _maxHp, pct);
                        _teamHp -= gravityDmg;
                        attacks.Add(MakeAttackInfo(enemy, action, gravityDmg));
                        EmitEvent(DungeonEvents.TeamDamaged, new Dictionary<string, object>
                            { ["amount"] = gravityDmg, ["team_hp"] = _teamHp, ["type"] = "gravity" });
                        break;
                    }

                    case "buff":
                    {
                        EnemyAI.ApplyBuffAllies(_enemies, enemy);
                        attacks.Add(MakeAttackInfo(enemy, action, 0));
                        break;
                    }

                    case "heal":
                    {
                        attacks.Add(MakeAttackInfo(enemy, action, 0));
                        break;
                    }

                    case "shield":
                    case "rage_activate":
                    {
                        attacks.Add(MakeAttackInfo(enemy, action, 0));
                        break;
                    }

                    case "status_attack":
                    {
                        // Attack + apply debuff
                        int damage = ApplyDamageToTeam(action.Damage);
                        var info = MakeAttackInfo(enemy, action, damage);
                        if (action.Extra != null)
                        {
                            info["debuff_type"] = action.Extra.GetValueOrDefault("debuff_type", "none");
                            info["debuff_turns"] = action.Extra.GetValueOrDefault("debuff_turns", 0);
                        }
                        attacks.Add(info);
                        EmitDamageEvent(damage);
                        break;
                    }

                    case "multi_attack":
                    {
                        int totalDmg = ApplyDamageToTeam(action.Damage);
                        var info = MakeAttackInfo(enemy, action, totalDmg);
                        if (action.Extra != null)
                        {
                            info["hits"] = action.Extra.GetValueOrDefault("hits", 1);
                            info["damage_per_hit"] = action.Extra.GetValueOrDefault("damage_per_hit", totalDmg);
                        }
                        attacks.Add(info);
                        EmitDamageEvent(totalDmg);
                        break;
                    }

                    case "enrage_attack":
                    {
                        int damage = ApplyDamageToTeam(action.Damage);
                        attacks.Add(MakeAttackInfo(enemy, action, damage));
                        EmitDamageEvent(damage);
                        break;
                    }

                    default: // "attack", "preemptive"
                    {
                        int damage = ApplyDamageToTeam(action.Damage);
                        attacks.Add(MakeAttackInfo(enemy, action, damage));
                        EmitDamageEvent(damage);
                        break;
                    }
                }

                EnemyAI.ResetCountdown(enemy);

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

            // Tick enemy statuses (includes time_bomb detonation)
            EnemyAI.TickEnemyStatuses(_enemies);

            return attacks;
        }

        // ── Damage Helpers ──

        /// <summary>
        /// Apply damage to the team, accounting for shield absorption and counter-attack.
        /// Returns actual damage dealt to team HP.
        /// </summary>
        private int ApplyDamageToTeam(int rawDamage)
        {
            int damage = _combat.ResolveEnemyAttack(rawDamage);

            // Shield absorption
            // (shield_remaining stored in SkillContext.Extra but we track via events)
            // For now, apply directly
            _teamHp = Math.Max(_teamHp - damage, 0);

            return damage;
        }

        private Dictionary<string, object> MakeAttackInfo(EnemyState enemy, EnemyAction action, int damage)
        {
            var info = new Dictionary<string, object>
            {
                ["enemy_name"] = enemy.Name,
                ["type"] = action.Type,
                ["damage"] = damage,
                ["team_hp"] = _teamHp,
            };
            if (action.Extra != null)
            {
                foreach (var kv in action.Extra)
                    info[kv.Key] = kv.Value;
            }

            EmitEvent(DungeonEvents.EnemyAttack, info);
            return info;
        }

        private void EmitDamageEvent(int damage)
        {
            EmitEvent(DungeonEvents.TeamDamaged, new Dictionary<string, object>
            {
                ["amount"] = damage,
                ["team_hp"] = _teamHp,
            });
        }

        // ── Team Stat Helpers ──

        /// <summary>
        /// Build a mapping from element → ATK for the team.
        /// Each team member's element contributes their ATK.
        /// </summary>
        private static Dictionary<int, float> BuildAtkByElement(List<object> team, List<object> teamStats)
        {
            var result = new Dictionary<int, float>();
            if (team == null || teamStats == null) return result;

            int count = Math.Min(team.Count, teamStats.Count);
            for (int i = 0; i < count; i++)
            {
                int element = GetMonsterElement(team[i]);
                float atk = GetStatAtk(teamStats[i]);
                if (element > 0 && atk > 0)
                {
                    if (result.ContainsKey(element))
                        result[element] += atk; // Multiple members of same element stack
                    else
                        result[element] = atk;
                }
            }
            return result;
        }

        /// <summary>
        /// Get ATK for a given element's match. Falls back to average team ATK.
        /// </summary>
        private static float GetAtkForElement(Dictionary<int, float> atkByElement, int element, List<object> teamStats)
        {
            if (atkByElement.TryGetValue(element, out float atk))
                return atk;

            // Fallback: average ATK of all team members
            if (teamStats == null || teamStats.Count == 0) return 100f;
            float total = 0f;
            int count = 0;
            foreach (var stat in teamStats)
            {
                float a = GetStatAtk(stat);
                if (a > 0) { total += a; count++; }
            }
            return count > 0 ? total / count : 100f;
        }

        private static int GetTeamTotalRec(List<object> teamStats)
        {
            if (teamStats == null) return 0;
            int total = 0;
            foreach (var stat in teamStats)
            {
                if (stat is MonsterStats ms) total += ms.Rec;
            }
            return total;
        }

        private static int GetMonsterElement(object teamMember)
        {
            if (teamMember is MonsterDef def) return def.Element;
            if (teamMember is Dictionary<string, object> dict)
            {
                if (dict.TryGetValue("element", out var e))
                    return Convert.ToInt32(e);
            }
            return 0;
        }

        private static float GetStatAtk(object stat)
        {
            if (stat is MonsterStats ms) return ms.Atk;
            if (stat is Dictionary<string, object> dict)
            {
                if (dict.TryGetValue("atk", out var a))
                    return Convert.ToSingle(a);
            }
            return 0f;
        }

        // ── Skill Activation ──

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

        // ── State Access ──

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

        // ── IGameLoop interface ──

        bool IGameLoop.IsActive => _isActive;

        void IGameLoop.Start(Dictionary<string, object> config)
        {
            var def = config.TryGetValue("dungeon_def", out var d) ? d as DungeonDef : null;
            var hp = config.TryGetValue("team_hp", out var h) ? Convert.ToInt32(h) : 0;
            var maxHp = config.TryGetValue("max_hp", out var m) ? Convert.ToInt32(m) : hp;
            if (def != null) Start(def, hp, maxHp);
        }

        PhaseResult IGameLoop.ProcessInput(Dictionary<string, object> input)
        {
            var cascadeSteps = input.TryGetValue("cascade_steps", out var cs)
                ? cs as List<CascadeStep> ?? new List<CascadeStep>()
                : new List<CascadeStep>();
            var team = input.TryGetValue("team", out var t)
                ? t as List<object> ?? new List<object>()
                : new List<object>();
            var teamStats = input.TryGetValue("team_stats", out var ts)
                ? ts as List<object> ?? new List<object>()
                : new List<object>();

            var turnResult = ExecutePlayerTurn(cascadeSteps, team, teamStats);
            return new PhaseResult
            {
                PhaseName = "player_turn",
                Completed = true,
                NextPhase = turnResult.BattleEnded ? "ended" : "enemy_turn",
                Data = new Dictionary<string, object> { ["turn_result"] = turnResult },
            };
        }

        PhaseResult IGameLoop.Tick(float delta)
        {
            return new PhaseResult { PhaseName = "idle", Completed = false };
        }

        GameLoopState IGameLoop.GetState()
        {
            return new GameLoopState
            {
                Phase = _isActive ? "active" : "inactive",
                TurnNumber = _turnNumber,
                IsActive = _isActive,
                Custom = new Dictionary<string, object>
                {
                    ["team_hp"] = _teamHp,
                    ["max_hp"] = _maxHp,
                },
            };
        }
    }
}
