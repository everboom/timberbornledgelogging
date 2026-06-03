using System.Collections.Generic;
using Timberborn.Navigation;
using Timberborn.WalkingSystem;
using UnityEngine;

namespace LedgeLogging.Game
{
    /// <summary>
    /// Sends a worker to the ledge standing tile via <em>road-spill-or-terrain</em>
    /// pathfinding, which respects impassable buildings.
    /// </summary>
    /// <remarks>
    /// The vanilla <c>PositionDestination</c> pathfinds with
    /// <c>NavigationService.FindPathUnlimitedRange</c> → <c>FindPathUncached</c>, which tries
    /// a terrain path <em>before</em> the road-spill path; the terrain pathfinder ignores the
    /// building-restricted-node mask, so a worker sent to a terrain tile can cut straight
    /// through an impassable building. <see cref="INavigationService.FindRoadSpillOrTerrainPathUnlimitedRange"/>
    /// prefers the road-spill path (which honours building obstacles via the district graph),
    /// matching how the reachability gate already routed around buildings.
    /// </remarks>
    internal sealed class LedgeStandingDestination : IDestination
    {
        #region Construction

        private readonly INavigationService _navigationService;
        private readonly Vector3[] _ends;

        /// <summary>Creates a destination targeting <paramref name="standingWorldCenter"/>.</summary>
        public LedgeStandingDestination(INavigationService navigationService, Vector3 standingWorldCenter)
        {
            _navigationService = navigationService;
            _ends = new[] { standingWorldCenter };
        }

        #endregion

        #region IDestination

        /// <inheritdoc/>
        public bool FindPath(Vector3 start, List<PathCorner> pathCorners, out float distance)
        {
            return _navigationService.FindRoadSpillOrTerrainPathUnlimitedRange(start, _ends, pathCorners, out distance);
        }

        #endregion
    }
}
