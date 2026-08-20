# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**Nothing is active.** Confirm scope with Matt before starting anything — including milestone 015 —
rather than inferring the next milestone from `ROADMAP.md` or from what was deferred below.

**Milestone 014 — One Complete Player-Owned Operation — is complete, self-reviewed, not yet
accepted.** Implemented 2026-08-19; see `docs/milestones/014-one-complete-player-owned-operation.md`
for the full account, including a correction to the feasibility pass's own decision count (the
operation naturally produces seven of Vincent's own decisions before the accepted consequence, not
five), and `docs/REVIEW_LEDGER.md`'s "Measured — milestone 014" section for the verification
baselines. Matt has not yet confirmed acceptance of a named commit, and Codex has not yet reviewed
it — see `REVIEW_LEDGER.md`'s "cleared to build on is not accepted".

Milestones 001–013 are complete and accepted — 011 and 012 as corrected by `3c86ba4`, 013 as corrected
twice, most recently by `a75a54e`, accepted 2026-08-19.

**Codex is intermittent rather than withdrawn.** Claude implements and reviews its own work in the
meantime — see `REVIEW_LEDGER.md` §"From milestone 010 onward, review is self-assessment".

## What is deferred, for whoever scopes the next milestone

Not authorization to start any of it — see `ROADMAP.md`, which is where scope is proposed from, and
`docs/milestones/013-coverage-accounting-not-vigilance.md`'s "Deferred work" section for the full,
current list. Milestone 014 touched no simulation behaviour, so it adds nothing new to it. In brief:
**124 live-edge findings and 5 apparently-dead lines**, itemized by region in
`docs/COVERAGE_ACCOUNTING.md`, none triaged by priority or acted on; systematic mutation automation
and seed-sweep promotion, both deliberately deferred as unnumbered `ROADMAP.md` candidates; whether
coverage can be collected over a natural run rather than the test suite, left open at planning time
and not settled; and everything carried into milestone 012 that neither 013 nor 014 touched — the
allegation option naming the same person twice, the developer trace's uniform "he", nobody holding a
scored relationship with Kane, the tuning guesses, the cast ceiling of six, obligation read but never
moved, no save/load, and the rest listed in that archive's own carried-forward section.
