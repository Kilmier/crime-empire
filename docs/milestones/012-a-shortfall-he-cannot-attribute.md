# Milestone 012 — A Shortfall He Cannot Attribute

**Authorized by Matt on 2026-08-18**, swapped forward ahead of the instruments milestone so Codex
would return to a design-level milestone rather than tooling. Implemented 2026-08-18/19. Append-only:
corrections go at the bottom, never into the account above.

Codex is intermittent rather than withdrawn; a round on `6a8a765` (milestone 011) is expected at a
later date and this milestone inherits whatever it concludes. Claude implemented and reviewed its own
work — see "Self-review" below, and `REVIEW_LEDGER.md` §"From milestone 010 onward, review is
self-assessment".

## What this milestone was for

Milestones 010 and 011 produced five findings of the same shape — *the fixture cannot exercise this*.
Chief among them: the family's takings are short by two shops and Salvatore's account of why names
only one. At seed 42, every variant, day 90 (measured before this milestone): `RevenueLoss` reached
0.90 and never fell; every assignment aimed at the grocery; Salvatore's stance on
`BusinessRefusesTribute(bellini-grocery)` stayed `Believes`; nobody, in any variant, held a claim that
the bakery was refusing. Salvatore kept sending his capo after a shop his capo had personally watched
start paying, and never formed the thought that something else must be causing it. `Cast.cs` argues at
length for the opening asymmetry and the argument is right — but an organisation that can *never*
attribute a shortfall is not a modelling choice, it is a dead end.

**Two findings, established during implementation, not one.** `Decision/Inference.cs` already existed
as the place a character derives something from what he holds, and the missing piece looked at first
like a single gap. It was two:

1. **The inference itself was missing.** Nothing read the contradiction `Cognition` already records
   (`InformationRecord.Contested`) against the still-live `OrgCondition.RevenueLoss` and drew the
   obvious defeasible conclusion.
2. **`Generators.FromResponsibility`'s mark selection was broken independently of the inference.**
   `var mark = refusing ?? ctx.VisibleTargets.FirstOrDefault();` — when `refusing` was null, the
   fallback always resolved to `bellini-grocery`, the alphabetically first business, and the resulting
   candidate's `RequiredKnowledge` then always failed, because reaching that branch at all meant he
   held no `BusinessRefusesTribute` for anybody. `dorato-bakery` appeared zero times in the full
   decision trace — rejected candidates included — in every variant, at every seed. Not outscored, not
   filtered: never considered.

Both were needed. The inference gives a reason to look; the generator fix gives him something to look
at. Fixing either alone leaves the bakery untouched, and both are pinned separately below.

## Scope, as authorized

**In:** 1, the inference (organisational conditions and the boss's own cognition only, never
`World.Businesses`, never a lookup of which shop it actually is); 2, a route from the resulting
suspicion to a named target that hands him no name — an existing candidate route, not a fact invented
to close the gap; 3, measure what the second campaign produces; 4, measure what a second incident makes
load-bearing, and state what it does not.

**Out:** no new characters, no new variants, no change to what the boss is told at the start, no tuning
of Nunzio's psychology, the grocer's, the resistance values or the escalation ladder to produce an
incident, no persistence, no tiering, no arrest model, no global attention value, no instruments work.

## The eight rulings, preserved

**1 — The opening asymmetry is preserved exactly.** Salvatore still starts knowing about the grocery
and not the bakery; `Cast.cs`'s note stays true. This milestone adds a route by which the organisation
can *come to* notice, late and by its own reasoning — after the delegator's question has already had
its uncontested wake, which is measured below rather than assumed.

**2 — He infers a gap, not an answer.** The inference may read organisational conditions and his own
cognition, never `World.Businesses`, the truth log, or another character's cognition. He ends up
suspecting that *something* is refusing, never *what*.

**3 — Being wrong must stay possible.** `Suspects`, `SourceKind.Inference`, sourced to himself,
revisable through `Cognition.Revise` like any other reading of his own.

**4 — No coefficient is tuned to produce a second incident.** Not Nunzio's psychology, not resistance,
not the escalation ladder. If the second campaign never reaches violence, that is the milestone's
result — measured, stated, recorded as a finding below.

**5 — The gate comes from what the model already computes.** Contestedness is a fact `Cognition`
already records (`InformationRecord.Contested`, read through `Cognition.IsContested`); the milestone
keys on it rather than inventing a fresh confidence threshold. The one genuinely new number —
`Organization.SignificantRevenueLoss = 0.35` — is not new: it is `Sim/Runner.cs`'s pre-existing
leadership-review threshold, extracted to a shared constant rather than duplicated, so "the condition
is still live" is one number instead of two independently chosen ones standing for the same question.

**6 — Actor parity and the player boundary.** Checked by driving a session (see Tests, below) rather
than argued by construction, per milestone 011's self-review finding that this was exactly the check
its own ruling 5 had skipped. Nothing new crosses the player boundary, and the snapshot does not carry
an organisational condition — the suspicion reaches the player only as an ordinary qualitative belief,
exactly like any other.

**7 — Baselines will move, and the two claims are measured separately.** See "What was measured", below.

**8 — Self-review by the method that has actually found things.** See "Self-review", below.

## Answered during implementation

Recorded in `CURRENT_MILESTONE.md` while the work was live and reproduced here because the file resets:

**Where the inference belongs.** `Decision/Inference.cs`. `Inference.Reconsider` already runs at the
top of the pipeline, before `PerceivedSituation` is built, so it never puts a world reference behind
the scorer — the new inference reads strictly less than the existing policy-breach one already there:
`org.Condition(RevenueLoss)` and the boss's own cognition, never `world.Businesses`.

**The mechanical reason nobody ever went to the bakery**, quoted above under "What this milestone was
for" — smaller and worse than assumed at planning time: a mark-selection defect, not a scoring or
filtering one, and separable from the inference.

## What was implemented

### 1 — the inference

`Decision/Inference.cs` gains `ReconsiderUnattributedShortfall`, called from `Reconsider` alongside the
existing policy-breach inference. Gated on three things, all facts the model already holds:

- **Only the organisation's boss.** `who.Id != org.BossId => return`. The organisational condition is
  a fact about the family's books, not about any one man's patch, and only its leadership is
  answerable for it.
- **The condition is still live.** `org.Condition(RevenueLoss) >= Organization.SignificantRevenueLoss`
  — the same threshold `Sim/Runner.cs`'s leadership review already used to decide whether a fresh
  assignment was warranted, now shared rather than duplicated.
- **The attribution he currently holds has actually been contradicted.** `who.Cognition.IsContested`
  on a held `BusinessRefusesTribute` record — a fact `Cognition.Receive` already records at the moment
  of disagreement, never re-derived from the current stance.

The resulting claim is `UnattributedShortfall(domain)` — a new `ClaimKind`, Subject a domain, never a
business. Which domain: `org.Offices.Select(o => o.Domain).FirstOrDefault()`, the same institutional
read the existing inference already performs to find who holds which office. The fixture has exactly
one office and one domain; a second domain is not something this milestone was asked to disambiguate
between, and the read is documented as bounded by that.

`Stance.Suspects`, `SourceKind.Inference`, sourced to himself, confidence
`SuspicionOfFact * attributed.Confidence` — the same `SuspicionOfFact = 0.45` constant the existing
inference already uses, not a second number invented for this one. Re-deriving the identical
conclusion on every wake is guarded the same way the existing inference guards it: if his current
suspicion is already at least as confident, nothing is relearned and the reconsideration stamp does not
move.

### 2 — the route from suspicion to a named target

Two changes, both reusing existing channels rather than inventing one.

**`Sim/Runner.cs`'s `LeadershipReview`** already builds the `Disclosed` list an assignment carries from
`boss.Cognition.OfKind(BusinessRefusesTribute)`. It now also discloses
`boss.Cognition.OfKind(UnattributedShortfall)`, at the boss's own stance and confidence — not firmed up
to `Believes 0.75` the way the named-refuser line is, because this is a suspicion and should not arrive
sounding surer than the man who formed it is himself. The capo receives it through the same
`Cognition.Receive` path any other briefing uses; nothing about the delivery mechanism is new.

**`Decision/Generators.cs`'s `FromResponsibility`** no longer falls back to
`ctx.VisibleTargets.FirstOrDefault()` unconditionally. With no named refuser, it checks whether the
actor holds `UnattributedShortfall(domain)`; if so, it offers the first visible business he has not
already concluded is paying (`Position(...)?.Stance != Rejects`), with `RequiredKnowledge` naming the
gap claim rather than a fact about the shop he has not established. Without the suspicion, nothing is
proposed at all — not even the old dead, always-rejected candidate. This is "a collection attempt that
discovers the refusal on arrival", one of the three honest shapes the planning document named; the
other two (an investigation, a question to somebody who would know) were checked and do not fit —
neither Salvatore nor Vincent has `Skill.Investigation >= 0.4`, and nobody is positioned to be *asked*
about a business nobody has named.

**One structural bonus, not authored deliberately.** Because the gap claim arrives as ordinary
testimony, `FromRelationship`'s existing secondhand-corroboration generator also picks it up on its
own — Vincent asks Tommy for his own account of `UnattributedShortfall(harbour)` in every measured run
that reaches this state. Nothing was written for this; it is the second of the three honest shapes
(a question to somebody who would know) firing through a route that already existed, on a claim that
happens to satisfy its ordinary conditions. Recorded because it is a real, measured consequence of the
design, not because it was scoped.

### Player-facing rendering

`Session/PlayerNarration.cs` and `Decision/Filters.cs` each gain a case for `ClaimKind.UnattributedShortfall`.
Both read `c.Subject` directly rather than through the `name()` lookup — the subject is a domain, never
a person or a business, and calling a display-name resolver on it would be a category error even
though it happens to degrade harmlessly (`Name()`'s fallback returns the raw string unresolved). Renders
as *"something in the harbour still is not paying what it owes"*, qualified exactly like any other
belief — no decimal, no raw enum name, no shop.

## What was measured

Run at seed 42, all five variants, after deleting every `bin`, `obj` and `.godot` directory.

- Build: **0 warnings, 0 errors** across four projects.
- Tests: **454 passed, 0 failed** (440 before this milestone). 14 added, in new
  `ShortfallAttributionTests.cs`. One pre-existing test's expected value changed rather than being
  added to — see "Baselines moved", below.
- `--verify` deterministic on `baseline`, `disloyal-vincent`, `resentful-tommy`.
- Both required viewpoint runs (`--variant disloyal-vincent --viewpoint salvatore`,
  `--variant baseline --viewpoint vincent`) exit 0 and render correctly, checked by eye against the
  no-leak rules.
- Godot headless self-test: **4 choices, 4 decision screens, exit 0**, unchanged from the accepted
  baseline, and its output contains none of `dorato-bakery`, `Nunzio`, any rejected-candidate wording,
  or any decimal number.

### `--compare`

| Variant | Trace | Chosen actions | Decisions | Violence | Bakery |
|---|---|---|---|---|---|
| baseline | `FEE45FD886F18CA8` | `7716CDDE3D0CA3A6` | 47 | 1 incident | not paying (0.50) |
| cautious-vincent | `86EC1ADA4A4E9179` | `7506045DDEB2DE14` | 24 | none | **paying (0.25)** |
| watchful-boss | `84AC3F65E4102EBA` | `955921AA69ABA44C` | 51 | 1 incident | **paying (0.25)** |
| disloyal-vincent | `45CCF5ADC6EC0302` | `BECCA9ED2E4E7137` | 48 | 1 incident | not paying (0.50) |
| resentful-tommy | `F5BD93386DE04082` | `B9B6D3BBE6A69200` | 42 | 1 incident | not paying (0.50) |

5 configurations, 5 distinct traces, 5 distinct chosen-action sequences — unchanged from milestone 011.

**The mechanism fires in exactly two of five variants, and only those two are the two where Salvatore's
attribution is actually contradicted before day 90.** `watchful-boss` and `cautious-vincent` are the
two variants where Vincent's own established rejection of `BusinessRefusesTribute(bellini-grocery)`
reaches Salvatore and marks it `Contested` — matching the pre-milestone measurement in
`CURRENT_MILESTONE.md` ("eroded to 0.47 in `watchful-boss` and 0.37 in `cautious-vincent`"). In
`baseline`, `disloyal-vincent` and `resentful-tommy` the contradiction never reaches him within the run,
the inference never fires, and the bakery is untouched — exactly as it was before this milestone. This
is not a coincidence checked after the fact: it is the gate (ruling 5) doing exactly what it was built
to do, confirmed against real runs rather than assumed from the design.

**Driven directly for `watchful-boss`, at seed 42:**

- 5 Apr — Vincent reports his rejection of the grocery claim to Salvatore, contradicting him. `Suspects
  UnattributedShortfall(harbour)` forms the next time Salvatore is woken.
- 6 Apr — the next leadership review reissues Vincent's assignment, now disclosing the gap alongside
  the (still-standing, still-`Believes`) stale grocery claim. Vincent receives both; the second
  assignment-delivery contradicts *him*, since his own rejection is protected (`Discovery`) and merely
  erodes rather than reverses.
- 11 May — Vincent, having nothing better to do with the assignment and holding
  `UnattributedShortfall(harbour)`, generates and chooses `lean on dorato-bakery`
  (`Threaten`, score 3.09), required knowledge the gap claim, uncertainty penalty visible in the trace
  (*"his information here is thin"*) reflecting the suspicion's own modest confidence.
- The bakery pays without escalating past `Threaten`. **No second incident.**

**Driven directly for `cautious-vincent`:** the same shape, `Persuade` first (talk it round, 2.82),
escalating once to `Threaten` after the target does not fold immediately, delegated to Tommy along the
way, completing without violence. Vincent's traits here are the least aggressive in the cast; that he
still reaches for `Threaten` once and not `Force` is the escalation ladder behaving as designed rather
than as tuned.

### Ruling 4 — the second campaign never reaches violence, in either variant it runs in

**Recorded as the result, per the ruling, not chased.** Violence incident count is **1 in every
variant** — the original grocery incident, unchanged. In both variants where the bakery gets
approached, Nunzio concedes at `Persuade` or `Threaten` and the escalation never reaches `Force`. No
coefficient governing Nunzio, the bakery's resistance, or the escalation ladder was touched to produce
or prevent this; it falls out of the same scoring competition every other collection in the fixture
already runs.

### Ruling 7 — what a second incident would make load-bearing, checked rather than assumed

**No second incident occurs in any variant at seed 42.** Milestone 010's witness-scoping (one man
holding two `WitnessSawIncident` beliefs) and milestone 011's incident-vs-location rules therefore stay
exactly where milestone 011 left them: real, correct, and still exercised only by staged tests, not by
a natural run. This milestone does not make either newly load-bearing, and states that plainly rather
than reporting the stronger claim ruling 7 warned against.

### Ruling 1 — does the second errand crowd out the delegator's question?

**No, checked directly rather than assumed.** In `watchful-boss`, Vincent's delegator's question to
Tommy (*"asked Tommy Nardo for his own account of `PersonUsedViolence`"*) fires on 3 April; the
shortfall suspicion is not disclosed to him until 6 April and he does not act on it until 11 May. In
`cautious-vincent` the same ordering holds. Milestone 011's exchanges — Kane's allegation to Tommy,
Salvatore's allegation to Vincent, Tommy's answers — all still fire in every variant, checked by grep
against the rendered trace. The opening asymmetry's payoff, which milestone 012 was scoped not to
disturb, is intact.

### Baselines moved, and the reason is stated

**`RelationalConsequenceTests.The_scenario_produces_the_expected_number_of_conflicts(watchful-boss)`
moved from 4 to 3.** Driven directly: the run still contains three conflicts, but the composition
changed. Before this milestone, both extra conflicts (over the shared base of two) were Salvatore
hearing Vincent reject the grocery claim, twice. After: only *one* is that (5 April); the other two are
the mirror image — Vincent hearing Salvatore's stale assignment-delivery reassert `Believes` on a claim
Vincent has personally rejected (6 April, 11 May). Salvatore's own stance on the grocery claim never
moves off `Believes` — only its confidence erodes — so every reissued assignment still carries and
still contradicts. What changed is Vincent's side: contradicted twice more, he does not report the same
old news to Salvatore a second time within the ninety days, because by the second contradiction he has
a competing, higher-value candidate this milestone's fix gave him — checking on the bakery — and it
wins the scoring competition a second report would have had to win instead. A redistribution between
whose side holds the conflict, not a disappearance of the mechanism. `The_capo_trusts_his_boss_less_after_being_contradicted`
and every other conflict-dependent assertion pass unchanged, because the Vincent-side conflict this
test already required is still present — now twice over instead of never.

No other pre-existing test needed a value change. `cautious-vincent`'s conflict count (3) is unchanged;
its extra Vincent-side conflict, if any, did not move the total because the shape there differs (see
the trace).

## Tests

New `ShortfallAttributionTests.cs`, 14 facts:

- The inference fires when the attribution is contested and the condition is live, driven through the
  real `Cognition.Receive` contradiction path rather than a hand-set `Contested` flag.
- It does not fire while uncontradicted, or while the condition has gone quiet.
- It names the domain and never a shop (checked against both business ids by name, not merely against
  the claim's own subject).
- It is his own reading and revisable through `Cognition.Revise`.
- Only the boss forms it — staged by giving Vincent the identical contradiction shape directly, so the
  only thing standing between him and the suspicion is the boss-only gate.
- Reconsidering again without new grounds does not move the reconsideration stamp.
- A named refuser is still the ordinary route, unchanged, with the original requirement.
- With no named refuser and no suspicion, nothing is proposed at all — the defect itself, pinned
  directly against `Generators.GenerateAll`.
- A suspected gap offers the first business not yet ruled out, offers the second once the first is
  ruled out, and proposes nothing once every business is ruled out.
- The assignment channel actually carries the suspicion, driven through the real scheduler
  (`Runner.Step`) to the leadership review `Cast.Build` already queues, not through a hand-built event.
- Ruling 6, driven through a real `SimulationSession` controlling Vincent in `watchful-boss`, resolved
  automatically at every pause (`SimulationSession.ResolveAutomatically`, which reproduces the batch
  history rather than an arbitrary "always pick the first option" policy that `Available`'s deliberate
  candidate-id ordering, per milestone 009 ruling 5, would otherwise defeat) — confirms *"lean on
  Dorato's bakery"* is actually among the options offered to a controlled player, not merely chosen
  autonomously.

## Self-review

Per ruling 8, `REVIEW_LEDGER.md`'s standing method.

**Mutation checks: 5, each reverting one part of the change and requiring a *named* test to fail.**

| Reverted | Test that must fail | Result |
|---|---|---|
| The `IsContested` gate | `He_does_not_suspect_a_gap_while_uncontradicted` | Failed as required |
| The `RevenueLoss` threshold gate | `He_does_not_suspect_a_gap_while_the_condition_has_gone_quiet` | Failed as required |
| The boss-only gate | `Only_the_boss_forms_the_suspicion` | Failed as required |
| The mark-selection fix, reverted to the old unconditional fallback | `With_no_named_refuser_and_no_suspicion_nothing_is_proposed`, `A_suspected_gap_offers_the_first_business_he_has_not_ruled_out`, `A_suspected_gap_skips_a_business_already_concluded_to_be_paying`, `A_suspected_gap_proposes_nothing_once_every_business_is_ruled_out` | All four failed as required |
| The assignment-disclosure wiring | `The_assignment_channel_carries_the_boss_suspicion_to_the_capo` | Failed as required |

All five caught on the first attempt; none needed a second pass. After each check the fix was restored
and the full suite (454) and the `--verify` baseline hash (`FEE45FD886F18CA8`, unchanged before and
after the full revert/restore cycle) were re-confirmed, so the mutation checks are evidence about the
tests rather than a side door that left the tree in a different state than reported above.

**Recurring-failure list, walked:**

- *A fix that narrows what can be expressed.* The removed fallback branch never produced a usable
  candidate — every path through it was rejected on knowledge before this milestone. Nothing honest was
  lost; the noise it produced (a permanently-rejected `bellini-grocery` candidate, regardless of what
  was actually unresolved) is what is gone.
- *A fix that collapses distinct states.* The named-refuser and suspected-gap routes stay separate
  branches with separate `RequiredKnowledge` claims (`BusinessRefusesTribute` vs. `UnattributedShortfall`)
  — a candidate's required knowledge always names the actual reason it is conceivable to him, checked
  directly in `A_named_refuser_is_still_the_ordinary_route` and `A_suspected_gap_offers_the_first_business_he_has_not_ruled_out`.
- *A fix that stops halfway along the path a value travels.* The gap claim's path — formed by
  Salvatore, disclosed via assignment, received by Vincent, read by the generator, carried onto the
  candidate, rendered to a player — was driven end to end by real production code and checked at each
  hop, not asserted from one end.
- *False-assurance tests.* Every test above drives production entry points — `Inference.Reconsider`,
  `Cognition.Receive`, `Cognition.IsContested`, `Generators.GenerateAll`, `Runner.Step`,
  `SimulationSession.ResolveAutomatically` — never a copied predicate. `The_assignment_channel_carries_the_boss_suspicion_to_the_capo`
  in particular drives the real scheduler rather than reaching for the institutional step directly by
  reflection, which was the first draft and was replaced once a cleaner production path was found.
- *Rewriting an append-only archive at closure.* No archive was edited; this is the first commit that
  creates this one.
- *Recording a review that did not happen.* Every figure above was measured on the clean, final tree —
  rebuilt and re-verified after the mutation-check cycle, not carried over from an earlier run.

**An instrument was not trusted before it reported correctly, per ruling 8's own text.** The first draft
of the actor-parity test drove the session by always choosing `Pending.Options[0]` — the pattern
milestone 011's own equivalent test used. It failed, not because the option was absent, but because
`PreparedDecision.Available` is deliberately sorted by candidate id rather than by rank (milestone 009,
ruling 5), so "always take the first one offered" drives an entirely different, lower-ranked history
that never reaches the state being tested for. Switched to `SimulationSession.ResolveAutomatically`,
which reproduces the batch-accepted history while still recording what was offered at every pause. The
failure was informative rather than a defect: it demonstrated the ordering guarantee working as
designed, on a candidate set rich enough for the difference between "first by id" and "first by rank" to
actually matter.

**A review returning no findings is weak evidence, so it is worth stating what did not need changing.**
The player-facing type graph, the `IReadOnlyList` freezing, the request/report channels, and
`PlayerClaim`'s `EventId`-dropping all handle the new claim kind automatically, because it is an
ordinary `Claim` flowing through paths that were already generic over `ClaimKind`. Nothing in
`Session/`, `Trace/TraceWriter.cs`, or the Godot interface needed a change, and the Godot self-test's
choice and screen counts are byte-identical to the accepted baseline — confirmed, not assumed, since the
scenario it drives (`baseline`, seed 42) is one of the three variants where this milestone's mechanism
does not fire.

## Carried forward

Everything carried into milestone 011, plus what its self-review added, plus the instruments findings
that are still waiting on a candidate rather than a milestone.

**From this milestone:**

- **The bonus corroboration route** (Vincent asking Tommy about `UnattributedShortfall`) is unauthored
  and unscoped — a real, measured consequence of routing the suspicion through ordinary testimony
  rather than something to rely on or design further.
- **`Organization.Offices.Select(o => o.Domain).FirstOrDefault()`** is correct for a one-office
  fixture and would need a real rule, not an extension of this one, the day a second domain exists.
- **The Vincent-side conflict from stale assignment re-disclosure** is a genuine, if unplanned, second
  edge this milestone's mechanism exercises more often than before (twice in `watchful-boss`, where it
  never fired before). Salvatore's own stance on a claim his capo has personally disproven never
  revises downward on its own — nothing makes him doubt it just because it keeps getting contradicted;
  only being told again, differently, or working something out himself would move it, and neither
  currently happens for this specific belief.

**From the deferred instruments work — real findings, not speculation, still waiting:**

- **Coverage has never been run**, at `92.2%` line, `84.2%` branch, `376` uncovered lines as of
  `6a8a765`, of which `Program.cs` is 118 and legitimately so.
- **Two live edges nothing has ever run**: `Runner.cs`'s grievance-and-resentment path for observing
  somebody else's policy breach, and `Utility.cs`'s pricing of a candid report made with something at
  stake.
- **Mutation is hand-picked** and lives in scratch rather than in the repository.

**From milestone 011 and its self-review:**

- The allegation option names the same person twice, and always will — clumsy, not wrong.
- The developer trace still says "he" for everybody, 59 strings.
- `AdvanceInvestigation` reads and writes `owner` throughout.
- **The cold-trail branch is unreachable at every seed tried**; **nobody holds a scored relationship
  with Kane.**

**Longer-standing:** the tuning guesses; the cast ceiling of six; obligation read but never moved;
nothing raises trust; negative trust and decay deferred; `GrievanceWeight` uncapped; no save/load; the
empty-domain `ConcealIncident(, target=…)` label; four decisions in ninety days in the Godot demo; the
timing of a pause is observable even when the occasion is not; the player cannot see why an option is
unavailable; nothing prevents a Godot script calling `Cast.Build` directly; `AGENTS.md` mentions
neither `docs/RELATIONSHIPS.md` nor the Godot headless check.

## Commit

One implementation-and-archive commit. Status is not established by this file —
`CURRENT_MILESTONE.md` says what is active, and Matt's confirmation of a named commit is the only
thing that counts as acceptance.
