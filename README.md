# Crime Empire

A headless C# behavioral prototype for a persistent-world, character-driven criminal empire simulation.

The project is currently testing its most expensive design premise: characters operating under shared causal rules while simulation depth is rationed by narrative relevance. There is no Godot client yet.

## Layout

- `docs/` — vision, architecture, decisions, information model, and open concerns
- `src/CrimeEmpire.Simulation/` — deterministic simulation logic
- `src/CrimeEmpire.Runner/` — console scenario runner and trace output
- `tests/CrimeEmpire.Simulation.Tests/` — automated simulation tests

## Run the behavioral spike

```powershell
dotnet run --project src/CrimeEmpire.Runner -- --verify --seed 42 --days 90
```

Use `--help` to see scenario and comparison options.

## Build and test

```powershell
dotnet build CrimeEmpire.sln
dotnet test CrimeEmpire.sln
```
