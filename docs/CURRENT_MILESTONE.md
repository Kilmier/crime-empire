# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**Nothing is active.** Confirm scope with Matt before starting anything — including milestone 019 —
rather than inferring the next milestone from `ROADMAP.md` or from what was deferred below.

**Milestone 018 — The Player Can See What Their Choice Did — corrected twice and awaiting re-review.**
Codex reviewed the original implementation (`ae06f61`) and returned FAIL (two P1 defects, one P2 proof
gap); Matt authorized a correction (`b9dfa49`) that fixed both. Codex reviewed that correction and
returned FAIL again: the correction's own fix for pending-vs-declined was itself a private-decision
leak (reading whether the asked character's own `DecisionRecord` existed, which the asker has no way
to know), the action-kind audit called `PlayerOption.Describe` directly instead of exercising
`PlayerView.Build`/`LastAction`, and `InformationRequest.WakeEventId` — genuine new persistent
linkage state, contrary to the correction's "no new persistent state" claim — was missing from both
replay comparators. Matt authorized a second correction to milestone 018 only.

All three are fixed. Request disposition is now `Pending`/`Answered` — two values, not three — read
entirely from the *asker's* own `Cognition.Testimony`, never from `World.Decisions` for anybody else;
a same-pass attempt to keep a third `Declined` value for "a communicated denial" was itself caught and
reverted by a test, since the natural proof scenario has Vincent give Salvatore a full, sincere,
informative account that happens to contradict him — an answer, not a refusal, and this simulation's
report vocabulary has no utterance distinct from "an account, possibly negative." The action-kind
audit now drives each variant event by event and asserts on a real `PlayerView.Build` snapshot's
`LastAction` after every decision. `WakeEventId` is now covered by both request comparators, with a
focused proof each that differing linkage identities cannot compare equal, and the "no new persistent
state" claim is corrected everywhere to the accurate, narrower one: no separate response log was
introduced.

Full account, including the original implementation and both appended correction sections:
`docs/milestones/018-the-player-can-see-what-their-choice-did.md`. 552 tests passing (548 prior + 4
net new); all five variant trace hashes byte-identical to `REVIEW_LEDGER.md`'s recorded baselines
across all three commits — this milestone changed no simulation behavior, only presentation. Stopping
here for Codex re-review.

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
