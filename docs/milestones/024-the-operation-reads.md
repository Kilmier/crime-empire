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

## Correction — the review-record misstated what Codex's review of `00613ca` returned, on `dd59a1b`, 2026-09-11

**Codex reviewed `dd59a1b` — the correction immediately above — and returned one P2, accepted by
Matt: this correction's own opening sentence, "Codex reviewed `00613ca`... and accepted the broader
mechanic as already correctly implemented," misstates the review it describes.** The review of
`00613ca` **returned FAIL**. What is true, and what the sentence above collapsed into an unconditional
acceptance, is narrower: Codex confirmed the broader mechanic's implementation was already
correct, and separately found the three documentation-and-coverage gaps this correction's own second
and third paragraphs describe — the confirmation and the FAIL verdict are not in tension, but they
are not the same claim, and reporting only the confirmation lets a reader miss that a FAIL was on
the record at all.

**The accurate sequence:** Codex confirmed the mechanic's implementation was correct while returning
FAIL on the three findings; Matt then accepted those findings and **explicitly authorized the
broader rule as stated** — the rule's own wording above ("a character may be involved in at most one
active operation at a time, either as its owner or as its delegated executor") is Matt's ruling,
carried by this correction, not a fact Codex certified on its own authority.

**No mechanic, test, or production code changed.** This is a documentation-only correction:
`docs/CURRENT_MILESTONE.md`'s matching text fixed in place, since that file is explicitly mutable
rather than history; this archive's own sentence above and `docs/REVIEW_LEDGER.md`'s matching row
left standing, per this project's append-only practice, each followed by a correction naming the
error rather than rewritten silently.

### Verification

- No build, test, or fixture verification applies — no `.cs` file changed. `git diff --stat` against
  `src/` and `tests/` for this commit is empty.
- `docs/DESIGN_DECISIONS.md` untouched, as authorized.
- `docs/PERSONALITY_AND_CHARACTER_PROFILES.md`, `docs/UI_AND_PLAYER_LEGIBILITY.md`, milestone 027,
  and the remaining 023–025 backlog untouched.

### Commit

One documentation-only correction commit, touching only `docs/CURRENT_MILESTONE.md`,
`docs/REVIEW_LEDGER.md`, and this archive. Awaits Codex re-review.

## Correction — a premature, self-referential claim in `ed12b5e`, on `ed12b5e`, 2026-09-11

**Codex reviewed `ed12b5e` — the correction immediately above — and returned one P1, accepted by
Matt: `docs/REVIEW_LEDGER.md`'s own text, written as part of that commit, closed with "Codex accepted
this correction on its own re-review" before any such re-review had occurred.** `ed12b5e` was itself
the correction; nothing in its own text could truthfully report Codex's verdict on it, since no
review of it existed until Codex performed one afterward. The row it pointed to as evidence — the
fourth-review section — covers `dd59a1b`, a different, earlier commit, and could not establish a
result for `ed12b5e`.

**No mechanic, test, or production code changed.** Documentation-only: the premature sentence left
standing in `docs/REVIEW_LEDGER.md` (append-only), followed there by a correction naming it and
stating the true sequence; `docs/CURRENT_MILESTONE.md` updated directly, since it is mutable, to
record that `ed12b5e` received this P1 and that this correction awaits its own review.

**This commit is not described as accepted, verified, passed, or closed anywhere in this record.**
It awaits Codex's own re-review, the same as every correction before Matt confirms it.

### Verification

- No build, test, or fixture verification applies — no `.cs` file changed. `git diff --stat` against
  `src/` and `tests/` for this commit is empty.
- `docs/DESIGN_DECISIONS.md` untouched, as authorized.
- `docs/PERSONALITY_AND_CHARACTER_PROFILES.md`, `docs/UI_AND_PLAYER_LEGIBILITY.md`, milestone 027,
  and the remaining 023–025 backlog untouched.

### Commit

One documentation-only correction commit, touching only `docs/CURRENT_MILESTONE.md`,
`docs/REVIEW_LEDGER.md`, and this archive. Awaits Codex re-review.

## Correction — delegated-execution authority, information boundary, postponement, assignment coherence, and policy-breach identity (the sixth correction), 2026-09-11

**Not yet reviewed by Codex. This section makes no claim that Codex has reviewed, passed, or
accepted this implementation — it records what Matt authorized and what was built and verified
against that authorization, nothing more.**

### What prompted it

A read-only playtest of the delegated `SecureTribute` path (seed 42, baseline) found four defects
in how a delegated operation is handled: Vincent was woken by `StrategyBlocked` instead of the
executor he delegated to; `DoNothing` at a genuine block left the strategy live with no pending step
while the panel still said the executor was handling it; leadership created a second assignment
after the first deadline while the original operation stayed linked to the expired one; and the
panel lost the owner-known method and delegation history once delegated. Matt authorized a revised,
bounded correction rather than the investigation's own proposed remedies verbatim.

### What was authorized and implemented

A shared `Strategies.CurrentExecution(world, actor)` derivation — the owner if he has not delegated,
otherwise whoever it is delegated to — threaded through wake-routing, candidate generation, scoring
and commit for `ContinueStrategy` / `AlterStrategy` / `PostponeStrategy` (new); `DelegateStrategy` /
`AbandonStrategy` stay owner-only. The executor gains firsthand participant knowledge on a refusal;
`ReportToSuperior` remains scored, not automatic; the existing `RevenueShortfall` pressure at
Blocked-time is gated to `owner.Id == executor.Id`, closing a synchronous owner-informed-without-a-
report leak. A `PostponeStrategy` candidate is generated at a genuine block with no pending step, and
the generic `DoNothing` floor candidate is suppressed there. `LeadershipReview` will not create a
second assignment while the officeholder still owns a live strategy in that domain, including a
delegated one. `StartStrategy` now fails closed, both when the actor is already executing delegated
work and when a different `StartStrategy` would overwrite his own still-delegated instance — the
owner must explicitly cancel first. `StrategyInstance.PolicyBreachDecisionMakerId` (new field)
records who chose the currently operative prohibited method, independent of who owns or executes the
operation; delegation and execution never rewrite it, but a genuine later `AlterStrategy` that
changes which prohibited method is operative may.

### The structural finding, substantiated not assumed

`Force` requires `RequiredCrew = 2` (`Generators.Coercive`); every delegate in the current cast —
Tommy, Angelo (capable-angelo) — has crew = 1, while every character who clears crew ≥ 2 — Vincent,
Salvatore — is never a delegate. `Filters.Apply`'s capability stage removes a Force candidate before
scoring, for any seed, because it depends only on which character is deliberating. **Force is
therefore structurally unreachable by any delegate in the current cast — a cast/crew fact, not a
seed-search gap — and Matt ruled that nothing may change (crew, traits, policy, RNG, fixtures,
coefficients) to restore the old natural Force chain.** Separately, `PolicyKind.NoPublicViolence`
gates only on `Force`, not `Threaten` — Threaten is not a policy breach at all — so no delegate can
naturally trigger `PolicyBreachDecisionMakerId` either; a natural proof of "the executor independently
breaches a policy" does not exist in the current cast and had to be staged, honestly labeled as
staged.

### Consequence: 44 pre-existing tests broke, and how each was classified and fixed

None from a coefficient, RNG, or fixture change. Diagnosed read-only, grouped by causal root, before
anything was touched, then remediated per Matt's rulings on the classification:

- **Group A (22 tests, retargeted to 6 retired / 16 preserved).** All sat downstream of a delegate
  autonomously reaching Force in an unstaged run — now impossible per the structural finding above.
  Matt ruled: retire only the tests whose actual claim is natural autonomous delegate-to-Force
  emergence; preserve the rest (provenance/authorship, witness/information-boundary, causal-feedback
  privacy and save/load, investigation question/answer, pronoun/actor rendering, executor-reporting)
  through honest staged origins. Retired, with append-only retraction notes rather than silent
  deletion: `ScenarioReachTests.The_delegator_puts_his_question_to_the_man_he_sent` (4 variants),
  `ScenarioReachTests.And_the_executor_gives_his_delegator_an_account_of_it`, and
  `ExecutorSuitabilityTests.The_natural_runs_chosen_executor_is_who_throws_the_punch`. Preserved
  through narrowly-scoped, feature-family staged origins — no shared helper across families, no
  hand-staged breach flag, no cast/crew/trait/policy/RNG change: provenance
  (`ProvenanceTests.The_author_holds_his_own_order_and_a_witness_does_not_learn_it`),
  investigation/pronoun (`InvestigationTests.cs`'s natural-suspect/answer/allegation tests,
  `PronounTests.A_pending_decision_speaks_of_its_actor_as_themselves`), causal feedback
  (`CausalFeedbackTests.cs`'s "pending vs. declined" section), and reporting
  (`ControlledAutonomousParityTests.Tommys_asked_to_account_decision_resolves_automatically_to_the_identical_report`,
  renamed from "...identical_partial_report" since Tommy's real candor is now Candid). Each stages
  only the originating incident through real `Commit.Apply` calls and lets everything downstream —
  delegation, observation, investigation, the exchange itself — run through the unstaged production
  pipeline.
- **Group B (9 tests + 2 Godot self-tests).** Shared one fixture — the `SevenChoiceSequence` golden
  path, duplicated in `PersistenceTests.cs`, `PlayerOwnedOperationTests.cs` and `Game.cs` — whose
  fourth step had the *owner* alter a delegated operation, exactly the authority this correction
  removed. Re-derived live, not guessed, renamed to `GoldenPathChoiceSequence`: persuade → continue →
  delegate → (Tommy's own escalation and the operation's genuine completion resolve entirely in the
  background, Vincent remaining the sole controlled character throughout) → an unrelated question
  from Salvatore → Vincent starting a fresh cycle once collection has genuinely completed. Persistence,
  restart, determinism, counterfactual, and information-boundary coverage are retained against the
  re-derived sequence; the Godot golden-path and two-process restart self-tests re-derived and
  reconfirmed live. `InPersonTests.A_delegated_threat_is_read_by_the_executor_and_not_the_owner`
  updated the same way — Tommy's escalation now left to resolve autonomously rather than chosen by
  Vincent.
- **Group C (2 tests + 1 new negative test).** `CausalFeedbackTests`' cautious-vincent proof that
  Vincent's answer to Salvatore reaches him within 3 days. Traced: Vincent's answer to the specific
  asked claim never arrives within a full 90-day run; he instead reports a later, superseding
  resolution ("the grocery has paid") that answers a different claim and never satisfies the original
  request. `DESIGN_DECISIONS.md`'s exact-claim resolution rule is correct and untouched — confirmed by
  reading it, not assumed. Fixed by controlling Vincent (not Salvatore) for the one decision that
  matters — Salvatore's own question still arrives entirely unstaged — and choosing the exact-claim
  answer explicitly; the retired premise that the answer "contradicts what the books told Salvatore"
  no longer holds either (it now reads as a corroboration, not a conflict), so the test reads
  resolution and attribution via `Known` rather than `Disagreements`. A new negative test
  (`A_later_report_asserting_a_different_claim_does_not_resolve_the_original_request`) pins the
  permanently-moot case as a real, unchanged production behaviour, and the gap itself is recorded as
  `docs/OPEN_CONCERNS.md` #7, deliberately not fixed here (out of this correction's scope). The
  identical retargeting was applied to the Godot `--selftest-corroboration` self-test.
- **Group D (9 tests, retargeted to 8 + 1 honest negative control).** Natural seed-42
  baseline/cautious-vincent timeline drift in the Salvatore/Vincent/Tommy conflict-and-agreement
  chain, downstream of legitimate pacing changes (postponement, suppressed `DoNothing`, gated
  pressure), traced individually rather than repinned. `RelationalConsequenceTests`'s conflict-count
  theory updated to baseline/watchful-boss/disloyal-vincent/resentful-tommy = 1, cautious-vincent = 0
  — each figure confirmed by an individual run, not inferred from baseline. Mechanism traced: Vincent
  no longer personally runs the delegated operation, so the report that used to contradict Salvatore
  is gone; the one surviving conflict in the four non-collapsed variants is a later, genuine
  assignment reissue. cautious-vincent's own larger collapse (every contradiction source gone, not
  just one) is pulled into its own honest negative-control test,
  `Cautious_vincent_no_longer_produces_a_conflict_to_be_contradicted_by`.
  `AccountAgreementTests.Salvatores_generated_answer_to_tommy_raises_tommys_trust_when_chosen`
  (renamed from "The_natural_seed42_chain_...") traced to Salvatore's own generated-candidate set at
  Tommy's `asked-to-account` wake: the answering candidate is genuinely generated and scores 0.2446,
  losing to an unrelated `SeekCorroboration`-to-Vincent candidate at 0.5849 — generated but not chosen,
  an honest behavioural non-result, preserved by explicitly choosing the real candidate through the
  controlled production pipeline (`Runner.Step`/`Pipeline.Resolve`) rather than routed around.
  `RelationshipReaderTests.The_diagnostic_reports_components_the_reason_list_drops` preserved as a
  focused staged setup on real, run-produced post-conflict trust, driven through the real
  `Utility.Score` boundary — diagnostic projection, not natural emergence, per Matt's own distinction.
- **Group E (1 test).** `ShortfallAttributionTests.A_player_controlling_the_capo_is_offered_the_bakery_once_he_suspects_a_gap`'s
  old trigger for Vincent suspecting a shortfall *was* the synchronous pressure leak this correction
  gated closed. Fixed by staging Salvatore's own organisational inference and its assignment-channel
  disclosure to Vincent — the identical two calls
  `The_assignment_channel_carries_the_boss_suspicion_to_the_capo` already uses — never a fabricated
  Tommy report.
- **Group F (1 test, renamed).** `RelationalConsequenceTests`'s one already-staged denial test
  hand-built a `StrategyInstance` outside `Commit.Apply`, so it could never populate
  `PolicyBreachDecisionMakerId`. Rebuilt through the real `Start` → `Alter(Force)` → `Delegate`
  sequence; Tommy's real score components at the question now favour Candid, not the old fixture's
  False, so the test is renamed
  (`An_executor_who_candidly_confirms_it_is_trusted_more_not_less`) and its assertions rewritten to
  what candour and corroboration production code actually produces, rather than forced back to a
  denial that no longer occurs.

### The policy-breach decision-maker identity pair, and a real bug found while building it

Two required paired tests, added to `InformationTransmissionTests.cs`: Vincent chooses Force then
delegates — he remains decision-maker, carried through to real `PersonBreachedPolicy` self-knowledge
once violence resolves (`Vincent_who_chooses_force_then_delegates_remains_the_decision_maker`);
Vincent delegates Persuade and Tommy independently alters to Force, staged at the `Commit` boundary
since no generator can offer a crew-1 delegate that escalation — Tommy becomes decision-maker,
Vincent gains no self-knowledge of it
(`Vincent_who_delegates_persuade_then_tommy_independently_alters_to_force_leaves_tommy_the_decision_maker`).

Both mutation-checked against wrong-owner and wrong-executor simplifications in
`Strategies.ResolveViolence`'s read site (swapping `decisionMakerId` for `owner.Id`, then for
`executor.Id`): each mutation was caught by exactly one of the two tests. The first test had to be
strengthened mid-check — it originally asserted only the stored field, not the resulting `Cognition`
consequence — because in that form it did not catch the wrong-executor mutation at all; extended to
assert Vincent (not Tommy) gains the real `PersonBreachedPolicy` self-knowledge, it did.

A genuine, pre-existing bug was found in the same pass: `Commit.cs`'s `AlterStrategy` case updated
`PolicyBreachDecisionMakerId` on every breach-bearing alter, including a repeated or no-op one that
left the operative method unchanged — contradicting the field's own doc comment and Matt's ruling that
delegation and execution must never rewrite it. Fixed with a guard (update only on the first breach
ever recorded, or when the method genuinely moves under a breaching candidate) and a new
mutation-checked negative test,
`A_repeated_alter_that_does_not_move_the_operative_method_does_not_rewrite_the_decision_maker`.

### Documentation corrections made in the same pass

`InformationTransmissionTests.cs`'s `StageForceBreach` doc comment, which had drifted to say "delegates
before altering," corrected to state the actual order — it always alters to the prohibited method
before any delegation, for the decision-maker-identity reason the comment itself gives — and a
duplicated summary block left orphaned above the wrong method by an earlier edit was moved back onto
`StageForceBreach`. `docs/OPEN_CONCERNS.md` #7 corrected to state accurately what milestone 018's own
three-times-corrected chain actually rejected — a private-decision leak (reading the asked
character's own `World.Decisions`), and treating a sincere denial as a distinct "Declined" outcome —
neither of which is a moot/supersession alternative, which that chain never considered. Every
`Milestone [N]` placeholder in the production-code comments, `docs/CURRENT_MILESTONE.md`, and the
Godot self-tests replaced with "milestone 024's sixth correction."

### Verification

- Full test suite: **680 passing, 0 failing** (up from 636 passing / 44 failing when this correction's
  implementation first broke them; net-additive, since Group C's negative test and the two
  `PolicyBreachDecisionMakerId` identity tests are new).
- `dotnet run --project src/CrimeEmpire.Runner -- --verify` (seed 42, baseline; also re-run at
  disloyal-vincent and resentful-tommy): identical hashes from two runs each — deterministic.
- `dotnet run --project src/CrimeEmpire.Runner -- --compare` (all six configurations, seed 42): 6
  distinct traces, 5 distinct chosen-action sequences, `violence: none` in every one — independent
  corroboration, at the CLI level, of the Force-impossibility finding.
- Both required viewpoints (`--viewpoint vincent`, baseline; `--viewpoint salvatore`,
  disloyal-vincent) render cleanly with no cross-character leak visible; Tommy's viewpoint checked as
  an additional, non-required sanity pass.
- All nine Godot self-tests pass: `--selftest`, `--selftest-goldenpath`, `--selftest-directaction`
  (its own trailing beats re-derived live — the concealment step now resolves in one pass rather than
  needing a continuation, unrelated to delegation but confirmed by an actual run, not assumed),
  `--selftest-corroboration` (rebuilt the same way as the equivalent xunit tests), `--selftest-tribute`,
  `--selftest-capability`, `--selftest-operation`, `--selftest-restart-save`, and
  `--selftest-restart-load` — the two-process restart proof, the loaded screen byte-identical to the
  golden path's own.
- Mutation checks performed and reverted: the exact-claim request-resolution comparison in
  `PlayerSnapshot.cs` (Group C); the `PolicyBreachDecisionMakerId` guard against both a wrong-owner and
  a wrong-executor simplification in `Strategies.ResolveViolence` (the identity pair above).

**Not yet done, recorded rather than glossed over:** not every one of the roughly thirty rebuilt or
retargeted tests was individually mutation-checked against its own reverted production change — only
the `PolicyBreachDecisionMakerId` pair and Group C's new negative test were. The rest were confirmed
correct against the real, unstaged pipeline (the point of "staged origin, unstaged everything
downstream"), but a mutation check specifically proving each one would catch a regression was not
performed for all of them.

### Scope discipline

`docs/DESIGN_DECISIONS.md`, `docs/PERSONALITY_AND_CHARACTER_PROFILES.md`, and
`docs/UI_AND_PLAYER_LEGIBILITY.md` untouched. Milestone 027 not started; the remaining pre-existing
review backlog (`1a7bcc6`, `95e60b5`) untouched. `docs/ROADMAP.md`'s pre-existing, independently
authored working-tree change (the continuous-calendar finding) preserved exactly and never staged by
this correction.

### Commit

One commit, "milestone 024 sixth correction," covering the production-code changes described above,
every test file listed, and this archive entry. **Awaits Codex's implementation review — not
described as reviewed, passed, accepted, or closed anywhere in this record.**

## Correction — review gaps in the sixth correction (the seventh correction), 2026-09-11

**Codex reviewed `ed7d38a` and returned FAIL with five findings, all accepted by Matt.** This entry
corrects the implementation, missing coverage, missing durable authority, and inaccuracies in the
sixth-correction account above. It does not rewrite that historical text.

### Implementation and coverage corrections

- `Commit.Apply`'s new self-start guard trusted `GeneratorContext.CurrentExecution`, a snapshot from
  preparation time. A direct caller or a world change between prepare and commit could pass null
  while authoritative `World` still named the actor as another owner's delegate, allowing him to
  start a second operation and violate the rule the guard claimed to enforce. The commit boundary now
  calls `Strategies.CurrentExecution(world, actor)` itself and fails closed from that result. A
  regression test deliberately supplies the stale null context and proves neither strategy changes.
- `StrategyInstance.PolicyBreachDecisionMakerId` was persistent, consequence-bearing actor identity
  but absent from both `SimulationReplayTests.Snapshot` and `BehavioralSnapshot`; two worlds that
  would later give policy-breach self-knowledge to different men compared equal. Both comparators now
  carry the field, and an independent paired-world test proves they distinguish only that identity.
- The sixth correction lacked focused acceptance tests for several central claims. New tests now
  drive the real scheduler and decision pipeline through a delegated refusal, proving the block wakes
  Tommy rather than Vincent; Tommy receives Continue/Alter/Postpone, not generic DoNothing; Vincent
  receives none of those executor-owned choices, gains no refusal knowledge, and receives no
  synchronous shortfall pressure; resolving Postpone preserves the same instance and schedules one
  live later step. Separate tests prove the commit-boundary stale-context guard, the owner's
  no-overwrite rule, and both halves of the domain-scoped leadership gate.
- The accepted delegated-authority, information, postponement, assignment-coherence, replacement,
  and policy-identity rulings are now recorded in `docs/DESIGN_DECISIONS.md`. They no longer depend
  on this archive or implementation comments to serve as authority.

### Corrections to the sixth-correction account

- The blanket Group A description was too broad. `CausalFeedbackTests` and
  `ControlledAutonomousParityTests` explicitly stage Vincent's suspicion and question in addition to
  staging the incident/method origin; their own comments disclose that honestly. They still exercise
  real production behavior after those stated seams, but the account's claim that only the
  originating incident was staged and *everything* downstream was unstaged was false.
- “680 passing ... net-additive” was false relative to the sixth correction's parent, which had 682
  tests. The correction retired cases and added/reworked others; 680 was the verified resulting
  total, not a net increase. This seventh correction adds five focused tests, producing 685 total.
- The immutable `ed7d38a` commit message says delegates lack enough **Capital** for Force. The actual
  gate is `RequiredCrew`, as the archive's structural analysis correctly says; “Capital” is a typo in
  the commit message, not a mechanic.
- `PlayerSessionTests.A_delegated_failure_tells_its_owner_nothing_until_somebody_does` manually
  injects the obsolete owner-routed event as a presentation robustness test. Its comment now says so;
  it no longer claims production `Strategies.Blocked` routes that event to the owner.
- `Strategies.ForceReferenceCoercion`'s comment described the old seed-42 Force histories in present
  tense. It now identifies them as the historical evidence used when milestone 020 selected the
  pivot and explicitly notes that the current accepted histories do not reach Force.

### Verification

- Full solution build: **0 warnings, 0 errors**.
- Full test suite: **685 passing, 0 failing**.
- Deterministic runner: baseline `96422513E42FCB10` (repeated), disloyal-vincent
  `CF3E412C9CE67185`, and resentful-tommy `6FDFE3EBACD69294`, all unchanged. `--compare`
  remains 6 distinct traces / 5 distinct chosen-action sequences with violence absent in all six.
  Required Vincent and Salvatore viewpoints, plus Tommy's executor viewpoint, all render cleanly.
- All nine Godot checks pass: the seven ordinary self-tests plus
  `--selftest-restart-save`/`--selftest-restart-load` in separate processes; restart load reaches the
  same 5 April screen as the golden path.
- `docs/ROADMAP.md`'s pre-existing working-tree change remains untouched and unstaged. The two
  off-limits design documents, milestone 027, and unrelated backlog remain untouched.

### Commit

One focused seventh-correction commit. It awaits Codex review and is not described here as reviewed,
passed, accepted, or closed.
