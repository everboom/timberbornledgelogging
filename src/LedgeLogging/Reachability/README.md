# src/LedgeLogging/Reachability

The mod's **pure, game-independent core**: given a resource a worker can't reach by the
direct path, pick a tile the worker *can* reach and stand on to clear it.

These types reference only the BCL — no Timberborn, no Unity. That is deliberate: this is
the part worth pinning with unit tests, kept free of the game so the test project
(`tests/LedgeLogging.Reachability.Tests`) can link and exercise it without a Timberborn
install. All game coupling — the actual navmesh/road reachability test and `Vector3Int`
conversion — is injected through the `TileReachabilityProbe` delegate by the game adapter
(`../Game/LedgeReachability`).

## Key types

- `TileCoord` — BCL-only integer tile coordinate (X/Y column, Z level). Mirrors
  Timberborn's `Vector3Int` grid layout; converted to/from it at the patch site.
- `TileReachabilityProbe` — delegate the caller supplies: "is this tile reachable, and at
  what walking distance?" The single seam to the game.
- `NeighbourColumnSearch` — `TryFindStandingTile(resourceColumn, maxDepthBelow, probe, …)`:
  searches the four orthogonally-adjacent columns one level below the resource (fixed up
  reach), at its level, and up to `maxDepthBelow` levels above it, returning the reachable
  candidate closest to the worker.

## Design notes

- **Orthogonal only.** Matches the vanilla terrain navmesh, which connects orthogonal
  neighbours only. No diagonal stances.
- **Up fixed at one, down configurable.** A worker can clear a resource up to **one level
  above** it (fixed; standing-tile `dz = -1`) and up to **`maxDepthBelow` levels below** it
  (standing-tile `dz = +1 … +maxDepthBelow`), plus same-level. `maxDepthBelow` is the player
  setting (`{1,2,3,Any}`; "Any" → map height; default 1, which reproduces the original ±1).
  The upward reach never widens beyond one.
- **Fallback-only cost.** The caller runs this only when the direct path fails; the
  expensive probe is gated behind a cheap navmesh check at the call site, so cost stays
  bounded (4 columns × `maxDepthBelow + 1` candidates, most cheap misses).
- **Deterministic.** Fixed enumeration order plus a strict-less-than comparison means ties
  resolve the same way every time (shallower stance preferred).

The standing-tile result feeds the demolish patches (see repo-root `CLAUDE.md`): the
`ReachableDemolishable` reachability gate (accept the resource + stash the tile) and the
`UncuttableReacher` approach patch (send the worker to the tile).
