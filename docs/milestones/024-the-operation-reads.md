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
