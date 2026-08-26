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

See the commit this file is part of.
