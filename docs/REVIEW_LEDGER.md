# Crime Empire — Review Ledger

The hand-maintained record of which commits have been reviewed, what those reviews concluded, and
the instruments a review runs against. It is the authority on review coverage up to its stated
checkpoint: within that range, a commit not on the table below has no established status, whatever
prose elsewhere says. Beyond the checkpoint the table is silent rather than authoritative — see the
note above the table.

**This file holds no active status and grants no permission.** What is being worked on lives in
`CURRENT_MILESTONE.md`; what is not yet built lives in `ROADMAP.md`. Nothing here is a licence to
start anything.

Read this before reviewing a commit, alongside the canon list in `AGENTS.md`.

## How review works here

Review is manual and ordered. Nothing runs on a timer and nothing tracks coverage on the
repository's behalf: commits are taken oldest-unreviewed-first, one at a time, by hand, and each
review names the exact commit whose diff was inspected. A later documentation commit does not stand
in for the implementation commit beneath it — if two land back to back, both still need reviewing.

Standing rules:

1. Review advances one commit at a time, oldest unreviewed first. Never skip to `HEAD`, even when
   the intervening commits are documentation-only.
2. Every report names the exact commit whose isolated diff was inspected, and distinguishes
   verification at that commit from verification performed only at a later `HEAD`.
3. Never write "verified" from a review report alone. Verified means Matt confirms acceptance of
   that specific reviewed commit.
4. The coverage table below is the record, through its stated checkpoint. Maintained by hand, which
   is why it is the authority rather than the prose around it.

Test-green is not review, and review is not acceptance. `714fbc3` built clean, passed its suite,
and was still rejected on three P1 findings; do not describe such a commit as unreviewed or as safe.

## Commit and review coverage

**Coverage checkpoint: `46a5651`.** The table is complete through that commit and says nothing
about anything after it. Commits later than the checkpoint have no row yet; their absence means
"not yet recorded here", not "unreviewed". **The milestone-007 closeout commit that moved this
checkpoint is itself later than it, has no row, and still needs reviewing in its turn.**

Two rows added with this move say "status not established", and that is the honest reading rather
than an oversight: the milestone-006 closeout and milestone 007's own rulings-provenance correction
are both documentation commits that nobody has inspected. Milestone 007 was accepted on the strength
of a review of `974a88a`, its implementation commit — not of everything standing beneath it.

A note on the rule below, because this closeout sits close to it. That rule forbids a commit whose
*only* purpose is to record its own review or the review of the commit before it, because such a
commit is manufactured solely to update this table and then tends to go unreviewed. A milestone
closeout is a step `AGENTS.md`'s lifecycle requires and Matt authorized on its own merits — it resets
the current-milestone file and appends the closure record — so folding the outstanding rows in here
is the intended path rather than the regress. The exposure is real all the same: this is a docs-only
commit and exactly the kind that has twice been skipped, which is why the paragraph above says so
explicitly instead of leaving its absence to be inferred.

The checkpoint exists because a tracked file cannot record the review of the commit that contains
it — the row would have to describe an outcome that does not exist until after the commit is
written. There is no version of this table that covers itself. So later review outcomes, and a
moved checkpoint, are folded in during the next change that was authorized on its own merits.
**Never create a commit whose only purpose is to record its own review, or the review of the commit
before it.** That is the regress this checkpoint exists to avoid, and it also manufactures exactly
the kind of commit that has twice gone unreviewed here.

Oldest first — the order review takes them in.

| Commit | What it did | Review status |
|---|---|---|
| `46f0777` | Initial simulation baseline | Reviewed in the reorg pass: the process-global decision-ID defect was found here and fixed in `4030699` (`milestones/001`). |
| `4030699` | Reorganize into `docs`/`src`/`tests`; decision-ID determinism fix | Status not established. `DESIGN_DECISIONS.md` §Stack records that Codex raised the .NET 10 target during this reorg. |
| `65a97c4` | Milestone lifecycle policy; archive milestone 001. Docs only | Status not established. |
| `7032981` | Migrate `net9.0` → `net10.0` (milestone 002) | Reviewed 2026-08-13, **no findings**; accepted as a safe base (`milestones/002`). |
| `5463157` | Record the milestone-002 review outcome. Docs only | Status not established. |
| `097fbda` | Milestone 003 implementation: reports, testimony, viewpoint view | Reviewed and **rejected**: five findings. Corrected by `cf22e5d`. |
| `cf22e5d` | First correction | Reviewed and **rejected**: three findings (6–8). Corrected by `2a74a5d`. |
| `2a74a5d` | Second correction | Reviewed and **rejected**: three findings, two P1 (9–11). Corrected by `f97ef76`. |
| `f97ef76` | Third correction; corroboration runaway | Reviewed and **rejected**: three findings (12–14). Corrected by `b8fe921`. |
| `2d9177d` | Point the review read order at the milestone archive. Docs only | Status not established. |
| `b8fe921` | Fourth correction: separate withheld from unsaid, scope requests | Reviewed and **rejected**: two P1 (15–16). Corrected by `e83dacf`. **Recorded as verified when it was not.** |
| `b3c404b` | Close milestone 003, reset the current-milestone file. Docs only | Status not established. Working tree clean at this commit. |
| `d142582` | Open milestone 004; add the two canonical context briefs. Docs only | Reviewed. Findings, fixed in `fb2c84d`. |
| `fb2c84d` | Correct the continuity record. Docs only | Reviewed and **rejected**; its findings were never recorded. **Retired as superseded** by Matt on 2026-08-14 — see the note below. |
| `e83dacf` | Fifth correction: scope the reply and the asking guard to the claim | Skipped at the time by a latest-commit-only rule; later reviewed and **rejected**: three findings, two P1 and one P2 (17–19). Corrected by `cbadb0d` and `170991b`. |
| `a5a72f1` | Record a milestone-003 verification and unblock 004. Docs only, and wrong | Reviewed. Findings accepted; the false verification was withdrawn in `d2af4c8`. |
| `714fbc3` | Milestone 004 implementation: split `Direct` into four categories | Reviewed and **rejected**: three P1. Corrected by `c828bfa`. |
| `11c4a4a` | Record the 004 implementation commit in its archive. Docs only | Status not established. |
| `d2af4c8` | Withdraw the false verification of `e83dacf`. Docs only | Status not established. |
| `cbadb0d` | Sixth correction (003): direct-answer path | Reviewed, **no code findings**; all verification passed. |
| `170991b` | Enforce the false-candour invariant at `Reporting.Compose` | Reviewed, **no code findings**. One documentation finding — the stale next-step gate — fixed in `d685015`. |
| `d685015` | Replace the stale gate. Docs only | Reviewed, **no findings**. Matt accepted milestone 003 on 2026-08-14. |
| `2893cf1` | Close milestone 003. Docs only | Status not established. |
| `dac4362` | Document ordered Codex review checkpoints. Docs only | **Not reviewed**; explicitly reconciled as an ordered-review checkpoint. It described a review automation that does not exist; that text was corrected under `612bd50`'s second finding. |
| `c828bfa` | First 004 correction: make knowledge travel | Reviewed and **rejected**: three P1 and two P2, chiefly a false denial transmitting the sender's private basis. Corrected by `d783745`. |
| `d783745` | Second 004 correction: claimed basis vs. private basis | Reviewed and **rejected**: two findings — a silent `ActualBasis` default marking honest briefings as misrepresented, and a repeat comparison collapsing `Participant` onto `Witness`. Corrected by `612bd50`. |
| `612bd50` | Third 004 correction | Reviewed and **rejected**: two findings — provenance still settable by halves through an object initializer or `with`, and live documentation claiming a review automation that does not exist. Corrected by `1fe8a15`. |
| `1fe8a15` | Fourth 004 correction: close the last half-set route | Reviewed, **no findings**. Matt accepted it on 2026-08-14. Milestone 004 closed. |
| `20f82bd` | Close milestone 004. Docs only | Reviewed and **rejected**. Corrected by `6cbc385`. |
| `6cbc385` | Correct a next-step gate that had gone stale a second time. Docs only | Reviewed and **rejected**. Corrected by `9703d83`. |
| `9703d83` | Retire `fb2c84d`'s unrecoverable findings as superseded. Docs only | Reviewed, **no findings**. Accepted by Matt. |
| `cdbcff1` | Replace the two canonical briefs with `REVIEW_LEDGER.md` and `ROADMAP.md`. Docs only | Reviewed and **rejected**: three documentation findings — the ledger's impossible "every commit has a row" claim, a completed `OPEN_CONCERNS.md` item-4 cleanup still listed in `CURRENT_MILESTONE.md`'s deferrals, and a build-status snapshot in `CLAUDE.md`. Corrected by `221b5cf`. |
| `221b5cf` | Bound the review ledger's coverage to an explicit checkpoint. Docs only | Reviewed, **no findings**. Accepted by Matt. |
| `2e895a5` | Open milestone 005: Stable Occasion Identity and Strategy Lifecycle Safety. Docs only | Status not established. |
| `f942871` | Milestone 005 implementation: causally local occasion keys, `ConcealIncident` termination | Reviewed and **rejected**: two P1 and three P2 — `ConcealIncident` redundancy scoped to `(Kind, TargetId)` instead of the incident; `ContinueStrategy` disturbing a live pending step; a `StrategyStep` with an unresolvable owner failing silently; a `ConcealIncident` candidate able to start unrecorded; the promised observation-key uniqueness test never written. Corrected by `90ff97c`. |
| `90ff97c` | Correct milestone 005: incident-scoped redundancy, preserved scheduling, explicit executor resolution, fail-closed concealment identity | Reviewed and **rejected**: one P1 documentation finding — `ROADMAP.md` still listed the RNG-keying and `ConcealIncident`-runaway debt as unresolved and offered them as candidate scope 6, and this file's determinism checklist pointed at that stale entry. Corrected by `5e2adc1`. |
| `5e2adc1` | Retire resolved RNG/concealment debt claims and reconcile review coverage. Docs only | Reviewed and **rejected**: one P1 finding — `CURRENT_MILESTONE.md` lines 20–21 still said milestone 005's commit was missing from this table and should be folded in later, although this same commit had already added it and advanced the checkpoint through `90ff97c`. Corrected in the documentation-only pass that follows this commit. |
| `711553c` | Replace `CURRENT_MILESTONE.md`'s stale commit-specific ledger note. Docs only; the correction to `5e2adc1`'s finding | Status not established. |
| `1fe5b9a` | Milestone 006 implementation: perceived account conflicts, `Domain/Relations.cs`, relationship state in both replay comparators | Reviewed and **rejected**: six findings — a grievance collection castable back to something mutable, an absent reading that could be contaminated and reported no `OtherId`, the missing delegator-to-executor account path, absent state-machine tests, an archive citing sixteen rulings it did not contain, and a false zero-warning claim taken from an incremental build. Corrected by `3ddd8a1`. |
| `3ddd8a1` | First 006 correction: relationship immutability, named absent reads, `Generators.FromDelegation` | Reviewed. The original six **accepted as sufficiently addressed**, including the staged delegator proof under ruling 7; **five further findings** — `SeekCorroboration` scored from the weakest unrelated testimony rather than its own `AboutClaim`, a trace claiming the actor was going behind a source that did not exist, undetected duplicate questions across two generators, a false provenance claim about the sixteen rulings, and a recantation test whose name contradicted its body. Corrected by `404b416`. |
| `404b416` | Second 006 correction: claim-specific question scoring, `(kind, target, claim)` deduplication, corrected rulings provenance | Reviewed, **no findings**. **Matt accepted it on 2026-08-15. Milestone 006 closed.** |
| `6355347` | Close milestone 006: record Codex's clean review and Matt's acceptance. Docs only | Status not established. |
| `974a88a` | Milestone 007 implementation: concealment priced on protection newly bought, repetition against a moved listener, a second contested business, structured behavioural digest | Reviewed. **One finding** — adding a sixth character breached the milestone's own "no new characters" exclusion. **Matt accepted it on 2026-08-16** as a bounded scenario-fixture exception, the second business requiring a distinct owner; explicitly not a licence for broader cast growth. **Milestone 007 closed.** |
| `46a5651` | Correct milestone 007's account of where its rulings are recorded. Docs only | Status not established. Its finding was right and its remedy was not — it rewrote an append-only archive header, which the closeout commit restores. |

Milestone 003 was accepted through `d685015`; milestone 004 through `1fe8a15`; milestone 006 through
`404b416`; milestone 007 through `974a88a`. Note the difference in shape: 003, 004 and 006 each took
their implementation plus every corrective round through review before acceptance, while 007 needed no
corrective round to its code and was accepted at its implementation commit with one finding ruled on
rather than fixed. Its two documentation commits are unreviewed and are recorded as such.

**On `fb2c84d`.** It was rejected and its findings were never written down, so what they were is not
recoverable from this repository. Matt retired them as superseded and non-actionable on 2026-08-14:
`fb2c84d` was a documentation commit, and every line it touched has since been rewritten and
re-reviewed several times over in the milestone-004 corrective rounds. Retired is not the same as
fixed, and the row must not be read as either — nobody addressed those findings one by one, and
nobody has claimed the original review passed. The text they were about no longer exists, so there
is nothing left to act on. The rejection stays on the record; only the expectation of further work
is discharged.

Do not squash or rewrite this history to make a milestone look cleaner. The corrective sequences
record useful architectural failures and review lessons.

## Verification baselines

Run from the repository root; the commands are in `AGENTS.md` §Verification.

Hashes are regression evidence for a snapshot, not permanent game-design requirements. A deliberate
behaviour change may legitimately move them if tests and milestone documentation are updated
coherently.

### Accepted — milestone 007, `974a88a`

Codex reviewed it and returned one finding — the sixth character, `nunzio`, breaching the milestone's
own "no new characters" exclusion. Matt accepted it on 2026-08-16 as a bounded scenario-fixture
exception, on the grounds that the second business requires a distinct owner, and stated explicitly
that it authorizes neither broader cast growth nor relaxed scope discipline. See
`milestones/007-scenario-reach.md` and its two corrections.

- Build: **0 warnings, 0 errors — measured after `dotnet clean`.**
- Tests: **276 passed**, 0 failed (240 before the milestone).
- Five variants, deterministic on repeated runs.

| Variant | Hash | Decisions | Reports | Requests | Conflicts |
|---|---|---|---|---|---|
| baseline | `26C7D3195DBCD67F` | 38 | 6 | 5 | 2 |
| cautious-vincent | `F0067A8493E74516` | 21 | 2 | 4 | 3 |
| watchful-boss | `83327839749FE63C` | 39 | 7 | 5 | 2 |
| disloyal-vincent | `837273496CBB7DCC` | 39 | 6 | 5 | 2 |
| resentful-tommy | `09F26760FB80EFB1` | 38 | 6 | 5 | 2 |

`--compare` reports **five distinct traces and four distinct chosen-action sequences**, and names the
convergence. That second figure is new and is the honest one: `resentful-tommy` chooses the identical
action at every decision as `baseline`, which the trace hash alone could never have told you.

**Read the report counts as the milestone's clearest signal.** Baseline fell from eleven reports to
six. Five of the eleven existed only because withholding the same claim from the same man was being
paid for as a fresh gain on every report.

**And read the conflict counts against milestone 006's.** The count rose, but the listener changed:
Salvatore is no longer contradicted at all, and Vincent is contradicted twice. 006's conflict reached
the page only on Vincent's *second* concealing report, and he no longer files it. The mechanism did
not get better at firing; it started firing on the one character who has decisions that read a
relationship. See `milestones/007-scenario-reach.md`.

### Superseded — milestone 006, `404b416`

Codex reviewed it with no findings and Matt accepted it on 2026-08-15. See
`milestones/006-relational-consequence.md` and its two appended corrections. Superseded as the
current accepted state by milestone 007 on 2026-08-16, and kept because it is the last baseline
before the fixture gained a second business and before concealment stopped being paid for twice.

- Build: **0 warnings, 0 errors — measured after `dotnet clean`.** The implementation commit reported
  zero warnings from an incremental build that had not recompiled the test project, and there were
  four. Take the clean build, or the number means nothing.
- Tests: **240 passed**, 0 failed (236 at the first correction, 226 at the implementation commit, 172
  before the milestone).
- **Five** variants; the fifth, `resentful-tommy`, was added by this milestone.
- Replay hashes `527764207C2F93AF` / `3EBD1BD64F24A5CB` / `B896EB976D876B98` / `EB83C979FB8B3DFC` /
  `BCB839C794DF6543` for baseline / cautious-vincent / watchful-boss / disloyal-vincent /
  resentful-tommy, each identical on both runs.
- Decision counts 33 / 16 / 33 / 34 / 33 — unchanged across all three commits of this milestone.

**Three things worth carrying out of these numbers.**

At the implementation commit, all four pre-existing hashes were byte-identical to milestone 005's: a
milestone that added a social consequence and moved trust during every run changed no accepted
history. The conflict edge fires in all five variants and Salvatore's trust in Vincent falls from
0.50 to 0.309 — it simply reaches no later decision. That is the milestone's central finding, not a
clean bill of health.

At the first correction, four hashes moved with decision counts held fixed. The cause is the
delegator's account question joining the candidate set and so appearing in the rendered trace; it was
chosen zero times in every variant, verified directly.

At the second correction, all five moved — including `cautious-vincent`, which had been byte-identical
throughout milestone 005 and 006 until now. That variant has no delegation, but it does use the
ordinary corroboration path, and correcting that path's scoring is the first change in either
milestone that reaches it. **No chosen action or its score changed anywhere**, verified by diffing
every `← chosen` line against the previous commit built in a scratch worktree; all five diffs are
empty. Counts fixed while hashes move is the signature of a scoring-and-wording change rather than a
choice change.

Note also that extending the test comparators cannot move these hashes by construction: `--verify`
hashes the rendered trace, which contains no relationship state. Snapshot coverage makes the tests
stricter and is invisible here.

### Superseded — milestone 004, `1fe8a15`

Kept because it is the last baseline before relationships moved at all. Milestone 006 replaced it as
the current accepted state.

- Build: 0 warnings, 0 errors.
- Tests: 139 passed, 0 failed.
- Replay hashes `B20C06E5838C0657` / `24A181B260F9C396` / `4B60DA962927A6F7` / `B274F395A61C5118`
  for baseline / cautious-vincent / watchful-boss / disloyal-vincent, each identical on both runs.
- Four variants produce four distinct histories. Decision counts 13 / 16 / 13 / 19.

Byte-identical to `c828bfa`: the last three corrective rounds closed real, API-reachable defects
that never fired in these four variants. Correct and currently invisible in play — see
`milestones/004-provenance-precision.md` for why in each case.

### Superseded

Kept for comparison only. The prose analysis of why each moved lives in the milestone archives.

| Commit | Tests | Decisions | Note |
|---|---|---|---|
| `d685015` | 99 | 13 / 16 / 13 / 47 | Milestone 003 accepted. Hashes `EF5082E438500CAA` / `DAB6010D48E61234` / `B351E55B3B2C61DB` / `7F1228BFE32F2108`. |
| `c828bfa` | 120 | 13 / 16 / 13 / 19 | Hashes as in the accepted baseline. Rejected on three P1 and two P2. |
| `714fbc3` | 86 | 13 / 16 / 13 / 45 | Hashes moved; the simulation did not. Rejected on three P1. |
| `e83dacf` | 73 | 13 / 16 / 13 / 45 | Reports 2 / 2 / 2 / 7. Reviewed and rejected. |
| `b8fe921` | 63 | 13 / 16 / 13 / 43 | Recorded as verified at the time; the review in fact returned two P1. |

### The scenario these baselines measure

The harbour scenario: one organization, one contested district, **two** pressured businesses, a
six-person cast. Vincent is aggressive, proud, under revenue pressure and carrying a grievance;
whether he escalates is a scoring outcome, not a scripted event. The five variants are the
falsification fixture —

- `baseline` — Vincent as written;
- `cautious-vincent` — personality changes, situation comparable;
- `watchful-boss` — stronger policy and stronger obligation to Salvatore;
- `disloyal-vincent` — Vincent owes Salvatore little and resents him;
- `resentful-tommy` — Tommy owes Vincent nothing and resents him, while Vincent still trusts Tommy
  (added by milestone 006).

They produce distinct histories, which is what demonstrates that traits and relationships
affect behaviour without directly triggering actions. `disloyal-vincent` is the only variant that
exercises the request channel, so it is the one that moves when the channel changes.

**The distinctness claim now states its own caveat.** `resentful-tommy` still makes the same decisions
as `baseline`; its hash differs only through seeded state reaching the trace summary. Since milestone
007, `--compare` computes a chosen-action digest from structured decision fields and reports trace
distinctness and behavioural distinctness separately — five and four — so the weaker of the two claims
can no longer be read as the stronger one. A future change that made two variants converge
behaviourally is caught by the second figure.

The variant was added to stage an executor denying his own act to his delegator, and still does not
achieve it. The delegator now asks and the executor now answers, in play; he answers honestly, because
he believes the street saw him. See `ROADMAP.md` for what would have to change.

**Also note the second business is never collected from.** Nobody in the organisation knows it is
refusing — deliberately, since that asymmetry is what leaves the capo room to question his own man
rather than be handed a second errand — so its collection path is present in the fixture and
unexercised.

## Load-bearing regression categories

Future changes should retain coverage for:

- Player output contains no truth unavailable to the viewpoint character.
- Unknown characters are not named from the global roster.
- Physical presence is not inferred from provenance that does not establish it.
- Policy authorship is inferred or reported, not observed from violence alone.
- One source cannot repeatedly corroborate or erode confidence with the same account.
- A changed account is not mistaken for repetition.
- Retractions and non-held positions can be composed and delivered.
- Withholding counts as addressing a claim, while length-cap omission stays outstanding.
- Information requests are bounded by `(asker, recipient, claim)`, not merely by the pair.
- No ordered request tuple repeats.
- No two reports between the same pair are content-identical in the bounded scenario.
- All variants remain within explicit decision and report budgets.
- Pause/resume preserves reports, testimony, requests, and the resulting history.
- Every `IsUnmediated()` record is self-sourced, across all variants.
- Relationship state is created and changed only through `Relations`, and reading never creates.
- A perceived account conflict is decided from the listener's side alone — never from the truth log,
  the report log, `ReportedClaim.ActualBasis`, or `Report.Candor`.
- A repeated identical account is not a fresh conflict, and does not cost trust twice.
- The social consequence is applied at every receipt path, not only the report channel.
- Player output never asserts that anyone lied, and never prints a relationship value.
- A collection exposed as read-only cannot be cast back to something mutable.
- An absent-relationship reading names the person it was asked about, creates nothing, and cannot be
  written to.
- A delegator's standing to ask his executor for an account survives the operation finishing.
- Warning counts are measured after `dotnet clean`, never from an incremental build.
- A question is scored from the claim it is about, not from the weakest unrelated belief held.
- A trace explanation never names a source the record does not have, and never says a character is
  going behind somebody when the claim was self-acquired or when the source *is* the person asked.
- Two generators proposing the same `(kind, target, claim)` question offer it once.
- Concealment is worth only the protection a report newly buys, per `(sender, recipient, claim)`,
  read from asserted stance rather than from `Report.Candor`, most recent treatment winning.
- A sender's belief moving may make a claim reportable again; it never refunds protection he has
  already spent.
- Concealment protection is completed per claim before the maximum is taken — never separate maxima
  added, which could combine halves from different claims.
- Identical words are inert unless the listener independently moved since that speaker's preceding
  account, and then count exactly once.
- A report records the question it answers; whether something is a reply is never inferred from
  timing.
- Behavioural distinctness between configurations is computed from structured chosen-decision fields,
  never from rendered trace text, and is reported separately from trace distinctness.
- Business ordering in the harbour is explicit: the grocery sorts first and the first collection cycle
  runs on it.
- A relationship movement that reaches no decision score is not a demonstrated consequence —
  decision-relevance is asserted by a counterfactual through the production scorer, not by the
  movement existing.

## Review checklist

### Architecture

- Does the simulation library remain engine-independent?
- Does decision code read character-relative information rather than truth? Decisions must use
  `PerceivedSituation`, not `World`, for situational facts — do not add a world reference to the
  perceived view or expose raw cognition to candidate generators.
- Do traits influence salience and evaluation without triggering behaviour?
- Are strategies bounded and authored rather than unrestricted plans?
- Are policy breaches possible and consequential rather than mechanically forbidden?

### Determinism

- Are dictionary/set traversals explicitly ordered where they affect outcomes? Adding collection
  traversal without explicit ordering is a hotspot.
- Are IDs allocated from world state rather than process-global static counters?
- Is random state derived from stable inputs? Occasion keys must never be built from
  `ScheduledEvent.Id`, `WorldEvent.Id`, or a `Claim.EventId` derived from the truth-log counter —
  the defect milestone 005 closed. See
  `milestones/005-stable-occasion-identity-and-strategy-lifecycle-safety.md` for what that keying
  looked like, why it was wrong, and the insertion-stability tests that now pin it.
- Does pause/resume produce the same history?
- Is every new piece of future-decision-relevant state included in replay comparison?

### Information safety

- Can player output name anyone the viewpoint does not know? `IntelligenceWriter` may use only
  identities present in the viewpoint character's claims, testimony, relationships, or grievances.
- Can it claim observation or attendance from confidence alone?
- Can an actor infer hidden authorship directly from a visible consequence?
- Can a report communicate a position its sender does not hold? The composer may read only the
  sender's perceived positions, never the truth log.
- Can truth leak through formatting code, scenario fixtures, or helper lookups?

### Report channel

Check the change against every invariant in `DESIGN_DECISIONS.md` §"Information channel — settled
invariants". Those are the contract; this checklist does not restate them. Ask additionally:

- Can bounded composition crowd out a changed position and then incorrectly mark it delivered?
- Can silence create an unbounded ask loop?
- Can concealment create an unbounded partial-report loop?
- Is source independence checked over the whole testimony history?

Review cognition changes as state-machine changes, not ordinary list updates. Walk at least: first
acquisition; identical repetition; independent corroboration; contradiction; recantation;
affirm → deny → affirm; held → doubted/rejected and communicated onward; acquisition time versus
reconsideration time; contestedness after the settled stance changes.

### Tests

- Does each regression test invoke the production rule it claims to pin, rather than a copy of it?
- Has the fix been temporarily reverted to prove the test fails, where practical?
- Does the behavioural budget measure the runaway unit, not only a nearby aggregate?
- Are both baseline and stress/disloyal paths covered?
- Are player-visible leak assertions testing rendered output, not only hidden claim state?

### Documentation and process

- Is the work within the assigned milestone only?
- Are design conflicts surfaced rather than silently resolved?
- Are corrections appended to the milestone archive rather than rewriting history?
- Is the commit focused and independently reviewable?
- Is `CURRENT_MILESTONE.md` reset only after verification and closeout?

## Design review questions

For any proposed feature:

1. What woke the actor?
2. What information did they actually possess?
3. What occurred to them, and why?
4. What was available, and what was ruled out?
5. How did traits, drives, relationships, commitments, and policy affect evaluation without firing
   the action directly?
6. What trace did the outcome leave?
7. Who can observe that trace, under what conditions?
8. How can the player learn it without receiving omniscient truth?
9. What distinct states might this implementation accidentally collapse?
10. What honest behaviour might a proposed safety or correctness filter accidentally remove?

## Recurring failure patterns

Five patterns have produced repeat findings. The detailed cases are in the milestone archives; what
follows is the question each one leaves behind.

**A correctness fix that narrows what can be expressed.** Filtering to held beliefs made retractions
unreportable; treating every repeat sender as a duplicate blocked recantation; matching any
historical account instead of the latest blocked affirm → deny → affirm.
*What honest state or transition can no longer be represented after this fix?*

**A correctness fix that collapses distinct states.** Treating deliberately withheld as never said
caused repeated partial reports; treating a request as person-to-person rather than claim-scoped
permanently closed the channel; treating confidence as provenance produced "personally witnessed"
without evidence of attendance.
*What two different things does this code now treat as one?*

**A correctness fix that stops halfway along the path a value travels.** The request gained a
subject that never reached the event, the reply, or the guard. Milestone 004 then demonstrated the
same shape four times: provenance decided only for new claims; a speaker's private basis travelling
with his lie; two fields that had to move together still movable apart.
*Where else does this value get read, and does the distinction survive the trip?*

**False-assurance tests.** Two tests copied the implementation predicate into the test rather than
invoking the production rule, and passed after the fix was reverted. Rules that need direct pinning
were made testable through production helpers such as `Generators.CanAsk`. Note the limit:
a snapshot-field addition cannot be mutation-checked the same way, since deleting a field merely
weakens the comparator — request actions are also written to the truth log so runner verification
gives an independent deterministic signal.

Milestone 007 found two more of a slightly different shape, and the difference is worth naming.
Neither copied a rule; each asserted against a **relationship the model did not have**. One treated
any report from the asked person to the asker within two days as the reply to a question, and duly
reported a man as having answered something he held no position on. The other required every held
belief to be self-acquired or backed by testimony, which a scenario-seeded belief from a source
outside the cast can never satisfy — it passed only because another character happened to speak about
that same claim later in the run, and stopped the moment that report stopped being filed.
*Is this assertion checking a link the simulation actually records, or one the test is inferring?*

**Recording a review that did not happen.** See below; it is the pattern this file exists to stop.

## How this record has failed

The predecessor of this file misreported status five times. It claimed verification that had not
happened, first for `b8fe921` and then for `e83dacf`; it wrongly recorded `fb2c84d` and `714fbc3` as
never reviewed; and its next-step gate went stale twice — first telling readers milestone 004 was
active and approved, then telling them it was blocked on three unfixed findings after all five had
been corrected and accepted.

Two mechanics made the false verifications easy to produce:

- **Reviews at the time went to the latest commit only.** Two commits landing back to back skipped
  the earlier one permanently and silently. That is how `e83dacf` was missed. Taking commits in
  order is what removes this failure mode — and it is a habit, not a mechanism, so it holds only as
  long as it is kept. An earlier document described an automatic checkpointing monitor; there is
  none, and believing there was is precisely how a reader stops checking whether a review happened.
- **A review report is not proof that a review ran.** It is a document, and like any other observed
  content it can be about a commit nobody inspected. The false `e83dacf` claim was assembled
  entirely from true measurements — real build, real test count, real hashes — and still asserted a
  review that did not occur.

The gate failures are the other half. A gate is prose that grants or withholds permission, so it
goes stale in both directions and is wrong in a way that changes what the next reader does. Sweeps
looking only for "awaiting review" or "not accepted" miss it — the second failure said "not fixed"
and slipped straight through one. When reconciling status, sweep for the sentences that cause
something to happen — "is active", "is approved", "next step is", "is closed" — before the ones that
merely describe. This file carries no such sentence by design; `CURRENT_MILESTONE.md` is the only
place status is stated.

Review question to carry: **which commit did this review actually inspect, and is that the commit
the record is about to call verified?**

## A note on names

The milestone archives are append-only and refer to two documents this file replaced,
`CANONICAL_CODE_REVIEW_CONTEXT.md` and `CANONICAL_DESIGN_CONTEXT.md`. Those references are
historical and correct as history; the files themselves are gone, their unique content divided
between this ledger, `ROADMAP.md`, `DESIGN_DECISIONS.md`, and `OPEN_CONCERNS.md`.
