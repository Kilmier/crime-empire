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

## Correction — `Rng.ForOccasion`'s finalizer, 2026-09-09

**The property the previous correction recorded "though out of scope to act on" — Salvatore's,
Vincent's and Kane's rolls on the same event never landing together across several thousand searched
seeds — was not a correlation quirk of xorshift32's first draw. It was a proof: under the old
finalizer, no two occasion keys' streams could ever be made to land together, at any seed, for any
pair of keys, related or not.** `Rng.ForOccasion` hashed its key with FNV-1a and combined it with the
world seed by XOR alone, then finalized with a single linear step, `h ^= h >> 15`. XOR and shift are
both linear over GF(2), and so is FNV-1a's own hash step; the pipeline end to end was GF(2)-linear.
That meant for any two occasion keys under one seed, the seed's own contribution cancelled out of
their XOR difference algebraically, leaving a fixed, seed-independent delta between the two streams'
entire output sequences — no seed could ever change it. Three street-talk observers of the same event
were found permanently unable to co-succeed not because the odds were long, but because the algebra
forbade it.

**The fix.** `ForOccasion`'s finalizer is now fmix32 (MurmurHash3's finalizer): multiplication by an
odd constant is not linear over GF(2), which breaks the cancellation the defect depended on. Nothing
about a key, a seed, or what a caller does with the resulting stream changed — a given seed and
occasion key still produce one fixed, reproducible stream, and an occasion key still carries no global
scheduling identifier, so unrelated event insertion still cannot reroll it. Only the *relationship*
between two distinct keys' streams is no longer forced into an unbreakable pattern; they are free to
land the same way, including all succeeding together, at some seed. See `Rng.cs`'s doc comment on
`ForOccasion` for the complete algebraic argument and `docs/DESIGN_DECISIONS.md`'s "Keyed stochastic
opportunities can co-succeed" for the durable rule this establishes project-wide. `Rng.ForDecision` has
the identical linear shape and is confirmed to have the identical defect, but is deliberately left
unfixed — see `OPEN_CONCERNS.md` #6.

**Authorization and scope.** Two read-only investigations preceded this correction: the first
reproduced the joint-exclusion defect directly against production `Rng.ForOccasion` over a declared
seed range and inventoried every production caller; the second traced, at the seed this correction
settled on, exactly how the corrected mixer changes what each variant's `--compare` run does, and
confirmed the changes are honest consequences of decoupling the streams rather than a new defect —
neither altered any repository file. Matt then authorized this bounded correction: replace
`ForOccasion`'s finalizer only; touch no other RNG method, no occasion-key construction, no
probability, trait, fixture, or scheduling code; strengthen `StreetTalkTests.cs`; and, since the fix's
blast radius turned out to reach seven files the original authorization did not name (below), a
follow-up authorization covering their repair specifically, under the same production-code
prohibition and the same falsification standard.

### `StreetTalkTests.cs` — the milestone's own tests, strengthened

Four things item 6 of the authorization asked for, all delivered in `StreetTalkTests.cs`:

- **`Three_observers_of_the_same_event_can_succeed_together_at_a_deterministic_seed`** — the bounded
  existence proof, kept deliberately thin: at seed 222, through the real production loop
  (`StagedBeating` + `Runner.Run`, nothing hand-rolled), Salvatore, Vincent and Kane all come to hold
  something about the one event under test — the exact joint outcome the old finalizer made
  structurally impossible. Mutation-checked: reverting the finalizer to the old single linear step and
  re-running at this identical seed fails it (confirmed directly, then restored).
- **`The_street_talk_survives_its_complete_production_path`** — moved from seed 25 (a pre-fix search
  result, where at most one of the three could ever land) to the same seed 222, and its two boundary
  assertions strengthened from guards (`is null or Discovery`) to positives: Salvatore's `Cognition`
  holds the executor's name as `SourceKind.Rumor`, attributed to the harbour, in the same real run
  where Vincent's `Cognition` holds it as `SourceKind.Discovery` (self-sourced) and Kane's holds the
  witness claim the same way — not "never as talk," but "as Discovery, specifically, together."
- **`Identical_seed_and_occasion_key_reproduce_the_same_observation_outcomes`** — two independently
  built worlds at the same seed reach byte-identical Stance/SourceKind/Confidence for all three
  observers, proving the corrected mixer is still a pure, reproducible function of (seed, key).
- **`Unrelated_event_insertion_does_not_reroll_an_observation`** — a causally unrelated `RoleReview`
  scheduled ahead of everything else (confirmed to actually perturb the run: Nunzio gets a real
  decision in the disturbed run and none in the undisturbed one) leaves all three observers' outcomes
  unchanged, since the occasion key never reads global scheduling state.

`StreetTalkTests.cs` now has **9 tests** (6 before this correction + 3 new; `Proximity_is_scheduled_
as_rumour_and_investigation_as_discovery`, `The_man_who_ordered_it_is_not_learning_it_from_the_street`,
and the three earlier-milestone tests are all untouched).

### The seed-42 honest non-result — retired, not merely moved

**"No rumour is acquired in any variant at seed 42" is now false, in every variant where the fixture
produces a violence incident at all.** Under the corrected mixer, at seed 42, Salvatore's rumour roll
lands in `baseline`, `watchful-boss`, `disloyal-vincent`, `resentful-tommy`, and `capable-angelo` —
every variant with an incident to hear about. (`cautious-vincent` has none: Vincent's traits there
never escalate the grocery operation to force, so there is nothing for anyone to hear, exactly as
before.) This is not the fixture gaining eligible listeners or the discoverability coefficient moving
— `0.5 × (0.4 + 0.6 × 0.15) ≈ 0.245` is untouched — it is the same roll, at the same seed, no longer
locked to the outcome three other keys' streams happened to force it into under the old finalizer.

**Vincent's and Kane's own rolls on the identical event still fail in every variant at seed 42** — the
corrected mixer does not raise anyone's odds, it only frees three independent draws to land however
they independently land, and at this particular seed they land Salvatore-only. The owner never comes
to hold his own man's act as Discovery, and the detective never opens a case, at seed 42, in any
variant. This is the new honest non-result for those two routes specifically, and — per the standing
practice this correction follows throughout — it retired several other tests' seed-42 history rather
than the milestone's own mechanism (see "Tests repaired in other files," below).

### Tests repaired in other files

Running the full suite after the fix surfaced 24 failures across seven files, not the one file this
correction's own scope named — every natural-run test that happened to read Vincent's or Kane's own
observation roll on this same event. Each was traced to the corrected mixer before being touched, per
the four-way classification the follow-up authorization required (natural-history assertion / mechanic
proof / cascaded snapshot / unexpected defect); every one classified as one of the first three. None
classified as an unexpected defect, so no further stop-and-report was needed and this correction
proceeds to commit per that authorization's own terms.

- **`ScenarioReachTests.cs`** — `The_delegator_puts_his_question_to_the_man_he_sent` (4 variants) and
  `And_the_executor_gives_his_delegator_an_account_of_it` moved to seed 199
  (`AltSeedWhereVincentAsksTommy`, found by search over the unmodified production scenario — the
  first seed at which Vincent's own discovery roll lands in all four variants together), since both
  read that exact request. `The_conflict_changes_what_a_later_decision_is_scored_on` moved to the same
  seed for a subtler reason, traced component-by-component rather than assumed: at seed 42 the first
  `ReportToSuperior`-to-Salvatore candidate scored after the relevant conflict is now a deceptive one
  ("tell salvatore it did not happen"), whose relationship math genuinely inverts direction (a lie
  against a less-trusted man costs less, not more, so lower trust from being contradicted makes it
  look relatively cheaper) — a correctly-computed answer to a different question than the test asks,
  not a defect. `Resentment_now_reaches_a_chosen_action_at_seed_42` is renamed
  `Resentment_no_longer_reaches_a_chosen_action_at_seed_42` and now asserts the retraction directly,
  per explicit instruction not to relocate a seed-42-named claim to another seed: `baseline` and
  `resentful-tommy` are now byte-identical in their chosen-action sequences at seed 42 (confirmed
  officially via `--compare`, not only via the test) — the same already-traced convergence Matt
  accepted as honest when authorizing this correction.
- **`CausalFeedbackTests.cs`** — the five "pending vs. declined" tests all read Tommy's own natural
  first controlled pause answering Vincent's direct question; moved to seed 199 for the identical
  reason as `ScenarioReachTests` above (confirmed: at that seed, Tommy's first pause is exactly that
  question, with the identical option wording these tests already assert against).
- **`ControlledAutonomousParityTests.cs`** — `Tommys_asked_to_account_decision_resolves_automatically_
  to_the_identical_partial_report` hardcodes `RecipientId == "vincent"`, specifically because it is
  about milestone 019's cited repro (Vincent asking); moved to seed 199. The other four tests in the
  file, including the comprehensive-fingerprint sweep across every variant and character, read
  whichever decision an actor actually reaches rather than assuming who asks, so they are unaffected
  and remain at seed 42.
- **`InvestigationTests.cs`** — `An_investigator_who_has_named_a_suspect_puts_it_to_him` (4 variants),
  `The_suspect_answers_the_detective` (3 variants), and `A_player_controlling_the_investigator_is_
  offered_the_allegation` all read Kane's own natural investigation reaching a named suspect; moved to
  seed 199 (`AltSeedWhereKaneNamesASuspect` — the same seed as above, since it also happens to be the
  first at which Kane's own roll lands in every variant). The file's 23 staged tests, which never run
  a natural loop, are untouched.
- **`PronounTests.cs`** — `A_pending_decision_speaks_of_its_actor_as_themselves` needs Kane to reach
  at least one decision of her own by day 90 when controlled from the start; moved to seed 199 for the
  identical reason. The file's other four tests, including the five-variant natural sweep
  `Every_viewpoint_is_described_as_themselves`, do not depend on Kane reaching a decision and are
  unaffected.
- **`RelationalConsequenceTests.cs`** — `The_scenario_produces_the_expected_number_of_conflicts`
  (`watchful-boss`) moves from 3 to 2, traced directly rather than assumed: the redistributed
  observation outcomes shift the timing of the events upstream of this count (Salvatore's own conflict
  now lands 1 April rather than 5; the second of two "Vincent hears Salvatore reassert" conflicts no
  longer fires), a redistribution of the same kind milestone 012's own archive already describes for
  this exact budget, driven by a different upstream cause. `cautious-vincent` (3), `baseline` (2),
  `disloyal-vincent` (2) and `resentful-tommy` (2) are unchanged; this file stays at seed 42
  throughout, since its claim is specifically about seed 42's own conflict count, not a capability.

**One seed, not several, wherever the same underlying fact was the reason.** `AltSeedWhereVincentAsks
Tommy` and `AltSeedWhereKaneNamesASuspect` are both 199, found once by search over the unmodified
production scenario (`Cast.Build`/`Runner.Run`, nothing staged) for the first seed satisfying both
Vincent's and Kane's own discovery rolls landing together across every affected variant, then reused
by name in each file rather than re-derived — the smallest local repair each test needed, not a shared
cross-file helper.

### The 27-versus-29 decision-count reconciliation

An earlier read-only causal-trace report estimated `baseline` and `resentful-tommy` converging to
"exactly 29 decisions each," using a scratch dump script that also emitted two `RUMOR holder=…`
annotation lines per file (for Salvatore's two rumour-sourced claims) alongside the real `DEC[...]`
lines, miscounted together by a naive line count. **The correct figure, confirmed against the final
production run via the accepted `--compare` tool rather than a scratch script, is 27 decisions for
both `baseline` and `resentful-tommy`** — matching the more precise `grep -c "^DEC"` check the same
report also ran and did not reconcile against its own headline figure at the time. Recorded here as
the authoritative reconciliation.

### Hashes, digests, and decision counts — the final production run

`--verify --seed 42 --days 90`, each configuration run twice and compared, all deterministic:

| variant | decisions | trace hash | chosen-action hash |
|---|---|---|---|
| `baseline` | 27 | `7832105EC1F24154` | `B52F36558276F8C5` |
| `cautious-vincent` | 26 | `C957771688740AFE` | `735B8AB6B9E4421D` |
| `watchful-boss` | 27 | `99D55F9A48DB059D` | `FABF7B1D6130B250` |
| `disloyal-vincent` | 29 | `6C23284BFD91C48D` | `EC0FD121F65CD24F` |
| `resentful-tommy` | 27 | `7A43D1AFB4A6E26F` | `B52F36558276F8C5` |
| `capable-angelo` | 32 | `5CACCFC566364BB7` | `FC7865347105D92F` |

`--compare --seed 42`: **6 configurations, 6 distinct traces, 5 distinct chosen-action sequences** —
`baseline` and `resentful-tommy` share `B52F36558276F8C5`, confirmed identical rather than merely
close. Every hash above is necessarily different from every previously accepted figure, in every
milestone's archive: the finalizer change touches every `ForOccasion`-derived roll in the simulation,
so a changed hash is the expected signature of this correction, not a red flag to chase.

### Verification

- Build **0 warnings, 0 errors**, both target frameworks.
- `git diff --stat` against `src/`: only `src/CrimeEmpire.Simulation/Sim/Rng.cs` — no other production
  file touched, confirmed rather than merely intended.
- Tests: **663 passed, 0 failed** (660 + 3 new in `StreetTalkTests.cs`). Every one of the 24 tests
  failing immediately after the fix now passes, having been individually traced and classified above.
- `--verify` on all four required configurations (table above) — deterministic on every one.
- `--compare` at seed 42 — table above; the honest non-result the milestone originally recorded is
  retired for Salvatore's route and holds in its new shape for Vincent's and Kane's.
- Both required viewpoint runs (`--variant disloyal-vincent --viewpoint salvatore --seed 42 --days 90`,
  `--variant baseline --viewpoint vincent --seed 42 --days 90`) exit 0.
- Godot: `--selftest`, `--selftest-goldenpath`, `--selftest-corroboration`, `--selftest-tribute`, and
  the two-process restart proof all exit 0 unchanged. `--selftest-directaction` — the direct-action
  fork, where Vincent personally executes rather than delegating — needed its own scripted choice
  sequence re-derived for the identical underlying reason as the C# tests above; see its own note
  below.
- Two required mutation checks on the new `StreetTalkTests.cs` proofs, each run and reverted: the
  bounded-existence and complete-production-path tests both fail under the restored old linear
  finalizer, confirming they exercise the fix rather than passing by construction.

### Commit

One correction commit. Still unreviewed pending a Codex round on this correction specifically.
