using System;

namespace LedgeLogging.Reachability
{
    /// <summary>
    /// Probe injected by the caller: can a worker reach <paramref name="tile"/> on the
    /// terrain navmesh, and if so at what walking <paramref name="distance"/>? Implemented
    /// game-side (e.g. via road→terrain pathfinding); abstracted here so the search stays pure.
    /// </summary>
    /// <param name="tile">Candidate standing tile.</param>
    /// <param name="distance">Walking distance when reachable; otherwise undefined.</param>
    /// <returns><see langword="true"/> if the tile is navmesh-reachable.</returns>
    public delegate bool TileReachabilityProbe(TileCoord tile, out float distance);

    /// <summary>
    /// Pure search for a tile a worker can stand on to clear a resource the direct
    /// path-to-it check rejected. Given the resource's column and level, it considers the
    /// four orthogonally-adjacent columns at: the resource's level; one level below the
    /// resource (the worker reaches <em>up</em> — a fixed one-level allowance); and up to
    /// <c>maxDepthBelow</c> levels above the resource (the worker reaches <em>down</em> — the
    /// configurable side). It returns the navmesh-reachable candidate closest to the worker.
    /// This is the standing-tile half of the mod; the caller then sends the worker to that tile.
    /// </summary>
    /// <remarks>
    /// Game-independent by design (see the folder README): all Timberborn coupling — the
    /// reachability test and coordinate conversion — lives behind <see cref="TileReachabilityProbe"/>.
    /// <para>In standing-tile terms (<c>dz</c> = standing level − resource level): the candidates
    /// are <c>dz = -1</c> (worker one level below the resource → resource one level <em>above</em>
    /// the worker; fixed), <c>dz = 0</c> (level), and <c>dz = +1 … +maxDepthBelow</c> (worker
    /// above the resource → resource <em>below</em> the worker; configurable). Equivalently, a
    /// worker can clear a resource up to one level above it and up to <c>maxDepthBelow</c> levels
    /// below it. Upward reach is fixed at one and never widens. Orthogonal-only adjacency matches
    /// the vanilla terrain navmesh, which connects orthogonal neighbours only.</para>
    /// <para>Fallback-only at the call site (run only when the direct path fails), and the
    /// expensive probe is gated behind a cheap navmesh check, so cost stays bounded even at
    /// large depths.</para>
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
        /// How many levels <em>above</em> the worker a resource may sit and still be reachable.
        /// Fixed at one (the original ±1 up-reach) and not exposed as a setting; only the
        /// downward reach (<c>maxDepthBelow</c>) is configurable.
        /// </summary>
        private const int FixedUpwardReach = 1;

        #endregion

        #region Search

        /// <summary>
        /// Finds the navmesh-reachable standing tile closest to the worker from which the
        /// resource at <paramref name="resourceColumn"/> can be cleared.
        /// </summary>
        /// <param name="resourceColumn">The resource's tile coordinate (column in X/Y, level in Z).</param>
        /// <param name="maxDepthBelow">
        /// How many terrain levels <em>below the worker</em> the resource may be — the largest
        /// positive <c>dz</c> (standing level minus resource level) to consider. Must be ≥ 0. The
        /// fixed one-level <em>upward</em> reach (<c>dz = -1</c>) is always included regardless.
        /// </param>
        /// <param name="probe">Reachability test; see <see cref="TileReachabilityProbe"/>.</param>
        /// <param name="standingTile">The chosen standing tile, when one is found.</param>
        /// <param name="distance">Walking distance to <paramref name="standingTile"/>, when found.</param>
        /// <returns><see langword="true"/> if a reachable standing tile was found.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="probe"/> is <see langword="null"/>.</exception>
        public static bool TryFindStandingTile(
            TileCoord resourceColumn,
            int maxDepthBelow,
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
                var x = resourceColumn.X + dx;
                var y = resourceColumn.Y + dy;

                // Enumeration order is fixed and ties keep the first (strict '<'), so the result
                // is deterministic: level first, then the fixed one-level up reach, then deepening
                // downward reach.
                Consider(new TileCoord(x, y, resourceColumn.Z), probe, ref standingTile, ref distance, ref found);
                Consider(new TileCoord(x, y, resourceColumn.Z - FixedUpwardReach), probe, ref standingTile, ref distance, ref found);
                for (var dz = 1; dz <= maxDepthBelow; dz++)
                {
                    Consider(new TileCoord(x, y, resourceColumn.Z + dz), probe, ref standingTile, ref distance, ref found);
                }
            }

            return found;
        }

        /// <summary>Probes one candidate and keeps it if reachable and strictly nearer than the best so far.</summary>
        private static void Consider(
            TileCoord candidate,
            TileReachabilityProbe probe,
            ref TileCoord standingTile,
            ref float distance,
            ref bool found)
        {
            if (probe(candidate, out var candidateDistance) && candidateDistance < distance)
            {
                standingTile = candidate;
                distance = candidateDistance;
                found = true;
            }
        }

        #endregion
    }
}
