# Current Milestone

Mutable active handoff. Detailed implementation/correction history belongs in the milestone archive;
review coverage belongs in REVIEW_LEDGER.

## Status

**No milestone is active.**

**Milestone 028 — Delegation creates bandwidth — is closed and accepted.** Matt accepted Claude's
independent Class A review of exact `663ba68c594179e7a9e4f9c1a5ce19347d90a56c` (**PASS AFTER FIXES**,
one P2 documentation finding) and closed the milestone on 2026-09-14.

`3db0cb1ffba07292f1c89ebc313ed46275276e24` remains the historical Class A **FAIL** — three P1 and two
P2 — corrected by `663ba68`. Closure does not convert it into a PASS, and every earlier FAIL stands.

**Milestone 027 remains closed and accepted** on its own earlier owner exception. That gate is not
reopened.

## Current gate

**Stop. Nothing is authorized.** No next milestone has been selected, and no implementation, fixture,
tuning, or runtime change is permitted by anything currently in the repository.

The documentation-only closure commit containing this update carries Matt's explicit bounded owner
exception from a further independent review round — the same instrument recorded for `c7dd34b` and
`303eed0`. It is an owner closure, not an independent PASS, and it cannot record its own review.

Scope for any future milestone comes from Matt and is written into this file before work begins.
`ROADMAP.md`'s candidates and `docs/proposals/` authorize nothing.

## Deliberately carried work

- Layer 2's reason to prefer one day's plan remains unproved. Proposed 029/030 work is not authorized.
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
