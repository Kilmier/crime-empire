# Milestone 002 — .NET 9 → .NET 10 Migration

Status: complete. Reviewed by Codex 2026-08-13 — no findings; build accepted as a safe base to
build on. The deferred items in "Deferred work" below were not closed by that review and remain
open (notably the unverified Godot/`net10.0` compatibility question).

## What was attempted

Execute the already-settled target-framework decision recorded in `docs/DESIGN_DECISIONS.md`
("Stack" → "Target framework: .NET 10 (LTS)"): move the repository off `net9.0` — a known,
documented temporary mismatch carried since the original scaffold — onto .NET 10, now that the
SDK is installed on the dev machine. Scoped deliberately to the framework move alone: no
dependency bumps, no language-feature adoption, no source changes.

## What was completed

- `Directory.Build.props`: `TargetFramework` `net9.0` → `net10.0`. This covers
  `CrimeEmpire.Simulation` and `CrimeEmpire.Runner`, whose `.csproj` files declare no TFM of
  their own.
- `tests/CrimeEmpire.Simulation.Tests/CrimeEmpire.Simulation.Tests.csproj`: `net9.0` → `net10.0`.
  This project redundantly re-declares the TFM rather than inheriting it (see Deferred work).
- New `global.json` at the repository root pinning the SDK:

  ```json
  { "sdk": { "version": "10.0.400", "rollForward": "latestFeature" } }
  ```

  `latestFeature` was chosen over `disable`/`patch` so a future 10.0.4xx SDK on the machine still
  builds the repo without a pin edit, while a stray .NET 9 SDK cannot silently pick up the build.
  Both 9.0.317 and 10.0.400 are installed side by side on the dev machine, so the pin is doing
  real work, not decoration.
- Test package versions (`Microsoft.NET.Test.Sdk` 17.12.0, `xunit` 2.9.2, `xunit.runner.visualstudio`
  2.8.2, `coverlet.collector` 6.0.2) were left untouched on purpose — they build and run clean
  against `net10.0`, and bundling a dependency bump would have broken the "isolated commit"
  requirement the decision entry asked for.
- No source file was modified. The migration is entirely build configuration.

## Tests / success criteria and results

All run from the repository root against SDK 10.0.400, after wiping `obj/`/`bin/` to clear stale
net9 restore assets:

- `dotnet build CrimeEmpire.sln` — **succeeded, 0 warnings, 0 errors.** Notably no obsolescence
  or analyzer warnings surfaced from the newer SDK's default analyzer set.
- `dotnet test CrimeEmpire.sln` — **6/6 passing**, including the three `SimulationReplayTests`
  determinism tests.
- `dotnet run --project src/CrimeEmpire.Runner -- --verify --seed 42 --days 90` — **DETERMINISTIC**,
  run A hash `20DDB25197093461` == run B hash.
- **Cross-runtime determinism check (added beyond the stated criteria).** The three checks above
  only prove self-consistency *within* one runtime; they would all pass even if .NET 10 had
  changed simulation outcomes wholesale. So the runner was additionally built and executed on
  `net9.0` and on `net10.0` from the same source, and the two outputs compared:
  - history hash `20DDB25197093461` on **both** runtimes — unchanged by the migration;
  - the full 376-line `--seed 42 --days 90` trace is **byte-identical** across the two runtimes
    (`diff` clean).

## Important discoveries

- **The migration touched two files, not the three the decision text predicted, and the reason
  matters.** `docs/DESIGN_DECISIONS.md` instructed "update all three `.csproj` files together."
  In fact only `CrimeEmpire.Simulation.Tests.csproj` declares its own `TargetFramework`; the other
  two inherit from `Directory.Build.props`. Adding redundant per-project TFM entries to satisfy the
  letter of the instruction would have made the repo *more* prone to exactly the version drift this
  milestone was cleaning up, so it was not done, and the decision entry has been amended to record
  `Directory.Build.props` as the single source of truth for the TFM.
- **Framework-migration determinism is a cross-runtime property, and the existing `--verify` flag
  cannot see it.** `--verify` compares two runs inside one process on one runtime. A runtime
  change that altered, say, floating-point behavior or hash/ordering semantics would leave
  `--verify` perfectly green while silently invalidating every previously recorded trace and any
  future save file. The A/B check above is what actually retires that risk here. If the repo ever
  moves runtimes again (or adopts a JIT/ILC option that could affect float behavior), the same
  cross-build hash comparison should be treated as the real acceptance test, not `--verify`.
  This is the same class of bug as milestone 001's process-global decision-ID defect: determinism
  that holds under the test you wrote but not under the property you meant.
- The `NETSDK1005` "assets file doesn't have a target for 'net10.0'" errors seen mid-migration were
  purely stale-restore artifacts from the pre-existing `obj/` directories, not a real
  incompatibility; wiping `obj/`/`bin/` and restoring cleared them.

## Deferred work

- **The redundant `TargetFramework` in `CrimeEmpire.Simulation.Tests.csproj` was left in place.**
  It is now correct (`net10.0`) but still duplicates `Directory.Build.props`, and it is the sole
  reason this migration was a two-file change instead of a one-file change. Deleting that line so
  the test project inherits like the other two is a small, obvious cleanup — held back only to keep
  this commit isolated to the framework move, per the decision entry's review requirement. Worth
  doing as its own trivial commit.
- Test-dependency version bumps (xunit, Test.Sdk, coverlet) — not attempted; no current reason to.
- No .NET 10 / C# 14 language or runtime features were adopted. `LangVersion` remains unset, so it
  now defaults to C# 14 by virtue of the TFM; nothing in the source depends on that.
- **Godot compatibility is unverified and is the one live risk this milestone leaves open.**
  `DESIGN_DECISIONS.md` commits to Godot 4 with C# for rendering, and Godot's .NET support has
  historically lagged the current .NET release. No Godot project exists yet, so nothing is broken
  today, but "the sim core targets net10.0" is not yet known to be compatible with whatever Godot
  version this project eventually adopts. If it is not, the resolution is most likely to
  multi-target or to keep the Godot-facing layer on a lower TFM — the simulation library is
  engine-independent precisely so that stays an option. Flagged here rather than silently assumed
  away.

## Relevant commits

- `65a97c4` — "Add milestone lifecycle policy and archive milestone 001". Immediate predecessor,
  not part of this milestone's work: pre-existing uncommitted docs/policy changes that were
  sitting in the working tree when this milestone started, committed separately first so that the
  migration commit stayed isolated per the decision entry's review requirement.
- The migration itself is the single commit that introduced this file — it contains
  `Directory.Build.props`, `tests/CrimeEmpire.Simulation.Tests/CrimeEmpire.Simulation.Tests.csproj`,
  the new `global.json`, this archive, and the `DESIGN_DECISIONS.md` / `CURRENT_MILESTONE.md`
  status updates, and nothing else. (Not cited by hash here for the obvious reason that a commit
  cannot contain its own hash; `git log --diff-filter=A -- docs/milestones/002-dotnet-10-migration.md`
  resolves it.)
