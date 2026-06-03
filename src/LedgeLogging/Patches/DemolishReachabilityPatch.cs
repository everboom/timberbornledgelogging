using HarmonyLib;
using LedgeLogging.Game;
using Timberborn.Demolishing;
using Timberborn.Navigation;

namespace LedgeLogging.Patches
{
    /// <summary>
    /// Patch 1 of 3 — reachability gate. <c>ReachableDemolishable.IsReachable(start, out
    /// distance)</c> decides whether a worker at <c>start</c> can reach a marked resource;
    /// <c>DemolishJobProvider</c> drops those it rejects. This postfix, when the direct
    /// road→terrain path to a <em>natural resource</em> fails, falls back to the ledge
    /// search: if a navmesh-reachable standing tile exists (one column over, at the resource's
    /// level or up to the player-configured number of levels above it — reaching down), the
    /// resource is accepted with that tile's distance.
    /// </summary>
    [HarmonyPatch(typeof(ReachableDemolishable), nameof(ReachableDemolishable.IsReachable),
        new[] { typeof(Accessible), typeof(float) },
        new[] { ArgumentType.Normal, ArgumentType.Out })]
    internal static class DemolishReachabilityGatePatch
    {
        #region Patch

        /// <summary>Accepts an otherwise-unreachable natural resource via a ledge standing tile.</summary>
        /// <param name="__instance">The resource's reachability component.</param>
        /// <param name="start">The worker's accessible (matches the original parameter name).</param>
        /// <param name="distance">Out distance (matches the original parameter name); set on a ledge hit.</param>
        /// <param name="__result">The reachability result; set true here when the fallback succeeds.</param>
        [HarmonyPostfix]
        private static void Postfix(ReachableDemolishable __instance, Accessible start, ref float distance, ref bool __result)
        {
            if (!DemolishPatchSupport.TryGetNaturalResourceCoordinates(__instance, out var coordinates))
            {
                return;
            }
            var reacher = __instance.GetComponent<DemolishableReacher>();

            // Already reachable by the normal road→terrain path — make sure no stale ledge
            // redirect lingers, then leave vanilla behaviour alone.
            if (__result)
            {
                if (reacher != null)
                {
                    LedgeApproachStore.Clear(reacher);
                }
                return;
            }

            // Stash the standing tile HERE, not at job assignment: DemolishJobProvider.GetJob
            // reserves and immediately drives navigation (which reads the reacher's
            // destination) within its own call, before any GetJob postfix would run.
            if (LedgeReachability.TryFindStandingTile(start, coordinates, NavMeshServiceLocator.MaxDepthBelow, out var standingWorldCenter, out var ledgeDistance))
            {
                __result = true;
                distance = ledgeDistance;
                if (reacher != null)
                {
                    LedgeApproachStore.Set(reacher, new LedgeApproach(standingWorldCenter));
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Patch 2 of 3 — selection status. <c>ReachableDemolishable.IsUnreachable()</c> drives
    /// the "UnreachableObject" status shown when a marked resource is selected. This postfix
    /// clears that status for a natural resource whose ledge neighbour tile is on a district
    /// road spill — so the UI matches the relaxed reachability instead of wrongly warning.
    /// </summary>
    [HarmonyPatch(typeof(ReachableDemolishable), nameof(ReachableDemolishable.IsUnreachable))]
    internal static class DemolishReachabilityStatusPatch
    {
        #region Patch

        /// <summary>Clears the unreachable status for a ledge-reachable natural resource.</summary>
        /// <param name="__instance">The resource's reachability component.</param>
        /// <param name="__result">The unreachable result; cleared here when a ledge tile is reachable.</param>
        [HarmonyPostfix]
        private static void Postfix(ReachableDemolishable __instance, ref bool __result)
        {
            if (!__result)
            {
                return;
            }
            if (!DemolishPatchSupport.TryGetNaturalResourceCoordinates(__instance, out var coordinates))
            {
                return;
            }
            if (LedgeReachability.AnyNeighbourOnRoadSpill(coordinates, NavMeshServiceLocator.MaxDepthBelow))
            {
                __result = false;
            }
        }

        #endregion
    }
}
