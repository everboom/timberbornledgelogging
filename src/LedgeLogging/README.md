# src/LedgeLogging

The mod's only project. Compiles to `LedgeLogging.dll` (a Timberborn 1.0 mod
assembly) and deploys, with `manifest.json`, to the local Mods folder.

## Purpose

Relax the lumberjack's reach so a beaver can cut a tree on an orthogonally- (and
optionally diagonally-) adjacent column whose surface is one terrain level above
or below the beaver's standing tile — a case vanilla forbids because the two
tiles aren't connected on the terrain navmesh.

## Key types

- `LedgeLoggingConfigurator` — `[Context("Game")]` entry point. Discovered by
  the mod loader; where the Harmony bootstrap and service bindings get wired.

## How it fits together

This is a Harmony mod: there is no clean Bindito/blueprint extension seam for the
target behaviour (the load-bearing logic lives in `private`/`internal` members of
vanilla `Timberborn.Forestry` / `Timberborn.YielderFinding`). See the repo-root
`CLAUDE.md` § "Technical background" for the exact patch points and rationale.
