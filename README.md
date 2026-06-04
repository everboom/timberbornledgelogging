# Ledge Logging

A small mod for **Timberborn 1.0** that lets your beavers clear trees and plants
marked for removal even when they sit on a **ledge** — one level above, or a few
levels below, the tile a worker can actually stand on.

## The problem it solves

When you mark a tree or plant with the **Clear / Delete** tool, a worker only goes
to remove it if they can *path all the way to the resource's own tile*. A single
1-block step up or down a cliff is impassable in Timberborn, so a tree perched on a
ledge — right next to a beaver, but one level up or down — is reported
**"unreachable"** and never gets cleared. You end up unable to tidy up cliff edges,
terraced ground, or the lip of a dig site without first building stairs or ramps
just to reach a tree you're about to delete anyway.

**Ledge Logging** relaxes only the *reach*, not the game's pathfinding: a worker
walks to a normal, reachable tile on the column **next to** the resource and clears
it across the ledge. Nothing else about beaver movement changes.

## What it does

- Lets a worker clear a marked tree or plant on an adjacent column up to **one level
  above** them, or up to a few levels **below** (configurable — see settings).
- Trees and plants only; buildings and ruins are unaffected.

## Requirements

These are separate mods that must also be installed (Ledge Logging does **not**
bundle them):

| Mod | Why |
| --- | --- |
| **Harmony** | Runtime patching framework this mod is built on. |
| **ModSettings** | Provides the in-game settings panel for the reach setting. |

Both are declared in `manifest.json`, and Timberborn will warn you if they're
missing.

## Installing

Subscribe to Ledge Logging on the Steam Workshop and the game loads it on next
launch. Make sure you're also subscribed to its required mods (above).

## Using it

Just play as normal: use the **Clear / Delete** tool to mark a tree or plant on a
ledge. Where vanilla would show *"unreachable"* and leave it standing, a worker now
walks to an adjacent tile and clears it across the ledge.

### Settings

Open **ModSettings** (from the main-menu mod list or in-game **Options → Mods**)
and find the **Ledge Logging** section:

- **Maximum levels below** — `1` / `2` / `3` / `Any` (default **`1`**). How many
  terrain levels *below a worker* a marked tree or plant may be cleared from.
  Reaching one level **up** is always allowed regardless, so the default `1`
  reproduces a symmetric one-level-up / one-level-down reach. `Any` lets a worker
  clear a resource from any reachable height above it (capped at the map height).

The setting takes effect on the next reachability check, so you can tune it
mid-game.

## Known limitation

A worker sent to a ledge tile may path across an *impassable building* to get there.
This is the same off-road terrain pathing vanilla uses for any natural-resource
removal, so it's accepted as base-game behaviour rather than worked around.

> **About the name:** "Logging" is a historical misnomer — the mod started life
> scoped to lumberjack cutting, but it's really about **removal via the Clear/Delete
> tool**. The id (`SylvanGames.LedgeLogging`) is kept for compatibility.

## Building from source

Code-only Harmony mod — no Unity assets or SDK round-trip.

- `dotnet build` — builds and deploys `LedgeLogging.dll` + `manifest.json` to your
  `Documents\Timberborn\Mods\LedgeLogging\` folder (always Release).
- `dotnet build -p:LedgeLoggingDeploy=false` — build without deploying.
- `dotnet test` — runs the unit tests for the pure reachability core. These build
  without a Timberborn install (the test project links the BCL-only source rather
  than referencing the game-coupled mod assembly).

Per-machine paths are not committed. Set `TimberbornInstallDir` via (highest
precedence first): `-p:TimberbornInstallDir=<path>`, the
`LEDGELOGGING_TIMBERBORN_DIR` environment variable, or a gitignored
`Directory.Build.local.props` (copy `Directory.Build.local.props.example`). The
ModSettings reference resolves similarly via `ModSettingsDir` /
`LEDGELOGGING_MODSETTINGS_DIR`. Optional deploy redirect: `LEDGELOGGING_DEPLOY_DIR`.

### Layout

```
LedgeLogging.slnx
├── src/LedgeLogging                       netstandard2.1 — the mod assembly (AssemblyName=LedgeLogging)
│   ├── Reachability                       BCL-only pure core (the unit-tested standing-tile search)
│   ├── Game                               game-coupled glue (navmesh probes, service locator)
│   ├── Patches                            the three Harmony patches on the demolish system
│   └── Settings                           the ModSettings reach setting
└── tests/LedgeLogging.Reachability.Tests  MSTest — links and exercises the pure core
```
