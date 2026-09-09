# Milestone 022 — The Street Talks

Chosen by Matt on 2026-09-04 from three candidates; scope reviewed and all rulings settled 2026-09-05.

## What this milestone was for

`SourceKind.Rumor` had been in the claim vocabulary since milestone 003 and **nothing produced it**,
while the entire receiving side sat built and unexercised: `Salience` discounts it hardest
(`0.45 × suspicion`), `Cognition.AttributionRank` ranks it lowest so a rumour that later becomes a
report re-attributes upward — its doc comment describes that exact case — `Provenance.IsTestimony`
includes it, and `PlayerNarration` had an arm for it.

Meanwhile the violence path filed **everything** as `SourceKind.Discovery`, sourced to the observer
himself. `Runner.Observe`'s own comment listed *"talk on the street"* among the traces being rolled
against and then stated *"there is no rumour network here."* So a man who works the same street
"came across" `PersonUsedViolence` — a claim naming who did it — as his own reading, a day later, by
proximity. That is the category bundling `Provenance.cs` exists to prevent, and it cost four things
at once: the light suspicion discount instead of the heavy one, resistance to being argued out of,
no attribution upgrade, and — the load-bearing one — **ineligibility for corroboration**, because you
can only check what you were told.

## The rulings

1. **The proximity route *becomes* rumour** rather than gaining one alongside (Matt). Layering would
   give one man two chances at one event, which `Offer`'s "better access wins" dedupe exists to prevent.
2. **The boss's better-access route is rumour too** (Matt). He is not looking; he is hearing about his
   own territory.
3. **The rumour names the executor**, at rumour provenance and low confidence, reusing the existing
   claims (Matt). This milestone therefore **does not** deliver canon's false rumour
   (`GAME_VISION.md`: "A capo may sincerely believe a false rumor") — that needs mutation, which
   `INFORMATION_AND_LEGIBILITY.md` lists as an open question. Deferred, not implied.
4. **The investigator route stays `Discovery`** (scope review, unopposed): she went looking.
5. **The wanted player-facing surface is the generic "ask X for his own account"** (Matt, during
   implementation) — see the finding below.

Canon's four requirements — *"an origin or plausible context, a transmission network, a claim, and
mutation **or** reliability rules"* — are met by the incident, proximity to the district, the existing
claims, and the reliability rules respectively. Mutation stays open, deliberately. The attribution
form is canon's own: *"a rumor attributed to a neighborhood or source."*

## What was completed

- **`Sim/ScheduledEvent.cs`** — `EventPayload` gains `AcquiredAs` and `AttributedTo`. Before this the
  scheduler could say how *likely* somebody was to find out and not *how*, which is why both routes
  collapsed to Discovery.
- **`Strategy/Strategies.cs`** — the proximity and boss routes emit `SourceKind.Rumor` attributed to
  the district; the investigator route stays `Discovery`; the owner keeps `Discovery` (below).
- **`Sim/Runner.cs`** — `Observe` honours the payload: rumour arrives as `Stance.Suspects` at `0.35`
  confidence attributed to the neighbourhood, discovery as `Stance.Believes` at `0.6` naming the
  holder. Both figures are labelled provisional and neither was tuned toward an outcome.
- **`Session/PlayerNarration.cs`** — the rumour arm becomes explicit and reads *"it is going round
  {place}"*, having been an unreachable catch-all since milestone 003.

## Two findings, both from implementation rather than review

### The man who ordered it is not learning it from the street

**My scope review missed this case entirely**, and it turned out to be the load-bearing one. Filing
the owner with the passers-by meant Vincent — who *ordered* the beating — merely suspected, from
street talk, that his own man had carried it out.

That is wrong on the definition: `SourceKind.Discovery` is *"came upon a trace or a consequence
afterwards. Explicitly implies he was not present"* — exactly a capo checking on the result of his own
order. And milestone 017's accepted ruling already said he acquires whether-it-was-carried-out
*"through a report or a discovery roll like anyone else"*; filing him as a hearer rewrote that in
passing.

**Recorded with the discovery order, because it is unflattering:** the argument above stands on its
own, but a failing test is what sent me looking. With the owner on rumour, his concealment calculus
shrank — every term is scaled by how sure he is of what he would be hiding — and `baseline` and
`watchful-boss` collapsed to **identical chosen actions**, tripping the distinctness guard that exists
so that an honest non-result cannot quietly become "nothing distinguishes anything". Four other pinned
natural-run expectations moved with it. Carving the owner out restored all five and every hash.

### Rumour made violence beliefs corroboratable, and that displaced milestone 007's attribution

A rumour is testimony. `FromDelegation` — the generator letting a delegator ask the man he sent for an
account — exists *because* the generic corroboration branch refuses beliefs you established yourself;
its header says that restriction "was quietly removing the most interesting exchange in the model."
Once violence arrives as talk, the generic branch accepts it, runs first, and wins the
`(kind, target, claim)` dedupe. Four variants stopped attributing the question to `FromDelegation`.

Reordering the generators restored it and was tested — but **Matt ruled the generic surface is the
wanted one**: *ask Tommy for his own account*, so we hear what Tommy says about it. So the reorder was
reverted and `ScenarioReachTests`' assertion was re-pointed from the generator name to the thing it
was standing in for: Vincent puts the question **to Tommy, about Tommy's own act** — which is a
stronger assertion than the one it replaced, since a generic question landing on Tommy about somebody
else's business would have passed the old check.

## The honest non-result

**No rumour is acquired in any variant at seed 42, and every trace hash and chosen-action digest is
unmoved.** The scope named this outcome in advance as acceptable and explicitly forbade raising the
discoverability until it fired.

The eligible population is nearly empty by construction: civilians were rejected by the scope review
(a civilian holding a violence rumour has no reader — `Fear` moves only through coercion resolution),
the executor is excluded, the detective goes the discovery route, and the owner is carved out. That
leaves one man in the accepted fixture — Salvatore — whose roll is `0.5 × (0.4 + 0.6 × 0.15) ≈ 0.245`,
and he fails it at seed 42.

This is the fifth finding of the form *the fixture cannot exercise this*; milestones 010 and 011
produced the earlier ones. **The honest lever is who is in earshot** — a scenario question about how
many people are on that street — not a nudged probability or a re-categorised owner.

## Verification

- Build **0 warnings, 0 errors**; tests **593 passed, 0 failed** (588 + 5 new, `StreetTalkTests.cs`).
- `--compare` at seed 42: 6 configurations, **6 distinct traces, 6 distinct chosen-action sequences**,
  every hash identical to milestone 021's — the measure of an inert mechanism rather than a claim that
  nothing changed.
- All viewpoint runs, all five Godot self-tests, and the two-process restart proof exit 0.

## Deferred

Rumour mutation and false rumours (canon's open question, and what would deliver "a capo may sincerely
believe a false rumor"); strength growing with repetition; street talk reaching civilians, which needs
rumour-to-fear to matter; media and public coverage; a social transmission graph.

## Where to look and what to distrust

**Unreviewed**, like everything since `34cd117`. The claim most worth re-deriving is the owner
carve-out: it is the difference between this milestone being inert and it destroying variant
distinctness, I found it by chasing a regression rather than by reasoning, and it happens to be the
configuration in which every test passes — which is exactly the shape of a conclusion reached for the
wrong reason. The argument from `SourceKind.Discovery`'s definition is what should be checked, not the
green suite.

## Commit

One implementation-and-archive commit.

## Correction — the production-path test gap, 2026-09-08

**Authorized narrowly, and explicitly not a redesign.** Every proof in `StreetTalkTests.cs` above
reads the *scheduled payload* directly — `Drain(world)` pulls the queued `ObservationOpportunity`
events and inspects `EventPayload.AcquiredAs`/`AttributedTo` without ever letting the simulation loop
resolve them. That pins what the scheduler decided and never proves `Runner.Observe` actually rolls
against the opportunity and actually lands the claim in the observer's own `Cognition` — the gap the
archive's own "Where to look and what to distrust" section left unaddressed.

**`The_street_talk_survives_its_complete_production_path`** stages the same real delegated-force
operation `StagedBeating()` already builds, then calls `Runner.Run` instead of draining the queue by
hand, so the queued `ObservationOpportunity` is resolved by the production loop exactly as a natural
run would resolve it. It asserts Salvatore's own `Cognition` — not the scheduler's payload — holds the
executor-naming claim as `SourceKind.Rumor`, attributed to the harbour rather than to himself; that the
owner and the investigator, read the same way, never come to hold it as talk; and that the harbour
still names nobody `Acquaintance.KnownTo` can find.

**The observation roll is a genuine Bernoulli draw, and the test says so rather than hiding it.**
Discoverability × attentiveness ≈ 0.35 × 0.49 ≈ 0.17 for Salvatore in this staged scenario — the same
figure the original archive computed for the natural fixture, differing only because the boss's
better-access route (discoverability 0.5) does not fire here: `StagedBeating()` constructs its
`Candidate`s directly rather than through the generator pipeline that would set `BreachesPolicyId`, so
`StrategyInstance.BreachedPolicyId` stays null and Salvatore — who is `Organization.BossId` — is scored
on the plain per-district-worker rate like anyone else in earshot. No character stat pushes 0.17 to
certainty without raising discoverability itself, which this correction does not touch. The occasion
key (`"obs|{owner}|{localSequence}|{ordinal}|violence|{observerId}"`) is built entirely from strategy
bookkeeping that never touches the RNG, so it is identical at every seed; only the seed moves the roll.
Seed 25 is a search over which seed lands this already-scheduled roll — found by computing
`Rng.ForOccasion(seed, key).Chance(...)` directly against the real scheduled payload for a range of
seeds, then confirmed by running the real loop — not a search over the mechanism, and it leaves seed
42's own honest non-result, and its accepted state, untouched.

**A property worth recording though out of scope to act on:** Salvatore's, Vincent's, and Kane's rolls
on this same event never landed together, in any pairing, across several thousand seeds searched —
individually each fired at its expected rate (~17%, ~16%, ~49%), but the joint case was not found. The
three occasion keys differ only in their trailing observer id, and `Rng.ForOccasion`'s xorshift32
stream is known to correlate poorly across closely related seeds on its first draw, which is what each
of these rolls consumes. This is not a defect this milestone's design depends on being independent —
nothing here claims three people's chances of noticing the same event are drawn independently of each
other — and changing the RNG scheme is explicitly out of this correction's authorization. Recorded so a
future reader does not re-derive it as a surprise, and so nobody assumes a positive joint demonstration
was simply not searched for hard enough.

**Preserved exactly:** `Runner.Observe` and the scheduler's route selection in `Strategies.cs` are
byte-for-byte unchanged — confirmed by `git diff` against `src/` being empty for this commit, not
merely asserted. Both required mutations were run and reverted: hardcoding `Runner.Observe` to
`SourceKind.Discovery`/the observer failed the new test while the other five `StreetTalkTests` (which
never resolve the loop) kept passing; reverting the scheduler's street route to `SourceKind.Discovery`
unconditionally failed the new test alongside the two pre-existing scheduling-level tests it shares the
claim with.

**Verification.** Build 0 warnings / 0 errors on both target frameworks; `src/` diff empty. Tests
**660** (659 + 1). `--verify` on all four required configurations, byte-identical to every prior
accepted figure — `baseline` `83D59F6D099B840A`, `disloyal-vincent` `33F3C92F3DB9250C`,
`resentful-tommy` `2899736537AF3BE3`, `capable-angelo` `34E6AF60C2673B95`. `--compare` at seed 42: 6
configurations, 6 distinct traces, 6 distinct chosen-action sequences, every digest unmoved — seed 42's
own honest non-result stands exactly as this milestone recorded it. Both required viewpoint runs and
all seven Godot invocations exit 0. Two mutation checks, each confirmed and reverted.

### Commit

One correction commit, test-only. Still unreviewed, like everything this milestone has carried since
`34cd117` — this correction has not been back to Codex.
