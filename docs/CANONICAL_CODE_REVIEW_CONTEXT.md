# Crime Empire — Canonical Code and Review Context

Status snapshot: 2026-08-14 on `main`.

**Milestone 003 is closed** — Codex reviewed `d685015` with no findings and Matt accepted it on
2026-08-14. **Milestone 004 is closed** — Codex reviewed `1fe8a15` with no findings and Matt accepted it on
2026-08-14, after four corrective rounds. No milestone is active.

The working tree is accepted as of `1fe8a15`: milestones 003 and 004 are both closed, and every
commit carrying code has now been reviewed and accepted.

This file has misreported status five times: it claimed verification that had not happened, first
for `b8fe921` and then for `e83dacf`; it wrongly recorded `fb2c84d` and `714fbc3` as never reviewed;
and its next-step gate went stale twice — first telling readers milestone 004 was active and
approved, then telling them it was blocked on three unfixed findings after all five had been
corrected and accepted.

Twice out of five in the same section is the pattern worth noticing. A gate is prose that grants or
withholds permission, so it goes stale in both directions and is wrong in a way that changes what
the next reader does. Sweeps that look only for "awaiting review" or "not accepted" miss it — the
second failure said "not fixed" and slipped straight through one. Read the gate directly whenever
status moves. Treat the "Review coverage" section below as the authority over any status wording
elsewhere, including this paragraph.

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
- Current automated tests: 139

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

### Superseded baseline at `b8fe921`

Recorded as verified at the time; the review of that commit in fact returned two P1 defects. Kept
for comparison, not as the accepted baseline.

- Build: 0 warnings, 0 errors.
- Tests: 63 passed, 0 failed.
- Baseline replay hash: `1BD9EA75CF02A84E` on both runs.
- Disloyal replay hash: `F743107E59F0EC7C` on both runs.
- Four variants produce four distinct histories.
- Decision counts: 13 / 16 / 13 / 43.
- Report counts: 2 / 2 / 2 / 6.

### Accepted baseline — milestone 004, closed at `1fe8a15`

- Build: 0 warnings, 0 errors.
- Tests: 139 passed, 0 failed.
- Replay hashes: `B20C06E5838C0657` / `24A181B260F9C396` / `4B60DA962927A6F7` / `B274F395A61C5118`
  for baseline / cautious-vincent / watchful-boss / disloyal-vincent, each identical on both runs.
- Four variants produce four distinct histories.
- Decision counts: 13 / 16 / 13 / 19.

**Byte-identical to `c828bfa`.** Separating claimed from actual provenance, extending the receive
state machine, and snapshotting assignment disclosures produced no behavioural movement at all. The
leak was real and reachable through the API — its mutation check proves it — but never fired in
these four variants, because the one false denial that occurs reaches a listener who already holds
the claim and therefore takes the contradiction path, which never rewrote provenance. Correct and
currently invisible in play.

### Accepted baseline — milestone 003, closed at `d685015`

- Build: 0 warnings, 0 errors.
- Tests: 99 passed, 0 failed.
- Replay hashes: `EF5082E438500CAA` / `DAB6010D48E61234` / `B351E55B3B2C61DB` / `7F1228BFE32F2108`
  for baseline / cautious-vincent / watchful-boss / disloyal-vincent, each identical on both runs
  and unchanged by the false-candour guard, which never fires in any current variant.
- Four variants produce four distinct histories.
- Decision counts: 13 / 16 / 13 / **47**.

`baseline` and `watchful-boss` are byte-identical to the milestone-004 baseline below.
`cautious-vincent` has an identical decision and event stream — its hash moves only because the
trace now records the belief an answer was drawn from. `disloyal-vincent` genuinely changes, and the
change is the fix: two `SeekApproval` candidates aimed *down* the chain are gone, and the decisions
that had gone to them now produce real accounts.

The concealment runaway in `disloyal-vincent` no longer appears, but is latent rather than fixed —
see the RNG-keying item under known technical debt.

### Superseded baseline — milestone-004 first correction at `c828bfa`, rejected

- Tests: 120 passed. Decision counts 13 / 16 / 13 / 19; hashes as in the current baseline above.
  Rejected on three P1 and two P2, chiefly the private-provenance leak.

### Superseded baseline — milestone 004 implementation, reviewed and rejected

- Build: 0 warnings, 0 errors.
- Tests: 86 passed, 0 failed.
- Replay hashes: `EF5082E438500CAA` / `67D2F9E8E70CE18E` / `B351E55B3B2C61DB` / `8658E56EDFDC06B5`
  for baseline / cautious-vincent / watchful-boss / disloyal-vincent, each identical on both runs.
- Four variants produce four distinct histories.
- Decision counts: 13 / 16 / 13 / 45 — **unchanged** from `e83dacf`.
- Report counts: 2 / 2 / 2 / 7 — **unchanged** from `e83dacf`.

The hashes move; the simulation does not. Diffing the full decision and event stream against a
pre-change build, with provenance labels normalised, shows no chosen action changed in either
variant — only four score magnitudes, each by about 0.05, both deltas traceable to named
reassignments. The hash movement is the developer trace and the player-facing wording, not
behaviour. See `milestones/004-provenance-precision.md` for why the predicted behavioural change
did not materialise.

### Previous baseline at `e83dacf` — reviewed and rejected

- Build: 0 warnings, 0 errors.
- Tests: 73 passed, 0 failed.
- Baseline replay hash: `17E91AAA09F72437` on both runs.
- Disloyal replay hash: `76B31DDF574AFF8F` on both runs.
- Four variants produce four distinct histories
  (`17E91AAA09F72437` / `2B3538B949522221` / `22F156DCBE81C196` / `76B31DDF574AFF8F`).
- Decision counts: 13 / 16 / 13 / 45.
- Report counts: 2 / 2 / 2 / 7.

The movement is confined to `disloyal-vincent`, the only variant that exercises the request channel:
two more decisions and one more report, because a man who has already given one account can now be
asked about a second matter, and because a reply is now scoped to the question rather than to
whatever was most newsworthy. The other three variants are unchanged in shape; their hashes move
only because the request line in the truth log now names its subject.
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
  replay coverage. Reviewed; returned two P1 defects, fixed in `e83dacf`. **Not** a verified
  implementation commit, despite an earlier version of this line saying so.
- `b3c404b` — Close milestone 003, move missing planning choices into the archive, and reset
  `CURRENT_MILESTONE.md`. Docs only.
- `d142582` — Open milestone 004 (Provenance Precision) and add the two canonical context briefs.
  Docs only.
- `fb2c84d` — Correct the continuity record and unblock the milestone-004 scope language. Docs only.
- `e83dacf` — Scope the reply and the asking guard to the claim. Fixes the two P1 defects from the
  review of `b8fe921`. **Reviewed and rejected**: three findings, corrected in the sixth correction.
- `a5a72f1` — Record a milestone-003 verification that had not happened, and unblock milestone 004
  on that basis. Docs only, and wrong; corrected below.

### Milestone 004 — provenance precision

- `714fbc3` — Split `Direct` into four acquisition categories. The implementation commit.
  **Reviewed and rejected**: three P1 findings. All three were corrected over the four corrective
  rounds that followed and accepted at `1fe8a15`; none remains outstanding.
- `11c4a4a` — Record the implementation commit in the milestone-004 archive. Docs only.
- `d2af4c8` — Withdraw the false verification of `e83dacf`. Docs only.
- `cbadb0d` — Fix the three findings against `e83dacf`. Reviewed, no code findings.
- `170991b` — Enforce the false-candour invariant at `Reporting.Compose`. Reviewed, no code findings.
- `d685015` — Replace the stale next-step gate. Docs only. Reviewed clean; **milestone 003 accepted
  and closed here.**

Do not squash or rewrite this history merely to make milestone 003 look cleaner. The corrective
sequence records useful architectural failures and review lessons.

### Review coverage — read this before trusting any "verified" claim above

Reconciled with Matt on 2026-08-14. An earlier version of this section said `fb2c84d` and `714fbc3`
had never been reviewed; both had been. Only `e83dacf` had genuinely been skipped, and it has since
been reviewed too.

| Commit | Status |
|---|---|
| `d142582` | Reviewed. Findings, fixed in `fb2c84d`. |
| `fb2c84d` | Reviewed and **rejected**. Its findings are not recorded in this repository — outstanding. |
| `e83dacf` | Reviewed and **rejected**: three findings, two P1 and one P2. Corrected by `cbadb0d` and `170991b`. |
| `a5a72f1` | Reviewed. Findings accepted; the false verification it recorded was withdrawn in `d2af4c8`. |
| `714fbc3` | Reviewed and **rejected**: three P1 findings. Corrected by `c828bfa`, which was itself rejected. |
| `c828bfa` | Reviewed and **rejected**: three P1 and two P2, chiefly a false denial transmitting the sender's private basis. Corrected by `d783745`. |
| `d783745` | Reviewed and **rejected**: a silent `ActualBasis` default that marked honest briefings as misrepresented, and a repeat comparison collapsing Participant onto Witness. Corrected by `612bd50`. |
| `612bd50` | Reviewed and **rejected**: provenance still settable by halves through an object initializer or `with`, and live documentation claiming a review automation that does not exist. Corrected by `1fe8a15`. |
| `1fe8a15` | Reviewed, **no findings**. Matt accepted it on 2026-08-14. **Milestone 004 is closed.** |
| `cbadb0d`, `170991b` | The milestone-003 sixth correction. Reviewed with **no code findings**; all verification passed. One documentation finding against `170991b` — the stale next-step gate — fixed in `d685015`. |
| `d685015` | Reviewed, **no findings**. Matt accepted the milestone-003 correction on 2026-08-14. **Milestone 003 is closed.** |
| `11c4a4a`, `d2af4c8` | Status not established here. |

**Accepted: milestone 003, through `d685015`.** Its implementation and every corrective round have
been reviewed and accepted.

**Accepted: milestone 004, through `1fe8a15`.** It took four corrective rounds — `714fbc3`
rejected, then `c828bfa`, `d783745` and `612bd50` each rejected in turn — before the implementation
was accepted. Every commit carrying code is now reviewed and accepted, so the tree these baselines
were measured on is itself accepted.

`fb2c84d`'s rejection findings were never recorded in this repository. Every line it touched has
since been rewritten and re-reviewed several times over, so the item is probably moot — but it is
listed rather than quietly dropped, because "probably moot" is a judgement and not a review.

Test-green is not review, and review is not acceptance. `714fbc3` builds clean and passes its suite
and was still rejected; do not describe it as unreviewed or as safe.

Review is **manual and ordered**. Nothing runs on a timer and nothing tracks coverage on the
repository's behalf: commits are reviewed oldest-unreviewed-first, one at a time, by hand, and each
review names the exact commit whose diff was inspected. A later documentation commit does not stand
in for the implementation commit beneath it — if two land back to back, both still need reviewing.

The table above is that record, maintained by hand. An earlier version of this section described an
automatic checkpointing monitor. There is none, and believing there was is precisely how a reader
stops checking whether a review happened.

## Review history and recurring failure patterns

### Recording a review that did not happen

Twice now the record has claimed verification that never occurred: first `b8fe921`, then `e83dacf`.
The second time it was Claude writing the claim, in good faith, from a review report that named the
commit and quoted its exact test count and replay hashes — and which no review had produced.

Two mechanics made this easy to get wrong and remain worth recording explicitly:

- **Reviews at the time went to the latest commit only.** Two commits landing back to back skipped
  the earlier one permanently and silently. That is how `e83dacf` was missed. Taking commits in
  order, as described above, is what removes this failure mode — and it is a habit, not a mechanism,
  so it holds only as long as it is kept.
- **A review report is not proof that a review ran.** It is a document, and like any other observed
  content it can be about a commit nobody inspected.

Standing review rules:

1. Review advances one commit at a time, oldest unreviewed first, by hand. Never skip directly to
   `HEAD`, even when the intervening commits are documentation-only.
2. Every report names the exact commit whose isolated diff was inspected and distinguishes
   verification at that commit from verification performed only at a later `HEAD`.
3. Never write "verified" from a review report alone. Verified means Matt confirms acceptance of
   that specific reviewed commit.
4. The "Review coverage" list above is the authority. If a commit is not on the reviewed list, it
   is unreviewed, whatever prose elsewhere says.

Review question: **which commit did this review actually inspect, and is that the commit the record
is about to call verified?**

Milestone 003 also exposed two recurring implementation mistakes.

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

- ~~`SourceKind.Direct` lacks precise provenance categories.~~ **Closed** by milestone 004, accepted
  at `1fe8a15` after four corrective rounds. It split into `Participant` / `Witness` / `Discovery` /
  `FirstHandTestimony`, with one named predicate per behaviour in `Domain/Provenance.cs`, and
  separated what a speaker claims from what he privately has.
- The `FirstHandTestimony` suspicion discount of `0.15`, and the `Discovery` discount of `0.10`, are
  tuning guesses rather than derived figures. Nothing yet distinguishes them behaviourally from
  neighbouring values.
- No scenario variant contradicts a delegator's first-hand testimony, so milestone 004's central
  distinction is provable only in unit tests and never visible in play.
- **RNG keying**: observation rolls are seeded from global event IDs
  (`Rng.ForDecision(seed, observerId, 5000 + ev.Id)`), so adding or removing any event anywhere
  re-rolls every character's perception. This is what silenced the `ConcealIncident` runaway — the
  event IDs moved and Tommy's police-interest rolls all began to miss — rather than any repair.
- **`ConcealIncident` runaway: latent, not fixed.** It no longer occurs in `disloyal-vincent` for
  the reason above, and returns whenever those rolls land again. Do not record it as resolved.
- The test project redundantly declares `TargetFramework` despite the centralized build property.
- Godot 4 C# compatibility with `net10.0` is unverified.
- SQLite persistence is selected but not implemented.
- Save/load is absent.
- Relevance-tier promotion/demotion is absent.
- Relationship data remains under-specified.
- `OPEN_CONCERNS.md` #4 is stale because the trait vocabulary is already closed in code.

## Current next-step gate

**No milestone is active. Do not start one.**

- **Milestone 003 — closed and accepted.** Accepted by Matt on 2026-08-14 after `d685015` reviewed
  clean.
- **Milestone 004 — closed and accepted.** Accepted by Matt on 2026-08-14 after `1fe8a15` reviewed
  with no findings, following four corrective rounds. Every finding raised against it was corrected
  and accepted; none remains outstanding.

Every commit carrying code has been reviewed and accepted. There is no outstanding corrective work.

**The next step is Matt's to authorize** — a maintenance task or milestone 005, named by him and
written into `CURRENT_MILESTONE.md` before any simulation behaviour changes. Nothing on the debt
list, the carried-forward items, or the candidate list is a licence to begin.

An earlier version of this section said milestone 003 was closed and milestone 004 "active and
approved" while both were false. It was the last place in this file still issuing an instruction,
which is the dangerous kind of stale — a status line that is out of date misinforms, but a gate that
says "approved" grants permission. When reconciling status, sweep for the sentences that cause
something to happen ("is active", "is approved", "next step is", "is closed") before the ones that
merely describe.

`CURRENT_MILESTONE.md` is the authoritative scope and this document is not; read it before
implementing anything, infer no scope from the debt list above, and take any scope change to Matt
and into that file first.

Kept for whoever next touches provenance: `SourceKind.Direct` was never merely a label. It was read
as a predicate at `Cognition.Learn` (override rule), `Cognition.Receive` (erosion resistance and
stance protection), and `Salience.Perceive` (suspicion discount). Splitting it forced an explicit
answer at each site, which is why it had to be reviewed as a state-machine change rather than a
rename — and why it took four rounds to land.
