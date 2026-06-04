using System.Collections.Generic;
using ModSettings.Common;
using ModSettings.Core;
using Timberborn.Modding;
using Timberborn.SettingsSystem;

namespace LedgeLogging.Settings
{
    /// <summary>
    /// Player-tunable mod settings, rendered as the "Ledge Logging" section in the
    /// <c>eMka.ModSettings</c> panel (main menu mod list and in-game options → mods).
    /// Exposes one control: how many terrain levels <em>below</em> a worker a marked tree or
    /// plant may be cleared from.
    /// </summary>
    /// <remarks>
    /// A <see cref="ModSettingsOwner"/> is an <c>ILoadableSingleton</c> that self-registers on
    /// load; bound in <see cref="LedgeLoggingSettingsConfigurator"/>. Uses the non-localized
    /// option/descriptor APIs (<see cref="NonLocalizedLimitedStringModSettingValue"/>,
    /// <c>ModSettingDescriptor.Create</c>), so the mod ships no localization file.
    /// </remarks>
    public class LedgeLoggingSettings : ModSettingsOwner
    {
        #region Setting

        /// <summary>
        /// Maximum terrain levels below the worker that a marked resource may be cleared from,
        /// as a dropdown of <see cref="DepthValues"/> ("1"/"2"/"3"/"Any"). Default "1" (which,
        /// with the always-on one-level upward reach, reproduces the original ±1 behaviour).
        /// </summary>
        public LimitedStringModSetting MaxDepthBelowSetting { get; } =
            new(defaultOptionIndex: 0, // "1"
                new List<NonLocalizedLimitedStringModSettingValue>
                {
                    new(DepthValues.One),
                    new(DepthValues.Two),
                    new(DepthValues.Three),
                    new(DepthValues.Any),
                },
                ModSettingDescriptor
                    .Create("Maximum levels below")
                    .SetTooltip("How many terrain levels below a worker it may reach down to clear a "
                        + "marked tree or plant. Reaching one level up is always allowed regardless. "
                        + "'Any' allows clearing from any reachable height above the resource."));

        #endregion

        #region ModSettingsOwner overrides

        /// <inheritdoc/>
        public override int Order => 0;

        /// <inheritdoc/>
        public override string HeaderLocKey => "Ledge Logging";

        /// <inheritdoc/>
        public override ModSettingsContext ChangeableOn =>
            ModSettingsContext.MainMenu | ModSettingsContext.Game;

        /// <inheritdoc/>
        protected override string ModId => "SylvanGames.LedgeLogging";

        /// <summary>Constructed by Bindito.</summary>
        public LedgeLoggingSettings(
            ISettings settings,
            ModSettingsOwnerRegistry modSettingsOwnerRegistry,
            ModRepository modRepository)
            : base(settings, modSettingsOwnerRegistry, modRepository)
        {
        }

        #endregion

        #region Resolved value

        /// <summary>
        /// The selected reach as an integer max-depth-below: "1"/"2"/"3" map to themselves and
        /// "Any" maps to <paramref name="mapHeight"/> (the cap that bounds the search). Unknown
        /// values fall back to <paramref name="mapHeight"/>.
        /// </summary>
        public int MaxDepthBelow(int mapHeight) => MaxDepthBelowSetting.Value switch
        {
            DepthValues.One => 1,
            DepthValues.Two => 2,
            DepthValues.Three => 3,
            _ => mapHeight,
        };

        /// <summary>Canonical persisted dropdown values for <see cref="MaxDepthBelowSetting"/>.</summary>
        public static class DepthValues
        {
            /// <summary>Clear resources at most one level below the worker.</summary>
            public const string One = "1";

            /// <summary>Clear resources at most two levels below the worker.</summary>
            public const string Two = "2";

            /// <summary>Clear resources at most three levels below the worker.</summary>
            public const string Three = "3";

            /// <summary>Clear resources any number of levels below the worker (capped at map height).</summary>
            public const string Any = "Any";
        }

        #endregion
    }
}
