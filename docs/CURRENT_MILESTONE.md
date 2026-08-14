# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**No milestone is active. Do not start one.**

Milestones 001–006 are complete. The most recent, **006 — Relational Consequence of a Perceived
Account Conflict**, gave a contradiction a directional social cost, put every relationship mutation
behind `Domain/Relations.cs`, and brought relationship and grievance state into both replay
comparators. Its full record, including what it found and what it failed to achieve, is in
`docs/milestones/006-relational-consequence.md`.

Milestone 006's implementation commit is **not reviewed and not accepted.** `REVIEW_LEDGER.md` alone
defines review coverage; consult its checkpoint directly rather than inferring status from prose
anywhere else, including this file. Its coverage table and checkpoint were deliberately left
untouched by that commit — advancing a checkpoint is a status claim, and this record has been damaged
before by status claims made in passing. What that commit did add to the ledger is a verification
baseline, explicitly labelled as measured rather than reviewed.

**Milestone 007 has not been chosen.** Do not infer it from the candidate list or technical-debt list
in `ROADMAP.md`, or from the carried-forward items below. Confirm scope with Matt and write it here
before changing simulation behaviour.

## What milestone 006 found, because it should shape the next scope decision

**The scenario is now the binding constraint, not the mechanisms.** Three consecutive milestones have
ended with a correct, mutation-checked mechanism the accepted scenario cannot demonstrate:

- 004's provenance distinction — no variant contradicts a delegator's first-hand account;
- 005's concealment termination — the candidate is considered but never wins;
- 006's trust edge — it *does* fire in every variant, and Salvatore's trust in Vincent really falls
  from 0.50 to 0.309, but he never afterwards scores anything that reads that relationship, so no
  history changes.

One organisation, five people and a single line of causation is running out of room. Adding a fourth
mechanism it also cannot show would be volume rather than progress. `ROADMAP.md` now carries this as
its first candidate scope.

## Carried forward

Open items the next scope decision should see. Fuller versions live in the milestone archives and
`ROADMAP.md`'s technical-debt list.

- **A delegator never receives an account from his own executor.** The only character who puts a
  question is the boss, and being asked redirects the answer to the asker, so a soldier's account
  goes past the capo who sent him. Structural, not a matter of degree. This is why milestone 004's
  central distinction is still unobservable in play and why 006's `resentful-tommy` variant is inert.
- **`resentful-tommy` makes the same decisions as baseline**, so `--compare`'s "five distinct
  histories" is a weaker signal than it reads.
- **Trust cannot go negative.** Absence of trust and distrust are the same state.
- **The concealment MVP rule is a placeholder, not a permanent design**, and its termination is
  proven only by dedicated tests, not by the accepted scenario.
- **Tuning guesses**: the `FirstHandTestimony` suspicion discount of `0.15`, the `Discovery` discount
  of `0.10`, and `Relations.ConflictTrustCost` of `0.35`.
- The empty-domain label, `ConcealIncident(, target=...)`.

## Longer-standing deferrals

- the relationship-design document — **possible milestone 007, not authorized.** `OPEN_CONCERNS.md`
  #3 now records the executable evidence it was always conditioned on, and what a document would
  still have to decide;
- relevance tiering and its continuous-calendar engineering risk;
- persistence and SQLite;
- Godot / `net10.0` compatibility — cheap, gates nothing today, worth a standalone commit rather than
  a milestone;
- generalized rumor, evidence, prosecution, media, and public-information channels;
- broader organizations, diplomacy, careers, corruption, and surveillance systems;
- the redundant test-project target framework, unless Matt separately authorizes a
  documentation/maintenance change.

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
