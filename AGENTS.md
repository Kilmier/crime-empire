# Crime Empire — Repository Guide

This file applies to every coding agent working in this repository (Claude Code, Codex, or
otherwise). `CLAUDE.md` contains additional guidance specific to Claude Code's own tooling (Plan
mode, etc.) and defers to this file for anything shared between agents — don't assume content
here is duplicated there.

Read the canonical project documents before making non-trivial design or simulation changes:

1. `docs/PROJECT_CONTEXT.md` — project history and working style (narrative background; for
   current work status see `docs/CURRENT_MILESTONE.md`, not this file)
2. `docs/GAME_VISION.md`
3. `docs/SIMULATION_ARCHITECTURE.md`
4. `docs/INFORMATION_AND_LEGIBILITY.md`
5. `docs/DESIGN_DECISIONS.md` — settled decisions, with citations to which doc/section settled
   them. Treat as authoritative; don't re-derive a decision from the docs above when it's already
   recorded here.
6. `docs/OPEN_CONCERNS.md` — known open risks, not yet resolved. If a change touches one of these,
   say so explicitly rather than silently picking an answer.
7. `docs/CURRENT_MILESTONE.md` — what's actively being worked on right now. Authoritative source
   for "what's the task," and the handoff surface between agents: current status, scope in and
   out, decisions taken at planning time, and what is deliberately carried over. There is no
   separate handoff document and there should not be one — a second copy of this would only drift
   out of sync with it. If it says nothing is active, confirm scope with Matt before starting
   anything rather than inferring the next milestone.
8. `docs/milestones/NNN-*.md` — completed milestones, append-only. Read the archive for the
   milestone a commit belongs to before reviewing that commit: corrections are appended there
   rather than folded into the original account, so the archive — not the commit message — is
   where "this was already found wrong, and here is what is still open" lives.

### Conditional reading

Not part of the universal list above — read these when the task calls for them:

- `docs/REVIEW_LEDGER.md` — **read before reviewing a commit.** The authority on what has been
  reviewed and what it concluded, plus verification baselines, the review checklist, and the
  recurring failure patterns. It carries no status and grants no permission.
- `docs/ROADMAP.md` — **read only when selecting or proposing future scope.** Technical debt,
  unbuilt work, and candidate milestones. Nothing on it authorizes anything; scope comes from Matt
  and goes into `docs/CURRENT_MILESTONE.md` first.

### When documents conflict

1. `DESIGN_DECISIONS.md` for settled decisions.
2. `GAME_VISION.md`, `SIMULATION_ARCHITECTURE.md`, `INFORMATION_AND_LEGIBILITY.md` for full design.
3. `OPEN_CONCERNS.md` for unresolved risks.
4. `CURRENT_MILESTONE.md` for currently assigned scope — and it is the only place active status is
   stated. Do not infer status from prose anywhere else.
5. `milestones/NNN-*.md` for completed work and appended corrections.
6. `REVIEW_LEDGER.md` for review coverage and verification baselines.

New durable decisions go to `DESIGN_DECISIONS.md`; new unresolved risks to `OPEN_CONCERNS.md`. Do
not originate authority in a summary — update the source first.

## Review workflow

This project uses a two-agent loop: Claude (implementation) and Codex (code review /
architectural-integrity review). The cycle is:

```
Claude implements
      ↓
Claude tests and commits
      ↓
Codex reviews that commit
      ↓
Owner (Matt) accepts/rejects findings
      ↓
Claude fixes accepted findings
      ↓
Codex verifies
      ↓
Next milestone
```

- Every commit should be reviewable in isolation — one coherent change, tests passing.
- Codex reviews against `docs/DESIGN_DECISIONS.md` and the canon docs above, not general
  best-practice opinion. A Codex finding that conflicts with a settled decision is a signal to
  surface to Matt, not to silently accept or silently dismiss.
- Claude should self-review against the constraints below before committing, so Codex's review
  catches real issues, not basics.

## Milestone lifecycle

`docs/CURRENT_MILESTONE.md` is mutable — update or replace it freely while work is underway. It
describes what's being attempted right now and is not meant to survive as history.

When the current milestone is complete, do the following, in order, and then stop:

1. Complete only the current milestone. Do not begin the next one.
2. Run the full relevant test suite.
3. Move the finished content of `docs/CURRENT_MILESTONE.md` into a new archive file at
   `docs/milestones/NNN-short-name.md` (zero-padded, incrementing), recording: what was
   attempted; what was completed; tests or success criteria and their results; important
   discoveries (architectural, not just "it worked"); deferred work; the relevant commit(s).
4. Commit the completed work — including the new milestone archive file — as one focused commit.
5. Do not begin the next milestone. Wait for review (Codex) and/or Matt before continuing.

Archived milestones under `docs/milestones/` are append-only records, never silently rewritten. If
something there turns out wrong or incomplete, add a correction or mark it superseded — don't edit
history to make it look like it was always accurate. Preserve final outcomes, not every
intermediate checklist.

This is distinct from `docs/DESIGN_DECISIONS.md` (durable, settled decisions, not tied to a
milestone) and `docs/OPEN_CONCERNS.md` (durable, unresolved risks).

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

For information-channel or relationship changes, also run:

```powershell
dotnet run --project src/CrimeEmpire.Runner -- --verify --variant disloyal-vincent --seed 42 --days 90
dotnet run --project src/CrimeEmpire.Runner -- --verify --variant resentful-tommy --seed 42 --days 90
dotnet run --project src/CrimeEmpire.Runner -- --compare --seed 42
dotnet run --project src/CrimeEmpire.Runner -- --variant disloyal-vincent --viewpoint salvatore --seed 42 --days 90
dotnet run --project src/CrimeEmpire.Runner -- --variant baseline --viewpoint vincent --seed 42 --days 90
```

`--compare` runs every variant in `Variants.All`, so it covers the full set as that list grows.

Recorded baselines to compare against are in `docs/REVIEW_LEDGER.md`.
