# Current Milestone

Mutable active handoff. Detailed implementation/correction history belongs in the milestone archive;
review coverage belongs in REVIEW_LEDGER.

## Status

**Milestone 029 — Grouped job-and-method attention — is implemented; awaiting independent exact-
commit Class A review and Matt acceptance. No subsequent milestone is authorized.**

Matt authorized the bounded production scope on 2026-09-16 after three held experiments. The
focused implementation and verification evidence are archived in
`milestones/029-grouped-job-and-method-attention.md`. The held grouped experiment remains intact as
evidence and was not promoted as production code.

**Milestone 028 — Delegation creates bandwidth — is closed and accepted.** Matt accepted Claude's
independent Class A review of exact `663ba68c594179e7a9e4f9c1a5ce19347d90a56c` (**PASS AFTER FIXES**,
one P2 documentation finding) and closed the milestone on 2026-09-14.

`3db0cb1ffba07292f1c89ebc313ed46275276e24` remains the historical Class A **FAIL** — three P1 and two
P2 — corrected by `663ba68`. Closure does not convert it into a PASS, and every earlier FAIL stands.

**Milestone 027 remains closed and accepted** on its own earlier owner exception. That gate is not
reopened.

## Current gate

**Stop.** The implementation is complete and its full gate is green: build 0 warnings/0 errors,
766 tests passed, the three required deterministic verification variants and their repeats passed,
all six chosen-action histories and coarse outcomes match the accepted baseline, both viewpoint runs
passed, and the authorized evaluation-hash changes are recorded in the archive. Do not alter the
implementation, push it, or begin later work before independent exact-commit review and Matt's
ruling.

## Deliberately carried work

- Layer 2's reason to prefer one day's plan remains unproved. Milestone 029 tests option
  representation only; proposed milestone-030 work remains unauthorized.
- **No information-triggered supervisory review fires naturally in any of the six variants.** The
  mechanism is correct and pinned, but demonstrated only through controlled production-path tests and
  fault probes. Recorded as an honest non-result in the 028 archive; nothing is to be tuned to make it
  fire.
- The provisional seven-day review interval is an implementation timing, not a durable design ruling.
- Rich progress requests/reports, delayed commands/refusal, skill-based supervision caps, and new
  failure penalties remain deferred. Existing attribution and information channels remain.
- Finished-operation history, clock presentation, adjustable speed and pacing remain separate work.
- `Rng.ForDecision`, arrest, territory, money sinks, succession, rival content and other layer-three
  systems remain out of scope.
- OPEN_CONCERNS' bounded attention, information topology and deferred tuning risks remain open.
- Stale ROADMAP labels for milestones 011 and 025, and the pre-existing uncommitted roadmap/proposal
  edits in the working tree, were deliberately not touched by this correction. REVIEW_LEDGER remains
  authoritative for review coverage.
- No Godot self-test was independently rerun in either milestone-028 review round; all ten headless
  results remain implementer-reported.
