# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**Nothing is active.** Confirm scope with Matt before starting anything — including milestone 021 —
rather than inferring the next milestone from `ROADMAP.md` or from what was deferred below.

**Milestone 020 — The Right Person for the Job — is implemented, tested, and awaiting Matt's
acceptance and Codex review.** Gave Vincent a second organisational subordinate (the `capable-angelo`
variant: Angelo Conti, Coercion 0.80, trusted at 0.35, against Tommy's 0.55/0.70) so
`Generators.FromRelationship`'s delegation choice is genuinely comparative rather than a foregone
pick of the single highest-trust subordinate. `Utility` gained one new "executor capability" score
component, present only when there is a real choice among two or more subordinates and therefore
never emitted for any existing accepted variant; `Strategies.ResolveViolence` now scales its force
outcome by the actual executor's Coercion, calibrated at Tommy's own stat (0.55 — the only value
that call has ever been exercised against in an accepted run) so no existing trace hash moved. All
five pre-existing variant hashes confirmed byte-identical via `--compare`; the natural run at seed 42
delegates to Angelo over Tommy, an honest measured result rather than a tuned one. Full account,
including both flagged judgment calls (the seventh character, the force-resolution calibration) and
all five required mutation checks: `docs/milestones/020-the-right-person-for-the-job.md`.

Milestones 001–019 are all complete and accepted; see their own archives and `REVIEW_LEDGER.md` for
the corrected acceptance record of 015, 016, 018, and 019 specifically.

**Codex is intermittent rather than withdrawn.** Claude implements and reviews its own work in the
meantime — see `REVIEW_LEDGER.md` §"From milestone 010 onward, review is self-assessment".

## What is deferred, for whoever scopes the next milestone

Not authorization to start any of it — see `ROADMAP.md`, which is where scope is proposed from.

Carried from milestone 020, per its own exclusions (`ROADMAP.md`'s narrowed "Executor
suitability/capability" entry): Persuasion's effect on tribute success; crew size, equipment,
preparation; recruitment, roster, payroll, or resource transfer from owner to delegate; personnel
management generally; escalation-capability ownership as a general rule beyond the one mechanism
milestone 020 touched; a third or later subordinate; a general suitability model across strategy
kinds; capability affecting anything beyond force resolution.

Carried from milestone 017 and earlier, still unresolved: the five-column layout and other
playtest-discovered presentation debt (milestone 018); the wrapped-date/toolbar debt; a
`PlayerNarration` prose rewrite; "You control"/"You see through" unification; territory, patrol,
weekly planning; additional businesses or operations; an eighth character; employee-stat displays or
new UI panels; the known pause-timing information leak; new organizations, careers, or alternate
playable roles.

Carried from milestone 016 and earlier, still unresolved: **124 live-edge findings and 5
apparently-dead lines** (`docs/COVERAGE_ACCOUNTING.md`); systematic mutation automation and
seed-sweep promotion; a queryable decision-trace store; the allegation option naming the same person
twice; the developer trace's uniform "he"; nobody holding a scored relationship with Kane; the tuning
guesses; obligation read but never moved; slot management, autosave, cloud save, a save-browser UI,
and cross-build save migrations. From `docs/OPEN_CONCERNS.md` #3, still open: decay and its rate,
negative trust, whether respect/resentment are separate dimensions, whether provenance should weight
the social consequence, and whether `GrievanceWeight` should be capped.
