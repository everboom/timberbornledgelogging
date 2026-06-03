using LedgeLogging.Reachability;
using Timberborn.Coordinates;
using Timberborn.Navigation;
using UnityEngine;

namespace LedgeLogging.Game
{
    /// <summary>
    /// Adapter between the game and the pure <see cref="NeighbourColumnSearch"/> for the
    /// <em>demolishing</em> (clear/remove) system. It builds navmesh-backed
    /// <see cref="TileReachabilityProbe"/>s and translates between Timberborn's
    /// <c>Vector3Int</c> grid coordinates and the search's <see cref="TileCoord"/>.
    /// </summary>
    /// <remarks>
    /// Demolishers reach work via roads then terrain, so the worker-specific probe uses
    /// <see cref="Accessible.FindRoadToTerrainPath(Vector3, out float)"/> (not the
    /// terrain-only <c>FindTerrainPath</c>). The UI "unreachable" status has no worker
    /// context, so it uses a worker-independent district-road-spill probe instead.
    /// </remarks>
    internal static class LedgeReachability
    {
        #region Worker-specific search (finder gate / assignment)

        /// <summary>
        /// Tries to find a navmesh tile one orthogonal column from the resource and at most
        /// one level away that the worker at <paramref name="start"/> can reach via
        /// roads+terrain, to stand on while clearing the resource.
        /// </summary>
        /// <param name="start">The worker's accessible (pathfinding origin).</param>
        /// <param name="resourceCoordinates">The resource's grid coordinates (X/Y column, Z level).</param>
        /// <param name="standingWorldCenter">World centre of the chosen standing tile, when found.</param>
        /// <param name="distance">Walking distance from the worker to the standing tile, when found.</param>
        /// <returns><see langword="true"/> if a reachable standing tile was found.</returns>
        public static bool TryFindStandingTile(
            Accessible start,
            Vector3Int resourceCoordinates,
            out Vector3 standingWorldCenter,
            out float distance)
        {
            var navMesh = NavMeshServiceLocator.NavMeshService;

            TileReachabilityProbe probe = (TileCoord tile, out float candidateDistance) =>
            {
                var coordinates = new Vector3Int(tile.X, tile.Y, tile.Z);
                if (!navMesh.IsOnNavMesh(coordinates))
                {
                    candidateDistance = 0f;
                    return false;
                }
                return start.FindRoadToTerrainPath(CoordinateSystem.GridToWorldCentered(coordinates), out candidateDistance);
            };

            return Search(resourceCoordinates, probe, out standingWorldCenter, out distance);
        }

        #endregion

        #region Worker-independent reachability (UI status)

        /// <summary>
        /// Whether any ±1-level orthogonal neighbour tile of the resource is on a district
        /// road spill — i.e. some district's workers could stand there. Used to clear the
        /// "unreachable" selection status; it has no specific worker, unlike
        /// <see cref="TryFindStandingTile"/>.
        /// </summary>
        public static bool AnyNeighbourOnRoadSpill(Vector3Int resourceCoordinates)
        {
            var navMesh = NavMeshServiceLocator.NavMeshService;
            var districts = NavMeshServiceLocator.DistrictService;

            TileReachabilityProbe probe = (TileCoord tile, out float candidateDistance) =>
            {
                candidateDistance = 0f;
                var coordinates = new Vector3Int(tile.X, tile.Y, tile.Z);
                return navMesh.IsOnNavMesh(coordinates)
                    && districts.IsOnInstantDistrictRoadSpill(CoordinateSystem.GridToWorldCentered(coordinates));
            };

            return Search(resourceCoordinates, probe, out _, out _);
        }

        #endregion

        #region Diagnostics (temporary)

        private static readonly (int Dx, int Dy)[] DiagnosticOffsets = { (1, 0), (-1, 0), (0, 1), (0, -1) };
        private static readonly int[] DiagnosticLevels = { 0, -1, 1 };

        /// <summary>
        /// Logs, once per candidate, whether each ±1 neighbour tile of the resource is on the
        /// navmesh, road→terrain reachable from <paramref name="start"/>, and on a district
        /// road spill — to diagnose why a resource is rejected. Temporary.
        /// </summary>
        public static void DiagnoseCandidates(Accessible start, Vector3Int resourceCoordinates)
        {
            var navMesh = NavMeshServiceLocator.NavMeshService;
            var districts = NavMeshServiceLocator.DistrictService;

            LedgeDiagnostics.Once($"diag-start:{resourceCoordinates}",
                $"diag {resourceCoordinates}: start.UnblockedSingleAccess={start.UnblockedSingleAccess.HasValue}");

            foreach (var (dx, dy) in DiagnosticOffsets)
            {
                foreach (var dz in DiagnosticLevels)
                {
                    var coordinates = new Vector3Int(
                        resourceCoordinates.x + dx, resourceCoordinates.y + dy, resourceCoordinates.z + dz);
                    var onNavMesh = navMesh.IsOnNavMesh(coordinates);
                    var world = CoordinateSystem.GridToWorldCentered(coordinates);
                    var roadPath = onNavMesh && start.FindRoadToTerrainPath(world, out _);
                    var roadSpill = onNavMesh && districts.IsOnInstantDistrictRoadSpill(world);
                    LedgeDiagnostics.Once($"diag:{resourceCoordinates}:{coordinates}",
                        $"  cand {coordinates}: navmesh={onNavMesh} roadPath={roadPath} roadSpill={roadSpill}");
                }
            }
        }

        #endregion

        #region Shared

        private static bool Search(
            Vector3Int resourceCoordinates,
            TileReachabilityProbe probe,
            out Vector3 standingWorldCenter,
            out float distance)
        {
            var resourceColumn = new TileCoord(resourceCoordinates.x, resourceCoordinates.y, resourceCoordinates.z);
            if (NeighbourColumnSearch.TryFindStandingTile(resourceColumn, probe, out var standingTile, out distance))
            {
                standingWorldCenter = CoordinateSystem.GridToWorldCentered(
                    new Vector3Int(standingTile.X, standingTile.Y, standingTile.Z));
                return true;
            }

            standingWorldCenter = default;
            distance = 0f;
            return false;
        }

        #endregion
    }
}
