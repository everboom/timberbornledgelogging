using System;
using System.Linq;
using HarmonyLib;
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
    public sealed class LedgeLoggingModStarter : IModStarter
    {
        #region Constants

        /// <summary>Harmony instance id; kept distinct per assembly so this mod coexists with others.</summary>
        public const string HarmonyId = "SylvanGames.LedgeLogging";

        /// <summary>
        /// Number of methods this mod's patches are expected to apply. Bump this in
        /// lockstep with every <c>[HarmonyPatch]</c> added or removed. A mismatch
        /// against the count Harmony actually applies means a patch target was
        /// renamed or removed by a game update — surfaced as a loud warning rather
        /// than silent in-game breakage. The three patched methods are
        /// <c>ReachableDemolishable.IsReachable</c> (gate + standing-tile stash),
        /// <c>ReachableDemolishable.IsUnreachable</c> (UI status), and
        /// <c>UncuttableReacher.Destination</c> (getter) — see the Patches folder.
        /// </summary>
        public const int ExpectedPatchedMethodCount = 3;

        #endregion

        #region IModStarter

        /// <summary>
        /// Applies all Harmony patches in this assembly and verifies the applied
        /// method count matches <see cref="ExpectedPatchedMethodCount"/>.
        /// </summary>
        /// <param name="modEnvironment">Mod environment supplied by the loader (unused).</param>
        public void StartMod(IModEnvironment modEnvironment)
        {
            var harmony = new Harmony(HarmonyId);
            try
            {
                harmony.PatchAll(typeof(LedgeLoggingModStarter).Assembly);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[LedgeLogging] Harmony PatchAll threw: {ex}");
                return;
            }

            // No silent failure: count the methods WE patched and report it. A Timberborn
            // update that renames/removes a target leaves PatchAll applying fewer methods
            // than expected; that mismatch must surface as a loud warning, not silent
            // in-game breakage. On success we still log a confirmation line — otherwise an
            // empty log is indistinguishable from "the mod never ran".
            var ours = harmony.GetPatchedMethods()
                .Where(m => m != null && Harmony.GetPatchInfo(m)?.Owners?.Contains(HarmonyId) == true)
                .ToList();
            var patchedList = string.Join("\n", ours.Select(m => $"  {m.DeclaringType?.FullName}.{m.Name}"));
            if (ours.Count == ExpectedPatchedMethodCount)
            {
                Debug.Log(
                    $"[LedgeLogging] Harmony applied {ours.Count} patch(es) (expected {ExpectedPatchedMethodCount}):\n{patchedList}");
            }
            else
            {
                Debug.LogWarning(
                    $"[LedgeLogging] Harmony patch count mismatch: applied {ours.Count}, expected " +
                    $"{ExpectedPatchedMethodCount}. A target may have been renamed in a game update.\n{patchedList}");
            }
        }

        #endregion
    }
}
