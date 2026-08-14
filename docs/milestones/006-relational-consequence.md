# Milestone 006 — Relational Consequence of a Perceived Account Conflict

Status: **implementation complete, awaiting Codex review. Not verified.** `REVIEW_LEDGER.md` is the
record of when a review happens; nothing here should be read as one having happened.

Scope was proposed against head `711553c`, approved in direction by Matt with eleven binding
revisions, and confirmed with five further rulings before implementation. All sixteen are reproduced
in the scope section of this file's predecessor content below, because several of them narrowed what
was originally proposed and the narrowed version is what was built.

## What was attempted

The information channel built by milestone 003 and made precise by 004 terminated in a cul-de-sac.
Accounts travelled, disagreed, eroded confidence and got marked contested — and then nothing
happened. `InformationRecord.Contested` was set in `Cognition.Receive` and read only by
`IntelligenceWriter` for display; no decision consulted it. Meanwhile the social graph those
decisions score against was frozen: across the whole simulation exactly two sites moved a
relationship dimension at runtime, and `Trust`, `Obligation` and `Affection` were written only by the
scenario builder and never moved again.

This milestone closes that edge for one narrow case: a perceived account conflict costs the listener
trust in the speaker, directionally, through a centralized relationship API that is the only code
able to change relationship state.

## What was completed

**`Domain/Relations.cs`** (new) and **`Domain/IRelationship`**. The concrete relationship class is a
`private sealed class` nested inside `Relations`, so outside that class the type cannot be named,
constructed, or cast to; everyone else holds a read-only interface. This was arrived at after a
first attempt got the C# accessibility rule backwards — a nested type can reach its enclosing type's
private members, not the reverse — and the compiler rejected it. The correction is worth recording,
because the failed version would have compiled under an `internal` mutator and left a convention
where the milestone promised a guarantee.

Grievances moved onto the relationship. `AgainstId` was always a relationship key wearing a
different name, and holding them there means they cannot be added behind the API, while
`GrievanceAgainst` becomes a local sum rather than a scan.

**Reads no longer create.** `SocialState.Toward` was a get-or-create called from scoring paths. That
was invisible while relationships sat outside the replay comparison and became a determinism hazard
the moment they entered it — scoring reads a great many relationships that do not exist, so the act
of scoring a candidate would have changed the snapshot. It now returns a shared zero-valued reading.

**`AccountConflict` and `Receipt`** (`Domain/Cognition.cs`). `Receive` returned the record alone, so
the one place that knows a contradiction occurred had no way to say so. It now returns both. The
conflict is assembled entirely from the listener's side — what he held, how he came to hold it, and
what was claimed at him — so nothing downstream can react to the truth of the matter, because the
truth of the matter is not in it. `Cognition` gained no reference to `SocialState`.

The conflict is emitted from exactly the branch that sets `Contested`, which sits after the
verbatim-repeat early return. Non-repetition is therefore inherited from the same guard that stops
repeated denials compounding confidence loss, and the two guarantees cannot drift apart.

**All three receipt paths apply it** — `Org/Reporting.Deliver`, `Commit`'s delegation briefing, and
`Runner.DeliverAssignment`. Applying it in one and not the others would have been this project's most
reliable defect, and would have let a superior contradict a subordinate for free by calling it an
instruction.

**`World.AccountConflicts`**, developer and test state, populated at each of those three sites.
Milestone 005's fifth finding was a promised run-wide property that no test actually checked; this
exists so the properties here are asserted rather than argued from the call sites' structure.

**`Affection` removed.** Declared since the first commit, never read or written by anything in the
simulation, the runner or the tests. Nothing was invented to preserve it.

**Scenario construction routed through `Relations`** in both `Cast` and `Variants`, so there is one
door rather than two.

**A fifth variant, `resentful-tommy`** — see the findings below for what it does and does not do.

**Player-facing rendering** — a `HOW HE TAKES THEM` section giving the viewpoint character's own
attitude outward, qualitatively.

## Tests and results

`dotnet test` — **226 passing** (was 172). Build clean, 0 warnings. Both `--verify` runs deterministic
on all five variants; `--compare` reports five configurations and five distinct histories; both
viewpoint commands run clean.

Five mutation checks, each caught by the intended test and then restored:

| Mutation | Result |
|---|---|
| `RecordAccountConflict` made a no-op | 13 failures across the conflict, receipt-path and run-wide tests |
| verbatim repeats allowed to emit a conflict | 6 failures, including both non-repetition tests |
| `Toward` restored to get-or-create | 6 failures, `Reading_a_relationship_does_not_create_one` and all five run-wide variants |
| assignment-briefing path's conflict handling removed | exactly 1 — its own test |
| delegation-briefing path's conflict handling removed | exactly 1 — its own test |

The last two are the ones that matter for the three-paths rule: each path fails alone, so none of
them is being covered by another's test.

**One limit, stated rather than glossed.** The replay comparators' new relationship lines cannot be
mutation-checked: the snapshot *is* the comparator, so deleting a field from it makes the comparison
blinder without making anything fail. This is the same limitation already recorded for the request
lines, and the same independent signal applies — behaviour that reaches a decision shows up in the
runner's `--verify` hash by a different route.

## Behavioural movement

**All four pre-existing variants are byte-identical to their milestone-005 accepted hashes** —
`5FBD6055D1170D84` / `0FFCBC7BDE91C001` / `C6FAC9C86A966399` / `1A201BB1816562BF` — with decision
counts unchanged at 33 / 16 / 33 / 34. The new variant hashes `4223D4E9F7668C83` at 33 decisions.

Under ruling 10 that is the outcome requiring explanation, and the explanation is the milestone's
central finding.

## Important discoveries

**The conflict edge fires in play, and its consequence has no reader.** Conflicts occur naturally in
every variant — one in each of baseline, watchful-boss, disloyal-vincent and resentful-tommy, and two
in cautious-vincent. In every case Salvatore is contradicted by Vincent about
`BusinessRefusesTribute(bellini-grocery)` at strength 0.546, and his trust in Vincent falls from
**0.50 to 0.309**. The relationship genuinely moves during an accepted run.

And no hash moved, because Salvatore never subsequently scores a candidate that reads
`Loyalty(salvatore → vincent)`. He is the boss: he does not report upward, does not seek approval,
and never delegates to or retaliates against Vincent after the conflict lands. The edge is wired,
correct, exercised — and currently terminates in nobody.

This is the third consecutive milestone to end with a correct mechanism that the accepted scenario
cannot show. It is worth naming as a pattern rather than a coincidence: **the harbour scenario has
one organisation, five people and a single line of causation, and it is running out of room to
demonstrate things.** Milestone 004's provenance distinction, milestone 005's concealment
termination, and now this trust edge are all proven only in isolation. The next scope decision should
weigh that directly — the constraint is no longer the mechanisms, it is the fixture.

**Behavioural relevance is proven by the staged boundary case, per ruling 7**, and it says something
worth keeping. `Utility` prices retaliation risk as `-(1.3 + 2.2 * loyalty)`; loyalty derives from
trust; so a boss who contradicts an account his capo holds makes moving against himself cheaper, and
at the margin that is the difference between the capo sitting on it and the capo acting. Nobody wrote
a rule connecting a disagreement to a betrayal. It falls out of a trust edge feeding a derived value
that a risk term already read — which is the kind of thing the emergence prototype exists to produce.

**`resentful-tommy` does not do what it was added for, and is named accordingly.** It was intended to
stage an executor denying his own act to his delegator — milestone 004's central distinction, still
provable only in unit tests. It does not. Tommy never gives Vincent an account at all: the only
character who puts a question is Salvatore, and being asked redirects the answer to the asker, so the
soldier's account goes to the boss and never to the capo who sent him. That is structural, not a
matter of degree — no configuration of trust, obligation or grievance changes who asks. It was
originally named `denying-tommy` and renamed once this was understood, because a fixture whose name
asserts something it does not do is worse than no fixture.

It is kept because the directional asymmetry it encodes — Vincent trusts Tommy, Tommy does not trust
Vincent — is a useful fixture and becomes live the moment a delegator-to-executor question path
exists. **Read `--compare`'s "five distinct histories" with that in mind:** `resentful-tommy` makes
the same decisions as baseline and differs only in seeded state that reaches the summary. The
distinctness check is weaker than it reads.

**A test of mine was wrong and the diagnostic caught it.** `A_full_run_creates_no_relationships_by_reading`
originally asserted that every stored relationship has a non-zero dimension, and ran against one
variant. A conflict with somebody you have no relationship with legitimately creates one and can
legitimately leave it at zero, because trust is floored at zero — `cautious-vincent` contains exactly
that case, in `salvatore → tommy`. The assertion conflated "created by an event" with "created by a
read" and passed only because the variant it ran against did not contain the case. It now allows
conflict-created relationships explicitly and runs against all five variants.

**Trust cannot go negative.** A stranger who contradicts you lands at zero, indistinguishable from a
stranger you have never met. Distrust as a distinct state from absence-of-trust is not representable.
Not fixed here — the range is pre-existing and changing it is a schema decision for the design pass.

## Deferred work

- **The scenario is the binding constraint**, per the discovery above. Whatever comes next should
  probably address the fixture's reach rather than adding another mechanism to it.
- **A delegator-to-executor question path**, without which `resentful-tommy` stays inert and
  milestone 004's distinction stays unobservable in play.
- **Negative trust**, or an explicit decision that absence and distrust are the same state.
- `ConflictTrustCost = 0.35` is provisional tuning, labelled as such at its definition. Nothing
  distinguishes it behaviourally from 0.25 or 0.45.
- Everything carried forward from milestone 005 is unchanged: the concealment MVP rule, its
  termination being unproven in play, the empty-domain label, and the
  `FirstHandTestimony`/`Discovery` suspicion discounts.
- `OPEN_CONCERNS.md` #3 is updated with this milestone's evidence and **not retired**. The
  relationship-design document remains a possible milestone 007, not an authorized one.

## Relevant commits

- The implementation commit that introduced this file. Not cited by hash, for the reason milestone
  001's archive gives: a commit cannot contain its own hash.
  `git log --diff-filter=A -- docs/milestones/006-relational-consequence.md` resolves it.

---

## Correction — Codex findings on `1fe5b9a`

Status: **awaiting Codex review. Not verified.** Nothing above is rewritten; the account of what the
milestone originally did stands, including the parts these findings contradict.

Six findings, all accepted.

### The sixteen rulings, reproduced

The account above claimed all sixteen were "reproduced in the scope section of this file's
predecessor content below." **They were not.** They lived only in `CURRENT_MILESTONE.md`, which the
same commit reset, so the rulings this milestone was built to were absent from the repository
entirely — recoverable only from `git show 1fe5b9a^:docs/CURRENT_MILESTONE.md`. That is the first
thing this correction fixes, because an archive citing constraints nobody can read is not a record.
Each is mapped to what became of it.

| # | Ruling | Outcome |
|---|---|---|
| 1 | The trigger is a perceived account conflict, not a detected lie. No relationship logic may consult truth, `ActualBasis`, or private candour. | **Completed.** Enforced by the argument type: `RecordAccountConflict` takes an `AccountConflict` assembled from the listener's side only. |
| 2 | The consequence is directional — the listener's relationship toward the speaker. No symmetric change. | **Completed.** Pinned by `Only_the_listener_relationship_moves`, and again end-to-end by the delegator test added here. |
| 3 | Relationship influence stays scoring-only. No change to candidate generation or salience. | **Completed, and re-checked under this correction.** The new `FromDelegation` generator keys on delegation state and a held claim; no relationship dimension gates, orders, or scores it. |
| 4 | `Cognition` returns a structured conflict outcome and stays independent of `SocialState`. | **Completed.** `Receipt`/`AccountConflict`; `Domain/Cognition.cs` references no social type. |
| 5 | The centralized API must be genuinely authoritative: reads must not create, runtime mutation cannot bypass, grievances included, ordered enumeration. | **Completed for creation, mutation and ordering; incomplete as shipped for immutability, corrected here.** See findings 1 and 2. |
| 6 | Both replay snapshots gain all future-decision-relevant relationship and grievance state. | **Completed, and extended here.** `DelegatedExecutorIds` gates generation and is now in both comparators. |
| 7 | Behavioural relevance proved by counterfactual or staged boundary case, never by tuning until a natural variant flips. | **Completed.** The retaliation boundary case. Re-affirmed here: the delegator question was not tuned to win, and does not win. |
| 8 | Player output exposes only the viewpoint character's own attitude or observable behaviour; two viewpoints tested. | **Completed.** Four viewpoint cases; no accusation, no number. |
| 9 | Remove `Affection` if purposeless; do not invent behaviour to preserve it. | **Completed.** Removed. |
| 10 | Baseline movement authorized only from snapshot coverage, centralized consequences, or the new variant. | **Completed originally** (four hashes byte-identical). **Extended by this correction** — see Behavioural movement. |
| 11 | `OPEN_CONCERNS.md` #3 updated with evidence, not retired; the design document is a possible 007, not authorized. | **Completed.** |
| 12 | A new, non-repeated conflict applies a directional Trust consequence; no automatic grievance. | **Completed.** |
| 13 | No separate social penalty for self-acquired versus testimonial priors; preserve provenance, test both paths, one rule. | **Completed.** |
| 14 | The coefficient is provisional tuning, labelled as such; may use actor-visible strength only. | **Completed.** |
| 15 | Initial relationship construction in `Cast` and `Variants` routes through `Relations`. | **Completed.** |
| 16 | A mechanical `Utility` edit to call a non-creating read is in scope; changing scoring semantics is not. | **Completed.** No scoring semantics changed, then or now. |

### 1. `IRelationship.Grievances` was read-only by politeness

`IReadOnlyList<T>` is an interface, not a guarantee. The property returned the backing
`List<Grievance>` as one, so any caller could cast it straight back and add to it — and the
absent-relationship reading was a single shared instance, so a contaminated one would have followed
every character in the world around, invisibly. Both are closed: grievances are exposed through a
cached `ReadOnlyCollection<Grievance>` that cannot be cast to the list, and the absent reading is no
longer shared (finding 2). Three regression tests attempt the bypass directly, including through
`IList<Grievance>` — which the wrapper does still implement, unavoidably, and where every mutating
member now throws rather than quietly succeeding.

### 2. Absent reads reported `OtherId = ""`

The shared sentinel could not say who it was about, so every read about somebody unknown claimed to
be about nobody. `Relations.Absent(otherId)` now returns a fresh unstored reading carrying the id
asked about. Nothing is stored, so reads still create no state; the mutation guard keys off an
explicit `Stored` flag rather than reference equality with a sentinel that no longer exists.

### 3. The delegator-to-executor account path

Implemented as `Generators.FromDelegation`: a delegator may ask the man he sent for an account of a
claim naming that executor. Deliberately separate from the corroboration generator, which refuses
anything the asker established himself — right for corroboration, wrong here, since a delegator
finding traces of his own man's work is not confirming his eyes but asking for an account of work he
ordered, which is exactly the case where the executor has reason to shade it.

**The first implementation was wrong in a way worth recording.** It read the live strategy's
`DelegatedToId`, so the standing to ask existed only while the operation was running — precisely the
window in which a man is too busy to ask. The question was offered only in competition with carrying
on, lost by four points every time, and had evaporated by the time he was free. Being owed an account
does not stop being true when the job finishes, so `ExecutionState.DelegatedExecutorIds` now records
it durably and never clears it.

**What this achieves, stated exactly.** The path exists, fires at the right moments, and carries an
executor's contradiction all the way to his delegator's trust — proved end-to-end through production
code by `An_executor_who_denies_it_costs_himself_his_delegators_trust`, which forces only the
delegator's *choice to ask* and lets the generator, `Commit`, the event queue, the executor's own
deliberation, `Reporting`, `Cognition.Receive` and `Relations` do everything else.

**What it does not achieve.** The contradiction still does not occur in the accepted scenario at seed
42. The question is now genuinely competitive — 0.74 against 0.96 in `resentful-tommy`, 0.74 against
0.89 in `disloyal-vincent` — but never wins, in any of the five variants. Per ruling 7 and this
correction's own instruction, nothing was tuned to change that.

Two causes, and the second is a new finding rather than a limitation:

- **Tommy would conceal rather than deny even if asked.** `ResolveViolence` leaves him inferring that
  people saw him, and `Utility` prices a denial almost entirely on that belief, so he withholds. That
  is the model working correctly — a man who thinks the street watched him does not tell his capo it
  never happened — and it is why the end-to-end test stages a Tommy without that belief. The denial
  still has to win its own utility competition there, which the test asserts rather than assumes.
- **The report that beats it is over-valued, and nobody has ruled on this.** "Report to Salvatore,
  leaving out his own part" wins by ~0.15 on a `+1.50` self-protection term for withholding
  `PersonBreachedPolicy` — a claim Vincent has already withheld from Salvatore on every previous
  report. `Reporting.LastAddressed` correctly treats a withheld claim as settled for *eligibility*;
  `Utility` still prices the concealment as freshly at stake each time. It is the same shape as the
  repeated-partial-report bug milestone 003 fixed, with the scoring half left undone. **Not fixed
  here** — outside these six findings, it would move every baseline, and silently widening a
  corrective pass into a behaviour change is how this project's scope discipline fails. Surfaced for
  Matt and Codex; it is the single thing standing between the delegator's question and the accepted
  scenario exercising it.

### 4. The archive's account of `resentful-tommy` was wrong

The account above says Tommy "never gives Vincent an account at all." He gives him three — Partial
reports on 04-09, 04-13 and 04-22, each withholding the beating. What never happened is a
*contradiction*, because withholding asserts nothing. The structural claim built on it — that a
soldier's account goes to the boss and never to the capo who sent him — was therefore also wrong: it
is true of *answers to questions*, which are redirected to the asker, and false of volunteered
reports. `ROADMAP.md` and `CURRENT_MILESTONE.md` carried the same error and are corrected.

### 5. Relationship state-machine coverage

Recantation and affirm → deny → affirm now have dedicated tests asserting both halves — how many
conflicts were emitted and where trust ended up — because an emission that applied no consequence and
a consequence applied without an emission are different defects.

One of them found the obvious expectation wrong. Affirm → deny → affirm against a *firmly held*
position yields **one** conflict, not two: the denial erodes without displacing, so the listener still
holds the claim when the speaker comes back round, and agreeing with what somebody already thinks is
not contradicting them. A companion test covers the case where the denial does displace — a weaker,
testimonial prior — and there the return trip is a genuine second conflict. Both are pinned rather
than the intuition.

### 6. The zero-warning claim was false

The account above reports "Build clean, 0 warnings." **That was measured on an incremental build that
did not recompile the test project.** A clean build reported `xUnit2029` at
`RelationalConsequenceTests.cs:312`, and fixing it surfaced three further `xUnit2031` warnings the
same incremental build had hidden. All four are fixed and the figure is now measured after
`dotnet clean`.

The lesson is one this repository keeps relearning: a measurement is only as good as what it actually
ran. The build was real and the number it printed was real, and it was still not the verification it
was presented as — the same shape as the false review claims `REVIEW_LEDGER.md` records.

### Tests

Ten added, **236 total** (was 226): three attempting the grievance and absent-reading bypasses, one
on absent-read identity, three on the delegator path including the end-to-end conflict, and three on
the state machine. Four mutation checks, each caught by exactly the intended tests and then restored:

| Mutation | Result |
|---|---|
| grievance list handed out unwrapped | 2 failures — both bypass tests |
| absent reading loses the requested id | exactly 1 — its own test |
| delegator question removed | 3 failures — both path tests and the end-to-end conflict |
| standing to ask tied back to the live strategy | exactly 1 — the survives-completion test |

### Behavioural movement

Decision counts are **unchanged**: 33 / 16 / 33 / 34 / 33, exactly as at `1fe5b9a`. Four of five
hashes move.

| Variant | Before | After |
|---|---|---|
| baseline | `5FBD6055D1170D84` | `5FD5FE9978E16E0C` |
| cautious-vincent | `0FFCBC7BDE91C001` | `0FFCBC7BDE91C001` — **unchanged** |
| watchful-boss | `C6FAC9C86A966399` | `346643410DA405F7` |
| disloyal-vincent | `1A201BB1816562BF` | `9785E00C1574AD1B` |
| resentful-tommy | `4223D4E9F7668C83` | `4E1623AB04752FED` |

The cause is named and is the delegator's question: it joins the candidate set, so it appears in the
rendered "weighed up" list that `--verify` hashes, and can displace an entry from the crowded-out
list. **It was chosen zero times in every variant**, verified directly, so no action changed anywhere
— counts holding fixed while hashes move is the signature of a candidate-set change rather than a
choice change.

`cautious-vincent` is byte-identical again, for the same structural reason as in milestone 005:
Vincent never delegates in that variant, so the generator has nobody to offer a question about. It
remains the clean control.

This movement is **not** authorized by ruling 10 as originally written, which named only snapshot
coverage, centralized consequences, and the new variant. It traces entirely to finding 3, which this
correction itself instructed — so the ruling is extended rather than breached, and the extension is
recorded here rather than assumed.

### Verification

Clean build (`dotnet clean` first), **0 warnings, 0 errors**, 236/236 tests. `--verify` deterministic
on all five variants; `--compare` reports five configurations and five distinct histories; both
viewpoint commands run clean.

### Still not fixed, and not caused here

Everything under "Deferred work" above stands, with two additions: the repeated self-protection
scoring defect described under finding 3, and the fact that the delegator's question does not yet win
in the accepted scenario. This was a corrective pass against six findings, not a reopening of scope.
