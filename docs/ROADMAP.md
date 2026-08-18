# Crime Empire — Roadmap

What is not built, what is known to be wrong or unfinished, and what could plausibly come next.

**Nothing here authorizes anything.** This file is read when scope is being selected or proposed,
not before ordinary work. The assigned scope lives in `CURRENT_MILESTONE.md` and nowhere else;
neither the debt list nor the candidate list below is a licence to begin. Settled decisions are in
`DESIGN_DECISIONS.md`, unresolved design risks in `OPEN_CONCERNS.md`, and review status in
`REVIEW_LEDGER.md`.

## Where the project stands

`SIMULATION_ARCHITECTURE.md`'s validation sequence is: simulation kernel → emergence prototype →
MVP vertical slice.

Milestone 001 proved the kernel. Milestone 003 built the first narrow information slice of the
emergence prototype, 004 made its provenance precise, and 006 — **closed and accepted 2026-08-15** —
gave a perceived account conflict a social consequence, which is the loop's return edge. Milestone
007 — **closed and accepted 2026-08-16** — made that edge reach a later decision in the accepted
scenario. Milestone 008 — **closed and accepted 2026-08-16** — settled the reader side of the
relationship schema and built the instrument that measures it. The rest of the emergence prototype is
not built, and the MVP has not begun. Milestone 002 was a framework migration, not a step along this
sequence. Full accounts are in `docs/milestones/`.

Milestone 011 — **implemented 2026-08-18, not reviewed and not accepted** — gave the detective a move
after she names a suspect. She had none: a candidate set of one option, with nothing generated and
nothing rejected, because every route to a question was structurally closed to an actor whose beliefs
are self-acquired by construction and who belongs to no institution. See
`milestones/011-the-detective-has-no-next-move.md`.

Milestone 010 — **accepted 2026-08-18 on `824f3fc`** — made concealment act on
the concealer's own belief and scoped the denial's exposure term to its own incident. Both defects
were real and both are fixed; **the denial still loses**, and the milestone's deliverable is therefore
the measured explanation of what else holds it shut rather than the exchange it went looking for. See
`milestones/010-a-denial-that-can-win.md`. Codex is withdrawn from the review loop from this milestone
onward, so its findings are the author's own and are recorded as weaker evidence — see
`REVIEW_LEDGER.md`.

Milestone 009 — **closed and accepted 2026-08-16** after five Codex rounds and one self-review,
nine findings in all — is the first step off that sequence rather than along it: a Godot
playable shell over the same kernel, with a person answering one character's decisions.
**All five variants are byte-identical to milestone 008's accepted baseline**; the second correction
moved `cautious-vincent` and the fourth moved it back, having found the cause a layer deeper. One
viewpoint render differs from 008 — Marco's, which gains a line. Current figures are in
`REVIEW_LEDGER.md`; the account is in `milestones/009-godot-playable-shell.md`, Corrections 1–4.

006 established where the difficulty sat: the mechanisms worked and the scenario could not show them.
007 was the scenario-reach answer to that. 008 then answered 007's own finding, and the answer
reframed it: the trust-to-partial-report path really is worth about four hundredths of a point, but
that is the weakest path in the channel rather than the channel. Removing relationship state changes
which candidate wins at 1–3 decisions in every variant. The constraint has moved again — from "is it
worth anything once it is shown" to **which readers are worth strengthening, and on what evidence**.

## Known technical debt

- ~~**RNG keying.**~~ and ~~**`ConcealIncident` runaway.**~~ **Retired 2026-08-14, resolved by
  milestone 005.** Occasion keys are now built from causally local strategy-instance identity —
  `(owner, local sequence, advance ordinal, trace kind, observer)` — never from `ScheduledEvent.Id`,
  `WorldEvent.Id`, or a `Claim.EventId`, so an unrelated scheduling change can no longer re-roll
  anyone's perception. `ConcealIncident` has an explicit, tested termination rule enforced in
  `Filters`, corrected during review to key off the incident itself rather than the target so a
  genuinely different incident at the same location stays eligible. Full account, including the
  correction, in `milestones/005-stable-occasion-identity-and-strategy-lifecycle-safety.md`.
  **Separately, and not retired by the above:** the one-attempt concealment rule itself remains only
  an MVP placeholder, not a permanent design — see that archive's deferred work and
  `CURRENT_MILESTONE.md`'s carried-forward items. Retiring the keying defect and the runaway does not
  retire that provisional concern.
- **Tuning guesses.** The `FirstHandTestimony` suspicion discount of `0.15` and the `Discovery`
  discount of `0.10` are not derived figures, and nothing yet distinguishes them behaviourally from
  neighbouring values. `Relations.ConflictTrustCost` of `0.35` now has one measured consequence and it
  is a small one — see the item below.
- ~~**The scenario is the binding constraint.**~~ **Addressed by milestone 007, closed and accepted
  2026-08-16.** A second contested business keeps the organisational shortfall
  alive past the first collection, which produces a second assignment briefing; that briefing
  contradicts the capo, and the trust it costs is read by a decision he takes afterwards. The
  delegator's question, competitive since 006 and never chosen, now wins in play in four of five
  variants. See `milestones/007-scenario-reach.md`.
- ~~**The trust edge reaches a score and barely moves it.**~~ **Answered by milestone 008, and the
  question was wrong.** The figures stand — two conflicts take Vincent's trust from 0.45 to 0.031 and
  the net relationship contribution on his next partial report to that boss moves from 0.0440 to
  0.0063 — but they measure the weakest path in the channel, not the channel. Removing relationship
  state changes which candidate wins at **1–3 decisions in every variant**. The attenuation on that
  particular candidate is structural: `+0.7 × loyalty` for the standing a report buys and
  `−0.5 × loyalty` for what an omission costs net to `0.2 × loyalty`. And grievance was being clamped
  away, not merely outweighing trust. Both are now visible and separately measured. See
  `milestones/008-relationship-readers.md` and `docs/RELATIONSHIPS.md`.
- ~~**Concealment does not quiet the witnesses it is named for.**~~ and
  ~~**`believedWitnesses` is scanned globally.**~~ **Both fixed by milestone 010.** The concealment
  instance now names its incident, the first step revises the concealer's own confidence that the
  street can place him there — up on a clumsy job, down on a clean one, and the world untouched
  either way — and the denial's exposure term is scoped to the incident the suppressed claim belongs
  to. **Neither fix made the denial win, which is milestone 010's result rather than a failure of
  it**; the margins and the three reasons the model is still shut are below and in
  `milestones/010-a-denial-that-can-win.md`.
- ~~**A man's own conclusions can only firm up, except through one method.**~~ **Both halves fixed by
  milestone 011.** `AdvanceInvestigation`'s cold-trail branch now goes through `Cognition.Revise`, and
  the whole investigation path is scoped to its incident rather than its address. Fixing the first
  required correcting `Revise` itself: it admitted `Inference` alone, which put `Discovery` in with
  Participant and Witness — the bundle `Provenance.cs` exists to prevent — so the repair was still a
  no-op until `Provenance.IsOwnReading` replaced the guard. **The branch remains unreachable in every
  variant at seed 42**, so a staged test is the only thing standing behind it.
- **The developer trace calls everybody "he"**, 59 strings across `Utility` and `TraceWriter`.
  Milestone 011 fixed the player-facing surfaces and deliberately left this one: it is a debugging
  tool the architecture doc separates from player-facing accounts by name, and changing it would move
  the trace hashes for no player-visible gain.
- **`AdvanceInvestigation` reads and writes `owner` throughout.** A delegated investigation would put
  its findings in the head of a man who was not there — the asymmetry milestone 010 resolved for
  concealment by moving the belief to the executor. Investigation is never delegated in the fixture,
  so nothing exercises it.
- **Nobody in the fixture holds a scored relationship with Det. Kane**, so the player-facing attitude
  list can never describe a woman in a natural run. Her one relationship is the all-zero record
  `Relations.Meet` writes when she questions Tommy, and the list filters those out.
- **One cleanup is worth `-0.2` and the denial needs about `-0.4`.** Measured by milestone 010. The
  MVP one-attempt-per-incident rule caps the mechanism at a single step, and the counterfactual shows
  the denial winning outright once the concealer's witness belief reaches zero. Bound up with the
  provisional status of the one-attempt rule noted above.
- **Tommy cannot roll a clean cleanup at any seed, and Vincent is never offered one.** The roll is
  `discretion + Range(-0.15, 0.15) > 0.45` over a half-open range, and Tommy's Discretion is `0.30` —
  his largest draw lands exactly on the threshold against a strict comparison. Vincent's Discretion of
  `0.35` clears it sometimes, but `Generators.FromPressure` offers concealment only when
  `LegalExposure` is the actor's *dominant* pressure and Vincent's is always `RevenueShortfall`. So
  the man who can clean up is not offered a cleanup and the man offered one cannot clean up. Both are
  cast and threshold facts; milestone 010's ruling 3 forbade touching either, so they are recorded.
  It changes nothing in the current scenario, which is why milestone 007 excluded it rather than
  folding a behaviour-neutral fix into a pass that already moved every baseline.
- **The bakery is never collected from.** Nobody in the organisation knows it is refusing — deliberate,
  and the asymmetry that leaves the capo room to think rather than a second errand — but it means a
  second collection cycle is present in the fixture and unexercised.
- ~~**A delegator never receives an account from his own executor.**~~ **Corrected and partly
  addressed 2026-08-14.** The original claim was wrong: Tommy volunteers three Partial reports to
  Vincent. What never happens is a *contradiction*, because withholding asserts nothing. The
  redirect-to-the-asker behaviour is real but applies only to answers, not to volunteered reports.
  Milestone 006's correction added `Generators.FromDelegation`, so a delegator can now put a question
  to the man he sent, and the end-to-end path from that question to a trust consequence is proven.
- ~~**The delegator's question never wins in the accepted scenario.**~~ and
  ~~**Self-protection is re-priced for a concealment already decided.**~~ **Retired by milestone 007,
  closed and accepted 2026-08-16.** Concealment is now worth only the
  protection a report newly buys, priced per `(sender, recipient, claim)` from message content, and
  the question wins in play in baseline, watchful-boss, disloyal-vincent and resentful-tommy —
  `cautious-vincent` has no delegation, so there is nobody to ask about. **Not retired by that:** the
  executor still answers rather than denies, for the concealment-step reason above.
- ~~**`resentful-tommy` still makes the same decisions as baseline.**~~ **Retired by milestone 008.**
  `--compare` now reports **5 distinct traces · 5 distinct chosen-action sequences**. At seed 42 Tommy
  conceals the incident himself on 9 April instead of reporting it to Vincent, because his grievance
  against Vincent is no longer clamped away and takes 0.21 out of what reporting to that particular
  man is worth. Nothing was tuned. **Recorded with its fragility:** the winning margin is 0.0279
  against ±0.05 per-candidate noise, and the divergence holds at seeds 42 and 31337 but not at 1, 7,
  99 or 2024.
- **Trust cannot go negative, and nothing raises it.** Absence of trust and distrust are the same
  state; separately, the only runtime path that moves trust is a perceived account conflict, which
  only lowers it, so a relationship can be damaged and never repaired. Both **deferred rather than
  retired** by milestone 008 — negative trust returns when a decision reads distrust differently from
  indifference. See `docs/RELATIONSHIPS.md`.
- **`GrievanceWeight` is unbounded**, and a cap was considered as milestone 008's remedy for grievance
  dominating loyalty and explicitly rejected in favour of unbundling the clamp. Open, not answered.
- **Obligation is read but never moves.** `Relations.Establish` is its only writer and that is
  scenario construction; it holds its seeded value for the whole of every run. Surfaced by writing
  `docs/RELATIONSHIPS.md`, not a defect introduced by it.
- ~~The test project redundantly declares `TargetFramework` despite the centralized build property in
  `Directory.Build.props`.~~ **Retired 2026-08-16 by milestone 009**, and not by deleting the line.
  `Directory.Build.props` no longer assigns a TFM at all — it publishes `CrimeEmpireHostTfm` and
  `CrimeEmpireEngineTfm` as named values, because assigning `TargetFramework` there and multi-targeting
  the simulation library are mutually exclusive. Every project now names the framework it wants, so
  the test project's declaration stopped being redundant rather than being tidied away. Carried since
  milestone 002.
- **The cast is six, and six is a ceiling rather than a trend.** `nunzio` was added by milestone 007
  against that milestone's own written "no new characters" exclusion, because `AdvanceTribute`
  resolves a demand through the owner's own decision and `Commit` finds a business by owner, so two
  shops need two owners. Codex found the breach and Matt accepted it as a bounded scenario-fixture
  exception on 2026-08-16, stating that it authorizes neither broader cast growth nor relaxed scope
  discipline. A seventh character needs its own ruling first.
- ~~**Candidate descriptions are developer-shaped, and the player now reads them.**~~ **Retired
  2026-08-16 by milestone 009's first correction, and the deferral was wrong.** The reasoning had been
  that a player-facing description vocabulary would be "a second implementation to drift against the
  first" — the wrong comparison, since the developer description and the player description are not
  two implementations of one thing but two different things. `PlayerOption` now builds the wording
  from the candidate's typed fields, so no generator string and no truth-log `EventId` reaches the
  player, and the interface reads properly besides.
- **The timing of a pause is itself observable, and the occasion fix does not close it.** The
  controlled character is woken when a delegated operation blocks or completes, so a player sees him
  stop on the day it happened even though the interface says nothing about why. Closing it would mean
  not waking him, which changes autonomous behaviour. Surfaced by milestone 009's first correction.
- ~~**`Generators.FromRelationship` draws a corroboration target from the whole organisation's
  membership.**~~ **Retired 2026-08-16 by milestone 009's THIRD correction.** Two things are worth
  keeping about how it got there.

  The original deferral said "inert in a three-member outfit"; that was never measured and was false.
  In `cautious-vincent` there is no violence, so nothing ever names Tommy to Salvatore, and Salvatore
  was going to him for an account anyway. The second correction fixed that much — `cautious-vincent`'s
  baseline moved, and the new figures are in `REVIEW_LEDGER.md` — **but it was rejected and did not
  retire this item**, because it widened the set back by `Pipeline.SuperiorOf` and
  `Pipeline.SubordinatesOf`, which are authority scans over the same roster.

  The implemented rule is the third correction's: `Acquaintance.KnownTo` is the single derivation,
  read by both `GeneratorContext.AcquaintedIds` and `PlayerView.KnownPeople`, and it is the
  character's own cognition and social state widened **only** by the holders of his organisation's
  `Organization.Offices` and `BossId`. No roster and no authority scan reaches a target.
- **Whether an outfit whose boss cannot name his own soldiers is the right model.** The line is drawn
  explicitly by milestone 009's third and fourth corrections: a named post is knowledge —
  `Organization.Offices` and `BossId` — an encounter is knowledge, and a headcount is not. The model has held that position since milestone 003, when
  `IntelligenceWriter` recorded that "who else is in this outfit" is the kind of thing a boss can be
  wrong about, so following it was the conservative choice rather than a new decision. The consequence
  is visible: a soldier holding no office is unreachable for corroboration until somebody names him,
  which in `cautious-vincent` never happens for Tommy. Whether that is *correct* is unanswered.
- **The player cannot see why an option is unavailable.** Rejected candidates are hidden per milestone
  009's scope, which is right for utility scores and arguably wrong for "he does not know that the
  bakery is holding out" — the single most legible line in a decision trace, and the one that proves
  the simulation is belief-limited rather than merely claiming to be. Whether a *belief-stage*
  rejection is player-facing is a real design question and is unanswered.
- **`AGENTS.md` §Verification does not mention the Godot headless check**, and `docs/RELATIONSHIPS.md`
  is still absent from its conditional-reading list. Same shape as the older item: flagged, not taken,
  because no ruling authorized editing `AGENTS.md`. The commands milestone 009 added to the
  verification set are recorded in `REVIEW_LEDGER.md`'s baseline section instead.
- **Nothing prevents a future Godot script from calling `Cast.Build` and `Runner.Run` directly.** The
  session's own surface is closed — `World` is `internal` and a reflection test pins the public
  surface — but the library it lives in is not. A separate player-contract assembly would close it;
  milestone 009 did not have the scope for a fifth project.
- **The milestone lifecycle does not durably record rulings.** Written into `CURRENT_MILESTONE.md`
  before implementation and reset out of it by the archive-and-close commit, they survive only in the
  archive that reproduces them. Milestone 006 lost its set this way and milestone 007 nearly repeated
  it. Fixing it means changing `AGENTS.md`, which is Matt's call and has not been made.

## Not yet implemented

- **Persistence.** SQLite is selected (`DESIGN_DECISIONS.md` §Stack) but not implemented. Save/load
  is absent.
- **Relevance tiering.** Active / Supporting / Background promotion and demotion are designed in
  `SIMULATION_ARCHITECTURE.md` and not implemented. The six-character cast makes this a non-issue
  at present scale, which also means it is unvalidated.
- ~~**Godot.**~~ **Retired 2026-08-16 by milestone 009, and the fallback is what happened.** Godot
  4.7.1 hosts .NET 8, not .NET 10, so the simulation library multi-targets `net8.0;net10.0` and the
  Godot project is on `net8.0` while the runner and tests stay on `net10.0`. The library gained no
  Godot reference. `src/CrimeEmpire.Godot` builds as part of `CrimeEmpire.sln` and starts headlessly.
  **Not retired by that:** everything in the presentation list below is still absent — no map, no art
  pipeline, no animation, no save/load, and the interface is a deliberately plain functional layout.
- **Generalized rumor propagation.** Explicitly excluded from milestone 003 and still out.
  `SourceKind.Rumor` remains in the vocabulary; no path produces it.
- Media and public-information channels, the case-board investigation model, prosecution, broader
  organizations, diplomacy, careers, corruption, and surveillance.
- Attribution on a corroborated belief credits only the first source; the full picture lives in
  testimony. A `SourceChain` is the eventual answer `INFORMATION_AND_LEGIBILITY.md` gestures at.

## Candidate scopes

Candidates only. They are not ordered by priority and must not be read as a queue — confirm scope
with Matt and write it into `CURRENT_MILESTONE.md` before changing simulation behaviour.

1. ~~**Relationship design pass**~~ — **became milestone 008**, which narrowed `OPEN_CONCERNS.md` #3
   rather than retiring it. What is left of it is decay, negative trust, whether respect and
   resentment are separate dimensions or derived, whether provenance should weight the social
   consequence, and whether grievance should be capped — each now carrying a stated condition for
   when it becomes answerable, which is what makes them not-yet-candidates rather than open questions.
2. ~~**A denial that can win**~~ — **became milestone 010**, implemented 2026-08-17 and awaiting
   review. Both defects are fixed and **the denial still loses**, narrowest margin 1.083. What the
   milestone delivered instead is the explanation: the mechanism is directionally right and one
   attempt is not enough of it, and neither man who could use it is in a position to. Anything
   further here is a tuning or cast question and was explicitly out of bounds under its rulings 3 and
   7 — so if it returns, it returns as a scope Matt writes, not as unfinished business. See
   `milestones/010-a-denial-that-can-win.md`.
3. **Persistence / SQLite** — begin storing the information and decision data now worth querying.
4. ~~**Godot / .NET compatibility spike**~~ — **subsumed by milestone 009**, which settled the
   constraint (the engine hosts .NET 8) and built the shell in the same pass rather than spiking it
   separately.
5c. **Instruments, not vigilance** — **planned as milestone 012 on 2026-08-18, not authorized to
   begin**, after Matt established that Codex is unavailable for review or advice for the foreseeable
   future. What Codex supplied was different priors, not diligence, and the only substitutes available
   are adversaries with no priors at all. `coverlet.collector` is already a package reference and
   produces a report the first time it is asked: **92.2% line, 84.2% branch, 376 uncovered lines**,
   never run before. Sampling separates legitimately-uncovered surface (`Program.cs` is 118 of the
   376) from **live edges nothing has ever run** — the grievance raised on observing a policy breach,
   and the price of a candid report made with something at stake. **Before milestone 011,
   `AdvanceInvestigation`'s cold-trail branch was in that category**, and it was found by accident ten
   milestones late. Adds no behaviour, so no baseline may move. Scope and rulings in
   `CURRENT_MILESTONE.md`.

5b. **Scenario reach II** — **planned on 2026-08-18 and deferred to milestone 013** when the
   instruments milestone was inserted ahead of it. Displaced rather than dropped: it moves every
   baseline again, and doing that on a third consecutive unreviewed milestone is the risk 012 exists
   to reduce first. The full plan is at `40f0ded`; the measured evidence and the two load-bearing
   rulings are preserved here.

   **Ruling A — he infers a gap, not an answer.** The inference may read organisational conditions and
   his own cognition, and nothing else. He must end up suspecting that *something* is refusing without
   being told *what*, because a boss who infers the exact shop from his own books has been handed the
   fixture rather than inferred anything.

   **Ruling B — do not over-claim what a second incident buys.** Two incidents at *different shops*
   leave location and incident correlated one-to-one, so milestone 011's lead-pickup and completion
   rules stay staged. What a second incident exercises naturally is milestone 010's witness scoping —
   one man holding two `WitnessSawIncident` beliefs. State both halves.

   The evidence, at seed 42, every variant, day 90: `OrgCondition.RevenueLoss` sits at **0.90** from
   early on and never falls; the boss issues **three assignments**, all "restore the harbour tribute",
   all aimed at the one shop his capo has personally watched start paying; his belief that it refuses
   survives at 0.75, eroded to 0.47 in `watchful-boss` and 0.37 in `cautious-vincent` where Vincent
   contradicts him, but never abandoned; and **nobody in any variant ever learns the bakery is
   refusing**. The organisation cannot notice a shortfall it cannot attribute. The opening asymmetry
   `Cast.cs` argues for is preserved — what is added is a way *out* of it, late and by his own
   reasoning.

   **Why it is a candidate at all**: milestones 010 and 011 produced five findings of the form *the
   fixture cannot exercise this* — two incidents at one shop are only ever staged, the cold-trail
   branch is unreachable at every seed, nobody holds a scored relationship with Kane, Tommy can never
   roll a clean cleanup, Vincent is never offered one. That is milestone 006's finding returning, and
   007 is the precedent for answering it.

5. ~~**Another bounded emergence slice**~~ — **taken up as milestone 011 in one specific form**, chosen
   by Matt on 2026-08-18: the detective's side. Not speculative — looking at `InvestigateIncident`
   after milestone 010 stumbled into two defects in it found something larger. **Det. Iris Kane opens a
   case, names a suspect, and then has a candidate set of exactly one option for the rest of the run**,
   with no rejections at all: nothing is outscored, nothing is generated. Every route to a question is
   structurally closed to her — corroboration requires a belief acquired through *testimony* and a
   detective's beliefs are self-acquired by construction; delegation requires people she has sent;
   reporting requires a superior and she belongs to no institution. Scope and rulings in
   `CURRENT_MILESTONE.md`. What remains of this candidate afterwards is rival activity and tier
   transitions, which are still unbuilt.
6. **A runtime path that raises trust.** Surfaced by milestone 008 writing the schema down: conflicts
   lower trust and nothing restores it, so a relationship can be damaged and never repaired. Nobody
   decided that. Small, and it would give the trust dimension a second update path to be read
   against.

Provenance precision was a candidate and became milestone 004, which is closed. RNG keying and the
concealment runaway were a candidate and became milestone 005, which is closed. The relationship
design pass was candidate 1 and became milestone 006 in its executable form; the schema document it
was originally framed as became **milestone 008**, written from measured results rather than ahead of
them. Scenario reach was candidate 1 and became milestone 007. "A scenario variant that contradicts a delegator's first-hand account" was
candidate 5, was attempted inside milestone 006 and did not succeed, and is now **half** achieved:
milestone 007 makes the delegator ask and the executor answer, in play, but the executor answers
honestly. What remains of it is candidate 2 above.
