# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**Nothing is active.** Confirm scope with Matt before starting anything — including milestone 020 —
rather than inferring the next milestone from `ROADMAP.md` or from what was deferred below.
Milestone 019 changed no simulation behavior and settled no design question, so nothing it produced
narrows what milestone 020 could be — it stands exactly where it stood after milestone 018.

**Milestone 019 — Controlled/Autonomous Actor Parity Is Pinned — is accepted and closed.**
Investigating the controlled-versus-autonomous decision anomaly milestone 018's archive and
`ROADMAP.md` had recorded found it did not reproduce — not at the exact cited Tommy decision, not
across a bounded sweep of every variant and every character, not under a mismatched viewpoint, and
not at either of milestone 018's own earlier commits. Matt ruled the milestone reframed as
verification-only rather than have a production change invented against a falsified premise:
`ROADMAP.md`'s entry retired in place, `docs/milestones/018-...md` gained an appended correction
retracting its Finding 3 "candid vs. partial" claim without touching Findings 1–2, and the
investigation was promoted into a permanent regression suite
(`tests/CrimeEmpire.Simulation.Tests/ControlledAutonomousParityTests.cs`).

Codex reviewed the implementation (`99db4de`) and returned **FAIL** (three P2s: an incomplete
comparator, no true viewpoint-only isolation, a doc comment contradicting its own assertion).
Correction `c9af6b6` closed all three, mutation-checking both additions as required proof.
Codex reviewed that correction and returned **FAIL** again (two P2s: the comparator still incomplete,
and the archive had misreported round 1's findings). Correction `c335d7c` closed the completeness
gap via an explicit `World`/`Character` audit and a required mutation check — but, reading queue
contents, added a new `internal` accessor to production code, and repeated the identical
archive-misreporting mistake it was meant to fix. Codex reviewed that correction and returned
**FAIL** a third time (two P2s: the new accessor violated "production code unchanged" regardless of
being read-only, and the archive had misreported findings again). The third correction (`8b5e70f`)
removed the accessor entirely — production simulation code is byte-identical to the pre-milestone
baseline, not merely behaviourally unchanged — reached the same queue state through test-only
`System.Reflection` instead, and corrected the record a final time. **Codex independently reviewed
`8b5e70f` and returned PASS with no findings; Matt accepted `8b5e70f` on 2026-08-27 on the strength
of that review and closed the milestone.** No defect was ever found in production simulation
behavior; what the milestone produced is a permanent regression suite pinning the parity the
investigation confirmed, plus two corrected documentation records. Full account, including all three
corrections: `docs/milestones/019-controlled-autonomous-actor-parity-is-pinned.md`.

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

Milestones 001–019 are all complete and accepted; see their own archives and `REVIEW_LEDGER.md` for
the corrected acceptance record of 015, 016, 018, and 019 specifically.

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
