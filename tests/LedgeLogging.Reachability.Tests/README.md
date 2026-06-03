# tests/LedgeLogging.Reachability.Tests

MSTest unit tests for the mod's pure reachability core
(`src/LedgeLogging/Reachability`).

## How it fits together

The project does **not** reference `LedgeLogging.csproj`. The mod project
references the Timberborn install DLLs, which need not be present on a machine
that only runs the tests. Instead this project **links the pure source files**
(`TileCoord.cs`, `NeighbourColumnSearch.cs`) directly via `<Compile Include>` —
they use the BCL only, so they compile straight into the test assembly. That is
exactly why the logic was factored out of the game-coupled code.

The game's navmesh reachability test is faked with an in-memory
`TileReachabilityProbe` that records which tiles it was asked about, letting the
tests assert both the *result* and the *search shape* (orthogonal-only, ±1 level,
exactly 12 candidates, deterministic tie-breaking).

## Run

```
dotnet test
```

Coverage (built into MSTest 4.x):

```
dotnet test --collect:"Code Coverage;Format=cobertura" --results-directory TestResults
```

Only the pure search is unit-tested here; the Harmony glue is verified in-game.
