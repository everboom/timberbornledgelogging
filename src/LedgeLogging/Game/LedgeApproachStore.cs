using System.Runtime.CompilerServices;
using Timberborn.ReservableSystem;

namespace LedgeLogging.Game
{
    /// <summary>
    /// Carries the ledge-cut standing tile from the assignment patch (which knows the
    /// flag and tree) to the reacher patches (which drive navigation and arrival).
    /// Keyed by the tree's <see cref="ReservableReacher"/> instance — there is one per
    /// tree, and a tree is reserved by at most one beaver at a time, so a single entry
    /// per reacher suffices. Entries are evicted automatically when the tree is collected.
    /// </summary>
    internal static class LedgeApproachStore
    {
        #region State

        private static readonly ConditionalWeakTable<ReservableReacher, LedgeApproach> Approaches = new();

        #endregion

        #region Operations

        /// <summary>Records (or replaces) the ledge approach for a tree's reacher.</summary>
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
