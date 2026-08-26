# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**Nothing is active.** Confirm scope with Matt before starting anything — including milestone 018 —
rather than inferring the next milestone from `ROADMAP.md` or from what was deferred below.

**Milestone 017 — Direct Action vs Delegation: Different Causal Footprints — is implemented, tested,
and archived, awaiting review.** Full account, including the fork mechanism, all verification figures,
every mutation-check result, and two recorded discoveries: `docs/milestones/017-direct-action-vs-delegation.md`.
Not yet accepted — per `REVIEW_LEDGER.md`'s standing rule, only Matt's confirmation of a named commit
establishes that, and this milestone has not had one yet.

Milestones 001–016 are all complete and accepted; see their own archives and `REVIEW_LEDGER.md` for
the corrected acceptance record of 015 and 016 specifically.

**Codex is intermittent rather than withdrawn.** Claude implements and reviews its own work in the
meantime — see `REVIEW_LEDGER.md` §"From milestone 010 onward, review is self-assessment".

## What is deferred, for whoever scopes the next milestone

Not authorization to start any of it — see `ROADMAP.md`, which is where scope is proposed from, and
`docs/milestones/017-direct-action-vs-delegation.md`'s "Deferred work" section for the full, current
list carried from milestone 017's own authorizing text. In brief: choosing between multiple
subordinates; recruitment, crew rosters, specialists, equipment, preparation, budget allocation;
making Persuasion, Coercion, or crew size affect tribute success; resolving whether escalation
capability belongs to the owner or the delegate; resource transfer from owner to delegate; territory,
patrol, weekly planning; additional businesses or operations; a seventh character; employee-stat
displays or new UI panels; the known pause-timing information leak; new organizations, careers, or
alternate playable roles. Executor suitability/capability — whether delegation ever reflects who would
actually do the job better — is now recorded in `ROADMAP.md`'s known technical debt, not already
having been before milestone 017.

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
