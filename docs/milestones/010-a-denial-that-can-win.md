# Milestone 010 — A Denial That Can Win

**Authorized by Matt on 2026-08-16**, from `ROADMAP.md`'s candidate 2. Implemented 2026-08-17.
Append-only: corrections go at the bottom, never into the account above.

Codex was withdrawn from the review loop for this milestone. Claude implemented and reviewed its own
work. See "Self-review" below for what that found and `REVIEW_LEDGER.md` §"From milestone 010 onward,
review is self-assessment" for why the result is recorded as weaker evidence than a Codex round.

## What this milestone was for

The model has had, since milestone 004, a precise distinction between what a man privately holds and
what he claims — built specifically so a subordinate could lie to the person who sent him. **No
accepted run had ever produced that exchange.** Tommy answered his delegator honestly every time, in
every variant, at every seed tried.

Two structural defects were identified as the reason, and both were real.

### Defect 1 — quieting the witnesses quieted nothing

`Strategies.AdvanceConceal` ran two steps, `"quiet the witnesses"` and `"tidy the paperwork"`, and did
the same thing for both: a discretion roll, and `LegalExposure` up or down by a tenth. The concealer's
own belief that he was seen was untouched. `Strategies.ResolveViolence` gives the executor
`WitnessSawIncident` at 0.6 as an `Inference`, and nothing in the game could ever move it.

### Defect 2 — the scan was global

`Utility` priced a denial almost entirely on that belief: a maximum over **every**
`WitnessSawIncident` he held, whatever incident it belonged to. A man concealing one thing was priced
on the most-witnessed thing he knew about. Same defect shape as the `SeekCorroboration` scan `404b416`
fixed, and on `REVIEW_LEDGER.md`'s load-bearing list under the same heading.

## Scope, as authorized

**In:** four numbered work items — carry the concealed incident on the strategy instance; make "quiet
the witnesses" act on the concealer's belief about *that* incident as a function of the discretion
roll already there; scope `believedWitnesses` to the incident the suppressed claim belongs to, by
`Claim.EventId`; measure whether a denial now wins and report the answer whichever way it falls.

**Out:** no new claim kinds, no new characters, no new scenario variants, no persistence, no tiering,
no relationship-schema work, no Godot or interface change beyond what the player boundary needs to
keep its guarantees.

## The eight rulings, preserved

**1 — The incident is the identity, not the target.** Milestone 005 settled this for concealment
redundancy after Codex found `(Kind, TargetId)` treating two beatings at one shop as one thing. The
same rule applies here: a step acts on the incident the instance is concealing, never on "whatever
happened at this address".

**2 — Quieting witnesses changes a belief, not the world.** It must not delete a `Trace`, alter the
truth log, or touch anybody else's cognition. What moves is the concealer's own confidence that he can
be placed at that incident. **Being wrong must stay possible in both directions** — a man who believes
he has cleaned up and has not is the interesting case, and `ResolveViolence` deliberately records his
witness belief as `Inference` rather than something he established, precisely so it can be wrong.

**3 — No coefficient is tuned to make the denial win.** Not `0.25`, not `3.0`, not the erosion rates,
not the discretion threshold. The two defects are structural, and fixing them either lets a denial
compete on the numbers already there or it does not. **If it still loses, that is the milestone's
result** — measured, with the margin stated, recorded as a finding rather than chased.

**4 — Deception stays a candidate, scored through the ordinary pipeline.** No code branches on a trait
to produce a lie, no scripted denial, no special case for the executor. `DESIGN_DECISIONS.md`
§"Information channel" settles this and nothing here reopens it.

**5 — Actor parity.** Whatever becomes available to Tommy is available to a player controlling him,
through the same candidate set. And the player boundary keeps its guarantees: a delegator must not be
shown the concealer's private belief about who saw him, and the pending decision must not start
carrying a fact its holder lacks.

**6 — Baselines will move, and that is expected.** A change to what concealment does will change
histories. Every moved figure goes into `REVIEW_LEDGER.md` with its reason. Milestone 009 ended
byte-identical to 008; this one will not, and that is not a defect.

**7 — The bakery stays uncollected and the cast stays at six.** Both are carried-forward items with
rulings behind them. Nothing here is a licence to touch either.

**8 — Self-review is the review, and it is recorded as weaker than what it replaces.** The method is
the one that actually found defects rather than the one that felt thorough: enumerate the real surface
empirically and diff it; mutation-check every fix by reverting it and watching a named test fail; test
for the *kind* of defect rather than the reported instance; walk `REVIEW_LEDGER.md`'s recurring-failure
list as an explicit checklist before declaring done. **A self-review that returns no findings is weak
evidence and will be recorded as such**, not as a clean bill.

## What was completed

All four work items. `Domain/Cognition.cs`, `Domain/ExecutionState.cs`, `Decision/Commit.cs`,
`Decision/Utility.cs`, `Strategy/Strategies.cs`; tests in a new
`tests/CrimeEmpire.Simulation.Tests/ExposureAndDenialTests.cs` and one line added to
`SimulationReplayTests.cs`.

### 1. The instance carries the incident

`StrategyInstance.SourceEventId` was declared in milestone 001 and set by nothing at all until now.
`Commit` sets it from `Candidate.AboutIncident`'s event id — the same claim it already recorded in
`AttemptedConcealments` and then dropped, so a concealment knew which incident it must not repeat and
not which incident it was concealing.

A claim with **no** event id yields `null` rather than `0`. Treating the default as a key would put
every unidentified claim into one shared incident, which is defect 2 one level down.

### 2. Quieting the witnesses moves the concealer's belief

Only the first step. `"tidy the paperwork"` is about records and keeps the exposure effect alone.

- **Whose belief:** the executor's. He went out and did it and came away with a view; a delegator who
  sent him learns nothing here, the same rule `Strategies` already states at the approach step and at
  the beating itself. The `LegalExposure` beside it still goes to the owner — pressure is
  motivational state applied to whoever carries the consequence, a belief is cognition and reaches
  nobody who was not there.
- **Which belief:** every `WitnessSawIncident` record carrying the instance's event id, and nothing
  else. An instance naming no incident quiets nothing rather than falling back on the target.
- **By how much:** `−0.2` clean, `+0.1` clumsy — the two magnitudes the step already applies to
  `LegalExposure` on the line above, in the same directions. Deliberately not new numbers, and
  deliberately not a gentler failure: the step has always modelled a clumsy cleanup as actively worse
  rather than merely ineffective (exposure rises; completion says "the cleanup made things worse"), so
  the belief follows the model already there. Choosing the milder no-op would have been picking a
  coefficient by what it does to a downstream decision, which ruling 3 forbids.

**A gap in `Cognition` had to be closed first, and it is the most interesting thing in the milestone.**
`Learn`'s override rule is `OverridesPriorRecord() || confidence >= prior.Confidence`, so an
`Inference` arriving *less* confident than what is already there is silently discarded. **A character's
own conclusions could only ever firm up.** `Learn` models acquiring information and `Receive` models
being told something; nothing modelled a man thinking better of something he worked out himself.

`Cognition.Revise` is that, and only that. It refuses a record that is not the holder's own
`Inference` — what he saw is what `Provenance.OverridesPriorRecord` and `ProtectsStance` exist to
defend, and what he was told is an account he would have to be argued out of. It moves confidence in
either direction, clamps to `[0,1]`, leaves the stance and the acquisition time alone, and moves the
reconsideration stamp — which is load-bearing, because `Reporting.NeedsConveying` reads it, so a man
who has changed his mind has something to say again.

### 3. The denial is priced from its own incident

`believedWitnesses` is now a maximum over the witness beliefs whose `Claim.EventId` matches a
suppressed claim's. A suppressed claim with no event id is skipped rather than matched against every
other idless claim.

In this fixture nobody ever holds two witness beliefs at once, so **this fix changed no decision in
any variant.** It is a correctness fix whose effect is latent here, which is exactly why it is pinned
by a staged test rather than by a baseline.

## The answer to the question the milestone asked

> Once a man can act on his own exposure, does lying to the person who sent him become a thing he
> would choose?

**No. The denial still loses everywhere, and the margins are stated below.** Per ruling 3 this is the
result, not a failure to be chased — and the model owes an explanation of what else is holding it
shut. There are three things, and they compound.

### Finding 1 — the mechanism works and is directionally right, but one attempt is not enough of it

Re-scoring every decision that offered a denial through the production scorer, with the actor's witness
belief held at counterfactual values and everything else untouched (seed 42, all five variants;
47 decision-offerings across the four variants where violence occurs):

| witness belief | narrowest losing margin | decisions the denial wins | of those, beyond ±0.05 noise |
|---|---|---|---|
| as it actually stands (0.6) | **+1.083** | 0 | 0 |
| 0.4 — one clean cleanup step | +0.411 | 0 | 0 |
| 0.2 | −0.059 | 5 | 5 |
| 0.0 — a perfect cleanup | −0.529 | 17 | 13 |

The narrowest case at every level is Vincent in `disloyal-vincent`, against "report to salvatore,
leaving out his own part".

So one clean step closes about 62% of the narrowest gap and flips nothing. Two would flip five
decisions by a margin barely beyond the per-candidate noise. **The MVP rule permits exactly one
concealment attempt per incident, one step of which quiets witnesses**, so the mechanism's ceiling in
the current model is a single `−0.2`. The exposure term is doing nearly all the work — at a witness
belief of zero the denial wins outright, so nothing structural beyond exposure is holding it shut.
What is holding it shut is that there is not enough cleanup available.

### Finding 2 — the one man positioned to lie cannot clean up, by arithmetic

The roll is `discretion + rng.Range(-0.15, 0.15) > 0.45` over a half-open range. Tommy's Discretion is
`0.30`. **His largest possible draw leaves him exactly at the threshold, and the comparison is
strict — so Tommy is clean at no seed whatsoever.** Every cleanup in every variant is "clumsily", and
that is not luck.

The consequence is that this milestone's mechanism, applied to Tommy, moves his belief the *wrong* way:
0.6 → 0.7, and the denial gets harder for him rather than easier. Ruling 3 forbids touching either the
`0.30` or the `0.45`, so this is reported rather than fixed.

### Finding 3 — and the man who can clean up is never offered a cleanup

`Generators.FromPressure` proposes `ConcealIncident` only when `LegalExposure` is the actor's
**dominant** pressure. At the end of a ninety-day run, in every variant:

| | dominant pressure | LegalExposure | Discretion | can roll clean | ever concealed |
|---|---|---|---|---|---|
| Tommy | `LegalExposure` 0.72 | 0.72 | 0.30 | **never** | yes |
| Vincent | `RevenueShortfall` 0.40 | 0.12 | 0.35 | sometimes | **never** |
| Salvatore | none | 0.00 | 0.65 | always | never |

Vincent — the man whose denial comes closest to winning, because his grievance against Salvatore makes
lying to him cheap — has his legal exposure permanently dominated by the revenue shortfall the whole
scenario is about. So he is never offered the cleanup he could actually perform. Salvatore could clean
up in his sleep and has nothing to clean up.

**The man who can clean up is not offered a cleanup, and the man offered one cannot clean up.** That is
the shape of the answer, and neither half can be moved without tuning a coefficient (ruling 3) or
changing the cast (ruling 7).

## What did move

Behaviour changed in four of five variants, as ruling 6 said it would, and for one reason:
`Cognition.Revise` moves the reconsideration stamp, `Reporting.NeedsConveying` reads it, so a concealer
whose view of his own exposure has moved has something to report again.

In `baseline` that produces a new exchange on 17 April: Tommy, having made a mess of the cleanup and
come away *more* sure the street can place him, reports to Vincent — and does it **candidly**, passing
on all three claims, where every earlier account of his had the worst of it left out. The denial scored
`−3.23` against the candid report's `+1.04` at that decision, its widest loss in the run. A man made
more frightened by his own cleanup tells the truth. Nothing was written to produce that; it falls out of
the belief moving and the report re-arming.

`cautious-vincent` is byte-identical, because it contains no violence, hence no concealment.

Every figure in `REVIEW_LEDGER.md`'s milestone 010 baseline section, with its reason.

## Tests and results

- **406 passed, 0 failed** (380 before the milestone). All 26 added are in the new
  `ExposureAndDenialTests.cs` — 15 facts and 11 theory cases; `SimulationReplayTests.cs` gains a
  snapshot field rather than a test.
- **Build: 0 warnings, 0 errors** across four projects, measured after deleting every `bin`, `obj` and
  `.godot` directory rather than after `dotnet clean` — the cheaper check has produced a false zero in
  this repository twice.
- `--verify` deterministic on `baseline`, `disloyal-vincent`, `resentful-tommy`.
- `--compare`: **5 distinct traces, 5 distinct chosen-action sequences** — the distinctness claim
  milestone 008 restored is preserved.
- Both viewpoint runs render. **23 of 30 viewpoint renders byte-identical** to the parent commit; the
  seven that move are Tommy's (his own confidence about the witnesses rising from "plausible" to
  "strongly supported" after the clumsy cleanup) and Salvatore's (the re-armed report reaching him
  through Vincent).
- Godot headless self-test: 4 choices, 4 decision screens, **exit 0**, and its UI text contains none of
  `Dorato's bakery is holding back what it owes`, `dorato-bakery`, `Nunzio`, any rejected-candidate
  wording, or any decimal number.

`SimulationReplayTests.Snapshot` gains `StrategyInstance.SourceEventId`: it decides which witness
beliefs the concealment's first step revises, so a run carrying a different incident on the instance
would leave the concealer holding a different view of his own exposure and score every later report
from it. It is deliberately **absent** from `BehavioralSnapshot`, which excludes every field derived
from a monotonic counter — `SourceEventId` is a `WorldEvent.Id`.

## Self-review

Per ruling 8. **This review returned findings, which is the only reason it is worth anything**; a
self-review returning none would have been recorded as weak evidence either way.

**18 mutation checks were run**, each reverting one part of the change and requiring a *named* test to
fail. All 18 are now caught. Two were not, at first, and both were defects in this milestone's own
tests rather than in its code:

- **A test that could not fail.** `No_view_carries_a_witness_belief_its_viewpoint_does_not_hold`
  forbade the production narrator's phrasing of each witness claim from appearing in
  `IntelligenceWriter`'s output. That phrase never appears there **for any viewpoint, including the
  ones who hold the claim** — so the assertion was vacuous, and it duly passed when `PlayerView.Build`
  was mutated to read every character's cognition. It is the ledger's false-assurance pattern,
  committed during the milestone whose ruling 8 exists to catch it, and it was caught only because the
  mutation check ran rather than because anything looked wrong. Replaced by
  `No_viewpoint_is_shown_a_belief_he_does_not_hold`, which asserts at the claim level over all six
  viewpoints and all five variants, and which the same mutation now fails in every variant.
- **A staged fixture that did not stage the thing.** The ruling-2 test recorded a violence event with
  its traces and then ran a concealment naming a *different* incident id, so a mutation deleting the
  incident's witness trace found nothing to delete and the test passed. Now the recorded event **is**
  the incident the concealment names.
- **Nothing pinned whose belief moves.** Concealment is never delegated in the fixture, so owner and
  executor coincide everywhere; swapping `executor` for `owner` in the new code passed all 405 tests.
  `A_delegated_cleanup_moves_the_executors_view_and_not_the_delegators` stages the split.

Two mutations were rejected as invalid rather than as passing: `WorldEvent.Traces` is
`IReadOnlyList<Trace>` over an array, so the attempts to mutate it in place did not compile. The
mutation harness was reporting a build failure as "no test failed", which is itself a false-assurance
shape — it was fixed to distinguish them before the checks were re-run.

**Recurring-failure list, walked as a checklist:**

- *A correctness fix that narrows what can be expressed.* `Revise` refuses anything but the holder's own
  inference, so there is no route for a character to lower confidence in a `Discovery` he now doubts.
  Nothing is lost — before this milestone there was no route at all — but the limit is stated rather
  than discovered later. A suppressed claim with no event id is now priced as though nobody saw it,
  where before it took the global maximum; no such claim exists in the fixture, since both suppressible
  kinds carry `ev.Id`.
- *A correctness fix that collapses distinct states.* `SourceEventId` is null both for "not a
  concealment" and for "a concealment of an unidentified incident". Only `QuietWitnesses` reads it and
  it does nothing in both cases, so nothing currently distinguishes them — flagged in case something
  later needs to.
- *A correctness fix that stops halfway along the path a value travels.* `SourceEventId` is `init`, so
  `AlterStrategy` and `DelegateStrategy` cannot silently re-point a running instance at a different
  incident. It reaches `QuietWitnesses` and the replay comparator. It is deliberately not put on the
  player boundary or into `EventPayload`, neither of which has a reader for it.
- *False-assurance tests.* Above; two found in this milestone's own work.
- *Rewriting an append-only archive at closure.* No archive was edited. Milestone 009's file is
  untouched.
- *Recording a review that did not happen.* This section describes checks that were run, and the
  measurements in it were taken after the last edit to anything they measure.

**What this cannot replace** is an adversary who does not share the author's assumptions. Every one of
the three test defects above was a place the author had convinced himself, and each was found by a
mechanical check rather than by looking again.

### Findings not acted on, because they are outside the authorized scope

Both are real, both are recorded here and carried forward, and neither was fixed.

1. **`Strategies.AdvanceInvestigation`'s "trail went cold" branch is a no-op**, for exactly the reason
   defect 1 existed. It calls
   `Learn(stale.Claim, Stance.Doubts, stale.Confidence * 0.5, SourceKind.Inference, …)` intending to
   let a detective stop treating dead street talk as actionable — and `Learn` discards a less confident
   inference, so the belief is unchanged. `Cognition.Revise` is precisely the method it needs. It is
   unreachable in all five variants at seed 42 (Kane's canvass always turns up a name), so it has never
   shown.
2. **The same global-scan shape survives in the investigation path.** `AdvanceInvestigation` picks its
   lead by `r.Claim.Subject == s.TargetId` and demotes stale claims the same way — by *location*, not
   by incident. That is ruling 1's "whatever happened at this address", in a mechanism this milestone
   was not authorized to touch. Two beatings at one shop would confuse it.

### Carried-forward item that this milestone did *not* incidentally fix

`CURRENT_MILESTONE.md` flagged the empty-domain `ConcealIncident(, target=…)` label as something a
concealment that knows its incident *might* also fix. It does not. The label reads
`StrategyInstance.Domain`, which is empty because `Generators.FromPressure` sets it from
`ctx.MyOffice?.Domain ?? ctx.Agenda.Domain` and Tommy is a soldier with no office answering a pressure
trigger with no domain. Knowing the incident does not supply the domain. Still open.

## Deferred / still carried

Everything carried into this milestone is still carried, plus the three items above.

- **The timing of a pause is observable even when the occasion is not.**
- **Whether an outfit whose boss cannot name his own soldiers is the right model.**
- **The player cannot see why an option is unavailable.**
- **Nothing prevents a Godot script from calling `Cast.Build` and `Runner.Run` directly.**
- **`AGENTS.md` mentions neither `docs/RELATIONSHIPS.md` nor the Godot headless check.** The two Godot
  commands remain recorded only in `REVIEW_LEDGER.md`'s baseline section.
- **One controlled character, one viewpoint character, chosen at the start screen and never changed.**
- **No save/load.**
- **Four decisions in ninety in-game days**, unchanged by this milestone — the Godot self-test still
  makes exactly four choices.
- Obligation is read but never moves; nothing raises trust; negative trust and decay deferred;
  `GrievanceWeight` uncapped; the tuning guesses; the cast ceiling of six; the empty-domain
  `ConcealIncident(, target=…)` label.
- **Tommy cannot roll a clean cleanup at any seed**, and **Vincent is never offered one.** Findings 2
  and 3 above. Both are cast/threshold facts and both are out of bounds under rulings 3 and 7, so they
  are carried rather than resolved.

## Commit

One implementation-and-archive commit; see `REVIEW_LEDGER.md` for its hash once reviewed. Status is not
established by this file — `CURRENT_MILESTONE.md` says what is active, and Matt's confirmation of a
named commit is the only thing that counts as acceptance.

## Correction — bec0370's numeric-literal claim, 2026-09-12

**`bec0370`'s probe — "the only numeric literals added anywhere under `src/` are the `0.2` and `0.1`
the concealment step already applies to `LegalExposure` beside them" — is false.** `824f3fc`'s diff
under `src/` also adds: `0` twice, in `Commit.cs`'s `EventId: not 0` pattern and `Utility.cs`'s
`if (s.Claim.EventId == 0) continue`; `0` and `1` together, in `Cognition.cs`'s
`Math.Clamp(confidence, 0, 1)`; and `1` again, in `Strategies.cs`'s `if (s.StepIndex == 1)`. Five more
numeric literals the probe did not count, on top of the two it named.

**The narrower conclusion the probe was built to support still holds.** Ruling 3's named coefficients —
`0.25`, `3.0`, the erosion rates, the discretion threshold — appear in no added or removed line under
`src/` in `824f3fc`. No existing scoring coefficient was retuned. What was false is the claim that
`QuietedWitnessesConfidence` and `StirredWitnessesConfidence` were the *only* numeric literals the diff
introduced, not the claim that none of ruling 3's coefficients moved.

Found by Astra's independent Class B review of the grouped `bec0370`/`22e73d1`/`925611a` commits;
Matt accepted the P2 on 2026-09-12.

### Commit

Documentation-only. No production code, test, fixture, or other archive changed. See
`REVIEW_LEDGER.md`'s active-range row for `bec0370`, `22e73d1`, `925611a` for Astra's review, its FAIL
with this single P2, and Matt's acceptance.

## Correction to the correction — the full grouped-review record, 2026-09-12

Appended, not folded into the section above, per this archive's append-only rule. Astra's independent
Class B review of the grouped `bec0370`/`22e73d1`/`925611a` commits returned **three** P2 findings, not
the single one the section above recorded, and that section's own replacement undercounted its own
claim. Matt accepted all three original findings and both gaps in the first correction, all on
2026-09-12.

**P2-1 — the numeric-literal claim above, corrected count.** The section above says "five more numeric
literals the probe did not count." That is itself wrong: the diff adds **six** additional executable
numeric-literal occurrences beyond the named `0.2` and `0.1`, not five. The missed one is
`Cognition.cs`'s `if (i < 0) return null;` in `Revise` — a third `0`, alongside `Commit.cs`'s
`EventId: not 0`, `Utility.cs`'s `EventId == 0`, the `0`/`1` pair in `Cognition.cs`'s
`Math.Clamp(confidence, 0, 1)`, and `Strategies.cs`'s `if (s.StepIndex == 1)`. The narrower conclusion
is unaffected either way: none of ruling 3's named coefficients (`0.25`, `3.0`, the erosion rates, the
discretion threshold) appear in any added or removed line under `src/` in `824f3fc`, so no existing
scoring coefficient was retuned.

**P2-2 — `925611a`'s false historical superlative.** "Milestone 010 needed no corrective round and is
**the first** here accepted on a review nobody but its author performed" is false. This ledger's own
legacy-coverage row for `7ca7819` records milestone 009 as also accepted on self-review only, and
milestone 009 precedes milestone 010. Milestone 010 is not the first self-review-only acceptance in
this project's history — it is, at best, the second.

**P2-3 — `925611a`'s overstated acceptance coverage.** "The coverage checkpoint moves to `824f3fc` and
the two outstanding rows are folded in here" overstated what Matt's 2026-08-18 acceptance actually
covered. Matt accepted milestone 010 itself, at `824f3fc`; he did not independently review `bec0370`'s
or `22e73d1`'s own content — the re-verification probes and the milestone 011 plan. "Folded in" is not
the same claim as "reviewed and accepted," and the ledger's own subsequent history shows the gap: it
carried this same group as an unresolved row through the entire 2026-09-12 process normalization,
until Astra's review settled it.

Found by Astra's independent Class B review, covering both the original three commits and the first
correction above. Matt accepted all three on 2026-09-12.

### Commit

Documentation-only. No production code, test, fixture, or other archive changed. See
`REVIEW_LEDGER.md`'s active-range row for `bec0370`, `22e73d1`, `925611a` for the full disposition, and
`CURRENT_MILESTONE.md` for this correction's own pending-review status.

## Correction to the second correction — the actual P2-2 and P2-3, 2026-09-12

Appended, not rewriting either section above, per this archive's append-only rule. The second
correction above misidentified what Astra's original P2-2 and P2-3 findings actually said. Matt
accepted the corrected account of both on 2026-09-12. P2-1's finding and its six-occurrence count,
above, are unaffected and stand as corrected.

**P2-2, correctly stated — `925611a`'s false "first" claim was about no corrective round, not about
self-review.** `925611a`'s own `REVIEW_LEDGER.md` edit read: "Milestone 010 is the first accepted on a
review nobody but its author performed, **and the first whose implementation commit needed no
corrective round**." The second correction above attacked the first half of that sentence, using
milestone 009's self-review-only acceptance as its counterexample — the wrong half and the wrong
counterexample. Milestone 009 needed four corrective rounds (see its own archive's Corrections 1–4),
so it is not a counterexample to "needed no corrective round" at all. The half that is actually false
is the second: milestone 010 was not the first whose implementation commit needed no corrective round.
**Milestone 002's implementation commit, `7032981`, was Codex-reviewed, returned no findings, and was
accepted with no corrective round** — and it precedes milestone 010 by every measure. The milestone-009
account in the second correction is withdrawn as a mischaracterization of this finding, not as a
retraction of the fact it stated — milestone 009 was in fact self-review-only accepted; that fact just
answers a different question than the one `925611a`'s "first whose implementation commit needed no
corrective round" got wrong.

**P2-3, correctly stated — `925611a`'s false blanket claim concerned every row from `c0bb60f` onward,
not "the two outstanding rows."** `925611a`'s own `REVIEW_LEDGER.md` edit read: "**Every row from
`c0bb60f` onward was produced without Codex**, and their status rests on Matt's acceptance and a
self-review." That same edit adds the row for `12d1054` — a commit between `c0bb60f` and `824f3fc` —
reading only "**Status not established**." A row explicitly recorded as status-not-established does
not rest on Matt's acceptance and a self-review; the blanket claim is falsified by a row `925611a`
itself added in the same diff. The second correction above instead attacked "the two outstanding rows
are folded in here," a different sentence in the same commit message that was never shown to be false,
and that account is withdrawn as this finding's basis.

The narrower conclusion carried from P2-1 is unaffected: no existing scoring coefficient was retuned,
and the six-occurrence count stands.

Found by Astra's independent Class B review of the grouped `bec0370`/`22e73d1`/`925611a` commits and of
the second correction's account of P2-2 and P2-3. Matt accepted the corrected findings on 2026-09-12.

### Commit

Documentation-only. No production code, test, fixture, or other archive changed. See
`REVIEW_LEDGER.md`'s active-range row for `bec0370`, `22e73d1`, `925611a` for the full, current
disposition, and `CURRENT_MILESTONE.md` for this correction's own pending-review status.

## Accepted closeout — correction chain closed at `0482dfc`, 2026-09-12

Astra (Codex) independently reviewed the exact Claude-authored correction `0482dfc` as Class B:
**PASS, no blocking findings.** The reviewer did not author that correction or the earlier commits
in this correction chain. Matt accepted this PASS on 2026-09-12. This supersedes the pending-review
status at the end of the third correction above.

The original grouped review covered all three exact commits `bec0370`, `22e73d1`, and `925611a`:
**FAIL, three P2s**, accepted by Matt. The first correction `da43fbb` received independent Class B
FAIL for omitting two findings and undercounting the numeric occurrences. The second correction
`bad7ad4` received independent Class B FAIL for misidentifying the original P2-2 and P2-3. Matt
accepted both rounds' findings. The third correction `0482dfc` passed and closes the correction
chain; it does not change any earlier exact-commit FAIL into PASS. All three correction accounts
above remain standing as append-only history.

Evidence checked for the `0482dfc` review: its complete exact diff, the original historical claims
and milestone-002 counterexample, the corrected six-occurrence account and no-retuning conclusion,
preservation of the existing archive as an unchanged prefix, the three-file documentation scope,
and `git diff --check`. The unrelated roadmap content hash remained unchanged. This Class B review
did not rerun simulation tests or historical runtime measurements; its PASS concerns the
documentation correction, not a fresh verification of simulation behaviour.

Matt subsequently explicitly authorized recording this acceptance, closing out the documentation,
and pushing it to GitHub on 2026-09-12, replacing the earlier instruction to defer the record.
That authorization covers this file, `CURRENT_MILESTONE.md`, and `REVIEW_LEDGER.md` only. The
pre-existing `ROADMAP.md` change remains excluded. The closeout commit itself has no independent
review outcome recorded here. `d6af1a6` also remains pending independent exact-commit review;
the next unresolved grouped target `40f0ded` / `520924b` / `3004d2f` / `c7ae3d6` was not reviewed
or started as part of this closeout.
