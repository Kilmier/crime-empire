# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**No milestone is active.**

- **Milestone 003 — closed.** Codex reviewed `d685015` with no findings and Matt accepted the
  correction on 2026-08-14. The record, including two occasions on which the archive claimed a
  verification that had not happened, is preserved in
  `docs/milestones/003-information-transmission.md`.
- **Milestone 004 — three times corrected, the third awaiting review.** `714fbc3` rejected on three
  P1; `c828bfa` rejected on three P1 and two P2, chiefly a false denial transmitting the sender's
  private basis; `d783745` rejected on a silent `ActualBasis` default that marked honest briefings
  as misrepresented, and a repeat comparison that collapsed Participant onto Witness. This
  correction fixes both and is **awaiting review**. Not verified or accepted. Every finding and fix
  is recorded in `docs/milestones/004-provenance-precision.md`.

Milestone 003 being closed does not make the working tree accepted. Its correction was delivered on
top of `714fbc3`, whose rejection is now twice corrected but not yet accepted.

`CANONICAL_CODE_REVIEW_CONTEXT.md`'s review-coverage section is the authority on what has been
looked at and what it concluded; do not infer status from the prose in any other file, including
this one.

**Next step is review of the third milestone-004 correction.** Not new work chosen from the
candidate list.

Do not infer milestone 005 from the candidate list in `CANONICAL_DESIGN_CONTEXT.md` or from the
deferred items below. Confirm scope with Matt and write it here before changing simulation
behaviour.

## Ordered review automation

The Codex monitor keeps an explicit reviewed-commit checkpoint. On each clean-tree run it enumerates
every later commit oldest-first, reviews only the oldest unseen commit, reports that exact hash, and
then advances the checkpoint to it. A later documentation commit therefore cannot hide an earlier
implementation commit. If the checkpoint is no longer an ancestor of `HEAD`, the monitor reports
the divergence instead of guessing at coverage. Non-`HEAD` commits are verified from an isolated
temporary copy; the main working tree is never switched or modified.

This removes the old requirement to pause between an implementation commit and its documentation
commit merely to keep the first one visible. It does not change the acceptance rule: never write
"verified" from a review report alone. A report must identify the exact commit actually reviewed,
and Matt must confirm acceptance before the repository calls it verified or closed.

## Carried forward from milestone 004

Recorded here because they came out of the work rather than from the plan, and the next scope
decision should see them:

- **The scenario does not exercise the distinction milestone 004 drew.** Provenance now separates
  authored participation from first-hand testimony, and the erosion rules read that correctly, but
  no current variant contradicts a delegator's first-hand account — so the difference is provable in
  unit tests and invisible in play. A variant where Tommy denies to Vincent that he touched the
  place would exercise it.
- **The `ConcealIncident` runaway is latent, not fixed.** `disloyal-vincent` used to choose
  `began ConcealIncident(...)` a dozen times, restarting rather than continuing, with an empty
  domain in the label. It no longer occurs, and the reason is not that anything was repaired.

  Observation rolls are keyed from global event IDs — `Rng.ForDecision(seed, observerId, 5000 + ev.Id)`.
  Removing the synthetic information events shifted every later event ID, which shifted Tommy's
  observation seeds, and his police-interest rolls now all miss. He never comes to believe he is
  being looked at, so the legal-exposure pressure driving the loop never rises. It will come back
  the moment those rolls land again.

  The deeper defect is the keying itself: a per-character RNG stream keyed off a global counter
  means an unrelated change anywhere silently re-rolls everybody's perception. That is a
  determinism-hygiene problem worth its own scope, and deliberately not folded into a provenance
  correction.
- The `FirstHandTestimony` suspicion discount of `0.15` is a tuning guess, not a derived figure.

## Longer-standing deferrals

- relationship-schema design, likely the next substantial design pass;
- relevance tiering and its continuous-calendar engineering risk;
- persistence and SQLite;
- Godot / `net10.0` compatibility;
- generalized rumor, evidence, prosecution, media, and public-information channels;
- broader organizations, diplomacy, careers, corruption, and surveillance systems;
- cleanup of stale `OPEN_CONCERNS.md` item 4 and the redundant test-project target framework,
  unless Matt separately authorizes a documentation/maintenance change.
