# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**Nothing is active.** Confirm scope with Matt before starting anything — including milestone 019 —
rather than inferring the next milestone from `ROADMAP.md` or from what was deferred below.

**Milestone 018 — The Player Can See What Their Choice Did — is implemented and awaiting review.**
Every new surface is a projection of already-authoritative typed state; no new persistent state was
added in `Commit.cs`, `Pipeline.cs`, or `Strategies.cs`. Full account, including the two natural
proofs (observed live before any test was pinned), the six mutation checks (each confirmed to fail for
the intended reason, then reverted — including one where the first attempt at a mutation guard proved
nothing until the test itself was strengthened), and complete verification output:
`docs/milestones/018-the-player-can-see-what-their-choice-did.md`. 543 tests passing (523 pre-existing
+ 20 new); all five variant trace hashes byte-identical to `REVIEW_LEDGER.md`'s recorded baselines —
this milestone changed no simulation behavior, only presentation. Stopping here for Codex review per
the milestone's own closing instruction.

Milestones 001–017 are all complete and accepted; see their own archives and `REVIEW_LEDGER.md` for
the corrected acceptance record of 015 and 016 specifically.

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
