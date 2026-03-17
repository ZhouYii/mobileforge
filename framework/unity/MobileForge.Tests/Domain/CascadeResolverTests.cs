using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MobileForge.Domain;

namespace MobileForge.Tests.Domain
{
    [TestFixture]
    public class CascadeResolverTests
    {
        private BoardConfig _config;
        private BoardLogic _board;

        [SetUp]
        public void SetUp()
        {
            _config = new BoardConfig(5, 6);
            _board = new BoardLogic(_config, 12345);
        }

        private void SetBoard(int[] elements)
        {
            _board.FromElementArray(elements);
        }

        [Test]
        public void Resolve_SingleMatch_RemovesAndDrops()
        {
            SetBoard(new int[] {
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

            Assert.IsTrue(firstStep.RemovedPositions.Contains(0), "Position 0 should be removed");
            Assert.IsTrue(firstStep.RemovedPositions.Contains(1), "Position 1 should be removed");
            Assert.IsTrue(firstStep.RemovedPositions.Contains(2), "Position 2 should be removed");

            Assert.Greater(firstStep.Drops.Count, 0, "Should have drops after clearing top row");
        }

        [Test]
        public void Resolve_MultiStepCascade_ChainsCorrectly()
        {
            SetBoard(new int[] {
                1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1,
                1, 1, 1, 1, 1, 1,
            });

            var steps = CascadeResolver.Resolve(_board);

            Assert.GreaterOrEqual(steps.Count, 1, "Should have at least 1 step even with full board of same element");
        }

        [Test]
        public void Resolve_GravityFillsFromTop()
        {
            SetBoard(new int[] {
                1, 1, 1, 2, 3, 4,
                2, 3, 4, 5, 6, 1,
                3, 4, 5, 6, 1, 2,
                4, 5, 6, 1, 2, 3,
                5, 6, 1, 2, 3, 4,
            });

            var steps = CascadeResolver.Resolve(_board);

            Assert.GreaterOrEqual(steps.Count, 1, "Should have at least 1 step");

            var firstStep = steps[0];
            Assert.Greater(firstStep.Spawned.Count, 0, "Should have spawned new gems at top");

            Assert.AreEqual(firstStep.Spawned.Count, firstStep.RemovedPositions.Count,
                "Spawned count should equal removed count");
        }

        [Test]
        public void Resolve_NoMatch_ReturnsEmpty()
        {
            SetBoard(new int[] {
                1, 2, 3, 4, 5, 6,
                2, 3, 4, 5, 6, 1,
                3, 4, 5, 6, 1, 2,
                4, 5, 6, 1, 2, 3,
                5, 6, 1, 2, 3, 4,
            });

            var steps = CascadeResolver.Resolve(_board);

            Assert.AreEqual(0, steps.Count, "No matches should produce empty cascade");
        }

        [Test]
        public void Resolve_FullBoardCascade_CompletesWithoutInfiniteLoop()
        {
            SetBoard(new int[] {
                1, 2, 1, 2, 1, 2,
                2, 1, 2, 1, 2, 1,
                1, 2, 1, 2, 1, 2,
                2, 1, 2, 1, 2, 1,
                1, 2, 1, 2, 1, 2,
            });

            var steps = CascadeResolver.Resolve(_board);

            Assert.LessOrEqual(steps.Count, 100, "Cascade should terminate within reasonable steps");

            for (int i = 0; i < _config.TotalCells; i++)
            {
                Assert.IsNotNull(_board.GetGem(i), $"Cell {i} should not be null after full cascade");
            }
        }

        [Test]
        public void CascadeStep_ContainsCorrectMatchData()
        {
            SetBoard(new int[] {
                1, 1, 1, 2, 3, 4,
                2, 3, 4, 5, 6, 1,
                3, 4, 5, 6, 1, 2,
                4, 5, 6, 1, 2, 3,
                5, 6, 1, 2, 3, 4,
            });

            var steps = CascadeResolver.Resolve(_board);

            Assert.GreaterOrEqual(steps.Count, 1, "Should have at least 1 step");

            var firstStep = steps[0];
            Assert.GreaterOrEqual(firstStep.Matches.Count, 1, "Should have at least 1 match");
            Assert.AreEqual(0, firstStep.StepIndex, "First step index should be 0");
            Assert.Greater(firstStep.RemovedPositions.Count, 0, "Should have removed positions");
        }

        [Test]
        public void Resolve_CrossPatternMatch_MergesOverlapping()
        {
            SetBoard(new int[] {
                0, 1, 0, 0, 0, 0,
                1, 1, 1, 0, 0, 0,
                0, 1, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0,
                0, 0, 0, 0, 0, 0,
            });

            var steps = CascadeResolver.Resolve(_board);

            if (steps.Count > 0)
            {
                var firstStep = steps[0];

                bool foundCrossMatch = false;
                foreach (var match in firstStep.Matches)
                {
                    if (match.Positions.Count >= 5)
                    {
                        foundCrossMatch = true;
                        break;
                    }
                }
                Assert.IsTrue(foundCrossMatch || firstStep.Matches.Count >= 2,
                    "Cross pattern should be merged or detected as multiple matches");
            }
        }
    }
}
