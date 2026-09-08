# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**Nothing is active.** Milestone 026 — In Person, Things Come Back — is implemented, tested and
committed, and **Matt is playtesting**. Milestone 021 now has a third correction. Codex reviewed
correction 2 (`b02b003`), confirmed its three fixes, and found one further P1: `Cognition.Learn`'s
overriding branch built its replacement from a brand-new `InformationRecord`, which defaults
`Reconsidered` to null, and then advanced `LastReconsideredAt` without ever naming a cause — the same
timestamp/cause pairing invariant correction 2 closed for `Revise` and `Receive`, left open in the one
writer neither had touched. `ReconsiderCause` gains `AcquiredAgain`, carrying the overriding call's own
source channel and identity; one regression test, mutation-checked. 655 tests, full verification green,
no hash moved. Full account in `docs/milestones/021-capability-is-a-belief-not-a-stat.md`'s
"Correction 3". **This third correction now awaits Codex re-review.** Milestones 021 through 026 are
all **unreviewed and unaccepted**.

**Milestone 026** came out of the playtest of 025 rather than the arc: a man asked about something he
holds nothing on now says so, and a man told something or threatened to his face shows something the
speaker reads — correctly, wrongly, or not at all — as an impression on his own relationship, never as
the other man's state. Every hash moved, as scoped. Corrected twice from play the same day: he knows
what he is good at, in words; and a pause says what hangs over him, in his own terms. Corrected a third
time from Codex's review of those two: `Reactions.Landed` inferred "news" from a timestamp coincidence,
and `PlayerSnapshot.Exposure`'s per-recipient reaction lookup was never tied to the specific report it
described, so a withheld-only report could borrow an older reaction to a different incident. Now
corrected a fourth time, from Codex's review of the third: the third correction's own match — recipient,
timestamp, claim — was still not unique, since nothing forbids two distinct reports to the same
recipient, about the same claim, at the same instant, and `Impression` carried no reference back to the
report that produced it. `Impression` gains `ReportId`, set from `Reactions.AfterReport`;
`Exposure` matches it exactly; the comprehensive replay comparator carries it and the narrower one
deliberately does not, for the same reason it already excludes every other `Report.Id`-derived field.
One regression test, one mutation check, no hash moved. 658 tests; all seven Godot invocations green.
Full account, with the five rulings as taken and all four corrections:
`docs/milestones/026-in-person-things-come-back.md`. **This fourth correction now awaits Codex
re-review.**

**Everything from `34cd117` onward is unreviewed.** Codex ran out of usage during milestone 020's
correction chain. `REVIEW_LEDGER.md` calls this *cleared to build on*, not *accepted*.

## Next, per the demo arc

`ROADMAP.md`'s "The demo arc" — layer 1 finishes with **027 The session has an ending**, and then
the layer ends in a playtest. Nothing is authorized; scope goes into this file one milestone at a
time, and what the playtest finds goes first.

**Carried into whatever comes next:** a reader of an impression (a man who saw he was not believed
has reason to act, and nothing offers him anything yet); the operation's pacing, parked at Matt's
word; a history of finished operations, which is 027's.
