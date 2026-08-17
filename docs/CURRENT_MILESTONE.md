# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**No milestone is active. Do not start one.** Milestone 009 is implemented and **has been reviewed
five times: four rejections, then a clean round at `0f52d75`.** Every finding is corrected.
**Matt has recorded no acceptance of it**, and a clean review is not one — `REVIEW_LEDGER.md`'s
rule 3. Milestone 010 has not been chosen and must not be inferred.

Codex rejected `901d345` on three findings, all accepted by Matt: the pending decision passed
`ScheduledEvent.Cause` through, so a delegated operation's failure or completion reached its owner
before anybody had told him (P1); the player-facing DTOs were castable back to mutable lists and left
raw `Claim`/`EventId` reachable; and the Godot self-test printed a failure while exiting 0.

Codex then reviewed `b4900aa`, confirmed those three fixed, and returned **one further P1**:
`Generators.FromRelationship` still picked corroboration targets out of the authoritative
organisation roster without establishing that the actor knew that person existed, and the
player-facing option then rendered the name. Also accepted.

Codex then reviewed `c447a23` and returned **the same P1 again**: that correction narrowed the roster
by knowledge and widened it back by "office relationships" derived from `Pipeline.SuperiorOf` and
`Pipeline.SubordinatesOf` — which are authority scans over the same roster. A same-organisation
stranger one rung below the actor was still reachable and still rendered by name, and none of the
three tests written for it could see the difference. Accepted, and corrected under Correction 3.

Codex then reviewed `49b71a6` and returned **no behavioural findings** and **two documentation
findings**: the canon documents still described the live rule as the rejected second correction had
left it, naming `HeardOf` rather than `KnownTo`, and the matching source comments with it. Accepted,
and corrected by `0f52d75` — which Codex then reviewed clean.

**Every round is corrected, and each of the three code rounds is pinned by a mutation check.** Full
account in `docs/milestones/009-godot-playable-shell.md`, Corrections 1–3; `0f52d75` was
documentation and source comments only and has no numbered correction of its own.

Milestones 001–008 are complete and accepted. The most recent, **009 — Godot Playable Shell**, is
implemented and closed. It has been **reviewed five times, rejected four times, and corrected after
each; the fifth round returned no findings, and no version of it has been accepted by Matt.** It
added a Godot 4.7.1 .NET project under
`src/CrimeEmpire.Godot`, an engine-neutral `SimulationSession` boundary, and a prepare/resolve split
in the decision pipeline so a person can answer one character's decisions through the same commit
path an NPC uses. Full record, including Matt's authorized scope reproduced verbatim and thirteen
rulings, is in `docs/milestones/009-godot-playable-shell.md`.

**The shell itself changed no simulation behaviour, and neither did the first or third correction,
nor `0f52d75`. The second one does, deliberately.** `cautious-vincent` moved — trace `A8A1BBD12D5334C2` →
`96EAE1A72850F3D7`, decisions 21 → 19, conflicts 3 → 2 — because Salvatore had been putting a
question to a man nothing in his head established. The other four variants are byte-identical to
milestone 008's accepted baseline throughout. Tests went 305 → 343 → 353 → 366 → 369. Current figures
in `REVIEW_LEDGER.md`.

`REVIEW_LEDGER.md` alone defines review coverage; consult its checkpoint directly rather than
inferring status from prose anywhere else, including this file. **The checkpoint now stands at
`0f52d75`**, the ordered backlog having been worked through from `3f08685` — which was itself
reviewed and rejected on one P2, and whose correction is what carried the checkpoint forward. One
commit is beyond it: the reconciliation that moved it, which has no row and needs reviewing in its
turn.

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
viewpoint renders were byte-identical across that change, which is what made it a refactor rather
than a rewrite. (One has moved since, under Correction 2 — a behaviour change, not a rendering one.)

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

- **Membership is not knowledge; a named office is.** **Settled by the third correction, and the
  boundary is implemented** — `Acquaintance.KnownTo` is the one derivation both `PlayerView` and
  candidate generation read: what the character holds in cognition and social state, widened only by
  the holders of his own organisation's `Organization.Offices` and `BossId`. Neither
  `Generators.FromRelationship` nor anything else selects a target from the organisation roster any
  more, and no authority scan stands in for an office. **What is carried forward is the design
  question, not the rule**: a soldier holding no office is unreachable for corroboration until
  somebody actually names him, which in `cautious-vincent` never happens for Tommy, and whether an
  outfit whose boss cannot name his own soldiers is the right model is unanswered.
- **The timing of a pause is observable even when the occasion is not.** The controlled character is
  woken when a delegated operation blocks or completes, so a player sees him stop on the day it
  happened. Closing it means not waking him, which changes autonomous behaviour.
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
