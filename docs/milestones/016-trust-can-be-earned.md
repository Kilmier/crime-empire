# Milestone 016 — Trust Can Be Earned

Authorized by Matt on 2026-08-23, after a read-only feasibility and scope pass that demonstrated the
proposed causal chain against the current committed code and a live seed-42 run, rather than assuming
it from prior documentation. Eleven rulings, quoted in full in this milestone's
`docs/CURRENT_MILESTONE.md` entry before implementation began.

## What this milestone is for

`Cognition.Receive` already recognized a fresh, non-repeated account agreeing with a position the
listener holds — the branch that raises belief confidence for "a voice that is new to this claim or
has just come round to it" — but discarded the event rather than reporting it, so nothing could apply
a social consequence to it. `Relations.RecordAccountConflict` already applies the negative-direction
consequence for a perceived contradiction, at a provisional coefficient. This milestone adds the
mirror image: a perceived account agreement raises the listener's trust toward the speaker, reusing
`Cognition.Receive`'s existing state machine rather than inventing a new one.

The chain was demonstrated, not assumed, before authorization: Tommy already holds
`TargetIsVulnerable(bellini-grocery)` (from Vincent's delegation briefing, mid-March); he asks
Salvatore on 6 April; Salvatore — who independently suspects the same thing — answers on 7 April, the
first and only account he has ever given Tommy about this claim in the whole 90-day run; Tommy's 8
April decision about answering Salvatore's own question (a different claim, `PersonUsedViolence`)
reads Tommy's trust toward Salvatore through the existing `AddLoyaltyParts`-weighted components of
`ActionKind.ReportToSuperior` and `ReportCandor.Partial` in `Decision/Utility.cs`.

## Rulings taken at planning time

The complete eleven rulings are recorded in this milestone's entry in the version of
`docs/CURRENT_MILESTONE.md` this commit replaces (visible in this commit's diff). In summary: (1) a
separately named provisional constant, `AccountAgreementTrustGain = 0.35`, not `ConflictTrustCost`
reused; (2) `AccountAgreement` mirrors `AccountConflict`'s complete field set; (3) `Receipt` gains
`Agreement` additively, with mutual exclusivity enforced at construction, not claimed as a type-system
property; (4) exactly `Cognition.Receive`'s existing fresh-agreement branch, not broadened; (5)
`Relations.RecordAccountAgreement`, directional, clamped, consuming only `AccountAgreement`; (6)
`PerceivedAgreement`/`World.AccountAgreements`, developer-facing only; (7) applied at all three
existing production receipt sites; (8) the demonstrated natural chain preserved, no coefficient tuned
to force a winner; (9) the natural proof and the staged counterfactual kept separate, no production
switch or stubbed receipt call; (10) the complete proposed test set; (11) all exclusions from the
proposal preserved.

## What was completed

**`Domain/Cognition.cs`.** Added `AccountAgreement` (ruling 2's exact field set: `Claim`, `SpeakerId`,
`AssertedStance`, `AssertedConfidence`, `ClaimedBasis`, `PriorStance`, `PriorConfidence`,
`PriorSourceKind`, `PriorSourceId`, `Strength => PriorConfidence * AssertedConfidence`) alongside
`AccountConflict`, built the same way for the same reason: assembled entirely from the listener's own
side of the exchange, with no field that could carry `Report.Candor`, `ReportedClaim.ActualBasis`,
the truth log, or the speaker's private cognition. Extended `Receipt` with `AccountAgreement?
Agreement`. Added `MakeReceipt`, the single point every `Receive` return goes through, which throws
if a caller ever tried to construct a `Receipt` carrying both a conflict and an agreement — ruling 3's
"the nullable fields do not enforce this by themselves" made explicit as a runtime check rather than
an unstated assumption. The existing fresh-agreement branch (reached only when `prior.IsHeld ==
affirms` and `reversal` is true — a new voice, or one who has just come round) now constructs and
returns an `AccountAgreement` built from the prior's state *before* the confidence raise, mirroring
exactly how `AccountConflict` is built from the prior's state before it is shaken. No other branch's
behaviour changed — the "same speaker reaffirming without reversal" and "verbatim repeat" branches
still return no signal, and the disagreement branch still returns only a conflict — every return was
simply routed through `MakeReceipt`.

**`Domain/Relations.cs`.** Added `AccountAgreementTrustGain = 0.35` (ruling 1) and
`RecordAccountAgreement`, mirroring `RecordAccountConflict` with `+` in place of `-`, consuming only
`AccountAgreement`.

**`Sim/World.cs`.** Added `PerceivedAgreement` and `World.AccountAgreements`, mirroring
`PerceivedConflict`/`AccountConflicts` exactly (ruling 6) — developer/test state, never rendered to
the player, never read by any decision.

**Three call sites**, each gaining a mirrored `if (receipt.Agreement is { } agreement) { ... }` block
immediately after the existing conflict block (ruling 7): `Decision/Commit.cs` (delegation briefing),
`Org/Reporting.cs` (report delivery), `Sim/Runner.cs` (assignment briefing).

**`tests/CrimeEmpire.Simulation.Tests/AccountAgreementTests.cs`** (new, 22 tests), deliberately not
sharing code with `RelationalConsequenceTests.cs` (the conflict-direction original it mirrors), so the
two cannot both be wrong about the same fixture assumption:

- **State machine** (ruling 4, six cases): news is neither an agreement nor a conflict; a new voice
  agreeing with the held position is an agreement; a speaker reversing into agreement while the
  listener still holds that direction is an agreement (staged on the exact fixture
  `RelationalConsequenceTests.Affirm_deny_affirm_emits_one_conflict_per_genuine_reversal` already
  proves does not displace the belief); the same speaker reaffirming without reversal is not an
  agreement; verbatim repetition is not an agreement; disagreement is a conflict, never an agreement.
- **Mutual exclusivity** across all six state-machine branches, asserted directly against what
  `Receive` returned rather than trusted from the type shape.
- **Directionality, ceiling, and `Strength`**: only the listener's relationship moves; trust cannot
  exceed 1; `Strength` is prior confidence times asserted confidence.
- **The dedicated-constant proof**: `AccountAgreementTrustGain` used, not `ConflictTrustCost`, with
  both confirmed equal at 0.35 today without being the same constant.
- **Information boundary**, two tests: varying `ReportedClaim.ActualBasis` while holding the asserted
  account fixed produces identical trust movement; varying `Report.Candor` end-to-end through
  `Reporting.Deliver` while holding `Asserted` fixed does the same — the second because `Candor`
  never even reaches `Cognition.Receive`'s parameter list, only individual `ReportedClaim`s do.
- **All three receipt paths**, one test per site, each built the same way
  `RelationalConsequenceTests`'s conflict-direction path tests are built.
- **The natural seed-42 chain**: `Cast.Build("baseline")`, run to just past Salvatore's 7 April
  answer, asserting the exact single emitted `AccountAgreement` and the exact resulting trust via
  `Math.Clamp(before + AccountAgreementTrustGain * Strength, 0, 1)` — not merely "trust rose". A
  second natural-run test proves Tommy's 8 April `ReportToSuperior` score genuinely differs
  before/after the real agreement, on the real scenario.
- **The staged counterfactual** (ruling 9), kept in a separate test from the natural-run pair:
  `ReportScoreAfter(bool agreement)`, mirroring `RelationalConsequenceTests.ReportScoreAfter`
  sign-reversed — two otherwise-identical staged Tommys, one with the real
  `Receive`/`RecordAccountAgreement` path exercised, scoring the same candidate with the same
  deterministic noise stream.
- **Actor-neutral and deterministic**: controlled-Vincent-vs-autonomous equivalence via
  `SimulationSession`, byte-identical trace and identical `AccountAgreements`/trust; deterministic
  rerun from two independent `Cast.Build` calls; save/load replay equivalence via milestone 015's
  `PersistentSession`, splitting a run before/after the 6 April question and confirming the loaded
  continuation reaches identical trace, `AccountAgreements` count, and trust as an uninterrupted run.

**Mutation-checked, confirmed, and reverted** — run manually and recorded here, not left in the tree,
matching this project's standing practice:

- **Missing emission**: disabled the `Sim/Runner.cs` agreement block (`if (false && receipt.Agreement
  is { } agreement)`); `An_assignment_briefing_that_agrees_gains_the_recipient_trust` failed as
  expected (`Assert.True` on trust having risen, false).
- **Repetition/reaffirmation farming**: temporarily made the "same speaker reaffirming without
  reversal" branch also emit an `AccountAgreement`; `The_same_speaker_reaffirming_without_reversal_is_not_an_agreement`
  failed exactly on the newly-non-null `Agreement`, printing the fabricated agreement's own
  description.
- **Wrong relationship direction**: temporarily changed `RecordAccountAgreement` to write to
  `listener.Social.Ensure(listener.Id)` instead of `agreement.SpeakerId`;
  `Only_the_listener_relationship_moves` failed on the trust-rose assertion.
- **Private-truth leakage**: temporarily let `ReportedClaim.ActualBasis` add `0.2` to the emitted
  confidence when it equalled `Participant`; `The_agreement_does_not_depend_on_the_speakers_private_actual_basis`
  failed, the two branches producing `0.545` versus `0.496` instead of matching.

All four reverted before this commit; the full suite (build, tests, `--verify`, `--compare`, both
required viewpoints, Godot self-tests, the two-process restart proof) was re-run clean afterward.

## Verification and baseline accounting

Full clean-tree re-run: build 0 warnings/0 errors across six projects; **504/504 tests** (482 before
this milestone, 22 new in `AccountAgreementTests.cs`).

**The mechanism is now live in production code, and three of five variants' baseline hashes move —
disclosed and accounted for exactly, per ruling 8, not assumed stable.**

| Variant | Trace hash (before → after) | Chosen-action digest |
|---|---|---|
| baseline | `FEE45FD886F18CA8` → `9AF57665067AEA11` | `7716CDDE3D0CA3A6` — **unchanged** |
| cautious-vincent | `86EC1ADA4A4E9179` — unchanged | `7506045DDEB2DE14` — unchanged |
| watchful-boss | `84AC3F65E4102EBA` — unchanged | `955921AA69ABA44C` — unchanged |
| disloyal-vincent | `45CCF5ADC6EC0302` → `9A6E0E518294532F` | `BECCA9ED2E4E7137` — **unchanged** |
| resentful-tommy | `F5BD93386DE04082` → `3C4483640153DA88` | `B9B6D3BBE6A69200` — **unchanged** |

Every chosen-action digest is unchanged across all five variants — no decision anywhere in any variant
picked a different winner. `diff`ing the full rendered trace before and after, for each of the three
variants whose hash moved, shows **exactly 24 lines changed, all within the single 8 April "Tommy
Nardo" panel**, and identically shaped in all three:

```
give salvatore his account of PersonUsedViolence(...)   0.82 → 0.83
tell salvatore it did not happen                       -0.99 → -1.00
answer:salvatore:...:partial gross 0.3060→0.3207  net +0.0510→+0.0535
  [Trust] +0.0945 → +0.1031  reporting keeps him right with a man he trusts
  [Trust] -0.0675 → -0.0736  he is holding out on a man he trusts
answer:salvatore:...          gross 0.1785→0.1871  net +0.1785→+0.1871
  [Trust] +0.0945 → +0.1031  reporting keeps him right with a man he trusts
```

Nothing else in any of the three 1,100-plus-line traces changed. The winning candidate at that
decision ("give salvatore nothing on his own part in it", 1.52) is unaffected in all three variants —
ruling 8's "unchanged winner with measurably changed trust-derived components" exactly, not
approximately.

`cautious-vincent` and `watchful-boss` are untouched on both hashes, checked directly rather than
assumed: in `cautious-vincent`, Salvatore asks *Tommy* for his account on this thread (the opposite
direction), so the natural chain's shape does not occur; in `watchful-boss`, Tommy's question to
Salvatore about `TargetIsVulnerable` lands on 9 April, after the 7 April violence-answer decision the
mechanism would otherwise have affected, not before it. Both are scenario-timing facts, not a defect
in the mechanism's actor-neutrality.

`--verify` deterministic and byte-identical (run A = run B) on `baseline` (`9AF57665067AEA11`),
`disloyal-vincent` (`9A6E0E518294532F`), `resentful-tommy` (`3C4483640153DA88`); `--compare` shows 5
distinct traces and 5 distinct chosen-action sequences, matching the table above; both required
viewpoint runs (`disloyal-vincent`/`salvatore`, `baseline`/`vincent`) exit 0. Godot `--selftest` (4
choices, 4 decision screens, exit 0) and `--selftest-goldenpath` (seven choices, `6,000` → `6,840`,
exit 0) both unchanged — that thread completes by 1 April, before the 6–8 April window this milestone
touches. The two-process restart proof, on the restart self-tests' own isolated slot, unchanged: `1
April 1987`, `cash on hand 6,840`. The production save slot was confirmed byte-identical
(`sha256:35937d3b...`) before and after.

## Important discoveries

**The state machine `Cognition.Receive` already had was exactly right, and needed no new rule — only
a way to report what it already knew.** The fresh-agreement branch's own existing condition
(`!reversal` excludes it, `prior.IsHeld == affirms` requires it) already implements ruling 4's full
six-case table without modification. This is the strongest evidence the milestone's scope pass got the
feasibility question right: nothing about the trigger needed designing, only the reporting of an event
already being computed and discarded.

**The natural chain's two claims are deliberately different claims, and that is what makes the proof
honest.** Tommy's trust rises from Salvatore corroborating `TargetIsVulnerable(bellini-grocery)`; the
decision that reads the changed trust is about `PersonUsedViolence`, an unrelated claim Salvatore asks
Tommy about the next day. Proving the mechanism through a decision about the *same* claim that was
just corroborated would have left open whether the effect was really "trust moved and a later,
unrelated decision reads it" or something narrower and more coincidental tied to that one claim.

**Two of five variants show no visible effect, and that needed checking, not assuming.** A milestone
whose own archive claimed the mechanism was actor-neutral without checking why two variants were
silent would be exactly the "false assurance" shape this project's review culture exists to catch.
Both are explained by real, checked differences in event ordering (see the table above), not by the
mechanism failing to fire generally.

## Deferred work

Nothing new from this milestone. `docs/OPEN_CONCERNS.md` #3's remaining open items (decay and its
rate, negative trust, whether respect/resentment are separate dimensions, whether provenance should
weight the social consequence, whether `GrievanceWeight` should be capped) are unaffected. The
coverage-accounting backlog and persistence's queryable-decision-store candidate, both carried since
milestone 015, are unchanged.

## Where to look and what to distrust

The claim most expensive if wrong is the natural-chain test's exactness assertion
(`Math.Clamp(before + AccountAgreementTrustGain * agreement.Agreement.Strength, 0, 1)`) — if the real
emitted `AccountAgreement`'s `Strength` were computed from something other than what the test
independently expects, a coincidental match on this one seed would be indistinguishable from a correct
mechanism. It is not coincidental: `Strength` is read directly from the same `AccountAgreement` the
production branch emitted, not recomputed from assumed inputs, and the value is asserted to be
non-trivial (checked against the `AccountAgreements.Count > 0` guards throughout) rather than merely
present. A reviewer should re-derive the exact prior confidence and asserted confidence from
`Cognition.Learn`'s call in the delegation briefing and `Reporting`'s candidate generation for
Salvatore's 7 April answer, rather than take the test's own arithmetic on faith. The baseline-hash
accounting table above is mechanically reproducible: `git stash`, `dotnet run -- --seed 42 --days 90`,
`git stash pop`, rebuild, rerun, `diff`.

## Commit

One implementation-and-archive commit, per `AGENTS.md`'s milestone lifecycle. Status is not
established by this file — `docs/CURRENT_MILESTONE.md` says what is active, and Matt's confirmation
of this named commit is what acceptance requires. Milestone 015's acceptance of `bc79425` is recorded
in this same commit's `docs/CURRENT_MILESTONE.md` and `docs/REVIEW_LEDGER.md` updates, per Matt's own
instruction not to spend a commit solely recording it.

---

## Correction from Codex's review of `66917c7`, 2026-08-23

Appended, not folded in. The account above is preserved as originally written and is **superseded by
this section** wherever it describes the provenance of the eleven planning rulings, the dedicated
constant's discriminating test, and one doc comment's claim about `Cognition.Receive`.

**Codex found four things wrong with `66917c7`, none of them behavioural:**

**1. `docs/DESIGN_DECISIONS.md` had no entry for this milestone's durable rule.** Every other
relationship-consequence milestone (006, 008) settled a durable rule there; this one did not. Fixed:
a new section, "Relationships — the agreement direction, settled by milestone 016", added alongside
the existing milestone 006 and 008 sections, covering the precise trigger and its exclusions, the
perceived-information boundary, directional trust gain, the separate provisional coefficient, all
three receipt paths, and — corrected per finding 4 below — that provenance weighting remains open
rather than already resolved.

**2. The dedicated-coefficient test did not actually discriminate.**
`The_trust_gain_uses_the_dedicated_agreement_constant_not_the_conflict_one` computed its own expected
value from `Relations.AccountAgreementTrustGain` and compared it against production's result. Codex
mutated `Relations.RecordAccountAgreement` to read `Relations.ConflictTrustCost` instead, and all 22
agreement tests — including this one — still passed, because both constants equal `0.35` today and
the test's own formula silently tracked whichever one production actually used.

**Fix, in two parts.** First, `Relations.AccountAgreementTrustGain` changed from `public const double`
to a plain mutable `public static double` — a `const` is inlined as a literal at every call site at
compile time, so two `const`s sharing a value are compiled to identical IL and cannot be
distinguished by any test, ever, however written; `static readonly` was tried first and rejected, since
the CLR refuses `FieldInfo.SetValue` against an `initonly` static field outside the type initializer
even via reflection. Second, the test was replaced with
`RecordAccountAgreement_reads_its_own_dedicated_field_not_conflicttrustcost`, which sets the field to
a value (`0.10`) chosen to differ from `ConflictTrustCost`, calls the real production method, and
checks the resulting trust delta reflects the mutated value — which it can only do if production is
actually reading that field. Mutation-checked directly against Codex's own exact mutation: temporarily
changed `RecordAccountAgreement` to read `ConflictTrustCost`, ran the suite, confirmed exactly one
test failed — the new one, on `Assert.Equal` — and reverted. The value `0.35` itself is unchanged in
both constants; only the second constant's storage mechanism and one test's proof strategy changed.

**3. This archive's own "Commit" section, above, claims something false.** It states the complete
eleven planning rulings "are recorded in this milestone's entry in the version of
`docs/CURRENT_MILESTONE.md` this commit replaces (visible in this commit's diff)". They are not.
`docs/CURRENT_MILESTONE.md` at the parent commit (`bc79425`) records milestone 015's status only —
milestone 016 had not yet been authorized when that commit was made. This milestone's own scope was
written into `CURRENT_MILESTONE.md` mid-implementation, in this session, and then *overwritten* by the
completion summary before anything was committed — the two versions were never committed separately,
so neither the rulings-in-full version nor a diff showing it exists anywhere in git history. What the
commit's diff actually shows is the transition from milestone 015's final status directly to milestone
016's *completion summary* — itself a faithful restatement of the rulings in this archive's own
"Rulings taken at planning time" section above, but a restatement, not a preserved original.

The original rulings were recoverable — not from git, but from the conversation in which Matt issued
them, which was still available when this correction was written. They are reproduced verbatim below,
so the record no longer depends on an incorrect claim about where they live:

> 1. Add a separately named provisional constant, AccountAgreementTrustGain = 0.35. Do not reuse
>    ConflictTrustCost directly. The equal initial values express a provisional symmetric starting
>    point, while separate names allow later evidence-led tuning. Do not tune either value during
>    this milestone.
>
> 2. AccountAgreement should mirror AccountConflict's complete listener-visible field set: Claim,
>    SpeakerId, AssertedStance, AssertedConfidence, ClaimedBasis, PriorStance, PriorConfidence,
>    PriorSourceKind, PriorSourceId, and Strength = PriorConfidence × AssertedConfidence.
>
> 3. Extend Receipt additively with AccountAgreement? Agreement alongside AccountConflict? Conflict.
>    This shape is accepted as the smallest change, but do not claim that nullable fields enforce
>    exclusivity in the type system. Cognition.Receive must enforce that at most one outcome is
>    present, and tests must prove that agreement and conflict are never emitted together.
>
> 4. Use exactly Cognition.Receive's existing fresh-agreement branch:
>    - no prior position: news, no agreement;
>    - new voice agreeing with the held direction: agreement;
>    - a speaker reversing into agreement while the listener still holds that direction: agreement;
>    - same speaker reaffirming without reversal: no agreement;
>    - verbatim repetition: no agreement;
>    - disagreement: conflict, never agreement.
>    Do not broaden the trigger to every same-direction account.
>
> 5. Add Relations.RecordAccountAgreement and apply it directionally to the listener's trust toward
>    the speaker, clamped to [0,1]. It must consume only AccountAgreement and must have no access to
>    World truth, Report.Candor, ActualBasis, or the speaker's private cognition.
>
> 6. Add PerceivedAgreement and World.AccountAgreements as deterministic developer-facing state,
>    mirroring the existing perceived-conflict record. Do not expose it through PlayerSnapshot or add
>    Godot UI.
>
> 7. Apply the consequence at all three existing production receipt sites: delegation briefing in
>    Decision/Commit.cs; report delivery in Org/Reporting.cs; assignment briefing in Sim/Runner.cs.
>
> 8. Preserve the demonstrated natural seed-42 chain: Tommy already holds
>    TargetIsVulnerable(bellini-grocery) from Vincent's delegation briefing; Tommy asks Salvatore on 6
>    April; Salvatore answers on 7 April; Tommy's trust toward Salvatore rises from 0.30; Tommy's 8
>    April decision about answering Salvatore reads the changed trust through its existing report
>    score components. Do not force a different winner. An unchanged winner with measurably changed
>    trust-derived components is acceptable. If a winner changes naturally, record it and do not tune
>    it away.
>
> 9. Keep the natural proof and causal counterfactual separate: the natural-run test must prove the
>    real exchange, agreement record, trust movement, and later score read; the counterfactual must
>    use two deliberately constructed equivalent staged states, applying the agreement consequence to
>    only one before scoring the same candidate with the same deterministic inputs. Do not add a
>    production switch, dependency-injection seam, or test-only "disable agreement" path. Do not stub
>    out production receipt calls or manipulate the natural fixture to manufacture the comparison.
>
> 10. Require the complete proposed test set: news/agreement/repetition/reaffirmation/reversal/conflict
>     state-machine cases; agreement/conflict mutual exclusivity; information-boundary test varying
>     Candor and ActualBasis while preserving the asserted account; listener-only directionality; all
>     three receipt paths; trust ceiling; natural-run and staged counterfactual proofs;
>     controlled-versus-autonomous equivalence; deterministic rerun and save/load replay equivalence;
>     discriminating, reverted mutation checks for missing emission, repetition farming, wrong
>     direction, omitted receipt sites, and private-truth leakage.
>
> 11. Preserve all exclusions from the proposal: no new actions, generators, candidates, scenario
>     fixtures, characters, organizations, player-facing surfaces, Godot features, negative trust,
>     decay, obligation movement, grievance changes, coefficient tuning, queryable persistence,
>     relevance tiering, rumors, mutation automation, or seed sweeps.

Comparing this against the "Rulings taken at planning time" summary earlier in this archive finds no
substantive discrepancy — the summary is accurate — but accurate is not the same claim as "preserved
verbatim in the commit diff," and the archive should not have said the second thing when only the
first was true. **Going forward, the surviving contract for this milestone is this correction's
verbatim quotation above, not the commit diff.**

**4. `Relations.RecordAccountAgreement`'s doc comment misstated why it ignores provenance.** It
claimed "the epistemic difference between direct observation and testimony is already charged in
`Cognition.Receive`'s confidence raise" — true for the conflict direction (`RecordAccountConflict`),
where erosion genuinely is provenance-differentiated (0.15 vs 0.45, plus stance protection), but false
for agreement: the confidence raise in `Cognition.Receive`'s fresh-agreement branch is a flat `0.15 ×
asserted confidence` regardless of `SourceKind`. Nothing charges the distinction anywhere for this
direction. Fixed: the doc comment now states plainly that milestone 016 applies one flat social rule
regardless of provenance *because nobody has decided otherwise*, not because the distinction is priced
elsewhere, and that `PriorSourceKind`/`PriorSourceId` are preserved specifically so a future,
deliberate decision can weight on them. `docs/DESIGN_DECISIONS.md`'s new section (finding 1) states
the same correction.

**What this correction is not.** No simulation behaviour changed and no coefficient was tuned — both
constants remain `0.35`; only `AccountAgreementTrustGain`'s storage mechanism (`const` →  mutable
`static`) and one test's proof strategy changed, verified by mutation-check to actually discriminate.
Full verification (build, all tests, `--verify` on all three moved variants, `--compare`, both
required viewpoints, both Godot self-tests, the two-process restart proof) was re-run clean; every
trace hash and chosen-action digest is unchanged from `66917c7`'s own table, which this correction did
not need to re-derive since nothing that produces those hashes changed.

**Recurring-failure list, walked.** *A test whose expected value is computed from the same live
constant production reads*: the classic shape where a test's own arithmetic silently launders a
production defect, found here because the two candidate constants happened to share a value —
exactly the situation this project's own review culture warns is the hardest version of this failure
to catch by inspection. *An archive claim about where evidence lives, unchecked against the actual
diff*: the "visible in this commit's diff" claim was written from memory of having typed the rulings
into `CURRENT_MILESTONE.md` earlier in the same session, not from checking what the committed diff
actually contained — a distinction this project's standing practice ("Recording a review that did not
happen") exists to catch when the gap is about *review*, and applies equally when the gap is about
*provenance of a written record*. *A doc comment's justification reused from a sibling method without
re-deriving whether it actually held*: `RecordAccountAgreement`'s comment inherited
`RecordAccountConflict`'s "already charged elsewhere" reasoning by analogy, without checking that the
agreement branch's confidence raise is actually provenance-differentiated the way the conflict
branch's erosion is. It is not, and the mirror-image framing this whole milestone otherwise earns its
keep by should have been checked at exactly this seam rather than assumed to carry over whole.

**Status.** This correction is implemented, tested, and mutation-checked as described above. Milestone
016 remains **not accepted** — Matt's confirmation of a named commit is what that requires. Neither
this section nor the account above is rewritten to read as though it were correct from the start.

---

## Second correction from Codex's review of `380a241`, 2026-08-23

Appended, not folded in. Both accounts above are preserved as originally written. This section
supersedes only what it describes: the storage type of `Relations.AccountAgreementTrustGain` and the
shape of the test proving `Relations.RecordAccountAgreement` reads it. Findings 1, 3, and 4 from the
first correction stand unaffected.

**Codex reviewed `380a241` and found one new P1: the first correction's fix to finding 2 introduced a
new defect while closing the original one.** Changing `AccountAgreementTrustGain` from `const` to a
plain mutable `public static double` made the discriminating test possible, but the field itself
became process-global mutable state with no persistence or replay story of its own — reachable by any
other test, or any other code, running in the same process. That is exactly the shape of state this
project's determinism and replay guarantees exist to rule out, introduced in the course of fixing an
unrelated test-rigor gap.

**Fix.** `AccountAgreementTrustGain` is `public static readonly double` again — immutable from any
caller's point of view, indistinguishable in behaviour from the original `const`, but still (unlike
`const`) a genuine static field with its own metadata token rather than a value inlined at every call
site. `RecordAccountAgreement_reads_its_own_dedicated_field_not_conflicttrustcost` no longer varies
any runtime value. It instead reads `Relations.RecordAccountAgreement`'s own compiled IL directly: a
new helper, `StaticFieldsReadBy`, walks the method body's bytecode instruction by instruction and
resolves every `ldsfld` it finds to the real `FieldInfo` being read, then asserts that set contains
`AccountAgreementTrustGain` and does not contain `ConflictTrustCost`. The walker's opcode-to-operand-
size table is built from `System.Reflection.Emit.OpCodes`' own metadata via reflection over its public
static `OpCode` fields, rather than hand-transcribed — a hand-typed table risks exactly the kind of
silent, undetectable error a test meant to prove structural correctness cannot afford, so the table is
read from the BCL's own canonical definitions instead of retyped.

**Mutation-checked directly against Codex's exact review mutation**: temporarily changed
`RecordAccountAgreement` to read `ConflictTrustCost`, rebuilt, and ran the focused test. It failed —
`Assert.Contains() Failure: Filter not matched in collection, Collection: []` — because
`ConflictTrustCost` is still `const` and a `const` read never produces a `ldsfld` at all (it is
inlined as a literal `ldc.r8` instruction instead), so the mutated method reads no static field
whatsoever from this walker's point of view. The test still fails for exactly the right reason: it
asserts the presence of `AccountAgreementTrustGain` in what the method reads, and under the mutation
that field is absent, which is precisely "production is not reading its own dedicated field" — the
claim under test. Reverted before this commit; the full suite re-confirmed green.

**What this correction is not.** No coefficient tuned — both `AccountAgreementTrustGain` and
`ConflictTrustCost` remain `0.35`. No simulation behaviour changed: `AccountAgreementTrustGain` is
read exactly once, in `RecordAccountAgreement`, and an immutable `static readonly` field is read
identically to how a `const` field was read from every call site's perspective. Findings 1 (the
`DESIGN_DECISIONS.md` entry), 3 (the archive's corrected provenance claim about the eleven rulings),
and 4 (the corrected doc comment on `RecordAccountAgreement`) from the first correction are untouched.

**Full verification re-run from a clean tree**: build 0 warnings/0 errors across six projects; 505/505
tests (the coefficient test's identity changed, its count did not); `--verify` deterministic and
byte-identical to `66917c7`'s own hashes on all three moved variants (`baseline` `9AF57665067AEA11`,
`disloyal-vincent` `9A6E0E518294532F`, `resentful-tommy` `3C4483640153DA88`); `--compare` unchanged
across all five; both required viewpoint runs exit 0; Godot `--selftest` and `--selftest-goldenpath`
unchanged; the two-process restart proof unchanged on the isolated slot; production save slot hash
confirmed unchanged before and after (`sha256:35937d3b...`).

**Recurring-failure list, walked.** *A fix that closes one gap by opening an adjacent one*: the first
correction's own fix — needed to make a discriminating test possible at all — introduced exactly the
kind of process-global mutable state this project's whole determinism apparatus (seeded RNG,
replay-based persistence, byte-identical trace verification) exists to prevent, in a single small
static field that looked, in isolation, like an ordinary test-support seam. *Solving a problem by
making the wrong thing mutable, when the actual need was inspectability*: the original goal was never
"vary this value at runtime" — it was "prove which of two field names a compiled method references" —
and `static readonly` combined with IL inspection meets that need without ever making the coefficient
itself a moving target. The narrower fix was available from the start; it took a second review to find
it because the first fix's test did pass, and a passing discriminating test reads as success even when
the mechanism that made it discriminate is itself the problem.

**Status.** This second correction is implemented, tested, and mutation-checked as described above.
Milestone 016 remains **not accepted**, and now stands **corrected twice** — Matt's confirmation of a
named commit is what acceptance requires. Neither this section nor either account above is rewritten
to read as though it were correct from the start.
