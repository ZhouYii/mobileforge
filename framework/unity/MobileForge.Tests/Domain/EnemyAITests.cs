using System;
using System.Collections.Generic;
using NUnit.Framework;
using MobileForge.Domain;

namespace MobileForge.Tests.Domain
{
    [TestFixture]
    public class EnemyAITests
    {
        private List<EnemyState> _enemies;

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

        private static EnemyState MakeEnemy(int id, int hp, int atk, int countdown)
        {
            return new EnemyState(new Dictionary<string, object>
            {
                ["id"] = id,
                ["name"] = $"Enemy_{id}",
                ["element"] = 1,
                ["hp"] = hp,
                ["atk"] = atk,
                ["countdown"] = countdown,
                ["max_countdown"] = countdown,
            });
        }

        // -----------------------------------------------------------------
        // Setup
        // -----------------------------------------------------------------

        [SetUp]
        public void SetUp()
        {
            // 3 enemies with countdowns [2, 1, 3]
            _enemies = new List<EnemyState>
            {
                MakeEnemy(0, 1000, 100, 2),
                MakeEnemy(1, 800, 150, 1),
                MakeEnemy(2, 1200, 80, 3),
            };
        }

        // -----------------------------------------------------------------
        // Tests
        // -----------------------------------------------------------------

        [Test]
        public void Tick_Countdowns_Returns_Ready()
        {
            // After tick: [1, 0, 2]. Only enemy_1 should be ready.
            var ready = EnemyAI.TickCountdowns(_enemies);

            Assert.AreEqual(1, ready.Count, "Only 1 enemy should be ready");
            Assert.AreEqual(1, ready[0].Id, "Enemy_1 should be the ready one");
            Assert.AreEqual(1, _enemies[0].Countdown, "Enemy_0 countdown should be 1");
            Assert.AreEqual(0, _enemies[1].Countdown, "Enemy_1 countdown should be 0");
            Assert.AreEqual(2, _enemies[2].Countdown, "Enemy_2 countdown should be 2");
        }

        [Test]
        public void Tick_Countdowns_Skips_Dead()
        {
            // Kill enemy_1, then tick
            _enemies[1].Hp = 0;
            var ready = EnemyAI.TickCountdowns(_enemies);

            // enemy_0: 2->1, enemy_1: dead (skipped), enemy_2: 3->2
            Assert.AreEqual(0, ready.Count, "No enemies should be ready");
            Assert.AreEqual(1, _enemies[0].Countdown, "Enemy_0 should be ticked");
            Assert.AreEqual(1, _enemies[1].Countdown, "Dead enemy_1 should NOT be ticked");
            Assert.AreEqual(2, _enemies[2].Countdown, "Enemy_2 should be ticked");
        }

        [Test]
        public void Decide_Action_Returns_Attack()
        {
            var action = EnemyAI.DecideAction(_enemies[0]);

            Assert.IsNotNull(action, "Action should not be null");
            Assert.AreEqual("attack", action.Type, "Default AI should return attack");
            Assert.AreEqual(_enemies[0].Atk, action.Damage, "Damage should match enemy ATK");
        }

        [Test]
        public void Reset_Countdown()
        {
            _enemies[1].Countdown = 0;
            EnemyAI.ResetCountdown(_enemies[1]);

            Assert.AreEqual(_enemies[1].MaxCountdown, _enemies[1].Countdown,
                "Countdown should reset to MaxCountdown");
        }

        [Test]
        public void Can_Attack_Ready()
        {
            _enemies[0].Countdown = 0;

            Assert.IsTrue(EnemyAI.CanAttack(_enemies[0]),
                "Enemy with countdown=0 should be able to attack");
        }

        [Test]
        public void Can_Attack_Not_Ready()
        {
            _enemies[0].Countdown = 2;

            Assert.IsFalse(EnemyAI.CanAttack(_enemies[0]),
                "Enemy with countdown>0 should not be able to attack");
        }

        [Test]
        public void Tick_Enemy_Statuses()
        {
            // Add a status with turns=1 to enemy_0
            _enemies[0].AddStatus("poison", 1);
            Assert.IsTrue(_enemies[0].HasStatus("poison"),
                "Enemy should have poison before tick");

            EnemyAI.TickEnemyStatuses(_enemies);
            Assert.IsFalse(_enemies[0].HasStatus("poison"),
                "Poison with turns=1 should expire after tick");

            // Add a longer status and verify it persists
            _enemies[1].AddStatus("defense_down", 3);
            EnemyAI.TickEnemyStatuses(_enemies);
            Assert.IsTrue(_enemies[1].HasStatus("defense_down"),
                "Status with turns=3 should persist after 1 tick");
        }
    }
}
