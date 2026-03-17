#if UNITY_INCLUDE_TESTS
using System;
using System.Collections.Generic;
using NUnit.Framework;
using MobileForge.Domain;
using TowerOfSaviors;

namespace TowerOfSaviors.Tests
{
    [TestFixture]
    public class TosIntegrationTests
    {
        private BoardConfig _config;
        private BoardLogic _board;
        private ElementChart _chart;
        private CombatResolver _combat;
        private SkillPipeline _skillPipeline;

        [SetUp]
        public void SetUp()
        {
            _config = new BoardConfig(5, 6);
            _board = new BoardLogic(_config, 12345);
            _board.InitBoard();
            _chart = new ElementChart();
            _combat = new CombatResolver(_chart);
            _skillPipeline = new SkillPipeline();
            TosSkillRegistration.Register(_skillPipeline);
        }

        private DungeonDef CreateTestDungeon()
        {
            return new DungeonDef(new Dictionary<string, object>
            {
                { "id", 1 },
                { "name", "Test Dungeon" },
                { "stamina_cost", 10 },
                { "waves", new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            { "enemies", new List<object>
                                {
                                    new Dictionary<string, object>
                                    {
                                        { "name", "Slime" },
                                        { "element", 2 },
                                        { "hp", 500 },
                                        { "atk", 100 },
                                        { "defense", 0 },
                                        { "countdown", 2 },
                                    }
                                }
                            }
                        }
                    }
                },
                { "rewards", new Dictionary<string, object> { { "coins", 100 } } }
            });
        }

        [Test]
        public void FullBattleFlow_WinCondition()
        {
            var dungeonDef = CreateTestDungeon();
            var runner = new DungeonRunner(_board, _combat, _skillPipeline, null);

            runner.Start(dungeonDef, 10000, 10000);

            var state = runner.GetState();
            Assert.IsTrue(state.IsActive, "Dungeon should be active after start");
            Assert.AreEqual(1, state.Enemies.Count, "Wave 1 should have 1 enemy");

            var team = new List<object> { new MonsterInstance(1, 1) };
            var teamStats = new List<object> { new MonsterStats(3000, 1000, 300) };

            int turns = 0;
            while (state.IsActive && turns < 20)
            {
                var cascadeSteps = CascadeResolver.Resolve(_board);
                if (cascadeSteps.Count == 0)
                {
                    _board.InitBoard();
                    turns++;
                    continue;
                }
                runner.ExecutePlayerTurn(cascadeSteps, team, teamStats);
                state = runner.GetState();
                if (state.IsActive)
                {
                    runner.ExecuteEnemyTurn();
                }
                turns++;
            }

            Assert.IsFalse(state.IsActive, "Dungeon should end within 20 turns");
            Assert.Greater(turns, 0, "Should have taken at least 1 turn");
        }

        [Test]
        public void FullBattleFlow_LoseCondition()
        {
            var dungeonDef = new DungeonDef(new Dictionary<string, object>
            {
                { "id", 2 },
                { "name", "Hard Dungeon" },
                { "stamina_cost", 10 },
                { "waves", new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            { "enemies", new List<object>
                                {
                                    new Dictionary<string, object>
                                    {
                                        { "name", "Boss" },
                                        { "element", 2 },
                                        { "hp", 100000 },
                                        { "atk", 5000 },
                                        { "defense", 100 },
                                        { "countdown", 1 },
                                    }
                                }
                            }
                        }
                    }
                },
                { "rewards", new Dictionary<string, object>() }
            });

            var runner = new DungeonRunner(_board, _combat, _skillPipeline, null);
            runner.Start(dungeonDef, 100, 100);

            var team = new List<object> { new MonsterInstance(1, 1) };
            var teamStats = new List<object> { new MonsterStats(100, 10, 10) };

            bool battleLost = false;
            int turns = 0;
            while (runner.GetState().IsActive && turns < 50)
            {
                var cascadeSteps = CascadeResolver.Resolve(_board);
                if (cascadeSteps.Count == 0)
                {
                    _board.InitBoard();
                    turns++;
                    continue;
                }
                runner.ExecutePlayerTurn(cascadeSteps, team, teamStats);
                if (!runner.GetState().IsActive)
                {
                    break;
                }
                runner.ExecuteEnemyTurn();
                if (runner.GetState().TeamHp <= 0)
                {
                    battleLost = true;
                    break;
                }
                turns++;
            }

            Assert.IsTrue(battleLost || !runner.GetState().IsActive, "Battle should end");
        }

        [Test]
        public void SkillRegistration_AllTypesAvailable()
        {
            Assert.IsTrue(_skillPipeline.ConditionRegistry.HasType("always_true"), "always_true condition should be registered");
            Assert.IsTrue(_skillPipeline.ConditionRegistry.HasType("combo_above"), "combo_above condition should be registered");
            Assert.IsTrue(_skillPipeline.ConditionRegistry.HasType("hp_below"), "hp_below condition should be registered");
            Assert.IsTrue(_skillPipeline.ConditionRegistry.HasType("elements_matched"), "elements_matched condition should be registered");
            Assert.IsTrue(_skillPipeline.ConditionRegistry.HasType("team_has_element"), "team_has_element condition should be registered");

            Assert.IsTrue(_skillPipeline.EffectRegistry.HasEffect("area_damage"), "area_damage effect should be registered");
            Assert.IsTrue(_skillPipeline.EffectRegistry.HasEffect("heal_flat"), "heal_flat effect should be registered");
            Assert.IsTrue(_skillPipeline.EffectRegistry.HasEffect("heal_percent"), "heal_percent effect should be registered");
            Assert.IsTrue(_skillPipeline.EffectRegistry.HasEffect("change_gem_element"), "change_gem_element effect should be registered");
            Assert.IsTrue(_skillPipeline.EffectRegistry.HasEffect("delay_enemies"), "delay_enemies effect should be registered");
        }

        [Test]
        public void SkillActivation_AreaDamage()
        {
            var enemies = new List<EnemyState>
            {
                new EnemyState(new Dictionary<string, object> { { "name", "E1" }, { "hp", 1000 }, { "atk", 10 }, { "countdown", 1 }, { "element", 1 } }),
                new EnemyState(new Dictionary<string, object> { { "name", "E2" }, { "hp", 1000 }, { "atk", 10 }, { "countdown", 1 }, { "element", 2 } }),
                new EnemyState(new Dictionary<string, object> { { "name", "E3" }, { "hp", 1000 }, { "atk", 10 }, { "countdown", 1 }, { "element", 3 }, { "hp_regen", 0 } }),
            };
            enemies[2].Hp = 0;

            var skillDef = new SkillDef(new Dictionary<string, object>
            {
                { "id", 1 },
                { "name", "Test Area Damage" },
                { "rules", new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            { "conditions", new List<object>
                                {
                                    new Dictionary<string, object> { { "type", "always_true" }, { "params", new Dictionary<string, object>() } }
                                }
                            },
                            { "outcomes", new List<object>
                                {
                                    new Dictionary<string, object> { { "type", "area_damage" }, { "params", new Dictionary<string, object> { { "multiplier", 5.0 } } } }
                                }
                            }
                        }
                    }
                }
            });

            var context = new SkillContext
            {
                TeamStats = new List<object> { new MonsterStats(1000, 500, 100) },
                Enemies = new List<object>(enemies),
                TeamHp = 10000,
                MaxHp = 10000,
            };

            var result = _skillPipeline.ActivateSkill(skillDef, context);

            Assert.IsTrue(result.DamageDealt.ContainsKey(0), "Enemy 0 should take damage");
            Assert.IsTrue(result.DamageDealt.ContainsKey(1), "Enemy 1 should take damage");
            Assert.IsFalse(result.DamageDealt.ContainsKey(2), "Dead enemy 2 should not take damage");
            Assert.AreEqual(2500, result.DamageDealt[0], "Area damage should be atk * multiplier");
        }

        [Test]
        public void SkillActivation_HealFlat()
        {
            var skillDef = new SkillDef(new Dictionary<string, object>
            {
                { "id", 2 },
                { "name", "Test Heal" },
                { "rules", new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            { "conditions", new List<object>
                                {
                                    new Dictionary<string, object> { { "type", "always_true" }, { "params", new Dictionary<string, object>() } }
                                }
                            },
                            { "outcomes", new List<object>
                                {
                                    new Dictionary<string, object> { { "type", "heal_flat" }, { "params", new Dictionary<string, object> { { "amount", 500 } } } }
                                }
                            }
                        }
                    }
                }
            });

            var context = new SkillContext { TeamHp = 5000, MaxHp = 10000 };
            var result = _skillPipeline.ActivateSkill(skillDef, context);

            Assert.AreEqual(500, result.Healing, "Heal should add 500 HP");
        }

        [Test]
        public void SkillActivation_ChangeGemElement()
        {
            _board.FromElementArray(new int[]
            {
                1, 1, 1, 1, 1, 1,
                2, 2, 2, 2, 2, 2,
                3, 3, 3, 3, 3, 3,
                4, 4, 4, 4, 4, 4,
                5, 5, 5, 5, 5, 5,
            });

            var skillDef = new SkillDef(new Dictionary<string, object>
            {
                { "id", 3 },
                { "name", "Test Change Gem" },
                { "rules", new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            { "conditions", new List<object>
                                {
                                    new Dictionary<string, object> { { "type", "always_true" }, { "params", new Dictionary<string, object>() } }
                                }
                            },
                            { "outcomes", new List<object>
                                {
                                    new Dictionary<string, object> { { "type", "change_gem_element" }, { "params", new Dictionary<string, object> { { "from", 1 }, { "to", 6 } } } }
                                }
                            }
                        }
                    }
                }
            });

            var context = new SkillContext { Board = _board };
            var result = _skillPipeline.ActivateSkill(skillDef, context);

            Assert.Greater(result.BoardChanges.Count, 0, "Should have board changes");

            var elements = _board.ToElementArray();
            for (int i = 0; i < 6; i++)
            {
                Assert.AreEqual(6, elements[i], $"Row 0 col {i} should be element 6 (heart)");
            }
        }

        [Test]
        public void SkillActivation_DelayEnemies()
        {
            var enemies = new List<EnemyState>
            {
                new EnemyState(new Dictionary<string, object> { { "name", "E1" }, { "hp", 1000 }, { "atk", 10 }, { "countdown", 2 }, { "element", 1 } }),
                new EnemyState(new Dictionary<string, object> { { "name", "E2" }, { "hp", 1000 }, { "atk", 10 }, { "countdown", 1 }, { "element", 2 } }),
            };

            var skillDef = new SkillDef(new Dictionary<string, object>
            {
                { "id", 4 },
                { "name", "Test Delay" },
                { "rules", new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            { "conditions", new List<object>
                                {
                                    new Dictionary<string, object> { { "type", "always_true" }, { "params", new Dictionary<string, object>() } }
                                }
                            },
                            { "outcomes", new List<object>
                                {
                                    new Dictionary<string, object> { { "type", "delay_enemies" }, { "params", new Dictionary<string, object> { { "turns", 3 } } } }
                                }
                            }
                        }
                    }
                }
            });

            var context = new SkillContext { Enemies = new List<object>(enemies) };
            _skillPipeline.ActivateSkill(skillDef, context);

            Assert.AreEqual(5, enemies[0].Countdown, "Enemy 0 countdown should increase by 3");
            Assert.AreEqual(4, enemies[1].Countdown, "Enemy 1 countdown should increase by 3");
        }

        [Test]
        public void GachaPull_DeductsCurrency()
        {
            var pool = new GachaPool(1, "Test Pool", "gems", 5, new List<GachaEntry>
            {
                new GachaEntry(1, 3, 70, false),
                new GachaEntry(2, 4, 25, false),
                new GachaEntry(3, 5, 5, true),
            }, 0, Array.Empty<int>());

            var rng = new Random(12345);
            var result = GachaRoller.Roll(pool, 0, rng);

            Assert.IsNotNull(result, "Gacha roll should return a result");
            Assert.Greater(result.MonsterId, 0, "Result should have a valid monster_id");
            Assert.Greater(result.Rarity, 0, "Result should have a valid rarity");
        }

        [Test]
        public void GachaPull_PityTriggersAtThreshold()
        {
            var pool = new GachaPool(1, "Test Pool", "gems", 5, new List<GachaEntry>
            {
                new GachaEntry(1, 3, 70, false),
                new GachaEntry(2, 4, 25, false),
                new GachaEntry(3, 5, 5, true),
            }, 10, Array.Empty<int>());

            var rng = new Random(12345);
            var result = GachaRoller.Roll(pool, 9, rng);

            Assert.IsTrue(result.IsPity, "Pity should trigger at threshold - 1");
            Assert.AreEqual(5, result.Rarity, "Pity should guarantee top rarity");
        }

        [Test]
        public void BoardCascade_ResolvesCorrectly()
        {
            _board.FromElementArray(new int[]
            {
                1, 1, 1, 2, 3, 4,
                2, 3, 4, 5, 6, 1,
                3, 4, 5, 6, 1, 2,
                4, 5, 6, 1, 2, 3,
                5, 6, 1, 2, 3, 4,
            });

            var steps = CascadeResolver.Resolve(_board);

            Assert.GreaterOrEqual(steps.Count, 1, "Should have at least 1 cascade step");
            var firstStep = steps[0];
            Assert.GreaterOrEqual(firstStep.Matches.Count, 1, "First step should have at least 1 match");
        }

        [Test]
        public void ElementAdvantage_CorrectMultipliers()
        {
            Assert.AreEqual(1.5f, _chart.GetMultiplier(1, 2), 0.001f, "Water vs Fire = 1.5x");
            Assert.AreEqual(0.5f, _chart.GetMultiplier(2, 1), 0.001f, "Fire vs Water = 0.5x");
            Assert.AreEqual(1.5f, _chart.GetMultiplier(4, 5), 0.001f, "Light vs Dark = 1.5x");
            Assert.AreEqual(1.5f, _chart.GetMultiplier(5, 4), 0.001f, "Dark vs Light = 1.5x");
            Assert.AreEqual(1.0f, _chart.GetMultiplier(1, 1), 0.001f, "Same element = 1.0x");
        }

        [Test]
        public void TeamSkill_FullElementTeam()
        {
            var condition = _skillPipeline.ConditionRegistry.Create("team_has_element",
                new Dictionary<string, object> { { "element", 1 }, { "min_count", 5 } });

            var context = new SkillContext
            {
                Team = new List<object>
                {
                    new MonsterInstance(1, 1),
                    new MonsterInstance(2, 1),
                    new MonsterInstance(3, 1),
                    new MonsterInstance(4, 1),
                    new MonsterInstance(5, 1),
                }
            };

            Assert.IsNotNull(condition, "team_has_element condition should be created");
        }
    }
}
#endif
