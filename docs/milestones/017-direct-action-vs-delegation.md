# Milestone 017 — Direct Action vs Delegation: Different Causal Footprints

Authorized by Matt after milestone 016 was accepted (commit `9364869`, confirmed present in history
before any work began; Codex reviewed that documentation-correction commit and returned no findings).
The full authorizing text is preserved in this commit's prior version of `docs/CURRENT_MILESTONE.md`
and is not reproduced here in full — this archive states what it asked for only as needed to explain
what was done.

## What this milestone is for

Prove, through the existing playable `SecureTribute` operation, that Vincent personally executing an
operation and delegating it to Tommy are causally different choices — different executor, different
first-hand knowledge, different exposure — even though both pursue the identical objective through the
identical shared rules. Not a personnel-management or "best employee" system: no new candidate
generator, no operation framework, no second action implementation. The milestone forbade all of that
explicitly, and none of it was built.

## What was already true before this milestone

Reconnaissance found the owner/executor split this milestone asks to prove is not new. It was built
across milestones 007–011 and exercised end to end by milestone 014's golden path:

- `Strategies.ScheduleNextStep`/`Advance` already wake `s.DelegatedToId ?? s.OwnerId`, never `OwnerId`
  alone.
- `AdvanceTribute`, `ResolveViolence`, and `QuietWitnesses` are already written entirely in terms of
  `executor` vs. `owner`, with explicit comments (predating this milestone) about why the delegator
  "learns nothing here."
- `Generators.GenerateAll` calls both `FromCommitment` (proposes `ContinueStrategy`, "carry on...")
  and `FromRelationship` (proposes `DelegateStrategy` to the highest-trust subordinate — deterministically
  Tommy, the only character at Vincent's `Authority - 1` — "have Tommy Nardo take it on")
  unconditionally whenever `Actor.Execution.Strategy` is running and undelegated.

This milestone's job was therefore to fork the existing mechanism at the point both options are offered
together, stage it where the natural run cannot guarantee reach, and prove the result — not to build a
new one. No changes were made to `Strategy/Strategies.cs`, `Decision/Commit.cs`, or
`Decision/Generators.cs`. Every trace hash and chosen-action digest below is unmoved from milestone
016's accepted baseline, confirmed by a full clean-tree re-run rather than assumed.

## The fork point

**Vincent's very first pause after starting the operation** — immediately after choosing "talk
Bellini's grocery round" — offers both "carry on getting Bellini's grocery to pay" and "have Tommy
Nardo take it on" in the same `PendingDecision`. This was assumed at planning time to be a later pause
(the second decision in `PlayerOwnedOperationTests.SevenChoiceSequence`, chosen there without
comparison against delegation); it is in fact the fork the moment the operation exists, because both
generators fire unconditionally once `Execution.Strategy` is set and undelegated. Confirmed directly by
`DirectActionVsDelegationTests.Vincents_first_pause_offers_both_direct_continuation_and_delegation_together`
and by the Godot proof below reading the same two option texts off the live screen at that pause.

## What was completed

### `tests/CrimeEmpire.Simulation.Tests/DirectActionVsDelegationTests.cs` (new, 18 tests)

**Section A — the natural fork, in memory (`SimulationSession`).** Two independent sessions from the
identical seed, both driven to the fork, diverging on exactly one deliberate choice each — then
`SimulationSession.ResolveAutomatically()` for everything after, the same legitimate "one choice then
autonomous continuation" pattern milestone 014's ruling 4 established. Nothing after the fork choice is
hand-picked, so nothing is scripted to manufacture a result.

- The fork offers both options together (requirement 1).
- Two fresh sessions reach byte-identical world state and `PlayerSnapshot` at the fork before either
  branch chooses (requirement 2).
- **The headline test**: `Direct_action_and_delegation_diverge_from_the_identical_fork` — ownership
  (`StrategyInstance.OwnerId`) never moves; execution responsibility (`DelegatedToId`) is exactly what
  diverges, checked immediately and structurally right after the one fork choice, before any further
  advance. The two branches' full traces differ. Ownership determines proceeds regardless of executor
  is checked as a required *negative*: when both branches reach collection, Vincent's cash rises in
  both.
- Vincent's projection carries no delegated first-hand knowledge: nothing in his cognition is sourced
  to Tommy as `Participant` or `Discovery` (the two source kinds that mean "I was there myself"), and
  the reflective value-graph walk `PlayerOwnedOperationTests` already established finds no developer
  text anywhere on the snapshot across the whole delegated arc (requirement 6).
- Each branch (direct, delegated) is deterministic across two fresh runs (requirement 9); each is
  equivalent whether advanced in one fast-forward or paused and resumed at every decision (requirement
  10, first half).

**Section B — staged boundary proof, through the real production path.** Mirrors
`InvestigationTests.cs`'s `OpenDelegatedCase`/`RunToCompletion` idiom, scoped to `SecureTribute`
against Bellini's grocery instead of `InvestigateIncident`: `Commit.Apply` opens the case and
(when the executor differs from the owner) delegates it, then `Strategies.Advance` is called directly
with real `ScheduledEvent`/`EventPayload` values — the same static methods production calls, never a
test-only path. Every proof is run twice, once with Vincent as his own executor and once with Tommy as
delegate, from otherwise-identical staged state:

- Violence and its evidence (`PersonUsedViolence`, `WitnessSawIncident`) name the actual executor, and
  the owner (when different) holds no `Participant`-sourced copy.
- Marco's encounter and fear (`world.Encounters`, `Relations.Frighten`) concern the actual executor;
  the owner, when different, gets neither.
- Collection is `Participant`-sourced, first-hand knowledge for the executor and `Discovery`-sourced
  for the owner — the existing split in `AdvanceTribute`'s "collected" branch, now asserted rather than
  only commented.
- The delegator receives nothing beyond `Commit.Apply`'s own bounded (at most two claims) briefing:
  Vincent's cognition record count is unchanged by everything Tommy subsequently forms on his own.
- **Reused for an angle `InvestigationTests.cs` did not cover**: investigation names the true executor
  of the underlying tribute violence, not the owner — the same class of proof that file's
  `A_delegated_investigations_lead_is_drawn_from_the_executors_own_belief` established for a *delegated
  investigation*, applied here to the executor of the incident a separate investigation canvasses.

**Section C — save/load through the shared fork.** Directly extends
`PersistenceTests.Counterfactual_valid_choices_from_the_same_save_diverge_naturally`'s own pattern:
`PersistentSession.Start` → advance to the start decision → choose it → advance to the fork → `Save` →
`Load` twice → send one load down "carry on", the other down "have Tommy Nardo take it on". Stops at
the fork's immediate structural divergence (`DelegatedToId`) rather than assuming an undiscovered full
choice sequence for the direct branch — `PersistentSession` exposes no autonomous-resolution path (only
`StepEvent`/`Choose`, matching what a real save file can actually replay), so driving it further would
have meant inventing a pinned sequence this milestone never asked for. Satisfies requirement 10
(second half) and the in-scope "save/load replay from the shared fork through both branches" item.

### `src/CrimeEmpire.Godot/Game.cs` — `--selftest-directaction` (requirement 12)

Structured exactly like the existing `--selftest-goldenpath`: real button presses only, "Next event" to
reach each pause, no candidate id, no score, no reflection. Presses the start, reaches the fork, asserts
both "carry on getting Bellini's grocery to pay" and "have Tommy Nardo take it on" are present as real,
simultaneously offered buttons — the executable feature claim itself, proven through the live interface
— then presses "carry on" (never delegate) and continues through six further real button presses,
independently pinned from a live run of the interactive path the same way the existing
`SevenChoiceSequence` was originally derived (see "Important discoveries" below for the exact method).

Confirms three things by reading the live rendered screen, never `SimulationSession`'s internal state:

- **Cash still rises to 6,840** — ownership determines proceeds regardless of executor, unchanged from
  the accepted delegated trace's own consequence. Checked as a required negative.
- **"Vincent Russo put hands on Bellini's grocery... beyond doubt for him, he had a hand in it
  himself"** — Vincent's own knowledge of his own act, `Participant`-sourced, on the live screen.
- **No occurrence of "Tommy Nardo put hands on Bellini's grocery"** anywhere on the screen — the exact
  phrasing the accepted delegated trace renders instead, absent here because Tommy never executed
  anything in this branch.

No change to what `Game.cs`/`PlayerSnapshot` expose. `Game.cs` gained one new self-test method and one
new flag constant; nothing else in the file changed.

### `docs/ROADMAP.md`

Added one new "Known technical debt" entry recording executor suitability/capability as deferred, per
the milestone's own instruction ("record executor suitability/capability as deferred if it is not
already recorded"). It was not already recorded anywhere in `ROADMAP.md`, `OPEN_CONCERNS.md`, or
`DESIGN_DECISIONS.md`, confirmed by search before writing the entry.

### `docs/DESIGN_DECISIONS.md`

**Not touched.** This milestone is confirmatory of already-settled architecture (milestones 007–011's
owner/executor split), not a source of new settled rules. Nothing here decides anything that was open
before it; the fork point, the staged proofs, and the Godot sequence are measured facts about the
existing scenario, which is what this archive is for, not a design decision.

## Verification

- Build: **0 warnings, 0 errors** across all projects (unchanged project count — no new project).
- Tests: **523 passed, 0 failed** (505 before this milestone; 18 new in
  `DirectActionVsDelegationTests.cs`).
- `--verify` deterministic and byte-identical on `baseline` (`9AF57665067AEA11`), `disloyal-vincent`
  (`9A6E0E518294532F`), `resentful-tommy` (`3C4483640153DA88`) — all three unmoved from milestone 016's
  accepted baseline. `--compare` shows 5 distinct traces, 5 distinct chosen-action sequences, all
  digests unmoved (`955921AA69ABA44C`, `BECCA9ED2E4E7137`, `B9B6D3BBE6A69200`, and the two unchanged
  from before — `cautious-vincent`, `watchful-boss`). Both required viewpoint runs
  (`disloyal-vincent`/`salvatore`, `baseline`/`vincent`) exit 0.
- Godot: `--selftest` (4 choices, 4 decision screens, exit 0), `--selftest-goldenpath` (seven choices,
  cash 6,000 → 6,840, exit 0) — both unchanged — and the new `--selftest-directaction` (exit 0,
  confirmed above). The two-process restart proof (`--selftest-restart-save` /
  `--selftest-restart-load`, each a genuinely separate headless invocation) exits 0 on both.

### Mutation checks

All four of the milestone's named tempting-wrong implementations were applied as real, temporary edits
to production code, confirmed to make the relevant test(s) fail for the intended reason, then reverted.
Full suite re-confirmed green (523/523) after every revert; `git diff --stat` against `src/` confirmed
no residual change after each one.

1. **Force every operation step to use `OwnerId` as executor** — `Strategies.ScheduleNextStep`'s and
   `Advance`'s `s.DelegatedToId ?? s.OwnerId` replaced with `s.OwnerId`. **6 tests failed**: the
   delegated staged proofs threw `SimulationInvariantException` ("a stale or misrouted step must never
   advance a replacement") because the delivered event's addressee no longer matched, and the
   information-boundary test failed because Tommy never formed any first-hand knowledge to leak in the
   first place — exactly the intended failure shapes.
2. **Copy the executor's new belief into the owner's cognition** — added a second `owner.Cognition.Learn`
   call beside the executor's own in `AdvanceTribute`'s approach step. **1 test failed**:
   `The_delegator_learns_nothing_the_executor_forms_after_the_briefing`'s record-count assertion moved
   from 0 to 1.
3. **Attribute evidence and fear to the owner regardless of executor** — `ResolveViolence`'s
   `world.Record`, `PersonUsedViolence`/`WitnessSawIncident` claims, and both `Relations.Frighten` calls
   in the escalation step changed from `executor.Id` to `owner.Id`. **2 tests failed**:
   `Violence_and_its_evidence_identify_the_actual_executor` (subject `"vincent"` where `"tommy"` was
   expected) and `Marcos_encounter_and_fear_concern_the_executor_not_the_owner` (fear on the wrong
   relationship).
4. **Leak scheduler causes into Vincent's projection** — `SimulationSession.Project`'s
   `PendingDecision.Occasion` field changed from `PlayerOccasion.For(prepared.Trigger, self)` to
   `prepared.Trigger.Cause` directly. This milestone's own new tests did not catch it — none of them
   happens to inspect `Occasion` text against a leaked-string oracle. **9 existing regression tests
   failed** instead, all in `PlayerSessionTests.cs`, for the intended reason (raw scheduler/developer
   text reaching a player surface — exactly milestone 009's original P1 shape). Recorded here rather
   than silently substituting a new test for this mutation: the milestone asked each *relevant* test to
   fail, and the relevant tests already existed.

## Important discoveries

**The fork is Vincent's very first pause after starting, not a later one.** Planning-time framing
(carried over from `PlayerOwnedOperationTests.SevenChoiceSequence`'s second entry, "carry on...")
assumed the fork was reached only after some intervening step. Both `FromCommitment` and
`FromRelationship` fire unconditionally the moment a strategy is running and undelegated, so the fork is
available the instant the operation exists. This changed nothing about scope — it made the natural
proof simpler than planned, not harder.

**Vincent's own prior first-hand belief can reach Tommy as testimony, and that is not a defect.** An
early version of `Vincents_projection_never_carries_tommys_first_hand_reading_of_the_target` asserted
Tommy's own reading of `TargetIsVulnerable` would be `Discovery`-sourced. It is not, at the fork this
milestone forks: Vincent's own "approach" step already fired, forming his own `Discovery`-sourced
belief, *before* he ever reaches the delegate decision — so `Commit.Apply`'s delegation briefing carries
Vincent's own already-held belief to Tommy as testimony (`Report`-sourced), and Tommy's own approach
step never re-runs, because `StepIndex` has already advanced past it. Vincent is entitled to his own
prior first-hand read; this is not delegated knowledge reaching him for free, because he already had
it before delegating. The test was rewritten to check the general structural boundary (nothing sourced
to Tommy as `Participant`/`Discovery` reaches Vincent's cognition) instead of this one claim's specific
source kind, and the staged proof (`The_delegator_learns_nothing_the_executor_forms_after_the_briefing`)
isolates the clean case directly. Recorded here per this project's standing practice: a premise stated
while writing a test that implementation shows to be narrower than assumed is a finding, not a detail
to quietly fix.

**The direct branch's Godot sequence was discovered empirically, the same way `SevenChoiceSequence`
itself was derived.** A temporary diagnostic self-test (built, run against the real Godot headless
binary, and removed before this commit — never part of the shipped code) drove the interactive path
with a fixed policy (never delegate; press "carry on" at the fork specifically; thereafter prefer force
over threats over carry-on over the first offered option) and logged every pause's real option text.
The resulting eight-choice sequence is what `DirectActionSelfTest` presses. Its most notable finding:
the direct branch naturally leads Vincent to conceal the incident himself ("clean up after it before
anyone else does") and, later, to give an account of his own violence directly to Det. Kane — an
investigation-names-the-executor consequence the natural run produces for free, without needing the
staged proof to demonstrate it for this particular seed and policy.

## Deferred work

Per the authorizing text: choosing between multiple subordinates; recruitment, crew rosters,
specialists, equipment, preparation, and budget allocation; making Persuasion, Coercion, or crew size
affect tribute success; resolving whether escalation capability belongs to the owner or the delegate;
transferring resources from owner to delegate; territory, patrol, or weekly planning; additional
businesses or operations; a seventh character; employee-stat displays or new UI panels; the known
pause-timing information leak (`ROADMAP.md`); new organizations, careers, or alternate playable roles.
Executor suitability/capability is now recorded as deferred in `ROADMAP.md`, not already having been
before this milestone.

## Where to look and what to distrust

The claim most expensive if wrong is the same shape milestone 014 flagged: that
`SimulationSession.ResolveAutomatically()` after one deliberate fork choice genuinely does not
manufacture the divergence. It is the pipeline's own preference at every subsequent pause, read through
the same `Pipeline.Resolve(prepared, null)` call every autonomous NPC decision uses — not a value this
milestone's own test code chose. A reviewer should re-derive that from `SimulationSession.cs` and
`Decision/Pipeline.cs` rather than take the test's own naming on faith, the same caution milestone 014's
archive raised about its own golden-path mechanism.

The Godot direct-action sequence is mechanically reproducible: rerun
`--headless --path src/CrimeEmpire.Godot -- --selftest-directaction` against this exact commit. Low
risk to re-check, and its diagnostic-derivation method is recorded above rather than left as an
unexplained pinned constant.

## Commit

One implementation-and-archive commit. Status is not established by this file —
`docs/CURRENT_MILESTONE.md` says what is active, and Matt's confirmation of a named commit is the only
thing that counts as acceptance.

---

## Correction from Codex's review of `9de2c75`

Appended, not folded in. The account above is preserved as originally written and is **superseded by
this section** wherever it describes the four items below. Codex reviewed the implementation commit
and returned **FAIL**, four proof defects, none of them a finding about the simulation behaviour
itself — every trace hash and chosen-action digest named above was unaffected by this correction.

**1 — the pause/fast-forward test proved nothing.** The original
`Pausing_and_resuming_reaches_the_same_state_as_an_uninterrupted_run` asserted only
`SessionStatus.Ready` and the ending date, which two completely different histories could both
satisfy. Replaced with
`Fast_forward_and_event_by_event_stepping_reach_equivalent_state_within_a_branch`: from the identical
fork, making the identical fork choice, one session is advanced entirely through
`SimulationSession.AdvanceTo` and the other through real single-event `StepEvent` calls for its own
early activity, followed by one bounded `AdvanceTo(End)` sweep — the same "event by event, then fast
forward" shape `PlayerSessionTests.Stepping_and_fast_forward_patterns_agree` already proves correct in
general, reused rather than re-derived. A raw `while (Date < End) StepEvent()` loop was deliberately
avoided: `StepEvent` is documented as unbounded (`Pump(DateTime.MaxValue, oneEventOnly: true)`), so
such a loop can process one event past `End` that a horizon-bounded `AdvanceTo(End)` would never touch.
A second, more basic defect was found and fixed while building this: `ReachFork` itself uses
`AdvanceTo`, which leaves an outstanding fast-forward horizon active; the very next `Choose` call on
the "stepped" session would silently resume that horizon and bulk-advance it exactly like the other
branch, proving nothing about stepping patterns at all. The corrected test reaches the fork for the
stepped branch entirely through raw `StepEvent` calls instead. Both branches resolve every pause after
the fork choice with the same real, visible, deterministic "last offered option" policy
`PlayerSessionTests.Settle` already established (added here as `Settle` and `StrategyFingerprint`),
never the hidden-score `ResolveAutomatically`. Compares the final trace, the final Vincent-facing
snapshot, and the operation's own replay/future-decision-relevant state (`StrategyInstance`'s owner,
delegate, method, step index, and target; Vincent's cash; the business's paying state) — not merely
status and date. Sanity-checked by temporarily forcing the stepped branch's fork choice to `CarryOn`
regardless of the theory's `firstChoice`: the `DelegateToTommy` case failed on the trace-equality
assertion exactly as expected; reverted before committing.

**2 — the save/load proof stopped at the immediate `DelegatedToId` flag.** Real, but not the "real
operation consequence" requirement 10 asks the save/load proof to reach, and it compared only the two
loaded branches against each other rather than against an unsaved control. Corrected: each loaded
branch (`goldenLoaded`, `declinedLoaded`) is now continued past the fork with the same "last offered
option" policy (`SettleLastOption`, `PersistentSession`'s own equivalent of `Settle` — no
`ResolveAutomatically` exists on that type) far enough to reach a real consequence, and compared
(`AssertPersistentEquivalence`: trace, snapshot, `StrategyFingerprint`, cash) against an **equivalent
unsaved control** — a fresh, never-saved session driven through the identical start-then-fork path and
the identical fork choice. A `TruthLog.Count > 1` guard confirms real progress was made past the fork
before the comparison is trusted. Sanity-checked by temporarily forcing `declinedControl`'s fork choice
to `CarryOn` (mismatching `declinedLoaded`'s `DelegateToTommy`): the equivalence assertion failed on
the trace-equality check exactly as expected; reverted before committing.

**3 — the investigation-attribution test fed itself a hand-typed answer.**
`An_investigation_names_the_true_executor_of_the_violence_not_the_owner` constructed a fresh
`WitnessSawIncident` claim naming `executorId` directly and handed it to Kane, so a defect in
`ResolveViolence`'s own `witnessClaim` attribution would never have been exercised — the test checked a
value it typed itself, not production output. Corrected: the real `ObservationOpportunity` event
`ResolveViolence` schedules for Kane through its own production `Offer`/`ScheduleObservation` path is
now drained from the world queue (`Drain`, independently copied from `InvestigationTests.cs` per this
project's practice), and the real `Claim` is read off that event's own payload — never retyped. Only
`Runner.Observe`'s discoverability *roll* is bypassed (staged delivery straight into Kane's cognition,
in the exact shape `Observe` itself writes); the claim's content, including its executor attribution,
is entirely production output. An explicit assertion confirms the drain actually finds the opportunity
before proceeding, so a future change that stopped Kane from being offered one would fail loudly rather
than the test silently passing on an empty premise.

**Mutation-checked exactly as required**: `Strategies.ResolveViolence`'s `witnessClaim` was changed
from `new Claim(ClaimKind.WitnessSawIncident, business.Id, executor.Id, ev.Id)` to
`new Claim(ClaimKind.WitnessSawIncident, business.Id, owner.Id, ev.Id)` — `violenceClaim`
(`PersonUsedViolence`) untouched, so it remained correctly attributed to `executor.Id` throughout. The
corrected test failed for the `Tommy` (delegated) case exactly as required (`Expected: "tommy"`,
`Actual: "vincent"`); the `Vincent` (direct, owner == executor) case still passed, since the mutation
is invisible when owner and executor are the same person. Reverted before committing;
`git diff --stat` against `Strategies.cs` confirmed no residual change.

**4 — the Godot fork-offering check read session-internal state, not the rendered interface.**
`DirectActionSelfTest` checked `session.Pending.Options` directly — the session's own internal state —
so a UI defect that silently failed to render one of the two fork buttons, while the session still
legitimately offered both underneath, would never have been caught. Corrected: the check now calls
`FindButton(this, "carry on getting Bellini's grocery to pay")` and
`FindButton(this, "have Tommy Nardo take it on")` — the same helper `Press` itself uses to find and
click a button by its rendered text — walking the actual live scene tree rather than the session.

**Mutation-checked exactly as required**: `BuildDecisionPanel`'s option-rendering loop (`Game.cs`,
around the `foreach (var option in pending.Options)` block) was temporarily given
`if (option.Description == "have Tommy Nardo take it on") continue;`, omitting only that one button
from the rendered UI while the session's own `Pending.Options` still contained it. Rebuilt and reran
`--headless --path src/CrimeEmpire.Godot -- --selftest-directaction`: the corrected self-test failed
(`carry-on button present: True, delegate button present: False`), exit code 1, exactly as required.
Reverted before committing; rerun confirmed exit 0 again.

**What this correction is not.** No coefficient, fixture, trait, or production-simulation behaviour
change survives in the committed diff — the two mutations above (finding 3's claim-attribution change,
finding 4's UI-omission change) were applied, confirmed to fail the corrected tests for the stated
reason, and reverted before this commit. `git diff --stat` against `src/CrimeEmpire.Simulation/` and
`src/CrimeEmpire.Godot/Game.cs`'s option-rendering loop confirms only the four corrections described
above and no residual mutation.

### Verification, re-run in full after the correction

- Build: **0 warnings, 0 errors** across all projects.
- Tests: **523 passed, 0 failed** — same total as before the correction (one theory replaced another
  of equal size: 2 cases removed, 2 added).
- `--verify` deterministic and byte-identical on `baseline` (`9AF57665067AEA11`), `disloyal-vincent`
  (`9A6E0E518294532F`), `resentful-tommy` (`3C4483640153DA88`) — all unmoved. `--compare` shows 5
  distinct traces, 5 distinct chosen-action sequences, all digests unmoved. Both required viewpoint
  runs (`disloyal-vincent`/`salvatore`, `baseline`/`vincent`) exit 0.
- Godot: `--selftest` (exit 0), `--selftest-goldenpath` (exit 0), the corrected `--selftest-directaction`
  (exit 0), and the two-process restart proof — `--selftest-restart-save` / `--selftest-restart-load`,
  each a genuinely separate headless invocation — both exit 0.

**Status.** This corrective commit is implemented, tested, and both required mutation checks (findings
3 and 4) ran and reverted as described above. It is **not accepted** — Matt's confirmation of this
named commit, after Codex's verification, is what that requires.
