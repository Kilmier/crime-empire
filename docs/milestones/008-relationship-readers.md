# Milestone 008 — Relationship Readers and the Executable Schema

Status: **awaiting Codex review. Not verified.** `REVIEW_LEDGER.md` is the record of when a review
happens; nothing here should be read as one having happened.

Scope was proposed against head `b8e5ed4` and authorized by Matt with ten binding rulings, reproduced
verbatim at the foot of this file. They were also written into `docs/CURRENT_MILESTONE.md` before
implementation — but ruling 10 keeps the existing lifecycle, under which the archive-and-reset land in
one commit, so **this archive is again the only surviving copy**. That is stated up front rather than
discovered later, which is what milestones 006 and 007 both had to do.

## What was attempted

Milestone 007 ended on the project's most useful open number: a full collapse of trust, 0.45 → 0.031,
moved a later decision's score by 0.0377. The question had moved from "can a relationship consequence
be shown at all" to "is it worth anything once it is shown."

This milestone was to answer *why* it was worth so little — the coefficients, or the shape of the
reader — and to build the instrument that could tell the difference. It was explicitly **not** general
tuning and **not** concealment work. No coefficient in the model was changed.

## Precheck, per ruling 1

A read-only harness outside the repository built each variant, ran it, and read the recorded
`ScoreBreakdown` components. It modified no production code. It was run before any edit, and its
purpose was to stop the milestone if the proposal's arithmetic did not survive contact.

**The load-bearing numbers reproduced exactly.**

| Claim | Proposed | Measured |
|---|---|---|
| Net relationship effects, pre-conflict partial report to Salvatore | 0.0440 | **0.0440** |
| Same, post-conflict, 11 May | 0.0063 | **0.0063** |
| Net on that candidate is `0.2 × Loyalty`; `∂net/∂Trust` = 0.09 | yes | yes |
| The rendered trace hides the cancelling half | yes | **worse than proposed** |

On the 11 May decision **both** halves — `+0.0219` and `−0.0156` — fall under `Significant()`'s `0.15`
cutoff, so the trace printed no relationship line at all for the candidate the milestone-007 finding
was measured on. Earlier in the run, at trust 0.45, it printed `+0.15` where the net was `+0.044`.

**Two discoveries that changed the implementation without contradicting the premise.**

**1. The component name `relationship effects` does not mean "reads a relationship."** Of 168 such
components across the five variants, **61 — 36% — read no relationship state at all**: they are
`SeekCorroboration`'s "going behind X", which is `−0.45 × proud`. Two existing production tests
aggregated on that name, and the whole planned diagnostic was about to. A label is not a derivation,
so the instrument tags at the point the value is computed. **This is the ledger's "two different
things treated as one" pattern, found inside the instrument built to measure it.**

**2. The relationship channel is not uniformly weak.** The largest relationship contribution anywhere
is **1.44** — Marco's `Concede`, reading `Fear`, on a decision whose margin is 0.25. It is
specifically the `Trust → Loyalty → report` path that collapses to 0.0063. The milestone's question
was therefore about the loyalty aggregation and its report readers, not about relationships being
decorative, and the scope was written that way.

## What was completed

### D1 — grievance unbundled from the clamped loyalty (ruling 3)

`Loyalty` returns a decomposed `LoyaltyReading` rather than a scalar. `Value` is
`clamp(0.45·Trust + 0.30·Obligation + 0.25·Belonging, 0, 1)`; grievance has left the clamped sum and
is applied by each reader as **its own named, separately tagged component** at that reader's own
coefficient.

The clamp was the second attenuator and the more consequential one. Under the old rule, once a
character's grievance exceeded his bond the sum floored at zero: further grievance was free and
further trust was worthless, so a bitter subordinate and an indifferent one scored identically.

**No coefficient changed.** `0.50` is preserved exactly and stays labelled provisional. The arithmetic
is unchanged wherever the old sum did not clamp.

### D2 — two considerations, kept and separated (ruling 2)

The `+0.7 × Loyalty` standing a report buys and the candour cost of the selected report — `+0.8`
Candid, `−0.5` Partial, `−1.4` False — are both retained at their existing coefficients. They were
never a defect to be merged; they were two legitimate things that became one number the moment they
were summed. Each is now separately named and separately tagged.

### D3 — facets at the point of derivation (rulings 3, 8)

`RelationshipFacet` on `ScoreComponent`, set where the value is computed: `Trust`, `Obligation`,
`Belonging`, `Grievance`, `Fear`, `None`. `SeekCorroboration`'s "going behind X" is tagged `None` and a
regression test pins it. `Belonging` is enumerated but excluded from `Relational`, because a man with
no relationships still has a need to belong.

### D4 — the diagnostic (ruling 8)

`ScoreBreakdown` gained `RelationshipComponents()`, `RelationshipGross()`, `RelationshipNet()`, and
`TotalWithoutRelationships()`. The trace prints a developer-facing relationship block for the chosen
candidate and its nearest rival — gross, net, margin, counterfactual, every component with its facet,
**no cutoff** — and flags when the channel decided the winner. It prints only when something actually
read relationship state.

`ScoreComponent.WithoutRelationship` records, at emission, what the component would be worth to a man
with no relationships. The counterfactual is that sum rather than a re-score: re-scoring would draw
fresh noise of up to ±0.05, larger than the effect being measured on every report candidate in the
scenario, and would require mutating relationship state, which milestone 006 deliberately made
impossible from outside `Relations`.

Nothing here reaches `IntelligenceWriter`. Verified by running both viewpoint commands and grepping
the output for every diagnostic term.

### D5 — choice-changing, measured both ways (ruling 9)

`--compare` now reports, per variant, how many decisions weighed relationship state and how many
would have chosen differently without it.

### D6 / D7 — vocabulary and document (rulings 6, 7)

`Trust`, `Fear`, `Obligation`, relationship-keyed `Grievances`. Each has a tested decision reader; a
test asserts all four are read across a natural run of all five variants. No dimension was added.
`docs/RELATIONSHIPS.md` was written last, from the measured results.

## Tests and results

`dotnet test` — **285 passing** (was 276). Clean build after `dotnet clean`: **0 warnings, 0 errors**.

Six mutation checks, each caught by exactly the intended tests and then restored:

| Mutation | Result |
|---|---|
| grievance folded back inside the clamped `Loyalty` | 2 — the unbundling test and the milestone-006 boundary case |
| "going behind X" tagged as a relationship read | exactly 1 — its own regression test |
| the 0.15 cutoff reapplied to the diagnostic | 2 — the cancellation test and 007's decision-relevance test |
| the two report considerations merged into one component | exactly 1 — its own test |
| counterfactual computed without the shared noise draw | exactly 1 — the like-for-like test |
| `Concede`'s fear component left untagged | 2 — the counterfactual and channel-decides tests |

**The merge mutation is the one worth noting.** It preserved the net exactly — `0.2 × Loyalty` either
way — and only the test that asserts the *distinction* caught it. Every other test in the suite was
blind to it, which is precisely the condition this milestone existed to end.

Two production tests were migrated from aggregating by component name to reading the facet. One of
them, milestone 007's decision-relevance test, had been getting a right answer from a wrong rule: the
"going behind" term happens not to appear on a `ReportToSuperior` candidate. That is the least durable
kind of correct.

## Behavioural movement

All five hashes move; all five variants remain deterministic on repeated runs.

| Variant | Hash | Decisions | Reports | Requests | Conflicts | Rel. read | Rel. decided |
|---|---|---|---|---|---|---|---|
| baseline | `3BA97219464FC2E4` | 38 | 6 | 5 | 2 | 19 | 2 |
| cautious-vincent | `EC664E9FB52010B7` | 21 | 2 | 4 | 3 | 12 | 3 |
| watchful-boss | `51AB00158218ACD0` | 39 | 7 | 5 | 2 | 18 | 3 |
| disloyal-vincent | `709F0B6E4B90A2F4` | 39 | 6 | 5 | 2 | 20 | 1 |
| resentful-tommy | `3D91B931EC2DAF3B` | 38 | 7 | 5 | 2 | 19 | 3 |

Decision counts are **unchanged** from milestone 007 at 38 / 21 / 39 / 39 / 38, as are requests and
conflicts. `resentful-tommy`'s reports rose from 6 to 7, downstream of the fork described below.

**Every hash moves for a reason that is not a behaviour change**: the trace now carries the
relationship diagnostic block. The scoring change was measured separately and before the trace was
touched, and that intermediate measurement is the more informative one:

| Variant | 007 accepted | after D1–D3, before the trace diagnostic |
|---|---|---|
| baseline | `26C7D3195DBCD67F` | `68E5E464986C444A` |
| cautious-vincent | `F0067A8493E74516` | `25FF67BD83C60BA5` |
| watchful-boss | `83327839749FE63C` | **`83327839749FE63C` — byte-identical** |
| disloyal-vincent | `837273496CBB7DCC` | `7AA94DEB00850D51` |
| resentful-tommy | `09F26760FB80EFB1` | `F523F88899C952A1` |

**`watchful-boss` is a natural control and it held.** It is the only variant in which nobody holds a
grievance against anybody — the variant clears Vincent's — and it is byte-identical across the
unbundling. That is direct evidence the movement everywhere else is the grievance change and nothing
riding along with it.

## Important discoveries

### The channel is load-bearing, and milestone 007's figure was measuring the weakest path in it

Removing relationship state changes which candidate wins at **1 to 3 decisions in every variant**, and
at 1–5 across six seeds tested. The relationship channel is not decorative. F3 — the falsification
criterion that would have reported it as decorative — is answered clearly in the negative.

That is a much stronger result than milestone 007's `0.0377` implied, and the reason is now visible:
007 could only see the trust-to-report path, which is the single place in the model where two loyalty
reads nearly annihilate. Fear was never small. **The honest summary of 006 → 008 is not "the
relationship edge is weak" but "the edge from a perceived account conflict to a partial report is
weak, and it is the only edge those milestones could see."**

### A soldier who resents his capo now conceals instead of reporting to him

At seed 42, `resentful-tommy` diverges from `baseline` for the first time. On 9 April, decision 19,
Tommy chooses `ConcealIncident` at 0.4689 over reporting to Vincent at 0.4410. The histories fork
there and stay forked; eleven of the thirty-eight paired decisions differ, which is one fork rather
than eleven changes.

The diagnostic states the cause without interpretation:

```
     conceal:11        gross 0.0000  net  0.0000  margin  0.0000  without-relationships 0.4689
     report:vincent    gross 0.2520  net -0.1680  margin -0.0279  without-relationships 0.6090
       [Trust, Obligation, Belonging] +0.1820 (relational +0.0420)  reporting maintains standing…
       [Grievance                   ] -0.2100 (relational -0.2100)  what he holds against the man…
     ⚠ the relationship channel decided this: without it, "report:vincent" would have won.
```

Nobody wrote a rule connecting resentment to concealment. Tommy's grievance against Vincent takes
0.21 out of what reporting to that particular man is worth, and a concealment candidate that was
always on the list wins by 0.03. This is what milestone 006 said the design was for — a consequence
falling out of a dimension feeding a derived value that a scorer already read — and it is the first
time it has happened to a *chosen action* in a natural run.

**Recorded with its fragility, because it has some.** The margin, 0.0279, is smaller than the ±0.05
per-candidate noise. The divergence holds at seeds 42 and 31337 and not at 1, 7, 99 or 2024. It is a
real choice change at this seed and it is not a robust one. Nothing was tuned to produce it; ruling 3
required the unbundling and this fell out of it.

### `resentful-tommy` has stopped converging, and a test had to move rather than be deleted

Milestone 007 recorded, honestly, that `resentful-tommy` chose the identical action to `baseline` at
every decision. `--compare` now reports **five distinct traces and five distinct chosen-action
sequences**, where 007 reported five and four.

`Behavioural_distinctness_is_read_from_decisions_not_from_rendered_text` used that convergence as its
witness: a pair that renders differently and chooses identically, so a digest taken from rendered text
would call them distinct and a structured digest does not. That witness stopped qualifying at seed 42.
The property still needs pinning, so the test moved to seed 1 — where the pair still qualifies, as it
does at 7 and 99 — with the reason recorded in the test itself, and a new test pins the seed-42
divergence. **Deleting it because its witness stopped qualifying would have quietly retired a
guarantee**, which is this repository's recurring-failure list in miniature.

### Obligation is read but never moves

Stated plainly in `RELATIONSHIPS.md` rather than glossed: `Relations.Establish` is the only path that
writes obligation, and it is scenario construction only. Obligation is seeded and then holds its value
for the rest of every run. It earns its place in the vocabulary by being read — `SeekApproval` and
`Loyalty` — not by being dynamic. The same is nearly true of trust in the other direction: conflicts
lower it and **nothing raises it**, so a relationship can be damaged and never repaired. Neither is a
defect introduced here; both are now written down.

## Deferred work

Deferred is not retired, and each item names what brings it back.

- **Negative trust** — returns when a decision exists that would read distrust differently from
  indifference. Ruling 5.
- **Decay** — returns when the calendar and relevance tiers supply a timescale. Ruling 5.
- **A runtime path that raises trust.** Surfaced by writing the schema down; nobody has decided that
  relationships should be unrepairable.
- **A cap on `GrievanceWeight`.** Unbounded. Considered as this milestone's remedy and explicitly
  rejected in favour of unbundling, so the question is open rather than answered.
- **`ConflictTrustCost` (0.35) and `LoyaltyReading.GrievanceWeight` (0.50)** remain provisional
  tuning, alongside the `FirstHandTestimony` and `Discovery` suspicion discounts.
- **Everything in milestone 009's territory is untouched**, per ruling 4: `AdvanceConceal` not
  quieting its witnesses, the global `believedWitnesses` scan, and the `0.9 × Loyalty` versus `0.4`
  denial-premium question.
- The bakery is never collected from; the boss-side conflict path is covered only by staged unit
  tests; the empty-domain label `ConcealIncident(, target=...)`.
- **`AGENTS.md` does not mention `docs/RELATIONSHIPS.md`, and this milestone deliberately did not add
  it.** The natural place is the conditional-reading list, alongside `REVIEW_LEDGER.md` and
  `ROADMAP.md`. It was left alone because no ruling authorized editing `AGENTS.md` and milestone 007's
  closing lesson was that noticing mid-implementation that something has to give is a reason to stop
  and ask rather than to proceed and annotate. The document is reachable from `DESIGN_DECISIONS.md`,
  `OPEN_CONCERNS.md`, `ROADMAP.md` and this archive, so nothing is lost while the question waits.
  **Flagged for Matt as a one-line follow-up, not taken.**

## `OPEN_CONCERNS.md` #3

**Narrowed, not retired.** Storage, update paths, decision readers, the dimension list and the
measurement instrument now have answers, and `docs/RELATIONSHIPS.md` is the document the item asked
for. What remains open is decay, negative trust, and whether respect and resentment are separate
dimensions or derived — each now with a stated condition for return rather than an open question.

## Relevant commits

- The implementation commit that introduced this file. Not cited by hash, for the reason milestone
  001's archive gives: a commit cannot contain its own hash.
  `git log --diff-filter=A -- docs/milestones/008-relationship-readers.md` resolves it.

---

## The rulings, reproduced verbatim

Issued by Matt when authorizing this milestone. This is the only copy in the repository — the
lifecycle resets `CURRENT_MILESTONE.md` in the same commit that creates this file, which is the
mechanism milestones 006 and 007 both recorded and which ruling 10 deliberately keeps.

> Approve the direction of milestone 008 with the following revisions. This is a bounded
> relationship-reader and executable-schema pass, not general tuning and not concealment work.
>
> 1. Run the proposed read-only precheck first. If its live numbers materially disagree with your
>    reconstruction, stop and report before editing.
> 2. Treat the report double-read as two legitimate but distinct considerations: general standing
>    from reporting and the relationship cost of the selected candor. Preserve both coefficients,
>    give them separately identifiable score components, and ensure the diagnostic reports their
>    gross values and net contribution without the `Significant()` cutoff hiding cancellation.
> 3. Do not add a grievance cap as the remedy. Remove grievance from the clamped derived `Loyalty`
>    calculation and expose it as its own named relationship contribution at decision readers.
>    Preserve the existing `0.5` grievance coefficient as provisional; do not tune it. Trust,
>    obligation, Belonging, and grievance must remain separately inspectable through the scoring
>    path.
> 4. Do not rule on `0.9 × Loyalty` versus the `0.4` denial premium. No concealment, `AdvanceConceal`,
>    `believedWitnesses`, or denial-outcome changes are in milestone 008.
> 5. Keep trust in `[0,1]` for the current kernel, but record negative trust as deferred—not
>    permanently retired—until a reader exists that distinguishes distrust from indifference. Defer
>    decay until calendar/tier timescales provide evidence.
> 6. Close the current executable relationship vocabulary only: Trust, Fear, Obligation, and
>    relationship-keyed Grievances, provided each has a tested decision reader. Add no speculative
>    dimensions.
> 7. Write `docs/RELATIONSHIPS.md` last from measured results. For every retained dimension, name its
>    purpose, range, update paths, decision readers, and deliberately absent/deferred behavior. Make
>    clear this is the prototype schema, not an irrevocable long-term list.
> 8. Add production-path diagnostic instrumentation for gross relationship components, their net
>    contribution, candidate margin, and counterfactual comparison. Keep it developer-facing.
> 9. Choice-changing is not required and must not be engineered. Measure it in both directions and
>    report the result. If zeroing the relationship channel changes no natural choices, that is an
>    honest result.
> 10. Use one coherent implementation-and-archive commit under the existing lifecycle. Do not make a
>     separate scope/bookkeeping commit. Reproduce these rulings in the archive before resetting
>     `CURRENT_MILESTONE.md`.

### How each was discharged

| # | Ruling | Outcome |
|---|---|---|
| 1 | Read-only precheck first; stop and report on material disagreement | **Completed.** Ran before any edit. Numbers reproduced exactly; two additive discoveries reported and folded into scope, neither contradicting the premise. |
| 2 | Two distinct considerations, both coefficients preserved, separately identifiable, gross and net without the cutoff | **Completed.** `A_partial_report_keeps_both_considerations_and_they_largely_cancel`, with a merge mutation that preserves the net and is caught only by that test. |
| 3 | No grievance cap; unbundle from the clamped `Loyalty`; preserve `0.5`; keep the four inputs separately inspectable | **Completed.** `LoyaltyReading`; `Grievance_is_not_clamped_away_against_the_bond`; no cap added. |
| 4 | No concealment, `AdvanceConceal`, `believedWitnesses`, or denial-outcome changes; no ruling on `0.9` vs `0.4` | **Completed.** None touched. All carried to milestone 009. |
| 5 | Trust stays `[0,1]`; negative trust deferred not retired; decay deferred | **Completed.** Both recorded with the condition that brings them back. |
| 6 | Close the vocabulary at Trust, Fear, Obligation, Grievances, each with a tested reader; no speculative dimensions | **Completed.** `Every_retained_relationship_dimension_is_read_by_some_decision`; none added. |
| 7 | `RELATIONSHIPS.md` last, from measured results; purpose, range, update paths, readers, absent/deferred; prototype not permanent | **Completed.** Written after the measurements, states its own status in the first line. |
| 8 | Production-path diagnostic: gross, net, margin, counterfactual; developer-facing | **Completed.** `ScoreBreakdown` diagnostics, trace block, `--compare` counts; leak-checked against both viewpoint commands. |
| 9 | Choice-changing measured both ways, never engineered; zero is an honest result | **Completed.** 1–3 per variant, reported by `--compare` and asserted. The one natural fork is recorded with its fragility and its seed-dependence. |
| 10 | One implementation-and-archive commit; no separate scope commit; reproduce the rulings before the reset | **Completed.** Rulings reproduced above; `CURRENT_MILESTONE.md` reset in this same commit. |

---

## Correction — Codex finding on `7a9773b`

Status: **awaiting Codex review. Not verified.** Nothing above is rewritten; the account of what the
milestone originally did stands, including the row in the table above that claims ruling 3 was
completed. It was completed in part, and the part it missed is this finding.

**One finding, accepted by Matt.**

### The finding

Ruling 3 required that **trust, obligation, Belonging and grievance remain separately inspectable
through the scoring path**. The implementation emitted grievance as its own component and fused the
other three into a single component tagged `Trust | Obligation | Belonging`. They were separately
*computed* — `LoyaltyReading` held all four apart — and then put back together at the emission site,
so nothing downstream could say how much of a score was trust and how much was obligation.

**Two of four separately inspectable, described as four.** The union flag made it look tagged while
answering none of the questions the tagging exists for.

This is the repository's signature defect and its third named appearance: *a distinction drawn in one
place and dropped on the way to the next.* Milestone 004 produced it four times. What makes this
instance worth recording is that it happened **inside the correction for the same pattern** — the
facets were introduced in this very milestone precisely because aggregating by component name
conflated things, and the first use of them conflated three things under one flag.

### What the corrective commit did

**Four contributions, four components, one facet each.** `AddLoyaltyParts` replaces `AddLoyalty` and
emits `TrustPart`, `ObligationPart`, `BelongingPart` and `GrievancePart` separately at every loyalty
reader, each at that reader's own coefficient.

**The clamp had to go, and could never have fired.** `Loyalty.Value` was
`Math.Clamp(0.45·T + 0.30·O + 0.25·B, 0, 1)`. A clamp that binds cannot be split: there is no honest
way to apportion a clamped total among its parts. It could not bind — the three weights total exactly
`1.0`, `Trust` and `Obligation` are clamped to `[0,1]` by `Relations` on every write, and `Belonging`
is a drive documented in the same range, so the sum is always in `[0,1]`. Removing it is what makes
emitting the parts exactly equal to emitting the sum. Pinned by
`The_parts_sum_to_the_bond_across_the_whole_range`, driven at the corners.

**The two affine readers gained an explicit non-relational base.** `Retaliate`'s risk was
`−(1.3 + 2.2·loyalty)·nerve` and policy reluctance was `−(0.6 + 1.2·loyalty)·scale`. The constant is
not relational — moving on anybody is a serious step, and a rule weighs something whoever set it — so
it is now its own component with no facet, and the loyalty-dependent part is split into four.

**`BareValue` was removed.** With Belonging emitted as its own component carrying its own
relationship-free value, nothing read it. A member with no reader goes, which is the rule that removed
`Affection`.

**The diagnostic lists Belonging as context.** `RelationshipComponents()` now returns every component
with any facet, so all four contributions to a loyalty term are visible together. Belonging's
`RelationshipShare` is zero, so it contributes nothing to gross, net or the counterfactual — shown,
never counted. Sweeping Belonging into a figure reported as relational is the exact conflation the
facets were introduced to end.

### Everything preserved, verified against `7a9773b` directly

`7a9773b` was built in a scratch worktree and every variant diffed line by line, excluding only the
reason list and the diagnostic block:

| Variant | Scores, choices, outcomes, world state |
|---|---|
| baseline | **identical** |
| cautious-vincent | **identical** |
| watchful-boss | **identical** |
| disloyal-vincent | **identical** |
| resentful-tommy | **identical** |

All five chosen-action digests are byte-identical — `38B7183ED2EEF34A` / `124E8FE932DD5A89` /
`4F15ECD8B7A593BB` / `3D7F2B79BA4DC3E3` / `18B507EBBE4FBA7E`. Decision counts, "rel. read" (19 / 12 /
18 / 20 / 19) and "rel. decided" (2 / 3 / 3 / 1 / 3) are all unchanged, as is
**5 distinct traces · 5 distinct chosen-action sequences**.

Trace hashes move, and only through the two excluded blocks:

| Variant | `7a9773b` | corrective commit |
|---|---|---|
| baseline | `3BA97219464FC2E4` | `20DD67E8CA4CB5AD` |
| cautious-vincent | `EC664E9FB52010B7` | `D2D070005176426D` |
| watchful-boss | `51AB00158218ACD0` | `6FC6D3243B0020E1` |
| disloyal-vincent | `709F0B6E4B90A2F4` | `5A91CFE9F3532E63` |
| resentful-tommy | `3D91B931EC2DAF3B` | `947BD13F07FE2AEA` |

### One consequence worth stating rather than discovering later

**The readable reason list shows fewer relationship lines: 10 in baseline, down from 14.**
Splitting one component into three makes each smaller, and more of them fall under `Significant()`'s
`0.15` cutoff. Where the trace used to say *"reporting maintains standing with his superior
(relationship effects +0.54)"* it now says *"reporting keeps him right with a man he trusts (+0.25)"*
and drops the obligation and belonging shares.

Nothing is lost — the diagnostic block prints all of them with no cutoff, immediately below. And the
line that survives is more informative than the one it replaced, because it names which binding is
doing the work. But it is a real cost to the narrative quality the architecture document asks that
line to carry, and it is the direct price of the ruling. Recorded so the next person to look at the
reason list knows it was paid deliberately.

### Tests

Six added, **292 total** (was 285). Four are new:
`A_reader_reports_all_four_contributions_as_separate_components`,
`Moving_one_dimension_moves_only_its_own_component`,
`No_component_carries_more_than_one_facet` (across all five variants), and
`The_parts_sum_to_the_bond_across_the_whole_range` (a five-case theory).
`A_partial_report_keeps_both_considerations_and_they_largely_cancel` was rewritten to sum each
consideration across its four facets and to assert the two phrase-sets are disjoint.

Two mutation checks, each caught by exactly the intended tests and then restored:

| Mutation | Result |
|---|---|
| the three bond parts re-fused into one component under a union flag | 3 — the four-component, independent-movement and one-facet tests |
| the candour phrases reused from the standing consideration | exactly 1 — the two-considerations test |

**The first mutation is literally the code as it stood at `7a9773b`.** Those three tests would have
failed against the reviewed commit, which is what makes them a pin on the finding rather than a
description of the fix.

### Verification

Clean build (`dotnet clean` first), **0 warnings, 0 errors**, **292/292 tests**. All five variants
deterministic under `--verify`; `--compare` reports five and five; both viewpoint commands run clean
and carry no diagnostic output.

### Not changed

No coefficient. Ruling 4's exclusions are untouched — no concealment, no `AdvanceConceal`, no
`believedWitnesses`, no denial-outcome change, and no ruling on `0.9 × Loyalty` versus `0.4`. The
deferred list above stands unaltered.

---

## Second correction — Codex findings on `9a29342`

Status: **awaiting Codex review. Not verified.** Nothing above is rewritten, including the
Behavioural-movement table in the first correction whose hashes these findings show to be wrong. The
wrong figures stand where they were written and are corrected here, which is what the append-only rule
is for: a reader must be able to see what the record said, not only what it should have said.

**Two findings, both accepted.**

### 1. The recorded verification hashes were false

The first correction records these as the trace hashes at `9a29342`:

| Variant | Recorded | Actual at `9a29342` |
|---|---|---|
| baseline | `20DD67E8CA4CB5AD` | **`6EB3F6B996CFC631`** |
| cautious-vincent | `D2D070005176426D` | **`A8A1BBD12D5334C2`** |
| watchful-boss | `6FC6D3243B0020E1` | **`DCEDCFF27928266F`** |
| disloyal-vincent | `5A91CFE9F3532E63` | **`E164E0A74E2EC7DC`** |
| resentful-tommy | `947BD13F07FE2AEA` | **`982EC77BD5C253CB`** |

The same wrong figures were copied into `REVIEW_LEDGER.md`'s awaiting-review baseline. Both are
corrected. The **chosen-action digests were right** — `38B7183ED2EEF34A` / `124E8FE932DD5A89` /
`4F15ECD8B7A593BB` / `3D7F2B79BA4DC3E3` / `18B507EBBE4FBA7E` — as were the decision counts and the
rel-read / rel-decided figures, which is the shape of the error: only the rendered trace moved.

**The exact change that occurred after the measurement, established rather than guessed.** The
corrective commit was built in two stages. The split was implemented first and `--compare` was run,
producing the recorded hashes. Then `RelationshipComponents()` was widened from a predicate matching
`Relational` facets or a non-zero relationship share, to `c => c.Reads != RelationshipFacet.None`, so
that the Belonging contribution would be listed in the diagnostic block alongside the other three.
That adds a `[Belonging …]` line to the trace for every candidate carrying one, and the trace is what
`--verify` hashes. **`--compare` was never re-run after it.**

This was confirmed by reverting that single predicate at `9a29342` and re-running: it reproduces
`20DD67E8CA4CB5AD` / `D2D070005176426D` / `6FC6D3243B0020E1` / `5A91CFE9F3532E63` / `947BD13F07FE2AEA`
exactly. Nothing else contributed.

**The lesson is one this milestone had already written down and then repeated.** Its own first
correction records milestone 006's false zero-warning claim — a real build, a real number, measured
before the thing it was reported as measuring. This is the identical shape: a real `--compare`, real
hashes, taken one edit too early and presented as the final state. The widening was a small,
deliberate, late improvement, and *late and small* is exactly the profile of a change that gets
measured before rather than after.

What makes it worse than 006's is that the verification section of that same commit said the change
had been checked. It had — by a line-by-line diff against `7a9773b` that **deliberately excluded the
diagnostic block**, which is the only place the widening shows up. The exclusion was correct for the
question it was asked (did behaviour change?) and it silently made the instrument blind to the one
thing that had. Two checks were run, each sound, and the gap between them was the answer.

**Carrying question:** *was this number measured after the last edit, or merely after an edit?*

### 2. The unclamped bond rested on documentation, not enforcement

`9a29342` removed `Math.Clamp` from `LoyaltyReading.Value`, arguing that the three weights total
exactly `1.0` and every input is already in `[0,1]`. `Trust` and `Obligation` are — `Relations` clamps
every relationship dimension on every write. **`Belonging` was not.** `Psychology` stated the range on
its indexers and enforced it nowhere: neither the public constructor nor `With(Drive, double)` checked
anything.

So the public API admitted values for which `7a9773b` clamped and `9a29342` did not, which makes the
removal a real behaviour change rather than the behaviour-neutral simplification it was recorded as. A
caller passing `Belonging = 5.0` gets a bond of `1.25` where it used to get `1.0`. No scenario does
this, which is why nothing failed — the same "true of every fixture, false of the API" gap the
milestone's own facet work was about.

**The fix, and why this one rather than restoring the clamp.** `Psychology`'s constructor now clamps
to `[0,1]`, and both `With` overloads build a dictionary and delegate to it, so one gate holds every
route in. That makes the stated range an actual invariant at the point the value enters the type,
which is where the range was always claimed.

Restoring `Math.Clamp` on the bond was the alternative and is the wrong fix for this milestone: a
clamp that can bind cannot be split, because there is no honest way to apportion a clamped total among
four separately inspectable parts. Enforcing upstream keeps the parts exactly summable — the accepted
constraint — while making the premise true. Clamping rather than throwing matches `Relations`, which
clamps rather than rejecting; both halves of a loyalty reading now enforce their range the same way.

Traits are clamped by the same gate. The range is stated on that indexer too, and `Utility` reads
traits with coefficients that assume it — `1 - 0.55 * proud` changes sign above `1.8`. Fixing drives
and leaving traits would have been a half-measure on the identical defect.

**Grievance is deliberately untouched by all of this**, per the accepted milestone-008 design: it is
outside the bond, not normalised into it, and free to exceed it.
`Grievance_is_outside_the_bond_and_is_not_clamped_with_it` pins that so a future tightening of the
range cannot quietly sweep it in.

### Tests

Thirteen added, **305 total** (was 292):

- `Psychology_clamps_out_of_range_values_in_its_constructor` — five cases, both ends, traits and
  drives, including values just outside the boundary.
- `Psychology_clamps_out_of_range_values_through_With` — three cases, both overloads, plus a chained
  call so a future `With` that mutated in place rather than delegating cannot slip through.
- `Loyalty_parts_stay_in_range_through_the_public_api` — four cases driving every input far out of
  range at both ends through `Relations.Establish` and `Psychology`, asserting each part stays inside
  its own weight, the bond stays inside `[0,1]` without a clamp of its own, and the parts still sum
  to it exactly.
- `Grievance_is_outside_the_bond_and_is_not_clamped_with_it`.

Two mutation checks, each caught by exactly the intended tests and then restored:

| Mutation | Result |
|---|---|
| the constructor stops clamping | 9 — every constructor, `With` and public-API range case |
| clamped at the lower bound only (`Math.Max(value, 0)`) | 5 — precisely the upper-bound cases, and no others |

**One thing deliberately not claimed.** Reintroducing `Math.Clamp` on the bond is now behaviour-neutral
and no mutation catches it — which is the point of the fix rather than a gap in it. With the range
enforced upstream the clamp cannot bind, so its presence or absence is unobservable. The property that
is pinned is the one that matters: the parts sum to the bond, and the bond stays in range.

### Behavioural movement

**None.** The full rendered trace of all five variants is **byte-identical to `9a29342`**, verified by
building that commit in a scratch worktree and diffing the complete output — not a filtered subset,
which is the mistake finding 1 records. Every cast and variant value was already inside `[0,1]`, so
the new gate clamps nothing that the scenario actually contains.

Hashes are therefore Codex's figures, which are now the recorded ones:

| Variant | Hash | Decisions | Rel. read | Rel. decided |
|---|---|---|---|---|
| baseline | `6EB3F6B996CFC631` | 38 | 19 | 2 |
| cautious-vincent | `A8A1BBD12D5334C2` | 21 | 12 | 3 |
| watchful-boss | `DCEDCFF27928266F` | 39 | 18 | 3 |
| disloyal-vincent | `E164E0A74E2EC7DC` | 39 | 20 | 1 |
| resentful-tommy | `982EC77BD5C253CB` | 38 | 19 | 3 |

`--compare` reports **five distinct traces and five distinct chosen-action sequences**; the
chosen-action digests are unchanged from `7a9773b` and `9a29342`.

### Verification

Clean build (`dotnet clean` first), **0 warnings, 0 errors**, **305/305 tests**. All five variants
deterministic under `--verify`; `--compare` as above; both viewpoint commands run clean and contain no
diagnostic output.

### Preserved

Every accepted milestone-008 constraint holds: one score component per facet; grievance outside the
bond and separately inspectable; Belonging visible in the diagnostic and non-relational in the
counterfactual; **no coefficient changed**; no concealment, denial, witness or milestone-009 work; no
decision forced. The deferred list stands unaltered.
