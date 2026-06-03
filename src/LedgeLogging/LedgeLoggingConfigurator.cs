using Bindito.Core;
using LedgeLogging.Game;

namespace LedgeLogging
{
    /// <summary>
    /// Game-scope DI configurator for the Ledge Logging mod. Timberborn's mod loader
    /// scans loaded assemblies for <see cref="Configurator"/> implementations and
    /// runs <see cref="Configure"/> when the Game context is built. This is the seam
    /// for <em>DI bindings</em> only; Harmony patches are applied earlier, from
    /// <see cref="LedgeLoggingModStarter"/> (an <c>IModStarter</c>), before any
    /// Bindito scope exists. This mod may register nothing here.
    /// </summary>
    [Context("Game")]
    internal sealed class LedgeLoggingConfigurator : Configurator
    {
        #region Configurator

        /// <summary>
        /// Registers the mod's Game-scope bindings. Binds <see cref="NavMeshServiceLocator"/>
        /// as a loadable singleton so it can publish Game-scope navigation services to the
        /// static Harmony patch code. The Harmony patches themselves are applied earlier,
        /// from <see cref="LedgeLoggingModStarter"/>, before any Bindito scope exists.
        /// </summary>
        protected override void Configure()
        {
            Bind<NavMeshServiceLocator>().AsSingleton();
        }

        #endregion
    }
}
