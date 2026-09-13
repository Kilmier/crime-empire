# Current Milestone

Mutable and intentionally short. This is the sole active handoff surface; completed history belongs
in `docs/milestones/`, review coverage in `REVIEW_LEDGER.md`, and future candidates in `ROADMAP.md`.

## Status

**No gameplay milestone is active.** Milestone 027 — The session has an ending — was implemented at
exact commit `64881a162581b366314ace043c0a866ef0ddb7c3`. Fresh Claude Opus 5 independently
reviewed it as Class A and returned **FAIL**, one P2 record finding and no blocking runtime defect.
Matt accepted P2-1 on 2026-09-12.

This focused correction records the previously accepted independent PASS of authorization transition
`608f05ef0e67149ba64a6ec50585fd5df6a30506` in `REVIEW_LEDGER.md`, adds `64881a1`'s FAIL and
owner ruling to the ordered range, updates the independent verification baseline, and appends the
correction to the milestone archive. Runtime code and tests are untouched. The correction commit
containing this file is itself unreviewed and unaccepted.

## Current gate

Stop for independent exact-commit review of this Milestone 027 record correction. Do not begin layer
2 or infer a next milestone from `ROADMAP.md`; no later gameplay work is authorized.

## Deliberately carried work

- Finished-operation history remains deferred to the legibility backlog.
- Clock animation, adjustable speed, “time passed” presentation, and pacing remain separate work.
- `Rng.ForDecision`, arrest, territory, money sinks, succession, rival content, and the other
  explicitly deferred layer-three systems remain out of scope.
