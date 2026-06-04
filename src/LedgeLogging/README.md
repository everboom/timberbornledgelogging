# src/LedgeLogging

The mod's only project. Compiles to `LedgeLogging.dll` (a Timberborn 1.0 mod
assembly) and deploys, with `manifest.json`, to the local Mods folder.

## Purpose

Let a worker **clear/remove** (demolish) a natural resource — tree or plant —
marked for destruction on an orthogonally-adjacent column one level **above** or some
levels **below** the worker's standing tile (the worker reaches across the ledge). Vanilla
forbids this because the resource's tile isn't reachable on the navmesh, so the resource
shows as "unreachable" and is never cleared. The mod extends the worker's *reach* (stand on
a neighbour tile and clear across the ledge), not the navmesh itself. The upward reach is
fixed at one level; how many levels **below** is a player setting (`{1, 2, 3, Any}`, default
`1`, which reproduces the original ±1) — see `Settings/`.

## Key types

- `LedgeLoggingModStarter` — `IModStarter` entry point. Runs before any Bindito
  scope exists; applies the assembly's Harmony patches (`PatchAll`) and warns
  loudly if the applied method count drifts from `ExpectedPatchedMethodCount`
  (keep that constant in sync as patches are added/removed).
- `LedgeLoggingConfigurator` — `[Context("Game")]` DI configurator. Binds
  `NavMeshServiceLocator` so the static patch code can reach Game-scope navigation
  services.

## Folders

- `Reachability/` — pure, BCL-only standing-tile search (unit-tested).
- `Game/` — game-coupled glue: the reachability adapter, the approach store, and the
  DI service locator (which also bridges the player setting to the static patches).
- `Patches/` — the three Harmony patches that implement the behaviour.
- `Settings/` — the `eMka.ModSettings` owner + configurator for the depth dropdown.

## How it fits together

This is a Harmony mod: there is no clean Bindito/blueprint extension seam for the
target behaviour (the load-bearing logic lives in `private`/`internal` members of
vanilla `Timberborn.Demolishing` / `Timberborn.UncuttableYielding`). See the
repo-root `CLAUDE.md` § "Technical background" for the exact patch points and rationale.
