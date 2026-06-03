using Bindito.Core;

namespace LedgeLogging
{
    /// <summary>
    /// Game-scope entry point for the Ledge Logging mod. Timberborn's mod loader
    /// scans loaded assemblies for <see cref="Configurator"/> implementations and
    /// runs <see cref="Configure"/> when the Game context is built; this is the
    /// canonical entry point for a Timberborn mod (preferred over IModStarter).
    /// </summary>
    [Context("Game")]
    internal sealed class LedgeLoggingConfigurator : Configurator
    {
        #region Configurator

        /// <summary>
        /// Registers the mod's bindings. Empty scaffold for now — the Harmony
        /// bootstrap (patching the lumberjack's tree-finding and reach) and any
        /// supporting services will be wired here.
        /// </summary>
        protected override void Configure()
        {
        }

        #endregion
    }
}
