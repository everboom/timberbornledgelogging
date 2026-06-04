# src/LedgeLogging/Settings

Player-facing mod settings, via the external **`eMka.ModSettings`** mod (declared as a
`RequiredMods` entry in `manifest.json`; referenced compile-only — see the csproj/
`Directory.Build.props` `ModSettingsDir`). `eMka.ModSettings` has no dependencies of its
own, so it's the only extra mod players need beyond Harmony.

## Key types

- `LedgeLoggingSettings` — a `ModSettingsOwner` exposing one dropdown,
  **"Maximum levels below" `{1, 2, 3, Any}`** (default `1`): how many terrain levels
  *below* a worker a marked tree/plant may be cleared from. The *upward* reach is fixed at
  one level (not a setting), so the default reproduces the original ±1. Uses the non-localized option
  and descriptor APIs (`NonLocalizedLimitedStringModSettingValue`, `ModSettingDescriptor.Create`),
  so the mod ships **no localization file**. `MaxDepthBelow(mapHeight)` resolves the choice
  to an int (`Any` → map height, the search's cap).
- `LedgeLoggingSettingsConfigurator` — `[Context("MainMenu")] [Context("Game")]`, binds the
  owner as a singleton. Kept **separate** from `LedgeLoggingConfigurator` (Game-only) so the
  main-menu scope doesn't pull in `NavMeshServiceLocator` (which needs game services). Same
  split Keystone uses.

## How the value reaches the patches

`ModSettingsOwner` is an `ILoadableSingleton` that self-registers on load. The static
Harmony patches can't be injected, so `Game/NavMeshServiceLocator` captures the Game-scope
`LedgeLoggingSettings` + `ITerrainService` and exposes `MaxDepthBelow` — resolved **live on
each read**, so changing the dropdown takes effect on the next reachability evaluation
without a reload. The reachability gate passes it into `NeighbourColumnSearch`.

There is no "off" value — uninstalling the mod is "off". The pattern (owner shape, ctor,
overrides, dual-context binding) mirrors Keystone's `Settings/` classes.
