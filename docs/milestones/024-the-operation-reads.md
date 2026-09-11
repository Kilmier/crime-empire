# Milestone 024 — The Operation Reads

Second milestone of the demo arc's layer 1. Authorized by Matt on 2026-09-05.

## What this milestone was for

A player saw a decision prompt when his character paused and **nothing between pauses**. He gave an
order and the order disappeared. `ExecutionState.Strategy` held what he had running the whole time —
kind, target, method, who it was handed to, when it started, how many attempts had come back empty —
and none of it reached `PlayerSnapshot`. The nearest thing was `LastAction`, one line naming his last
choice, gone the moment he chose anything else.

## The milestone is one branch

**Progress is reported for work he does himself and withheld for work he handed to somebody.** Not a
display preference — the settled information rule, applied to a new surface.

The step he has reached and the refusals he has met are his own experience: he knows he has been to
the shop and been told no. How far along *somebody else* has got is that man's state, and milestones
017 and 022 both settled that the man who ordered a job learns whether it was carried out "through a
report or a discovery roll like anyone else." Reading `StepIndex` off a delegated instance would hand
him that for free — through a panel rather than through a belief, which makes it no less a leak.

So a delegated operation reads as *what he ordered, who is carrying it, and silence*. What breaks the
silence is a report, a rumour, or the takings arriving, all through channels that already exist.

The arc across one run, from the real rendered viewpoint:

```
day 8    getting Bellini's grocery to pay
         running since 2 Mar
         he has put the demand

day 12   getting Bellini's grocery to pay
         running since 2 Mar
         he has put the demand, and once it came back empty

day 20   getting Bellini's grocery to pay
         running since 2 Mar
         Tommy Nardo is carrying it
         nothing has come back yet
```

The progress line *disappears* when Tommy takes it. That is the information boundary made visible
rather than hidden.

## What was completed

- **`Session/PlayerSnapshot.cs`** — `PlayerOperation` and `PlayerSnapshot.Operation`, plus the
  `Operating` projection that owns the branch.
- **`Session/PlayerNarration.cs`** — `OwnProgress`, naming the step reached and counting refusals in
  words.
- **`CrimeEmpire.Godot/Game.cs`** and **`Runner/Trace/IntelligenceWriter.cs`** — both render it, and
  agree.

## Decisions worth their reasoning

**No sixth column.** The Godot layout already carries five and has not been rebalanced since a fourth
was added — milestone 018's recorded debt, which 025 exists to clear. The standing order went into
"WHAT JUST HAPPENED", which is where what he did and what came of it already lives, rather than making
025's problem worse.

**The wording is `PlayerOption.Work`, not a second phrasing.** That method is `internal` precisely so a
running strategy is described the same way an option describes one; its own comment records that the
alternative is how `StrategyInstance.Label` — a developer string carrying raw ids — once reached the
player as a decision's focus.

**Named steps, not indices; refusals in words, not a count.** `StepIndex` is 0–3 and the steps are
already written in plain language in `Strategies`, so the projection reports the last one completed
rather than inventing a second vocabulary. A number would be the model's arithmetic rather than
something that happened to him.

## A false line, caught by reading the output

The first version rendered **"Tommy Nardo has had it since 2 Mar"** — using `StartedAt`, which is when
the *operation* began. Nothing anywhere records when a job was handed over, so that sentence is false
whenever delegation came later than the start, which in the accepted fixture it always does.

Fixed by wording rather than by adding state: the date is given as the operation's own age
("running since 2 Mar") and who holds it is stated separately. A `DelegatedAt` field would have been
new persistent state needing replay coverage, to support a sentence the milestone does not need.

Found by looking at the rendered output, not by a test — the second time in two milestones, and the
argument for the arc's rule that a milestone must be visible in play.

## Verification

- Build 0 warnings / 0 errors; tests **608 passed, 0 failed** (599 + 9, `OperationReadsTests.cs`).
- **All six variants byte-identical on trace and chosen actions.** Projection only; any movement would
  have meant something leaked into the simulation.
- All five Godot self-tests and the two-process restart proof exit 0.
- **Mutation check:** making the projection show progress unconditionally — the tempting
  simplification, and the exact leak this milestone exists to prevent — fails
  `How_far_a_delegate_has_got_never_reaches_the_player_but_his_own_progress_does` and
  `The_panel_is_populated_during_a_natural_run`, and nothing else. Reverted.

The boundary test is deliberately a difference rather than an inspection: two worlds identical but for
how far a *delegated* operation has advanced must render identically, and the same difference on *own*
work must not. Asserting only "delegated progress is null" would pass against an implementation that
never showed progress at all.

## Deferred, and one of them is a real gap

**A history of finished operations.** `Strategies.Complete` sets `Execution.Strategy = null`, so a
completed operation leaves nothing behind anywhere — there is no list of what he has done, only what he
is doing. The panel therefore goes empty the moment a job finishes, which is honest but thin. Its own
scope, and a candidate for 026 where a session that ends needs to be able to say what happened in it.

Also out: multiple simultaneous operations, which the model does not have — one `Strategy` per
character; any change to how operations run or resolve; the interface debt (025).

## Where to look and what to distrust

Unreviewed, like everything since `34cd117`. The claim worth re-deriving is the boundary itself: it
rests on `DelegatedToId is null` being a true test of "is this my own work", and on nothing else in the
snapshot carrying delegated progress by another route. The test proves the first; the second is an
argument from having looked, which is the weaker half.

## Commit

One implementation-and-archive commit.

## Correction — actor-neutrality on `f993386`, 2026-09-11

**Codex reviewed `f993386` and returned three findings, all accepted by Matt.**

**First, and the behavioral one: `Operating` read only the viewpoint character's own
`Execution.Strategy`, which is the owner's field.** `StrategyInstance` is never copied onto the
executor — `DelegatedToId` on the owner's own instance is the only record of who is carrying a
delegated job — so a man actually doing work handed to him by somebody else read his own
`Execution.Strategy` as null and saw nothing running at all, for the entire life of the instance.
The boundary this milestone protects (an owner sees no progress on delegated work) was intact; its
mirror (an executor sees his own progress on work he is carrying) did not exist. **Fixed by adding a
fallback scan**: when `who.Execution.Strategy` is null, `Operating` now scans `world.Characters`
for the one other character, if any, whose own `Execution.Strategy.DelegatedToId` names `who` — the
only place a delegated instance is indexed, since delegation is a pointer on the owner's record, not
a second copy. `ExecutorName`, `Since` and `Progress` are then derived the same way the codebase
already names "the one doing the work" elsewhere (`Strategies.cs`'s own
`actor.Id != (s.DelegatedToId ?? s.OwnerId)`): progress shown only to `who.Id == (s.DelegatedToId ??
s.OwnerId)`, unconditionally on which of the two characters is asking.

**Second and third, both documentation claims, both already false only in `f993386` itself and
already corrected by unrelated later work — verified rather than re-fixed.** `PlayerSnapshot.cs`
does not, in the current tree or in `f993386`, contain any comment claiming `Since` is a delegation
or handover time; the one place that risk existed was the *rendered wording*, and this milestone's
own "A false line" section above already records that it was caught and fixed before `f993386` was
committed. `PlayerNarration.cs`'s `Past` table comment read "there are seven of them in the whole
game" as of `f993386`, which was accurate for the seven-entry switch at the time; milestone 025 and
026 each added a step phrase to the table without touching the count comment beside it, and by the
current tree the comment already reads "nine" against a nine-entry table (confirmed by counting the
`switch` arms) — so both claims are correct as committed here and needed no code change, only this
record that they were checked rather than assumed.

**Production-path tests, added rather than relying on the staged-world tests above,** because those
vary `StepIndex` by hand-constructing a `StrategyInstance` and cannot by themselves prove the real
pipeline produces a delegated instance an executor can read. `Cast.Build` at day 20 is the fixture
that already does: `The_executor_sees_the_operation_he_is_carrying_with_his_own_progress` reads
Tommy's own view of the operation Vincent delegated to him and asserts his progress is non-null;
`An_unrelated_character_sees_no_operation_in_the_same_natural_run` reads Salvatore's view of the
same run and asserts null. Vincent's side of the same natural run was already covered by
`The_panel_is_populated_during_a_natural_run`. Mutation-checked: reverting `Operating` to the
owner-only lookup made exactly `The_executor_sees_the_operation_he_is_carrying_with_his_own_progress`
fail (`Assert.NotNull() Failure: Value is null`) and no other test in the file, confirmed, then
reverted.

**Both final presentation surfaces protected, each against the section-blind assertion the
`53694a2`/`7036f0d` corrections found missing on the roster panel.** "Bellini's grocery" already
appears in the unrelated belief-list section ("WHAT HE HAS" / "WHAT ... KNOWS") independently of
whether the operation section renders anything, on both surfaces and for both viewpoints — confirmed
by reading the actual rendered output before writing either test, not assumed.

- `IntelligenceWriter`: `The_operation_section_is_isolated_in_the_runners_render_for_both_men`
  (`OperationReadsTests.cs`) locates `WHAT HE HAS OUT`, isolates everything up to the next `HOW HE
  TAKES THEM` header, asserts the false-assurance precondition holds (the target words appear before
  the section starts) and then asserts within the isolated section only, for both Vincent's and
  Tommy's render of the identical day-20 run. Mutation-checked: short-circuiting the operation
  `if` block in `IntelligenceWriter.Render` made this test fail (`the render has no "WHAT HE HAS
  OUT" section`), confirmed, then reverted — `git diff --stat` against `IntelligenceWriter.cs` shows
  no output.
- Godot: `--selftest-operation` (`Game.cs`) starts the real `baseline` session at seed 42 twice —
  once viewpoint Vincent, once viewpoint Tommy — advances each 20 days, calls `Refresh()` explicitly
  (`AdvanceDays` does not trigger the screen rebuild that a button press does; the self-test's first
  run before this fix showed the unchanged start date because of exactly this), reads the live
  `Screen()`, isolates the `DOING` panel from the `WHAT JUST HAPPENED` marker that follows it the
  same way, and checks the same false-assurance precondition before asserting inside the section.
  Mutation-checked: short-circuiting `BuildDoing`'s operation `if` block made the self-test fail
  (`CE-OPERATION FAILED — vincent: DOING section does not contain "Tommy Nardo is handling it" —
  tommy: DOING section does not contain "made his demand"`), confirmed, then reverted — `git diff
  --stat` against `Game.cs` shows no output beyond the additive self-test itself.

### Verification

- Build 0 warnings / 0 errors.
- Tests: **674 passed** — 671 + 3 new (`The_executor_sees_the_operation_he_is_carrying_with_his_own_progress`,
  `An_unrelated_character_sees_no_operation_in_the_same_natural_run`,
  `The_operation_section_is_isolated_in_the_runners_render_for_both_men`).
- `--verify` on all four required configurations, byte-identical to every prior accepted hash:
  `baseline` `7832105EC1F24154`, `disloyal-vincent` `6C23284BFD91C48D`, `resentful-tommy`
  `7A43D1AFB4A6E26F`, `capable-angelo` `5CACCFC566364BB7`.
- `--compare` at seed 42: 6 configurations, 6 distinct traces, 5 distinct chosen-action sequences —
  unmoved from `023-the-roster-reads.md`'s accepted figure.
- Both required viewpoint runs (`disloyal-vincent`/`salvatore`, `baseline`/`vincent`) exit clean.
- All seven Godot self-tests, including the new `--selftest-operation`, and the two-process restart
  proof (`--selftest-restart-save` then `--selftest-restart-load`, genuinely separate invocations)
  exit 0.
- Two mutation checks, each confirmed and reverted: `IntelligenceWriter.Render`'s operation block
  short-circuited (the new xunit test failed for the stated reason), and `Game.cs`'s `BuildDoing`
  operation block short-circuited separately (the new self-test failed for the stated reason). A
  third mutation check — `Operating` reverted to the owner-only lookup — is recorded above, against
  the production-path tests rather than the presentation-surface ones.
- `git diff --stat` against `src/`: `PlayerSnapshot.cs` (the `Operating` rewrite and its doc
  comment), `Game.cs` (additive only — one flag, one dispatch branch, two new methods, no existing
  method body changed), and the two new test files' additions. `PlayerNarration.cs` is untouched, as
  the verification above records.

### Commit

One correction commit, covering the behavioral fix, its production-path and presentation-surface
tests, and this record. Awaits Codex re-review.

## Correction — the delegation-time leak and one operation per executor, on `9ac569b`, 2026-09-11

**Codex reviewed `9ac569b` and returned three findings, all accepted by Matt.**

**First: the executor inherited the owner's pre-delegation `StartedAt`.** The first correction fixed
*whether* Tommy could see the operation at all; it left `Since` reading `s.StartedAt` unconditionally
for whoever asked, which is the owner's own act — when *he* began it, not when it was handed over.
Nothing records a handover time (the milestone's own "A false line" section above already explains
why not), so showing Tommy "running since 2 Mar" would read as *his* tenure on the job when he was
actually delegated on the 14th — a different, still-false version of the exact line this milestone
was built to avoid. **Fixed by making `PlayerOperation.Since` nullable and populating it only for the
owner** (`who.Id == s.OwnerId`), never the executor, with both renderers changed to omit the
"running since" line entirely rather than print an empty one. Confirmed against the live Godot
render: Vincent's panel still reads "running since 2 Mar / Tommy Nardo is handling it"; Tommy's now
reads "getting Bellini's grocery to pay / he has made his demand, and been refused again and again"
with no date line at all.

**Second: nothing enforced that a subordinate could carry only one operation.** `Operating`'s own
fallback scan (added by the first correction) took whichever delegated instance it found first —
silently correct only because nothing stopped a subordinate from being handed a second job while
still carrying a first, or from running one of his own at the same time nobody had checked against.
**Fixed at both places a delegation is created**, not in the projection:

- `Pipeline.AvailableToExecute(World, string)` — one new definition: a candidate is available to
  execute when he owns no strategy of his own and no other character's own instance already names
  him as `DelegatedToId`. Read directly off `World`, the same authoritative footing
  `Pipeline.SubordinatesOf`/`OrgMembersOf` already stand on — who is free to staff is organisational
  bookkeeping, not a character's belief.
- `Generators.FromRelationship` never offers a busy subordinate as a delegate at all — the identical
  shape the acquaintance-boundary correction already established for an unacquainted stranger, now
  reading a new `GeneratorContext.AvailableSubordinateIds`, which `Pipeline.Prepare` populates by
  calling `AvailableToExecute` over `SubordinateIds`.
- `Commit.Apply`'s `DelegateStrategy` case refuses, fail-closed, with the identical check called
  directly against `World` — mirroring the `ConcealIncident` guard already in `StartStrategy` — so a
  hand-built candidate that skipped filtering, or one a future caller generates outside this
  pipeline, cannot bypass the rule.

**Third, and downstream of the second: `Operating`'s own scan now relies on uniqueness instead of
defending against its absence.** With at most one delegated match now genuinely guaranteed, the
scan was rewritten from a `foreach` that took the first hit to `SingleOrDefault`, which throws if
that invariant is ever violated again rather than silently picking one — a wrong answer chosen
quietly here would hide the exact defect this correction exists to make loud.

**Tests, through the production path where the claim is about the natural run and staged where the
claim is about the rule in isolation.** Both existing natural-run tests
(`The_panel_is_populated_during_a_natural_run`, `The_executor_sees_the_operation_he_is_carrying_with_his_own_progress`)
gained a `Since` assertion — Vincent's equal to `Cast.Start`'s date (the exact instant is later the
same day, since the operation starts partway through 2 March rather than at the fixture's own
midnight-adjacent constant; the date is all the rendered text ever carries), Tommy's null. Four new
tests: a subordinate who owns his own strategy is not offered as another operation's executor
(`A_subordinate_who_owns_a_strategy_is_not_offered_as_another_operations_executor`, staged directly
against `Generators.GenerateAll`); a subordinate already carrying somebody else's delegated work is
not offered a second one (`A_subordinate_already_executing_delegated_work_is_not_offered_a_second_delegation`,
staging the first delegation through real `Commit.Apply` calls so the busy state it reads is
genuinely produced); and the fail-closed counterpart of each
(`Commit_refuses_to_delegate_to_a_subordinate_who_owns_a_strategy`,
`Commit_refuses_to_delegate_to_a_subordinate_already_executing_delegated_work`), both asserting
`Assert.Throws<SimulationInvariantException>` against a hand-built candidate.

**Four mutation checks, each confirmed and reverted; the second exposed a genuine gap in the new
test itself, not only in production code.** The fail-closed guard removed from `Commit.Apply`: both
`Commit_refuses_to_delegate...` tests failed with "no exception was thrown," confirmed, reverted.
The `.Where(available.Contains)` clause removed from `Generators.FromRelationship`: the *first*
not-offered test failed as expected, but the second did not — because it never gave Vincent his own
`Execution.Strategy`, so `FromRelationship`'s whole delegate-candidate branch never ran and the
assertion passed vacuously regardless of the filter. **Fixed the test**, not the mutation-check
result: added the missing `vincent.Execution.Strategy = NewStrategy(...)`, re-ran the same mutation,
confirmed both tests now fail for the stated reason, then reverted the mutation. `Operating`'s
`SingleOrDefault` predicate weakened (the `&& candidate.DelegatedToId == who.Id` clause dropped):
`An_unrelated_character_sees_no_operation_in_the_same_natural_run` failed —
`PlayerOperation { Description = getting Bellini's grocery to pay, ExecutorName = Tommy Nardo, ... }`
where `null` was expected, Salvatore wrongly inheriting Vincent's own operation — confirmed, reverted.
This is the mutation check the first correction's own review found missing (see the `REVIEW_LEDGER.md`
correction alongside this one): the earlier claim that all three new production tests were
"independently mutation-checked" was true only of
`The_executor_sees_the_operation_he_is_carrying_with_his_own_progress`; the unrelated-viewer test had
never actually been shown to fail under any mutation before now.

**`GeneratorContext` gained one field, mechanically threaded through every construction site.**
`AvailableSubordinateIds`, added as the record's last parameter specifically so every existing
positional and named construction (`Pipeline.Prepare` plus nine test-local `Context` helpers) needed
only one appended argument rather than a reordering. No test's own intent changed — most pass
`Array.Empty<string>()` (no subordinates modelled in that file at all) or re-derive it from
`SubordinateIds` through `Pipeline.AvailableToExecute` itself, never a hand-rolled copy of the rule.

### Verification

- Build 0 warnings / 0 errors.
- Tests: **678 passed** — 674 + 4 new (two existing tests strengthened with a `Since` assertion
  rather than counted as additions).
- `--verify` on all four required configurations, byte-identical to every prior accepted hash:
  `baseline` `7832105EC1F24154`, `disloyal-vincent` `6C23284BFD91C48D`, `resentful-tommy`
  `7A43D1AFB4A6E26F`, `capable-angelo` `5CACCFC566364BB7`. The availability rule is a no-op against
  every accepted fixture — each delegates exactly once, to a subordinate nothing else has touched —
  so this is the expected result, not merely a hoped-for one.
- `--compare` at seed 42: 6 configurations, 6 distinct traces, 5 distinct chosen-action sequences —
  unmoved.
- Both required viewpoint runs (`disloyal-vincent`/`salvatore`, `baseline`/`vincent`) exit clean.
- All seven Godot self-tests, including `--selftest-operation` re-run against the live render (Tommy's
  panel confirmed to carry no date line at all), and the two-process restart proof exit 0.
- Four mutation checks, each confirmed and reverted, detailed above.
- No simulation behaviour, scoring, RNG, fixture, or accepted hash changed — only the projection
  (`PlayerSnapshot.cs`), the delegation eligibility path (`Generators.cs`, `Pipeline.cs`,
  `Commit.cs`), both renderers, and tests.

### Commit

One correction commit. Awaits Codex re-review.

## Correction — record the rule, close the Pipeline.Prepare gap, fix the review record, on `00613ca`, 2026-09-11

**Codex reviewed `00613ca` — the second correction above — and accepted the broader mechanic as
already correctly implemented: a character may be involved in at most one active operation at a
time, either as its owner or as its delegated executor.** No production behaviour changed by this
correction. Three findings, all accepted by Matt, none of them about the mechanic itself.

**First: the rule had never been written down in `docs/DESIGN_DECISIONS.md`.** It existed only in
`Pipeline.AvailableToExecute`'s own doc comment and this archive's prose. Added a new section,
"Operation staffing," recording the rule itself and a second, related settled point: operation
staffing availability is read as authoritative organisational state for eligibility —
`AvailableToExecute` reads `World` directly, the same footing `SubordinatesOf`/`OrgMembersOf` stand
on — while identity/nameability remains the separate, distinct question `Acquaintance.KnownTo`
settles (milestone 009's second correction). The two filters in
`Generators.FromRelationship` — acquaintance and availability — are independent; either alone is
enough to keep a subordinate off the offered list, and neither implies the other.

**Second: every test proving the rule exercised `Generators`/`Commit` directly against a hand-built
`GeneratorContext`, never `Pipeline.Prepare` — the one place `AvailableSubordinateIds` is actually
computed and wired onto a context a real deliberation uses.** Four new tests drive Vincent through
the genuine pipeline (`Runner.Step`, which calls `Pipeline.Prepare` for the controlled character) to
his own delegation fork and read `PreparedDecision.Available`: a free, nameable subordinate is
offered (the positive control every negative case depends on, since Tommy's absence could otherwise
mean either "correctly excluded" or "the pipeline never offers him at all"); a subordinate owning an
undelegated operation is not; a subordinate carrying delegated work (staged through a real
`Commit.Apply` delegation from Salvatore) is not; and — pinning the broader involvement rule rather
than merely the narrower "not currently a live delegate" reading — a subordinate who owns an
operation and has already delegated it onward to a third man (Tommy hands his own job to Kane) is
still not offered, since `AvailableToExecute` excludes on ownership alone, delegated onward or not.
Mutation-checked as one check against the shared wiring: `Pipeline.Prepare`'s
`subordinates.Where(id => AvailableToExecute(world, id)).ToList()` replaced with the unfiltered
`subordinates`, all three negative tests failed (Tommy wrongly offered in each — confirmed by reading
the failure output, not assumed), the positive control still passed as it must, reverted.

**Third: the review record itself needed two corrections, not the code.**

- **The mutation-check count was overstated.** The prior "### Verification" section above claims
  "Four mutation checks"; the prose immediately before it describes exactly three distinct
  production mutations (`Commit.Apply`'s guard removed; `Generators.FromRelationship`'s availability
  filter removed, run twice because the first run exposed a bug in the *test's own setup* rather
  than in production code, which is a re-run of one mutation after a test fix, not a second,
  independent one; `Operating`'s `SingleOrDefault` condition weakened). No fourth mutation is
  documented anywhere in that section. The accurate count is three. Left in place above rather than
  rewritten, per this file's own practice — this paragraph is the correction.
- **"No simulation behaviour, scoring, RNG, fixture, or accepted hash changed" overstated what was
  actually verified.** The correction added a real new refusal path (`Commit.Apply`'s guard) and
  narrowed what `Generators.FromRelationship` offers — that is a change in the simulation's decision
  machinery, even though it never fires against any accepted fixture. What was actually verified,
  and is the true claim: **no accepted fixture's behaviour, trace hash, or chosen-action sequence
  changed** — the four required `--verify` hashes, the `--compare` figure, and both required
  viewpoints stayed byte-identical, which is what "a no-op against every accepted fixture" (the line
  immediately above it in that same section) already correctly says. Also left in place above.

### Verification

- Build 0 warnings / 0 errors.
- Tests: **682 passed** — 678 + 4 new, all driven through `Pipeline.Prepare` via `Runner.Step`.
- `--verify` on all four required configurations, byte-identical to every prior accepted hash:
  `baseline` `7832105EC1F24154`, `disloyal-vincent` `6C23284BFD91C48D`, `resentful-tommy`
  `7A43D1AFB4A6E26F`, `capable-angelo` `5CACCFC566364BB7`.
- `--compare` at seed 42: 6 configurations, 6 distinct traces, 5 distinct chosen-action sequences —
  unmoved.
- Both required viewpoint runs (`disloyal-vincent`/`salvatore`, `baseline`/`vincent`) exit clean.
- All seven Godot self-tests and the two-process restart proof exit 0.
- One mutation check, against the shared `Pipeline.Prepare` wiring, confirmed and reverted: detailed
  above.
- No accepted fixture's behaviour, trace hash, or chosen-action sequence changed — the correction
  adds a new eligibility rule to the decision pipeline and a design-decision record; it does not
  touch scoring, RNG, or any fixture.
- `docs/PERSONALITY_AND_CHARACTER_PROFILES.md` and `docs/UI_AND_PLAYER_LEGIBILITY.md` untouched;
  milestone 027 and the remaining 023–025 backlog untouched.

### Commit

One correction commit: `DESIGN_DECISIONS.md`, four new `Pipeline.Prepare`-driven tests, and this
review-record correction. Awaits Codex re-review.
