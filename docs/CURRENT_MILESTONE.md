# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**Nothing is active.** Confirm scope with Matt before starting anything — including milestone 021 —
rather than inferring the next milestone from `ROADMAP.md` or from what was deferred below.

**Milestone 020 — The Right Person for the Job — corrected twice, awaiting Codex re-review.** Gave
Vincent a second organisational subordinate (the `capable-angelo` variant: Angelo Conti, Coercion
0.80, trusted at 0.35, against Tommy's 0.55/0.70) so `Generators.FromRelationship`'s delegation
choice is genuinely comparative rather than a foregone pick of the single highest-trust subordinate.
`Utility` gained one new "executor capability" score component, present only when there is a real
choice among two or more subordinates and therefore never emitted for any existing accepted variant;
`Strategies.ResolveViolence` now scales its force outcome by the actual executor's Coercion,
calibrated at Tommy's own stat (0.55 — the only value that call has ever been exercised against in an
accepted run) so no existing trace hash moved.

**Correction 1 (`436f6c7`, committed).** Codex reviewed the original implementation (`f468e19`) and
returned **FAIL** (one P1, no P2s): the delegation-scoring "executor capability" component read each
subordinate's exact `Capabilities[Skill.Coercion]` straight off `World` — `Pipeline.SubordinatesOf`
reading the roster to learn *who* Vincent's subordinates are is a settled, legitimate authority scan,
but *how good* each one is at the job is a fact about that person, not the org chart, and nothing
licensed reading it past the belief limit every other score component in `Decision/Utility.cs`
already respects. The correction added `Relations.AssessedCoercion`, an actor-held relationship
dimension alongside `Trust`/`Obligation`/`Fear`, set at scenario construction to match the
pre-correction figures exactly (Tommy 0.55, Angelo 0.80) so every accepted trace hash and the natural
run's own preference stayed unmoved; `Generators.FromRelationship` reads it via
`ctx.Actor.Social.Toward(sub).AssessedCoercion` instead of a `World`-sourced dictionary, which is
gone. Three new tests in `ExecutorSuitabilityTests.cs` (9 → 12) proved the assessment, not the
objective figure, drives scoring, and that a missing assessment neither falls back to the objective
figure nor reads as zero.

**Correction 2 (`34cd117`, committed).** Codex reviewed `436f6c7` and returned **FAIL** again: one P1, two
P2s.

- **P1.** `FromRelationship`'s delegation loop iterated `ctx.SubordinateIds` directly —
  `Pipeline.SubordinatesOf`'s own authority scan, legitimate for identity but, per
  `DESIGN_DECISIONS.md`'s settled "an office relationship is only an office relationship if it comes
  from an office" ruling, not itself grounds to treat a subordinate as somebody the actor could name.
  Fixed by filtering `SubordinateIds` through `ctx.AcquaintedIds` before generating any delegate
  candidate, and computing the "genuine choice" `comparative` flag from the filtered (nameable) set
  rather than the raw organisational count. A no-op for every accepted variant, since Cast.Build and
  Variants.Apply already establish a relationship between Vincent and each real subordinate.
- **P2.** Added the staged unknown-subordinate test (an organisationally-subordinate, wholly
  unacquainted stranger is never offered as a delegate), its positive acquaintance control (a
  genuinely acquainted subordinate still is, and the "genuine choice" gate reads the filtered set),
  and a mutation check confirming both collapse together when the filter is removed.
- **P2.** `AssessedCoercion` was missing from both of `SimulationReplayTests.cs`'s relationship
  comparator fingerprints (`Snapshot` and `BehavioralSnapshot`), so a defect that corrupted it between
  two otherwise-identical runs would have gone undetected. Added to both, formatted to distinguish
  null (no assessment) from `0.0` (assessed at the floor); a new test perturbs nothing but this one
  dimension and confirms both comparators catch it, independently verified per-comparator by
  mutation.

Full verification after correction 2: build 0/0, tests 578/578 (576 + 2 new — the acquaintance-
boundary test in `ExecutorSuitabilityTests.cs` and the comparator test in `SimulationReplayTests.cs`);
`--verify` baseline/`capable-angelo`/`disloyal-vincent`/`resentful-tommy` all deterministic and
byte-identical to their previously accepted hashes; `--compare` at seed 42: all six configurations'
trace hashes and chosen-action digests unmoved (`baseline 9AF57665067AEA11`, `cautious-vincent
86EC1ADA4A4E9179`, `watchful-boss 84AC3F65E4102EBA`, `disloyal-vincent 9A6E0E518294532F`,
`resentful-tommy 3C4483640153DA88`, `capable-angelo 2060465B4F31E6DD`/`CD9A30C1CD408F1D`); all three
required viewpoint runs exit 0; Godot headless self-tests (`--selftest`, `--selftest-goldenpath`,
`--selftest-directaction`, `--selftest-corroboration`, `--selftest-tribute`) and the two-process
restart proof (`--selftest-restart-save`/`--selftest-restart-load`) all pass, unchanged — a Godot
executable was in fact found in this environment (`Godot_v4.7.1-stable_mono_win64_console.exe` under
the user profile), so correction 1's "none was available" note reflected an incomplete search, not a
genuine absence.

Full account, including both flagged judgment calls from the original implementation and both
corrections in full: `docs/milestones/020-the-right-person-for-the-job.md`.

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
