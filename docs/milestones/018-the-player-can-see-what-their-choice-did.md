# Milestone 018 — The Player Can See What Their Choice Did

Authorized by Matt after milestone 017 was accepted (correction commit `0f56f1e`, documentation
close-out `8ce2893`, both confirmed present in history before any work began). The full authorizing
text is preserved in this commit's prior version of `docs/CURRENT_MILESTONE.md` and is not reproduced
here in full — this archive states what it asked for only as needed to explain what was done.

## What this milestone is for

Every deliberate choice — the viewpoint character's own, whether made by a person or the pipeline
itself — must leave a player-visible causal thread: immediate acknowledgement of what was chosen,
unresolved status while pending, and perspective-limited resolution once its consequence becomes
known. Not an omniscient event log, not a new consequences system, not a second action path for
players. The milestone forbade all of that explicitly, and none of it was built.

## Preflight

The working tree held uncommitted Godot-editor regeneration artifacts overlapping `Game.cs`: a
whitespace-only tabs-for-spaces reformat (`git diff -w` showed zero content difference), a literal
`net8.0` overwrite of `CrimeEmpire.Godot.csproj`'s `$(CrimeEmpireEngineTfm)` reference, and
`project.godot`'s deliberate "bare shell" header replaced with Godot's boilerplate. Matt chose to
revert all three to `HEAD` and delete the two Godot-generated sidecar artifacts
(`CrimeEmpire.Godot.csproj.old`, `Game.cs.uid`) before implementation began, rather than build on top
of them.

## Architecture: everything is a projection, nothing new is persisted

Surveyed before writing code: `PlayerSnapshot`/`PlayerView.Build`, `PlayerOption`, `PlayerOccasion`,
`PlayerNarration`, `SimulationSession`, `Cognition`/`Testimony`, `Reporting`, `Relations`/
`AccountConflict`/`AccountAgreement`, `Commit`, `Pipeline`, `World`, and `Game.cs`. The design that
came out of that survey, confirmed correct by every test below: **no new persistent state was added
anywhere in `Commit.cs`, `Pipeline.cs`, or `Strategies.cs`.** Every new surface is a read-time
projection over collections that already existed and were already written for other reasons.

`PlayerSnapshot` gained four fields, each derived inside `PlayerView.Build`:

- **`LastAction`** — `world.Decisions` filtered to `ActorId == viewpoint`, most recent entry, rendered
  through the existing `PlayerOption.Describe` applied to `.Chosen?.Candidate`. `world.Decisions` is
  written by `Pipeline.Resolve` for every commit, player-chosen or autonomous — this is what makes the
  acknowledgement actor-neutral by construction rather than by a second write path. When `Chosen` is
  null (nothing was open — `DecisionRecord.Outcome` in that case is the developer-only literal
  `"nothing was open to him"`), there is no `LastAction` at all.
- **`MyBusiness`** — `world.Businesses` filtered to `OwnerId == viewpoint`, exposing only
  `PayingTribute`. `Business.Resistance` is deliberately excluded — its own doc comment says
  "objective; characters only estimate it" — on the same self-knowledge footing milestone 014 gave
  `Cash`.
- **`AwaitingAnswers`** — `world.Requests` filtered to `AskerId == viewpoint`, resolved (and dropped
  from the list) the moment `viewpoint.Cognition.Testimony` shows the asked person's account of
  exactly that claim at or after the moment it was asked. Verified against `Org/Reporting.cs`: an
  answer to a `SeekCorroboration` is a `ReportToSuperior` candidate `Generators.FromRelationship`'s
  `"asked-to-account"` branch addresses back to the asker, so `Reporting.Deliver` calls
  `Cognition.Receive` on the asker's own cognition. Nothing here reads `World.Reports` or
  `Report.AnsweringClaim`.
- **`RecentTrustMovements`** — `world.AccountConflicts`/`AccountAgreements` filtered to
  `ListenerId == viewpoint`, within the existing `PlayerView.RecentWindow`, projecting only who moved
  and which direction. Scoped to trust alone because those two collections are the only
  relationship-mutating events with an existing audit trail of "this moved, this way, toward this
  person" — fear and grievance have none, and none was built for this milestone.

`PlayerOccasion.For`'s signature widened from `(ScheduledEvent, Pronouns)` to
`(ScheduledEvent, Character, Func<string,string>)` to add one new case: a tribute demand
(`EventKind.Incident`, `Payload.Note == "tribute-demanded"`) names the demander, and states force only
when the viewpoint holds a genuine held `PersonUsedViolence` claim naming him. Threaten
(`Relations.Frighten`) leaves no claim at all, so a threatened-but-not-forced demand reads as a plain
demand — proven as a deliberate mutation guard, not left to chance.

`World.cs`'s doc comments on `PerceivedConflict`/`PerceivedAgreement`/`AccountConflicts`/
`AccountAgreements`, previously "never rendered to the player," and `PlayerSnapshot`'s own header,
previously stating flatly that it never reads `World.Decisions`/`Requests`, are both amended to state
precisely what is now read and why it stays bounded — the same treatment milestone 014 gave the header
once already for `Cash`.

## Godot presentation

No redesign of the existing four-column layout. Added a fifth column, "WHAT JUST HAPPENED"
(`BuildCausalThread`): the last committed action, the viewpoint's own business status if he owns one,
and an "AWAITING ANSWERS" subsection. Extended the existing "HOW HE TAKES THEM" column
(`BuildAttitudes`) with a "RECENTLY" subsection for trust movement, phrased through a new
`PlayerNarration.Movement` helper matching `Standing`/`Wariness`'s existing no-numbers style.
`BuildDecision`'s existing `Occasion`/`Focus` rendering needed no change — the tribute-demand context
reaches Marco's panel automatically once `PlayerOccasion.For` names it.

## The two natural proofs, observed before any test was pinned

Both proofs were driven live against the unmodified seed-42 fixture — via a throwaway staged xunit
test, deleted before this commit — before any assertion or Godot self-test text was written, per the
milestone's own instruction not to guess or tune.

**Salvatore asks Vincent** (`cautious-vincent`, Salvatore controlled and viewpoint): the ask —
`"ask Vincent Russo for his own account of whether Bellini's grocery is holding back what it owes"` —
becomes available 1987-04-03. Vincent's own answer arrives naturally within about two days, through
the ordinary report channel, no staging, no tuning — and it *contradicts* what Salvatore already held
from "the books," producing a genuine `PlayerDisagreement` with both accounts, not a bare corroboration.
Both halves the milestone asked for (unresolved-then-resolved) are therefore provable from one
unmodified run, examined at two points in time.

**Marco and the tribute demand** (`baseline`, Marco controlled and viewpoint): the first demand lands
1987-03-08, from Vincent directly, before any escalation — a clean case for the "plain demand"
phrasing. A later demand, after refusal and delegation, carries a genuine held `PersonUsedViolence`
claim naming the by-then-different demander, proving the "force already used" phrasing is reachable
too, not merely written and never exercised.

## What was completed

### `tests/CrimeEmpire.Simulation.Tests/CausalFeedbackTests.cs` (new, 20 tests)

Covers both natural proofs end to end (immediate acknowledgement, genuinely-unresolved-not-merely-
unrendered status, resolution with attribution, save/load across the unresolved→resolved boundary,
repeated-snapshot stability, deterministic reruns), actor-neutrality (player-chosen vs.
`ResolveAutomatically` render identical `LastAction` text), both `PlayerOccasion` phrasing branches
(staged directly, since the natural run's second demand comes several days and choices later),
listener-scoped trust movement in both directions (staged against hand-built `AccountConflict`/
`AccountAgreement`, since the narrow scenario slice does not naturally produce a second character's
own movement to compare against), the developer-outcome exclusion when nothing was open, and three
dedicated resolution mutation guards (stale testimony, third-party sender, mismatched claim).

### Existing files extended

- `Session/PlayerSnapshot.cs` — four new records (`PlayerCommittedAction`, `PlayerBusinessStatus`,
  `PlayerRequest`, `PlayerRelationshipMovement`), four new `PlayerSnapshot` fields, derivation logic in
  `PlayerView.Build`, amended class doc comment.
- `Session/PlayerOccasion.cs` — widened `For` signature, new tribute-demand case, `Demand` helper.
- `Session/SimulationSession.cs` — one call-site update for the widened signature.
- `Session/PlayerNarration.cs` — new `Movement` phrasing helper.
- `Sim/World.cs` — four doc-comment amendments (no behavioral change).
- `CrimeEmpire.Godot/Game.cs` — fifth column, extended attitudes column, two new self-test flags
  (`--selftest-corroboration`, `--selftest-tribute`) with their own `Run*`/`*SelfTest` method pairs,
  matching `--selftest-goldenpath`/`--selftest-directaction`'s existing shape exactly.
- `tests/CrimeEmpire.Simulation.Tests/PlayerSessionTests.cs` — four call sites updated for
  `PlayerOccasion.For`'s widened signature (added a `BareActor()` helper); `Phrases(PlayerSnapshot)`
  extended to scan the four new fields, so the existing no-authored-string and no-numeric-leak tests
  actually cover the new surfaces rather than silently skipping them.

## Mutation checks — each confirmed to fail for the intended reason, then reverted

1. Drop the `t.At >= r.At` guard on request resolution → `Testimony_from_before_the_request_was_made_does_not_resolve_it`
   failed (`Assert.Single` on an empty collection).
2. Drop the sender filter → `An_account_from_a_third_party_does_not_resolve_a_request_addressed_to_someone_else`
   failed the same way.
3. Loosen claim matching from `.Equals` to `.Kind ==` → `An_account_of_a_different_claim_does_not_resolve_the_request`
   failed the same way.
4. Render `DecisionRecord.Outcome` when `Chosen` is null → `When_nothing_was_open_no_last_action_is_shown`
   failed, showing the literal leaked string `"nothing was open to him"`.
5. Read `Relations.Frighten`'s fear effect as a "threatened" fact in `PlayerOccasion.For` — the first
   attempt at this mutation did not fail any test, because the existing test's staged demander had no
   fear set on it; the test was strengthened to actually call `Relations.Frighten` first (matching what
   `Strategies.cs`'s real Threaten branch does), and the strengthened test then failed correctly against
   the mutation. Recorded because it is itself a finding: a mutation guard that stages too little proves
   nothing, and this one very nearly shipped that way.
6. Drop the `ListenerId == who.Id` filter on trust movements → both
   `Trust_movement_is_shown_only_to_the_listener_whose_own_relationship_moved` and
   `Trust_agreement_warms_and_is_also_listener_scoped` failed, showing Vincent's own movement toward
   Tommy leaking into Salvatore's snapshot.

## Verification

- `dotnet build CrimeEmpire.sln` — clean, 0 warnings, 0 errors.
- `dotnet test CrimeEmpire.sln` — **543 passed, 0 failed** (523 pre-existing + 20 new).
- `dotnet run --project src/CrimeEmpire.Runner -- --verify --seed 42 --days 90` — deterministic,
  `9AF57665067AEA11` on both runs, matching the accepted baseline exactly.
- `dotnet run --project src/CrimeEmpire.Runner -- --compare --seed 42` — all five variant trace hashes
  byte-identical to `REVIEW_LEDGER.md`'s recorded baselines: `baseline` `9AF57665067AEA11`,
  `cautious-vincent` `86EC1ADA4A4E9179`, `watchful-boss` `84AC3F65E4102EBA`, `disloyal-vincent`
  `9A6E0E518294532F`, `resentful-tommy` `3C4483640153DA88`. This milestone changed no simulation
  behavior, only projection — confirmed rather than assumed.
- `--variant disloyal-vincent --viewpoint salvatore` and `--variant baseline --viewpoint vincent` — both
  exit 0.
- Godot self-tests, all headless via `Godot_v4.7.1-stable_mono_win64_console.exe --headless --path
  src/CrimeEmpire.Godot`: `--selftest`, `--selftest-goldenpath`, `--selftest-directaction`, the
  two-process `--selftest-restart-save`/`--selftest-restart-load` pair — all unchanged and passing —
  plus the two new `--selftest-corroboration` and `--selftest-tribute`, both passing on the real
  interface. The corroboration run's actual captured screen shows all four new surfaces working
  together naturally in one unmodified run: the acknowledgement, the unresolved request, and — after
  one more event — the resolved request, the belief marked `(contradicted)`, the disagreement listing
  Vincent by name, and `RECENTLY` reading `"his trust in Vincent Russo cooled"`.

## Deferred, per the milestone's own exclusions

Full GUI redesign; maps/portraits/animation/art; restructuring the four-column layout beyond adding a
fifth; the wrapped-date/toolbar-sizing debt; a prose rewrite of `PlayerNarration`'s wording;
combining "You control"/"You see through"; a general operations dashboard; recruitment/personnel/
territory/additional businesses; new criminal actions or consequences; new informant mechanics;
relationship tuning or new dimensions; an omniscient history/developer console/queryable trace
database. Recorded as playtest-discovered presentation debt in `docs/ROADMAP.md` rather than silently
dropped.

## Commit

Original implementation: `ae06f61`.

## Correction (Codex review of `ae06f61`)

Codex reviewed `ae06f61` and returned **FAIL**: two P1 implementation defects and one P2 proof gap.
Matt authorized a correction to milestone 018 only — not a new milestone. This section records what
was found, what was fixed, and the new proofs; the account above is left as it was written and not
edited to look like it was always correct.

### Finding 1 (P1) — pending and declined were the same state

The original `Answered(InformationRequest)` check resolved a request from the asker's own testimony
alone. A request the asked character had already decided to decline — chosen `DoNothing`, or a
`Partial`/`False` report that withholds the very claim asked about, or anything else that does not
assert the claim — read identically to one he had simply not gotten to yet: both stayed "pending"
forever. This contradicts the canonical `InformationRequest` doc comment itself: "whether the other
man chooses to say anything is his decision, and silence is itself an answer."

**Fix.** `InformationRequest` gained a `WakeEventId` field — the id of the
`EventKind.RoleReview`/`"asked-to-account"` event the request itself schedules, captured by reordering
`Commit.Apply`'s `SeekCorroboration` case to schedule the wake before filing the request rather than
after (`Decision/Commit.cs`). A new `Domain.RequestDisposition` enum (`Pending`, `Answered`,
`Declined`) is derived, never stored: `Answered` from testimony exactly as before; `Declined` when a
`DecisionRecord` already exists for the asked character with `TriggerEventId == WakeEventId` and no
matching testimony; `Pending` otherwise. Both reads are of already-authoritative, already-replayed
state (`World.Decisions`, `Cognition.Testimony`) — no new write anywhere in `Commit.cs`, `Pipeline.cs`,
or `Strategies.cs`, and no reference anywhere to elapsed calendar time or to whether the asked
character is controlled. `PlayerRequest` carries `Disposition`; `Answered` requests still drop out of
`AwaitingAnswers` entirely (the account already reaches `Known`/`Recent`/`Disagreements` the existing
way), but `Pending` and `Declined` both remain visible, distinguished in `Game.cs`'s rendering ("no
answer yet" vs. "he chose not to say").

### Finding 2 (P1) — force matched the demander, not the demand

`PlayerOccasion.Demand`'s "already used force" check matched only `PersonUsedViolence.Subject ==
demanderId`, ignoring `.Object` (the business the violence was against). The same demander's violence
at an unrelated business — plausible in this fixture, since one executor can be sent against more than
one shop — would have read as "already used force over this" for a demand it had nothing to do with.

**Fix.** `Strategies.cs`'s two `"tribute-demanded"` `EventPayload` constructions now set `AboutClaim =
new Claim(ClaimKind.BusinessRefusesTribute, business.Id)` — reusing the same claim shape the owner's
own resistance belief is already learned from at that exact call site, per the review's instruction to
carry the business identity through the existing typed payload rather than add a new field.
`PlayerOccasion.Demand` now requires both `Claim.Subject == demanderId` and `Claim.Object ==
businessId` before naming force.

### Finding 3 (P2) — proof gaps

Six categories of required proof were added or corrected, all in
`tests/CrimeEmpire.Simulation.Tests/CausalFeedbackTests.cs` (5 new tests; total file count 20 → 25):

- **Pending → declined.** `Declining_to_answer_leaves_the_request_marked_declined_not_pending` — not
  staged: Vincent's natural, unmodified delegation audit
  (`ScenarioReachTests.The_delegator_puts_his_question_to_the_man_he_sent`) reaches Tommy's own
  "asked-to-account" pause on 1987-04-04, his very first pause when controlled. `"let it lie"` is
  always offered there (`Generators.FromTrigger`'s floor candidate); choosing it is Tommy's own real
  decision to stay silent at a real pause, not a shortcut.
- **Save/load for pending and declined.** The existing pending→answered save/load test was kept
  unchanged; `Save_load_preserves_a_declined_requests_disposition` adds the declined case through the
  identical `PersistentSession` replay mechanism.
- **Player/autonomous parity.** Reaching for this test found a genuinely useful fact rather than a
  clean confirmation: Tommy's own top-ranked preference at his first controlled pause, read directly
  off `PreparedDecision.Scored[0]` before resolving, is *not* the candid answer — it is a `Partial`
  report that withholds precisely the claim asked, his own self-protective instinct. (This differs from
  the fully-autonomous baseline's behavior at what appears to be the identical decision — a separate,
  unexplained timing effect of pausing, recorded honestly in `docs/ROADMAP.md`'s known technical debt
  rather than papered over or chased down, since root-causing it was outside this correction's
  authorized scope.) `Request_disposition_computation_is_identical_whether_declining_was_autonomous_or_player_chosen`
  therefore compares `ResolveAutomatically()` against an explicit `Choose()` of the identical rendered
  option, both with Tommy controlled — isolating exactly the variable the review asked about (did a
  person or the pipeline choose this specific resolution?) rather than conflating it with whether Tommy
  was controlled at all.
- **Cross-business violence must not read "over this".**
  `The_same_demanders_violence_at_a_different_business_does_not_read_as_over_this` — Nunzio holds a
  genuine `PersonUsedViolence(tommy -> bellini-grocery)` claim while Tommy demands tribute from
  Nunzio's own bakery; the demander matches, the business does not, and the phrasing stays a plain
  demand.
- **Action-kind audit.** `Every_reachable_action_kind_renders_through_the_shared_last_action_projection`
  walks every `DecisionRecord.Chosen` across all five variants' full 90-day autonomous runs and renders
  each through `PlayerOption.Describe` — the identical function `LastAction` calls. Reachable at seed
  42: `StartStrategy`, `ContinueStrategy`, `AlterStrategy`, `DelegateStrategy`, `ReportToSuperior`,
  `SeekApproval`, `SeekCorroboration`, `Retaliate`, `Concede`, `Refuse`, `DoNothing` — 11 of 14.
  **Not reached by any variant, recorded rather than forced:** `AbandonStrategy`, `PostponeStrategy`,
  `RequestHelp` — matching `ROADMAP.md`'s existing "apparently-dead lines" finding.
- **Mutation checks**, each confirmed to fail for the intended reason and then reverted: dropping the
  `t.At >= r.At` guard, the sender filter, and claim-equality (all three carried over unchanged from
  the original implementation, re-verified against the corrected code); rendering `DecisionRecord
  .Outcome` when `Chosen` is null; reading `Relations.Frighten`'s effect as a "threatened" fact; and
  exposing another character's trust movement. Also newly exercised for this correction: the existing
  `A_demand_after_force_names_it_and_a_demand_after_only_a_threat_does_not` test now supplies
  `AboutClaim` on its staged triggers, matching the corrected production shape — without it, the
  business-scoping fix itself would have made that test fail to compile, which is its own form of
  mutation coverage for the missing field.

### Verification (post-correction)

- `dotnet build CrimeEmpire.sln` — clean, 0 warnings, 0 errors.
- `dotnet test CrimeEmpire.sln` — **548 passed, 0 failed** (543 prior + 5 new).
- `dotnet run --project src/CrimeEmpire.Runner -- --verify --seed 42 --days 90` — deterministic,
  `9AF57665067AEA11` on both runs, unchanged.
- `dotnet run --project src/CrimeEmpire.Runner -- --compare --seed 42` — all five variant trace
  hashes byte-identical to the pre-correction figures and to `REVIEW_LEDGER.md`'s recorded baselines:
  `baseline` `9AF57665067AEA11`, `cautious-vincent` `86EC1ADA4A4E9179`, `watchful-boss`
  `84AC3F65E4102EBA`, `disloyal-vincent` `9A6E0E518294532F`, `resentful-tommy` `3C4483640153DA88`. The
  correction's `Commit.cs` reordering and `Strategies.cs` payload addition touch no candidate
  generation, scoring, or RNG draw, and this confirms it rather than assumes it.
- `--variant disloyal-vincent --viewpoint salvatore` and `--variant baseline --viewpoint vincent` —
  both exit 0.
- Godot self-tests, all headless: `--selftest`, `--selftest-goldenpath`, `--selftest-directaction`,
  `--selftest-restart-save`/`--selftest-restart-load` — unchanged and passing; `--selftest-corroboration`
  and `--selftest-tribute` — both re-run against the corrected code, both still passing (the
  corroboration proof's natural run is genuinely `Answered`, never touching the `Declined` branch; the
  tribute proof's plain-demand phrasing is unaffected by the business-scoping fix, which only changes
  the force-already-used branch).

### Documentation updated in place

`src/CrimeEmpire.Simulation/Session/PlayerSnapshot.cs` and `Domain/Report.cs`'s doc comments (mutable,
corrected in place, not append-only — unlike this archive); `docs/DESIGN_DECISIONS.md`'s "Causal
feedback" section, whose second and fourth bullets stated the pre-correction rules and are now rewritten
to state the corrected ones; `docs/ROADMAP.md` gained the parity-timing finding above.

### Correction commit

Correction (pending vs. declined, and demand-business scoping): `b9dfa49`.

## Second correction (Codex review of `b9dfa49`)

Codex reviewed `b9dfa49` and returned **FAIL** again: one P1 defect in the first correction itself,
and two P2 gaps. The demand/business fix from the first correction was accepted as correctly
implemented and is untouched here. This section records what was found, what changed, and the new
proofs; neither the original account nor the first correction's account above is edited.

### Finding 1 (P1) — the first correction's own fix was itself a private-decision leak

`RequestDisposition.Declined`, as shipped in the first correction, was derived from
`World.Decisions.Any(d => d.ActorId == r.AskedId && d.TriggerEventId == r.WakeEventId)` — whether a
`DecisionRecord` existed for the *asked* character. Codex's review was direct: the asker never
receives any message establishing that the asked person decided anything at all, so rendering "he
chose not to say" exposed a private mental event nothing in the fiction communicated to him. This is
the canonical information boundary this project holds everywhere else, violated in the one place a
projection milestone was supposed to be safest — reading a field's mere *existence*, not its content,
is still reading state nothing entitles the asker to.

**Fix.** `DispositionOf` no longer reads `World.Decisions` for anybody but the viewpoint's own
`LastAction`. Two different private, uncommunicated choices by the asked person — silence
(`DoNothing`), or a `Partial` report that withholds precisely the asked claim — now both produce no
testimony at all and are structurally indistinguishable: both read `Pending`.

**A same-pass attempt to salvage a three-way split was also wrong, and a test caught it within this
same correction.** The first draft of the fix split `Answered` into `Answered` (a communicated
affirmation) and `Declined` (a communicated denial), reasoning that a denial is a "refusal" and
therefore legitimately distinguishable from silence — genuinely communicated, so not a boundary
violation. `A_delivered_answer_resolves_the_request_and_attributes_the_account_to_vincent` and
`Save_load_preserves_an_unresolved_request_and_its_later_resolution` immediately failed: the natural
proof scenario has Vincent give Salvatore a full, sincere, informative account that happens to
contradict what Salvatore already believed from "the books" — a real answer, not a refusal. This
simulation's report vocabulary has no utterance distinct from "an account, possibly negative": Candid
and False both assert a stance to the recipient, differing only in which way the stance points, and
nothing communicates "I decline to discuss this" as a thing in itself. `RequestDisposition` is
therefore two values, not three — `Pending`/`Answered` — with direction (affirms or denies) left to the
existing `Known`/`Recent`/`Disagreements` surfaces to render, exactly as they already did for every
other belief. Recorded because the mistaken three-way split, and the test that caught it, are both
worth keeping visible: the failure is exactly the kind of finding a mutation-check exists to produce,
just discovered by an assertion against the natural scenario instead of a deliberately staged mutation.

### Finding 2 (P2) — the action-kind audit didn't exercise the surface under review

`Every_reachable_action_kind_renders_through_the_shared_last_action_projection` called
`PlayerOption.Describe` directly on candidates pulled from a post-hoc scan of `world.Decisions` after
a full 90-day run — proving the renderer works, never proving `PlayerView.Build`/
`PlayerSnapshot.LastAction`'s own selection logic does.

**Fix.** The test now drives each of the five variants event by event via `SimulationSession
.StepEvent()`. After every step that adds one or more new `DecisionRecord`s, it calls
`PlayerView.Build(world, decision.ActorId, decision.At)` — the actor's real snapshot, at the moment
his decision was fresh — and asserts on `LastAction` directly: non-null, timestamped to the decision,
non-empty, not equal to `DecisionRecord.Outcome` (the developer-only literal), and free of `#`
(a `Claim.ToString()` correlation suffix) or the raw candidate id. Confirmed to actually exercise the
selection logic by mutation: temporarily changing `LastAction`'s construction to render
`lastDecision.Outcome` instead of the candidate's description made this test fail immediately, citing
the leaked outcome text; reverted. The reachable/unreached `ActionKind` findings themselves are
unchanged from the original correction (11 of 14 reachable; `AbandonStrategy`, `PostponeStrategy`,
`RequestHelp` recorded as not reached, not forced).

### Finding 3 (P2) — `WakeEventId` was invisible to both replay/request comparators

`InformationRequest.WakeEventId`, added by the first correction, is genuine persistent,
replay-reconstructed state — but neither of this project's two independent request comparators
(`SimulationReplayTests.Snapshot`, `InformationTransmissionTests.Channel` — deliberately not shared
between the two files, per this project's existing convention) included it in their `request|...`
lines. Two requests differing only in which wake event they scheduled would have compared equal in
both, meaning a regression that dropped or corrupted the field silently would have passed every
existing determinism and replay-fidelity test. Separately, the correction's own account claimed "no
new persistent state was added" — true of `RequestDisposition` (derived, never stored) but not of
`WakeEventId` itself, a new field on an existing, replayed record.

**Fix.** Both comparators now include `q.WakeEventId` in their request line. One focused proof per
comparator — `SimulationReplayTests.The_snapshot_distinguishes_requests_that_differ_only_by_their_wake_event_id`
and `InformationTransmissionTests.The_channel_distinguishes_requests_that_differ_only_by_their_wake_event_id`
— construct two otherwise-identical requests differing only in `WakeEventId` and assert the two
comparators' output differs. Both confirmed to fail against the pre-fix line (mutation-checked, then
reverted). The "no new persistent state" claim is corrected everywhere it appeared in mutable prose
(`docs/DESIGN_DECISIONS.md`, `docs/CURRENT_MILESTONE.md`, `PlayerSnapshot.cs`/`Report.cs`'s doc
comments) to the accurate, narrower claim: no *separate response log* was introduced — `WakeEventId`
is a field on an existing record, not a new collection or write path, and `RequestDisposition` itself
remains genuinely derived.

### Mutation checks — each confirmed to fail for the intended reason, then reverted

1. Reintroduce a `World.Decisions`-based check into `DispositionOf` (updated for the two-value enum:
   `communicated || askedPersonHasDecided` → `Answered`) →
   `Two_different_private_non_communicating_choices_are_indistinguishable_to_the_asker` failed: both
   requests wrongly resolved to `Answered` and vanished from `AwaitingAnswers` entirely.
2. Drop `WakeEventId` from `SimulationReplayTests.Snapshot`'s request line →
   `The_snapshot_distinguishes_requests_that_differ_only_by_their_wake_event_id` failed (strings
   compared equal that must not).
3. The identical drop from `InformationTransmissionTests.Channel` →
   `The_channel_distinguishes_requests_that_differ_only_by_their_wake_event_id` failed the same way.
4. Render `DecisionRecord.Outcome` in place of the candidate's rendered description in `LastAction`'s
   construction → `Every_reachable_action_kind_renders_through_the_shared_last_action_projection`
   failed, citing the leaked outcome text (`"began SecureTribute(..."` vs. the expected offered
   wording).

### Verification (post-second-correction)

- `dotnet build CrimeEmpire.sln` — clean, 0 warnings, 0 errors.
- `dotnet test CrimeEmpire.sln` — **552 passed, 0 failed** (548 prior + 4 new: the indistinguishability
  proof, the pending-after-a-private-decline save/load proof, and one focused `WakeEventId` proof per
  comparator; two tests were corrected in place rather than added, and two were renamed to match the
  corrected semantics).
- `dotnet run --project src/CrimeEmpire.Runner -- --verify --seed 42 --days 90` — deterministic,
  `9AF57665067AEA11` on both runs, unchanged.
- `dotnet run --project src/CrimeEmpire.Runner -- --compare --seed 42` — all five variant trace hashes
  byte-identical to every prior figure recorded in this archive and in `REVIEW_LEDGER.md`.
- `--variant disloyal-vincent --viewpoint salvatore` and `--variant baseline --viewpoint vincent` —
  both exit 0.
- Godot self-tests, all headless: `--selftest`, `--selftest-goldenpath`, `--selftest-directaction`,
  `--selftest-restart-save`/`--selftest-restart-load`, `--selftest-corroboration`,
  `--selftest-tribute` — all unchanged and passing. The corroboration proof's natural run resolves
  Vincent's account as `Answered` (it always did; the disposition it exercises was never `Declined`
  under either the first or second correction's model), and the tribute proof is unaffected by any of
  this finding's changes.

### Documentation updated in place (second correction)

`src/CrimeEmpire.Simulation/Session/PlayerSnapshot.cs`, `Domain/Report.cs`, and `CrimeEmpire.Godot
/Game.cs`'s doc comments and rendering; `docs/DESIGN_DECISIONS.md`'s "Causal feedback" section's second
bullet (request disposition) and its "no new persistent state" bullet, both rewritten to state the
corrected rules and the corrected, narrower claim; `docs/CURRENT_MILESTONE.md`.

### Second correction commit

See the commit this correction is part of.
