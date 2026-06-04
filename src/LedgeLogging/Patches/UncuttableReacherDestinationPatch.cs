using System;
using System.Reflection;
using HarmonyLib;
using LedgeLogging.Game;
using Timberborn.ReservableSystem;
using Timberborn.WalkingSystem;

namespace LedgeLogging.Patches
{
    /// <summary>Resolves the <c>internal</c> <c>UncuttableReacher</c> type and its private destination factory.</summary>
    internal static class UncuttableReacherAccess
    {
        #region Reflection

        /// <summary>The resolved <c>Timberborn.UncuttableYielding.UncuttableReacher</c> type.</summary>
        public static readonly Type ReacherType =
            AccessTools.TypeByName("Timberborn.UncuttableYielding.UncuttableReacher")
            ?? throw new InvalidOperationException(
                "[LedgeLogging] Type Timberborn.UncuttableYielding.UncuttableReacher not found.");

        private static readonly FieldInfo FactoryField =
            AccessTools.Field(ReacherType, "_positionDestinationFactory")
            ?? throw new InvalidOperationException(
                "[LedgeLogging] Field UncuttableReacher._positionDestinationFactory not found.");

        /// <summary>Reads the reacher's injected <see cref="PositionDestinationFactory"/>.</summary>
        public static PositionDestinationFactory GetFactory(object reacher) =>
            (PositionDestinationFactory)FactoryField.GetValue(reacher);

        /// <summary>
        /// Forces resolution of the reacher type and its destination-factory field, throwing if
        /// either is missing. Called once at startup (<see cref="LedgeLoggingModStarter"/>) so a
        /// renamed/removed game member fails loudly there — and disables the mod — rather than
        /// throwing on the first in-game demolish. Touching the members runs the field
        /// initializers, whose null-coalescing throws surface a missing type/field.
        /// </summary>
        public static void EnsureResolved()
        {
            _ = ReacherType;
            _ = FactoryField;
        }

        #endregion
    }

    /// <summary>
    /// Patch 3 of 3 — approach. When a ledge approach is stashed for this resource, the
    /// worker's destination becomes the navmesh-reachable standing tile instead of the
    /// resource centre (which it can't path to across the ledge).
    /// </summary>
    /// <remarks>
    /// The replacement is a <see cref="PositionDestination"/> built by the reacher's own
    /// factory — a built-in destination type. It must be built-in: the game serializes a
    /// walker's current destination on save (<c>DestinationValueSerializer</c>), and only
    /// <c>PositionDestination</c>/<c>AccessibleDestination</c> are serializable — any custom
    /// <see cref="IDestination"/> throws during save. This is the same destination type and
    /// pathfinding vanilla uses to send a worker to a natural resource.
    /// </remarks>
    [HarmonyPatch]
    internal static class UncuttableReacherDestinationPatch
    {
        #region Constants

        /// <summary>Stopping distance for the standing-tile destination; zero stands the worker on the tile centre.</summary>
        private const float LedgeStoppingDistance = 0f;

        #endregion

        #region Patch

        private static MethodBase TargetMethod() =>
            AccessTools.DeclaredPropertyGetter(UncuttableReacherAccess.ReacherType, "Destination");

        /// <summary>Redirects the destination to the standing tile for a ledge removal.</summary>
        [HarmonyPostfix]
        private static void Postfix(ReservableReacher __instance, ref IDestination __result)
        {
            if (!LedgeLoggingState.IsActive)
            {
                return;
            }
            try
            {
                if (!LedgeApproachStore.TryGet(__instance, out var approach) || approach == null)
                {
                    return;
                }
                var factory = UncuttableReacherAccess.GetFactory(__instance);
                __result = factory.Create(approach.StandingWorldCenter, LedgeStoppingDistance);
            }
            catch (Exception ex)
            {
                LedgeLoggingState.Disable("runtime error in the reacher destination patch", ex);
            }
        }

        #endregion
    }
}
