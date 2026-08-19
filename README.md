# Crime Empire

A C# behavioral prototype for a persistent-world, character-driven criminal empire simulation.

The project is testing its most expensive design premise: characters operating under shared causal
rules while simulation depth is rationed by narrative relevance. The simulation core is headless and
engine-independent; a minimal Godot playable shell sits over the same kernel to confirm the simulation
is actually playable, not to present it — no art pipeline, map, or save/load yet.

Read `AGENTS.md` first — it holds the canonical doc read order, the review workflow, and the milestone
lifecycle every change here follows. `docs/CURRENT_MILESTONE.md` says what, if anything, is active.

## Layout

- `docs/` — vision, architecture, decisions, information model, open concerns, and the append-only
  milestone archive
- `src/CrimeEmpire.Simulation/` — deterministic, engine-independent simulation logic
- `src/CrimeEmpire.Runner/` — console scenario runner, trace output, and viewpoint rendering
- `src/CrimeEmpire.Godot/` — the playable shell over the same simulation kernel
- `tests/CrimeEmpire.Simulation.Tests/` — automated simulation tests

## Run the behavioral spike

```powershell
dotnet run --project src/CrimeEmpire.Runner -- --verify --seed 42 --days 90
```

Use `--help` to see scenario, comparison, and viewpoint options.

## Build and test

```powershell
dotnet build CrimeEmpire.sln
dotnet test CrimeEmpire.sln
```

Full verification commands, including variant and viewpoint checks, are in `AGENTS.md` §Verification.

## Run the Godot shell

```powershell
dotnet build src/CrimeEmpire.Godot/CrimeEmpire.Godot.csproj
```

Open `src/CrimeEmpire.Godot/` in the Godot 4.7 (Mono) editor to play it, or run the headless self-test
from `docs/REVIEW_LEDGER.md`'s milestone 009 baseline section.
