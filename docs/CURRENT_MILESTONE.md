# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**No milestone is active. Do not start one.** Milestone 009 is implemented, **was reviewed and
rejected once, and is corrected and awaiting re-review**; milestone 010 has not been chosen and must
not be inferred.

Codex rejected `901d345` on three findings, all accepted by Matt: the pending decision passed
`ScheduledEvent.Cause` through, so a delegated operation's failure or completion reached its owner
before anybody had told him (P1); the player-facing DTOs were castable back to mutable lists and left
raw `Claim`/`EventId` reachable; and the Godot self-test printed a failure while exiting 0. All three
are corrected, with the P1 fix pinned by a mutation check — reverting it fails seven tests. Full
account in `docs/milestones/009-godot-playable-shell.md`, "Correction 1".

Milestones 001–008 are complete and accepted. The most recent, **009 — Godot Playable Shell**, is
implemented and closed but **not reviewed and not accepted by anybody**. It added a Godot 4.7.1 .NET
project under `src/CrimeEmpire.Godot`, an engine-neutral `SimulationSession` boundary, and a
prepare/resolve split in the decision pipeline so a person can answer one character's decisions
through the same commit path an NPC uses. Full record, including Matt's authorized scope reproduced
verbatim and thirteen rulings, is in `docs/milestones/009-godot-playable-shell.md`.

**It changed no simulation behaviour**, and neither did the correction. All five trace hashes, all
five chosen-action digests, every decision, report, request and conflict count, and both relationship
columns are unchanged from milestone 008's accepted baseline, and all 30 viewpoint renders are
byte-identical to both `3f08685` and `901d345`. Tests went 305 → 343 → 353.

`REVIEW_LEDGER.md` alone defines review coverage; consult its checkpoint directly rather than
inferring status from prose anywhere else, including this file. **The checkpoint still stands at
`7e0700e`** and cannot advance, because `3f08685` sits between it and everything later with no
established outcome. Three commits are now beyond it: `3f08685` (unreviewed), `901d345` (reviewed and
rejected), and this correction (unreviewed). Review takes them in that order.

## What milestone 009 found, because it should shape the next scope decision

**The engine question was four milestones of carried debt and one line of configuration.** Godot
4.7.1 hosts .NET 8 — `GodotPlugins.runtimeconfig.json` says so — so the simulation library
multi-targets `net8.0;net10.0` and nothing else moved. The recorded fallback is exactly what happened,
and there is no coupling to report: no Godot reference, no `#if`, no conditional code in the library.

**A player is a preference, not a second action implementation.** Deliberation now splits at the
pipeline's fifth question — which available option do you prefer — and only that question is answered
differently. `Pipeline.Deliberate` is `Resolve(Prepare(...), null)` and is still what `Runner` calls
for everybody autonomous, so NPC behaviour is unchanged *by construction*. The byte-identity tests
check that the construction is what it appears to be; they are not what establishes the claim.

**Two surfaces showing a character what he knows would have been one derivation too many.**
`IntelligenceWriter` had held the source limit since milestone 003. A second surface deriving it
independently is this project's signature failure — a distinction drawn in one place and dropped on
the way to the next — and the divergence would have stayed invisible until one of them leaked.
`PlayerView.Build` now owns it, the console renderer became a layout over its snapshot, and all 30
viewpoint renders are byte-identical, which is what makes that a refactor rather than a rewrite.

**And the fixture answers a player differently in four choices.** Taking the first offered option
every time, Vincent asks Tommy for an account, twice asks Salvatore to relax the no-violence rule,
then answers his boss about the grocery — and ninety days later the harbour is exactly where it
started. The autonomous Vincent breaches the policy and the grocery pays. Same character, same
beliefs, same options; a different history because somebody else answered the last question.

**And the boundary's first version was wrong in the way this project is reliably wrong.** Its ruling 7
argued that every deliberation-waking cause is authored from the waking character's own side — an
enumeration of the schedulers that existed, presented as a structural guarantee. Two of them hand the
*owner* of a delegated operation its outcome, so a player controlling Vincent read "Bellini's grocery
held out against force" on a day nobody had told him anything. The replacement enumerates nothing: a
closed vocabulary, silent by default, so being wrong now requires adding an entry rather than merely
adding an event.

Question to carry: **is this claim true of the thing I am saying it about, or only of the instances of
it I happened to look at?** And its sibling, which the correction also turned on: **is this a rule the
type system keeps, or a rule somebody has to remember?** Milestone 009 got `World` out of the UI's
reach with `internal` and console text out with a project reference; it did not get `Cast.Build` out
of reach, and said so.

## Carried forward

Open items the next scope decision should see. Fuller versions live in the milestone archives and
`ROADMAP.md`'s technical-debt list. **Everything carried out of milestone 008 is still carried; 009
touched none of it.**

New from milestone 009:

- **The timing of a pause is observable even when the occasion is not.** The controlled character is
  woken when a delegated operation blocks or completes, so a player sees him stop on the day it
  happened. Closing it means not waking him, which changes autonomous behaviour.
- **`Generators.FromRelationship` draws a corroboration target from the whole organisation's
  membership**, not from people the character has heard of. Pre-existing, inert at this cast size, and
  more visible now that option wording resolves ids to names.
- **The player cannot see why an option is unavailable.** Right for utility scores; arguably wrong for
  "he does not know that the bakery is holding out", which is the line that proves the simulation is
  belief-limited rather than merely claiming to be.
- **Nothing stops a future Godot script from calling `Cast.Build` and `Runner.Run` directly.** A
  player-contract assembly would close it; a fifth project was out of scope.
- **`AGENTS.md` now has two things it does not mention** — `docs/RELATIONSHIPS.md` in its
  conditional-reading list, and the Godot headless check in §Verification. Both flagged, neither
  taken, because no ruling authorized editing it.

Still open from milestone 008 and earlier:

- **Concealment does not quiet the witnesses it is named for**, and `believedWitnesses` is scanned
  globally rather than scoped to the incident. Together they are what keeps an executor from ever
  denying to his delegator.
- **The `0.9 × Loyalty` versus `0.4` denial-premium question is unruled**, deliberately. It gates the
  candidate above.
- **Obligation is read but never moves.** `Relations.Establish` is its only writer and that is
  scenario construction.
- **Nothing raises trust.** Conflicts lower it and no runtime path restores it, so a relationship can
  be damaged and never repaired.
- **Negative trust and decay are deferred, not retired**, each with a stated condition for return.
- **`GrievanceWeight` is unbounded.** A cap was considered as 008's remedy and explicitly rejected in
  favour of unbundling, so it is open rather than answered.
- **Tuning guesses**: `FirstHandTestimony` 0.15, `Discovery` 0.10, `Relations.ConflictTrustCost` 0.35,
  and `LoyaltyReading.GrievanceWeight` 0.50.
- **The cast is six and that is a ceiling, not a trend.**
- **The lifecycle loses rulings** when the archive and the reset land in one commit. Mitigated again by
  reproducing them in the archive. `AGENTS.md` is Matt's.
- The bakery is never collected from; the boss-side conflict path is covered only by staged unit
  tests; the empty-domain label `ConcealIncident(, target=...)`.

## Longer-standing deferrals

- relevance tiering and its continuous-calendar engineering risk;
- persistence, SQLite, and save/load — now the most conspicuous gap, because a session ends when the
  process does;
- ~~Godot / `net10.0` compatibility~~ — settled by milestone 009;
- art, map, tilemaps and animation, which passing the Godot sequencing gate did **not** unlock;
- generalized rumor, evidence, prosecution, media, and public-information channels;
- broader organizations, diplomacy, careers, corruption, and surveillance systems;
- the redundant test-project target framework — **retired**: since milestone 009 removed the
  centralized `TargetFramework` assignment, every project names the framework it wants and that entry
  is no longer redundant.

## Ordered review process

Review is **manual**. There is no monitor running on a timer, no checkpoint the repository keeps for
itself, and nothing that will notice a commit unless somebody points a review at it.

- Matt takes commits in order, oldest unreviewed first, one at a time.
- Each review names the exact commit whose diff was inspected.
- A later documentation commit does not stand in for the implementation commit beneath it.
- The coverage table in `REVIEW_LEDGER.md` is the record. It is maintained by hand, which is why it
  is the authority rather than the prose around it.

Never write "verified" or "closed" from a review report alone. A report must name the exact commit
reviewed, and Matt must confirm acceptance. That rule exists because the record twice claimed a
verification that had not happened — and, at `9a29342`, once recorded real measurements taken before
the last edit as though they described the commit.
