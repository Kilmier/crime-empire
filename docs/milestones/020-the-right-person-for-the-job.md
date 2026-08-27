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
