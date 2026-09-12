# Current Milestone

Mutable and intentionally short. This is the sole active handoff surface; completed history belongs
in `docs/milestones/`, review coverage in `REVIEW_LEDGER.md`, and future candidates in `ROADMAP.md`.

## Status

**Milestone 027 — The session has an ending — is authorized by Matt as of 2026-09-12.**
Implementation has not begun.

The historical chronological review gate is closed through exact commit
`8651ecd1c3b34fc30adec98637cedbaf265f8d14`. Astra independently reviewed
`891368d8d2dcac59ef817e110c9317319a79e65b` and `8651ecd1c3b34fc30adec98637cedbaf265f8d14`
as separate Class B exact commits, returned PASS with no findings on each, and Matt accepted both
outcomes on 2026-09-12. Earlier FAIL outcomes remain historical FAILs; later corrections did not
retroactively convert them to passes. Full coverage is in `docs/REVIEW_LEDGER.md`.

This Class B transition commit must receive its own independent exact-commit review before milestone
implementation begins. It cannot record its own review outcome.

## Feature intent

Give the bounded harbour demo a beginning-to-end scale without creating a universal win condition or
a verdict on the controlled character. From session start, the player sees one out-of-fiction
objective:

> Bring the harbour shortfall under control before this 90-day session ends.

At `Cast.Start.AddDays(90)` — 31 May 1987 at 08:00 UTC — after every event and controlled decision
due at or before that instant has completed, the session resolves:

- `ObjectiveMet` when `RevenueLoss < Organization.SignificantRevenueLoss`;
- `ObjectiveUnmet` at or above that threshold, including equality.

The result is scenario-scoped and viewpoint-neutral. Before resolution, no authoritative live
progress meter is exposed; the player's in-fiction understanding remains limited to the ordinary
`PlayerSnapshot`. Resolution may disclose only the one-bit scenario outcome in a clearly
out-of-fiction block.

Natural seed-42 evidence to preserve and prove through the real session path: `cautious-vincent`
collects on 7 May and ends at `RevenueLoss 0.15` (`ObjectiveMet`); `baseline` reaches agreement on
29 May without collection by the deadline and ends at `RevenueLoss 0.90` (`ObjectiveUnmet`). It is an
honest non-result if a particular controlled route, seed, or future variant reaches only one outcome;
do not tune psychology, resistance, timing, fixtures, or coefficients to manufacture both.

## Approved rulings R1–R6

Matt approved Astra's six recommended rulings on 2026-09-12:

1. **R1 — Objective boundary.** Use one scenario-scoped, out-of-fiction objective visible from the
   beginning, with no live truth-based progress meter.
2. **R2 — Name and threshold.** Name it “Bring the harbour shortfall under control.” Met means
   strictly below the existing `Organization.SignificantRevenueLoss` threshold. Keep that constant's
   current meaning; introduce no new coefficient.
3. **R3 — Terminal instant.** Resolve at exactly `Cast.Start.AddDays(90)`, not at the end of that
   calendar date. Process every event and required choice at or before the instant first.
4. **R4 — Result language.** Use `ObjectiveMet` / `ObjectiveUnmet`, never win/loss or a verdict on the
   controlled character. Unmet ends this demo session but is not canonical campaign defeat.
5. **R5 — Finished-operation history.** Remove it from Milestone 027 and return it to the legibility
   backlog.
6. **R6 — Finality.** After resolution, no further simulation step, advance, or choice is allowed.
   Saving and loading a resolved session remains allowed and must reproduce the same result.

## In scope

- one immutable session objective brief, available from session start;
- one session-level terminal evaluation at the fixed inclusive deadline;
- resolved session status and immutable objective-result data separated from `PlayerSnapshot`;
- finality guards on every simulation-input path, applied before mutation;
- save/load before, during, and after the terminal boundary;
- Godot presentation of the objective and both terminal outcomes, including disabled terminal
  controls after resolution;
- the smallest implementation on the existing session, queue, persistence, projection, and Godot
  boundaries; no generic objective framework.

## Required proof

- The public session and compiled Godot surface show the objective name and deadline before time
  advances; removing the brief fails the rendered proof.
- Watch-only seed-42 `baseline` naturally resolves Unmet and `cautious-vincent` naturally resolves
  Met, without staged world state or tuning.
- Focused threshold tests cover below, equal to, and above the existing `0.35` threshold; equality
  remains Unmet.
- Every event at or before the deadline is drained. A controlled decision exactly at the deadline
  remains pending until answered, then the same terminal path resolves the session.
- Oversized `AdvanceDays`, `AdvanceTo`, and `StepEvent` calls stop at the boundary; an event after it
  remains queued.
- Every advance and choice method refuses after resolution before mutation; world and session
  fingerprints remain unchanged after each refusal.
- The result stays separate from `PlayerSnapshot`; no character cognition, another character's
  private state, or raw `RevenueLoss` reaches the player-facing account.
- Controlled, automatically resolved, watch-only, and alternate-viewpoint sessions use the same
  terminal path and produce the same scenario result from the same history.
- Save/load immediately before the deadline, at a deadline choice, and after resolution reproduces
  status, result, queue, trace, and snapshot exactly.
- All six current variant trace hashes and chosen-action sequences remain unchanged. Any movement
  needs a specific authorized explanation and is presumptively a defect. Godot-rendered output is
  expected to change because the objective and ending become visible.
- A compiled Godot self-test exercises the opening brief, both outcomes, terminal controls, and
  post-resolution load; snapshot-only unit tests are insufficient.

## Explicitly deferred or out of scope

- **Finished-operation history is explicitly deferred** to the legibility backlog. Do not add an
  operation-history record or panel in this milestone.
- Clock animation, adjustable speed controls, “time passed” presentation, and pacing changes remain a
  separate clock-legibility milestone.
- `Rng.ForDecision`, arrest, territory, money sinks, succession, rival content, and other layer-three
  systems remain deferred.
- No generic objective registry, multiple-objective system, score, player-specific result,
  `EventKind.SessionDeadline`, objective claim, or cognition write.
- Do not alter unrelated roadmap or proposal work as part of this milestone.

## Current gate

Stop after this authorization transition. After an independent reviewer passes this exact Class B
commit and Matt accepts that outcome, implement only the scope above, run the full Class A
verification it requires, commit one coherent milestone diff with a new append-only milestone-027
archive, and stop for independent review. Do not begin layer 2.
