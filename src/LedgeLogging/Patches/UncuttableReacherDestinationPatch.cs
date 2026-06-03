using System;
using System.Reflection;
using HarmonyLib;
using LedgeLogging.Game;
using Timberborn.ReservableSystem;
using Timberborn.WalkingSystem;

namespace LedgeLogging.Patches
{
    /// <summary>Resolves the <c>internal</c> <c>UncuttableReacher</c> type (the demolish reacher on every natural resource).</summary>
    internal static class UncuttableReacherAccess
    {
        #region Reflection

        /// <summary>The resolved <c>Timberborn.UncuttableYielding.UncuttableReacher</c> type.</summary>
        public static readonly Type ReacherType =
            AccessTools.TypeByName("Timberborn.UncuttableYielding.UncuttableReacher")
            ?? throw new InvalidOperationException(
                "[LedgeLogging] Type Timberborn.UncuttableYielding.UncuttableReacher not found.");

        #endregion
    }

    /// <summary>
    /// Patch 3 of 3 — approach. When a ledge approach is stashed for this resource, the
    /// worker's destination becomes the navmesh-reachable standing tile instead of the
    /// resource centre (which it can't path to across the ledge). The replacement is a
    /// <see cref="LedgeStandingDestination"/> rather than the vanilla
    /// <c>PositionDestination</c>, so the worker paths around impassable buildings instead
    /// of through them. The reacher's own <c>NotifyReservableReached</c> already faces the
    /// resource centre and does not reposition, so no arrival patch is needed.
    /// </summary>
    [HarmonyPatch]
    internal static class UncuttableReacherDestinationPatch
    {
        #region Patch

        private static MethodBase TargetMethod() =>
            AccessTools.DeclaredPropertyGetter(UncuttableReacherAccess.ReacherType, "Destination");

        /// <summary>Redirects the destination to a building-aware path to the standing tile.</summary>
        [HarmonyPostfix]
        private static void Postfix(ReservableReacher __instance, ref IDestination __result)
        {
            if (!LedgeApproachStore.TryGet(__instance, out var approach) || approach == null)
            {
                return;
            }
            __result = new LedgeStandingDestination(NavMeshServiceLocator.NavigationService, approach.StandingWorldCenter);
            LedgeDiagnostics.Once($"reach:{approach.StandingWorldCenter}", $"reacher: redirected destination to standing tile {approach.StandingWorldCenter}.");
        }

        #endregion
    }
}
