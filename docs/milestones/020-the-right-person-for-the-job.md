# Milestone 020 — The Right Person for the Job

Authorized by Matt in chat. The full authorizing text is preserved in this commit's prior version of
`docs/CURRENT_MILESTONE.md` and is summarized here as needed to explain what was done.

## What this milestone was for

`ROADMAP.md` had carried, since milestone 017, that executor suitability/capability is not modelled:
`Generators.FromRelationship` delegated to the single highest-trust subordinate available —
deterministically Tommy, Vincent's only organisational subordinate — with nothing about who would
actually do the job *better* entering the choice. This milestone closes the narrowest slice of that
debt: add one bounded scenario variant with exactly one additional subordinate so Vincent naturally
reaches a `SecureTribute` delegation decision involving two known, eligible and available executors;
generate one normal delegation candidate per subordinate; evaluate each through the shared decision
pipeline using relationship state and one task-relevant existing capability (Coercion); and ensure
the selected executor's capability reaches the existing force/escalation resolution — all while
preserving milestone 017's owner/executor attribution rules and every existing accepted variant's
trace hash and chosen-action digest. Explicitly out of scope: broader persuasion effects, crew,
equipment, preparation, recruitment, roster, payroll, resource-transfer, or personnel-management
systems.

## What was already true before this milestone

`Generators.FromRelationship` already generated exactly one `DelegateStrategy` candidate — the
subordinate with the highest trust — whenever `ctx.SubordinateIds.Count > 0`. `Utility.Score`'s
`DelegateStrategy` branch already scored relationship state generically per `Candidate.TargetId`
(`AddLoyaltyParts("relationship effects", 0.9, Loyalty(actor, psy, cand.TargetId), ...)`), so once
more than one candidate existed with different `TargetId`s, each would already be scored against its
own subordinate's relationship state for free — no change was needed there.
`Strategies.ResolveViolence` read no capability at all: force resolved as a flat
`business.Resistance -= 0.3`, regardless of who applied it.

## The two judgment calls

**The seventh character.** `ROADMAP.md` records the cast as a ceiling of six, with a seventh needing
its own ruling — the exception milestone 007 needed for `nunzio` (Matt's acceptance, recorded in
`REVIEW_LEDGER.md`'s `974a88a` row). Matt's own milestone-020 scope text — "one bounded scenario
variant with exactly one additional subordinate" — is treated as that ruling here, recorded rather
than silently exceeded. `angelo` exists only inside `Variants.Apply`'s new `capable-angelo` case,
never in `Cast.Build`, so every other variant's cast is still exactly six.

**Force resolution vs. hash preservation.** Wiring the executor's Coercion into
`Strategies.ResolveViolence`'s force outcome and preserving every accepted variant's trace hash are
only simultaneously possible because, verified empirically via `--compare` at seed 42 before writing
any code: **Tommy is the only executor whose Coercion (0.55) has ever been exercised at that call in
any accepted variant** — violence fires in baseline/watchful-boss/disloyal-vincent/resentful-tommy,
never cautious-vincent, and Tommy is the delegate every time. The new formula
(`Math.Clamp(0.3 + 0.4 * (coercion - 0.55), 0.1, 0.5)`) is calibrated to reduce to exactly the old
constant at Tommy's own Coercion, and only diverges for a different executor — the same frankly
provisional treatment `Utility.BaseRisk`/`BaseEffect`/`Exposure` already get. Flagged during planning
and confirmed against the user before implementation, per `AGENTS.md`'s "surface design conflicts
rather than resolving them silently" — not discovered as a defect during review.

## What was completed

### `src/CrimeEmpire.Simulation/Decision/Candidate.cs`

One new field: `ExecutorCoercion` (`double?`), documented as set only when there is a genuine choice
among two or more subordinates, and left `null` — not merely unread — otherwise.

### `src/CrimeEmpire.Simulation/Decision/Generators.cs`

`GeneratorContext` gained `SubordinateCoercion` (`IReadOnlyDictionary<string, double>`), each
subordinate's own Coercion read directly, the same non-belief-limited way `SubordinateIds` itself
already is (an authority-scoped organisational fact, not a belief — matching the existing precedent
that a boss already institutionally knows who reports to him). `FromRelationship`'s delegation block
now yields one candidate per `ctx.SubordinateIds` entry instead of pre-selecting the highest-trust
one; with exactly one subordinate this degenerates to exactly the one candidate it always produced,
same id, same fields. `ExecutorCoercion` is attached only when `ctx.SubordinateIds.Count > 1`.

### `src/CrimeEmpire.Simulation/Decision/Pipeline.cs`

`Prepare` builds the `subordinateCoercion` dictionary once per deliberation and threads it into
`GeneratorContext`.

### `src/CrimeEmpire.Simulation/Decision/Utility.cs`

One new score component, "executor capability", fires only for `DelegateStrategy` candidates
carrying a non-null `ExecutorCoercion`, centered on 0.5 (the natural midpoint of the skill range —
not calibrated to any character, since this component is never emitted for an existing accepted
candidate). Tagged `RelationshipFacet.None` and named distinctly from "relationship effects" so the
two considerations stay separately inspectable, matching milestone 008's facet-tagging discipline.

### `src/CrimeEmpire.Simulation/Strategy/Strategies.cs`

`ResolveViolence` now reads `executor.Capabilities[Skill.Coercion]` and scales the resistance
reduction by it via the calibrated formula above. Attribution, the owner-learns-nothing rule, and
the discoverability loop are untouched — milestone 017's owner/executor rules are preserved by not
touching that code, not by re-proving it. Threaten/`Relations.Frighten` and Persuasion were
deliberately not touched, per the milestone's "Coercion only for force/escalation resolution" scope.

### `src/CrimeEmpire.Simulation/Scenario/Variants.cs`

A new `capable-angelo` case in `Variants.Apply`: Angelo Conti, soldier, `Authority = 1` (Tommy's own
rung, so `Pipeline.SubordinatesOf` picks him up with no change to that method), Coercion 0.80
(harder-hitting than both Tommy's 0.55 and Vincent's 0.75), modest Persuasion/Discretion/
Investigation mirroring Tommy's shape, Psychology kept broadly parallel to Tommy's so the variant
isolates trust and capability rather than adding a third variable. Vincent's trust in Angelo (0.35)
is deliberately below his trust in Tommy (0.70) — the less-trusted, more-capable option, which is the
whole point of the variant. Added to `Variants.All` and `Variants.Describe`.

### `tests/CrimeEmpire.Simulation.Tests/ExecutorSuitabilityTests.cs` (new, 9 tests)

Natural (the fork offers continuation and both subordinates together — the direct falsifier of
mutation check 1), eligibility (both delegate candidates independently survive `Filters`), capability
present/absent (both candidates carry and are scored on `ExecutorCoercion`; a single subordinate gets
no such component at all — the direct falsifier of mutation check 2), staged owner/executor-split-plus
-capability-scaled-outcome (mirroring milestone 017's Section B idiom exactly, run once per executor
— the direct falsifiers of mutation checks 3 and 4), the natural run's chosen executor being who
actually throws the punch (consequence, tied to the staged proof's exact arithmetic rather than
re-derived across a full 90-day run where Marco's own concession also moves `Resistance`), save/load
through the Tommy-vs-Angelo fork (mirroring milestone 017's Section C idiom), and a targeted
preference-leakage check for the new score vocabulary specifically (the direct falsifier of mutation
check 5) — narrower than, and additional to, the existing generic "no player phrase carries a
decimal" regression.

**Deliberately not re-proven**, and stated as such in the file's own class-level doc comment:
controlled/autonomous parity and viewpoint-only isolation for `capable-angelo` — both already covered,
not merely in principle, by `ControlledAutonomousParityTests`'s and `PlayerSessionTests`' existing
sweeps, which iterate `Variants.All` generically. Adding `capable-angelo` to that array is what
extends them; the full suite was confirmed green (573/573) with no new test code required for either.
The same is true of the generic decimal-leak regression.

### Mutation checks

All five required checks were applied as real, temporary edits to production code, confirmed to make
the relevant test(s) fail for the stated reason, then reverted. Full suite re-confirmed green (573/573)
after every revert; `git diff --stat` against `src/` confirmed no residual change after each one.

1. **Highest-trust-only generation** — `FromRelationship`'s new per-subordinate loop reverted to the
   pre-020 `OrderByDescending(trust).First()`. **5 tests failed**: the fork stopped offering Angelo at
   all, eligibility dropped from 2 candidates to 1, and the capability-comparison test threw
   ("sequence contains no matching element") because there was nothing to compare.
2. **Capability-free scoring** — the "executor capability" block's condition changed to an
   always-false pattern (`(double?)null is { } execCoercion`). **1 test failed**: neither candidate's
   breakdown carried the component at all.
3. **Owner-capability execution** — `ResolveViolence` changed to read
   `owner.Capabilities[Skill.Coercion]` instead of the executor's. **2 tests failed** (both cases of
   the staged theory): both Tommy's and Angelo's runs produced Vincent's 0.75-derived reduction
   (0.38), matching neither expected value (0.30, 0.40).
4. **Wrong executor attribution** — `witnessClaim`'s subject changed from `executor.Id` to `owner.Id`
   (`violenceClaim` left correctly attributed). **2 tests failed**, reprising milestone 017's own
   mutation for the second subordinate: both cases expected `"tommy"`/`"angelo"` and got `"vincent"`.
5. **Preference leakage** — `PlayerOption`'s `DelegateStrategy` case temporarily appended
   `"(better suited)"`/`"(not the man for rough work)"` to the rendered option text. **3 tests
   failed**: the targeted leak-freedom check, plus two others whose exact-description matching
   (`ChooseByDescription`) broke as a direct consequence of the leaked text changing the option's
   wording — a genuine failure, not a false assurance, since it demonstrates the leaked phrase reaches
   the exact surface a real interface reads.

## Verification

- Build: **0 warnings, 0 errors**, unchanged project count.
- Tests: **573 passed, 0 failed** (564 before this milestone; 9 new in `ExecutorSuitabilityTests.cs`).
- `--verify` deterministic and byte-identical on `baseline` (`9AF57665067AEA11`) and the new
  `capable-angelo` (`2060465B4F31E6DD`).
- `--compare` at seed 42: **6 configurations · 6 distinct traces · 6 distinct chosen-action
  sequences**. All five pre-existing variants' hashes **unmoved**: `baseline 9AF57665067AEA11`,
  `cautious-vincent 86EC1ADA4A4E9179`, `watchful-boss 84AC3F65E4102EBA`,
  `disloyal-vincent 9A6E0E518294532F`, `resentful-tommy 3C4483640153DA88`. `capable-angelo`:
  40 decisions, 1 violence incident, policy breached, grocery resistance 0.05, trace
  `2060465B4F31E6DD`, actions `CD9A30C1CD408F1D`.
- Both required viewpoint runs (`disloyal-vincent`/`salvatore`, `baseline`/`vincent`) exit 0, plus the
  new `capable-angelo`/`salvatore`.
- Godot self-tests were **not run** for this milestone — no Godot executable was available in this
  environment, and `AGENTS.md`'s own `§Verification` list does not include the Godot headless check
  (recorded there as a known gap independent of this milestone). Nothing in this diff touches
  `src/CrimeEmpire.Godot`.

## Important discoveries

**The natural run picks the more capable, less trusted man.** At seed 42, Vincent delegates to Angelo
(score 6.52) over Tommy (score 6.38) despite trusting him barely half as much — the relationship
channel alone favours Tommy (gross 0.2835 vs. Angelo's 0.1688, per the developer diagnostic), but the
new "executor capability" component (+0.30 for Angelo) and the rest of the shared scoring outweigh it.
This was not tuned toward either outcome — the capability coefficient was chosen once, centered on the
natural 0.5 midpoint, and never adjusted after seeing which subordinate won, matching this project's
standing practice of reporting whichever result a genuinely comparative mechanism produces.

**`Candidate.Description` and the player-facing option text are different vocabularies, and
conflating them in a test is a real defect, not a typo.** The first draft of `AdvanceToVincentsFork`
matched the low-level `Candidate.Description` ("talk bellini-grocery round", the developer trace's own
wording) against the player-facing constant `StartPersuade` ("talk Bellini's grocery round") and
failed with "sequence contains no matching element" — `PreparedDecision.Available` carries
`Candidate`, not `PlayerOption`. Fixed by matching on structured fields (`Kind`/`Strategy`/`TargetId`/
`Method`) instead, which is also more robust than string matching. `PlayerOption.cs`'s own header
already documents why the two vocabularies are deliberately separate; this is a fresh instance of
relying on that separation rather than a new finding about it.

**`StrategyStep` never pauses, so "the next event" and "Vincent's next pause" are not the same
question.** The first draft of the pipeline-level fork-reaching helper called `Runner.Step` exactly
once after resolving the start decision and asserted `AwaitingChoice` — wrong, because `Runner.Handle`
routes `StrategyStep` straight to `Strategies.Advance` with no `Think`/pause regardless of who it
addresses. Vincent's own approach step, and the round trip through Marco's own demand/refuse decision,
are real intervening advances. Fixed by looping `Runner.Step` calls with a guard, the same pattern
`ControlledAutonomousParityTests.AdvanceToTommysPause` already established — reused rather than
re-derived once the shape was recognized.

## Deferred work

Per the milestone's own exclusions, recorded in `ROADMAP.md`'s narrowed "Executor
suitability/capability" entry: Persuasion's effect on tribute success; crew size, equipment,
preparation; recruitment, roster, payroll, or resource transfer from owner to delegate; personnel
management generally; escalation-capability ownership as a general rule beyond the one mechanism this
milestone touches (Coercion at force resolution, resolved in favour of the executor for that
mechanism specifically); a third or later subordinate; a general suitability model across strategy
kinds; capability affecting anything beyond force resolution. Territory, patrol, weekly planning,
additional businesses or operations, an eighth character, employee-stat displays or new UI panels, the
known pause-timing information leak, new organizations, careers, or alternate playable roles are all
untouched and unaffected, per `docs/CURRENT_MILESTONE.md`'s carried-forward list at the time this
milestone began.

## Where to look and what to distrust

The two judgment calls above — the seventh character and the force-resolution calibration — are the
claims most expensive if wrong. Both were confirmed empirically before being written into production
code (the `--compare` run establishing Tommy as the only exercised Coercion value; the plan itself
approved before implementation began) rather than asserted from the shape of the code. A reviewer
should re-derive the calibration claim independently: run `--compare` at seed 42 against the
pre-milestone baseline commit and confirm violence fires only where Tommy is the delegate.

The "executor capability" score component's centering at 0.5 is the other place to check for hidden
tuning-toward-a-result: it was chosen before the natural run was ever executed, and the archive's own
"important discoveries" section above states the natural winner (Angelo) honestly rather than
selecting a coefficient that would have produced a different, perhaps more dramatic-sounding, result.

## Commit

One implementation-and-archive commit. Status is not established by this file —
`docs/CURRENT_MILESTONE.md` says what is active, and Matt's confirmation of a named commit is the only
thing that counts as acceptance.

## Correction 1 — the executor-capability score read the objective world, not a held assessment

Codex reviewed the implementation commit (`f468e19`) and returned **FAIL**: one P1, no P2s.

**The finding.** `Pipeline.Prepare` built `GeneratorContext.SubordinateCoercion` by reading
`world.Get(id).Capabilities[Skill.Coercion]` for every subordinate, straight off `World`, and
`Generators.FromRelationship` copied that value onto each `DelegateStrategy` candidate's
`ExecutorCoercion` for `Utility` to score. That is an omniscient read: `Decision/Utility.cs`'s own
file header states the rule every other score component obeys — `Score` "receives a
`PerceivedSituation` and never a `World`", so a character cannot score an option using a fact he does
not hold. `Pipeline.SubordinatesOf` reading `world.Characters` to learn *who* reports to Vincent is a
settled, legitimate exception — `DESIGN_DECISIONS.md`'s "an office relationship is only an office
relationship if it comes from an office" ruling licenses exactly that authority scan for identity —
but *how good* a named subordinate is at his job is a fact about that person, not the org chart, and
nothing in that ruling or anywhere else extends the exception to a skill value. The nine tests this
milestone added exercised the mechanism but never varied the objective figure independently of what
scoring used, so all nine passed against the very violation being reported: they pinned the
omniscient read rather than disproving it.

**The correction.** Smallest change that separates the two questions Codex's finding distinguishes —
*who* Vincent's subordinates are (still an authority scan, untouched) from *how good* one is at the
job (now a held belief, not a `World` read) — without building any wider personnel-management system:

- `Domain/Relations.cs`: a new `double? AssessedCoercion` dimension on `IRelationship`, alongside
  `Trust`, `Obligation` and `Fear` — what the character holds toward another believes about that
  person's Coercion, set only from scenario construction (nothing yet lets a character revise it from
  observation, and this correction does not add such a mechanism). Null means no assessment formed,
  which must not be confused with an assessment of zero. `Relations.Establish` gained an optional
  `assessedCoercion` parameter; a new `Relations.SetAssessedCoercion` mutator changes only this
  dimension, for tests that need to vary it independently of trust/obligation/fear.
- `Scenario/Cast.cs` and `Scenario/Variants.cs`: Vincent's assessment of Tommy (0.55) and Angelo
  (0.80) is set to match each subordinate's own actual `Capabilities[Skill.Coercion]` exactly — the
  same figures the pre-correction code read directly — so every accepted trace hash and the natural
  run's own preference are unmoved. This is a deliberate reproduction of the old numbers as a belief
  Vincent happens to hold accurately, not a claim that beliefs and reality must match in general.
- `Decision/Generators.cs`: `FromRelationship` now reads `ctx.Actor.Social.Toward(sub).AssessedCoercion`
  — the actor's own relationship record, the exact non-creating channel `Utility.Loyalty` already
  reads for trust/obligation/fear — instead of a dictionary sourced from `World`. The "genuine choice"
  gating (only attached when there are two or more subordinates) is unchanged.
- `Decision/Pipeline.cs`: the `subordinateCoercion` dictionary and the `world.Get(id).Capabilities[...]`
  read that built it are gone. `GeneratorContext.SubordinateCoercion` is gone from the record entirely
  — nine test files that constructed a `GeneratorContext` directly needed the now-removed parameter
  dropped from their call sites, a mechanical follow-on rather than a second defect.
- `Decision/Candidate.cs` and `Decision/Utility.cs`: doc-comment corrections only. `Utility`'s scoring
  logic for the "executor capability" component is byte-for-byte unchanged — it already read
  `cand.ExecutorCoercion`, which now carries an assessment instead of a `World` reading, and needed no
  code change to do the right thing once its input was fixed at the source.
- `Strategy/Strategies.cs`: **untouched.** `ResolveViolence` still reads the executor's actual,
  objective `Capabilities[Skill.Coercion]` — Codex's finding and the correction's own scope both treat
  *committed force resolution* as correctly reading `World` (it is not scoring an option, it is
  computing what actually happened), so nothing here needed to change, and nothing did.

**Three new tests**, `ExecutorSuitabilityTests.cs` (9 → 12), covering three of Codex's five list
items; the fourth (requirement 3) was satisfied by the existing, unmodified staged force-resolution
test, and the fifth (requirement 5) was run as a temporary mutation rather than a permanent test —
both explained where they fall below:

1. `Changing_only_angelos_hidden_actual_capability_leaves_scoring_unchanged` — two worlds sharing the
   same Vincent-held assessment (0.80) but different actual `Capabilities[Skill.Coercion]` (0.80 vs.
   0.15) produce identical `Available` candidate ids, an identical `ExecutorCoercion`/`Total` for
   Angelo's delegate candidate, and an identical top preference.
2. `Changing_only_vincents_assessment_of_angelo_changes_score_and_can_flip_preference` — two worlds
   sharing Angelo's actual Coercion (0.80) but different assessments (0.80 vs. 0.15) move the
   "executor capability" component and flip which of the two delegate candidates specifically outscores
   the other (Angelo beats Tommy when trusted as capable; Tommy beats Angelo when Angelo is doubted).
   Deliberately scoped to the delegate-versus-delegate comparison rather than `Scored[0]` overall: at
   the very first fork "carry on what he just started" outscores either delegate regardless — the
   archive's own 6.52-vs-6.38 Angelo/Tommy comparison above was measured at a later decision in the
   full run, after the first attempt had already failed once, discovered only while writing this test.
3. Requirement 3 ("objective executor capability still changes committed force consequences") needed
   no new test: the original `Force_resolution_is_attributed_to_and_scaled_by_the_actual_executor`
   theory already stages `ResolveViolence` directly from each executor's real `Capabilities`, entirely
   outside the scoring path this correction changed, and still passes unmodified.
4. `A_missing_assessment_is_neither_the_objective_capability_nor_zero` — a world where Vincent has
   formed no assessment of Angelo (`assessedCoercion: null`) produces `Candidate.ExecutorCoercion ==
   null` and no "executor capability" component at all for Angelo specifically, while Tommy's own
   (present) assessment still scores normally — proving the missing case is neither a fallback to the
   objective figure nor a silent floor of zero, and that it does not disable the comparative branch
   entirely.
5. Requirement 5 ("reintroducing the direct read fails the intended test") was run as a temporary
   mutation rather than a permanent test, matching this project's established practice: `Pipeline.Prepare`
   was edited to force each of Vincent's subordinates' `AssessedCoercion` to their actual objective
   Capabilities value on every deliberation (scoped to `actor.Id == "vincent"` specifically — an
   unscoped version also disturbed `PlayerSessionTests.An_authority_adjacent_stranger_holding_no_office_is_not_a_target`,
   an unrelated acquaintance-boundary fixture, because the mutator used to force the value,
   `Relations.SetAssessedCoercion`, creates a stored relationship as a side effect the same way
   `Establish` does — exactly the kind of side effect the real corrected code avoids by using a
   non-creating `Social.Toward` read instead). Full suite under the mutation: **3 failed, 573
   passed** — precisely tests 1, 2 and 4 above, each failing for the stated reason (test 1: Angelo's
   total moved with the hidden capability; test 2: `ExecutorCoercion` read back as the objective 0.80
   instead of the assessed 0.15; test 4: `ExecutorCoercion` was no longer null). Reverted; `git diff
   --stat` confirmed no residual change to `Pipeline.cs` afterward.

**A `BuildAngeloWorld` test helper**, local to `ExecutorSuitabilityTests.cs`, parameterises Angelo's
actual and assessed Coercion independently — the two figures the pre-correction code forced equal.
It duplicates `Variants.Apply`'s `capable-angelo` construction rather than adding a test-only knob to
production scenario code, matching this file's own established practice of not sharing helpers across
milestone-specific test files and, more importantly, keeping any test-only parameterisation out of
`Scenario/Variants.cs` entirely.

**Verification.**

- Build: 0 warnings, 0 errors.
- Tests: **576 passed, 0 failed** (573 carried + 3 new, all three in `ExecutorSuitabilityTests.cs`,
  which moves from 9 tests to 12; two of Codex's five requirements were satisfied by an existing test
  and a temporary mutation rather than new permanent tests, so three new tests cover the remaining
  three. The other eight test files that construct a `GeneratorContext` directly needed only their
  now-removed `SubordinateCoercion` constructor argument dropped, no behavioural change).
- `--verify --seed 42 --days 90`: baseline `9AF57665067AEA11`, deterministic.
- `--compare --seed 42`: all six configurations, six distinct traces, six distinct chosen-action
  sequences, every hash and digest identical to the pre-correction values recorded above — including
  `capable-angelo`'s own `2060465B4F31E6DD`/`CD9A30C1CD408F1D`.
- `--verify --variant capable-angelo`, `--variant disloyal-vincent`, `--variant resentful-tommy`: all
  deterministic, all matching their recorded hashes.
- `--variant disloyal-vincent --viewpoint salvatore`, `--variant baseline --viewpoint vincent`, and
  `--variant capable-angelo --viewpoint salvatore`: all exit 0.
- Godot headless self-tests (`--selftest`, `--selftest-goldenpath`, `--selftest-directaction`,
  `--selftest-corroboration`, `--selftest-tribute`) and the two-process restart proof
  (`--selftest-restart-save` / `--selftest-restart-load`, genuinely separate processes sharing the
  restart self-test's own isolated save slot): all exit 0, all print their own `ok` marker, all
  unchanged from before this correction. **A Godot executable was in fact available in this
  environment** (`Godot_v4.7.1-stable_mono_win64_console.exe`, under the user profile) — the original
  implementation's "no Godot executable was available" note above reflected an incomplete search at
  the time, not a genuine absence, and is left standing rather than edited, per this project's
  append-only rule for archived milestone text.

## Commit (correction 1)

Committed as `436f6c7`. Status is not established by this file — `docs/CURRENT_MILESTONE.md` says what
is active, and Matt's confirmation of a named commit is the only thing that counts as acceptance.

## Correction 2 — a raw authority scan named a delegate candidate's target, and a new relationship dimension was missing from the replay comparators

Codex reviewed `436f6c7` and returned **FAIL**: one P1, two P2s.

### P1 — SubordinateIds is not itself Acquaintance

**The finding.** `Generators.FromRelationship`'s delegation loop iterated `ctx.SubordinateIds`
directly to decide who to offer as a delegate — unchanged by correction 1, which touched only what
value each candidate carried, not which candidates were generated in the first place. `SubordinateIds`
is `Pipeline.SubordinatesOf`'s own authority scan over `world.Characters`: a legitimate, settled route
to knowing *who* reports to Vincent (`DESIGN_DECISIONS.md`'s "office relationship" ruling licenses
exactly this for identity), but not itself grounds to treat that man as somebody Vincent could name.
`Acquaintance.KnownTo`'s own header states the rule this violates directly: "a soldier holding no
office is therefore not knowable this way, however senior he is." Every candidate's target must come
from `ctx.AcquaintedIds` — the single derivation `DESIGN_DECISIONS.md` settled after two prior
corrections (milestone 009's) got exactly this wrong in the same shape — and a `DelegateStrategy`
candidate is a candidate like any other. Nine tests in `ExecutorSuitabilityTests.cs` exercised
delegation candidates without ever varying `AcquaintedIds` independently of `SubordinateIds`, so all
of them passed against the very gap being reported.

**Why every accepted variant's hash survives this fix regardless.** Every scenario fixture this
project ships establishes a relationship between Vincent and each of his real subordinates at
construction — `Cast.Build` for Tommy, `Variants.Apply` for Angelo — which independently puts them in
`AcquaintedIds` via `SocialState.Others`. `SubordinateIds ⊆ AcquaintedIds` already held for every
accepted variant; nothing in the fixtures has ever exercised the gap the fix closes.

**The correction.** `FromRelationship` now filters `ctx.SubordinateIds` through a
`HashSet<string>` built from `ctx.AcquaintedIds` before generating any delegate candidate, and
computes the `comparative` "genuine choice" flag from the filtered (nameable) set rather than the raw
organisational count — a subordinate present in `SubordinateIds` but absent from `AcquaintedIds` is
now excluded entirely, and a boss with two subordinates on the roster but only one he could actually
name gets no executor-capability comparison at all, since there is nothing nameable to compare that
one man against.

**One new test**, `ExecutorSuitabilityTests.An_organisationally_subordinate_but_unacquainted_stranger_is_not_offered_as_a_delegate`,
staged directly against `Generators.GenerateAll` (there is no natural route to an
organisationally-subordinate, wholly-unacquainted stranger through any shipped scenario fixture,
so the pipeline/`World` level cannot exercise this case — the `GeneratorContext` has to be hand-built).
One staged set proves three things together:

1. **Negative.** A hand-built "aldo-stranger", present in `SubordinateIds` but absent from
   `AcquaintedIds`, is never offered as a delegate.
2. **Positive acquaintance control.** Tommy, who genuinely is acquainted, is still offered in the same
   candidate set — proving the filter is selective, not a blanket suppression that would make the
   negative assertion vacuous (a filter that excluded everyone would also "pass" it).
3. **Comparative computed from the filtered set, not the raw count.** Raw `SubordinateIds.Count` in
   this staging is 2 (Tommy plus the stranger) — which the pre-correction gate would have read as a
   genuine two-way choice — but with the stranger filtered out, only one nameable subordinate remains,
   so Tommy's own candidate carries no `ExecutorCoercion` comparison at all.

**Mutation check.** `FromRelationship`'s filtered subordinate list was temporarily reverted to
`ctx.SubordinateIds.ToList()` (bypassing the acquaintance filter entirely, reproducing the
pre-correction shape). The new test failed on its very first assertion — the stranger appeared in the
delegate candidates — confirmed, then reverted; `git diff` against `Generators.cs` confirmed no
residual change.

### P2 — the replay comparators did not know AssessedCoercion existed

**The finding.** `SimulationReplayTests.cs` carries two independent relationship fingerprints —
`Snapshot` (the comprehensive comparator, reused wholesale by `ControlledAutonomousParityTests`) and
`BehavioralSnapshot` (the narrower one used by the id-perturbation regression tests) — and neither
printed `Relations.AssessedCoercion`, correction 1's own new relationship dimension. A defect that
corrupted `AssessedCoercion` between two otherwise-identical runs — including a regression of exactly
the P1 shape above, or a future one — would have passed both comparators undetected, the same
completeness gap milestone 019's own correction cycle spent three rounds closing for a different
field.

**The correction.** Both `Snapshot` and `BehavioralSnapshot` now print
`NullableNumber(rel.AssessedCoercion)` alongside `Trust`/`Fear`/`Obligation`, where
`NullableNumber` renders `null` as the literal token `"none"` rather than collapsing it to the same
text `0.0` would produce — preserving, inside the comparator itself, the same null-versus-zero
distinction `Relations.IRelationship.AssessedCoercion`'s own contract requires of the production code
it is checking.

**One new test**, `SimulationReplayTests.The_comparators_capture_the_assessed_coercion_relationship_dimension`.
Because "the snapshot is the comparator, so deleting a field from it makes the comparison blinder
without making anything fail" (this file's own established exception, first stated for the Requests
line), the field cannot be mutation-checked the usual way by reverting *production* code — there is no
production behaviour here to revert, only the comparator's own completeness. The technique this file
already uses for exactly that situation — assert on the comparator's actual content — is applied here
instead: three otherwise-identical `World`s, perturbed on nothing but `Relations.AssessedCoercion`
(a changed value; a null-versus-assessed-at-the-floor pair), must compare unequal under both
`Snapshot` and `BehavioralSnapshot`.

**Mutation check**, performed as this project's stated exception to the usual technique demands: the
`NullableNumber(rel.AssessedCoercion)` term was removed from each comparator's relationship line in
turn. Removing it from both at once failed the new test on its first `Snapshot` assertion; restoring
`Snapshot`'s term alone while leaving `BehavioralSnapshot`'s removed moved the failure to the
`BehavioralSnapshot` assertion specifically — confirming each comparator's own addition is
independently load-bearing, not merely coincidentally passing because of the other. Both reverted;
`dotnet build` returned to 0 warnings (the reverted state briefly produced two `CS8321` "unused local
function" warnings for `NullableNumber`, itself a small confirming signal that the mutation had
actually taken effect).

### Verification

- Build: 0 warnings, 0 errors.
- Tests: **578 passed, 0 failed** (576 carried + 2 new: the acquaintance-boundary test in
  `ExecutorSuitabilityTests.cs`, the comparator test in `SimulationReplayTests.cs`).
- `--verify --seed 42 --days 90`: baseline `9AF57665067AEA11`, deterministic.
- `--compare --seed 42`: all six configurations, six distinct traces, six distinct chosen-action
  sequences, every hash and digest identical to every value recorded above and in correction 1 —
  including `capable-angelo`'s own `2060465B4F31E6DD`/`CD9A30C1CD408F1D`.
- `--verify --variant capable-angelo`, `--variant disloyal-vincent`, `--variant resentful-tommy`: all
  deterministic, all matching their recorded hashes.
- `--variant disloyal-vincent --viewpoint salvatore`, `--variant baseline --viewpoint vincent`, and
  `--variant capable-angelo --viewpoint salvatore`: all exit 0.
- Godot headless self-tests (`--selftest`, `--selftest-goldenpath`, `--selftest-directaction`,
  `--selftest-corroboration`, `--selftest-tribute`) and the two-process restart proof
  (`--selftest-restart-save` / `--selftest-restart-load`): all exit 0, all print their own `ok`
  marker, all unchanged.

## Commit (correction 2)

Committed as `PENDING-HASH` — recorded in a small follow-up documentation commit once known, per this
project's practice of not self-referencing a commit's own hash from inside itself (the same reason
`docs/milestones/019-...md`'s acceptance record was written in a separate commit rather than inside
the correction it accepts). Status is not established by this file — `docs/CURRENT_MILESTONE.md` says
what is active, and Matt's confirmation of a named commit is the only thing that counts as acceptance.
