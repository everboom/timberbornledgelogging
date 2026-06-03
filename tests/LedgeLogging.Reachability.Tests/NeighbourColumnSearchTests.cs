using System;
using System.Collections.Generic;
using System.Linq;
using LedgeLogging.Reachability;

namespace LedgeLogging.Reachability.Tests
{
    /// <summary>
    /// Unit tests for <see cref="NeighbourColumnSearch"/> — the pure standing-tile search.
    /// The game's reachability test is faked with an in-memory probe, so these pin the
    /// search's contract: orthogonal-only, ±1 level, nearest-reachable, deterministic.
    /// </summary>
    [TestClass]
    public sealed class NeighbourColumnSearchTests
    {
        #region Fixtures

        /// <summary>A tree sitting at column (5, 5), terrain level 3, used by most cases.</summary>
        private static readonly TileCoord Tree = new TileCoord(5, 5, 3);

        /// <summary>
        /// Builds a probe that reports the given tiles as reachable (at the given distances)
        /// and everything else as unreachable, while recording every tile it was asked about.
        /// </summary>
        private static (TileReachabilityProbe Probe, List<TileCoord> Queried) MakeProbe(
            Dictionary<TileCoord, float> reachable)
        {
            var queried = new List<TileCoord>();
            TileReachabilityProbe probe = (TileCoord tile, out float distance) =>
            {
                queried.Add(tile);
                return reachable.TryGetValue(tile, out distance);
            };
            return (probe, queried);
        }

        #endregion

        #region Found cases

        [TestMethod]
        public void TryFindStandingTile_ReturnsSameLevelNeighbour_WhenOnlyItIsReachable()
        {
            // Arrange
            var expected = new TileCoord(6, 5, 3);
            var (probe, _) = MakeProbe(new Dictionary<TileCoord, float> { [expected] = 2f });

            // Act
            var found = NeighbourColumnSearch.TryFindStandingTile(Tree, probe, out var tile, out var distance);

            // Assert
            Assert.IsTrue(found);
            Assert.AreEqual(expected, tile);
            Assert.AreEqual(2f, distance);
        }

        [TestMethod]
        public void TryFindStandingTile_ReturnsNeighbourOneLevelAbove()
        {
            // Arrange — only the +1-level neighbour is reachable (the up-a-ledge case).
            var expected = new TileCoord(6, 5, 4);
            var (probe, _) = MakeProbe(new Dictionary<TileCoord, float> { [expected] = 7f });

            // Act
            var found = NeighbourColumnSearch.TryFindStandingTile(Tree, probe, out var tile, out var distance);

            // Assert
            Assert.IsTrue(found);
            Assert.AreEqual(expected, tile);
            Assert.AreEqual(7f, distance);
        }

        [TestMethod]
        public void TryFindStandingTile_ReturnsNeighbourOneLevelBelow()
        {
            // Arrange — only the -1-level neighbour is reachable (the down-a-ledge case).
            var expected = new TileCoord(5, 6, 2);
            var (probe, _) = MakeProbe(new Dictionary<TileCoord, float> { [expected] = 4f });

            // Act
            var found = NeighbourColumnSearch.TryFindStandingTile(Tree, probe, out var tile, out var distance);

            // Assert
            Assert.IsTrue(found);
            Assert.AreEqual(expected, tile);
            Assert.AreEqual(4f, distance);
        }

        [TestMethod]
        public void TryFindStandingTile_PicksNearest_AmongMultipleReachable()
        {
            // Arrange — three reachable candidates at different distances.
            var (probe, _) = MakeProbe(new Dictionary<TileCoord, float>
            {
                [new TileCoord(6, 5, 3)] = 5f,
                [new TileCoord(4, 5, 3)] = 2f, // nearest
                [new TileCoord(5, 6, 4)] = 9f,
            });

            // Act
            var found = NeighbourColumnSearch.TryFindStandingTile(Tree, probe, out var tile, out var distance);

            // Assert
            Assert.IsTrue(found);
            Assert.AreEqual(new TileCoord(4, 5, 3), tile);
            Assert.AreEqual(2f, distance);
        }

        #endregion

        #region Not-found case

        [TestMethod]
        public void TryFindStandingTile_ReturnsFalse_WhenNothingReachable()
        {
            // Arrange
            var (probe, queried) = MakeProbe(new Dictionary<TileCoord, float>());

            // Act
            var found = NeighbourColumnSearch.TryFindStandingTile(Tree, probe, out _, out _);

            // Assert
            Assert.IsFalse(found);
            Assert.HasCount(12, queried, "All candidates should still be probed when none is reachable.");
        }

        #endregion

        #region Search shape

        [TestMethod]
        public void TryFindStandingTile_ProbesExactlyTheTwelveOrthogonalPlusMinusOneCandidates()
        {
            // Arrange — nothing reachable forces the search to probe every candidate.
            var (probe, queried) = MakeProbe(new Dictionary<TileCoord, float>());
            var expected = new HashSet<TileCoord>
            {
                new(6, 5, 3), new(6, 5, 2), new(6, 5, 4),
                new(4, 5, 3), new(4, 5, 2), new(4, 5, 4),
                new(5, 6, 3), new(5, 6, 2), new(5, 6, 4),
                new(5, 4, 3), new(5, 4, 2), new(5, 4, 4),
            };

            // Act
            NeighbourColumnSearch.TryFindStandingTile(Tree, probe, out _, out _);

            // Assert
            Assert.HasCount(12, queried, "Should probe each candidate exactly once.");
            CollectionAssert.AreEquivalent(expected.ToList(), queried);
        }

        [TestMethod]
        public void TryFindStandingTile_NeverProbesDiagonalsOrBeyondOneLevel()
        {
            // Arrange
            var (probe, queried) = MakeProbe(new Dictionary<TileCoord, float>());

            // Act
            NeighbourColumnSearch.TryFindStandingTile(Tree, probe, out _, out _);

            // Assert
            foreach (var t in queried)
            {
                var dx = Math.Abs(t.X - Tree.X);
                var dy = Math.Abs(t.Y - Tree.Y);
                Assert.AreEqual(1, dx + dy, $"{t} is not an orthogonal neighbour column of the tree.");
                Assert.IsLessThanOrEqualTo(1, Math.Abs(t.Z - Tree.Z), $"{t} is more than one level from the tree.");
            }
        }

        [TestMethod]
        public void TryFindStandingTile_NeverProbesTheTreesOwnColumn()
        {
            // Arrange
            var (probe, queried) = MakeProbe(new Dictionary<TileCoord, float>());

            // Act
            NeighbourColumnSearch.TryFindStandingTile(Tree, probe, out _, out _);

            // Assert — the beaver cannot stand where the tree is.
            Assert.IsFalse(
                queried.Any(t => t.X == Tree.X && t.Y == Tree.Y),
                "The tree's own column must never be offered as a standing tile.");
        }

        #endregion

        #region Determinism

        [TestMethod]
        public void TryFindStandingTile_OnEqualDistance_PrefersTheTreesOwnLevel()
        {
            // Arrange — same column, equal distance, one tile at the tree's level and one below.
            var sameLevel = new TileCoord(6, 5, 3);
            var below = new TileCoord(6, 5, 2);
            var (probe, _) = MakeProbe(new Dictionary<TileCoord, float>
            {
                [sameLevel] = 5f,
                [below] = 5f,
            });

            // Act
            var found = NeighbourColumnSearch.TryFindStandingTile(Tree, probe, out var tile, out _);

            // Assert — deterministic: the tree's own level wins the tie.
            Assert.IsTrue(found);
            Assert.AreEqual(sameLevel, tile);
        }

        #endregion

        #region Guard clauses

        [TestMethod]
        public void TryFindStandingTile_ThrowsOnNullProbe()
        {
            // Arrange / Act
            var threw = false;
            try
            {
                NeighbourColumnSearch.TryFindStandingTile(Tree, null!, out _, out _);
            }
            catch (ArgumentNullException)
            {
                threw = true;
            }

            // Assert
            Assert.IsTrue(threw, "A null probe should throw ArgumentNullException.");
        }

        #endregion
    }
}
