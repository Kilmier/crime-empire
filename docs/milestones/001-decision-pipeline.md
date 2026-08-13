# Milestone 001 — Decision Pipeline Behavioral Spike

Status: complete. Archived retroactively — this milestone was implemented and reorganized before
the `docs/milestones/` archive policy existed, so this entry was reconstructed from git history
(commits `46f0777`, `4030699`) rather than written live from a `CURRENT_MILESTONE.md`.

## What was attempted

The task specified in `docs/PROJECT_CONTEXT.md`'s "Immediate next step": build the smallest
possible executable proof of the character decision pipeline from `docs/SIMULATION_ARCHITECTURE.md`
— trigger → update beliefs → select agenda → generate bounded candidates → reject unavailable →
score via local utility → commit → schedule reconsideration — with a small hardcoded cast, no
engine, no rendering, no save/load. Console decision traces only.

## What was completed

- A headless C# console project implementing the full 8-stage pipeline (`Decision/Pipeline.cs`),
  with belief/salience perception, organizational office and assignment lookup, agenda selection,
  context-scoped candidate generation, availability filtering, utility scoring, commitment, and
  reconsideration scheduling as separate stages — matching the architecture doc's stated goal of
  being able to answer "what woke him / what mattered / what occurred to him / what was open to
  him / what he preferred" independently.
- The **harbour scenario**: one organization (the Greco family), one contested district (the
  harbour), five characters (`src/CrimeEmpire.Simulation/Scenario/Cast.cs`) — deliberately kept
  small so an odd trace could be attributed to the pipeline rather than cast size. Vincent
  (aggressive, proud harbour capo with a revenue problem and a grievance) is given the inputs for
  an authority violation but nothing scripts it — whether he breaks Salvatore's rule is a scoring
  outcome the variants test is genuinely contingent on those inputs, not authored.
- Character variants (e.g. `cautious-vincent`) to test that personality actually changes outcomes
  rather than personality being cosmetic.
- A deterministic event queue and world model with a seeded RNG.
- An xUnit test project (`tests/CrimeEmpire.Simulation.Tests`) with 6 tests total:
  - `EventQueueTests` (3): ordering by time then insertion, cancelled-event skipping with reason
    retained, `Next` not advancing past requested calendar time.
  - `SimulationReplayTests` (3): identical seed/variant/day-count inputs produce identical
    histories; pausing and resuming produces the same history as running straight through;
    a meaningfully different character variant (`cautious-vincent` vs. `baseline`) produces a
    *different* history — i.e. the test suite checks both determinism and that determinism isn't
    hiding a system that ignores its inputs.
- Repository reorganized from a flat `sim/CrimeSim/` prototype layout into
  `src/CrimeEmpire.Simulation` (engine-independent library) / `src/CrimeEmpire.Runner` (console
  entry point, scenarios, trace rendering) / `tests/CrimeEmpire.Simulation.Tests`, with
  `docs/` holding the canonical documents and `CrimeEmpire.sln` tying the three projects together.

## Tests / success criteria and results

- Success criterion per `PROJECT_CONTEXT.md`: does the character behavior look motivated and
  legible from the console trace, or does it look arbitrary? Reported as producing legible,
  motivated traces in the worked scenario (not independently re-verified in this session — see
  Known limitations below).
- All 6 xUnit tests reported passing as of commit `4030699`, including the determinism-under-
  variant-change test described above.

## Important discoveries

- **Architectural bug caught by the reorg/review pass, not by original implementation review**:
  decision IDs were originally generated from `private static long _nextDecisionId` — process-global
  state (`sim/CrimeSim/Decision/Pipeline.cs` at commit `46f0777`). Two runs with identical seed and
  inputs produced *behaviorally* identical histories but *different* decision IDs, because ID
  assignment depended on how many decisions had been made process-wide, not on the deterministic
  world/actor state. This is a direct violation of the architecture doc's own invariant: "seeded
  runs are reproducible under the same inputs and build." Fixed in the same pass by moving decision
  ID assignment to per-actor state (`actor.DecisionCount++`, seen in commit `4030699`'s
  `Pipeline.cs`), keyed off `world.Seed` and `actor.Id`. This is the kind of bug the
  Claude-implements/Codex-reviews loop exists to catch, and it did.
- The fix and the `docs/src/tests` reorganization landed in the same commit (`4030699`), not as
  the two separate commits ("baseline snapshot" then "structural migration") originally described
  during planning. Not a problem in itself, but worth naming since the stated intent at the time
  was to keep them separable for review.

## Deferred work

- SQLite persistence, save/load — not started (correctly out of scope for this milestone).
- Godot/rendering — not started (correctly out of scope).
- Simulation relevance tiering (Active/Supporting/Background) — not implemented; 5-character cast
  makes this a non-issue at this scale, but it is unvalidated.
- `.NET 9 → .NET 10` migration — decided but not executed; see `DESIGN_DECISIONS.md`. As of this
  archive, `TargetFramework` is still `net9.0` in all three `.csproj` files.
- `docs/milestones/` policy itself postdates this milestone, hence the retroactive reconstruction
  noted at the top of this file.

## Relevant commits

- `46f0777` — Initial Crime Empire simulation baseline (original `sim/CrimeSim/` implementation,
  process-global decision ID bug present).
- `4030699` — Reorganize simulation into docs/src/tests; decision ID determinism fix included in
  the same commit.
