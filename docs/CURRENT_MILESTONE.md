# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**Nothing is active.** Confirm scope with Matt before starting anything — including milestone 016 —
rather than inferring the next milestone from `ROADMAP.md` or from what was deferred below.

**Milestone 015 — The Operation Survives a Restart — is complete, corrected once, not yet
accepted.** Implemented 2026-08-23, commit `9537b38`; reviewed by Codex the same day (four findings,
two P1 and two P2, all about verification strength rather than the persistence mechanism itself) and
corrected in the commit that follows it. See
`docs/milestones/015-the-operation-survives-a-restart.md` for the full account, including the
appended correction — replay-backed SQLite persistence in a new `src/CrimeEmpire.Persistence`
project, one fixed Godot save slot never reachable from the restart self-tests, and a two-process
restart proof of the existing baseline seed-42 Vincent `SecureTribute` operation, run for real and
recorded in that archive. `docs/REVIEW_LEDGER.md`'s "Measured — milestone 015" section has the
verification baselines.

Milestones 001–015 are implemented; 001–014 are accepted (011 and 012 as corrected by `3c86ba4`, 013
as corrected twice by `a75a54e`, 014 as corrected twice by `ff4213a`, accepted 2026-08-23). Milestone
015 has had one Codex review and one correction so far — Matt has not yet confirmed acceptance of any
named commit for it.

**Codex is intermittent rather than withdrawn.** Claude implements and reviews its own work in the
meantime — see `REVIEW_LEDGER.md` §"From milestone 010 onward, review is self-assessment".

## What is deferred, for whoever scopes the next milestone

Not authorization to start any of it — see `ROADMAP.md`, which is where scope is proposed from, and
`docs/milestones/015-the-operation-survives-a-restart.md`'s "Deferred work" section for the full,
current list. In brief: **124 live-edge findings and 5 apparently-dead lines**, itemized by region in
`docs/COVERAGE_ACCOUNTING.md`, none triaged by priority or acted on; systematic mutation automation
and seed-sweep promotion; the allegation option naming the same person twice; the developer trace's
uniform "he"; nobody holding a scored relationship with Kane; the tuning guesses; the cast ceiling of
six; obligation read but never moved; and, new from milestone 015 and deliberately excluded rather
than accidentally omitted — slot management, autosave, cloud save, a save-browser UI, and cross-build
save migrations. `ROADMAP.md` candidate 3's original framing (decision data worth querying) remains
open; milestone 015 executed the storage-technology half of `DESIGN_DECISIONS.md` §Stack's persistence
entry, not the querying-capability half.
