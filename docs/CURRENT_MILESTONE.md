# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**Milestone 012 — A Shortfall He Cannot Attribute — is ACTIVE.** Authorized by Matt on 2026-08-18.

It was planned as 013 behind an instruments milestone and swapped forward, for one reason: **Codex
returns in about two days, and instrument work is a poor target for an adversary whose strength is
simulation design.** Codex should arrive to find milestone 011 and a design-level milestone, not
tooling. The instruments plan is preserved in `ROADMAP.md` and loses nothing by waiting — its two best
findings are recorded there and carried below.

**Milestone 011 is self-reviewed and cleared to build on, at `6a8a765`. It is not accepted.** The
self-review returned three findings, corrected at `3871d23`. `REVIEW_LEDGER.md`'s coverage checkpoint
stands at `824f3fc`. **A Codex round on `6a8a765` is expected, and this milestone inherits whatever it
concludes** — see the ledger's "cleared to build on is not accepted".

Milestones 001–010 are complete and accepted.

## What this milestone is for

Milestones 010 and 011 produced five findings of the same shape — *the fixture cannot exercise this*.
Two incidents at one shop are only ever staged; the cold-trail branch is unreachable at every seed;
nobody holds a scored relationship with Kane; Tommy can never roll a clean cleanup; Vincent is never
offered one. The mechanisms have got dense and the scenario has not kept up. That is milestone 006's
finding returning, and milestone 007 is the precedent for answering it.

**The root cause is one thing, and it is measured.** At seed 42, every variant, day 90:

| | value |
|---|---|
| `OrgCondition.RevenueLoss` | **0.90**, reached early and never falling |
| Assignments issued | **3**, all "restore the harbour tribute", all aimed at the grocery |
| Boss's stance on `BusinessRefusesTribute(grocery)` | still `Believes` — 0.75, eroded to 0.47 in `watchful-boss` and 0.37 in `cautious-vincent` where Vincent contradicts him |
| Characters who know the bakery is refusing | **nobody, in any variant** |

Salvatore is permanently 0.90 short. He keeps sending his capo after a shop his capo has personally
watched start paying. And he never forms the thought that something else must be causing it.

**This is the fixture's most productive asymmetry working so well that it has no exit.** `Cast.cs`
argues at length for it and the argument is right: telling Salvatore about the bakery at the start was
tried, and it handed Vincent a second errand on the very wake where he would otherwise have gone to
ask his own man for an account — crowding out the delegator's question that milestones 007 to 011 are
built on. Partial knowledge is not a workaround there; it is what leaves him room to think.

But an organisation that can *never* attribute a shortfall is not a modelling choice, it is a dead
end. The boss holds an objective condition and a belief about its cause, and when the second is
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
2. **Make that suspicion actionable without naming the answer.** He holds "something in the harbour is
   still not paying", not "the bakery is not paying". Turning that into a named target is somebody's
   *work* — an existing candidate route, not a fact handed over. Establish which route before writing
   anything; if none fits, that is a finding and the milestone reports it rather than inventing a
   generator to close the gap.
3. **Measure what the second campaign produces**, and report it whichever way it falls: does anybody
   go and look; does the bakery get collected from; does it escalate; is there a second incident.
4. **Measure what a second incident makes load-bearing**, precisely, and state what it does *not*.

**Out:** no new characters (the cast stays at six); no new scenario variants; no change to what the
boss is told at the start — the opening asymmetry is preserved exactly, and this milestone adds a way
*out* of it rather than removing it; no tuning of Nunzio's psychology, the grocer's, the resistance
values, or the escalation ladder to produce an incident; no persistence; no tiering; no arrest model;
no global attention value; no instruments work (that is a `ROADMAP.md` candidate again).

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
controlling that character through the same candidate set — and **that is checked by driving a session
rather than argued by construction**, which is exactly what milestone 011's self-review found missing
from its own ruling 5. Nothing new crosses the boundary, and the snapshot does not start carrying an
organisational condition.

**7 — Baselines will move, and the two claims are measured separately.** A second campaign will move
every variant. **Do not report "a second incident makes the incident-scoping rules load-bearing"
without checking which ones** — two incidents at *different shops* leave location and incident still
correlated one-to-one, so milestone 011's lead-pickup and completion rules stay staged. What a second
incident does exercise naturally is milestone 010's: one man holding two `WitnessSawIncident` beliefs.
State both halves.

**8 — Self-review by the method that has actually found things**, and it is now a delegated function
rather than a supplement: enumerate the real surface empirically and diff it; mutation-check every fix
by reverting it and watching a *named* test fail; test for the kind of defect rather than the reported
instance; walk the recurring-failure list as a checklist. **An instrument is not evidence until it has
been shown to report correctly** — milestone 011's review nearly filed a false finding because a
harness was believed before it was checked. A review returning no findings is weak evidence and is
recorded as such.

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
5. **Measure what became load-bearing**, and convert the staged tests that no longer need staging. Per
   ruling 7, expect this to cover milestone 010's witness scoping and not milestone 011's
   incident-vs-location rules.
6. **Archive as `docs/milestones/012-…md`, reset this file, one coherent commit, stop.**

## Answered during implementation

**Where the inference belongs — settled 2026-08-18.** `Decision/Inference.cs`. `Inference.Reconsider`
already takes `World` and runs **at the top of the pipeline, before `PerceivedSituation` is built**, so
it never puts a world reference behind the scorer. Its docstring already bounds what it may read, and
the new inference reads strictly less than the existing one: `org.Condition(RevenueLoss)` and the
boss's own cognition, and **not** `world.Businesses`, which is the read ruling 2 forbids.

**And the mechanical reason nobody ever goes to the bakery is smaller and worse than the plan
assumed.** `FromResponsibility`'s collection branch is:

```
var refusing = ctx.Perceived.OfKind(BusinessRefusesTribute).Select(r => r.Claim.Subject).FirstOrDefault();
string? mark = refusing ?? ctx.VisibleTargets.FirstOrDefault();
```

**It picks exactly one shop.** `VisibleTargets` is every business in the domain ordered by id, so the
fallback is always `bellini-grocery` — the alphabetically first, and the one Vincent has personally
watched start paying. The candidate it then builds requires `BusinessRefusesTribute(mark)`, which he
has *rejected*, so the knowledge filter correctly refuses it. **`dorato-bakery` appears zero times in
the full decision trace — rejected candidates included — in every variant.** It is not outscored and
not filtered; it is never considered.

So a capo standing on a patch with two shops, who knows the first is paying, is offered nothing about
the second. That is a defect in mark selection, and it is separable from the inference: the inference
gives him a *reason* to look, and this is why there is nothing to look *at*. Both are needed, and the
milestone should report them as two findings rather than one.

## Open questions to settle during implementation, not now
- Does Nunzio fold at a threat? `Cast.cs` says he is softer on pride and harder on security than
  Marco, deliberately, "so the second cycle is a second experiment rather than a replay". If he folds
  without violence there is no second incident, and ruling 4 says that stands.
- Does the second errand crowd out the delegator's question after all? Ruling 1 governs the answer.
- Does a second campaign fit inside ninety days? The first runs 2 March to 1 April; a second starting
  in mid-April has room, but the run ends 31 May.

## Carried forward

Everything carried into milestone 011, plus what its self-review added, plus the instruments findings
that are now waiting on a candidate rather than a milestone.

**From the deferred instruments work — real findings, not speculation:**

- **Coverage has never been run**, though `coverlet.collector` has been a package reference for some
  time. At `6a8a765` it reports **92.2% line, 84.2% branch, 376 uncovered lines**, of which
  `Program.cs` is 118 and legitimately so.
- **Two live edges nothing has ever run.** `Runner.cs` raises a grievance and resentment when a
  character observes somebody else's policy breach — a relationship-write path no test and no seed has
  executed. `Utility.cs` prices a candid report made with something at stake — the counterpart of the
  denial 010 and 011 measured, and nobody has ever taken it.
- **Mutation is hand-picked**, so the harness inherits the priors it exists to escape, and it lives in
  scratch rather than in the repository. Ruling 9 of the deferred plan settled where it would go.

**From milestone 011 and its self-review:**

- **The allegation option names the same person twice**, and always will, because the target of an
  allegation is the subject of the claim. Clumsy rather than wrong; the obvious rephrasing would make
  a question read as an assertion.
- **The developer trace still says "he" for everybody**, 59 strings.
- **`AdvanceInvestigation` reads and writes `owner` throughout**, so a delegated investigation would
  put findings in the head of a man who was not there.
- **The cold-trail branch is unreachable at every seed**; **two incidents at one shop are only ever
  staged**; **nobody holds a scored relationship with Kane.** All three are this milestone's business.
- **Four known reasons the denial stays shut**, of which loyalty is the smallest.

**Longer-standing:** the tuning guesses; the cast ceiling of six; obligation read but never moved;
nothing raises trust; negative trust and decay deferred; `GrievanceWeight` uncapped; no save/load; the
empty-domain `ConcealIncident(, target=…)` label; four decisions in ninety days in the Godot demo; the
timing of a pause is observable even when the occasion is not; the player cannot see why an option is
unavailable; nothing prevents a Godot script calling `Cast.Build` directly; `AGENTS.md` mentions
neither `docs/RELATIONSHIPS.md` nor the Godot headless check.

## Ordered review process

Unchanged. Matt takes commits in order, oldest first; each review names the exact commit whose diff
was inspected; the coverage table in `REVIEW_LEDGER.md` is the record. **Never write "verified" or
"accepted" from a review report alone** — including one of Claude's own. Matt's confirmation of a
named commit is the only thing that counts, and a self-review clears work to build on without
establishing anything about correctness.
