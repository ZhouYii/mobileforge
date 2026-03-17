using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using MobileForge.Domain;

namespace MobileForge.Tests.Domain
{
    [TestFixture]
    public class MatchDetectorTests
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
        // Helper
        // -----------------------------------------------------------------

        private void SetBoard(int[] elements)
        {
            _board.FromElementArray(elements);
        }

        private MatchResult FindMatchWithPositions(List<MatchResult> matches, int[] positions)
        {
            var posSet = new HashSet<int>(positions);
            foreach (var m in matches)
            {
                if (m.Positions.Count == positions.Length &&
                    new HashSet<int>(m.Positions).SetEquals(posSet))
                {
                    return m;
                }
            }
            return null;
        }

        // -----------------------------------------------------------------
        // Tests
        // -----------------------------------------------------------------

        [Test]
        public void Horizontal_3_Match()
        {
            // From test vector: horizontal_3_match
            SetBoard(new[]
            {
                1, 1, 1, 2, 3, 4,
                2, 3, 4, 5, 6, 1,
                3, 4, 5, 6, 1, 2,
                4, 5, 6, 1, 2, 3,
                5, 6, 1, 2, 3, 4,
            });

            var matches = MatchDetector.FindMatches(_board);

            Assert.AreEqual(1, matches.Count, "Should find exactly 1 match");
            Assert.AreEqual(1, matches[0].ElementId, "Match element should be Water");
            Assert.AreEqual(3, matches[0].GemCount, "Match should have 3 gems");
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, matches[0].Positions);
        }

        [Test]
        public void Vertical_3_Match()
        {
            // From test vector: vertical_3_match
            SetBoard(new[]
            {
                2, 3, 4, 5, 6, 1,
                2, 1, 3, 4, 5, 6,
                2, 4, 5, 6, 1, 3,
                3, 5, 6, 1, 2, 4,
                4, 6, 1, 2, 3, 5,
            });

            var matches = MatchDetector.FindMatches(_board);

            Assert.AreEqual(1, matches.Count, "Should find exactly 1 match");
            Assert.AreEqual(2, matches[0].ElementId, "Match element should be Fire");
            Assert.AreEqual(3, matches[0].GemCount, "Match should have 3 gems");
            CollectionAssert.AreEqual(new[] { 0, 6, 12 }, matches[0].Positions);
        }

        [Test]
        public void No_Match()
        {
            // From test vector: no_match
            SetBoard(new[]
            {
                1, 2, 3, 4, 5, 6,
                2, 3, 4, 5, 6, 1,
                3, 4, 5, 6, 1, 2,
                4, 5, 6, 1, 2, 3,
                5, 6, 1, 2, 3, 4,
            });

            var matches = MatchDetector.FindMatches(_board);

            Assert.AreEqual(0, matches.Count, "Should find no matches");
        }

        [Test]
        public void LShape_Merge()
        {
            // From test vector: l_shape_merge
            SetBoard(new[]
            {
                1, 1, 1, 2, 3, 4,
                3, 4, 1, 5, 6, 2,
                2, 5, 1, 6, 4, 3,
                4, 6, 3, 2, 5, 1,
                5, 2, 4, 3, 6, 2,
            });

            var matches = MatchDetector.FindMatches(_board);

            Assert.AreEqual(1, matches.Count, "L-shape should merge into 1 match");
            Assert.AreEqual(1, matches[0].ElementId, "Match element should be Water");
            Assert.AreEqual(5, matches[0].GemCount, "L-shape should have 5 gems");
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 8, 14 }, matches[0].Positions);
        }

        [Test]
        public void Multiple_Matches()
        {
            // From test vector: multiple_independent_matches
            SetBoard(new[]
            {
                1, 1, 1, 3, 3, 3,
                2, 4, 5, 6, 4, 2,
                3, 5, 6, 4, 5, 1,
                4, 6, 4, 5, 6, 2,
                5, 4, 5, 6, 4, 3,
            });

            var matches = MatchDetector.FindMatches(_board);

            Assert.AreEqual(2, matches.Count, "Should find 2 independent matches");

            var waterMatch = FindMatchWithPositions(matches, new[] { 0, 1, 2 });
            var grassMatch = FindMatchWithPositions(matches, new[] { 3, 4, 5 });

            Assert.IsNotNull(waterMatch, "Should find water match at [0, 1, 2]");
            Assert.IsNotNull(grassMatch, "Should find grass match at [3, 4, 5]");
            Assert.AreEqual(1, waterMatch.ElementId, "Water match element");
            Assert.AreEqual(3, grassMatch.ElementId, "Grass match element");
        }

        [Test]
        public void Locked_Gems_Not_Matched()
        {
            // Set up a board with 3 same-element gems in a row, but middle one is LOCKED
            SetBoard(new[]
            {
                1, 1, 1, 2, 3, 4,
                2, 3, 4, 5, 6, 1,
                3, 4, 5, 6, 1, 2,
                4, 5, 6, 1, 2, 3,
                5, 6, 1, 2, 3, 4,
            });

            // Lock the middle gem of the would-be match at position 1
            _board.GetGem(1).AddStatus(GemStatus.Locked);

            var matches = MatchDetector.FindMatches(_board);

            Assert.AreEqual(0, matches.Count, "Locked gem should prevent match");
        }

        [Test]
        public void Five_Gem_Match()
        {
            // From test vector: horizontal_5_match
            SetBoard(new[]
            {
                3, 3, 3, 3, 3, 1,
                1, 2, 4, 5, 6, 2,
                2, 4, 5, 6, 1, 3,
                4, 5, 6, 1, 2, 4,
                5, 6, 1, 2, 3, 5,
            });

            var matches = MatchDetector.FindMatches(_board);

            Assert.AreEqual(1, matches.Count, "Should find exactly 1 match");
            Assert.AreEqual(3, matches[0].ElementId, "Match element should be Grass");
            Assert.AreEqual(5, matches[0].GemCount, "Match should have 5 gems");
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, matches[0].Positions);
        }
    }
}
