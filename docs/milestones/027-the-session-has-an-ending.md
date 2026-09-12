# Milestone 027 — The Session Has an Ending

Authorized by Matt on 2026-09-12 after independent scope review and after he accepted the Class B
transition commit `608f05ef0e67149ba64a6ec50585fd5df6a30506`. The implementation began only after
that commit's exact review outcome was accepted.

## What this milestone was for

Give the bounded harbour demo a beginning-to-end scale without creating a universal win condition or
a verdict on the controlled character. From the opening instant the interface presents one
out-of-fiction objective:

> Bring the harbour shortfall under control before this 90-day session ends.

At exactly `Cast.Start.AddDays(90)` — 31 May 1987 at 08:00 UTC — after every event and controlled
choice due at or before that instant is complete, the session resolves. `RevenueLoss` strictly below
the existing `Organization.SignificantRevenueLoss` value (`0.35`) produces `ObjectiveMet`; equality
or anything above it produces `ObjectiveUnmet`. The result ends this demo and is not canonical
campaign victory or defeat.

## Human rulings implemented

1. The objective is scenario-scoped, out of fiction, and visible from the start, with no live
   truth-based progress meter.
2. Its name is “Bring the harbour shortfall under control”; the existing significance threshold is
   reused without a new coefficient.
3. The terminal instant is exactly `Cast.Start.AddDays(90)` and inclusive of every event and choice.
4. Result language is `ObjectiveMet` / `ObjectiveUnmet`, never win/loss or character judgment.
5. Finished-operation history is not part of this milestone.
6. No simulation step, advance, or choice is accepted after resolution; save/load remains available.

## What was completed

**One session boundary, not an objective framework.** `SessionObjective`, `SessionResult`, and
`ObjectiveOutcome` are small immutable session types. `SimulationSession` owns one fixed objective
and evaluates one one-bit result from authoritative organization state only after its inclusive
deadline has drained. No `EventKind.SessionDeadline`, generic registry, score, cognition write,
objective claim, or world mutation was added.

**Inclusive event-driven finality.** `AdvanceDays` clamps before date arithmetic, so even
`int.MaxValue` cannot overflow past the boundary; `AdvanceTo` clamps any later horizon; and
`StepEvent` remains “next event” within the scenario while treating the deadline as its maximum.
Single stepping at the deadline drains every event at that same instant rather than stopping after
the first. If that event pauses for a controlled decision, the session remembers the terminal
horizon; answering resumes the same-instant drain before evaluation. Events after the boundary stay
queued. Every advance and choice entry point checks terminal state before mutation.

**Information boundary.** The objective and result are explicitly out-of-fiction session metadata,
separate from `PlayerSnapshot`. The result carries only `ObjectiveOutcome` and `ResolvedAt`: no raw
`RevenueLoss`, score, progress, character-specific state, or path back to the world. The existing
snapshot projection and actor-neutral decision path are unchanged.

**Replay-backed persistence.** `PersistentSession` passes the objective/result through and continues
to log only successful inputs. Replaying a save immediately before the deadline, while a
deadline-bound fast-forward is paused for a controlled choice, and after resolution reproduces the
complete wrapper fingerprint, including command log, pending horizon, queue, trace, snapshot, and
terminal result. Rejected post-resolution calls never enter the replay log. The separate
session-level inclusive proof places a controlled choice at the exact deadline; the accepted natural
fixtures do not happen to put a player choice at that instant, and no psychology, timing, or fixture
was tuned to manufacture one.

**Godot presentation.** A compact `SESSION OBJECTIVE` block sits outside the character-facing
columns from the opening screen onward, showing the objective and exact end instant without progress.
At resolution it becomes `OBJECTIVE MET` or `OBJECTIVE UNMET`; the decision panel becomes
`SESSION ENDED`; all three clock controls remain visible but disabled. Save and Load remain enabled.
The new `--selftest-ending` flag drives real buttons through natural seed-42 `baseline` Unmet and
`cautious-vincent` Met endings, saves and reloads the resolved baseline, and reads the live compiled
scene tree for the opening brief, both outcomes, control state, and absence of raw progress.

## Proof and verification

- Full solution build: **0 warnings, 0 errors**, both simulation/persistence target frameworks and
  the compiled Godot project.
- Full test suite: **707 passed, 0 failed**. The 16 new tests cover opening metadata; both natural
  outcomes; below/equal/above threshold; inclusive same-instant draining; an exact-deadline
  controlled choice; all three oversized advance forms; immutable refusal after resolution on both
  session and replay wrapper; snapshot separation; actor/viewpoint parity; and the three persistence
  boundary positions with complete deep fingerprints.
- Required runner determinism, all unchanged:

| Variant | Trace hash | Chosen-action hash |
|---|---:|---:|
| `baseline` | `92F742E3CB85E54B` | `BC280412B238B49F` |
| `cautious-vincent` | `957DAC26D3DCBEF5` | `37640788CD6BA71B` |
| `watchful-boss` | `38D0C93FB5F6B0AF` | `BC280412B238B49F` |
| `disloyal-vincent` | `455A684A29A5F717` | `90C660AFB38741BB` |
| `resentful-tommy` | `ADC3F2DDF1A9D50C` | `BC280412B238B49F` |
| `capable-angelo` | `842B0968FB0388E9` | `2D16B6CD6153037C` |

  `--compare --seed 42` remains 6 configurations, 6 distinct traces, and 4 distinct chosen-action
  sequences. The required `disloyal-vincent`/Salvatore and `baseline`/Vincent viewpoint runs exit 0.
- Godot headless checks all exit 0 after the final production change: `--selftest`,
  `--selftest-goldenpath`, `--selftest-directaction`, `--selftest-corroboration`,
  `--selftest-tribute`, `--selftest-capability`, `--selftest-operation`, the new
  `--selftest-ending`, and the genuinely separate `--selftest-restart-save` then
  `--selftest-restart-load` processes.
- Three focused production mutations were confirmed and reverted: changing `<` to `<=` fails the
  equality threshold case; restoring the old one-event return fails the exact-instant drain; and
  removing the remembered horizon at an exact-deadline pause fails post-choice resolution.
- `git diff --check` passes.

## Important discoveries and limits

- `StepEvent` could not merely replace `DateTime.MaxValue` with the deadline. Its ordinary
  one-event return would strand a second same-instant event and could evaluate too early; the return
  is therefore suppressed only at the terminal instant.
- A choice reached by a one-event step normally has no outstanding horizon. At the deadline it must
  retain one specially so the answer drains same-instant consequences and reaches the shared result
  path. This is session scheduling state, not a second choice implementation.
- One older test helper asked for 90 additional days from a date already inside the run. It now
  bounds that request at the public objective deadline; the tested causal outcome still occurs and
  the full suite remains green.

## Deferred

- Finished-operation history remains in the legibility backlog.
- Clock animation, adjustable speed, a rendered sense of elapsed quiet time, and pacing changes.
- `Rng.ForDecision`, arrest, territory, money sinks, succession, rival content, and other later
  simulation layers.

## Commit

One implementation-and-archive commit containing this file. It is Class A, unreviewed, and
unaccepted; the implementer has not recorded an independent review of their own work.
