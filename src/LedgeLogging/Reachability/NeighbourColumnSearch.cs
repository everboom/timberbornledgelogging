using System;

namespace LedgeLogging.Reachability
{
    /// <summary>
    /// Probe injected by the caller: can a beaver reach <paramref name="tile"/> on the
    /// terrain navmesh from the lumberjack flag, and if so at what walking
    /// <paramref name="distance"/>? Implemented game-side (e.g. via
    /// <c>Accessible.FindTerrainPath</c>); abstracted here so the search stays pure.
    /// </summary>
    /// <param name="tile">Candidate standing tile.</param>
    /// <param name="distance">Walking distance from the flag when reachable; otherwise undefined.</param>
    /// <returns><see langword="true"/> if the tile is navmesh-reachable from the flag.</returns>
    public delegate bool TileReachabilityProbe(TileCoord tile, out float distance);

    /// <summary>
    /// Pure search for a tile a lumberjack can stand on to cut a tree that the direct
    /// path-to-trunk check rejected. Given the tree's column and level, it considers the
    /// four orthogonally-adjacent columns at the tree's level and one level above/below
    /// (a ±1 vertical reach), and returns the navmesh-reachable candidate closest to the
    /// flag. This is the standing-tile half of the mod; the caller is responsible for then
    /// sending the beaver to that tile and facing the tree.
    /// </summary>
    /// <remarks>
    /// Game-independent by design (see the folder README): all Timberborn coupling — the
    /// actual reachability test and coordinate conversion — lives behind
    /// <see cref="TileReachabilityProbe"/>. Orthogonal-only adjacency matches Timberborn's
    /// terrain navmesh connectivity rule (it connects orthogonal neighbours only); diagonals
    /// are intentionally excluded. The search is fallback-only at the call site (run only
    /// when the direct path fails), which bounds its cost to at most 12 probes per tree.
    /// </remarks>
    public static class NeighbourColumnSearch
    {
        #region Constants

        /// <summary>
        /// The four orthogonal column offsets. Diagonals are excluded so the relaxation stays
        /// consistent with the vanilla terrain navmesh, which connects orthogonal neighbours only.
        /// </summary>
        private static readonly (int Dx, int Dy)[] OrthogonalOffsets =
        {
            (1, 0), (-1, 0), (0, 1), (0, -1),
        };

        /// <summary>
        /// Level deltas to try, ordered by preference for tie-breaking: the tree's own level
        /// first (most natural stance), then one below, then one above. The ±1 span is the
        /// mod's whole point and must not be silently widened.
        /// </summary>
        private static readonly int[] LevelDeltas = { 0, -1, 1 };

        #endregion

        #region Search

        /// <summary>
        /// Finds the navmesh-reachable standing tile closest to the flag from which the tree
        /// at <paramref name="treeColumn"/> can be cut.
        /// </summary>
        /// <param name="treeColumn">The tree's tile coordinate (column in X/Y, level in Z).</param>
        /// <param name="probe">Reachability test from the flag; see <see cref="TileReachabilityProbe"/>.</param>
        /// <param name="standingTile">The chosen standing tile, when one is found.</param>
        /// <param name="distance">Walking distance from the flag to <paramref name="standingTile"/>, when found.</param>
        /// <returns><see langword="true"/> if a reachable standing tile was found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="probe"/> is <see langword="null"/>.</exception>
        public static bool TryFindStandingTile(
            TileCoord treeColumn,
            TileReachabilityProbe probe,
            out TileCoord standingTile,
            out float distance)
        {
            if (probe == null)
            {
                throw new ArgumentNullException(nameof(probe));
            }

            standingTile = default;
            distance = float.PositiveInfinity;
            var found = false;

            foreach (var (dx, dy) in OrthogonalOffsets)
            {
                foreach (var dz in LevelDeltas)
                {
                    var candidate = new TileCoord(treeColumn.X + dx, treeColumn.Y + dy, treeColumn.Z + dz);

                    // Strict '<' keeps the first candidate on ties, so the fixed enumeration
                    // order (offsets above, then LevelDeltas) makes the result deterministic.
                    if (probe(candidate, out var candidateDistance) && candidateDistance < distance)
                    {
                        standingTile = candidate;
                        distance = candidateDistance;
                        found = true;
                    }
                }
            }

            return found;
        }

        #endregion
    }
}
