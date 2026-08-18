# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**Milestone 012 — A Shortfall He Cannot Attribute — is planned and NOT authorized to begin.** Matt
chose the direction on 2026-08-18; the scope and rulings below are Claude's proposal against it and
need his sign-off before any code is written.

**Milestone 011 is implemented and awaiting Matt's review**, at `6a8a765`. Nobody has reviewed it and
nobody has accepted it. `REVIEW_LEDGER.md`'s coverage checkpoint stands at `824f3fc`, which is
milestone 010. **Milestone 012 must not begin before 011 is accepted.**

Milestones 001–010 are complete and accepted.

**Codex remains withdrawn from the review loop.** Claude implements and reviews its own work — see
`REVIEW_LEDGER.md` §"From milestone 010 onward, review is self-assessment".

## What this milestone is for

Milestones 010 and 011 produced five findings of the same shape — *the fixture cannot exercise this*.
Two incidents at one shop are only ever staged; the cold-trail branch is unreachable at every seed;
nobody holds a scored relationship with Kane; Tommy can never roll a clean cleanup; Vincent is never
offered one. The mechanisms have got dense and the scenario has not kept up. That is milestone 006's
finding returning, and milestone 007 is the precedent for answering it.

**The root cause is one thing, and it is measurable.** At seed 42, every variant, day 90:

| | value |
|---|---|
| `OrgCondition.RevenueLoss` | **0.90**, reached early and never falling |
| Assignments issued | **3**, all "restore the harbour tribute", all aimed at the grocery |
| Boss's stance on `BusinessRefusesTribute(grocery)` | still `Believes` — 0.75, eroded to 0.47 in `watchful-boss` and 0.37 in `cautious-vincent` where Vincent contradicts him |
| Characters who know the bakery is refusing | **nobody, in any variant** |

Salvatore is permanently 0.90 short. He keeps sending his capo after a shop his capo has personally
watched start paying. And he never forms the thought that something else must be causing it.

**This is the fixture's most productive asymmetry working so well that it has no exit.** `Cast.cs`
argues at length for it, and the argument is right: telling Salvatore about the bakery at the start
was tried, and it handed Vincent a second errand on the very wake where he would otherwise have gone
to ask his own man for an account — crowding out the delegator's question that milestones 007 to 011
are built on. Partial knowledge is not a workaround there; it is what leaves him room to think.

But an organisation that can never attribute a shortfall is not a modelling choice, it is a dead end.
The boss holds an objective condition and a belief about its cause, and when the second is
contradicted he has everything he needs to suspect the first has another cause — and no way to think
it.

**The fix is a missing inference, not a tuning change.** `Decision/Inference.cs` already exists as the
place a character derives something from what he holds, and already sets the precedent for
institutional facts.

## Scope

**In:**

1. **A boss can infer that a shortfall he cannot attribute has another cause.** When the
   organisational condition says the takings are short and the refusal he blames it on has been
   contradicted, he may come to suspect that something else in his domain is refusing. Through
   `Inference`, from what he holds — never from `World.Businesses`, and never a lookup of which shop
   it actually is.
2. **Make that suspicion actionable without naming the answer.** He holds "something in the harbour
   is still not paying", not "the bakery is not paying". Turning that into a named target is
   somebody's *work* — an existing candidate route, not a fact handed over. Establish which route
   before writing anything; if none fits, that is a finding and the milestone reports it rather than
   inventing a generator to close the gap.
3. **Measure what the second campaign produces**, and report it whichever way it falls: does anybody
   go and look; does the bakery get collected from; does it escalate; is there a second incident.
4. **Measure what a second incident makes load-bearing**, precisely, and state what it does *not*.

**Out:** no new characters (the cast stays at six); no new scenario variants; no change to what the
boss is told at the start — the opening asymmetry is preserved exactly, and this milestone adds a way
*out* of it rather than removing it; no tuning of Nunzio's psychology, the grocer's, the resistance
values, or the escalation ladder to produce an incident; no persistence; no tiering; no arrest model;
no global attention value.

## Rulings taken at planning time

**1 — The opening asymmetry is preserved exactly.** Salvatore still starts knowing about the grocery
and not the bakery. `Cast.cs`'s note stays true and stays there. What this milestone adds is a route
by which the organisation can *come to* notice, late and by its own reasoning — after the delegator's
question has already had its uncontested wake. **If the second errand crowds out that question after
all, that is the result**, and it is measured against milestone 011's figures rather than explained
away.

**2 — He infers a gap, not an answer.** The inference may read organisational conditions and his own
cognition. It may not read `World.Businesses`, the truth log, or any other character's cognition. He
must end up suspecting that *something* is refusing without being told *what* — because a boss who
infers the exact shop from his own books has not inferred anything, he has been handed the fixture.

**3 — Being wrong must stay possible.** The takings can be short for reasons other than a second
refusal, and a boss who concludes otherwise is drawing a defeasible conclusion. The stance and
provenance must reflect that: `Suspects`, `SourceKind.Inference`, sourced to himself, and revisable
through `Cognition.Revise` like any other reading of his own.

**4 — No coefficient is tuned to produce a second incident.** Not Nunzio's psychology, not his shop's
resistance, not the fear applied by threats, not the escalation ladder. Whether the second campaign
reaches violence is a property of the model already there. **If it does not, that is the milestone's
result** — measured, stated, recorded as a finding.

**5 — The gate comes from what the model already computes.** Contestedness is a fact the model
records; a fresh confidence threshold would be a new coefficient wearing a rule's clothes. Key it on
what is already there.

**6 — Actor parity and the player boundary.** Whatever becomes available is available to a player
controlling that character through the same candidate set. Nothing new crosses the boundary, and the
snapshot does not start carrying an organisational condition.

**7 — Baselines will move, and the two claims are measured separately.** A second campaign will move
every variant. **Do not report "a second incident makes the incident-scoping rules load-bearing"
without checking which ones** — two incidents at *different shops* leave location and incident still
correlated one-to-one, so milestone 011's lead-pickup and completion rules stay staged. What a second
incident does exercise naturally is milestone 010's: one man holding two `WitnessSawIncident` beliefs.
State both halves.

**8 — Self-review by the method that has actually found things**: enumerate the real surface
empirically and diff it; mutation-check every fix by reverting it and watching a *named* test fail;
test for the kind of defect rather than the reported instance; walk the recurring-failure list as a
checklist. Milestone 011's self-review found three defects in its own tests and every one came from a
mutation check. A review returning no findings is weak evidence and is recorded as such.

## Implementation plan

1. **Establish where the inference belongs and what it may read.** `Decision/Inference.cs` and how
   `PerceivedSituation` reaches organisational conditions — it may not today, and if it does not, how
   a condition reaches a decision without putting `World` behind the scorer is the first question to
   answer. *Do not add a world reference to the perceived view* — `REVIEW_LEDGER.md`'s architecture
   checklist names that specifically.
2. **The inference itself**, gated on what the model already computes. *Tests: it fires when the
   attributed cause is contested and the condition is live; it does not fire while he is
   uncontradicted; it names no shop; it is `Suspects` via `Inference` and is revisable.*
3. **The route from suspicion to a named target.** Read the existing generators before writing one.
   The honest shapes are an investigation, a question to somebody who would know, or a collection
   attempt that discovers the refusal on arrival — all of which exist. *Ruling 2 forbids the fourth
   shape, which is handing him the name.*
4. **Measure the second campaign**, all five variants: assignments, targets, whether the bakery is
   approached, how far it escalates, and whether milestone 011's exchanges survive — Kane's question,
   Salvatore's allegation to Vincent, Tommy's answers. Record against milestone 011's figures.
5. **Measure what became load-bearing**, and convert the staged tests that no longer need staging.
   Per ruling 7, expect this to cover milestone 010's witness scoping and not milestone 011's
   incident-vs-location rules.
6. **Archive as `docs/milestones/012-…md`, reset this file, one coherent commit, stop.**

## Open questions to settle during implementation, not now

- Can a decision see an organisational condition at all today, and if not, what is the smallest way
  in that does not put `World` behind `Utility`? This decides step 1 and is the first thing to check.
- Does Nunzio fold at a threat? `Cast.cs` says he is softer on pride and harder on security than
  Marco, deliberately, "so the second cycle is a second experiment rather than a replay". If he folds
  without violence there is no second incident, and ruling 4 says that stands.
- Does the second errand crowd out the delegator's question after all? Ruling 1 governs the answer.
- Does a second campaign fit inside ninety days? The first runs 2 March to 1 April; a second starting
  in mid-April has room, but the run ends 31 May.

## Carried forward

Everything carried into milestone 011, plus what it added. Full list at the end of
`docs/milestones/011-the-detective-has-no-next-move.md`. The items this milestone is most likely to
touch or resolve:

- **Two incidents at one shop are only ever staged**, and ruling 7 says this milestone probably does
  not change that.
- **The cold-trail branch is unreachable at every seed** — a second case might reach it, and might
  not.
- **Nobody holds a scored relationship with Kane**, so the attitude list can never describe a woman
  in a natural run.
- **The developer trace still says "he" for everybody**, 59 strings, deliberately outside 011.
- **`AdvanceInvestigation` reads and writes `owner` throughout**, so a delegated investigation would
  put findings in the head of a man who was not there.
- **Four known reasons the denial stays shut**, of which loyalty is the smallest.
- The tuning guesses; the cast ceiling of six; obligation read but never moved; nothing raises trust;
  negative trust and decay deferred; `GrievanceWeight` uncapped; no save/load; the empty-domain
  `ConcealIncident(, target=…)` label.

## Ordered review process

Unchanged. Matt takes commits in order, oldest first; each review names the exact commit whose diff
was inspected; the coverage table in `REVIEW_LEDGER.md` is the record. **Never write "verified" or
"accepted" from a review report alone** — including one of Claude's own. Matt's confirmation of a
named commit is the only thing that counts, and that rule matters more now that the reviewer and the
author are the same.
