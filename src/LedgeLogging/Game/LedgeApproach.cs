using UnityEngine;

namespace LedgeLogging.Game
{
    /// <summary>
    /// The resolved ledge approach for one assignment: the world centre of the navmesh
    /// tile the worker should walk to and stand on to clear a resource it can't reach
    /// directly. A reference type so it can be stored in a
    /// <see cref="System.Runtime.CompilerServices.ConditionalWeakTable{TKey,TValue}"/>.
    /// </summary>
    /// <remarks>
    /// Facing is left to the reacher's own <c>NotifyReservableReached</c> (which already
    /// looks toward the resource centre), so only the standing position is carried here.
    /// </remarks>
    internal sealed class LedgeApproach
    {
        #region Construction

        /// <summary>Creates a ledge approach.</summary>
        /// <param name="standingWorldCenter">World centre of the tile the worker walks to and stands on.</param>
        public LedgeApproach(Vector3 standingWorldCenter)
        {
            StandingWorldCenter = standingWorldCenter;
        }

        #endregion

        #region Properties

        /// <summary>World centre of the navmesh-reachable tile the worker stands on.</summary>
        public Vector3 StandingWorldCenter { get; }

        #endregion
    }
}
