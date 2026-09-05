# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**Nothing is active.** Milestone 021's correction — Codex's review of `e65f0cd` — is implemented,
tested and committed, and is **awaiting Codex re-review**. Milestone 025 is implemented and committed
and **Matt is playtesting it**. Milestones 021 through 025 are all **unreviewed and unaccepted**;
021's correction has not been back to Codex either.

**Milestone 021's correction** removed a belief revision from the silent blocked path (nothing had
reached the man), made every `Cognition.Revise` state its occasion alongside the acquisition source,
and made `CapabilityBar`'s ladder resolve on read through one shared accessor so the scorer and the
roster cannot reach different tiers. **`capable-angelo`'s hashes moved, authorized by Matt on
2026-09-05**: trace `12AF1B71EBBDF51F`, actions `1EDE45C580544105`, 37 decisions; the other five
variants unmoved. 636 tests; four mutation checks. Full account: the appended correction in
`docs/milestones/021-capability-is-a-belief-not-a-stat.md`.

**Milestone 025** cleared milestone 018's presentation debt by subtraction: two of the five columns
were copies and went, with their projections; the rest became four weighted panels under a two-row
strip; "Play as" is one field; and — widened by Matt mid-milestone, reversing his own ruling 3 —
every player-facing phrase was rewritten into plain English and put in the second person for the
character being played. All six hashes unmoved by construction; 623 tests; seven Godot invocations
green; the duplicate-claim guard mutation-checked. Full account, including the authorization text as
written: `docs/milestones/025-the-interface-stops-fighting-the-player.md`.

**Everything from `34cd117` onward is unreviewed.** Codex ran out of usage during milestone 020's
correction chain. `REVIEW_LEDGER.md` calls this *cleared to build on*, not *accepted*. The playtest at
the end of layer 1 is the review 025 gets.

## Next, per the demo arc

`ROADMAP.md`'s "The demo arc" — layer 1 finishes with **026 The session has an ending**, and then the
layer ends in a playtest. Nothing is authorized; scope goes into this file one milestone at a time,
and what the playtest of 025 finds goes first.

**Carried into 026 from 025:** a history of finished operations (`Strategies.Complete` nulls the
instance, so the "what you are doing" panel goes empty the moment a job finishes); and whatever lines
still read badly in play — the prose has been rewritten once, by one reader.
