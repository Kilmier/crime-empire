# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**No milestone is active. Do not start one.**

Milestones 001–005 are complete. The most recent, **005 — Stable Occasion Identity and Strategy
Lifecycle Safety**, replaced the global-scheduling-id RNG keying with causally local occasion keys
and gave `ConcealIncident` a state-based termination rule; its full record, including the causal
accounting for the resulting behavioural movement, is in
`docs/milestones/005-stable-occasion-identity-and-strategy-lifecycle-safety.md`.

`REVIEW_LEDGER.md` alone defines review coverage. Consult its checkpoint directly rather than
inferring status from prose anywhere else, including this file — and do not restate here which
commit is or isn't yet covered: that detail goes stale the instant the ledger's own checkpoint
advances, which is exactly what happened to an earlier version of this paragraph.

**Milestone 006 has not been chosen.** Do not infer it from the candidate list or technical-debt
list in `ROADMAP.md`, or from the carried-forward items below. Confirm scope with Matt and write it
here before changing simulation behaviour.

## Ordered review process

Review is **manual**. There is no monitor running on a timer, no checkpoint the repository keeps for
itself, and nothing that will notice a commit unless somebody points a review at it.

- Matt takes commits in order, oldest unreviewed first, one at a time.
- Each review names the exact commit whose diff was inspected.
- A later documentation commit does not stand in for the implementation commit beneath it. If two
  land back to back, both still need reviewing, in order.
- The coverage table in `REVIEW_LEDGER.md` is the record. It is maintained by hand, which is why it
  is the authority rather than the prose around it.

Never write "verified" or "closed" from a review report alone. A report must name the exact commit
reviewed, and Matt must confirm acceptance. That rule exists because the record twice claimed a
verification that had not happened.

## Carried forward

Open items the next scope decision should see. Fuller versions live in the milestone-005 archive and
`ROADMAP.md`'s technical-debt list.

- **The concealment MVP rule is a placeholder, not a permanent design.** One attempt per character
  per incident, across running and completed states, is the smallest rule that terminates the
  restart loop. The eventual shape is likely incident-relative state about evidence and exposure in
  the world rather than a private per-character tally.
- **`ConcealIncident`'s termination is proven only by dedicated tests, not by the accepted
  scenario.** At seed 42 the candidate is considered but never wins the utility competition in any
  variant, so the redundancy rule never fires in play. A scenario change that makes concealment win
  would exercise it — the same category of gap milestone 004 left for its own central distinction.
- **The scenario does not exercise milestone 004's central distinction.** No variant contradicts a
  delegator's first-hand account, so the difference between authored participation and being told is
  provable in unit tests and invisible in play. A variant where Tommy denies to Vincent that he
  touched the place would exercise it.
- **Tuning guesses**: the `FirstHandTestimony` suspicion discount of `0.15` and the `Discovery`
  discount of `0.10` are not derived figures.

## Longer-standing deferrals

- relationship-schema design, likely the next substantial design pass;
- relevance tiering and its continuous-calendar engineering risk;
- persistence and SQLite;
- Godot / `net10.0` compatibility;
- generalized rumor, evidence, prosecution, media, and public-information channels;
- broader organizations, diplomacy, careers, corruption, and surveillance systems;
- the redundant test-project target framework, unless Matt separately authorizes a
  documentation/maintenance change.
