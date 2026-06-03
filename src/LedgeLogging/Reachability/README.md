# src/LedgeLogging/Reachability

The mod's **pure, game-independent core**: the logic that, given a tree the
lumberjack can't reach by the direct trunk path, picks a tile the beaver *can*
reach and stand on to cut it.

These types reference only the BCL — no Timberborn, no Unity. That is deliberate:
this is the part of the mod worth pinning with unit tests, so it is kept free of
the game so the test project (`tests/LedgeLogging.Reachability.Tests`) can link
and exercise it without a Timberborn install. All game coupling — the actual
navmesh reachability test and `Vector3Int` conversion — is injected through the
`TileReachabilityProbe` delegate by the Harmony glue.

## Key types

- `TileCoord` — BCL-only integer tile coordinate (X/Y column, Z level). Mirrors
  Timberborn's `Vector3Int` grid layout; converted to/from it at the patch site.
- `TileReachabilityProbe` — delegate the caller supplies: "is this tile reachable
  from the flag, and at what walking distance?" The single seam to the game.
- `NeighbourColumnSearch` — `TryFindStandingTile(...)`: searches the four
  orthogonally-adjacent columns at the tree's level ±1 and returns the reachable
  candidate closest to the flag.

## Design notes

- **Orthogonal only.** Matches the vanilla terrain navmesh, which connects
  orthogonal neighbours only. No diagonal stances.
- **±1 level only.** The mod's whole premise; `NeighbourColumnSearch` must never
  silently widen this span.
- **Fallback-only cost.** The caller runs this only when the direct path fails, so
  the search costs at most 12 reachability probes (4 columns × 3 levels) per tree.
- **Deterministic.** Fixed enumeration order plus a strict-less-than comparison
  means ties resolve the same way every time (tree's own level preferred).

The standing-tile result feeds two patches (see repo-root `CLAUDE.md`): the
`YielderFinder` reachability patch (accept the tree) and the `TreeReacher`
approach patch (send the beaver to the tile, facing the tree).
