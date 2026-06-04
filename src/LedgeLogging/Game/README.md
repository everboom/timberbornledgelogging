# src/LedgeLogging/Game

The **game-coupled glue** between the pure search (`../Reachability`) and the Harmony
patches (`../Patches`). Everything here references Timberborn types and is verified
in-game, not unit-tested.

## Key types

- `LedgeReachability` — the adapter. Builds `TileReachabilityProbe`s and feeds them to
  `NeighbourColumnSearch.TryFindStandingTile`, converting between `Vector3Int` grid
  coordinates and the search's `TileCoord`. Two probes for the demolish system:
  - **worker-specific** (finder gate / assignment): `INavMeshService.IsOnNavMesh`, reject
    restricted nodes (`NavMeshServiceLocator.IsRestricted`), then
    `Accessible.FindRoadToTerrainPath` — demolishers reach via roads then terrain.
  - **worker-independent** (UI status): `IsOnNavMesh`, reject restricted nodes, then
    `IDistrictService.IsOnInstantDistrictRoadSpill`.

  Both probes reject **restricted navmesh nodes** — tiles a worker may path *through* but not
  stand on as a destination (e.g. inside a building). That keeps the mod from sending a worker
  to clear a resource while standing inside a building (and from clearing the "unreachable"
  status for one), matching vanilla's own rule. See `../Patches/README.md`.
- `LedgeApproach` — the chosen standing tile's world centre for one assignment.
- `LedgeApproachStore` — a `ConditionalWeakTable<ReservableReacher, LedgeApproach>` that
  carries the approach from the reachability gate patch to the reacher patch (the stash
  happens in the gate, not at job assignment — see the type's doc). Keyed per resource
  (one reacher per resource; one worker reserves it at a time).
- `NavMeshServiceLocator` — an `ILoadableSingleton` that publishes Game-scope DI services
  (`INavMeshService`, `IDistrictService`, and `RestrictedNodeMap`/`NodeIdService` behind the
  `IsRestricted(Vector3Int)` helper) to static fields, since the patch methods are static and
  cannot be injected. `RestrictedNodeMap`/`NodeIdService` are `internal`, reached by
  publicizing `Timberborn.Navigation` at build time (see repo-root `CLAUDE.md`). Bound in
  `LedgeLoggingConfigurator`; reads throw if accessed before the Game scope has loaded (no
  silent null use).

## Why the navmesh gate matters

Each probe checks `IsOnNavMesh(exact coordinate)` before testing road/terrain reachability,
so reachability can't "succeed" by snapping a non-existent level onto the column's real
surface — that would let a resource more than one level away be accepted, breaking the ±1
rule the pure search enforces.
