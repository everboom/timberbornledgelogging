using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.NaturalResources;
using UnityEngine;

namespace LedgeLogging.Patches
{
    /// <summary>
    /// Shared gating for the demolish patches. The ledge relaxation applies only to
    /// natural resources (trees, plants) — buildings and ruins are demolished from
    /// roads/accesses and must keep vanilla reachability.
    /// </summary>
    internal static class DemolishPatchSupport
    {
        #region Gate

        /// <summary>
        /// Returns whether <paramref name="component"/> belongs to a natural resource and,
        /// if so, its grid coordinates (X/Y column, Z level).
        /// </summary>
        public static bool TryGetNaturalResourceCoordinates(BaseComponent component, out Vector3Int coordinates)
        {
            coordinates = default;
            if (component.GetComponent<NaturalResource>() == null)
            {
                return false;
            }
            var blockObject = component.GetComponent<BlockObject>();
            if (blockObject == null)
            {
                return false;
            }
            coordinates = blockObject.Coordinates;
            return true;
        }

        #endregion
    }
}
