# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**Nothing is active.** Confirm scope with Matt before starting anything — including milestone 017 —
rather than inferring the next milestone from `ROADMAP.md` or from what was deferred below.

**Milestone 016 — Trust Can Be Earned — is accepted.** Matt confirmed acceptance of correction
commit `809fe60` on 2026-08-23 and closed the milestone, on the strength of an independent review
(the `implementation-fidelity-reviewer` agent, standing in for Codex) that verified the commit
against the repository directly — clean build, all 505 tests, all three `--verify` runs and
`--compare` reproducing the archive's own recorded hashes exactly, both required viewpoint runs, and
a live reflection probe confirming `AccountAgreementTrustGain` genuinely rejects
`FieldInfo.SetValue` — rather than being taken on the corrective commit's own word. Two informational
notes were raised, neither a defect: `static readonly`'s cross-assembly behaviour differs from
`const` in a direction that favours correctness, and `docs/RELATIONSHIPS.md` still calls the field a
"constant" in prose, pre-existing and unrelated to this commit. No commit was made solely to record
this acceptance; see `docs/REVIEW_LEDGER.md`'s updated "Measured — milestone 016" section for the
full account.

Milestones 001–016 are now all complete and accepted — 015 as corrected twice by `bc79425`, accepted
2026-08-23; 016 as corrected twice by `809fe60`, accepted 2026-08-23.

**Codex is intermittent rather than withdrawn.** Claude implements and reviews its own work in the
meantime — see `REVIEW_LEDGER.md` §"From milestone 010 onward, review is self-assessment".

## What is deferred, for whoever scopes the next milestone

Not authorization to start any of it — see `ROADMAP.md`, which is where scope is proposed from, and
`docs/milestones/016-trust-can-be-earned.md`'s "Deferred work" section for the full, current list. In
brief: **124 live-edge findings and 5 apparently-dead lines** (`docs/COVERAGE_ACCOUNTING.md`);
systematic mutation automation and seed-sweep promotion; a queryable decision-trace store
(`ROADMAP.md` candidate 3's original framing, narrowed by milestone 015 to a replay log); the
allegation option naming the same person twice; the developer trace's uniform "he"; nobody holding a
scored relationship with Kane; the tuning guesses; the cast ceiling of six; obligation read but never
moved; slot management, autosave, cloud save, a save-browser UI, and cross-build save migrations
(milestone 015's exclusions). From `docs/OPEN_CONCERNS.md` #3, still open after milestone 016: decay
and its rate, negative trust, whether respect/resentment are separate dimensions, whether provenance
should weight the social consequence, and whether `GrievanceWeight` should be capped.
