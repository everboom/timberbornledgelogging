using System;
using System.Linq;
using HarmonyLib;
using LedgeLogging.Patches;
using Timberborn.ModManagerScene;
using UnityEngine;

namespace LedgeLogging
{
    /// <summary>
    /// Mod entry point. Timberborn's mod loader invokes <see cref="StartMod"/> via
    /// <see cref="IModStarter"/> before any Bindito scope spins up — the earliest
    /// hook the game offers, and the correct place to apply Harmony patches so they
    /// are live before any downstream <c>ILoadableSingleton.Load()</c> fires.
    /// </summary>
    /// <remarks>
    /// Startup is fail-safe: the mod only becomes active (<see cref="LedgeLoggingState"/>)
    /// after the patches verify. If <c>PatchAll</c> throws, applies the wrong number of
    /// methods, or a reflection target fails to resolve, the starter rolls the patches back
    /// (<see cref="Harmony.UnpatchAll(string)"/> — only this mod's) and leaves the game exactly
    /// as vanilla, logging the reason. Since the patch bodies are no-ops while inactive, the
    /// mod cannot half-install or crash the game on a botched startup.
    /// </remarks>
    public sealed class LedgeLoggingModStarter : IModStarter
    {
        #region Constants

        /// <summary>Harmony instance id; kept distinct per assembly so this mod coexists with others.</summary>
        public const string HarmonyId = "SylvanGames.LedgeLogging";

        /// <summary>
        /// Number of methods this mod's patches are expected to apply. Bump this in
        /// lockstep with every <c>[HarmonyPatch]</c> added or removed. A mismatch
        /// against the count Harmony actually applies means a patch target was
        /// renamed or removed by a game update — which now <em>disables</em> the mod
        /// (after rollback), not merely warns, so it never runs half-patched. The three
        /// patched methods are <c>ReachableDemolishable.IsReachable</c> (gate +
        /// standing-tile stash), <c>ReachableDemolishable.IsUnreachable</c> (UI status),
        /// and <c>UncuttableReacher.Destination</c> (getter) — see the Patches folder.
        /// </summary>
        public const int ExpectedPatchedMethodCount = 3;

        #endregion

        #region IModStarter

        /// <summary>
        /// Applies and verifies this assembly's Harmony patches, activating the mod only if
        /// everything checks out. Any failure — including an unexpected one in this method
        /// itself — rolls the patches back and leaves the mod disabled; it never throws into
        /// the game.
        /// </summary>
        /// <param name="modEnvironment">Mod environment supplied by the loader (unused).</param>
        public void StartMod(IModEnvironment modEnvironment)
        {
            var harmony = new Harmony(HarmonyId);
            try
            {
                Install(harmony);
            }
            catch (Exception ex)
            {
                // Last-resort guard: a bug anywhere in our own startup path must never bubble
                // out into the game. Roll back whatever applied and stay disabled.
                Rollback(harmony, "unexpected error during startup", ex);
            }
        }

        #endregion

        #region Install / rollback

        /// <summary>
        /// Applies the patches and runs the three startup checks (applied, counted, reflection
        /// resolves). On the first failed check it rolls back and returns; on success it arms
        /// the mod.
        /// </summary>
        private static void Install(Harmony harmony)
        {
            // 1. Apply the patches.
            try
            {
                harmony.PatchAll(typeof(LedgeLoggingModStarter).Assembly);
            }
            catch (Exception ex)
            {
                Rollback(harmony, "Harmony PatchAll threw", ex);
                return;
            }

            // 2. Verify we patched exactly the methods we expect. A Timberborn update that
            // renames or removes a target leaves PatchAll applying a different set; refuse to
            // run half-patched.
            var ours = harmony.GetPatchedMethods()
                .Where(m => m != null && Harmony.GetPatchInfo(m)?.Owners?.Contains(HarmonyId) == true)
                .ToList();
            var patchedList = string.Join("\n", ours.Select(m => $"  {m.DeclaringType?.FullName}.{m.Name}"));
            if (ours.Count != ExpectedPatchedMethodCount)
            {
                Rollback(harmony,
                    $"patch count mismatch (applied {ours.Count}, expected {ExpectedPatchedMethodCount} — "
                        + $"a target may have been renamed in a game update):\n{patchedList}");
                return;
            }

            // 3. Verify the reflection targets the runtime patches depend on actually resolve,
            // so a missing internal type/field surfaces here (loud, once) rather than throwing
            // on the first in-game demolish.
            try
            {
                UncuttableReacherAccess.EnsureResolved();
            }
            catch (Exception ex)
            {
                Rollback(harmony, "reflection target validation failed", ex);
                return;
            }

            // 4. All checks passed — switch the patch bodies on.
            LedgeLoggingState.MarkActive();
            Debug.Log(
                $"[LedgeLogging] Harmony applied {ours.Count} patch(es) (expected {ExpectedPatchedMethodCount}):\n{patchedList}");
        }

        /// <summary>
        /// Disables the mod and removes any patches this Harmony id applied, so a failed or
        /// partial install leaves the game exactly as vanilla. Safe to call when nothing was
        /// patched, and only ever unpatches methods owned by <see cref="HarmonyId"/> — never
        /// other mods' patches.
        /// </summary>
        private static void Rollback(Harmony harmony, string reason, Exception? error = null)
        {
            LedgeLoggingState.Disable($"could not install ({reason})", error);
            try
            {
                harmony.UnpatchAll(HarmonyId);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LedgeLogging] UnpatchAll during rollback threw: {ex}");
            }
        }

        #endregion
    }
}
