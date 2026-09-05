# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**Nothing is active.** Milestone 023 — The Roster Reads — is implemented, tested and committed.
Milestones 021, 022 and 023 are all **unreviewed and unaccepted**.

**Milestone 023** gave the roster the half it never had: why a standing moved, dated and attached to
the man it concerns, alongside where it stands. Reverses a stated position in
`PlayerNarration.Standing` on Matt's ruling of 2026-09-04. History rather than a fifth relationship
dimension — nothing scores it, so all six variants are byte-identical on both hashes. Full account,
including two findings for later milestones:
`docs/milestones/023-the-roster-reads.md`.

**Everything from `34cd117` onward is unreviewed.** Codex ran out of usage during milestone 020's
correction chain. `REVIEW_LEDGER.md` calls this *cleared to build on*, not *accepted*.

## Next, per the demo arc

`ROADMAP.md`'s "The demo arc" — layer 1 continues with **024 The operation reads**, then **025 The
interface stops fighting the player**, then **026 The session has an ending**, and the layer ends in
a playtest. Nothing there is authorized yet; scope goes into this file one milestone at a time.

## Open, and Matt's call

**Trust from completed work.** Matt's own roster example — *"Don's opinion of Vincent is up because he
completed a heist for him successfully"* — still does not occur. Nothing raises trust when a man
completes work he was given; trust moves only through account conflicts and corroborations. Small in
code, at an existing call site, and genuinely interlocking: trust feeds `Utility.Loyalty`, so it moves
hashes. Not folded into 023 because it is a behaviour change rather than a display one.

## What is deferred

Not authorization to start any of it — see `ROADMAP.md`.

**From 023:** the RECENTLY feed now duplicates the roster history less informatively, a candidate for
removal in 025; `IntelligenceWriter` does not show the history, deliberately.

**From 022:** rumour mutation and false rumours; strength growing with repetition; street talk
reaching civilians, which needs rumour-to-fear to matter. And the standing finding: **the honest lever
for making rumour live is who is in earshot** — which layer 2's rival gang and extra shopkeepers
supply for free.

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
