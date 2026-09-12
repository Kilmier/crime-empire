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
- `docs/proposals/README.md` — **read only when evaluating future design ideas or checking whether an
  idea has been preserved.** It is a noncanonical intake index that routes to focused proposal files.
  Do not read every proposal by default, treat a proposal as a roadmap commitment, or implement one
  without a human ruling and scope in `docs/CURRENT_MILESTONE.md`.

### When documents conflict

1. `DESIGN_DECISIONS.md` for settled decisions.
2. `GAME_VISION.md`, `SIMULATION_ARCHITECTURE.md`, `INFORMATION_AND_LEGIBILITY.md` for full design.
3. `OPEN_CONCERNS.md` for unresolved risks.
4. `CURRENT_MILESTONE.md` for currently assigned scope — and it is the only place active status is
   stated. Do not infer status from prose anywhere else.
5. `milestones/NNN-*.md` for completed work and appended corrections.
6. `REVIEW_LEDGER.md` for review coverage and verification baselines.
7. `proposals/` carries no authority. When a proposal conflicts with anything above, the proposal
   loses until Matt explicitly rules and the authoritative source is updated.

New durable decisions go to `DESIGN_DECISIONS.md`; new unresolved risks to `OPEN_CONCERNS.md`. Do
not originate authority in a summary — update the source first.

## Review workflow

This project uses an implementer/reviewer loop. Claude and Codex may exchange those roles, but the
reviewer must be independent of the commit being reviewed. Agent or model names are recorded as
reviewer identity, not treated as proof of independence.

```
Implementer completes one authorized change and tests it
      ↓
Implementer commits one coherent diff
      ↓
Independent reviewer assigns a review class from that exact diff and reviews it
      ↓
Matt accepts or rejects the findings
      ↓
Implementer fixes accepted findings in one focused correction
      ↓
Independent reviewer verifies the correction
      ↓
Matt accepts the state; only then may the next milestone begin
```

Review remains chronological: take the oldest commit whose active-range outcome is not established.
Do not skip a commit because it is documentation-only, and do not let a later commit stand in for an
earlier one. `docs/REVIEW_LEDGER.md` defines the active range and records the exact queue.

### Proportional review classes

The reviewer assigns the class after inspecting the exact diff. A commit mixing classes receives the
highest applicable class. If the class is genuinely ambiguous, use **Class B** and state the doubt.

- **Class A — implementation or simulation-risk change.** Runtime code, persistence, fixtures,
  gameplay/UI behaviour, tests that alter claimed assurance, build configuration, or a change whose
  correctness depends on simulation behaviour. Review the authorized milestone and canon, trace the
  production path, run the full relevant verification, inspect information boundaries and persistent
  state, and mutation-check load-bearing tests where practical.
- **Class B — authority, status, or process change.** Canon, design decisions, open concerns,
  `CURRENT_MILESTONE.md`, `ROADMAP.md`, this file, `REVIEW_LEDGER.md`, or any document that can alter
  what another agent believes is authorized, accepted, unresolved, or required. Review the exact diff
  against its cited source and repository history. Run targeted commands needed to substantiate its
  claims; do not run the entire simulation suite merely because prose changed.
- **Class C — focused non-authoritative change.** Proposal intake, comments, formatting, archive-only
  additions, or tooling prose that changes neither behaviour nor authority. Check scope, links,
  append-only history, and false claims. Escalate to A or B if the diff actually crosses either
  boundary.

All three classes require an exact-commit review. Classes scale depth; they do not waive review.

### Outcomes and findings

- A review records: exact commit, class, reviewer identity, independence from the author, commands or
  evidence actually checked, findings, limits, and verdict.
- **PASS** means the reviewer found no blocking defect in the reviewed diff. **FAIL** means at least
  one accepted correction is required. Neither means Matt accepted the commit until his ruling is
  recorded separately.
- P1 and P2 findings enter the correction loop when Matt accepts them. A **NOTE** is non-blocking and
  does not create a correction cycle. Repeated NOTE-level concerns are surfaced to Matt for a design
  or process ruling; repetition does not silently promote them into defects.
- Test-green is evidence, not review. Self-review is useful preflight evidence, never independent
  acceptance. If the reviewer authored any reviewed commit, another reviewer must inspect it before
  acceptance unless Matt explicitly records a bounded exception.
- A finding that conflicts with a settled design decision is surfaced to Matt rather than silently
  accepted or dismissed. Reviews judge the project against its canon and authorized milestone, not
  generic best-practice preference.

### Where review information lives

- `CURRENT_MILESTONE.md` holds only active authorization, the present gate, and deliberately carried
  work. It does not retell completed correction chains.
- `REVIEW_LEDGER.md` holds the compact coverage table, current verification baseline, checklist, and
  recurring cross-milestone lessons. It does not duplicate full review reports.
- `docs/milestones/NNN-*.md` holds the detailed, append-only implementation and correction history.
  A ledger row links there instead of copying the narrative.
- Do not create a second handoff, review-process, or consolidated-history document. Update the source
  with the responsibility above.

Every commit should be reviewable in isolation: one coherent change, with its relevant tests passing.
The implementer should self-review before committing so the independent round can spend its attention
on feature fidelity, architecture, information boundaries, persistence, and false assurance.

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

- `docs/` is the canonical design record, except `docs/proposals/`, which is explicitly noncanonical
  intake material and cannot authorize implementation.
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
