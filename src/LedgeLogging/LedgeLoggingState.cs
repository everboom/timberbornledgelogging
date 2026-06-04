using System;
using System.Threading;
using UnityEngine;

namespace LedgeLogging
{
    /// <summary>
    /// Global activation state and failure reporting for the mod. Every Harmony patch body
    /// is a postfix that checks <see cref="IsActive"/> before doing any work, so the mod can
    /// cleanly turn itself off — at startup if its patches do not install as expected, or at
    /// runtime if a patch body hits an unexpected error — without the danger of unpatching a
    /// method while it is executing.
    /// </summary>
    /// <remarks>
    /// The mod starts <em>inactive</em>. <see cref="LedgeLoggingModStarter"/> calls
    /// <see cref="MarkActive"/> only after it has verified the patches applied and the
    /// reflection targets resolved; if anything is off it stays inactive (and the starter
    /// unpatches), so a patch that somehow remains applied is still a no-op.
    /// <para>Harmony patch failures are <em>deterministic</em>, not transient — a renamed game
    /// member, an unresolved type, or a logic bug throws the same way on every call — and the
    /// three patches only work as a set, so there is nothing to retry. The first unexpected
    /// error therefore disables the whole mod for the session via <see cref="Disable"/> rather
    /// than being caught and ignored.</para>
    /// </remarks>
    internal static class LedgeLoggingState
    {
        #region State

        private static volatile bool _active;
        private static int _disableLogged;

        /// <summary>
        /// Whether the mod has verified itself and may act. <see langword="false"/> until
        /// <see cref="MarkActive"/>, and again after <see cref="Disable"/>.
        /// </summary>
        public static bool IsActive => _active;

        #endregion

        #region Transitions

        /// <summary>
        /// Marks the mod active after the starter has verified its patches, and logs a
        /// confirmation line so a healthy run is distinguishable from "the mod never loaded".
        /// </summary>
        public static void MarkActive()
        {
            _active = true;
            Debug.Log("[LedgeLogging] Patches verified; mod is active.");
        }

        /// <summary>
        /// Turns the mod off for the rest of the session and logs <paramref name="reason"/> as
        /// an error <em>once</em> — subsequent calls only flip the flag and stay silent, so a
        /// per-frame patch cannot flood the Player log. Patch bodies observe
        /// <see cref="IsActive"/> become <see langword="false"/> and stop affecting the game.
        /// </summary>
        /// <param name="reason">Human-readable cause, written to the Player log.</param>
        /// <param name="error">Optional exception to include for diagnosis.</param>
        public static void Disable(string reason, Exception? error = null)
        {
            _active = false;

            // Log exactly once. Interlocked guards against the (unlikely) case of a patch body
            // running off the main thread; the cost is trivial on the common path.
            if (Interlocked.Exchange(ref _disableLogged, 1) != 0)
            {
                return;
            }

            var detail = error == null ? string.Empty : $"\n{error}";
            Debug.LogError(
                $"[LedgeLogging] Disabled: {reason}. The mod has stopped affecting the game for "
                + $"this session; please report this with your Player.log.{detail}");
        }

        #endregion
    }
}
