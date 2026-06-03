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
    /// four orthogonally-adjacent columns at the resource's level and up to
    /// <c>maxDepthBelow</c> levels <em>above</em> it — i.e. the worker stands level with the
    /// resource or higher and reaches <em>down</em> to it — and returns the navmesh-reachable
    /// candidate closest to the worker. This is the standing-tile half of the mod; the caller
    /// then sends the worker to that tile.
    /// </summary>
    /// <remarks>
    /// Game-independent by design (see the folder README): all Timberborn coupling — the
    /// reachability test and coordinate conversion — lives behind <see cref="TileReachabilityProbe"/>.
    /// <para>Downward-only: candidates are at the resource's level (<c>dz = 0</c>) and above
    /// (<c>dz = +1 … +maxDepthBelow</c>), never below, so a worker can clear a resource on
    /// lower ground but not one perched above it. Orthogonal-only adjacency matches the vanilla
    /// terrain navmesh, which connects orthogonal neighbours only.</para>
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

        #endregion

        #region Search

        /// <summary>
        /// Finds the navmesh-reachable standing tile closest to the worker from which the
        /// resource at <paramref name="resourceColumn"/> can be cleared.
        /// </summary>
        /// <param name="resourceColumn">The resource's tile coordinate (column in X/Y, level in Z).</param>
        /// <param name="maxDepthBelow">
        /// How many terrain levels below the worker the resource may be — i.e. the largest
        /// <c>dz</c> (standing level minus resource level) to consider. Must be ≥ 0; the worker's
        /// standing tile is searched at the resource's level through <c>+maxDepthBelow</c> above it.
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
                // dz = 0 (level with the resource) up to +maxDepthBelow (worker above, reaching
                // down). Ascending order means a shallower stance wins ties (strict '<' below),
                // and the fixed enumeration order keeps the result deterministic.
                for (var dz = 0; dz <= maxDepthBelow; dz++)
                {
                    var candidate = new TileCoord(resourceColumn.X + dx, resourceColumn.Y + dy, resourceColumn.Z + dz);

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
