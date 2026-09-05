# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**Nothing is active.** Milestone 024 — The Operation Reads — is implemented, tested and committed.
Milestones 021 through 024 are all **unreviewed and unaccepted**.

**Milestone 024** put the standing order on screen: what he has running, who is carrying it, and — for
his own work only — how far he has got. A delegated job reads as what he ordered and silence, because
how far somebody else has got is that man's state, and milestones 017 and 022 both settled that the
owner learns whether it was carried out through a report or a roll. All six hashes unmoved. Full
account: `docs/milestones/024-the-operation-reads.md`.

**Everything from `34cd117` onward is unreviewed.** Codex ran out of usage during milestone 020's
correction chain. `REVIEW_LEDGER.md` calls this *cleared to build on*, not *accepted*.

## Next, per the demo arc

`ROADMAP.md`'s "The demo arc" — layer 1 finishes with **025 The interface stops fighting the player**
and **026 The session has an ending**, and then the layer ends in a playtest. Nothing is authorized
yet; scope goes into this file one milestone at a time.

**025 has accumulated a specific list**, which is worth having in one place when it starts:

- Milestone 018's recorded debt: the five-column layout never rebalanced after a fourth was added, the
  vertically-wrapped date, the oversized toolbar, `PlayerNarration`'s prose, and "You control" / "You
  see through" as one selector.
- The RECENTLY feed now duplicates the roster history less informatively — the roster line gives the
  cause and the feed does not. Candidate for removal or narrowing (from 023).
- 024 added the standing order to the "WHAT JUST HAPPENED" column rather than a sixth column,
  deliberately, to avoid making the layout worse before it is fixed.

## Open, and Matt's call

**A history of finished operations.** `Strategies.Complete` nulls `Execution.Strategy`, so a completed
operation leaves nothing behind: the panel goes empty the moment a job finishes, and there is nowhere
to read what he has already done. Honest but thin, and a natural fit for 026, where a session that ends
needs to be able to say what happened in it.

## What is deferred

Not authorization to start any of it — see `ROADMAP.md`.

**From 024:** multiple simultaneous operations, which the model does not have — one `Strategy` per
character.

**From 023:** trust from completed work was considered and **declined** — trust means "would I take his
word" and moves on account conflicts and corroborations, so folding job outcomes into it would collapse
the distinction milestone 021 drew between reliability as an informant and reliability as an executor.

**From 022:** rumour mutation and false rumours; strength growing with repetition; street talk reaching
civilians, which needs rumour-to-fear to matter. The honest lever for making rumour live is **who is in
earshot** — which layer 2's rival gang and extra shopkeepers supply for free.

**From 021:** assessments of skills other than Coercion; decay of an assessment; a capability belief
acquired by testimony being permanently unrevisable; a character's confidence in his own ability.

**From 020 and earlier:** crew, equipment, preparation, recruitment, payroll, resource transfer; a
third subordinate; a general suitability model. Territory, patrol, weekly planning; the known
pause-timing information leak.

**From 016 and earlier:** 124 live-edge findings and 5 apparently-dead lines
(`docs/COVERAGE_ACCOUNTING.md`); systematic mutation automation and seed-sweep promotion; a queryable
decision-trace store; nobody holding a scored relationship with Kane; the tuning guesses; obligation
read but never moved; save slots, autosave, a save browser, cross-build migrations. From
`OPEN_CONCERNS.md` #3: decay, negative trust, whether respect and resentment are separate dimensions,
whether provenance should weight the social consequence, and whether `GrievanceWeight` should be
capped.
