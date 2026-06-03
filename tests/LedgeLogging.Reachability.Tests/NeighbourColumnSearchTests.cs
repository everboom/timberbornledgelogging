using System;
using System.Collections.Generic;
using System.Linq;
using LedgeLogging.Reachability;

namespace LedgeLogging.Reachability.Tests
{
    /// <summary>
    /// Unit tests for <see cref="NeighbourColumnSearch"/> — the pure standing-tile search.
    /// The game's reachability test is faked with an in-memory probe, so these pin the
    /// search's contract: orthogonal-only, downward-only, bounded by <c>maxDepthBelow</c>,
    /// nearest-reachable, deterministic.
    /// </summary>
    [TestClass]
    public sealed class NeighbourColumnSearchTests
    {
        #region Fixtures

        /// <summary>A resource sitting at column (5, 5), terrain level 3, used by most cases.</summary>
        private static readonly TileCoord Resource = new TileCoord(5, 5, 3);

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
            // Arrange — the worker stands level with the resource (dz = 0), adjacent.
            var expected = new TileCoord(6, 5, 3);
            var (probe, _) = MakeProbe(new Dictionary<TileCoord, float> { [expected] = 2f });

            // Act
            var found = NeighbourColumnSearch.TryFindStandingTile(Resource, maxDepthBelow: 1, probe, out var tile, out var distance);

            // Assert
            Assert.IsTrue(found);
            Assert.AreEqual(expected, tile);
            Assert.AreEqual(2f, distance);
        }

        [TestMethod]
        public void TryFindStandingTile_ReturnsNeighbourAboveTheResource_ReachingDown()
        {
            // Arrange — only the tile one level ABOVE the resource is reachable (worker stands
            // at z=4, resource at z=3 → resource one level below the worker).
            var expected = new TileCoord(6, 5, 4);
            var (probe, _) = MakeProbe(new Dictionary<TileCoord, float> { [expected] = 7f });

            // Act
            var found = NeighbourColumnSearch.TryFindStandingTile(Resource, maxDepthBelow: 1, probe, out var tile, out var distance);

            // Assert
            Assert.IsTrue(found);
            Assert.AreEqual(expected, tile);
            Assert.AreEqual(7f, distance);
        }

        [TestMethod]
        public void TryFindStandingTile_ReturnsDeepNeighbour_WithinMaxDepth()
        {
            // Arrange — resource three levels below the worker (dz = 3), allowed by maxDepthBelow 3.
            var expected = new TileCoord(5, 6, 6);
            var (probe, _) = MakeProbe(new Dictionary<TileCoord, float> { [expected] = 9f });

            // Act
            var found = NeighbourColumnSearch.TryFindStandingTile(Resource, maxDepthBelow: 3, probe, out var tile, out var distance);

            // Assert
            Assert.IsTrue(found);
            Assert.AreEqual(expected, tile);
            Assert.AreEqual(9f, distance);
        }

        [TestMethod]
        public void TryFindStandingTile_PicksNearest_AmongMultipleReachable()
        {
            // Arrange
            var (probe, _) = MakeProbe(new Dictionary<TileCoord, float>
            {
                [new TileCoord(6, 5, 3)] = 5f,
                [new TileCoord(4, 5, 5)] = 2f, // nearest
                [new TileCoord(5, 6, 4)] = 9f,
            });

            // Act
            var found = NeighbourColumnSearch.TryFindStandingTile(Resource, maxDepthBelow: 3, probe, out var tile, out var distance);

            // Assert
            Assert.IsTrue(found);
            Assert.AreEqual(new TileCoord(4, 5, 5), tile);
            Assert.AreEqual(2f, distance);
        }

        #endregion

        #region Not-found case

        [TestMethod]
        public void TryFindStandingTile_ReturnsFalse_WhenNothingReachable()
        {
            // Arrange — maxDepthBelow 2 → levels dz 0,1,2 over 4 columns = 12 candidates.
            var (probe, queried) = MakeProbe(new Dictionary<TileCoord, float>());

            // Act
            var found = NeighbourColumnSearch.TryFindStandingTile(Resource, maxDepthBelow: 2, probe, out _, out _);

            // Assert
            Assert.IsFalse(found);
            Assert.HasCount(12, queried, "4 columns x (maxDepthBelow + 1) levels should all be probed.");
        }

        #endregion

        #region Search shape

        [TestMethod]
        public void TryFindStandingTile_ProbesExactly_TheOrthogonalColumnsAtLevelThroughMaxDepthAbove()
        {
            // Arrange — maxDepthBelow 1 → dz in {0, +1} over the 4 orthogonal columns.
            var (probe, queried) = MakeProbe(new Dictionary<TileCoord, float>());
            var expected = new HashSet<TileCoord>
            {
                new(6, 5, 3), new(6, 5, 4),
                new(4, 5, 3), new(4, 5, 4),
                new(5, 6, 3), new(5, 6, 4),
                new(5, 4, 3), new(5, 4, 4),
            };

            // Act
            NeighbourColumnSearch.TryFindStandingTile(Resource, maxDepthBelow: 1, probe, out _, out _);

            // Assert
            Assert.HasCount(8, queried, "Should probe each candidate exactly once.");
            CollectionAssert.AreEquivalent(expected.ToList(), queried);
        }

        [TestMethod]
        public void TryFindStandingTile_NeverProbesAboveTheWorker_Diagonals_OrBeyondMaxDepth()
        {
            // Arrange
            var (probe, queried) = MakeProbe(new Dictionary<TileCoord, float>());
            const int maxDepthBelow = 2;

            // Act
            NeighbourColumnSearch.TryFindStandingTile(Resource, maxDepthBelow, probe, out _, out _);

            // Assert
            foreach (var t in queried)
            {
                var dx = Math.Abs(t.X - Resource.X);
                var dy = Math.Abs(t.Y - Resource.Y);
                var dz = t.Z - Resource.Z;
                Assert.AreEqual(1, dx + dy, $"{t} is not an orthogonal neighbour column of the resource.");
                Assert.IsGreaterThanOrEqualTo(0, dz, $"{t} is above the worker (negative dz) — upward reach is not allowed.");
                Assert.IsLessThanOrEqualTo(maxDepthBelow, dz, $"{t} is deeper than maxDepthBelow.");
            }
        }

        [TestMethod]
        public void TryFindStandingTile_NeverProbesTheResourcesOwnColumn()
        {
            // Arrange
            var (probe, queried) = MakeProbe(new Dictionary<TileCoord, float>());

            // Act
            NeighbourColumnSearch.TryFindStandingTile(Resource, maxDepthBelow: 3, probe, out _, out _);

            // Assert — the worker cannot stand where the resource is.
            Assert.IsFalse(
                queried.Any(t => t.X == Resource.X && t.Y == Resource.Y),
                "The resource's own column must never be offered as a standing tile.");
        }

        [TestMethod]
        public void TryFindStandingTile_WithZeroMaxDepth_ProbesOnlyTheResourceLevel()
        {
            // Arrange
            var (probe, queried) = MakeProbe(new Dictionary<TileCoord, float>());

            // Act
            NeighbourColumnSearch.TryFindStandingTile(Resource, maxDepthBelow: 0, probe, out _, out _);

            // Assert — 4 columns, only dz = 0.
            Assert.HasCount(4, queried);
            Assert.IsTrue(queried.All(t => t.Z == Resource.Z));
        }

        #endregion

        #region Determinism

        [TestMethod]
        public void TryFindStandingTile_OnEqualDistance_PrefersTheShallowerStance()
        {
            // Arrange — same column, equal distance, one tile level with the resource and one above.
            var sameLevel = new TileCoord(6, 5, 3);
            var above = new TileCoord(6, 5, 4);
            var (probe, _) = MakeProbe(new Dictionary<TileCoord, float>
            {
                [sameLevel] = 5f,
                [above] = 5f,
            });

            // Act
            var found = NeighbourColumnSearch.TryFindStandingTile(Resource, maxDepthBelow: 2, probe, out var tile, out _);

            // Assert — deterministic: the shallower (resource-level) stance wins the tie.
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
                NeighbourColumnSearch.TryFindStandingTile(Resource, maxDepthBelow: 1, null!, out _, out _);
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
