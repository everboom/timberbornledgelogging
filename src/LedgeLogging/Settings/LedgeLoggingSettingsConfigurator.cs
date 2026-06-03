using Bindito.Core;

namespace LedgeLogging.Settings
{
    /// <summary>
    /// Binds <see cref="LedgeLoggingSettings"/> in both the <c>MainMenu</c> and <c>Game</c>
    /// scopes so the settings panel finds the owner whether opened from the main-menu mod list
    /// or from in-game options → mods.
    /// </summary>
    /// <remarks>
    /// Deliberately separate from <see cref="LedgeLoggingConfigurator"/> (Game-only): widening
    /// that to the main-menu scope would drag the Game-only <c>NavMeshServiceLocator</c> (which
    /// depends on game services) into the main-menu container, where it would fail to construct.
    /// Keeping the settings owner in its own dual-scope configurator scopes the MainMenu binding
    /// to just the owner — the same split Keystone uses.
    /// </remarks>
    [Context("MainMenu")]
    [Context("Game")]
    internal sealed class LedgeLoggingSettingsConfigurator : Configurator
    {
        /// <inheritdoc/>
        protected override void Configure()
        {
            Bind<LedgeLoggingSettings>().AsSingleton();
        }
    }
}
