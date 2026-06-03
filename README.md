# Ledge Logging

A small Timberborn 1.0 mod: lets lumberjacks cut trees on a neighbouring tile
**one terrain level above or below** them. Vanilla forbids this because a 1-level
step is impassable on the terrain navmesh, so a tree at a cliff edge is never
offered to a lumberjack even when a beaver is standing right beneath (or above) it.

- **Id:** `SylvanGames.LedgeLogging`
- **Requires:** Harmony (declared in `manifest.json`, not bundled)
- **Stack:** code-only Harmony mod — no Unity assets, no Unity SDK round-trip.

## Build / deploy

- `dotnet build` — builds and post-build-deploys `LedgeLogging.dll` + `manifest.json`
  to `%USERPROFILE%\Documents\Timberborn\Mods\LedgeLogging\`. Always Release.
- `dotnet build -p:LedgeLoggingDeploy=false` — build without deploying.
- `dotnet test` — runs the unit tests for the pure reachability core. These build
  without a Timberborn install (the test project links the BCL-only source rather
  than referencing the game-coupled mod assembly).

Per-machine paths are not committed. Set `TimberbornInstallDir` via (highest
precedence first): `-p:TimberbornInstallDir=<path>`, the
`LEDGELOGGING_TIMBERBORN_DIR` environment variable, or a gitignored
`Directory.Build.local.props` (copy `Directory.Build.local.props.example`).
Optional deploy redirect: `LEDGELOGGING_DEPLOY_DIR`.

## Layout

```
LedgeLogging.slnx
├── src/LedgeLogging                       netstandard2.1 — the mod assembly (AssemblyName=LedgeLogging)
│   └── Reachability                       BCL-only pure core (the unit-tested standing-tile search)
└── tests/LedgeLogging.Reachability.Tests  MSTest — links and exercises the pure core
```
