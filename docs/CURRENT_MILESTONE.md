# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**No milestone is active. Do not start one.**

Milestones 001–004 are complete and accepted. The most recent, **004 — Provenance Precision**, was
accepted at `1fe8a15` on 2026-08-14 after four corrective rounds; its full record, including every
finding and every correction, is in `docs/milestones/004-provenance-precision.md`.

Every commit carrying code has been reviewed and accepted. `CANONICAL_CODE_REVIEW_CONTEXT.md`'s
review-coverage section is the authority on what has been looked at and what it concluded; do not
infer status from prose anywhere else, including this file.

**Milestone 005 has not been chosen.** Do not infer it from the candidate list in
`CANONICAL_DESIGN_CONTEXT.md`, from the carried-forward items below, or from the technical-debt list
in the code brief. Confirm scope with Matt and write it here before changing simulation behaviour.

## Ordered review process

Review is **manual**. There is no monitor running on a timer, no checkpoint the repository keeps for
itself, and nothing that will notice a commit unless somebody points a review at it.

- Matt takes commits in order, oldest unreviewed first, one at a time.
- Each review names the exact commit whose diff was inspected.
- A later documentation commit does not stand in for the implementation commit beneath it. If two
  land back to back, both still need reviewing, in order.
- The coverage table in `CANONICAL_CODE_REVIEW_CONTEXT.md` is the record. It is maintained by hand,
  which is why it is the authority rather than the prose around it.

Never write "verified" or "closed" from a review report alone. A report must name the exact commit
reviewed, and Matt must confirm acceptance. That rule exists because the record twice claimed a
verification that had not happened.

## Carried forward

Open items the next scope decision should see. Fuller versions live in the milestone-004 archive and
the code brief's technical-debt list.

- **The scenario does not exercise milestone 004's central distinction.** No variant contradicts a
  delegator's first-hand account, so the difference between authored participation and being told is
  provable in unit tests and invisible in play. A variant where Tommy denies to Vincent that he
  touched the place would exercise it.
- **RNG keying, and the latent `ConcealIncident` runaway.** Observation rolls are seeded from global
  event IDs, so adding or removing any event anywhere re-rolls every character's perception. That is
  what silenced the concealment loop rather than any repair; it returns whenever those rolls land
  again. Worth its own scope.
- **Tuning guesses**: the `FirstHandTestimony` suspicion discount of `0.15` and the `Discovery`
  discount of `0.10` are not derived figures.

## Longer-standing deferrals

- relationship-schema design, likely the next substantial design pass;
- relevance tiering and its continuous-calendar engineering risk;
- persistence and SQLite;
- Godot / `net10.0` compatibility;
- generalized rumor, evidence, prosecution, media, and public-information channels;
- broader organizations, diplomacy, careers, corruption, and surveillance systems;
- cleanup of stale `OPEN_CONCERNS.md` item 4 and the redundant test-project target framework,
  unless Matt separately authorizes a documentation/maintenance change.
