# Milestone 003 — Information Transmission Slice

Status: complete. Awaiting Codex review.

## What was attempted

A deliberately narrow slice of `SIMULATION_ARCHITECTURE.md`'s emergence prototype (Validation
Sequence step 2), which as written bundles six subsystems. Matt scoped this to the information half
only: **direct observation, one explicit report/message channel, one deceptive or incomplete
report, one conflicting source, and a player-readable history constrained to the viewpoint
character's information — with generalized rumor propagation explicitly excluded.**

That scope is near-verbatim `INFORMATION_AND_LEGIBILITY.md`'s own "Pre-MVP Kernel Scope", and that
document's worked test scenario is the harbour scenario milestone 001 already built.

## What was completed

- **`Report` as a first-class object** (`Domain/Report.cs`), modelling the doc's "Report or
  Message" contract: sender, recipient, asserted claims with the stance and confidence *as
  asserted*, a `Withheld` list, and a `ReportCandor` of Candid/Partial/False. Stored on
  `World.Reports` as developer truth.
- **`Reporting.Compose`/`Deliver`** (`Org/Reporting.cs`) — the single report channel. Reports are
  composed from the sender's own `PerceivedSituation`, never from world state, which makes "sources
  never communicate facts unavailable to them" structural rather than a discipline.
- **Candour as a scored decision.** Candid/Partial/False are three *candidates*, generated only
  when the actor holds a claim naming himself as having used force or breached a policy (nothing
  else is worth lying about), gated by salience and scored by utility against self-protection,
  believed witnesses, and loyalty to the recipient. No code branches on a trait to produce a lie.
- **Append-only testimony** (`Domain/Cognition.cs`). `Receive` records every account verbatim and
  never collapses it, while the settled belief keeps one stance per claim so no existing decision
  changed what it reads. Contradiction *erodes* confidence rather than letting the newer account
  overwrite the older; corroboration only counts from an independent sender; direct observation
  resists hardest but is not immune. `IsContested` reports disagreement.
- **`SeekCorroboration`** — a recipient-initiated use of the same channel, which
  `INFORMATION_AND_LEGIBILITY.md` sanctions directly ("Leaders can request audits, seek
  corroboration"). Needed because `Pipeline.SuperiorOf` returns the *lowest* authority above an
  actor, so Tommy reports to Vincent and could never reach Salvatore unaided.
- **`IntelligenceWriter`** (`Runner/Trace/`) plus a `--viewpoint` flag: a player-facing account
  built only from one character's beliefs and testimony, with qualitative confidence only,
  conflicting accounts side by side with attribution, and an explicit "what he cannot settle"
  section. Kept in a separate file from `TraceWriter` because the doc requires developer traces
  stay separate from player-facing information.
- **`disloyal-vincent` variant** — the mirror of `watchful-boss`, cutting the bond without touching
  a trait, which is what flips Vincent from omission to outright denial.
- Observation is now genuinely *direct observation*: discovery previously recorded claims as
  `SourceKind.Rumor` sourced to "the street", which is rumour vocabulary for a mechanic that is a
  single discovery roll with no network, no mutation and no re-transmission.

## Tests / success criteria and results

`dotnet test` — **17/17 passing** (was 6). `--verify` is DETERMINISTIC on all four variants, and
`--compare` reports 4 configurations, 4 distinct histories.

New tests in `InformationTransmissionTests.cs`:

- **No-leak (the load-bearing one).** For every claim held by anyone or asserted in any report, if
  the viewpoint character does not hold it, its exact rendered wording must not appear in his view.
  Runs across all four variants and calls the renderer's own `Describe`, so it cannot pass while
  the renderer drifts.
- Developer-only material (report framing, candour, utility component names) never reaches the view.
- A report conveys only what its sender holds, and withheld claims are genuinely absent.
- An incomplete report leaves the recipient without what was withheld.
- Candour distinguishes lying from omitting: a denial asserts `Rejects` on a claim its sender
  holds; an omission never asserts `Rejects` at all.
- A contradicting source leaves a conflict that is still attributable to a named sender.
- Being contradicted shakes a directly observed belief without erasing it, and does not rewrite
  its acquisition time.
- Corroboration counts only from a new source, not the same source repeating itself.
- `SimulationReplayTests.Snapshot` extended to cover reports and testimony.

**The no-leak test was mutation-checked.** Pointing the renderer at every character's cognition
instead of the viewpoint character's made it fail in all four variants; it was then restored. A
leak test that cannot fail is worth nothing, and this one was confirmed to fail for the right
reason before being trusted.

The real success criterion was reading `--viewpoint salvatore` and asking whether a person could
form a plausible account without being handed the true one. On `disloyal-vincent` at seed 42 he
ends up holding that Vincent went outside the rule — marked **contradicted**, with his own eyes and
Vincent's denial listed side by side — while never learning it with certainty. On `baseline` he
never learns of the breach at all: Vincent's partial report names Tommy as the man who used force
and quietly omits his own order.

## Important discoveries

- **A candidate that can never win is worse than no candidate.** `ReportCandor.False` was
  initially unreachable in every configuration because of a flat `0.9` risk penalty on lying.
  Re-derived so the risk is carried almost entirely by *believed witnesses*: a denial that nobody
  can contradict is genuinely better than an omission, because an omission leaves the fact
  retrievable. The man who thinks the street saw him now omits; the man who thinks nobody saw him
  denies. This was a modelling error surfaced only by trying to reach the behaviour, not by review.
- **The person who orders a breach must know he ordered it.** `ResolveViolence` created a
  `PersonBreachedPolicy` claim naming the strategy's owner but only offered it to the *boss* as a
  discovery. Vincent therefore held no claim naming himself, could only ever report candidly, and
  the delegate who carried out the violence absorbed all the exposure. Fixed by having the owner
  learn his own breach directly.
- **One event must mean one chance to notice per person.** Adding proximity-based observation gave
  the boss two independent rolls on the same event (one as boss owed an account of a breach, one as
  a man working that district). He noticed twice and deliberated twice at the same instant on the
  same news. Fixed by collecting observers into a map before scheduling, best access winning.
- **`acquired_at` and `last reconsidered_at` are different fields, and conflating them silently
  rewrites the player's timeline.** Corroboration was updating `AcquiredAt`, so something learned
  in March displayed as learned in May purely because somebody mentioned it again. The doc's
  Character Information Record lists both; now so does `InformationRecord`.
- **Report exchanges need a termination condition or they run until the calendar does.** Two
  separate loops appeared: a boss re-asking the same man forever (fixed — you cannot seek
  corroboration from someone who has already given you their account), and a subordinate
  volunteering the same report every few days (fixed — reporting requires having learned something
  since you last spoke). Both are instances of the same omission: nothing in the model said an
  exchange had been *spent*.
- Milestone 001's delegation makes the scenario richer than the doc's worked example assumed:
  Vincent delegates to Tommy, so the man who ordered the breach and the man who committed it are
  different people with different things to hide, and they conceal *different* claims from the same
  boss. This was emergent, not designed.

## Deferred work

- **Generalized rumor propagation** — explicitly out of scope, and still out. `SourceKind.Rumor`
  remains in the vocabulary but nothing now produces it.
- **A conflict of the form "one source asserts what another conspicuously omits" is not detected.**
  `IsContested` requires an actual denial. In the baseline both Vincent and Tommy omit rather than
  deny, so their accounts differ without formally conflicting. Whether that shape deserves
  first-class treatment is a real design question, not an oversight.
- Attribution on a corroborated belief still credits only the first source; the full picture lives
  in testimony. Fine for now, but a `SourceChain` is the eventual answer the doc gestures at.
- Media/public coverage, the case-board investigation model, tier transitions, and relationship
  schema (`OPEN_CONCERNS.md` #3, still open and still blocking "richer relationships and
  grievances").
- Carried over from milestone 002 and still untouched: the redundant `TargetFramework` override in
  `CrimeEmpire.Simulation.Tests.csproj`, and unverified Godot/`net10.0` compatibility.
- `OPEN_CONCERNS.md` #4 (trait vocabulary not closed) was already stale before this milestone —
  milestone 001 closed it in `Domain/Psychology.cs`. Not corrected here to keep this commit to one
  concern; worth a separate docs pass.

## Relevant commits

- The milestone is the single commit that introduced this file.
  `git log --diff-filter=A -- docs/milestones/003-information-transmission.md` resolves it.
