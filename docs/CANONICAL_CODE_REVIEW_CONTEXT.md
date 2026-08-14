# Crime Empire — Canonical Code and Review Context

Status snapshot: 2026-08-13 at repository commit `d142582` on `main`. Milestone 003 is complete and
Codex-verified against implementation commit `b8fe921`. Milestone 004 (Provenance Precision) is
active and approved; no implementation commits exist for it yet.

## Purpose and authority

This document is a compact context package for a code/review-focused workspace. It records the
repository shape, implementation milestones, commit history, verification baseline, known technical
debt, and review lessons.

It does not replace:

- `AGENTS.md` for repository-wide agent instructions and review workflow;
- `CURRENT_MILESTONE.md` for the currently assigned task;
- `DESIGN_DECISIONS.md` and the canonical design docs for architectural intent;
- `milestones/NNN-*.md` for append-only milestone history and corrections.

Review findings should be judged against those sources, not generic best-practice preference.

## Unique responsibility and maintenance

This is the **single continuity brief for the code-review workspace**. It owns the readable
synthesis of:

- repository structure and implementation boundaries;
- the commit and milestone-to-commit ledger;
- the last verified build, test, replay, and behavioral baseline;
- known technical debt and integration risks;
- recurring failure patterns and the standing review checklist.

It does **not** own game ideation, design proposals, or the narrative account of design progress.
Those belong in `CANONICAL_DESIGN_CONTEXT.md` and the canonical design record it summarizes.

Update this brief after a milestone closes, a reviewed corrective commit changes the accepted
baseline, repository boundaries change, or a durable review lesson is discovered. During an active
milestone, `CURRENT_MILESTONE.md` remains the live handoff; do not duplicate its changing checklist
here.

## Repository

- Solution: `CrimeEmpire.sln`
- Branch at snapshot: `main`
- Runtime: .NET 10
- SDK pin: `10.0.400` in `global.json`, `rollForward: latestFeature`
- Target framework source of truth: `Directory.Build.props` (`net10.0`)
- Test framework: xUnit
- Current automated tests: 63

### Layout

```text
Crime Empire/
├─ docs/
│  ├─ PROJECT_CONTEXT.md
│  ├─ GAME_VISION.md
│  ├─ SIMULATION_ARCHITECTURE.md
│  ├─ INFORMATION_AND_LEGIBILITY.md
│  ├─ DESIGN_DECISIONS.md
│  ├─ OPEN_CONCERNS.md
│  ├─ CURRENT_MILESTONE.md
│  └─ milestones/
├─ src/
│  ├─ CrimeEmpire.Simulation/
│  │  ├─ Decision/
│  │  ├─ Domain/
│  │  ├─ Org/
│  │  ├─ Scenario/
│  │  ├─ Sim/
│  │  └─ Strategy/
│  └─ CrimeEmpire.Runner/
│     └─ Trace/
├─ tests/
│  └─ CrimeEmpire.Simulation.Tests/
├─ CrimeEmpire.sln
├─ Directory.Build.props
└─ global.json
```

### Project boundaries

- `CrimeEmpire.Simulation` contains deterministic, engine-independent simulation logic.
- `CrimeEmpire.Runner` contains command-line hosting and presentation/trace formatting.
- `CrimeEmpire.Simulation.Tests` verifies invariants, behavior, source limitation, and replay.
- Godot, console formatting, file-system concerns, and UI must stay outside the simulation library.

## Required reading before review

Read in this order:

1. `AGENTS.md`
2. `docs/DESIGN_DECISIONS.md`
3. The relevant sections of `GAME_VISION.md`, `SIMULATION_ARCHITECTURE.md`, and
   `INFORMATION_AND_LEGIBILITY.md`
4. `docs/OPEN_CONCERNS.md`
5. `docs/CURRENT_MILESTONE.md`
6. The archive file in `docs/milestones/` for the commit being reviewed

The milestone archive is essential for corrective commits because corrections are appended rather
than rewritten into the original account.

## Verification commands

Run from the repository root:

```powershell
dotnet build CrimeEmpire.sln
dotnet test CrimeEmpire.sln
dotnet run --project src\CrimeEmpire.Runner -- --verify --seed 42 --days 90
```

For information-channel changes, also run:

```powershell
dotnet run --project src\CrimeEmpire.Runner -- --verify --variant disloyal-vincent --seed 42 --days 90
dotnet run --project src\CrimeEmpire.Runner -- --compare --seed 42
dotnet run --project src\CrimeEmpire.Runner -- --variant disloyal-vincent --viewpoint salvatore --seed 42 --days 90
dotnet run --project src\CrimeEmpire.Runner -- --variant baseline --viewpoint vincent --seed 42 --days 90
```

### Verified baseline at `b8fe921`

- Build: 0 warnings, 0 errors.
- Tests: 63 passed, 0 failed.
- Baseline replay hash: `1BD9EA75CF02A84E` on both runs.
- Disloyal replay hash: `F743107E59F0EC7C` on both runs.
- Four variants produce four distinct histories.
- Decision counts: 13 / 16 / 13 / 43.
- Report counts: 2 / 2 / 2 / 6.
- Working tree after closeout commit `b3c404b`: clean.

Hashes are regression evidence for this snapshot, not permanent game-design requirements. A
deliberate behavior change may legitimately change them if tests and milestone documentation are
updated coherently.

## Implementation map

### Simulation and scheduling

- `Sim/EventQueue.cs` — deterministic scheduling and cancellation.
- `Sim/ScheduledEvent.cs` — event kinds, payloads, and discoverable traces.
- `Sim/Runner.cs` — advances only to scheduled events and dispatches institutional, perceptual,
  strategic, and deliberative work.
- `Sim/World.cs` — authoritative world state, truth log, decisions, reports, and information
  requests.
- `Sim/Rng.cs` — deterministic random streams.

Event ordering must remain deterministic. Adding collection traversal without explicit ordering is
a review hotspot.

### Decision pipeline

- `Decision/Pipeline.cs` — orchestrates perception through commitment.
- `Decision/Agenda.cs` — bounded agenda selection.
- `Decision/Generators.cs` — candidate generation and report/request eligibility.
- `Decision/Salience.cs` — trait-influenced perception and candidate salience.
- `Decision/Filters.cs` — knowledge, capability, and access rejection.
- `Decision/Utility.cs` — local utility scoring.
- `Decision/Commit.cs` — converts a selected candidate into world state and future events.
- `Decision/Inference.cs` — derives claims only from information the actor actually holds.
- `Decision/DecisionRecord.cs` — developer-facing explanation data.

The strongest architectural invariant is that decisions use `PerceivedSituation`, not `World`, for
situational facts. Do not add a world reference to the perceived view or expose raw cognition to
candidate generators.

### Domain and cognition

- `Domain/Character.cs` — character state and restricted `CharacterView`.
- `Domain/Psychology.cs` — closed trait/drive vocabulary.
- `Domain/Claim.cs` — structured claim vocabulary, stance, source kind, information records, and
  testimony.
- `Domain/Cognition.cs` — settled positions, testimony history, corroboration, contradiction, and
  reconsideration.
- `Domain/Report.cs` — reports, asserted/withheld claims, and claim-scoped information requests.
- `Domain/SocialState.cs` — directional relationships and grievances.

Review cognition changes as state-machine changes, not ordinary list updates. Check at least:

- first acquisition;
- identical repetition;
- independent corroboration;
- contradiction;
- recantation;
- affirm → deny → affirm;
- held → doubts/rejects and communication onward;
- acquisition time versus reconsideration time;
- contestedness after the settled stance changes.

### Organization and reporting

- `Org/Organization.cs` — conditions, priorities, policies, offices, and assignments.
- `Org/Reporting.cs` — message composition, per-recipient delivery status, and bounded content.

The report composer may read only the sender's perceived positions. It must never consult the truth
log to make the sender accurate.

Current channel distinctions that must not be collapsed:

- asserted;
- deliberately withheld;
- omitted because the message cap was reached;
- never considered;
- repeated unchanged;
- changed stance or confidence;
- explicit denial;
- unanswered request.

### Strategies and scenario

- `Strategy/Strategies.cs` — authored, parameterized multi-step procedures.
- `Scenario/Cast.cs` — harbor scenario and fixtures.
- `Scenario/Variants.cs` — configuration-only variants used to falsify personality behavior.

Strategies resolve against objective world state. Decisions to begin, alter, continue, delegate,
or abandon them remain belief-limited.

### Presentation

- `Runner/Trace/TraceWriter.cs` — omniscient developer trace; never a player view.
- `Runner/Trace/IntelligenceWriter.cs` — viewpoint-constrained player-readable history.
- `Runner/Program.cs` — command-line options, verification, comparison, and viewpoint selection.

`IntelligenceWriter` must not enumerate the authoritative roster to reveal unknown identities. It
may use only identities present in the viewpoint character's claims, testimony, relationships, or
grievances. Generic source kinds must not be rendered as physical attendance.

## Test map

- `EventQueueTests.cs` — event ordering, cancellation, and calendar boundaries.
- `SimulationReplayTests.cs` — deterministic histories, pause/resume equivalence, variant
  divergence, and snapshots of all future-decision-relevant state including requests.
- `InformationTransmissionTests.cs` — source limitation, deception, conflict, reporting,
  corroboration, identity/provenance leakage, behavior budgets, duplicate reports, request scope,
  and disloyal pause/resume behavior.

### Load-bearing regression categories

Future changes should retain coverage for:

- Player output contains no truth unavailable to the viewpoint.
- Unknown characters are not named from the global roster.
- Physical presence is not inferred from generic `Direct` provenance.
- Policy authorship is inferred or reported, not observed from violence alone.
- One source cannot repeatedly corroborate or erode confidence with the same account.
- A changed account is not mistaken for repetition.
- Retractions and non-held positions can be composed and delivered.
- Withholding counts as addressing a claim, while length-cap omission stays outstanding.
- Information requests are bounded by `(asker, recipient, claim)`, not merely by the pair.
- No ordered request tuple repeats.
- No two reports between the same pair are content-identical in the bounded scenario.
- All variants remain within explicit decision and report budgets.
- Pause/resume preserves reports, testimony, requests, and resulting history.

## Commit ledger

### Foundation

- `46f0777` — Initial Crime Empire simulation baseline.
- `4030699` — Reorganize simulation into `docs`, `src`, and `tests`.

### Milestone process and kernel archive

- `65a97c4` — Add milestone lifecycle policy and archive milestone 001.

### Milestone 002 — .NET 10

- `7032981` — Migrate target framework from `net9.0` to `net10.0`.
- `5463157` — Record Codex review outcome for milestone 002.

### Milestone 003 — information transmission

- `097fbda` — Original information-transmission implementation.
- `cf22e5d` — Fix first five Codex review findings.
- `2a74a5d` — Fix three verification findings from the first correction.
- `f97ef76` — Fix runaway corroboration loop and two report-channel defects.
- `2d9177d` — Clarify `CURRENT_MILESTONE.md` as the handoff surface and point reviewers to
  milestone archives.
- `b8fe921` — Separate withheld from unsaid, scope requests to a claim, and add request state to
  replay coverage. This is the verified implementation commit.
- `b3c404b` — Close milestone 003, move missing planning choices into the archive, and reset
  `CURRENT_MILESTONE.md`. Docs only.
- `d142582` — Open milestone 004 (Provenance Precision) and add the two canonical context briefs.
  Docs only.

Do not squash or rewrite this history merely to make milestone 003 look cleaner. The corrective
sequence records useful architectural failures and review lessons.

## Review history and recurring failure patterns

Milestone 003 exposed two recurring implementation mistakes.

### Narrowing expressiveness while fixing correctness

Examples:

- Filtering to held beliefs prevented retractions from being reported.
- Treating every repeat sender as duplicate blocked recantation.
- Matching any historical account instead of the latest blocked affirm → deny → affirm.

Review question: **What honest state or transition can no longer be represented after this fix?**

### Collapsing distinct states

Examples:

- Treating deliberately withheld as never said caused repeated partial reports.
- Treating a request as person-to-person rather than claim-scoped permanently closed the channel.
- Treating confidence as provenance produced “personally witnessed” claims without evidence of
  attendance.

Review question: **What two different things does this code now treat as one?**

### False-assurance tests

Two tests initially copied the implementation predicate into the test rather than invoking the
production rule. Both passed after the actual fix was reverted. Rules that need direct pinning were
made testable through production helpers such as `Generators.CanAsk`.

Mutation-checking is valuable, but a snapshot-field addition cannot be mutation-checked in the same
way: deleting a field merely weakens the comparator. Request actions are also written to the truth
log so runner verification gives an independent deterministic signal.

## Review checklist

### Architecture

- Does the simulation library remain engine-independent?
- Does decision code read character-relative information rather than truth?
- Do traits influence salience/evaluation without triggering behavior?
- Are strategies bounded and authored rather than unrestricted plans?
- Are policy breaches possible and consequential rather than mechanically forbidden?

### Determinism

- Are dictionary/set traversals explicitly ordered where they affect outcomes?
- Are IDs allocated from world state rather than process-global static counters?
- Is random state derived from stable inputs?
- Does pause/resume produce the same history?
- Is every new piece of future-decision-relevant state included in replay comparison?

### Information safety

- Can player output name anyone the viewpoint does not know?
- Can it claim observation or attendance from confidence alone?
- Can an actor infer hidden authorship directly from a visible consequence?
- Can a report communicate a position its sender does not hold?
- Can truth leak through formatting code, scenario fixtures, or helper lookups?

### Report-channel state machine

- Is the incoming account identical, changed in confidence, changed in stance, or reversed?
- Is source independence checked over testimony history?
- Does reconsideration make the updated position reportable?
- Can bounded composition crowd out a changed position and then incorrectly mark it delivered?
- Is withheld distinct from cap-omitted?
- Is the request spent when asked and scoped to the exact claim?
- Can silence create an unbounded ask loop?
- Can concealment create an unbounded partial-report loop?

### Tests

- Does each regression test invoke the production rule it claims to pin?
- Has the fix been temporarily reverted to prove the test fails where practical?
- Does the behavioral budget measure the runaway unit, not only a nearby aggregate?
- Are both baseline and stress/disloyal paths covered?
- Are player-visible leak assertions testing rendered output, not only hidden claim state?

### Documentation and process

- Is the work within the assigned milestone only?
- Are design conflicts surfaced rather than silently resolved?
- Are corrections appended to the milestone archive rather than rewriting history?
- Is the commit focused and independently reviewable?
- Is `CURRENT_MILESTONE.md` reset only after verification and closeout?

## Known technical debt and deferred work

- `SourceKind.Direct` lacks precise provenance categories.
- The test project redundantly declares `TargetFramework` despite the centralized build property.
- Godot 4 C# compatibility with `net10.0` is unverified.
- SQLite persistence is selected but not implemented.
- Save/load is absent.
- Relevance-tier promotion/demotion is absent.
- Relationship data remains under-specified.
- `OPEN_CONCERNS.md` #4 is stale because the trait vocabulary is already closed in code.

## Current next-step gate

Milestone 003 is closed. Milestone 004 (Provenance Precision) is active and approved: replace the
broad `SourceKind.Direct` with the smallest vocabulary that keeps participating, witnessing,
discovering a trace, and receiving a first-hand account distinct.

`CURRENT_MILESTONE.md` is the authoritative scope, not this document. Read it before implementing;
nothing beyond it should be inferred from the debt list above, and any scope change goes to Matt and
into that file first.

Note for review: `SourceKind.Direct` is not merely a label. It is used as a predicate at
`Cognition.Learn` (override rule), `Cognition.Receive` (erosion resistance and stance protection),
and `Salience.Perceive` (suspicion discount). Splitting it forces an explicit answer at each site,
which is why milestone 004 asks for it to be reviewed as a state-machine change.
