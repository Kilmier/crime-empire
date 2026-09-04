# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**Nothing is active.** Confirm scope with Matt before starting anything — including milestone 021 —
rather than inferring the next milestone from `ROADMAP.md` or from what was deferred below.

**Milestone 020 — The Right Person for the Job — is accepted and closed at `c25129a`.** Vincent
gained a second organisational subordinate in one bounded variant (`capable-angelo`: Angelo Conti,
Coercion 0.80 against Tommy's 0.55, trusted at 0.35 against Tommy's 0.70), so
`Generators.FromRelationship`'s delegation choice is genuinely comparative rather than a foregone pick
of the single highest-trust subordinate: one `DelegateStrategy` candidate per subordinate, one new
"executor capability" score component emitted only where there is a real choice, and
`Strategies.ResolveViolence`'s force outcome scaled by the executor's own Coercion instead of a flat
constant. All five pre-existing variants' trace hashes and chosen-action digests are byte-identical
to milestone 019's accepted figures.

**Two Codex rounds, two FAILs, both a P1, both the same class of defect in the same generator.**
`f468e19` scored the executor-capability component from `world.Get(id).Capabilities[Skill.Coercion]`
— the objective figure, which a character does not hold; correction `436f6c7` replaced it with
`Relations.AssessedCoercion`, a new actor-held relationship dimension alongside `Trust`/`Obligation`/
`Fear`, seeded to the same numbers so nothing accepted moved. Codex then found that the same generator
was still choosing *who to offer* out of `ctx.SubordinateIds`, the raw authority scan, which
`Acquaintance.KnownTo` and `DESIGN_DECISIONS.md` both forbid as a source of candidate targets;
correction `34cd117` filters through `ctx.AcquaintedIds` and computes the comparative gate from the
filtered set, and also closed two P2s (a staged unknown-subordinate proof, and `AssessedCoercion`
missing from both `SimulationReplayTests` comparators). `c25129a` records `34cd117`'s own hash.

**Accepted without a Codex round on either correction-2 commit, because Codex ran out of usage.**
Matt accepted and closed on 2026-09-04. This is a weaker basis than milestone 019's close and is
recorded as such rather than smoothed over: `34cd117` is now the oldest unreviewed commit in the
repository, both prior Codex rounds on this milestone found a P1, and the second P1 was invisible
until the first was fixed. What stands behind the accepted state independently of the author's own
reading is three mutation checks, 578 passing tests, and every hash confirmed unmoved by measurement.
Full accounting, including what a future review should look at first:
`docs/REVIEW_LEDGER.md` §"Measured — milestone 020". Full implementation account and both
corrections: `docs/milestones/020-the-right-person-for-the-job.md`.

Milestones 001–020 are all complete and accepted; see their own archives and `REVIEW_LEDGER.md` for
the corrected acceptance record of 015, 016, 018, 019, and 020 specifically.

**Codex is out of usage as of 2026-09-04**, so Claude implements and reviews its own work until that
changes — see `REVIEW_LEDGER.md` §"From milestone 010 onward, review is self-assessment", including
the 2026-09-04 note at its head.

## What is deferred, for whoever scopes the next milestone

Not authorization to start any of it — see `ROADMAP.md`, which is where scope is proposed from.

**Carried from milestone 020, per its own exclusions** (`ROADMAP.md`'s narrowed "Executor
suitability/capability" entry): Persuasion's effect on tribute success; crew size, equipment,
preparation; recruitment, roster, payroll, or resource transfer from owner to delegate; personnel
management generally; escalation-capability ownership as a general rule beyond the one mechanism
milestone 020 touched; a third or later subordinate; a general suitability model across strategy
kinds; capability affecting anything beyond force resolution.

**New, surfaced by milestone 020's own corrections and not addressed by them:**
`Relations.AssessedCoercion` is written only by scenario construction and never revised — a character
cannot learn that the man he thought was useful is not, or the reverse. That is the same shape as the
long-standing "obligation is read but never moves" debt, one dimension over, and it is what would make
executor suitability a live belief rather than a fixed one. Nothing authorizes it.

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
