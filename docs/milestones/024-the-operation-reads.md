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
