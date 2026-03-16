using System;
using System.Collections.Generic;
using NUnit.Framework;
using MobileForge.Domain;

namespace MobileForge.Tests.Domain
{
    /// <summary>
    /// Tests for DungeonRunner -- the main dungeon orchestrator.
    /// DungeonRunner does not yet exist as a source file; these tests define
    /// the expected API contract (test-first development).
    /// </summary>
    [TestFixture]
    public class DungeonRunnerTests
    {
        private DungeonRunner _runner;
        private BoardLogic _board;
        private CombatResolver _combat;
        private SkillPipeline _skillPipeline;
        private List<Dictionary<string, object>> _capturedEvents;

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

        private void CaptureEvent(string eventName, Dictionary<string, object> data)
        {
            _capturedEvents.Add(new Dictionary<string, object>
            {
                ["event"] = eventName,
                ["data"] = data,
            });
        }

        private static Dictionary<string, object> MakeDungeonDef()
        {
            // 2 waves: wave 0 has 2 weak enemies, wave 1 has 1 stronger enemy
            return new Dictionary<string, object>
            {
                ["id"] = 100,
                ["name"] = "Test Dungeon",
                ["stamina_cost"] = 10,
                ["waves"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["enemies"] = new List<object>
                        {
                            new Dictionary<string, object>
                            {
                                ["id"] = 0, ["name"] = "Slime A", ["element"] = 1,
                                ["hp"] = 100, ["atk"] = 10, ["defense"] = 0, ["countdown"] = 2,
                                ["max_countdown"] = 2,
                            },
                            new Dictionary<string, object>
                            {
                                ["id"] = 1, ["name"] = "Slime B", ["element"] = 2,
                                ["hp"] = 100, ["atk"] = 15, ["defense"] = 0, ["countdown"] = 3,
                                ["max_countdown"] = 3,
                            },
                        }
                    },
                    new Dictionary<string, object>
                    {
                        ["enemies"] = new List<object>
                        {
                            new Dictionary<string, object>
                            {
                                ["id"] = 2, ["name"] = "Boss", ["element"] = 3,
                                ["hp"] = 500, ["atk"] = 50, ["defense"] = 10, ["countdown"] = 1,
                                ["max_countdown"] = 1,
                            },
                        }
                    },
                },
                ["rewards"] = new List<object>
                {
                    new Dictionary<string, object> { ["type"] = "gold", ["id"] = 0, ["count"] = 100 },
                },
            };
        }

        // -----------------------------------------------------------------
        // Setup
        // -----------------------------------------------------------------

        [SetUp]
        public void SetUp()
        {
            _capturedEvents = new List<Dictionary<string, object>>();
            _board = new BoardLogic(new BoardConfig());
            _combat = new CombatResolver(new ElementChart());
            _skillPipeline = new SkillPipeline();

            // DungeonRunner constructor: (BoardLogic, CombatResolver, SkillPipeline, Action<string, Dictionary>)
            _runner = new DungeonRunner(_board, _combat, _skillPipeline,
                (Action<string, Dictionary<string, object>>)CaptureEvent);
        }

        // -----------------------------------------------------------------
        // Tests
        // -----------------------------------------------------------------

        [Test]
        public void Start_Initializes_State()
        {
            _runner.Start(MakeDungeonDef(), 5000);
            var state = _runner.GetState();

            Assert.IsTrue(state.IsActive, "State should be active after start");
            Assert.AreEqual(0, state.CurrentWaveIndex, "Should start at wave 0");
            Assert.AreEqual(5000, state.TeamHp, "TeamHp should match");
        }

        [Test]
        public void Start_Loads_Enemies()
        {
            _runner.Start(MakeDungeonDef(), 5000);
            var state = _runner.GetState();

            Assert.AreEqual(2, state.Enemies.Count, "Wave 0 should have 2 enemies");
            Assert.AreEqual("Slime A", state.Enemies[0].Name, "First enemy name should match");
            Assert.AreEqual("Slime B", state.Enemies[1].Name, "Second enemy name should match");
        }

        [Test]
        public void Execute_Player_Turn_Deals_Damage()
        {
            _runner.Start(MakeDungeonDef(), 5000);

            // Provide cascade_steps that simulate matched gems
            var cascadeSteps = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    ["matches"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["element"] = 1,
                            ["positions"] = new List<object> { 0, 1, 2 },
                            ["gem_count"] = 3,
                        }
                    }
                }
            };

            var result = _runner.ExecutePlayerTurn(cascadeSteps, 1, 1);

            int totalDamage = 0;
            foreach (var kv in result.DamagePerEnemy)
                totalDamage += kv.Value;

            Assert.Greater(totalDamage, 0, "Cascade with matches should deal damage");
        }

        [Test]
        public void Execute_Player_Turn_Kills_Enemy()
        {
            _runner.Start(MakeDungeonDef(), 5000);

            // Force enemy to 1 HP
            var state = _runner.GetState();
            state.Enemies[0].Hp = 1;

            var cascadeSteps = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    ["matches"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["element"] = 1,
                            ["positions"] = new List<object> { 0, 1, 2, 3, 4, 5 },
                            ["gem_count"] = 6,
                        }
                    }
                },
                new Dictionary<string, object>
                {
                    ["matches"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["element"] = 1,
                            ["positions"] = new List<object> { 6, 7, 8 },
                            ["gem_count"] = 3,
                        }
                    }
                },
                new Dictionary<string, object>
                {
                    ["matches"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["element"] = 2,
                            ["positions"] = new List<object> { 9, 10, 11 },
                            ["gem_count"] = 3,
                        }
                    }
                },
            };

            var result = _runner.ExecutePlayerTurn(cascadeSteps, 3, 1);

            Assert.Greater(result.EnemiesKilled.Count, 0, "At least one enemy should be killed");
        }

        [Test]
        public void Execute_Player_Turn_Wave_Clear()
        {
            _runner.Start(MakeDungeonDef(), 5000);

            // Kill all enemies in wave 0
            var state = _runner.GetState();
            foreach (var enemy in state.Enemies)
                enemy.Hp = 1;

            var cascadeSteps = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    ["matches"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["element"] = 1,
                            ["positions"] = new List<object> { 0, 1, 2, 3, 4, 5 },
                            ["gem_count"] = 6,
                        }
                    }
                },
                new Dictionary<string, object>
                {
                    ["matches"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["element"] = 2,
                            ["positions"] = new List<object> { 6, 7, 8 },
                            ["gem_count"] = 3,
                        }
                    }
                },
            };

            var result = _runner.ExecutePlayerTurn(cascadeSteps, 2, 1);

            Assert.IsTrue(result.WaveCleared, "All enemies dead -> wave should be cleared");
        }

        [Test]
        public void Execute_Enemy_Turn_Deals_Damage()
        {
            _runner.Start(MakeDungeonDef(), 5000);
            var state = _runner.GetState();
            int initialHp = state.TeamHp;

            // Set an enemy countdown to 0 so it attacks
            state.Enemies[0].Countdown = 0;
            var result = _runner.ExecuteEnemyTurn();

            Assert.Greater(initialHp, state.TeamHp, "TeamHp should decrease after enemy attack");
            Assert.Greater(result.EnemyAttacks.Count, 0, "Should record at least one attack");
        }

        [Test]
        public void Battle_Won_After_All_Waves()
        {
            _runner.Start(MakeDungeonDef(), 5000);
            var state = _runner.GetState();

            // Clear wave 0: kill all enemies
            foreach (var enemy in state.Enemies)
                enemy.Hp = 0;
            _runner.AdvanceWave();

            // Now on wave 1: kill boss
            state = _runner.GetState();
            foreach (var enemy in state.Enemies)
                enemy.Hp = 0;
            _runner.AdvanceWave();

            state = _runner.GetState();
            Assert.IsFalse(state.IsActive,
                "After clearing all waves, battle should no longer be active");
        }

        [Test]
        public void Battle_Lost_On_Zero_Hp()
        {
            _runner.Start(MakeDungeonDef(), 100);
            var state = _runner.GetState();

            // Set enemy countdown to 0, give it massive ATK
            state.Enemies[0].Countdown = 0;
            state.Enemies[0].Atk = 99999;
            _runner.ExecuteEnemyTurn();

            state = _runner.GetState();
            Assert.AreEqual(0, state.TeamHp, "TeamHp should be 0 after massive damage");
            Assert.IsFalse(state.IsActive, "Battle should be over when TeamHp reaches 0");
        }
    }
}
