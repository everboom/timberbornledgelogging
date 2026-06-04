using System.Runtime.CompilerServices;
using Timberborn.ReservableSystem;

namespace LedgeLogging.Game
{
    /// <summary>
    /// Carries the ledge standing tile from the reachability gate patch (which knows the
    /// worker and resource) to the reacher patch (which drives navigation). The stash happens
    /// in the gate, not at job assignment: <c>DemolishJobProvider.GetJob</c> reserves and
    /// immediately reads the reacher's destination within its own call, before any assignment
    /// postfix would run. Keyed by the resource's <see cref="ReservableReacher"/> instance —
    /// there is one per resource, and a resource is reserved by at most one beaver at a time,
    /// so a single entry per reacher suffices. Entries are evicted automatically when the
    /// resource is collected.
    /// </summary>
    internal static class LedgeApproachStore
    {
        #region State

        private static readonly ConditionalWeakTable<ReservableReacher, LedgeApproach> Approaches = new();

        #endregion

        #region Operations

        /// <summary>Records (or replaces) the ledge approach for a resource's reacher.</summary>
        public static void Set(ReservableReacher reacher, LedgeApproach approach)
        {
            Approaches.AddOrUpdate(reacher, approach);
        }

        /// <summary>Gets the ledge approach for a reacher, if one is recorded.</summary>
        public static bool TryGet(ReservableReacher reacher, out LedgeApproach? approach)
        {
            return Approaches.TryGetValue(reacher, out approach);
        }

        /// <summary>Removes any ledge approach for a reacher (no-op if none).</summary>
        public static void Clear(ReservableReacher reacher)
        {
            Approaches.Remove(reacher);
        }

        #endregion
    }
}
