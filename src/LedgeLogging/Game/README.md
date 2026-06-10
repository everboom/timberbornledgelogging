# src/LedgeLogging/Game

The **game-coupled glue** between the pure search (`../Reachability`) and the Harmony
patches (`../Patches`). Everything here references Timberborn types and is verified
in-game, not unit-tested.

## Key types

- `LedgeReachability` — the adapter. Builds `TileReachabilityProbe`s and feeds them to
  `NeighbourColumnSearch.TryFindStandingTile`, converting between `Vector3Int` grid
  coordinates and the search's `TileCoord`. Two probes for the demolish system:
  - **worker-specific** (finder gate / assignment): `INavMeshService.IsOnNavMesh`, then
    `Accessible.FindRoadToTerrainPath` — demolishers reach via roads then terrain.
  - **worker-independent** (UI status): `IsOnNavMesh`, then
    `IDistrictService.IsOnInstantDistrictRoadSpill`.

  A standing tile inside a pass-through building (a "restricted" navmesh node) is **accepted**:
  the mod will route a worker to stand there and clear the resource. (An earlier build rejected
  those via `RestrictedNodeMap`; that gate was removed by request.) See `../Patches/README.md`.
- `LedgeApproach` — the chosen standing tile's world centre for one assignment.
- `LedgeApproachStore` — a `ConditionalWeakTable<ReservableReacher, LedgeApproach>` that
  carries the approach from the reachability gate patch to the reacher patch (the stash
  happens in the gate, not at job assignment — see the type's doc). Keyed per resource
  (one reacher per resource; one worker reserves it at a time).
- `NavMeshServiceLocator` — an `ILoadableSingleton` that publishes Game-scope DI services
  (`INavMeshService`, `IDistrictService`, `ITerrainService`, and the mod settings) to static
  fields, since the patch methods are static and cannot be injected. Bound in
  `LedgeLoggingConfigurator`; reads throw if accessed before the Game scope has loaded (no
  silent null use).

## Why the navmesh gate matters

Each probe checks `IsOnNavMesh(exact coordinate)` before testing road/terrain reachability,
so reachability can't "succeed" by snapping a non-existent level onto the column's real
surface — that would let a resource more than one level away be accepted, breaking the ±1
rule the pure search enforces.
