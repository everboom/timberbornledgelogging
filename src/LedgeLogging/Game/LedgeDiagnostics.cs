using System.Collections.Generic;
using UnityEngine;

namespace LedgeLogging.Game
{
    /// <summary>
    /// Lightweight de-duplicated logging for tracing the ledge-removal flow in-game.
    /// <see cref="Once"/> emits each distinct <c>key</c> only once, so hot paths (the
    /// reachability gate runs per job per builder per tick) don't flood the log.
    /// Temporary diagnostics — remove once the behaviour is verified working.
    /// </summary>
    internal static class LedgeDiagnostics
    {
        #region Logging

        private static readonly HashSet<string> Seen = new();

        /// <summary>Logs <paramref name="message"/> the first time <paramref name="key"/> is seen.</summary>
        public static void Once(string key, string message)
        {
            lock (Seen)
            {
                if (!Seen.Add(key))
                {
                    return;
                }
            }
            Debug.Log($"[LedgeLogging] {message}");
        }

        #endregion
    }
}
