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
