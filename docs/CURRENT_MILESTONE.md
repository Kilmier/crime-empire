# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**Nothing is active.** Milestone 010 — A Denial That Can Win — was implemented on 2026-08-17 and is
**awaiting Matt's review**. Confirm scope with Matt before starting anything; do not begin milestone
011.

Milestones 001–009 are complete and accepted. **Milestone 009 was accepted on 2026-08-16 on the
strength of `7ca7819`**, and `REVIEW_LEDGER.md`'s coverage checkpoint stands there. The milestone 010
implementation commit is later than the checkpoint, has no row, and needs reviewing in its turn.

**Codex remains withdrawn from the review loop until further notice.** Claude implements and reviews
its own work — see `REVIEW_LEDGER.md` §"From milestone 010 onward, review is self-assessment" for the
method, and for why a self-review is recorded as weaker evidence than what it replaced.

## What milestone 010 concluded

Full account in `docs/milestones/010-a-denial-that-can-win.md`. In short: both structural defects were
real and both are fixed — a concealment now names its incident and its first step revises the
concealer's own belief about who saw him, and the denial's exposure term is scoped to that incident
rather than maxed over everything he holds.

**The denial still loses in every variant**, narrowest margin 1.083, which ruling 3 stated in advance
would be the result rather than a failure. What the milestone delivers instead is the measured
explanation: one cleanup attempt is worth `-0.2` where roughly `-0.4` is needed and the MVP rule
permits exactly one; Tommy cannot roll a clean cleanup at any seed; and Vincent, whose denial comes
closest, is never offered a cleanup at all.

Baselines moved in four of five variants, as ruling 6 said they would. Every moved figure is in
`REVIEW_LEDGER.md` with its reason.

## What is open, for whoever picks up next

Not a queue and not authorization. Candidate scopes live in `ROADMAP.md`; nothing there or here is a
licence to begin.

Two findings from milestone 010's self-review were deliberately **not** fixed, being outside its
authorized scope, and are recorded in `ROADMAP.md`'s debt list:

- `Strategies.AdvanceInvestigation`'s "the trail went cold" branch is a no-op, for exactly the reason
  milestone 010's defect 1 existed. `Cognition.Revise` is the method it needs.
- The same location-scoped-rather-than-incident-scoped shape survives in that function's lead pickup
  and stale-claim demotion.

Everything carried into milestone 010 is still carried, plus the two items above and the two cast /
threshold facts named in its findings 2 and 3. The full list is at the end of
`docs/milestones/010-a-denial-that-can-win.md`.

## Ordered review process

Unchanged. Matt takes commits in order, oldest first; each review names the exact commit whose diff
was inspected; the coverage table in `REVIEW_LEDGER.md` is the record. **Never write "verified" or
"accepted" from a review report alone** — including one of Claude's own. Matt's confirmation of a
named commit is the only thing that counts, and that rule matters more now that the reviewer and the
author are the same.
