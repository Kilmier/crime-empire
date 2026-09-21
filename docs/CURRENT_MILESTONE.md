# Current Milestone

## Status and gate

**Milestone 031 — Commission the Job: accepted P1 corrected; awaiting independent correction review.**
Matt authorized the revised scope after accepting its P1 and five P2 findings and supplying the two
owner rulings. The implementation and verification record is in
[`milestones/031-commission-the-job.md`](milestones/031-commission-the-job.md).

Sol independently reviewed `c279d04a7b65b802d359286cac8806eaf2b0303f` against `ae4e465`:
Class A **FAIL**, one P1 for executor alternatives replacing the selected business target and losing
its knowledge inputs. Matt accepted that finding for correction. The focused correction preserves
operation inputs and carries executor identity separately. Review the correction commit containing
this update against `c279d04`; no independent PASS, owner acceptance of the corrected state, or
milestone closure is asserted. Do not begin another milestone or extend this correction.

## Delivered scope

Direct SecureTribute commissioning uses the existing retained operation leaf, then protected eligible
self plus at most five known eligible direct subordinates. The paused draft is reversible and replayed
through the existing save system. Confirmation validates and commits once through the shared player/
autonomous path. Initial delegation preserves identity, scheduling, responsibility, existing controls
and source-limited briefings. Planning gives a static multi-day expectation; owner status requires an
operation-attributed account, with private executor progress kept private.

Durable owner rulings are recorded in `DESIGN_DECISIONS.md`, "Direct commissioning". The archived
scope and tests are the implementation review target; this short handoff does not add authorization.

## Verification and outstanding human evidence

Correction build passed with no warnings/errors; 809 tests passed; all six seed-42 variants replayed
identically; 24 Godot UI/restart checks passed with the ordinary playtest save unchanged. Restoring
the reviewed target substitution makes the focused regression fail. See the appended archive record
for evidence, hashes and limits. The original 12 mutation probes missed this defect.

Matt reported that initial delegation, staffing availability, income attribution and save/load at
various phases worked, with no duplicated payment and executor/job/method retained. He also raised
briefing, permission-label, report/response and post-completion relevance concerns, and requested
future owner control over a delegate's approach. These observations do not close the human
comprehension gate or authorize those additional changes; the archive preserves the playtest notes.

## Deliberately carried work

- OPEN_CONCERNS #8 has a semantic ruling, but pacing remains provisional. #9 is addressed by the
  implementation candidate and is not closed by implementer tests.
- Milestone 030 remains operationally closed through its owner exception; retrospective independent
  review remains carried. Its pre-existing ledger reference differs from this checkout's history;
  the archive records that limit without revising unrelated review authority.
- New intervention mechanics, delayed commands, new refusal/negotiation, live forecasts and reporting
  vocabulary remain deferred. No tuning, fixture/RNG/timing changes or extra content were authorized.
- Existing ROADMAP/proposal modifications are unrelated user work, left untouched and uncommitted.
- Await independent review and Matt's ruling. Do not push or start the next milestone.
