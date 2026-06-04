# src/LedgeLogging/Patches

The Harmony patches that let a worker **clear/remove** (demolish) a natural resource on
a ±1-level ledge. Applied by `LedgeLoggingModStarter.StartMod` via `PatchAll`. **Keep
`LedgeLoggingModStarter.ExpectedPatchedMethodCount` equal to the number of distinct
methods patched here** (currently 3) — on a mismatch the bootstrap rolls the patches back
and disables the mod, which is how a game update renaming a target surfaces.

## Fail-safe contract

Every postfix below follows the same shape, so a broken patch degrades to vanilla rather
than crashing the game:

1. **`if (!LedgeLoggingState.IsActive) return;`** first — the mod is inactive until the
   starter verifies the install, and any failure flips it back off, so the body is a no-op
   then.
2. **`try { … } catch (Exception ex) { LedgeLoggingState.Disable(…, ex); }`** around the
   work — an unexpected throw is logged once and turns the whole mod off for the session
   (patch failures are deterministic; there is nothing to retry). Because these are
   *postfixes*, swallowing the exception just lets the game's original result stand.

The starter also calls `UncuttableReacherAccess.EnsureResolved()` at startup to force the
reflection lookups (the `internal` type + private field) so a renamed game member fails at
load — loud and once — instead of on the first in-game demolish.

## The three patches

| File | Target | Kind | Job |
|------|--------|------|-----|
| `DemolishReachabilityPatch` (gate) | `ReachableDemolishable.IsReachable(Accessible, out float)` | Postfix | Accept a natural resource the direct road→terrain path rejected, if a ledge standing tile exists — **and stash that standing tile** for the reacher. |
| `DemolishReachabilityPatch` (status) | `ReachableDemolishable.IsUnreachable()` | Postfix | Clear the "UnreachableObject" selection status when a ledge neighbour tile is on a district road spill. |
| `UncuttableReacherDestinationPatch` | `UncuttableReacher.Destination` (getter) | Postfix | Route the worker to the stashed standing tile instead of the resource centre. |

The stash happens **in the gate**, not at job assignment: `DemolishJobProvider.GetJob`
reserves a job and immediately drives navigation (reading the reacher's destination) within
its own call, so a separate `GetJob` postfix would run too late. The gate runs (with the
worker's `start`) just before that, which is where the standing tile is computed and stored.

`UncuttableReacher` is decorated onto natural resources only, so the reacher patch is
inherently resource-scoped (buildings/ruins use `AccessibleDemolishableReacher`, untouched).
The two `ReachableDemolishable` patches are shared with buildings, so they gate on the
`NaturalResource` component (`DemolishPatchSupport`). ±1-level and orthogonal-only come
from `NeighbourColumnSearch` (the tested core).

## Targeting internal types

`UncuttableReacher` is `internal`, so its patch resolves the target by name with
`AccessTools.TypeByName` + a `TargetMethod()` and reads the private
`_positionDestinationFactory` via `AccessTools`; the lookups throw on failure, so a renamed
target fails loudly at `PatchAll` time. `ReachableDemolishable` is public, so the
gate/status patches target it directly by `typeof` (the overloaded `IsReachable` is
disambiguated by argument types).

## Known limitation: building clip

The reacher must hand the walker a **built-in** `IDestination` (`PositionDestination`),
because the game serializes a walker's current destination on save and
`DestinationValueSerializer` only accepts `PositionDestination`/`AccessibleDestination` — a
custom destination throws during save. Both built-in destinations pathfind via
`FindPathUncached`, which for a bare-terrain standing tile can fall back to the terrain
pathfinder and cut through an impassable building. This is the **same** pathfinding vanilla
uses to send a worker to an off-road natural resource, so it's an accepted base-game
limitation, not fixable from a mod without breaking saves. See repo-root `CLAUDE.md`.

## How they cooperate

Patches share state through `../Game/LedgeApproachStore`: the gate patch writes the standing
tile; the reacher patch reads it. The reachability search lives in `../Game`
(`LedgeReachability`, road→terrain + road-spill probes) and `../Reachability`
(`NeighbourColumnSearch`, the tested core).
