# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**Milestone 005 — Stable Occasion Identity and Strategy Lifecycle Safety — is active.** Matt
authorized the scope below on 2026-08-14 after three rounds of planning revision. No code has been
written yet.

Milestones 001–004 are complete and accepted. Review coverage through the ledger's checkpoint is in
`REVIEW_LEDGER.md`, which is the authority on what has been looked at and what it concluded; do not
infer status from prose anywhere else, including this file.

## Why this milestone exists

Two defects, one cause. Observation rolls and strategy-step rolls are both seeded from
`ScheduledEvent.Id`, a counter incremented by every scheduling call anywhere in the simulation — so
adding or removing any event re-rolls every character's perception. And `ConcealIncident` can be
restarted indefinitely, because nothing rejects a candidate to start a strategy already running, and
nothing at all prevents restarting one that has finished.

The second defect is currently invisible only because the first is hiding it: the concealment loop
stopped when event IDs shifted and Tommy's police-interest rolls began to miss. Fixing the keying is
expected to bring the loop back, which is why both belong in one milestone rather than two.

## Scope — in

**Occasion identity.** No RNG key may contain a global monotonic identifier — not
`ScheduledEvent.Id`, not `WorldEvent.Id`, not `Claim.EventId`. Those remain for trace, history and
incident identity only.

- `Character.StrategyCount`, monotonic per character, exactly as `DecisionCount`.
- `StrategyInstance.OwnerId` and `LocalSequence` (both init) — instance identity is
  `(OwnerId, LocalSequence)`.
- `StrategyInstance.NextAdvanceOrdinal` — the ordinal the next advance will consume. Named for which
  side of the increment it is on. `StepIndex` is **not** part of any key: `AlterStrategy` rewinds it
  to `2` on escalation, so a key containing it would redraw identical rolls.
- `Rng.ForOccasion(worldSeed, key)` over the existing FNV-1a. No algorithm change.
- Keys: `step|{owner}|{sequence}|{ordinal}` and
  `obs|{owner}|{sequence}|{ordinal}|{traceKind}|{observer}`, built at the causal source and carried
  on the payload — never reconstructed at dispatch.

**Dispatch validation.** `Strategies.Advance` validates all five of: `StrategyOwnerId`,
`StrategySequence`, `AdvanceOrdinal`, `PendingStepEventId == ev.Id`, and awakened executor equals
`DelegatedToId ?? OwnerId`. After validation it clears `PendingStepEventId` and increments
`NextAdvanceOrdinal` exactly once. A delivered stale or misrouted step throws
`SimulationInvariantException` — this is a development kernel and a stale delivery is a real bug.
Properly cancelled events are still skipped safely by `EventQueue` and never reach the check.

**Step routing.** `Strategies.ScheduleNextStep` becomes the only path that schedules a
`StrategyStep`, and cancels any existing pending step first. `Commit.PostponeStrategy` and
`Commit.SeekApproval` currently bypass it and never set `PendingStepEventId`, so neither can be
cancelled today; both are rerouted.

**Replacement hygiene.** Commitment ids become instance-unique, `strategy:{OwnerId}:{LocalSequence}`,
and replacement, abandonment and completion clean up by that identity. This also closes a leak: the
delegate's commitment is added to the subordinate and never removed by either `Complete` or
`AbandonStrategy`, and `CommitmentWeight` feeds utility, so it has been biasing his decisions.

**Concealment bounding.** `ExecutionState.AttemptedConcealments` records the incident claim at start.
A new Stage 0 — Redundancy in `Filters.Apply` rejects the duplicate **before** the bounded candidate
cap, with an explicit reason, so an invalid duplicate cannot crowd out a valid option.

> **MVP rule, not a permanent design commitment.** One `ConcealIncident` attempt per character per
> incident, across running and completed states. A genuinely different incident remains eligible.
> This is the smallest rule that terminates the loop. The eventual shape is likely incident-relative
> state about evidence and exposure in the world rather than a private per-character tally — a man
> who cleans up badly and learns the traces are still there has a real reason to go again. Revisit
> when concealment gets its own scope.

**Observation occasion uniqueness.** `(instance, ordinal, traceKind, observer)` identifies exactly
one merged opportunity. True in the current implementation — `ResolveViolence` merges observers into
a dictionary with best-access-wins, and both observation sites schedule at most once per advance — so
no observation ordinal is needed. A run-wide test pins the property; if it is ever violated, the
fallback is a causally local observation ordinal appended to the key.

## Scope — out

General RNG redesign beyond this defect; the xorshift32 and FNV-1a algorithms themselves;
`Pipeline`'s decision keying, which is already stable; new gameplay systems; threshold and discount
tuning; relationship work; relevance tiering; persistence; Godot; any milestone-004 information-model
change.

**Deferred:** the empty-domain label `ConcealIncident(, target=…)`. It does not affect incident
identity, which is the incident `Claim`. If implementation shows otherwise, stop and report rather
than widening scope.

## Expected behavioural movement

All four baseline hashes will move. Five causes, each to be accounted for **separately** in the
archive rather than as one aggregate claim:

1. new RNG identity — every observation and step roll redrawn once;
2. step routing — postpone and approval now cancel and reschedule through the helper;
3. stale-event elimination;
4. delegate commitment cleanup — scores move because `CommitmentWeight` feeds utility;
5. concealment bounded — the loop is expected to return under stable keys, then be held to one
   attempt per incident.

Verification diffs the full decision and event stream against a stashed pre-change build. Every
changed line must attribute to one of the five. Anything unattributable is a finding, not a baseline
update.

## Success criteria

1. No RNG key contains `ScheduledEvent.Id`, `WorldEvent.Id`, or `Claim.EventId` — asserted by a test.
2. A cancelled unrelated event, and an unrelated truth-log record, each leave every causal occasion's
   outcome identical.
3. No `StrategyStep` can advance an instance other than the exact one it was scheduled for; each of
   the five validation checks has its own throwing test, plus a positive control proving a properly
   cancelled event does not throw.
4. `NextAdvanceOrdinal` increments exactly once per delivered step and never on a rejected one.
5. Every `StrategyStep` in the codebase is scheduled through the one helper.
6. The same incident starts `ConcealIncident` once across running and completed states; a different
   incident remains eligible, proved by a paired positive control.
7. No duplicate observation occasion key in any variant.
8. Same-seed determinism and pause/resume equivalence in all four variants, with `StrategyCount`,
   `LocalSequence`, `NextAdvanceOrdinal`, `PendingStepEventId` and `AttemptedConcealments` in the
   replay snapshot.
9. Every hash and count change attributed to one of the five named causes.
10. Build clean; any change to an existing test recorded and justified, not quietly adjusted.

## Delivery

One coherent milestone, **one final implementation-and-archive commit**. Run the full verification in
`AGENTS.md`, including the four information-channel commands, before committing. Do not begin
milestone 006; wait for Codex review and Matt.

## Ordered review process

Review is **manual**. There is no monitor running on a timer, no checkpoint the repository keeps for
itself, and nothing that will notice a commit unless somebody points a review at it.

- Matt takes commits in order, oldest unreviewed first, one at a time.
- Each review names the exact commit whose diff was inspected.
- A later documentation commit does not stand in for the implementation commit beneath it. If two
  land back to back, both still need reviewing, in order.
- The coverage table in `REVIEW_LEDGER.md` is the record. It is maintained by hand, which is why it
  is the authority rather than the prose around it.

Never write "verified" or "closed" from a review report alone. A report must name the exact commit
reviewed, and Matt must confirm acceptance. That rule exists because the record twice claimed a
verification that had not happened.

## Carried forward

Open items this milestone does not address. Fuller versions live in the milestone-004 archive and
`ROADMAP.md`'s technical-debt list.

- **The scenario does not exercise milestone 004's central distinction.** No variant contradicts a
  delegator's first-hand account, so the difference between authored participation and being told is
  provable in unit tests and invisible in play. A variant where Tommy denies to Vincent that he
  touched the place would exercise it.
- **Tuning guesses**: the `FirstHandTestimony` suspicion discount of `0.15` and the `Discovery`
  discount of `0.10` are not derived figures.

## Longer-standing deferrals

- relationship-schema design, likely the next substantial design pass;
- relevance tiering and its continuous-calendar engineering risk;
- persistence and SQLite;
- Godot / `net10.0` compatibility;
- generalized rumor, evidence, prosecution, media, and public-information channels;
- broader organizations, diplomacy, careers, corruption, and surveillance systems;
- the redundant test-project target framework, unless Matt separately authorizes a
  documentation/maintenance change.
