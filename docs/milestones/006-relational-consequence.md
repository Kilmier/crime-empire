# Milestone 006 — Relational Consequence of a Perceived Account Conflict

Status: **implementation complete, awaiting Codex review. Not verified.** `REVIEW_LEDGER.md` is the
record of when a review happens; nothing here should be read as one having happened.

Scope was proposed against head `711553c`, approved in direction by Matt with eleven binding
revisions, and confirmed with five further rulings before implementation. All sixteen are reproduced
in the scope section of this file's predecessor content below, because several of them narrowed what
was originally proposed and the narrowed version is what was built.

> **Appended note, not part of the text above.** The header and the two paragraphs above are this
> file exactly as it stood at `404b416`, restored. Commit `6355347` rewrote them on closure — it
> replaced the status line and rewrote the ruling-provenance paragraph — and `AGENTS.md` requires
> milestone archives to be append-only, so that was a defect rather than a tidy-up.
>
> Both things `6355347` was trying to say remain true and remain recorded, without editing anything.
> **Milestone 006 is closed and accepted**; the closure record is appended at the foot of this file,
> which is where it belongs. And the claim above that the sixteen rulings are "reproduced in the scope
> section of this file's predecessor content below" **is false** — but it was already corrected inside
> this archive before `6355347` touched it, under *Second correction*, finding 1, which records that
> no committed revision has ever contained them and that the table under the first correction is the
> only copy. Nothing is lost by restoring the original wording, because the correction for it was
> already appended where corrections go.

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

---

## Second correction — Codex findings on `3ddd8a1`

Status: **awaiting Codex review. Not verified.** The original six findings were accepted as
sufficiently addressed, including the staged delegator proof under ruling 7. Five further findings,
all accepted. Nothing above is rewritten.

### 1. The sixteen rulings were never in the repository at all

The first correction said they were "recoverable only from
`git show 1fe5b9a^:docs/CURRENT_MILESTONE.md`." **That is wrong, and it is the same class of error it
was written to fix.** `1fe5b9a^` is `711553c`, which predates the rulings entirely — its
`CURRENT_MILESTONE.md` says no milestone is active. The rulings were written into the working copy
during milestone 006's implementation and reset out of that file inside the same commit, so **no
committed revision has ever contained them**; `git log --all -S` over the ruling text returns nothing.

Their actual provenance: Matt issued them in the review conversation for this milestone — eleven when
approving the scope in direction, five more when confirming it — and the table in the first
correction is a **reconstruction from that conversation**, not a recovery from history. It is the only
copy in the repository. That is worth stating plainly rather than implying a git command would
produce them, because the whole point of reproducing them was that a reader should not have to take
somebody's word for what the constraints were.

### 2. `SeekCorroboration` was scored from the wrong belief

The scorer scanned every testimonial belief the actor held and priced the question off the weakest
one, whatever the question was about. Two consequences, both live:

- A man perfectly confident about the matter in hand scored a large "he is not satisfied with the
  account he has" because something unrelated and thin was sitting in his head.
- Once `FromDelegation` could ask about a claim the actor had established himself, the term had no
  connection to the candidate at all — the delegator's question was priced entirely off an unrelated
  rumour.

The standing cost had the matching defect: `-0.45 * proud` was charged unconditionally with the text
"going behind the man who told him", so a delegator asking the very person named in a claim he found
himself was recorded as going behind a source who did not exist.

Both now read `cand.AboutClaim`. Uncertainty is `1.5 * (1 - confidence)` in the asked claim; the
standing cost applies only when that claim is testimonial *and* the source is somebody other than the
person being asked, and the explanation names them. Putting a question back to the man who told you
is not going around him, which is now also true of the trace.

### 3. Duplicate questions

`GenerateAll` deduplicated by candidate id, and the two generators build different ids for what can
be the same act — same target, same claim. Now deduplicated on `(kind, target, claim)` as well, first
in generator order winning, which is fixed and therefore deterministic. Neither path was removed: a
direct question to the man who did the work and an unsolicited partial report from him are different
acts, and so are corroborating an account you were given and auditing your own executor. Only the
overlap is collapsed, and a test drives both generators onto one claim to prove it.

### 4. The recantation test's name contradicted its body

`A_recantation_by_the_only_source_is_not_a_conflict` asserted, correctly, that it *is* one. The
implementation was right and the description was wrong. Renamed to
`A_source_taking_back_what_he_said_contradicts_the_belief_he_created`, with the reasoning stated:
being told the opposite of your current position is a conflict whoever put you there, which keeps the
rule about the listener's state rather than the speaker's history. It now also asserts that the prior
the conflict names is the one that speaker's own earlier account created.

### Tests

Four added, **240 total** (was 236). Two of them failed on first run for the same reason, which is
worth recording because it nearly produced two more tests that passed for the wrong reason:
`Cognition.Learn` deliberately refuses to lower an existing confidence unless the incoming basis
overrides, so a fixture that seeded 0.6 and then "set" 0.2 silently kept 0.6. Both now build the
belief once, through a parameterised fixture, instead of overwriting it afterwards.

Mutation checks, each caught by exactly the intended tests and then restored:

| Mutation | Result |
|---|---|
| uncertainty scanned from the weakest testimony again | 2 failures — the unrelated-testimony and asked-confidence tests |
| standing cost charged unconditionally | 1 — the trace-explanation test |
| `(kind, target, claim)` deduplication removed | 1 — the duplicate-question test |

### Behavioural movement

**All five hashes move. Decision counts are unchanged** at 33 / 16 / 33 / 34 / 33, and **no chosen
action or its score changed in any variant** — verified by diffing every `← chosen` line, including
its score, against `3ddd8a1` built in a scratch worktree. All five diffs are empty.

| Variant | Before | After |
|---|---|---|
| baseline | `5FD5FE9978E16E0C` | `527764207C2F93AF` |
| cautious-vincent | `0FFCBC7BDE91C001` | `3EBD1BD64F24A5CB` |
| watchful-boss | `346643410DA405F7` | `B896EB976D876B98` |
| disloyal-vincent | `9785E00C1574AD1B` | `EB83C979FB8B3DFC` |
| resentful-tommy | `4E1623AB04752FED` | `BCB839C794DF6543` |

`cautious-vincent` moves for the first time in this milestone, and that is expected rather than
alarming: it has no delegation, so `FromDelegation` never fires there, but it does use the ordinary
corroboration path, and that path's scoring is what finding 2 corrected. It stops being a control for
this change specifically because this change is the first one that reaches it.

The movement is 23 differing trace lines in `baseline` and 14 in `cautious-vincent`, in three groups:

1. **New "what he knew" entries.** Scoring now reads the asked claim through
   `PerceivedSituation.Position`, which records the read, so the belief a question was priced from
   appears in the decision's consulted list. The old code read the raw belief list and marked
   nothing, so the trace scored off information it never declared consulting. This is the trace
   becoming honest, not the character learning anything.
2. **Delegation-question scores** — 0.25 → 0.75, 0.28 → 0.82, 0.69 → 0.76 — now priced on their own
   claim rather than an unrelated one, with the ordering of the weighed-up list shifting to match.
3. **Explanations naming the source**: "going behind the man who told him" → "going behind vincent",
   "going behind the books". Same values, accurate text.

The question still does not win anywhere, and nothing was tuned to make it: at its new score it
remains below the report that beats it, for the reason recorded under the previous correction's
finding 3, which is deliberately still unfixed.

### Verification

Clean build (`dotnet clean` first), **0 warnings, 0 errors**, 240/240 tests. `--verify` deterministic
on all five variants; `--compare` reports five configurations and five distinct histories; both
viewpoint commands run clean.

---

## Closed

Codex reviewed `404b416` with no findings. **Matt accepted it on 2026-08-15. Milestone 006 is
closed.**

### What it delivered

A perceived account conflict — somebody asserting the opposite of a position a character holds — now
costs the listener trust in the speaker. Perceived, never detected: deception, sincere disagreement,
faulty memory and a false prior belief produce the identical shape, and the conflict record is built
entirely from the listener's side, so nothing downstream can react to the truth of the matter because
the truth of the matter is not in it. Directional, trust alone, one rule whatever the prior's
provenance was, and never twice for the same account.

`Domain/Relations.cs` is the only code that can create or change relationship state, enforced by the
concrete type being private to it. Reads never create. Grievances live on the relationship.
`Affection` is gone for having no behavioural purpose. Relationship and grievance state, and the
delegation record that gates the executor question, are in both replay comparators. A delegator can
put a question to the man he sent, and the path from that question to a trust consequence is proven
end-to-end through production code.

### Accepted state

- Clean build, **0 warnings, 0 errors**, **240/240 tests** (172 before the milestone).
- Replay hashes `527764207C2F93AF` / `3EBD1BD64F24A5CB` / `B896EB976D876B98` / `EB83C979FB8B3DFC` /
  `BCB839C794DF6543`, deterministic across repeated runs, five variants producing five distinct
  histories.
- Decision counts 33 / 16 / 33 / 34 / 33 — **unchanged across all three commits of this milestone.**

### Three rounds, and what they have in common

| Round | Against | Outcome |
|---|---|---|
| Implementation | `1fe5b9a` | Six findings |
| First correction | `3ddd8a1` | Original six addressed; five further findings |
| Second correction | `404b416` | No findings. **Accepted.** |

Milestone 004's rounds all found the same defect: a distinction drawn in one place and not carried
through to another. This milestone's have a different shape, and it is worth naming because the habit
that catches it is different. **Every round but the last found a claim that was true of the code and
false of the record.** The grievance collection was read-only in its type name and not in its
behaviour. The archive said it reproduced sixteen rulings it did not contain, then said they were
recoverable from a revision that predates them. The build reported zero warnings from a compilation
that had not read the file with the warnings in it. A test asserted one thing and was named for its
opposite. A scorer priced a question off a belief the trace never admitted consulting.

None of those were logic errors. Each was a description that had drifted from what it described, and
each was invisible to the tests because the tests agreed with the code and only the words disagreed.
The question this milestone leaves behind is therefore not milestone 004's *where else does this
value get read* but: **what does this claim assert, and did anything actually check it?**

### Carried forward

- **The scenario is the binding constraint.** Three consecutive milestones have ended with a
  mechanism the accepted scenario cannot demonstrate. 006's trust edge does fire — Salvatore's trust
  in Vincent falls from 0.50 to 0.309 in every variant — but reaches no later decision.
- **The delegator's question never wins**, at 0.74 against 0.96.
- **Self-protection is re-priced for a concealment already decided**, which is what keeps the report
  beating it. Surfaced, deliberately unfixed, and needing a ruling: it would move every baseline.
- **Trust cannot go negative**; absence of trust and distrust are the same state.
- `Relations.ConflictTrustCost = 0.35` joins the `FirstHandTestimony` and `Discovery` discounts as
  provisional tuning.
- `OPEN_CONCERNS.md` #3 stays open, now carrying this milestone's evidence.

### Relevant commits

- `1fe5b9a` — implementation. `3ddd8a1` and `404b416` — the two corrections. The closeout commit that
  records this acceptance is not cited by hash here, for the reason milestone 001's archive gives.
