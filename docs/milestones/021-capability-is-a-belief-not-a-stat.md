# Milestone 021 — Capability Is a Belief, Not a Stat

Authorized by Matt on 2026-09-04, with all rulings settled the same day. The scope and rulings as
written before implementation are preserved in this commit's prior version of
`docs/CURRENT_MILESTONE.md`.

## What this milestone was for

Milestone 020 introduced `Relations.AssessedCoercion`: what a delegator believed about a
subordinate's Coercion, read by `Utility`'s "executor capability" component. It was **a belief-shaped
field with no belief mechanics** — written only by scenario construction, never revised, carrying no
source and no confidence. And it was in the wrong place. Trust, Fear and Obligation are *attitudes*
with no truth value; there is no fact of the matter about how much Vincent trusts Tommy beyond
Vincent's own state. An assessment of somebody's skill has a referent — that man's actual
`Capabilities[Skill.Coercion]` — so it can be **wrong**, and `AGENTS.md` requires truth, knowledge and
belief be kept distinct.

That this was worth fixing rather than tidying is best argued by milestone 020's own record: three
rounds, three defects, all in the same scoring path, each pinned by a passing test rather than caught
by one. Two were found by Codex, the third while scoping this milestone.

## The rulings

1. **Capability belief moves into `Cognition`** (Matt, option (b) over keeping it as a relationship
   dimension).
2. **The relationship vocabulary does not reopen.** `RELATIONSHIPS.md` is back to four.
3. **`Strategies.ResolveViolence` keeps reading the objective figure** — committed force resolution
   computes what happened; it is not scoring an option.
4. **Only `capable-angelo`'s hashes may move.** The other five byte-identical on trace *and*
   chosen-action digest, as a hard constraint.
5. **Magnitude is which propositions are held — graded threshold claims.** Settled after Matt pushed
   back on whether "confidence" was needed at all, which surfaced that three things were in play and
   had to stay apart:

   | | What it is | Where it lives |
   |---|---|---|
   | Skill | how good the man actually is | `Capabilities[Skill.Coercion]`, in `World`, never read by scoring |
   | Certainty | how sure the delegator is of what he believes | `InformationRecord.Confidence`, per proposition |
   | Self-confidence | how confident the *executor* is in his own ability | does not exist, and was not added — ruling 7 |

   Encoding magnitude as certainty would collapse two distinctions into one number: the defect
   `LoyaltyReading` was unbundled to avoid and `RelationshipFacet` was built to detect.
6. **Revising from confounded evidence is a deliberate attribution error**, and the milestone must
   therefore prove an assessment can end up *further* from the truth than it started.
7. **The executor's own confidence in doing the job is out of scope**, recorded in `ROADMAP.md`
   rather than built — a fourth concept with no decision reading it.

## What was completed

### `Domain/Claim.cs`

`ClaimKind.PersonIsCapable` — subject is a person, object names a bar. Plus `CapabilityBar`: named
constants (`RoughWork`, `HardMan`), an ordered `Ladder`, and an `About(personId, bar)` factory so no
call site invents a bar. The ladder's implication (clearing the high bar implies clearing the low
one) is deliberately **not** enforced on write: a character may hold an incoherent pair, because he
is allowed to be wrong and nothing gets to tidy his beliefs behind his back.

### `Domain/Suitability.cs` (new)

`RecordDelegatedOutcome(owner, executorId, succeeded, at)` — the cognition counterpart of
`Relations.RecordAccountConflict`, and deliberately the same shape: one named consequence, in one
place, taking only what the delegator can perceive. **It takes the executor's id and never his
`Character`**, so the rule structurally cannot reach `Capabilities[Skill.Coercion]` — enforced by the
signature rather than by discipline.

Direction is relative to what he already believes, not to the outcome alone: a success makes him
surer of a bar he holds and *less* sure of one he has rejected. Read the other way, a boss who thinks
his man is no hard man would grow more certain of that every time the man succeeded — a counter, not
a belief. It revises positions he holds and never invents one, a boundary `Cognition.Revise` makes
structural by returning null when there is no such record.

### `Decision/Utility.cs`

The "executor capability" component now reads `PersonIsCapable` positions out of `perceived`, one
component per bar held, each scaled by that belief's own confidence and summed. Tagged
`RelationshipFacet.None` — the tag milestone 020 used, now for the stated reason, since no
relationship state is read. `RelationshipFacet.Capability`, added three days earlier by milestone
020's correction 3, is deleted.

### `Decision/Candidate.cs` and `Decision/Generators.cs`

`ExecutorCoercion` (a `double?`) becomes `ComparingExecutors` (a `bool`). **That the field can no
longer hold a capability value is the point**: a generator can see `World` and the scorer cannot, and
carrying a number across that boundary is what made the same defect available twice. What is left is
only the question generation can answer and scoring cannot — *is there anybody to compare him
against*.

### `Domain/Relations.cs`

`AssessedCoercion`, its backing property, its `Establish` parameter, `SetAssessedCoercion` and its
`ToString` suffix are all gone. A comment records why, against the interface it was removed from.

### `Scenario/Variants.cs`

Vincent's beliefs seeded in the `capable-angelo` case: Angelo clears both bars, Tommy clears the low
one and Vincent has **no view** on whether Tommy is exceptional. `SourceKind.Inference` with Vincent
as his own source — load-bearing, not decoration: `Cognition.Revise` admits only records passing
`IsOwnReading()` that name their holder, so any other source would have made the belief permanently
unrevisable and quietly defeated the milestone.

Nobody is seeded with a `Doubts`/`Rejects` on a bar. Vincent having concluded Tommy is positively not
a hard man would be an opinion the fixture invented for him; the negative branch is proven by a
staged test instead.

**A fixture asymmetry, recorded rather than buried:** the beliefs are seeded only in this variant,
including Tommy's. Ruling 4 requires the other five stay byte-identical, and a belief added to
Vincent's cognition appears in the trace's "what he knew" and would move their hashes for no
behavioural gain — none of them has a second subordinate, so nothing there ever reads one. If
capability beliefs ever come to matter with a single subordinate, this is the first thing to change.

### `Strategy/Strategies.cs`

Two call sites, both already distinguishing owner from executor: the `collected` branch
(`succeeded: true`) and `Blocked` (`succeeded: false`).

## Important discoveries

**Capability beliefs travel through the report and corroboration channels, and this was not
designed.** Putting capability into the ordinary claim vocabulary means the generic generators treat
it like any other claim: in the natural run Vincent asks Angelo for his account of whether Angelo is a
hard man, and Salvatore ends up holding `PersonIsCapable(angelo → rough-work)` "via reported:vincent".

Left in place rather than special-cased out, because the alternative is carving an exception into
generic machinery to serve one claim kind, and because the resulting exchanges are legible — a boss
sounding a man out about whether he is up to the work is a real act, and an answer that is
self-serving by construction is exactly the kind of testimony the model already lets a listener
discount.

**But it has a consequence, and it is recorded in `ROADMAP.md` as debt rather than assumed benign:**
`Cognition.Revise` admits only a holder's own reading, so **a capability belief acquired by testimony
can never be revised.** Salvatore's second-hand view of Angelo is frozen for the rest of the run.
Arguably correct — a man who has never worked with somebody has no grounds of his own to update — but
it is the same "belief with no mechanism to revise it" shape this milestone exists to remove, one
character over. Relaxing the `IsOwnReading` guard is **not** the fix; that guard is milestone 011's,
and loosening it is how the `Provenance` bundle comes back.

**Moving into `Cognition` made the replay comparators cover it for free.** Milestone 020's correction
2 had to add a bespoke `AssessedCoercion` line to both `SimulationReplayTests` fingerprints. As an
ordinary belief it is covered by the `knowledge|` lines both comparators already emit — kind, subject,
object, stance, confidence, source, and the reconsideration stamp revision moves. **A field that needs
the comparator taught about it is a field in the wrong place**, and that is a cheap test for the next
time this question comes up.

**The natural run picks the same man for a better-stated reason.** Vincent still delegates to Angelo,
now on `angelo is a hard man, not just a willing one (+0.19)` rather than a number derived from a
stat. His belief then moves during the run — "plausible" → "uncertain" → "plausible" — as jobs come
back.

## Verification

- Build: **0 warnings, 0 errors**.
- Tests: **588 passed, 0 failed** (579 before this milestone; the file gains the ladder, certainty,
  rejected-bar, no-view, and five revision tests, and loses the two that guarded the removed scalar).
- `--verify` deterministic: `baseline` `9AF57665067AEA11`, `capable-angelo` `6B355EEF852AFA6C`.
- `--compare` at seed 42: **6 configurations · 6 distinct traces · 6 distinct chosen-action
  sequences.** **The five pre-existing variants are byte-identical on both hashes** — ruling 4 met, and
  verified rather than argued: `baseline 9AF57665067AEA11`/`7716CDDE3D0CA3A6`, `cautious-vincent
  86EC1ADA4A4E9179`/`7506045DDEB2DE14`, `watchful-boss 84AC3F65E4102EBA`/`955921AA69ABA44C`,
  `disloyal-vincent 9A6E0E518294532F`/`BECCA9ED2E4E7137`, `resentful-tommy
  3C4483640153DA88`/`B9B6D3BBE6A69200`. `capable-angelo` moves on both, as authorized:
  `35BB0B8BE4219C6A` → `6B355EEF852AFA6C` and `CD9A30C1CD408F1D` → `3D2A052BDF22B72A`. Its run keeps
  its shape — 36 decisions (was 40), one violence incident, policy breached, grocery paying at 0.05.
- All three viewpoint runs exit 0.
- Godot `--selftest`, `--selftest-goldenpath`, `--selftest-directaction`, `--selftest-corroboration`,
  `--selftest-tribute`, and the two-process restart proof: all exit 0.

### Mutation checks

Four, each a real temporary edit to production code, each confirmed to fail the intended test for the
stated reason, each reverted; `git diff` against `src/` confirmed no residual change.

1. **Direction ignores what he already holds** (`succeeded == prior.IsHeld` → `succeeded`).
   `Success_erodes_a_position_he_holds_against_the_man` failed — a rejection grew stronger on success.
2. **Certainty stops scaling the term** (`* position.Confidence` dropped).
   `How_sure_he_is_moves_separately_from_which_bars_he_holds` failed, which is the ruling-5
   distinction: with certainty removed there is nothing left to vary independently of magnitude.
3. **A rejected bar scores as a held one** (`direction` forced to `+1`). Two failed:
   `A_rejected_bar_scores_worse_than_a_bar_he_has_no_view_on` and the preference-flip test.
4. **An outcome invents an opinion he never had** (`Learn` on the null-prior branch).
   `An_outcome_neither_invents_a_view_nor_revises_one_he_was_told` failed.

**One guarantee is structural rather than mutation-checked, and is recorded as such:** the scorer
cannot read the executor's objective capability, because `Utility.Score` receives no `World` and the
candidate no longer carries a figure. There is no edit that reintroduces the milestone-020 defect
without first re-threading `World` into scoring — which is the point of removing the field.

## Deferred

Per the milestone's own exclusions: assessments of Persuasion, Discretion or Investigation; decay of
an assessment over time (`RELATIONSHIPS.md` — decay returns when tiers supply a timescale); a third
subordinate, crew, equipment, recruitment, roster, payroll, resource transfer; player-facing display
of the assessment; any change to `ResolveViolence`. Plus the two items this milestone added to
`ROADMAP.md`: unrevisable testimony-acquired capability beliefs, and a character's confidence in his
own ability (ruling 7).

## Where to look and what to distrust

**This milestone is unreviewed.** Codex ran out of usage on 2026-09-04, mid-correction on milestone
020, and every commit from `34cd117` onward — including all of this one — has had no adversarial
review. Milestone 020's record is the reason to treat that as material: two Codex rounds, two P1s,
plus a third defect found only when somebody looked hard while scoping.

The two claims most expensive if wrong, and both are places a reader should re-derive rather than
trust: **the fixture asymmetry** (beliefs seeded only in `capable-angelo`, which is what keeps five
hashes still — if that reasoning is wrong, the hash preservation is an artifact rather than a
property), and **the emergent testimony behaviour** above, which was discovered by reading a trace
after the fact rather than predicted, and whose frozen-second-hand-belief consequence is recorded as
debt on the judgement that it is defensible rather than on a proof that it is.

## Commit

One implementation-and-archive commit. Status is not established by this file —
`docs/CURRENT_MILESTONE.md` says what is active, and Matt's confirmation of a named commit is the only
thing that counts as acceptance.

## Correction 1 — Codex's review of `e65f0cd`, 2026-09-05

**Appended, not rewritten.** Everything above stands as the account of what was built and why. This
section records what was wrong with it, and one statement above that this correction reverses. Codex
returned FAIL with three code defects and two documentation requirements; all five are addressed in
one commit, and none of the milestone's design was rejected.

### 1. A belief moved where nothing had reached the man

`Suitability.RecordDelegatedOutcome` was called from `Strategies.Blocked` as well as from the
collection. **The blocked branch is silent.** The target held out, and no report, no observation and
no discovery roll carries that to whoever ordered the job — the owner is not there, nobody has told
him, and the branch files no claim on him the way the collection path does. His read of the man he
sent moved anyway, which is the owner reading world state he has no access to: the same omniscience
this milestone's own scoring term had already been corrected for twice.

The event that branch schedules does wake him, and **waking is not learning** —
`EventKind.StrategyBlocked` carries no claim into anybody's cognition. That the *timing* of a pause is
observable to a player is a separate, known leak recorded in `ROADMAP.md`; it is not a channel to his
character.

The call is removed. The collection keeps its own, where he genuinely comes upon the takings and the
same branch already files a `SourceKind.Discovery` claim for exactly that.

**The milestone's deliverable survives intact, which was measured before the change was made.** With
the call removed, all of `ExecutorSuitabilityTests` still passes — including
`The_delegators_read_of_his_man_moves_during_a_natural_run`, this milestone's headline claim. In the
unmodified `capable-angelo` run the belief moves through the *collection*, so the natural proof never
depended on the path that was wrong. Ruling 6's proof obligation also survives: a success still makes
an already-wrong belief wronger, because direction is relative to what he already holds.

**It moves `capable-angelo`, and only `capable-angelo`.** Trace `6B355EEF852AFA6C` →
`12AF1B71EBBDF51F`, chosen actions `3D2A052BDF22B72A` → `1EDE45C580544105`, 36 decisions → 37. The
other five variants are byte-identical, verified before and after. Ruling 4 permits exactly this
shape; Matt authorized the new baseline on 2026-09-05.

### 2. A revision could not say what moved it

Confidence drifted with nothing on the record but a `ReconsideredAt` stamp, so a belief that shifted
because a job came back was indistinguishable from one that shifted because a canvass found nothing.

`InformationRecord` gains `Reconsidered`, a `Reconsideration(Cause, Via, AboutId)` recording the
occasion, the channel the evidence arrived through, and who or what it concerned. **Kept alongside the
acquisition source, never in place of it** — overwriting `SourceKind`/`SourceId` with the revision's
would make a March inference look like a May discovery, which is the silent rewrite `AcquiredAt` and
`ReconsideredAt` are already kept separate to prevent.

`Cognition.Revise` takes it as a **required** parameter. Optional would have left the same gap open to
the next caller, and the defect being answered is precisely a figure moving with no reason recorded.
All three production call sites now name their occasion; the comprehensive replay comparator carries
it, and `BehavioralSnapshot` deliberately does not, because no decision reads it.

### 3. The ladder was not a ladder — and this reverses a statement above

The section "What was completed → `Domain/Claim.cs`" states that the implication is "deliberately
**not** enforced on write: a character may hold an incoherent pair, because he is allowed to be wrong
and nothing gets to tidy his beliefs behind his back."

**The reason stands; the conclusion drawn from it was too broad.** A character being *factually
wrong* and the *model* contradicting itself about what his beliefs amount to are different things, and
that sentence licensed the second in the name of the first. Vincent holding that Angelo is a hard man
while rejecting that he is up to rough work produced two "executor capability" components pulling in
opposite directions — the same man simultaneously a reason to send him and a reason not to.

**Matt's ruling, 2026-09-05: resolve on read.** Storage still admits the incoherent pair, nothing
rewrites cognition, and no lower-bar belief is invented — the raw records stay exactly as he formed
them, available to developer traces and to replay. What changed is that no *reader* may act on the raw
pair: `Utility` and `PlayerView` both go through `CapabilityBar.Read`.

The rule is one sentence: **the highest bar he holds sets his tier, and every bar below it is
entailed.** That covers both ways the raw records fall short — a gap (holds the high bar, no view of
the low one) and a contradiction (holds the high bar, rejects the low one). Letting the rejection win
instead would need a second rule and would leave the gap case inconsistent with it. Entailment
supplies a position and never overwrites one that agrees, so an independently held bar keeps its own
confidence rather than inheriting a shakier one from above.

Nothing at runtime can build the pair: `Cognition.Revise` moves confidence and never stance, so only a
scenario fixture can seed one. **It does not move any hash** — in the accepted fixture Vincent holds
both bars on Angelo coherently, so there is nothing to resolve, which was measured rather than assumed.

### 4 and 5 — the durable rulings, and the record

`DESIGN_DECISIONS.md` gains "Capability as belief — settled by milestone 021 and its correction":
capability lives in `Cognition`; magnitude is graded propositions and never confidence; the ladder
resolves on read; a belief moves only where information reached the character and the record says what
moved it; and confounded attribution is deliberate. **That last one is narrowed rather than
transcribed**, because correction 1 removed half of what ruling 6 originally covered — an assessment
may be confounded, wrong, and get wronger, but it may not move on information the character never
received. Writing ruling 6 down verbatim would have made the canon durably describe a mechanism this
correction deleted.

The raw `PersonIsCapable` player-output leak Codex noted was already fixed by `4da1e66` and was not
touched again.

### What this correction's own testing found

**A mutation check caught a green test of this correction's that proved nothing.** The ladder test was
written asserting the scorer and the roster could not disagree — and the mutation that should have
falsified the roster half did not fail it. `PlayerNarration.TakenFor`'s switch matches `(_, true)`
before it ever reaches the low bar, so raw `(false, true)` and resolved `(true, true)` render the same
sentence. **The roster could never have displayed the disagreement; the observable defect was
scoring-only.** The test was rewritten to claim only what it proves and now states the distinction in
its own summary rather than leaving a reader to assume both halves are load-bearing.

Recorded because it is the fourth instance in this project of a green test concealing a real gap, and
the first that the mutation discipline itself caught rather than a later reader.

### Verification

Build 0 warnings / 0 errors; tests **636** (623 + 12 + 1). `--verify` on baseline,
`disloyal-vincent`, `resentful-tommy` and `capable-angelo`; both required viewpoint runs; five Godot
self-tests and the two-process restart proof all exit 0. Four mutation checks, each a real temporary
production edit, each failing exactly the intended tests and nothing else, each reverted with
`git diff` confirmed clean afterward.

### Commit

One correction commit. Still unreviewed and unaccepted — this correction has not been back to Codex.
