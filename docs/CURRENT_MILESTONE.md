# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**Nothing is active.** Confirm scope with Matt before starting anything — including milestone 017 —
rather than inferring the next milestone from `ROADMAP.md` or from what was deferred below.

**Milestone 016 — Trust Can Be Earned — is complete, corrected twice, still awaiting a Codex pass
that finds nothing further and Matt's acceptance.** Implemented 2026-08-23, commit `66917c7`;
reviewed by Codex the same day (four findings, none behavioural: a missing `DESIGN_DECISIONS.md`
entry, a dedicated-coefficient test that did not actually discriminate between two equal-valued
constants, an archive claim about where the planning rulings live that the commit's actual diff did
not support, and a doc comment that misattributed why provenance is unweighted) and corrected in
`380a241`. That correction's own fix to the coefficient-test finding introduced a new one — a plain
mutable `public static` field is process-global state with no persistence or replay story — which
Codex found on review and which is corrected a second time in the commit that follows `380a241`: the
field is `static readonly` again, and the test now proves which field
`Relations.RecordAccountAgreement` reads by walking its compiled IL directly rather than by varying
a runtime value. See `docs/milestones/016-trust-can-be-earned.md` for the full account, including
both appended corrections — a perceived account agreement raises the listener's trust toward the
speaker, the mirror image of milestone 006's account-conflict consequence, reusing
`Cognition.Receive`'s existing fresh-agreement branch. `docs/REVIEW_LEDGER.md`'s "Measured —
milestone 016" section has the verification baselines, including the exact accounting of which trace
hashes moved and why.

Milestones 001–016 are implemented; 001–015 are accepted (015 as corrected twice by `bc79425`,
accepted 2026-08-23). Milestone 016 has had two Codex reviews and two corrections so far — Matt has
not yet confirmed acceptance of any named commit for it.

**Codex is intermittent rather than withdrawn.** Claude implements and reviews its own work in the
meantime — see `REVIEW_LEDGER.md` §"From milestone 010 onward, review is self-assessment".

## What is deferred, for whoever scopes the next milestone

Not authorization to start any of it — see `ROADMAP.md`, which is where scope is proposed from, and
`docs/milestones/016-trust-can-be-earned.md`'s "Deferred work" section. In brief, unchanged by this
milestone: **124 live-edge findings and 5 apparently-dead lines** (`docs/COVERAGE_ACCOUNTING.md`);
systematic mutation automation and seed-sweep promotion; a queryable decision-trace store
(`ROADMAP.md` candidate 3's original framing, narrowed by milestone 015 to a replay log); the
allegation option naming the same person twice; the developer trace's uniform "he"; nobody holding a
scored relationship with Kane; the tuning guesses; the cast ceiling of six; obligation read but never
moved; slot management, autosave, cloud save, a save-browser UI, and cross-build save migrations
(milestone 015's exclusions). From `docs/OPEN_CONCERNS.md` #3, still open after this milestone: decay
and its rate, negative trust, whether respect/resentment are separate dimensions, whether provenance
should weight the social consequence, and whether `GrievanceWeight` should be capped.
