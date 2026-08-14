# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**No milestone is active.**

Milestone 004 (Provenance Precision) is complete and awaiting Codex review. Its record is in
`docs/milestones/004-provenance-precision.md`.

Do not infer milestone 005 from the candidate list in `CANONICAL_DESIGN_CONTEXT.md` or from the
deferred items below. Confirm scope with Matt and write it here before changing simulation
behaviour.

## Carried forward from milestone 004

Recorded here because they came out of the work rather than from the plan, and the next scope
decision should see them:

- **The scenario does not exercise the distinction milestone 004 drew.** Provenance now separates
  authored participation from first-hand testimony, and the erosion rules read that correctly, but
  no current variant contradicts a delegator's first-hand account — so the difference is provable in
  unit tests and invisible in play. A variant where Tommy denies to Vincent that he touched the
  place would exercise it.
- **Possible pre-existing runaway in `disloyal-vincent`**: `began ConcealIncident(...)` is chosen
  around fifteen times, restarting rather than continuing, with an empty domain in the label. It is
  identical before and after milestone 004, so it is not a regression from that work. It resembles
  the corroboration runaway fixed in `f97ef76` and deserves its own look.
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
