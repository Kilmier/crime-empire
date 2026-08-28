# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**Nothing is active.** Confirm scope with Matt before starting anything — including milestone 021 —
rather than inferring the next milestone from `ROADMAP.md` or from what was deferred below.

**Milestone 020 — The Right Person for the Job — corrected once, awaiting Codex re-review.** Gave
Vincent a second organisational subordinate (the `capable-angelo` variant: Angelo Conti, Coercion
0.80, trusted at 0.35, against Tommy's 0.55/0.70) so `Generators.FromRelationship`'s delegation
choice is genuinely comparative rather than a foregone pick of the single highest-trust subordinate.
`Utility` gained one new "executor capability" score component, present only when there is a real
choice among two or more subordinates and therefore never emitted for any existing accepted variant;
`Strategies.ResolveViolence` now scales its force outcome by the actual executor's Coercion,
calibrated at Tommy's own stat (0.55 — the only value that call has ever been exercised against in an
accepted run) so no existing trace hash moved.

Codex reviewed the original implementation (`f468e19`) and returned **FAIL** (one P1, no P2s): the
delegation-scoring "executor capability" component read each subordinate's exact
`Capabilities[Skill.Coercion]` straight off `World` — `Pipeline.SubordinatesOf` reading the roster to
learn *who* Vincent's subordinates are is a settled, legitimate authority scan, but *how good* each
one is at the job is a fact about that person, not the org chart, and nothing licensed reading it past
the belief limit every other score component in `Decision/Utility.cs` already respects (that file's
own header: scoring "receives a `PerceivedSituation` and never a `World`"). The tests pinned the
omniscient read rather than disproving it.

**Correction (uncommitted at time of writing, applied in the same session).** A new
`Relations.AssessedCoercion` dimension on `IRelationship`, alongside `Trust`/`Obligation`/`Fear` —
Vincent's own held belief about each subordinate's Coercion, distinct from that subordinate's actual
`Capabilities[Skill.Coercion]`. Set at scenario construction to match the pre-correction figures
exactly (Tommy 0.55 in `Cast.Build`, Angelo 0.80 in `Variants.Apply`), so every accepted trace hash,
the `capable-angelo` hash, and the natural run's own preference are all unmoved — confirmed via
`--verify`/`--compare` rather than assumed. `Generators.FromRelationship` now reads
`ctx.Actor.Social.Toward(sub).AssessedCoercion` — the actor's own relationship record, the same
non-creating channel `Utility.Loyalty` already reads — instead of a `GeneratorContext.SubordinateCoercion`
dictionary `Pipeline.Prepare` used to build from `World`; that dictionary and field are gone entirely.
`Strategies.ResolveViolence` is untouched: force resolution still reads the executor's real,
objective Coercion, which the correction's scope explicitly kept on the World side of the line.

Four new tests in `ExecutorSuitabilityTests.cs` prove the assessment, not the objective figure, is
what scoring reads: changing only Angelo's hidden actual capability leaves the fork's candidates,
totals and preference unchanged; changing only Vincent's assessment moves the "executor capability"
component and flips which of the two delegate candidates he'd prefer between them; objective executor
capability still (unchanged) drives the committed force outcome; and a missing assessment produces
no "executor capability" term at all — `Candidate.ExecutorCoercion` is null, never a silent fallback
to the objective figure and never a silent zero. A fifth, temporary mutation check reintroduced the
flagged regression (scoped to Vincent only, to avoid disturbing an unrelated acquaintance-boundary
test elsewhere that a naive reintroduction would have side-effected) and confirmed exactly the three
new comparison tests fail, for the stated reason, before being reverted.

Full verification after the correction: build 0/0, tests 576/576 (573 + 3 new, all in
`ExecutorSuitabilityTests.cs`, which now has 12);
`--verify` baseline/`capable-angelo`/`disloyal-vincent`/`resentful-tommy` all deterministic and
byte-identical to their previously accepted hashes; `--compare` at seed 42: all six configurations'
trace hashes and chosen-action digests unmoved (`baseline 9AF57665067AEA11`, `cautious-vincent
86EC1ADA4A4E9179`, `watchful-boss 84AC3F65E4102EBA`, `disloyal-vincent 9A6E0E518294532F`,
`resentful-tommy 3C4483640153DA88`, `capable-angelo 2060465B4F31E6DD`/`CD9A30C1CD408F1D`); all three
required viewpoint runs (`disloyal-vincent`/`salvatore`, `baseline`/`vincent`,
`capable-angelo`/`salvatore`) exit 0. Godot headless self-tests — `--selftest`,
`--selftest-goldenpath`, `--selftest-directaction`, `--selftest-corroboration`, `--selftest-tribute`
— and the two-process restart proof (`--selftest-restart-save`/`--selftest-restart-load`) all ran
this time (a Godot executable was found in this environment after all, at
`Godot_v4.7.1-stable_mono_win64_console.exe` under the user profile — the original milestone's
archive recorded none being available, which was an incomplete search rather than a genuine absence)
and all passed, unchanged.

Full account, including both flagged judgment calls from the original implementation and this
correction in full: `docs/milestones/020-the-right-person-for-the-job.md`.

Milestones 001–019 are all complete and accepted; see their own archives and `REVIEW_LEDGER.md` for
the corrected acceptance record of 015, 016, 018, and 019 specifically.

**Codex is intermittent rather than withdrawn.** Claude implements and reviews its own work in the
meantime — see `REVIEW_LEDGER.md` §"From milestone 010 onward, review is self-assessment".

## What is deferred, for whoever scopes the next milestone

Not authorization to start any of it — see `ROADMAP.md`, which is where scope is proposed from.

Carried from milestone 020, per its own exclusions (`ROADMAP.md`'s narrowed "Executor
suitability/capability" entry): Persuasion's effect on tribute success; crew size, equipment,
preparation; recruitment, roster, payroll, or resource transfer from owner to delegate; personnel
management generally; escalation-capability ownership as a general rule beyond the one mechanism
milestone 020 touched; a third or later subordinate; a general suitability model across strategy
kinds; capability affecting anything beyond force resolution.

Carried from milestone 017 and earlier, still unresolved: the five-column layout and other
playtest-discovered presentation debt (milestone 018); the wrapped-date/toolbar debt; a
`PlayerNarration` prose rewrite; "You control"/"You see through" unification; territory, patrol,
weekly planning; additional businesses or operations; an eighth character; employee-stat displays or
new UI panels; the known pause-timing information leak; new organizations, careers, or alternate
playable roles.

Carried from milestone 016 and earlier, still unresolved: **124 live-edge findings and 5
apparently-dead lines** (`docs/COVERAGE_ACCOUNTING.md`); systematic mutation automation and
seed-sweep promotion; a queryable decision-trace store; the allegation option naming the same person
twice; the developer trace's uniform "he"; nobody holding a scored relationship with Kane; the tuning
guesses; obligation read but never moved; slot management, autosave, cloud save, a save-browser UI,
and cross-build save migrations. From `docs/OPEN_CONCERNS.md` #3, still open: decay and its rate,
negative trust, whether respect/resentment are separate dimensions, whether provenance should weight
the social consequence, and whether `GrievanceWeight` should be capped.
