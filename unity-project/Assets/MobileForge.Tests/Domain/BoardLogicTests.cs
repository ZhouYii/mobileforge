using System.Collections.Generic;
using NUnit.Framework;
using MobileForge.Domain;

namespace MobileForge.Tests.Domain
{
    [TestFixture]
    public class BoardLogicTests
    {
        private BoardConfig _config;
        private BoardLogic _board;

        [SetUp]
        public void SetUp()
        {
            _config = new BoardConfig(5, 6);
            _board = new BoardLogic(_config, 42);
        }

        // -----------------------------------------------------------------
        // Tests
        // -----------------------------------------------------------------

        [Test]
        public void InitBoard_Fills_All_Cells()
        {
            _board.InitBoard();

            for (int i = 0; i < _config.TotalCells; i++)
            {
                Assert.IsNotNull(_board.GetGem(i), $"Cell {i} should not be null after init");
            }
        }

        [Test]
        public void InitBoard_No_Initial_Matches()
        {
            _board.InitBoard();

            var matches = MatchDetector.FindMatches(_board);
            Assert.AreEqual(0, matches.Count, "Board should have no matches after init");
        }

        [Test]
        public void SwapGems_Swaps_Positions()
        {
            _board.InitBoard();

            var gemA = _board.GetGem(0);
            var gemB = _board.GetGem(1);
            int elemA = gemA.ElementId;
            int elemB = gemB.ElementId;

            bool result = _board.SwapGems(0, 1);

            Assert.IsTrue(result, "Swap should succeed");
            Assert.AreEqual(elemB, _board.GetGem(0).ElementId, "Gem at 0 should now have element of former gem at 1");
            Assert.AreEqual(elemA, _board.GetGem(1).ElementId, "Gem at 1 should now have element of former gem at 0");
            Assert.AreEqual(0, _board.GetGem(0).Position, "Position field should be updated");
            Assert.AreEqual(1, _board.GetGem(1).Position, "Position field should be updated");
        }

        [Test]
        public void SwapGems_Frozen_Gem_Fails()
        {
            _board.InitBoard();

            var gem = _board.GetGem(0);
            gem.AddStatus(GemStatus.Frozen);

            bool result = _board.SwapGems(0, 1);

            Assert.IsFalse(result, "Swap should fail when gem is frozen");
        }

        [Test]
        public void MoveGemPath_Moves_Along_Path()
        {
            // Set up a known board
            var elements = new int[30];
            for (int i = 0; i < 30; i++)
                elements[i] = (i % 6) + 1;
            _board.FromElementArray(elements);

            int elemAt0 = _board.GetGem(0).ElementId;
            int elemAt1 = _board.GetGem(1).ElementId;
            int elemAt2 = _board.GetGem(2).ElementId;

            // Move gem from 0 along path [1, 2]
            _board.MoveGemPath(0, new List<int> { 1, 2 });

            // After moving: gem originally at 0 is now at 2
            Assert.AreEqual(elemAt0, _board.GetGem(2).ElementId, "Original gem should be at end of path");
            // Gem originally at 1 should now be at 0 (from first swap)
            Assert.AreEqual(elemAt1, _board.GetGem(0).ElementId, "Gem from pos 1 should be at pos 0");
            // Gem originally at 2 should now be at 1 (from second swap)
            Assert.AreEqual(elemAt2, _board.GetGem(1).ElementId, "Gem from pos 2 should be at pos 1");
        }

        [Test]
        public void FromElementArray_And_ToElementArray_Roundtrip()
        {
            var elements = new[]
            {
                1, 2, 3, 4, 5, 6,
                2, 3, 4, 5, 6, 1,
                3, 4, 5, 6, 1, 2,
                4, 5, 6, 1, 2, 3,
                5, 6, 1, 2, 3, 4,
            };

            _board.FromElementArray(elements);

            var result = _board.ToElementArray();

            Assert.AreEqual(elements.Length, result.Length, "Array sizes should match");
            for (int i = 0; i < elements.Length; i++)
            {
                Assert.AreEqual(elements[i], result[i], $"Element at {i} should match");
            }
        }

        [Test]
        public void ChangeGemElement_Updates_Element()
        {
            _board.InitBoard();

            _board.ChangeGemElement(0, (int)Element.Heart);

            Assert.AreEqual((int)Element.Heart, _board.GetGem(0).ElementId, "Element should be changed to Heart");
        }

        [Test]
        public void BoardConfig_PosConversion()
        {
            // Position 8 = row 1, col 2 (5 rows, 6 cols)
            Assert.AreEqual(1, _config.PosToRow(8), "Pos 8 -> row 1");
            Assert.AreEqual(2, _config.PosToCol(8), "Pos 8 -> col 2");
            Assert.AreEqual(8, _config.RcToPos(1, 2), "Row 1, col 2 -> pos 8");

            // Position 0 = row 0, col 0
            Assert.AreEqual(0, _config.PosToRow(0), "Pos 0 -> row 0");
            Assert.AreEqual(0, _config.PosToCol(0), "Pos 0 -> col 0");
            Assert.AreEqual(0, _config.RcToPos(0, 0), "Row 0, col 0 -> pos 0");

            // Position 29 (last cell) = row 4, col 5
            Assert.AreEqual(4, _config.PosToRow(29), "Pos 29 -> row 4");
            Assert.AreEqual(5, _config.PosToCol(29), "Pos 29 -> col 5");
            Assert.AreEqual(29, _config.RcToPos(4, 5), "Row 4, col 5 -> pos 29");
        }

        [Test]
        public void BoardConfig_Adjacency()
        {
            // Horizontal neighbors
            Assert.IsTrue(_config.AreAdjacent(0, 1), "Pos 0 and 1 are adjacent (horizontal)");
            // Vertical neighbors
            Assert.IsTrue(_config.AreAdjacent(0, 6), "Pos 0 and 6 are adjacent (vertical)");
            // Diagonal = NOT adjacent
            Assert.IsFalse(_config.AreAdjacent(0, 7), "Pos 0 and 7 are diagonal, not adjacent");
            // Same position
            Assert.IsFalse(_config.AreAdjacent(0, 0), "Same position is not adjacent");
            // Far apart
            Assert.IsFalse(_config.AreAdjacent(0, 29), "Pos 0 and 29 are not adjacent");
            // Wrap-around should NOT count (end of row 0 and start of row 1)
            Assert.IsFalse(_config.AreAdjacent(5, 6), "Pos 5 and 6 are not adjacent (different rows)");
        }
    }
}
