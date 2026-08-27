# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**Nothing is active.** Confirm scope with Matt before starting anything — including milestone 020 —
rather than inferring the next milestone from `ROADMAP.md` or from what was deferred below.

**Milestone 019 — Controlled/Autonomous Actor Parity Is Pinned — is complete pending Codex re-review.**
Investigating the controlled-versus-autonomous decision anomaly milestone 018's archive and
`ROADMAP.md` had recorded found it did not reproduce — not at the exact cited Tommy decision, not
across a bounded sweep of every variant and every character, not under a viewpoint different from
the controlled character, and not at either of milestone 018's own earlier commits. Matt ruled the
milestone reframed as verification-only: the investigation was promoted into a permanent regression
suite (`tests/CrimeEmpire.Simulation.Tests/ControlledAutonomousParityTests.cs`), and the record was
corrected — `ROADMAP.md`'s entry retired in place and `docs/milestones/018-...md` gained an appended
correction retracting its Finding 3 "candid vs. partial" claim without touching Findings 1 and 2.

Three Codex review rounds followed, and production simulation code (`src/CrimeEmpire.Simulation/`)
is, as of the third correction, byte-identical to the pre-milestone baseline — the only thing that
moved in either direction along the way was one narrow accessor added in round 2's correction and
then removed again in round 3's, never anything simulation-behavioural. **Round 1**, reviewing
`99db4de`, found three P2s: the fingerprint didn't cover enough persistent state, there was no true
viewpoint-only isolation, and a doc comment on the focused Tommy test contradicted its own assertion
(claimed a request "drops out of `AwaitingAnswers`" when it correctly stays outstanding). Correction
`c9af6b6` closed all three, and — required as proof for the first two, not a finding of its own —
mutation-checked both additions (which incidentally surfaced and fixed a real bug in the test
harness's own driving loop, unrelated to production code). **Round 2**, reviewing `c9af6b6`, found
two P2s: the fingerprint remained incomplete (`Capabilities.Cash`, `Execution.Intention`,
`ObservationOccasionKeys`, queued-event contents named specifically, with more found by an explicit
audit), and this archive's own account of round 1 was inaccurate. Correction `c335d7c` closed the
completeness gap — a full `World`/`Character` audit, a `Capabilities.Cash` mutation check required
as proof for that same finding — but reading queue contents that round added a new `internal`,
read-only `EventQueue.PendingEvents` accessor to production code, and separately mislabeled
mutation-checking as round 2's *second* finding rather than proof for its first, burying what the
real second finding (the archive's own inaccuracy) actually was. **Round 3**, reviewing `c335d7c`,
correctly rejected the new accessor — read-only and behaviourally inert is not an exception a
milestone's own archive gets to grant itself against an explicit "production code unchanged"
requirement — and caught the repeated mislabeling. This correction removes `PendingEvents` entirely
(`git diff` against the pre-milestone baseline for `src/CrimeEmpire.Simulation/` is now empty) and
reaches the identical queue state through test-only `System.Reflection` instead, verified once by a
temporary sanity-check test (confirmed the reflection path reads real, non-empty, count-matching
queue contents) then deleted rather than kept as a sixth permanent test. It also states plainly, for
a reader who does not want to reconstruct it from three overlapping corrections, what each round's
findings actually were and that mutation-checking was required proof work under the completeness
finding in both rounds it appeared, never an independent finding in either. All accepted trace
hashes and chosen-action digests were confirmed unchanged across all three verification passes. Full
account, including all three corrections: `docs/milestones/019-controlled-autonomous-actor-parity-is-pinned.md`.

**Milestone 018 — The Player Can See What Their Choice Did — is accepted and closed.** Codex reviewed
the original implementation (`ae06f61`) and returned **FAIL** (two P1 defects, one P2 proof gap); the
correction in `b9dfa49` fixed both. Codex reviewed that correction and returned **FAIL** again: the
correction's own fix for pending-vs-declined was itself a private-decision leak (reading whether the
asked character's own `DecisionRecord` existed, which the asker has no way to know), the action-kind
audit called `PlayerOption.Describe` directly instead of exercising `PlayerView.Build`/`LastAction`, and
`InformationRequest.WakeEventId` — genuine new persistent linkage state, contrary to the correction's
"no new persistent state" claim — was missing from both replay comparators. The second correction in
`f5246c0` fixed all three: request disposition became `Pending`/`Answered` — two values, not three —
read entirely from the *asker's* own `Cognition.Testimony`; the action-kind audit was rewritten to drive
each variant event by event and assert on a real `PlayerView.Build` snapshot; `WakeEventId` was covered
by both replay comparators. Codex reviewed that correction and confirmed all three defects resolved,
but flagged one P2 cleanup: `WakeEventId` had no production consumer left, and
`RequestDisposition`/`PlayerRequest.Disposition` were redundant, since `PlayerRequest` objects are only
ever built for requests that already fail the "answered" check. The third correction in `43379e0`
removed all three: `InformationRequest.WakeEventId`, the `RequestDisposition` enum, and
`PlayerRequest.Disposition` are gone; `Commit.cs`'s `SeekCorroboration` case reverted to its original
request-before-schedule ordering; `AwaitingAnswers` now filters directly on the asker's own
`Cognition.Testimony` with no disposition value at all. **Codex independently reviewed `43379e0` and
returned PASS with no findings; Matt accepted `43379e0` on 2026-08-26 on the strength of that review and
closed the milestone.** Full account, including the original implementation and all three appended
corrections: `docs/milestones/018-the-player-can-see-what-their-choice-did.md`.

Milestones 001–018 are all complete and accepted; see their own archives and `REVIEW_LEDGER.md` for
the corrected acceptance record of 015, 016, and 018 specifically.

**Codex is intermittent rather than withdrawn.** Claude implements and reviews its own work in the
meantime — see `REVIEW_LEDGER.md` §"From milestone 010 onward, review is self-assessment".

## What is deferred, for whoever scopes the next milestone

Not authorization to start any of it — see `ROADMAP.md`, which is where scope is proposed from, and
`docs/milestones/018-the-player-can-see-what-their-choice-did.md`'s "Deferred" section for this
milestone's own carried items (full GUI redesign, the five-column layout not yet rebalanced, the
wrapped-date/toolbar debt, a `PlayerNarration` prose rewrite, "You control"/"You see through"
unification, and the rest — recorded in `ROADMAP.md`'s "Known technical debt" as playtest-discovered
presentation debt).

Carried from milestone 017 and earlier, still unresolved: choosing between multiple subordinates;
recruitment, crew rosters, specialists, equipment, preparation, budget allocation; making Persuasion,
Coercion, or crew size affect tribute success; resolving whether escalation capability belongs to the
owner or the delegate; resource transfer from owner to delegate; territory, patrol, weekly planning;
additional businesses or operations; a seventh character; employee-stat displays or new UI panels; the
known pause-timing information leak; new organizations, careers, or alternate playable roles. Executor
suitability/capability — whether delegation ever reflects who would actually do the job better — is
recorded in `ROADMAP.md`'s known technical debt.

Carried from milestone 016 and earlier, still unresolved: **124 live-edge findings and 5
apparently-dead lines** (`docs/COVERAGE_ACCOUNTING.md`); systematic mutation automation and
seed-sweep promotion; a queryable decision-trace store (`ROADMAP.md` candidate 3's original framing,
narrowed by milestone 015 to a replay log); the allegation option naming the same person twice; the
developer trace's uniform "he"; nobody holding a scored relationship with Kane; the tuning guesses;
the cast ceiling of six; obligation read but never moved; slot management, autosave, cloud save, a
save-browser UI, and cross-build save migrations (milestone 015's exclusions). From
`docs/OPEN_CONCERNS.md` #3, still open: decay and its rate, negative trust, whether respect/resentment
are separate dimensions, whether provenance should weight the social consequence, and whether
`GrievanceWeight` should be capped.
