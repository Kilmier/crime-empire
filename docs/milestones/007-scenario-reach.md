# Milestone 007 — Scenario Reach

Status: **awaiting Codex review. Not verified.** Scope was proposed against head `6355347`, revised
once on eight binding rulings, and authorized with two further corrections. The rulings are
reproduced verbatim at the foot of this file; they were also written into `CURRENT_MILESTONE.md`
before implementation began, so unlike milestone 006's they exist in a committed revision.

## What was attempted

Three consecutive milestones had ended with a correct, mutation-checked mechanism the accepted
scenario could not demonstrate. Milestone 006 named the reason: the constraint was no longer the
mechanisms, it was the fixture. This milestone was to make the mechanisms built in 004–006 produce an
observable behavioural consequence in a natural scenario — by removing what blocked them and giving
the fixture the one piece of room it lacked, not by adding a fourth mechanism and not by moving a
coefficient until something flipped.

Measured against `6355347` at seed 42, the chain was one blocked link long:

- the conflict edge fired — Salvatore's trust in Vincent fell 0.50 → 0.309 — and Salvatore has no
  scored decision that reads a relationship, so it terminated in nobody;
- the delegator's question was competitive and never won: 0.82 / 0.76 / 0.82 / 0.81 against
  `report to salvatore, leaving out his own part` at 0.97 / 0.89 / 0.96 / 0.94;
- that report won on `self-protection +1.50` for withholding the same claim from the same man four
  times, because `Reporting.LastAddressed` treated it as settled for eligibility while `Utility`
  re-priced it as freshly at stake;
- `resentful-tommy` chose the identical action at every decision as `baseline`, while `--compare`
  reported five distinct histories.

## Success definition, and the distinction it turns on

Two terms, used consistently here per ruling 8:

- **Decision-relevant** — the trust the conflict moved contributes a non-zero, named component to the
  score of a decision taken after the conflict, and that contribution vanishes when
  `Relations.ConflictTrustCost` is zero. **This was the bar, and it is met.**
- **Choice-changing** — the same contribution alters which candidate wins. **Not required, and it
  does not happen.** Measured and reported below rather than engineered.

## What was completed

### D1 — self-protection is paid for protection a report newly buys

`PriorDisclosureState` (`Domain/Report.cs`) records what one recipient has already been given by one
sender about one claim, as a fact about **messages**: never addressed, withheld, disclosed
affirmatively, denied. `Reporting.PriorDisclosure` computes it, public and parameterised so tests
invoke the production rule rather than a copy — the `Generators.CanAsk` precedent.

Two things it deliberately does not read.

**Not the sender's beliefs.** Report *eligibility* and marginal concealment *value* answer different
questions. `NeedsConveying` asks whether his own position has moved since he last spoke and
legitimately re-arms a report; what his change of mind cannot do is make his recipient un-hear the
silence he already bought. The first draft of this scope had them keyed off one predicate, on a
"one rule, two readers" argument that was simply wrong about which rule.

**Not `Report.Candor`.** A candid rejection and a deceptive denial put the same denying stance in
front of the recipient. Candour records what the sender was *trying* to do; this needs what the
recipient ended up holding, so it reads the asserted stance. Keying on candour would have caught the
liar and let the sincere retraction go on buying protection it had already spent.

Precedence is **most recent treatment**, because the question is what this recipient currently has.
`Denied` is therefore not absorbing: deny → affirm → deny earns the premium again, since a listener
who now holds the claim has to be talked back out of it.

Protection is computed **to completion per claim, then maximised**:

```
Partial:  1.5 × stakes    if never addressed, else 0
False:    1.9 × stakes    if never addressed
          0               if already denied
          0.4 × stakes    otherwise
```

Taking separate maxima of the two halves and adding them would let them come from different claims
and report a figure no single act of concealment buys — a two-claim case where the two disagree is
pinned by its own test.

**No coefficient changed.** `1.9` was decomposed into the `1.5` that silence buys and the `0.4` a
denial adds on top of it. A first-time denial still scores exactly 1.9.

`SuppressedClaim(Claim, PriorDisclosureState)` carries the state with the claim it is about.
`Candidate.Suppressed` is now a list of these. Parallel collections indexed by position were
considered and rejected: milestone 004 produced the "distinction dropped on the way to the next
reader" defect four times, and that is its shape.

### D2 — the repeat guard reads whether the listener moved

Refined settled invariant:

> Identical words do not compound confidence **unless the listener independently reconsidered that
> claim after the speaker's preceding account**. After such intervening movement the repeated words
> may create **one** new conflict; further identical repetitions are again inert.

`verbatimRepeat` now also requires `prior.ReconsideredAt <= latestFromSender.At` — the non-nullable
accessor, which falls back to `AcquiredAt`; the nullable backing field would have read a
never-revisited belief as one that had moved.

Both halves are structural rather than second rules that could drift. "Independently" falls out of
comparing against the speaker's *latest* account, which set the stamp itself, so only movement from
elsewhere can push it later. "One, then inert" falls out of the disagreement branch stamping the
record at `at` while the new testimony carries the same `at`, so the next identical account finds
them equal and returns early.

Without this the milestone's bar was unreachable: Salvatore's second briefing is byte-identical to
his first — `Believes`, 0.75, basis `Report` — and produced no conflict at all.

### D3 — one additional contested business

`dorato-bakery`, owned by `nunzio`, refusing tribute in the harbour. Its purpose is to keep
`OrgCondition.RevenueLoss` alive past the first collection, which is what produces a second
`LeadershipReview`, a second assignment, and the briefing that contradicts Vincent.

It needed an owner. `AdvanceTribute` resolves a demand through the owner's own decision rather than a
roll made on his behalf, and sharing Marco would have been worse than a sixth character: `Commit`'s
concede and refuse paths find a business by owner and would have had him answering for the wrong
shop. This is a documented departure from the scope's own "no new characters" exclusion, which was
written about systems growth; it is recorded here rather than quietly taken.

**Salvatore is not told about the bakery, and that turned out to be the load-bearing part.** See the
discoveries below.

### D4 — `--compare` distinctness from structured fields

`DecisionRecord.ChosenActionSignature()` builds `(At, ActorId, Kind, Id, TargetId, AboutClaim,
AnsweringClaim, Candor)`. `AboutClaim` and `AnsweringClaim` are carried separately rather than
coalesced into one "subject claim". Score is excluded on purpose: this compares what he did, not how
nearly he did something else.

It lives in the simulation library, so the tests call the same code the runner renders. `--compare`
now reports trace distinctness and chosen-action distinctness separately, and says plainly when a
configuration chose the same actions as another throughout.

### D5 — the stale `resentful-tommy` comment

`Variants.cs` still asserted that Tommy never gives Vincent an account and that a
delegator-to-executor path would need a milestone of its own. Both were false — corrected by 006's
first correction and by `Generators.FromDelegation`. Rewritten to say what is actually true, including
what still does not happen.

### Not planned, found on the way

**`Report.AnsweringClaim`.** Two tests identified a reply as "any report from the asked person to the
asker within two days". That is a link the model did not have, and when behaviour moved it reported
Tommy as having answered a question he held no position on — what he had actually done was ignore it
and volunteer an unrelated account the next evening. The report now records the question it answers,
and both tests match on it.

## Tests and results

`dotnet test` — **276 passing** (was 240). Clean build after `dotnet clean`: **0 warnings, 0 errors.**
All five variants deterministic under `--verify`; both viewpoint commands run clean.

Ten mutation checks, each caught by exactly the intended tests and then restored:

| Mutation | Result |
|---|---|
| self-protection restored to unconditional `1.5 × stakes` | 21 — the whole concealment table, the per-claim test, and every end-to-end "the question wins" test |
| `DenialPremium` folded to zero | 5 — the three denial rows, the per-claim test, and the staged executor denial |
| `PriorDisclosureState` keyed on `Report.Candor` | 2 — the candid-rejection and deny-then-affirm tests |
| state derived from eligibility (the superseded design) | exactly 1 — the reconsideration test |
| state computed per report rather than per `(recipient, claim)` | exactly 1 — the never-addressed test |
| separate maxima added instead of per-claim completion | exactly 1 — its own test |
| `verbatimRepeat` ignoring intervening movement | 14, including the three-account sequence and every run-wide conflict test |
| `verbatimRepeat` ignoring the words | 6 — the whole milestone-003/006 repetition and recantation set |
| `Relations.ConflictTrustCost = 0` | 17, including **the decision-relevance test** |
| second business seeded to sort first | exactly 1 — the business-ordering test |
| digest keyed on rendered text | exactly 1 — the structured-distinctness test |

**The decision-relevance test** is the one the milestone stands on, and `ConflictTrustCost = 0` is its
mutation. Because that constant is a `public const`, a runtime swap would have meant opening a
mutation surface on `Relations` that milestone 006 deliberately closed. It uses two arms instead, both
through the production scorer: a live arm reading the component off a candidate actually weighed in
the accepted run, and a counterfactual arm putting only the trust back to its starting value and
scoring the same candidate again. The delta is computed by `Utility`, never re-implemented in the
test.

Same limit as 006: replay-comparator field additions cannot be mutation-checked, since deleting a
field weakens the comparator without failing anything. The independent signal remains `--verify`.

## Behavioural movement

All five hashes move; all five variants remain deterministic and within budget.

| Variant | Hash | Decisions | Reports | Requests | Conflicts |
|---|---|---|---|---|---|
| baseline | `26C7D3195DBCD67F` | 38 | 6 | 5 | 2 |
| cautious-vincent | `F0067A8493E74516` | 21 | 2 | 4 | 3 |
| watchful-boss | `83327839749FE63C` | 39 | 7 | 5 | 2 |
| disloyal-vincent | `837273496CBB7DCC` | 39 | 6 | 5 | 2 |
| resentful-tommy | `09F26760FB80EFB1` | 38 | 6 | 5 | 2 |

Decision counts were 33 / 16 / 33 / 34 / 33. Budgets are 100 decisions and 25 reports; neither was
relaxed. **Reports fell** — baseline from eleven to six — which is the fix working: five of the
eleven existed only because withholding the same claim was being paid for afresh.

**Cycle one is unchanged.** The first fourteen chosen actions of `baseline` are identical to
`6355347`'s, score for score, through Vincent's first partial report on 1 April. The second shop
changes what happens after the grocery pays, not how it is collected.

### The chain, as it now runs in `baseline`

```
01 Apr  Vincent keeps his own breach back from Salvatore        — first concealment, still worth 1.50
06 Apr  Salvatore's second briefing re-asserts that the grocery
        is holding out, to a capo who watched it start paying   — conflict; trust 0.45 → 0.214
06 Apr  In the same deliberation, the concealing report is now
        worth -0.56 and Vincent goes to Tommy instead           — the delegator's question, 0.76, chosen
07 Apr  Tommy answers, keeping his own part back
09 Apr  Tommy tells Vincent what he has
11 May  A third briefing, same words — and this one counts too,
        because Tommy moved him in between                      — conflict; trust 0.214 → 0.031
```

## Important discoveries

**The conflict moved from the boss to the capo, and the milestone's own fix is what moved it.**
Milestone 006's demonstrated conflict — Salvatore contradicted by Vincent about
`BusinessRefusesTribute` — no longer occurs. It only ever reached the page on Vincent's *second*
concealing report, once the first three claims had been conveyed and his rejection led the news
ordering. He does not file that second report any more, because it existed only to be paid twice for
one silence. Salvatore now ends the run with his trust in Vincent untouched.

This is the right outcome and it is worth stating plainly rather than presenting as a straight win.
The mechanism did not get better at firing; it started firing somewhere useful. Vincent is the only
character in the cast with decisions that read a relationship, so a conflict that lands on him is
worth more than one that lands on his boss — and 006's central finding was precisely that the boss
was a dead end. Two run-wide milestone-006 regression tests were retargeted accordingly, with the
boss-side direction still covered by the staged unit tests.

**Partial knowledge is what left him room to think.** The second shop was first written into
Salvatore's beliefs as well as into the world. That version produced everything except the point of
the milestone: the second assignment named the bakery, Vincent had a fresh collection job on the very
wake where he would otherwise have gone to ask his own man, and the delegator's question lost 0.76 to
3.72. Taking the bakery out of the boss's head — leaving the shortfall objective and his account of
its cause incomplete — is what restores the room. It is not a workaround. It is the truth/knowledge
distinction the project rests on, applied to an organisation's own books, and it produces the best
sentence in the run: he goes on telling his capo the grocery will not pay, after his capo has
personally watched it start paying, because the shortfall he can see has a cause he cannot.

**Decision-relevant, and nowhere near choice-changing.** On the post-conflict partial report to
Salvatore, the `relationship effects` component reads **0.0063** with the conflicts and **0.0440**
without them. Non-zero, moved entirely by trust, and about four hundredths of a point — against
decision margins in this scenario of the order of one. `Relations.ConflictTrustCost = 0.35` scaled by
strengths of 0.675 and 0.523 takes trust from 0.45 to 0.031, and `Utility.Loyalty` weights trust at
0.45 while a grievance of 0.35 is already subtracting 0.175, so almost all of the movement is
absorbed before it reaches a score. That is the first real evidence about the size of these
coefficients rather than their sign, and it belongs to the tuning question rather than to this
milestone.

**An executor contradiction still does not occur, as predicted.** Vincent now puts the question and
Tommy now answers — the first delegator-to-executor exchange the accepted scenario has ever produced,
where milestone 006 could prove the path only through a staged test. He answers honestly. `Utility`
prices a denial almost entirely on whether he believes anyone can contradict him, and
`ResolveViolence` leaves him inferring the street saw him; at `Loyalty(tommy, vincent) = 0` in
`resentful-tommy` the denial still loses by roughly 2.9. Recorded as an honest non-result under
ruling 7. The route that would produce it honestly is noted in `ROADMAP.md`: `AdvanceConceal`'s first
step is called "quiet the witnesses" and moves only `LegalExposure`, leaving the concealer's belief
that he was seen exactly where it was.

**`resentful-tommy` converged, as predicted, and now says so.** Its chosen-action signature is
byte-identical to `baseline`'s while its rendered trace differs. `--compare` reports "5 distinct
traces · 4 distinct chosen-action sequences" and names the convergence. It was not tuned and not
re-cut; manufacturing distinctness would have been inventing a result.

**Two tests were passing by accident, and the movement exposed both.** One inferred a reply from a
two-day window and asserted against a link the model did not have. The other, `Hierarchy_and_shared_membership_transfer_nothing`,
required every held belief to be self-acquired or backed by testimony, and Salvatore's seeded
"the books" belief satisfies neither — it passed only because Vincent happened to give him an account
of that same claim later in the run. Its sibling test already documented the exemption for sources
outside the cast; this one had never needed it. Both are now asserted against something real. The
question milestone 006 left behind — *what does this claim assert, and did anything actually check
it?* — found two more.

## Deferred work

- **The `believedWitnesses` global scan.** `Utility` maxes over every `WitnessSawIncident` the actor
  holds, regardless of the incident being concealed — the same defect shape as the `SeekCorroboration`
  scan `404b416` fixed. It changes nothing in this scenario, which is why it was excluded rather than
  folded in.
- **Concealment that does not quiet the witnesses it is named for**, which is what stands between the
  executor's answer and an executor's denial.
- Everything carried from 006: negative trust; the concealment MVP rule; the empty-domain label; the
  `FirstHandTestimony` and `Discovery` discounts; `ConflictTrustCost`, now with a measured
  consequence rather than only a magnitude.
- `OPEN_CONCERNS.md` #3 is updated with this milestone's evidence and **not retired.**
- The bakery is never collected from. Nobody in the organisation knows it is refusing, which is
  deliberate; it does mean a second collection cycle remains unexercised.

## Relevant commits

- The implementation commit that introduced this file. Not cited by hash, for the reason milestone
  001's archive gives: a commit cannot contain its own hash.
  `git log --diff-filter=A -- docs/milestones/007-scenario-reach.md` resolves it.

---

## The rulings, reproduced verbatim

Issued by Matt in review conversation, in two rounds, and written into `CURRENT_MILESTONE.md` before
implementation rather than reconstructed afterwards.

### Round 1 — approval subject to one D1 revision and two clarifications

> The milestone-007 direction is approved subject to one D1 revision and two clarifications. Do not
> implement yet.
>
> 1. Separate report eligibility from marginal concealment value. `NeedsConveying` may make a changed
>    position reportable again, but `LastReconsideredAt` must not erase what this recipient has
>    already heard.
>
>    Model prior treatment per `(sender, recipient, claim)` from actual message content, not merely
>    `Report.Candor`:
>
>    * never addressed: Partial `1.5 × stakes`, False `1.9 × stakes`;
>    * previously withheld: Partial `0`, False `0.4 × stakes`;
>    * previously disclosed affirmatively: Partial `0`, False `0.4 × stakes`;
>    * previously given a rejection/denial: Partial `0`, False `0`.
>
>    A candid rejection and a deceptive denial both mean the recipient has already heard the denying
>    stance. Add focused tests for prior candid disclosure and for belief reconsideration not
>    restoring previously purchased protection.
>    `SuppressedClaim(Claim, PriorDisclosureState)` is approved and preferred over parallel arrays.
> 2. D2 is approved. Refine the settled invariant to say that identical words do not compound unless
>    the listener independently reconsidered that claim after the speaker's preceding account. After
>    that intervening movement, the repeated words may create one new conflict; further identical
>    repetitions must again be inert.
> 3. D3 is approved as the bounded scenario-reach lever. Preserve cycle-one choices and test the
>    intended business ordering explicitly.
> 4. D4 is approved, but behavioral distinctness must be computed from structured chosen-decision
>    fields—time, actor, action kind/id, target, subject claim and candor—not rendered trace strings.
>    Report trace distinctness and chosen-action distinctness separately.
> 5. D5 is approved.
> 6. Keep `resentful-tommy` and report its behavioral convergence honestly. Do not tune or recut it
>    merely to manufacture distinctness.
> 7. It is acceptable that an executor contradiction is predicted not to occur. Record that as an
>    honest non-result, not a milestone failure and not something to force.
> 8. Milestone success does not require tuning until the relationship term flips a choice. It does
>    require the natural question to win, a real conflict to reach Vincent, and the resulting
>    relationship change to contribute a non-zero, traceable component to a later decision score.
>    Setting `ConflictTrustCost` to zero must remove that component. Clearly distinguish
>    "decision-relevant" from "choice-changing" in the archive.

### Round 2 — authorization with two corrections

> Milestone 006 is closed and must not be reopened. Its assessment is preserved in the canonical
> documents and has already been incorporated into milestone 007.
> Approve most-recent-treatment precedence for D1. Denied is not absorbing; deny → affirm → deny may
> earn the final `0.4 × stakes` denial premium again.
> Before implementation, make these two corrections:
>
> 1. Compute concealment protection independently for each suppressed claim, then take the maximum
>    completed per-claim value. Do not add separate maxima that could come from different claims.
> 2. In D2, compare `prior.ReconsideredAt` with the latest account timestamp, not nullable
>    `prior.LastReconsideredAt`.
>
> With those corrections, milestone 007 — Scenario Reach is authorized. Write the complete scope and
> rulings into `docs/CURRENT_MILESTONE.md`, then implement the milestone.
> Preserve all stated exclusions, baseline predictions, falsification criteria, structured behavioral
> digest, mutation checks, and honest reporting of non-results. Do not pull in milestone-006
> deferrals such as the full relationship schema, negative trust, global `believedWitnesses`
> correction, coefficient tuning, or concealment-rule redesign.
> Run the full required verification, archive milestone 007, commit it as one coherent
> implementation-and-archive commit, and stop for Codex review. Do not begin milestone 008.

### How each was discharged

| # | Ruling | Outcome |
|---|---|---|
| 1 | Eligibility separate from concealment value; four states from message content, not candour; focused tests for prior candid disclosure and for reconsideration | **Completed.** `PriorDisclosureState`, `Reporting.PriorDisclosure`, the eight-row scoring table, and `Reconsidering_a_belief_does_not_restore_protection_already_spent`. |
| 2 | Refined repetition invariant: one conflict after intervening movement, inert again afterwards | **Completed.** `Repetition_is_inert_until_he_moves_and_then_inert_again`, and observed in play — three briefings, two conflicts. |
| 3 | Preserve cycle-one choices; test the business ordering explicitly | **Completed.** Cycle one is byte-identical through 1 April; ordering has its own test and its own mutation. |
| 4 | Structured digest; report the two distinctness figures separately | **Completed.** `ChosenActionSignature()`; `--compare` prints both and names the convergence. |
| 5 | Correct the stale variant comment | **Completed.** |
| 6 | Keep `resentful-tommy`, report convergence honestly, do not tune or recut | **Completed.** Untouched; convergence asserted and printed. |
| 7 | An executor contradiction not occurring is an honest non-result | **Completed.** Recorded above with the arithmetic and the honest route to it. |
| 8 | Question wins, conflict reaches Vincent, relationship change is decision-relevant, zeroing the cost removes it, and the two terms are distinguished | **Completed.** 0.0440 → 0.0063, mutation-checked, and choice-changing is separately measured and answered no. |
| R2.1 | Per-claim completion, then maximum | **Completed.** `Protection_is_completed_per_claim_before_the_maximum_is_taken`, with its own mutation. |
| R2.2 | Compare `prior.ReconsideredAt`, not the nullable field | **Completed.** |
