# src/LedgeLogging/Patches

The Harmony patches that let a worker **clear/remove** (demolish) a natural resource on
a ±1-level ledge. Applied by `LedgeLoggingModStarter.StartMod` via `PatchAll`. **Keep
`LedgeLoggingModStarter.ExpectedPatchedMethodCount` equal to the number of distinct
methods patched here** (currently 4) — the bootstrap warns loudly on a mismatch, which is
how a game update renaming a target surfaces.

## The four patches

| File | Target | Kind | Job |
|------|--------|------|-----|
| `DemolishReachabilityPatch` (gate) | `ReachableDemolishable.IsReachable(Accessible, out float)` | Postfix | Accept a natural resource the direct road→terrain path rejected, if a ledge standing tile exists. |
| `DemolishReachabilityPatch` (status) | `ReachableDemolishable.IsUnreachable()` | Postfix | Clear the "UnreachableObject" selection status when a ledge neighbour tile is on a district road spill. |
| `DemolishAssignmentPatch` | `DemolishJobProvider.GetJob` | Prefix + Postfix | On a new reservation, compute & stash the (worker, resource) standing tile — or clear a stale one for a normal removal. |
| `UncuttableReacherDestinationPatch` | `UncuttableReacher.Destination` (getter) | Postfix | Route the worker to the standing tile instead of the resource centre. |

`UncuttableReacher` is decorated onto natural resources only, so patch 4 is inherently
resource-scoped (buildings/ruins use `AccessibleDemolishableReacher`, untouched). The two
`ReachableDemolishable` patches are shared with buildings, so they gate on the
`NaturalResource` component (`DemolishPatchSupport`). ±1-level and orthogonal-only come
from `NeighbourColumnSearch` (the tested core).

## Targeting internal types

`DemolishJobProvider` and `UncuttableReacher` are `internal`, so those patches resolve
their targets by name with `AccessTools.TypeByName` + a `TargetMethod()` and read private
fields (`_positionDestinationFactory`) via `AccessTools`; the lookups throw on failure, so
a renamed target fails loudly at `PatchAll` time. `ReachableDemolishable` is public, so the
gate/status patches target it directly by `typeof` (the overloaded `IsReachable` is
disambiguated by argument types).

## How they cooperate

Patches share state through `../Game/LedgeApproachStore`: the assignment patch writes the
standing tile; the reacher patch reads it. The reachability search lives in `../Game`
(`LedgeReachability`, road→terrain + road-spill probes) and `../Reachability`
(`NeighbourColumnSearch`, the tested core).
