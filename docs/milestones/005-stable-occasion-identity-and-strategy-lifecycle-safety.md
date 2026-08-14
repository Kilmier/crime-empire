# Milestone 005 — Stable Occasion Identity and Strategy Lifecycle Safety

Status: **closed.** Implemented, verified, and archived in a single commit per Matt's authorization —
no corrective rounds were needed before archiving. Not yet reviewed by Codex; the coverage table in
`REVIEW_LEDGER.md` is the record of when that happens.

## What was attempted

Two interacting defects, both traced during milestone 004 and left deliberately unaddressed there:

1. **Observation and strategy-step RNG were keyed off `ScheduledEvent.Id`**, a counter incremented
   by every scheduling call anywhere in the simulation. Adding or removing any event anywhere
   re-rolled every character's subsequent perception and every subsequent strategy-step outcome.
2. **`ConcealIncident` had no termination condition.** Nothing rejected a candidate to start a
   strategy already running, and nothing at all prevented restarting one that had already finished.

The defects have independent causes — one is a key-derivation defect, the other a missing state
machine — but the first was masking the second: the concealment runaway observed in milestone 004
(`ConcealIncident` restarting roughly fifteen times in `disloyal-vincent`) had stopped only because
event IDs shifted and Tommy's police-interest rolls began to miss, not because anything about
restarting was repaired. Fixing the keying was expected to bring the loop back, which is why both
were scoped as one milestone.

Three rounds of planning revision preceded implementation, each tightening the design: causally
local occasion keys with no global identifier anywhere in the key; an actor-local strategy sequence
threaded through every scheduled step; exact-identity dispatch validation that throws on a stale or
misrouted delivery rather than silently advancing a replacement; redundancy enforced before the
salience cap; and an explicitly labelled MVP rule for concealment covering both the running and the
completed state. The full planning record is in the conversation preceding this archive; only the
final, approved scope is repeated here.

## What was completed

**Occasion identity** (`Domain/Character.cs`, `Domain/ExecutionState.cs`, `Sim/Rng.cs`,
`Sim/ScheduledEvent.cs`):

- `Character.StrategyCount` — monotonic per character, the `DecisionCount` pattern one level down.
- `StrategyInstance.OwnerId` and `LocalSequence` (both init) — instance identity, immutable for the
  instance's life, independent of delegation and of any other character's activity.
- `StrategyInstance.NextAdvanceOrdinal` — the ordinal the next delivered step must carry. Not
  `StepIndex`: `AlterStrategy` legitimately rewinds `StepIndex` to `2` to re-run a step under a new
  method, so a key built from it would redraw identical rolls for the escalated attempt. The ordinal
  never rewinds.
- `Rng.ForOccasion(worldSeed, occasionKey)` over the existing FNV-1a. No algorithm change.
- Keys: `step|{owner}|{sequence}|{ordinal}` and
  `obs|{owner}|{sequence}|{ordinal}|{traceKind}|{observer}`, built at the causal source
  (`Strategies.ScheduleObservation`, `Strategies.Advance`) and carried on `EventPayload`, never
  reconstructed at dispatch. `traceKind` (`"violence"` / `"surveillance"`) distinguishes the two
  reasons the same advance can offer more than one kind of opportunity.

**Dispatch validation** (`Strategies.Advance`): validates `StrategyOwnerId`, `StrategySequence`,
`AdvanceOrdinal`, that the delivered event is the instance's own `PendingStepEventId`, and that the
awakened character is `DelegatedToId ?? OwnerId`. All five must hold. A delivery that fails any of
them throws `SimulationInvariantException` — a new type, since this guards a simulation invariant
rather than a caller contract. `PendingStepEventId` is cleared and `NextAdvanceOrdinal` incremented
exactly once, in that order, before the occasion key is built from the validated (pre-increment)
value.

**Step routing** (`Strategies.ScheduleNextStep`): the one path that may schedule a `StrategyStep`.
Cancels any pending step for the instance before scheduling a new one, so "at most one pending step
per instance" is an invariant rather than an accident of a single-slot field. `Commit.PostponeStrategy`
and `Commit.SeekApproval` previously scheduled directly and never set `PendingStepEventId`, so neither
could be cancelled by `AbandonStrategy`; both are rerouted, with the helper gaining an optional
interval parameter for their non-default delays (7 days, 5 days).

**Replacement hygiene** (`Decision/Commit.cs`, `Strategies.RemoveCommitments`): commitment ids became
instance-unique, `strategy:{OwnerId}:{LocalSequence}`. `Strategies.RemoveCommitments` removes an
instance's commitment from both the owner and, if execution was delegated, the delegate — closing a
leak found during implementation: the delegate's commitment was added at delegation time and never
removed by either `Complete` or `AbandonStrategy`, so a finished delegate went on carrying weight for
work that was already done, and `CommitmentWeight` feeds utility scoring directly.

**Concealment bounding** (`Domain/ExecutionState.AttemptedConcealments`, `Decision/Filters.cs`,
`Decision/Candidate.AboutIncident`): `Commit.StartStrategy` records the incident claim on
`AttemptedConcealments` when a `ConcealIncident` starts. A new Stage 0 — Redundancy in `Filters.Apply`
runs before salience and rejects a `StartStrategy` candidate that either matches the actor's currently
running `(Kind, TargetId)` or names an incident already in `AttemptedConcealments` — the second check
covers the completed state a running-only check cannot express. Running it before the candidate cap
means a redundant duplicate can never occupy one of the bounded slots and crowd out a genuinely
different option; verified directly by mutation (see Tests).

> **MVP rule, not a permanent design commitment.** One `ConcealIncident` attempt per character per
> incident, across running and completed states. A genuinely different incident remains eligible. The
> eventual shape is likely incident-relative state about evidence and exposure in the world rather
> than a private per-character tally — a man who cleans up badly and learns the traces are still
> there has a real reason to go again.

**Observation occasion uniqueness**: verified, not merely assumed. `ResolveViolence` already merges
observers into a dictionary keyed by observer id with best-access-wins ordering, and both observation
call sites schedule at most one opportunity per advance — so `(instance, ordinal, traceKind, observer)`
does identify exactly one opportunity today, and no separate observation ordinal was needed.

## Tests / success criteria and results

`dotnet test` — **161 passing** (was 139): 8 new insertion-stability tests in
`SimulationReplayTests.cs`, 14 new tests in the new `StrategyLifecycleTests.cs`, the existing
behavioural budget extended with a per-character `StrategyCount` bound, and one existing test rewritten
(see Important discoveries). Both `--verify` runs deterministic on all four variants; `--compare`
reports four distinct histories.

**Insertion-stability, the load-bearing property** (`SimulationReplayTests.cs`):
`An_unrelated_cancelled_event_does_not_change_the_history` and
`An_unrelated_truth_log_entry_does_not_change_the_history`, each run across all four variants. Neither
uses the existing `Snapshot()` comparator — that comparator deliberately bakes in `ScheduledEvent.Id`
(via `DecisionRecord.TriggerEventId`) and lets `Claim.ToString()` print a `WorldEvent`-derived
`EventId` inside report and candidate text, both of which are *expected* to shift under either
perturbation and would fail the test even when the fix is correct. A new `BehavioralSnapshot()`
compares only fields provably free of any global counter — which candidate kind/strategy/method/target
was chosen, final business and character state, and cognition content by kind/subject/object/stance/
confidence/source. **Mutation-checked**: reverting either the observation key or the strategy-step key
back to an `ev.Id`-based form failed these tests in three or all four variants respectively; both were
then restored and reconfirmed passing.

**Stale-event validation** (`StrategyLifecycleTests.cs`): one positive-control fact
(`A_valid_step_event_advances_without_throwing`, asserting the ordinal increments exactly once and
the pending id changes), five facts each corrupting exactly one of the five validated fields
(nonexistent owner, stale sequence, stale ordinal, wrong pending-event id, wrong executor), a fact
proving a properly cancelled step is skipped by `EventQueue` and never reaches `Advance`, and a fact
constructing a genuinely stale event by hand (standing in for whatever upstream failure could produce
one) and confirming it throws against a replacement instance rather than advancing it.

**Concealment redundancy** (`StrategyLifecycleTests.cs`): `Commit` and `Filters` driven directly
rather than through the full pipeline's utility competition — deliberately, since "does concealment
win the competition" is a separate, out-of-scope tuning question from "can it be offered again once
tried." One test proves the rule holds in both the running and the completed state; a paired positive
control proves a genuinely different incident remains eligible; a crowding test proves the redundancy
stage runs before the candidate cap, **mutation-checked** by moving the exclusion after the cap and
confirming the passed-candidate count drops from six to five with the redundant candidate having
consumed a slot; a wiring test confirms the generator actually names the incident a candidate is
about. The redundancy rule's own removal was also mutation-checked directly: disabling the
completed-state branch failed the running/completed test as expected.

**Delegate commitment cleanup**: two facts prove `RemoveCommitments` clears the instance's commitment
from both the owner and the delegate, on completion and on abandonment respectively.

**Replay snapshot extended**: `StrategyCount`, `LocalSequence`, `NextAdvanceOrdinal`,
`PendingStepEventId`, and `AttemptedConcealments` are now in `Snapshot()`'s character line, so the
existing same-seed and pause/resume determinism tests cover them without a dedicated test.

## Important discoveries

**The scenario does exercise the concealment runaway's cause, but not its natural resolution.** At
seed 42, the `"clean up after the incident before anyone else does"` candidate is generated and scored
six times across the `disloyal-vincent` run — it is genuinely considered — but never wins the utility
competition, so it is never chosen and the termination rule is never naturally exercised. This
confirms the plan's own prediction that a dedicated, seed-independent reproduction was necessary rather
than optional: the accepted scenario does not, on its own, prove the fix works. `cautious-vincent`
never proposes it at all, since Vincent never uses force in that variant and no `LegalExposure`
pressure is ever generated.

**`cautious-vincent` is an unplanned control group, and it is the cleanest evidence the fix is
correctly scoped.** It is the one variant where Vincent never uses force, so `ResolveViolence` and
every observation opportunity it schedules never fire at all. Its decision count is **16 in both the
pre- and post-fix code**, byte-for-byte the only variant unmoved. The three variants that do trigger
observation rolls moved by comparable, non-arbitrary amounts (below).

**A previously undetected delegate-commitment leak.** Found while making commitment ids
instance-unique (a change the plan already called for): a delegate's commitment, added at delegation
time, was never removed on completion or abandonment — only the owner's was. Since
`ExecutionState.CommitmentWeight` feeds utility scoring, every delegate in every prior run had been
carrying dead weight from finished work indefinitely. Fixed as part of `RemoveCommitments`; not a
pre-existing test gap this milestone was scoped to hunt for, but adjacent enough to the exact code
being touched that leaving it would have been leaving a known defect in place.

## Behavioural movement and causal accounting

All four baseline hashes moved, as expected and pre-authorized. Decision counts:

| Variant | Before | After | Δ |
|---|---|---|---|
| baseline | 13 | 33 | +20 |
| cautious-vincent | 16 | 16 | +0 |
| watchful-boss | 13 | 33 | +20 |
| disloyal-vincent | 19 | 34 | +15 |

Report counts: baseline 2→10, disloyal-vincent 2→10, cautious-vincent unchanged at 2.

Five named causes were identified at planning time. Stated honestly: causes 2 through 5 are each
independently verified by a dedicated, mutation-checked test and do not individually explain much of
the volume above. The overwhelming majority of the movement traces to **cause 1 — the new occasion
keys redrawing every observation roll.** The clearest concrete instance: in the post-fix baseline,
Kane (the detective) successfully starts `InvestigateIncident` — `began InvestigateIncident(harbour,
target=bellini-grocery, method=Persuade)` — which never happens anywhere in the pre-fix trace at this
seed. Her investigation depends on holding a `WitnessSawIncident` claim, acquired only through an
observation roll that, under the old keying, was exposed to the same accidental-miss risk milestone
004 diagnosed for Tommy's police-interest rolls. Under stable keys the same roll now lands, and from
that one redrawn coin flip the entire downstream chain differs deterministically: what Kane comes to
believe, what she reports, who asks her about it, what they in turn report. This is not a defect —
redrawing rolls exactly once, permanently, is the explicit and intended cost of making them
insertion-stable — but it is why a line-by-line attribution of all ~30 decisions in a variant to one
of five discrete causes is not attempted here: once an early roll differs, the deterministic pipeline
guarantees everything causally downstream of it differs too, by construction, and that cascade *is*
cause 1, not five separate causes acting independently.

Causes 2 (step routing through one helper), 3 (stale-event elimination), 4 (delegate commitment
cleanup), and 5 (concealment bounded) are real and each demonstrated by its own test, but none of them
fired in a way that changed which candidate won in this specific accepted scenario — 5 in particular
was shown above to be inert here. What they change is guaranteed by the tests, not read off this run.

The behavioural budget (decisions < 100, reports < 25 per variant, `StrategyCount` < 10 per character)
holds comfortably for all four variants under the new counts.

## Deferred work

- **The empty-domain label**, `ConcealIncident(, target=...)`, remains unaddressed. Confirmed during
  implementation not to affect incident identity — identity is the incident `Claim`, not the domain
  string — so it stayed out of scope per Matt's ruling.
- **The concealment MVP rule's eventual replacement** with incident-relative state about evidence and
  exposure in the world, as flagged in what was completed above.
- A scenario variant that makes `ConcealIncident`'s termination win the utility competition naturally,
  so the rule is visible in play and not only in the dedicated tests and mutation checks above — the
  same category of gap milestone 004 left for its own central distinction.
- The `FirstHandTestimony`/`Discovery` suspicion discounts remain tuning guesses, untouched by this
  milestone.

## Relevant commits

The single implementation-and-archive commit that introduced this file. Not cited by hash here, for
the same reason milestone 001's archive gives: a commit cannot contain its own hash.
`git log --diff-filter=A -- docs/milestones/005-stable-occasion-identity-and-strategy-lifecycle-safety.md`
resolves it.
