# src/LedgeLogging/Game

The **game-coupled glue** between the pure search (`../Reachability`) and the Harmony
patches (`../Patches`). Everything here references Timberborn types and is verified
in-game, not unit-tested.

## Key types

- `LedgeReachability` — the adapter. Builds `TileReachabilityProbe`s and feeds them to
  `NeighbourColumnSearch.TryFindStandingTile`, converting between `Vector3Int` grid
  coordinates and the search's `TileCoord`. Two probes for the demolish system:
  - **worker-specific** (finder gate / assignment): `INavMeshService.IsOnNavMesh` then
    `Accessible.FindRoadToTerrainPath` — demolishers reach via roads then terrain.
  - **worker-independent** (UI status): `IsOnNavMesh` then
    `IDistrictService.IsOnInstantDistrictRoadSpill`.
- `LedgeApproach` — the chosen standing tile's world centre for one assignment.
- `LedgeApproachStore` — a `ConditionalWeakTable<ReservableReacher, LedgeApproach>` that
  carries the approach from the assignment patch to the reacher patch. Keyed per resource
  (one reacher per resource; one worker reserves it at a time).
- `NavMeshServiceLocator` — an `ILoadableSingleton` that publishes Game-scope DI services
  (`INavMeshService`, `IDistrictService`) to static fields, since the patch methods are
  static and cannot be injected. Bound in `LedgeLoggingConfigurator`; reads throw if
  accessed before the Game scope has loaded (no silent null use).

## Why the navmesh gate matters

Each probe checks `IsOnNavMesh(exact coordinate)` before testing road/terrain reachability,
so reachability can't "succeed" by snapping a non-existent level onto the column's real
surface — that would let a resource more than one level away be accepted, breaking the ±1
rule the pure search enforces.
