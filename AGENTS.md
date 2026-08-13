# Crime Empire — Repository Guide

Read the canonical project documents before making non-trivial design or simulation changes:

1. `docs/PROJECT_CONTEXT.md`
2. `docs/GAME_VISION.md`
3. `docs/SIMULATION_ARCHITECTURE.md`
4. `docs/INFORMATION_AND_LEGIBILITY.md`
5. `docs/DESIGN_DECISIONS.md`
6. `docs/OPEN_CONCERNS.md`

`CLAUDE.md` contains additional collaboration guidance for Claude Code. This file applies to every coding agent.

## Repository boundaries

- `docs/` is the canonical design record.
- `src/CrimeEmpire.Simulation/` is the deterministic, engine-independent simulation library.
- `src/CrimeEmpire.Runner/` is the command-line behavioral-spike host and trace presentation.
- `tests/CrimeEmpire.Simulation.Tests/` verifies simulation invariants and deterministic behavior.

Keep Godot, UI, console formatting, and file-system concerns out of the simulation library. The scenario fixtures currently live in the simulation project so tests can exercise complete worlds without referencing the command-line host; move them to a dedicated fixtures project only when that separation earns its cost.

## Simulation constraints

- Preserve causal parity without assuming equal computational depth for every actor.
- Traits modify perception, salience, and evaluation; they do not directly trigger actions.
- Do not introduce unrestricted planning or continuous full deliberation for all characters.
- Keep truth, knowledge, belief, rumor, and evidence distinct.
- Prefer the smallest implementation that tests the intended behavior.
- A design decision that conflicts with canonical docs must be surfaced, not silently encoded.

## Verification

From the repository root:

```powershell
dotnet build CrimeEmpire.sln
dotnet test CrimeEmpire.sln
dotnet run --project src/CrimeEmpire.Runner -- --verify --seed 42 --days 90
```
