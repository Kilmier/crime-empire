# Milestone 019 — Controlled/Autonomous Actor Parity Is Pinned

Authorized by Matt to resolve the controlled-versus-autonomous decision anomaly recorded in
`docs/milestones/018-the-player-can-see-what-their-choice-did.md`'s Finding 3 proof-gap section and
carried into `ROADMAP.md`: at Tommy's natural first asked-to-account decision in the baseline
seed-42 scenario, controlling him and immediately calling `SimulationSession.ResolveAutomatically()`
was reported to pick a self-protective partial report, while the fully autonomous run answered
candidly at what appeared to be the identical decision. The full authorizing text — including
required work items, guardrails, and the required verification suite — was written into
`docs/CURRENT_MILESTONE.md` before implementation began and is not reproduced here in full.

## What this milestone found

The first required step was to reproduce the anomaly through the real session and decision
pipeline before touching anything, and to identify the first actual divergence rather than
inferring a cause from the different final choice. It was reproduced — and it did not diverge.

**The exact cited repro.** `SimulationSession.Start(42, "baseline", "tommy", "vincent")`, advanced
to Tommy's first pause — his genuinely first-ever deliberation, `DecisionCount` 0→1, the
"asked-to-account" `RoleReview` trigger on 1987-04-04 — was compared field by field against
`Cast.Build(42, "baseline")` run fully autonomously (`Runner.Run`, `controlledCharacterId: null`) to
the same event. `PreparedDecision.Scored` (all six candidates, same ids, same totals, same `Candor`)
and `Chosen` (`answer:vincent:PersonUsedViolence(tommy -> bellini-grocery#11):partial`, `Candor =
Partial`) were byte-identical between the two paths. The fully autonomous run does not answer
candidly at this decision — it produces the identical self-protective partial report the controlled
path does.

**Why milestone 018 believed otherwise.** The cited evidence for "candid" —
`ScenarioReachTests.And_the_executor_gives_his_delegator_an_account_of_it` — only asserts that a
report exists whose `Report.AnsweringClaim` matches the question's claim.
`Org.Reporting.Compose` sets `Report.AnsweringClaim` from `Candidate.AnsweringClaim`
unconditionally, before the branch that decides `Candor`, so a `Partial` report that withholds
precisely the asked claim still carries the matching `AnsweringClaim`. That test passes identically
whether Tommy answers candidly or partially and never actually distinguished the two. The actual
committed regression test for this decision,
`Request_disposition_computation_is_identical_whether_declining_was_autonomous_or_player_chosen`
(since renamed to `Request_outstanding_status_is_identical_whether_the_asked_persons_choice_was_
autonomous_or_player_chosen`), only ever compared two *controlled* paths against each other
(`ResolveAutomatically()` vs. an explicit `Choose()`), never against a fully autonomous run. The
"candid" claim traces to a manual trace-reading conclusion recorded in milestone 018's own archive
during its own development, never pinned by a passing test, and it was wrong.

**A bounded sweep**, all five `Variants.All`, all six characters each individually controlled with
every pause immediately auto-resolved, driven event-by-event across the full 90-day seed-42
horizon, comparing every `DecisionRecord.ChosenActionSignature()` for every actor (not only the
controlled one) plus a fingerprint of meaningful final world state (reports, requests, business
tribute status, organisational conditions, every pairwise relationship) against the same variant run
fully autonomously: **zero mismatches**, at HEAD (`43379e0`/`b2d7779`).

**The same sweep with a viewpoint deliberately different from the controlled character** throughout
(guarding the "viewpoint cannot change simulation behaviour" invariant specifically on every
iteration): zero mismatches.

**The same sweep re-run against two earlier historical commits** — `ae06f61` (milestone 018's
original implementation) and `b9dfa49` (the second correction, whose `Commit.cs` scheduled Tommy's
wake event before filing the request — the ordering the third correction later reverted): zero
mismatches at either commit either.

No divergence was found at the exact cited decision, at any other decision, for any other
character, under any variant, at any of three historical commits spanning the entirety of
milestone 018's development. The premise the milestone was scoped from does not currently hold, and
there is no evidence it ever held in a state that was actually committed.

## Disposition

Presented to Matt with the investigation account above rather than either closing the milestone
silently or inventing a production change against a guardrail-constrained brief to have something to
ship (per the milestone's own instruction: stop and ask before treating a fix as needed when the
premise itself is in question). Matt's ruling: reframe as a verification-only milestone — leave
production simulation code unchanged, promote the investigation into a permanent regression suite,
and correct the record.

## What was done

**No production simulation code was changed.** `src/CrimeEmpire.Simulation/` is untouched by this
milestone (a temporary mutation described below was made and fully reverted during verification,
never committed).

**`tests/CrimeEmpire.Simulation.Tests/ControlledAutonomousParityTests.cs`** (new, 4 tests):

- `Tommys_asked_to_account_decision_has_identical_prepared_candidates_and_totals` — compares
  `PreparedDecision.Scored` (candidate ids, `Candor`, `TargetId`, `AnsweringClaim`, and `Total` to
  nine decimal places) between the autonomous run and the controlled path read directly off the same
  `Runner.Step`/`Pipeline.Prepare` boundary `SimulationSession` itself pauses on, before either path
  resolves anything.
- `Tommys_asked_to_account_decision_resolves_automatically_to_the_identical_partial_report` — drives
  an actual `SimulationSession` to the pause and calls the real, named
  `SimulationSession.ResolveAutomatically()`, then compares the resulting
  `DecisionRecord.ChosenActionSignature()` (kind, id, target, claims, `Candor`) against the
  autonomous run, and separately compares the produced `Report`'s `Candor`, `Asserted` and
  `Withheld` claims, and confirms the request stays identically outstanding in both
  `PlayerSnapshot.AwaitingAnswers` (a Partial report withholding precisely the asked claim is
  structurally indistinguishable from silence, per `DESIGN_DECISIONS.md`'s "Causal feedback"
  section — the parity claim is that both paths land on that same outstanding state, not that the
  request resolves).
- `Answering_claim_alone_cannot_distinguish_candid_from_partial_but_the_chosen_action_signature_does`
  — the required comparator guard. Stages a `Candid` and a `Partial` candidate with the identical
  `AnsweringClaim`, asserts the false-assurance condition directly (`AnsweringClaim` equal, `Candor`
  not), and asserts `DecisionRecord.ChosenActionSignature()` — the comparator the parity tests above
  actually use — distinguishes them. This is the guard against the exact shape of error that
  produced the retracted "candid" claim.
- `Auto_resolved_control_reproduces_fully_autonomous_history_for_every_variant_and_character` — the
  bounded sweep above, promoted to a permanent regression: every variant, every character
  individually controlled with every pause auto-resolved across the full 90-day horizon, viewpoint
  deliberately different from the controlled character, compared via a deterministic text
  `WorldFingerprint` (decision signatures for every actor, reports, requests, business tribute
  status, organisational conditions, every pairwise relationship) against the fully autonomous run.

**Mutation check, performed and reverted.** `SimulationSession.ResolveAutomatically()` was
temporarily edited to resolve to `prepared.Available[^1].Id` instead of the character's own
preference (`null`) — recreating, in miniature, a player-only resolution path that diverges from
what the pipeline itself would choose. With the mutation in place:
`Tommys_asked_to_account_decision_resolves_automatically_to_the_identical_partial_report` and
`Auto_resolved_control_reproduces_fully_autonomous_history_for_every_variant_and_character` both
failed, for the intended reason — Tommy's chosen action changed from the Partial report to
`DoNothing`, and the sweep's world fingerprint diverged downstream (Kane's `2 things known` decision
sequence shortened, and Vincent's/Tommy's relationship toward Salvatore moved differently).
`Tommys_asked_to_account_decision_has_identical_prepared_candidates_and_totals` — which compares
only the pre-resolution `Prepare` boundary and never calls `ResolveAutomatically` — correctly stayed
green, confirming the test suite's specificity: it distinguishes "the offer differs" from "the
resolution differs." The mutation was then fully reverted; `git diff` on
`SimulationSession.cs` is empty.

**Documentation corrected, not merely noted.** `ROADMAP.md`'s "Controlling a character and
immediately auto-resolving is not guaranteed to reproduce..." entry is struck through and marked
retired, citing this investigation and stating precisely why the original entry's own cited evidence
never established what it claimed. `docs/milestones/018-...md` gained an appended correction (its
original text unchanged) identifying the Finding 3 "candid vs. partial" framing as unsupported by
any committed test and retracting it, while leaving Findings 1 and 2 — genuinely diagnosed and
fixed — untouched.

## Tests and success criteria

All items from the authorized scope's "required work" list are satisfied by verification rather
than by a code fix, since no defect was found to fix:

1. Reproduced through the real session and decision pipeline — done, see above.
2. Compared at every listed causal stage — done for the exact cited decision (world state,
   trigger/causal identity, actor/decision count, perceived inputs and agenda, candidates and
   totals, chosen candidate, consequences); no stage showed a difference.
3. Identified the first actual divergence rather than inferring one from the final choice — there
   is none to identify; this was verified rather than assumed.
4. Smallest fix applied — none required; the smallest correct action given a false premise is not
   to invent a change.
5. Settled architecture preserved — trivially, since nothing was changed.
6. Tests added proving: the exact natural Tommy decision matches on chosen candidate and
   consequences (✓); equivalent decision inputs before resolution (✓, the `Scored`/totals test);
   viewpoint cannot change the decision (✓, the sweep's mismatched-viewpoint construction on every
   iteration); existing Vincent parity still holds (✓, `CausalFeedbackTests
   .Player_chosen_and_autonomously_resolved_decisions_render_identical_last_action_text` untouched
   and still passing, plus Vincent is one of the six characters the sweep covers directly); a
   bounded sweep finds no other control-induced divergence (✓, the permanent sweep test).
7. Meaningful world consequences compared, not only option wording — the sweep's `WorldFingerprint`
   covers decision identity, reports, requests, business tribute status, organisational conditions,
   and every pairwise relationship.
8. Mutation-checked — done and reverted, see above.
9. Accepted autonomous trace hashes and chosen-action digests preserved — confirmed unchanged, see
   Verification below; no ruling from Matt was needed since no change to autonomous behavior was
   made.
10. No pending-decision or replay-relevant state changed, so no additional pause/save/load/resume
    equivalence work was needed beyond what milestone 018 already covers.

## Guardrails honored

No trait, coefficient, threshold, candidate, or fixture was changed. No new action, mechanic,
character, organisation, or UI. No separate player/NPC resolver was introduced (none was needed).
No manager, generic strategy framework, or speculative persistent state. The separate known
pause-timing information leak was not touched — it was never implicated, since nothing about
*when* Tommy's decision resolves changed, only whether the retracted "candid" comparison had ever
been real. No GUI layout or prose work was done.

## Verification

- `dotnet build CrimeEmpire.sln` — clean, 0 warnings, 0 errors.
- `dotnet test CrimeEmpire.sln` — **554 passed, 0 failed** (550 prior + 4 new).
- `dotnet run --project src/CrimeEmpire.Runner -- --verify --seed 42 --days 90` — deterministic,
  `9AF57665067AEA11` on both runs, **unchanged from the accepted baseline.**
- `dotnet run --project src/CrimeEmpire.Runner -- --compare --seed 42` — all five variant trace
  hashes **byte-identical to every prior figure recorded in this archive and in
  `REVIEW_LEDGER.md`**: `baseline` `9AF57665067AEA11`, `cautious-vincent` `86EC1ADA4A4E9179`,
  `watchful-boss` `84AC3F65E4102EBA`, `disloyal-vincent` `9A6E0E518294532F`, `resentful-tommy`
  `3C4483640153DA88`.
- `--variant disloyal-vincent --viewpoint salvatore --seed 42 --days 90` and `--variant baseline
  --viewpoint vincent --seed 42 --days 90` — both exit 0.
- Godot self-tests, all headless: `--selftest` (`CE-SELFTEST ok`), `--selftest-goldenpath`
  (`CE-GOLDENPATH ok`), `--selftest-directaction` (`CE-DIRECTACTION ok`),
  `--selftest-corroboration` (`CE-CORROBORATION ok`), `--selftest-tribute` (`CE-TRIBUTE ok`), and
  the two-process restart proof — `--selftest-restart-save` in one process
  (`CE-RESTART-SAVE ok`) followed by `--selftest-restart-load` in a genuinely separate process
  invocation (`CE-RESTART-LOAD ok`).

No accepted trace hash or chosen-action digest changed anywhere in this verification pass, which is
exactly what "no production code changed" predicts and is confirmed here rather than merely assumed.

## Deferred / not addressed

Everything already deferred by milestone 018 and earlier — recorded in `docs/CURRENT_MILESTONE.md`'s
"What is deferred" section and `ROADMAP.md` — remains deferred and untouched by this milestone. The
separate known pause-timing information leak (`ROADMAP.md`) remains open and was not investigated
further, per the authorized scope's explicit instruction not to expand into it absent proof it was
the direct cause here.

## Commit

See the commit this archive is part of.

## Correction (2026-08-26): Codex review, three P2 gaps closed

Codex reviewed commit `99db4de` and returned **FAIL** — production code was confirmed unchanged and
safe to build on, but three proof/documentation gaps in the verification suite itself needed closing
before acceptance. All three were structural rather than substantive: the underlying parity claim
was not in doubt, but the tests and one comment did not yet demonstrate it as rigorously as the
milestone required.

### Finding 1 (P2) — the fingerprint did not cover enough persistent state

`WorldFingerprint` covered decisions, reports, requests, business tribute status, organisational
conditions, and pairwise relationships, but omitted cognition/testimony, full business state
(`MonthlyRevenue`, `Resistance`, `Damaged`), execution commitments, several `StrategyInstance`
fields, milestone 018's causal-feedback state (`AccountConflicts`/`AccountAgreements`),
`Encounters`, truth-log `Trace`s, and most of `Organization`'s own state (priorities, policies,
offices, assignments).

**Fix.** `SimulationReplayTests.Snapshot` — the project's own comprehensive replay comparator,
already covering the truth log, decisions, reports, requests, businesses, and every character's
tier/strategy/relationship/cognition/testimony state — was widened from `private` to `internal` and
reused wholesale rather than re-derived. `WorldFingerprint` was renamed `ComprehensiveFingerprint`
and rebuilt on top of it, appending exactly what `Snapshot` does not cover: commitments, the
`StrategyInstance` fields `Snapshot` omits, reconsideration state, `AccountConflicts`/
`AccountAgreements`, `Encounters`, truth-log traces, and the organisation's conditions, priorities,
policies, offices and assignments.

**A genuine discovery in the course of this, not a mutation.** Rebuilding the fingerprint on
`Snapshot` immediately surfaced a real failure — but in the test harness, not in production
behaviour. `Snapshot`'s `now`/`queue` lines (state the original narrower `WorldFingerprint` never
read) disagreed between the autonomous and controlled runs for `baseline`/`kane`: the controlled
side had processed one extra event past the 90-day horizon. The cause was
`DriveFullyAutoResolved`'s original implementation, which drove the session with repeated
`SimulationSession.StepEvent()` calls — `StepEvent` pumps with an unbounded horizon
(`DateTime.MaxValue`) by design, so a guard loop built on it can process an event scheduled after
the intended `end` before the loop's own `session.Date < end` check ever notices, unlike
`Runner.Run(world, end)` (the autonomous reference) or `SimulationSession.AdvanceTo(end)`, both of
which are genuinely bounded by `Queue.Next(until)`. Every other line in the fingerprint — all 47
decisions, every report, every request, every relationship, every belief — was already
byte-identical; only the clock and queue depth differed. `DriveFullyAutoResolved` was rewritten to
call `session.AdvanceTo(end)` once and then only `ResolveAutomatically()` on each pause (which
resumes the recorded fast-forward through the session's own `Resume()`), matching how a real caller
fast-forwards past a choice. With that fix, all five (then five, now six with the addition below)
tests in the file pass. This was a defect in this milestone's own verification-only test code, not a
new finding about production behaviour, and no production code changed because of it.

### Finding 2 (P2) — no true viewpoint-only parity check

The existing sweep varied viewpoint away from the controlled character on every iteration, but never
in isolation: it could not distinguish "viewpoint never mattered" from "viewpoint's effect happened
to be masked by the autonomous-vs-controlled comparison it was bundled into."

**Fix.** Added `Only_the_viewpoint_differing_does_not_change_simulation_history`: for every variant
and every controlled character, two fully controlled, fully auto-resolved sessions are driven to the
identical 90-day horizon, identical in every respect except `ViewpointCharacterId`, and their
`ComprehensiveFingerprint`s are compared directly — isolating viewpoint as the only variable, with
the autonomous-vs-controlled sweep kept as a separate test answering a separate question.

### Finding 3 (P2) — both additions needed mutation-checking

**Fix, part A — a controlled-only difference in newly covered state.** Reapplied the original
mutation (`SimulationSession.ResolveAutomatically` forced to `prepared.Available[^1].Id` instead of
`null`) and re-ran the sweep. It failed immediately and, this time controlling `kane` specifically
(whose own first decision — `investigate:bellini-grocery:11` — is itself forced to `DoNothing` by
the mutation), cascaded through nearly every section of the fingerprint. Confirmed by direct
inspection of the failure diff: beyond the decisions/reports/requests/relationships/testimony lines
`SimulationReplayTests.Snapshot` already carried, the *newly added* sections also diverged and were
reported — two `encounter|...` lines present only in the autonomous history
(`encounter|tommy|kane|1987-04-08...`, `encounter|salvatore|vincent|1987-04-10...`), one
`conflict|vincent|1987-05-11...` line, and two `agreement|vincent|1987-04-09...` lines, all absent
from the controlled side because the cascading divergence meant Kane's investigation, and everything
it would otherwise have caused Tommy and Vincent to encounter, contest or agree on, never happened.
The mutation was then reverted; `git diff` on `SimulationSession.cs` is empty.

**Fix, part B — a viewpoint-specific behaviour difference.** Since nothing in production code reads
`ViewpointCharacterId` outside `Snapshot()`/`PlayerView.Build` — that is the entire property being
pinned — proving the new viewpoint-only test has teeth required injecting exactly the leak it
guards against: `SimulationSession.ResolveAutomatically` was temporarily edited to force the
last-ordered candidate whenever `ViewpointCharacterId == "salvatore"` and more than one option was
available, leaving every other viewpoint on the true preference. Running
`Only_the_viewpoint_differing_does_not_change_simulation_history` failed immediately
(`variant 'baseline', controlling 'kane': viewpoint 'kane' vs. 'salvatore' produced different
simulation histories`), for the intended reason. The mutation was then reverted; `git diff` on
`SimulationSession.cs` is again empty.

### Finding — contradictory XML comment

`Tommys_asked_to_account_decision_resolves_automatically_to_the_identical_partial_report`'s summary
said the request "drop[s] out of `AwaitingAnswers`," contradicting the test body three lines below,
which correctly asserts the request *stays* in `AwaitingAnswers` (a `Partial` report withholding
precisely the asked claim asserts nothing Vincent's cognition can register, so it is structurally
indistinguishable from silence — the actual, correct behaviour the test proves parity for). The
comment was rewritten to match the assertion it sits above.

### Tests after this correction

`ControlledAutonomousParityTests.cs` now has 5 tests (was 4): the two Tommy-decision tests
unchanged in substance (one comment fixed), the comparator guard unchanged, the autonomous-vs-
controlled sweep now built on `ComprehensiveFingerprint`, and the new
`Only_the_viewpoint_differing_does_not_change_simulation_history`.

### Verification (post-correction)

- `dotnet build CrimeEmpire.sln` — clean, 0 warnings, 0 errors.
- `dotnet test CrimeEmpire.sln` — **555 passed, 0 failed** (554 prior + 1 new).
- `dotnet run --project src/CrimeEmpire.Runner -- --verify --seed 42 --days 90` — deterministic,
  `9AF57665067AEA11` on both runs, **unchanged**.
- `dotnet run --project src/CrimeEmpire.Runner -- --compare --seed 42` — all five variant trace
  hashes **unchanged**: `baseline` `9AF57665067AEA11`, `cautious-vincent` `86EC1ADA4A4E9179`,
  `watchful-boss` `84AC3F65E4102EBA`, `disloyal-vincent` `9A6E0E518294532F`, `resentful-tommy`
  `3C4483640153DA88`.
- `--variant disloyal-vincent --viewpoint salvatore --seed 42 --days 90` and `--variant baseline
  --viewpoint vincent --seed 42 --days 90` — both exit 0.
- Godot self-tests, all headless: `--selftest`, `--selftest-goldenpath`, `--selftest-directaction`,
  `--selftest-corroboration`, `--selftest-tribute` — all `ok`; two-process restart proof
  (`--selftest-restart-save` then `--selftest-restart-load` in a genuinely separate process) — `ok`.

No accepted trace hash or chosen-action digest changed. Production code (`src/CrimeEmpire.Simulation/`)
remains untouched by this milestone; both mutation-check edits to `SimulationSession.cs` were fully
reverted before this verification pass.

### Correction commit

See the commit this correction is part of.

### Clarification (2026-08-26): correcting this correction's own account of the review

The section above mislabels what Codex's review of `99db4de` actually found. Recorded here rather
than edited into the text above, per the archive's own append-only convention. Codex's review of
commit `99db4de` returned exactly **three** P2 findings:

1. `ComprehensiveFingerprint` (then `WorldFingerprint`) did not cover enough persistent state.
2. There was no true viewpoint-only parity check.
3. The `Tommys_asked_to_account_decision_resolves_automatically_to_the_identical_partial_report`
   XML comment contradicted its own test body — the "the same request dropping out of
   `AwaitingAnswers`" line this archive's earlier section already describes fixing.

**Mutation-checking findings 1 and 2 was not itself a fourth, separate finding.** It was the
required proof that closing findings 1 and 2 actually worked — the same discipline every milestone
in this project applies to a production fix, applied here to a test addition instead. The section
above numbers it "Finding 3 (P2) — both additions needed mutation-checking" and demotes the actual
finding 3 (the contradictory comment) to an unlabeled closing paragraph with no number at all. The
substance of everything under both headings is accurate and remains correct; only the numbering and
which item was independently flagged by Codex versus which was this milestone's own required
verification step were wrong. This clarification exists so a future reader comparing this archive
against Codex's own review transcript is not misled about which is which.

### Correction (2026-08-26, second): two remaining P2 gaps closed

Codex reviewed correction commit `c9af6b6` and returned **FAIL** with two further P2 gaps, confirming
production code was still fine and safe to build on, and separately confirming the true viewpoint-only
test, the corrected `AwaitingAnswers` comment, and the bounded `AdvanceTo(end)` driver from the first
correction were all good.

**Finding 1 (P2) — `ComprehensiveFingerprint` still omitted meaningful mutable state.** Named
specifically: `Character.Capabilities.Cash`, `Execution.Intention`, `World.ObservationOccasionKeys`,
and deterministic queued-event contents rather than only `Queue.Count`. Required: an audit of the
remaining mutable `World` and `Character` state before claiming the comparator covers every
replay-relevant collection, not only the four named examples.

**The audit**, read against `Domain/Character.cs`, `Domain/Capabilities.cs`, `Domain/Motivations.cs`,
`Domain/SocialState.cs`, `Domain/Psychology.cs`, `Sim/World.cs`, `Sim/ScheduledEvent.cs` and
`Sim/EventQueue.cs`, and confirmed against every production write site with a grep for each field's
assignment: beyond the four named gaps, `Motivations` was entirely absent from the comparator —
`Ambition` (set once, at `Cast.cs`'s scenario construction, never reassigned during a run),
`Responsibilities` (mutated by `Runner.DeliverAssignment` and `Strategies.cs`'s assignment
completion), `Pressures` (mutated by nine call sites across `Commit.cs`, `Runner.cs` and
`Strategies.cs` via `AddPressure`), and `ImmediateNeeds` (declared, never written by anything —
included anyway, since an audit that silently trusts "currently unused" is the same mistake as the
one being corrected). Also absent: `Capabilities.Crew` (never mutated at runtime, but part of the
same type as `Cash`), `SocialState.OrganizationId` (set once at `Cast.cs` construction, never
reassigned), and `world.Queue.Cancelled` (which events were invalidated and why — a fact about
replay-relevant state distinct from which events remain pending). Confirmed by the same grep-every-
write-site method to be genuinely static across a run, and included anyway rather than trusted
silently: `Psychology` (only ever reassigned by `Variants.cs` before a run starts — never written by
`Commit.cs`, `Pipeline.cs`, or `Strategies.cs`) and `Capabilities.Authority`/`Districts`.

**Fix.** `ComprehensiveFingerprint` gained, per character: `Capabilities.Cash`/`Crew`/`Authority`/
`Districts`; `Social.OrganizationId`; every `Motivations` field; `Execution.Intention`. And, at the
world level: `ObservationOccasionKeys`; and the full deterministic contents of every event still
pending, plus `Queue.Cancelled`.

**Queue contents required one narrow production addition — the only one this milestone has made.**
`EventQueue` wraps a `PriorityQueue<ScheduledEvent, (DateTime, long)>` privately and exposed nothing
but `Count` before this correction; there was no way to read pending-event *contents* without one.
Added `EventQueue.PendingEvents` — `internal`, read-only, returning every pending event sorted by
`(Time, Id)` via the BCL `PriorityQueue.UnorderedItems` without dequeuing anything, so inspecting it
changes nothing about how `Next` later drains the queue. This mirrors the exact pattern
`SimulationSession.World` already uses — an `internal` accessor the type system admits to the test
assembly and nothing else, added purely so a test can see state a production caller never needs and
must never get, not a new capability any simulation code reads or could read. No simulation
*behaviour* changed — `git diff --stat` against `b2d7779` for `src/CrimeEmpire.Simulation/` shows
exactly one file, `Sim/EventQueue.cs`, 17 lines added and none removed (one property plus its doc
comment), otherwise byte-identical, and the accepted trace hashes below confirm it. Every prior "no
production code changed" claim in this archive is correct about behaviour and should now be read
narrowly against this one, deliberate, additive, read-only exception.

**Finding 2 (P2) — the new fields were not mutation-checked.** Required: perturb at least one newly
added field on only one of the two compared paths, and confirm the parity test fails.

**Fix.** `Auto_resolved_control_reproduces_fully_autonomous_history_for_every_variant_and_character`
was temporarily edited to add `session.World.Get(charId).Capabilities.Cash += 1;` immediately after
driving the controlled session, on the controlled side only. The test failed immediately
(`variant 'baseline', controlling 'kane'...`), and the failure diff isolated to exactly the expected
line — `capabilities|kane|0.0000|2|0|harbour` (autonomous) versus `capabilities|kane|1.0000|2|0|
harbour` (controlled), nothing else differing. The line was then removed; `git diff` on
`ControlledAutonomousParityTests.cs` shows only the intended, permanent additions.

### Tests after this correction

`ControlledAutonomousParityTests.cs` still has 5 tests — this correction widened
`ComprehensiveFingerprint`'s coverage and added one production-side testability accessor
(`EventQueue.PendingEvents`); it added no new test method.

### Verification (post-second-correction)

- `dotnet build CrimeEmpire.sln` — clean, 0 warnings, 0 errors.
- `dotnet test CrimeEmpire.sln` — **555 passed, 0 failed** (unchanged count — no new test methods
  this round).
- `dotnet run --project src/CrimeEmpire.Runner -- --verify --seed 42 --days 90` — deterministic,
  `9AF57665067AEA11` on both runs, **unchanged**.
- `dotnet run --project src/CrimeEmpire.Runner -- --compare --seed 42` — all five variant trace
  hashes **unchanged**: `baseline` `9AF57665067AEA11`, `cautious-vincent` `86EC1ADA4A4E9179`,
  `watchful-boss` `84AC3F65E4102EBA`, `disloyal-vincent` `9A6E0E518294532F`, `resentful-tommy`
  `3C4483640153DA88`.
- `--variant disloyal-vincent --viewpoint salvatore --seed 42 --days 90` and `--variant baseline
  --viewpoint vincent --seed 42 --days 90` — both exit 0.
- Godot self-tests, all headless: `--selftest`, `--selftest-goldenpath`, `--selftest-directaction`,
  `--selftest-corroboration`, `--selftest-tribute` — all `ok`; two-process restart proof
  (`--selftest-restart-save` then `--selftest-restart-load` in a genuinely separate process) — `ok`.

No accepted trace hash or chosen-action digest changed. `git diff --stat` against `b2d7779` for
`src/CrimeEmpire.Simulation/` shows only `Sim/EventQueue.cs`, and only the additive, read-only,
`internal` accessor described above.

### Second correction commit

See the commit this correction is part of.
