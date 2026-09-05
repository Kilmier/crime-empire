# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**Nothing is active.** Milestone 022 — The Street Talks — is implemented, tested and committed;
milestones 021 and 022 are both **unreviewed and unaccepted**.

**Milestone 022** gave `SourceKind.Rumor` its first producer. Proximity to a beating and the boss's
access to his own territory now yield street talk — attributed to the neighbourhood, never to a man,
at the heaviest suspicion discount — where both previously arrived as the observer's own discovery.
The man who ordered it keeps discovery, and the detective who went looking keeps hers. **It is inert
in the accepted fixture**: no rumour survives its roll at seed 42, every hash is unmoved, and the
proofs are staged. That was named as an acceptable outcome before implementation and is recorded
rather than engineered away. Full account, including the two findings that came out of implementation
rather than review: `docs/milestones/022-the-street-talks.md`.

**Everything from `34cd117` onward is unreviewed.** Codex ran out of usage during milestone 020's
correction chain. `REVIEW_LEDGER.md` calls this state *cleared to build on*, not *accepted*.
Milestone 020 took two Codex rejections, each a P1, plus a third defect found while scoping 021; 021's
own headline claim proved half-demonstrated; and 022's scope review missed its load-bearing case.
Four instances of a green suite concealing a real problem, none caught by the tests.

## What is next

Matt is considering a planned arc of roughly fifteen milestones toward a playable demo — built in
batches, playtested between them, with the playtest standing in for the missing adversary. Nothing
about that is scoped yet and none of it is authorized.

## What is deferred

Not authorization to start any of it — see `ROADMAP.md`.

**From milestone 022:** rumour mutation and false rumours (canon's own open question, and what would
deliver `GAME_VISION.md`'s "a capo may sincerely believe a false rumor"); strength growing with
repetition; street talk reaching civilians, which needs rumour-to-fear to matter; media coverage; a
social transmission graph. And the finding that shapes any of it: **the honest lever for making rumour
live is who is in earshot**, a scenario question, not a nudged probability.

**From milestone 021:** assessments of skills other than Coercion; decay of an assessment; a
capability belief acquired by testimony being permanently unrevisable (in `ROADMAP.md` as debt); a
character's confidence in his own ability.

**From milestone 020 and earlier:** crew, equipment, preparation, recruitment, roster, payroll,
resource transfer, personnel management; a third subordinate; a general suitability model. Territory,
patrol, weekly planning; additional businesses or operations; an eighth character; the playtest-
discovered presentation debt from milestone 018 (five-column layout, wrapped dates, `PlayerNarration`
prose, "You control"/"You see through"); the known pause-timing information leak.

**From milestone 016 and earlier:** 124 live-edge findings and 5 apparently-dead lines
(`docs/COVERAGE_ACCOUNTING.md`); systematic mutation automation and seed-sweep promotion; a queryable
decision-trace store; nobody holding a scored relationship with Kane; the tuning guesses; obligation
read but never moved; save slots, autosave, a save browser, cross-build migrations. From
`OPEN_CONCERNS.md` #3: decay, negative trust, whether respect and resentment are separate dimensions,
whether provenance should weight the social consequence, and whether `GrievanceWeight` should be
capped.
