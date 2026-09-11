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

### From milestone 010 onward, review is self-assessment

**Matt withdrew Codex from the loop on 2026-08-16, until further notice.** Claude implements and
reviews its own work.

**Updated 2026-08-18, second time: Matt delegated the review function to Claude while Codex is
unavailable, with a Codex round expected about two days later.** That creates a state the four
standing rules did not have a word for, so it gets one. **"Cleared to build on" is not "accepted."**

- *Cleared to build on* means the author reviewed his own work, recorded the findings, and Matt
  authorised the next milestone to proceed on top of it. It unblocks work. It establishes nothing
  about correctness.
- *Accepted* still means what rule 3 says: Matt confirms a named commit. Nothing Claude writes can
  produce it.

The two must never be collapsed in a row, because the whole point of the distinction is that the
first is cheap and reversible and the second is not. A commit cleared-to-build-on that Codex later
rejects is an ordinary outcome, not a contradiction — and everything built on top of it inherits the
rejection.

**Updated 2026-09-04: Codex ran out of usage mid-milestone.** Milestone 020 got two Codex rounds —
both FAIL, both a P1 — and then none on the correction that answered the second, which Matt accepted
and closed on the author's own work. This is the "adversary that arrives long after the work" state
below, in its sharpest form yet: not a milestone that went unreviewed, but a *correction chain cut off
partway*, where the two data points available say the adversary was finding something every time. The
consequence for the next milestone is not a rule change; it is that **`34cd117` is the oldest
unreviewed commit in the repository and everything built after it inherits whatever a review of it
would find.** See the milestone 020 baseline section for the full accounting.

**Updated 2026-09-08: that backlog has since been reviewed, oldest first, and Codex is active again.**
`34cd117`, `c25129a`, `826b1e2` and `8e6878e` are no longer unreviewed — see the milestone 020 section's
own 2026-09-08 update for the outcome, which moves milestone 020's accepted state from `c25129a` to
`8e6878e`. Milestones 021 and 026 have each since gone through full correction-and-review cycles and
been accepted at their final commits; see their own sections. The claim above is left standing rather
than edited: it was true when written, and the record of the gap it describes is part of what this
update explains closing.

**Updated 2026-08-18: Codex is intermittent rather than withdrawn.** Matt intends a Codex round on
`6a8a765` — milestone 011 — at a later date. So the standing arrangement is not "no adversary" but
**an adversary that arrives long after the work**, which is a different problem and a worse one to
reason about: a self-review declared clean can sit unchallenged across several milestones before
anything contradicts it. Two rules follow. **A milestone accepted on a self-review alone and one later
confirmed by Codex do not have the same standing, and the rows must not read as though they do** —
milestone 010 is the first kind and 011 is expected to become the second. And **a late review still
takes commits oldest-unreviewed-first**: `6a8a765` is followed by documentation commits, and reviewing
straight to `HEAD` is exactly how `e83dacf` was permanently skipped. See "How this record has failed". The rows below are the argument for treating that as a weakened process rather
than an equivalent one: across milestone 009, Codex returned **nine findings in five rounds** on work
already declared verified, including the same P1 three times. A self-review then found four more —
but only after the method changed. Re-reading the code found nothing; dumping what a player actually
sees, across every variant and character, found four defects immediately.

So the standing method for a self-review is **not vigilance, it is refusing to trust a reading**:
enumerate the real surface empirically and diff it, mutation-check every fix by reverting it and
watching a test fail, test for the *kind* of defect rather than the reported instance, and walk the
recurring-failure list below as an explicit checklist. Findings go into the milestone archive whether
or not they flatter the author.

**What this cannot replace is an adversary who does not share the author's assumptions**, and every
one of those nine findings was a place the author had convinced himself. A self-review that returns
no findings is weak evidence, and must be recorded as what it is.

**Milestones 011 and 012 rest on a weaker basis than their "accepted" status alone conveys, and this
is recorded permanently rather than left implicit in the story above.** Both were self-reviewed first
and Codex-reviewed only after landing — an adversary that arrived, per the paragraph above, but
arrived **four milestones late**, across a window in which each was cleared to build on with nothing
but the author's own reading standing behind it. Codex's own findings on that delayed pass (recorded
at the `6a8a765` and `3c86ba4` rows) were real: `AdvanceInvestigation`'s owner/executor confusion, a
reconsideration-stamp test that never checked the reconsideration stamp, a docstring contradicted by a
later milestone. All three were the kind of defect this file's own recurring-failure list already
named — a false-assurance test, a distinction quietly narrowed — and a self-review that had already
walked that list did not catch them.

**What milestone 013's instruments do and do not make up for.** Coverage accounting (`docs/COVERAGE_ACCOUNTING.md`)
answers a narrower question than "was this reviewed correctly": *what surface has never been
exercised at all*, which is necessary for catching an unreached branch but has nothing to say about a
reached branch computing the wrong thing — exactly the shape of all three `3c86ba4` findings, each of
which ran and passed its own (wrong) assertion. Per ruling 5, coverage does not replace an adversary,
does not prove exercised code does what the author thinks, and does not catch the author's framing
being wrong. It closes one specific gap — an unexercised region sitting unnoticed for ten milestones,
which is exactly how `AdvanceInvestigation`'s cold-trail branch was found before this milestone
existed — and leaves the rest of what only Codex's different priors have ever caught exactly as open
as it was.

## Commit and review coverage

**Coverage checkpoint: `824f3fc`.** The table is complete through that commit and says nothing
about anything after it. Commits later than the checkpoint have no row yet; their absence means
"not yet recorded here", not "unreviewed". **The commit that moved this checkpoint is itself later
than it, has no row, and still needs reviewing in its turn.**

It advanced from `7e0700e` once the ordered backlog was worked through — `3f08685` was the oldest
uncovered commit and blocked everything behind it — then to `7ca7819` when Matt accepted milestone
009, and to `824f3fc` when he accepted milestone 010 on 2026-08-18. **Every row from `c0bb60f`
onward was produced without Codex**, and their status rests on Matt's acceptance and a self-review;
the rows say so rather than leaving it to be inferred.

**Every commit in the range `6355347`–`7ca7819` inclusive has an established outcome.** The bound
matters: "onward" would claim an outcome for commits that do not exist yet, which is the same
open-ended promise this file's first version made when it said every commit had a row. Older rows
before that range carry "status not established"; that is longstanding and untouched here.

Milestone 007 was accepted on the strength of a review of `974a88a`, its implementation commit. Read
the six rows in its group together rather than singly: one implementation commit accepted, and five
documentation commits of which four were rejected.

**Milestone 008 was accepted on the strength of a review of `7e0700e`, its second corrective commit —
not of its implementation commit.** Its code went through three review rounds: `7a9773b` rejected on
one finding, `9a29342` rejected on two, `7e0700e` clean and accepted. That is the shape milestones
003, 004 and 006 had and 007 did not, and the difference matters when reading the rows: for 008,
*implementation commit reviewed* and *milestone accepted* name different commits.

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
| `6355347` | Close milestone 006: record Codex's clean review and Matt's acceptance. Docs only | Reviewed and **rejected**: one P1 — it rewrote the header and introductory status text of `milestones/006-relational-consequence.md`, which `AGENTS.md` requires to be append-only. Milestone 006's acceptance itself is unaffected and stands on `404b416`. Corrected by the commit that follows `53e912e`. |
| `974a88a` | Milestone 007 implementation: concealment priced on protection newly bought, repetition against a moved listener, a second contested business, structured behavioural digest | Reviewed. **One finding** — adding a sixth character breached the milestone's own "no new characters" exclusion. **Matt accepted it on 2026-08-16** as a bounded scenario-fixture exception, the second business requiring a distinct owner; explicitly not a licence for broader cast growth. **Milestone 007 closed.** |
| `46a5651` | Correct milestone 007's account of where its rulings are recorded. Docs only | Reviewed and **rejected**: its finding was right and its remedy was not — it rewrote the header of an append-only milestone archive instead of leaving the original standing and correcting it alongside. Corrected by `1c6889f`. |
| `1c6889f` | Close milestone 007: restore the archive header, record the review and its bounded acceptance. Docs only | Reviewed and **rejected**: one documentation finding — it recorded `46a5651` as "status not established" when that commit had in fact been reviewed and rejected, and that very review is what produced `1c6889f`. A false review-history claim, which is the failure this file exists to stop. Corrected by `53e912e`. |
| `53e912e` | Record the real review history of milestone 007's documentation commits. Docs only | Reviewed, **no findings**. **Matt accepted it.** He also directed that no commit be made merely to record this review, so it was folded in at the next change authorized on its own merits — which is the rule below working as intended rather than an oversight. |
| `6ba0737` | Restore milestone 006's archive header and reconcile review coverage. Docs only | Reviewed and **rejected**: three findings, two P1 and one P2 — the corrective note was inserted below the restored intro instead of appended at the end of the file, so the archive still carried a hunk above EOF; "every commit from `6355347` onward has an established outcome" was unbounded and claimed an outcome for commits that do not exist yet; and the recurring-failure section still said "Five patterns" after a sixth was added. Corrected by the commit that follows this one. |
| `b8e5ed4` | Append milestone 006's corrective note at EOF and bound the coverage claim. Docs only | Reviewed, **no findings**. **Matt accepted it.** It was deliberately left beyond the checkpoint at the time, to be folded in at the next independently authorized documentation change rather than by a standalone bookkeeping commit — which is this closeout. |
| `7a9773b` | Milestone 008 implementation: facet-tagged relationship contributions, grievance unbundled from the clamped loyalty, developer-facing diagnostic, `docs/RELATIONSHIPS.md` | Reviewed and **rejected**: one finding — ruling 3 required trust, obligation, Belonging and grievance to be separately inspectable, and three of the four were emitted fused into a single component tagged `Trust \| Obligation \| Belonging`. Separately computed, then reassembled at the emission site. Matt accepted the finding. Corrected by `9a29342`. |
| `9a29342` | First 008 correction: one component per facet at every loyalty reader | Reviewed and **rejected**: two findings — the verification hashes recorded in the archive and this file were false, having been measured before a late widening of the diagnostic listing and never re-measured; and the unclamped bond it introduced rested on a `[0,1]` range that `Psychology` documented on its indexers and enforced nowhere, so the clamp removal was a real behaviour change for any out-of-range caller. Matt accepted both. Corrected by `7e0700e`. |
| `7e0700e` | Second 008 correction: true hashes with the cause established, and range enforcement in `Psychology`'s constructor | Reviewed, **no findings**. **Matt accepted the corrected milestone 008 implementation. Milestone 008 closed.** |
| `3f08685` | Close milestone 008: record the review history and advance the checkpoint. Docs only | Reviewed and **rejected**: one P2 — this file claimed "Milestone 008 is the first whose code was rejected twice", which the rows above it disprove: milestone 003 had its implementation and five corrective rounds rejected, and milestone 004 four. A superlative asserted about the table containing its own refutation. Matt accepted the finding. Corrected by the commit that moved this checkpoint. |
| `901d345` | Milestone 009 implementation and archive: Godot playable shell, session boundary, prepare/resolve split | Reviewed and **rejected**: three findings, one P1 — `PendingDecision.Occasion` passed `ScheduledEvent.Cause` straight through, so a `StrategyBlocked` or `StrategyComplete` handed the owner of a *delegated* operation its outcome before anybody had told him; the player-facing DTOs backed `IReadOnlyList<T>` with castable `List<T>` and left raw `Claim`/`EventId` reachable; and the Godot self-test printed `CE-SELFTEST FAILED` while exiting 0. Matt accepted all three. Corrected by `b4900aa`. |
| `b4900aa` | First 009 correction: source-limited occasion, opaque immutable boundary, self-test exit code | Reviewed. The original three **confirmed fixed** and all verification passing; **one further P1** — `Generators.FromRelationship` still picked its corroboration target out of `ctx.OrgMemberIds`, the authoritative roster, without establishing that the actor knew that person existed, and `PlayerOption` then rendered the name. Matt accepted it. Corrected by `c447a23`. |
| `c447a23` | Second 009 correction: belief-limited corroboration targets | Reviewed and **rejected**: two findings. **The same P1 again** — the correction narrowed the roster by knowledge and widened it back by "office relationships" derived from `Pipeline.SuperiorOf`/`SubordinatesOf`, which are authority scans over that same roster, so a same-organisation stranger one rung below the actor stayed reachable and renderable; and none of its three tests could see it, one having compared `PlayerView.KnownPeople` against the function it already delegated to. Plus a documentation contradiction: this file recorded `cautious-vincent`'s moved baseline in one place and "nothing moved / all 30 identical" in another, and `CURRENT_MILESTONE.md` called milestone 009 both twice-rejected and "not reviewed". Matt accepted both. Corrected by `49b71a6`. |
| `49b71a6` | Third 009 correction: `Acquaintance.KnownTo`, an office rather than an authority rung | Reviewed. **No behavioural findings**, and its verification passed; **two documentation findings**, one P1 — `CURRENT_MILESTONE.md`, `DESIGN_DECISIONS.md` and `ROADMAP.md` still described the live rule as the rejected second correction had left it, naming `HeardOf` rather than `KnownTo` — and one P2 on the matching source comments. Matt accepted both. Corrected by `0f52d75`. |
| `0f52d75` | Reconcile the live rule across the canon documents and the source comments. Docs and comments only | Reviewed by Codex, **no findings**; behavioural verification passed. |
| `c0bb60f` | Correct a false superlative; advance the checkpoint through `0f52d75`. Docs only | **Not reviewed by Codex** — it was the last commit before Codex was withdrawn. Covered by Matt's acceptance below. |
| `7ca7819` | Fourth 009 correction: `Relations.Meet`, a truthful occasion, a derived focus, a self-test that presses buttons | **Not reviewed by Codex.** Produced by a self-review at Matt's request, which found four defects — two of them the same P1 Codex had raised twice, still live on generators the fix had not reached and invisible to a test scoped to the one that had. **Matt accepted milestone 009 on the strength of this commit on 2026-08-16. Milestone 009 closed.** |
| `12d1054` | Close milestone 009 as accepted; open milestone 010. Docs only | Status not established. |
| `6a8a765` | Milestone 011 implementation: the detective's move, incident-scoped investigation, pronouns | **Self-reviewed by its author on 2026-08-18, three findings** — a coverage gap predating the milestone (`resentful-tommy` in neither insertion-stability theory), a ruling asserted but never tested (actor parity on the new allegation route, which holds), and a legibility wart that is carried. Corrected by the commit that carries this row. **Reviewed by Codex on 2026-08-19 (reported by Matt), one further finding** — `AdvanceInvestigation` read and wrote the strategy owner's cognition throughout rather than the executor's, harmless while the accepted scenario never delegates an investigation and wrong in general. Corrected, mutation-checked, and appended to this milestone's archive in `3c86ba4`, which Matt accepted after Codex's no-findings review. |
| `3c86ba4` | Correct delegated investigation cognition ownership, reconsideration-stamp coverage, and `SourceEventId` documentation | Reviewed by Codex on 2026-08-19, **no findings**. Exact-commit verification: clean build, 458/458 tests, all five variants deterministic and distinct, both required viewpoints source-limited, and the Godot headless self-test passed. **Matt accepted the correction and authorized milestone 013 to baseline on it.** This row is beyond the `824f3fc` checkpoint and does not advance past the intervening commits. |
| `40f0ded`, `520924b`, `3004d2f`, `c7ae3d6` | Milestone 012/013 planning and the Codex-premise correction. Docs only | Not yet reviewed. They are later than the checkpoint and still take their turn, oldest first — a docs commit does not stand in for the implementation commit beneath it. |
| `824f3fc` | Milestone 010 implementation: a concealer acts on his own exposure; the denial is priced from its own incident | **Reviewed by its own author, the only reviewer this project now has.** Eighteen mutation checks, of which two initially caught nothing and exposed three defects in the milestone's own tests — a vacuous boundary assertion, a fixture staging a trace on an incident the concealment did not name, and nothing at all pinning whose belief moves. All three were fixed before the commit; none was found by re-reading. **Matt accepted milestone 010 on the strength of this commit on 2026-08-18. Milestone 010 closed.** |

Milestone 003 was accepted through `d685015`; milestone 004 through `1fe8a15`; milestone 006 through
`404b416`; milestone 007 through `974a88a`; milestone 008 through `7e0700e`; milestone 009 through
`7ca7819`; milestone 010 through `824f3fc`.

**Milestone 010 is the first accepted on a review nobody but its author performed**, and the first
whose implementation commit needed no corrective round. Read those two facts together rather than
singly: the absence of corrections is not evidence of quality here, because the only reviewer shares
every assumption the author made. What the round did produce is three findings, all in the
milestone's own tests, all surfaced by mutation checks rather than by inspection — which is the
argument for the method and not for the result. Note the difference in
shape: 003, 004, 006 and 008 each took their implementation plus every corrective round through review
before acceptance, while 007 needed no corrective round to its code and was accepted at its
implementation commit with one finding ruled on rather than fixed. **Its corrective rounds were all
documentation**, and two of them were rejected — one for rewriting an append-only header, one for
misreporting that rejection as no review at all. Milestone 007's accepted status rests on `974a88a`
and is unaffected by either. Milestone 006's rests on `404b416` and is likewise unaffected by its own
closeout being rejected.

**Milestone 008's code was rejected in two successive rounds.** `7a9773b` fused three of four
contributions it was required to keep apart; `9a29342` fixed that and reported hashes its own build did
not produce, on a premise its own API did not enforce. Both rejections were of the code and its
verification, not of the design, and neither disturbed anything accepted earlier. The milestone's
accepted state is `7e0700e` and nothing before it.

**That paragraph used to open "Milestone 008 is the first whose code was rejected twice", which was
false, and the rows above this one disprove it.** Milestone 003 had its implementation and five
corrective rounds rejected — `097fbda`, `cf22e5d`, `2a74a5d`, `f97ef76`, `b8fe921`, `e83dacf` — and
milestone 004 had four. The claim was a superlative asserted about a table it was sitting in, and
checking it required only reading upward. Found by Codex reviewing `3f08685` and corrected here. It is
replaced with a plain description rather than a corrected superlative on purpose: **a comparative
claim about the whole history has to be re-checked every time the history grows, and nothing prompts
that.** Prefer describing the thing in front of you.

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

### Measured — milestone 026 correction 5, `ab235b1` reviewed, one P2 test gap, corrected once, accepted

**Codex reviewed correction 4 (`ab235b1`) and confirmed the runtime fix — the explicit `ReportId`
linkage and both replay comparators — as correct.** One P2 remained, a test-integrity gap rather than
a runtime defect: `Two_reports_at_the_same_instant_do_not_share_a_reaction` stages its two impressions
by hand, passing `ReportId` directly, so it proves `Exposure`'s reader matches exactly but never
proves `Reactions.AfterReport` — the one production writer meant to populate the field — actually does.
Codex mutated `AfterReport` to omit `report.Id`; all 658 tests, all sixteen `InPersonTests` included,
still passed.

One further regression test, `AfterReport_stamps_the_impression_with_its_own_report_id`: delivers a
report through the real pipeline (`Reporting.Deliver`) and asserts the resulting `Impression.ReportId`
equals the report's own `Id`. Mutation-checked against the exact production call Codex named — reverting
`AfterReport`'s fourth constructor argument fails the new test while the other sixteen `InPersonTests`,
the hand-staged same-instant test included, keep passing. That test is kept rather than replaced: it
independently proves the consumer side's exact-id selection, a claim the new writer-side test does not
make.

**Verification.** Test-only; no production file touched. Build 0/0; tests **659** (658 + 1). `--verify`
on all four required configurations, byte-identical to correction 4's figures. `--compare` at seed 42:
6 configurations, 6 distinct traces, 6 distinct chosen-action sequences, every digest unmoved. Both
required viewpoint runs and all seven Godot invocations exit 0. One mutation check, confirmed and
reverted.

**Codex reviewed `abcffd5` and returned no findings. Matt accepted it on 2026-09-08 and closed
milestone 026.** Milestone 026's accepted state is `abcffd5` and nothing before it — the implementation
commit and each of the four corrections ahead of this one were each reviewed and each returned at
least one finding; every FAIL is what produced the correction that follows it, exactly as the standing
rules describe. Full account: `docs/milestones/026-in-person-things-come-back.md`, all five
corrections.

### Measured — milestone 026 correction 4, `c644b30` reviewed, one further P1, corrected once, still unaccepted

**Codex reviewed correction 3 (`c644b30`) and confirmed the `Receipt.IsNews` fix and the
different-incident `Exposure` test as correct, and returned one further P1: the `Exposure` fix's own
three-way match (recipient, timestamp, claim) was still not unique.** Nothing forbids two distinct
reports to the same recipient, about the same claim, at the exact same instant, and `Impression`
carried no reference back to the report that produced it — Codex reproduced the newest of two
same-instant reports being described while the reaction shown had come from the older one.

`Impression` gains `ReportId` (nullable, since `Reactions.AfterDemand`'s reading answers to no report
at all), set from `report.Id` in `Reactions.AfterReport`. `PlayerSnapshot.Exposure`'s lookup now
requires `i.ReportId == latest.Id` alongside the existing timestamp and claim checks, which are kept
rather than dropped.

**Both replay comparators, and why only one changed.** `SimulationReplayTests.Snapshot` (the
comprehensive comparator) gains `ReportId` on its `Impressions` line — the same reasoning `WakeEventId`
was added under in milestone 018. `BehavioralSnapshot`, the narrower sibling, does not: its own header
already names `Report.Id` as a monotonic-counter class it deliberately excludes, since that raw number
legitimately shifts when an unrelated report is scheduled elsewhere in the run — exactly the false
difference the insertion-stability suite built on it must not see.

One regression test, `Two_reports_at_the_same_instant_do_not_share_a_reaction`: two reports, same
recipient, same claim, same timestamp, different ids, different reactions, staged in arrival order.
Mutation-checked by removing the `ReportId` term — the test failed, picking the older report's reaction
over the newer report's own, while the other fifteen `InPersonTests` kept passing.

**Verification.** Build 0/0 on both target frameworks; tests **658** (657 + 1). `--verify` on all four
required configurations (`baseline` `83D59F6D099B840A`, `disloyal-vincent` `33F3C92F3DB9250C`,
`resentful-tommy` `2899736537AF3BE3`, `capable-angelo` `34E6AF60C2673B95`), all unmoved. `--compare` at
seed 42: 6 configurations, 6 distinct traces, 6 distinct chosen-action sequences, every digest unmoved.
Both required viewpoint runs and all seven Godot invocations exit 0. One mutation check, confirmed and
reverted.

No scoring, chosen action, or fixture touched — `ReportId` is read only by `Exposure`'s own selection
logic and the comprehensive comparator.

**Still unreviewed and unaccepted.** This correction has not been back to Codex. Full account:
`docs/milestones/026-in-person-things-come-back.md`, "Fourth correction".

### Measured — milestone 026 correction 3, `3a45a27` and `e4df2ff` reviewed, two P1s, corrected once, still unaccepted

**Codex reviewed the two playtest corrections milestone 026 had already appended (`3a45a27`, `e4df2ff`)
and returned two defects, one per commit — both a shortcut standing in for a link the model does not
actually have.**

1. **`Reactions.Landed` (`3a45a27`) inferred "news" from a coincidence of dates.** Its rule —
   `receipt.Record.AcquiredAt == at` — is true for a claim this exact `Receive` call just created, but
   equally true for a claim the listener already held whose acquisition happens to date to the same
   day this unrelated report lands. A report asserting both an old, coincidentally-dated claim and a
   genuinely fresh one could have the old claim out-rank the news, deterministically when asserted
   first, since the loop returns its first match. `Receipt` gains `IsNews`, set only by the branch of
   `Cognition.Receive` that actually finds no prior record; every other branch sets it `false`
   regardless of the timestamps. `Landed` reads it directly.
2. **`PlayerSnapshot.Exposure` (`e4df2ff`) attached a reaction to a report that never earned one.**
   Its per-recipient "what he told whom" line looked up the most recent impression about any act
   claim toward that recipient — never checking that the impression came from the specific report the
   line was describing. A later report that only withheld an incident (earning no reaction at all,
   since `Reactions.AfterReport` only reacts to what is actually asserted) could still surface an
   earlier, unrelated incident's reaction. The lookup now requires the impression's own timestamp to
   match the selected report's and its claim to be one that report actually asserted.

Two regression tests in `InPersonTests.cs`, each mutation-checked against the specific mechanism it
proves: `The_reaction_is_about_the_claim_that_is_actually_new_not_one_sharing_its_timestamp` (reverting
`Landed` to the date comparison fails it while the pre-existing "news outranks what the report led
with" test keeps passing — the mutation isolates the coincidence rather than breaking the mechanism
generally; a second mutation flipping `IsNews`'s only `true` assignment fails both tests) and
`A_withheld_only_report_does_not_inherit_an_older_reaction_to_a_different_incident` (reverting the
lookup to its unscoped form fails it on the withheld line acquiring the earlier incident's reaction).

**Verification.** Build 0/0 on both target frameworks; tests **657** (655 + 2). `--verify` on all four
required configurations (`baseline` `83D59F6D099B840A`, `disloyal-vincent` `33F3C92F3DB9250C`,
`resentful-tommy` `2899736537AF3BE3`, `capable-angelo` `34E6AF60C2673B95`), all identical to milestone
026's accepted figures — neither defect was reachable by any accepted variant's natural run.
`--compare` at seed 42: 6 configurations, 6 distinct traces, 6 distinct chosen-action sequences, every
digest unmoved. Both required viewpoint runs and all seven Godot invocations exit 0. Four mutation
checks, each confirmed and reverted.

No scoring, chosen action, or fixture touched — both fixes are presentation-layer selection logic
reading state that already exists; actor neutrality, information boundaries and determinism are
unaffected.

**Still unreviewed and unaccepted.** This correction has not been back to Codex. Full account:
`docs/milestones/026-in-person-things-come-back.md`, "Third correction".

### Measured — milestone 021, capability is a belief, corrected once, still unaccepted

**The first Codex review since the tool ran out of usage mid-way through milestone 020.** It read
`e65f0cd` and returned FAIL with three defects plus two documentation requirements. All five are
addressed by one correction commit; nothing in the milestone's design was rejected, and the three
code findings are all instances of rules this project had already settled elsewhere.

**What the three findings were, and why each was right.**

1. **A belief moved where no information reached the character.** `Suitability.RecordDelegatedOutcome`
   ran on `Strategies.Blocked` as well as on the collection. The blocked branch is silent — the target
   held out, and no report, observation or discovery roll carries that to whoever ordered the job — so
   the delegator's read of his man was moving on world state he had no access to. Waking him is not
   informing him: the `EventKind.StrategyBlocked` that branch schedules carries no claim into anybody's
   cognition. The call is removed; the collection keeps its own, where he genuinely finds the takings
   arriving and the same branch already files a `SourceKind.Discovery` claim for it.
2. **A revision could not say what had moved it.** Confidence drifted with only a `ReconsideredAt`
   stamp on the record, so a belief that shifted because a job came back was indistinguishable from one
   that shifted because a canvass found nothing. `InformationRecord` gains `Reconsidered`, and
   `Cognition.Revise` takes the occasion as a **required** parameter — optional would have left the same
   gap open to the next caller. Acquisition is preserved rather than overwritten.
3. **The `CapabilityBar` ladder was not enforced anywhere.** `HardMan` implies `RoughWork`, and each
   reader resolved a contradictory pair alone.

**Matt's ruling on the third, 2026-09-05, and it reverses a position this archive stated.** Milestone
021 recorded that the implication was "deliberately not enforced on write … nothing gets to tidy his
beliefs behind his back." That reason is kept and the mechanism moved: **resolution happens on read.**
Raw cognition may still hold the incoherent pair, no lower-bar belief is invented, and both consumers
go through `CapabilityBar.Read`. One rule — the highest bar he holds sets his tier and everything below
is entailed — covers both the gap and the contradiction.

**Authorized hash movement, and it is correction 1's alone.** Matt authorized the new `capable-angelo`
baseline on 2026-09-05 after being shown the measurement.

- `capable-angelo` trace `6B355EEF852AFA6C` → **`12AF1B71EBBDF51F`**; chosen actions
  `3D2A052BDF22B72A` → **`1EDE45C580544105`**; decisions 36 → **37**. The superseded figures stay
  written here rather than being edited out; they were correct for the state they described.
- **All five pre-existing variants unmoved**, verified before and after: `baseline 9AF57665067AEA11`,
  `cautious-vincent 86EC1ADA4A4E9179`, `watchful-boss 84AC3F65E4102EBA`,
  `disloyal-vincent 9A6E0E518294532F`, `resentful-tommy 3C4483640153DA88`.
- Corrections 2 and 3 moved nothing further, which was measured rather than assumed: the hashes were
  read after correction 1 alone and again after all three, and they match. That is what establishes the
  ladder resolution is behaviour-preserving on the accepted fixture — in it, Vincent holds both bars on
  Angelo coherently, so there is nothing for the rule to resolve.

**Verification.** Build 0/0; tests **636** (623 + 12 new + 1 comparator test); `--verify` on baseline,
`disloyal-vincent`, `resentful-tommy` and `capable-angelo`; both required viewpoint runs; five Godot
self-tests and the two-process restart proof all exit 0.

**Four mutation checks, each a real temporary production edit, each confirmed and reverted.**
Restoring the blocked-path revision fails exactly the correction-1 test; dropping `Reconsidered` from
`Revise` fails exactly the three provenance tests; reverting the scorer to read raw positions fails
exactly the ladder test; dropping the occasion from the replay comparator fails exactly the comparator
test.

**One of those mutations found a defect in this correction's own tests, and it is the row worth
reading.** The roster half of the ladder test was written asserting that the panel and the scorer could
not disagree — and the mutation that should have falsified it did not. `PlayerNarration.TakenFor`'s
switch matches `(_, true)` before it reaches the low bar, so raw `(false, true)` and resolved
`(true, true)` render the identical sentence: **the roster never displayed the disagreement, and the
observable defect was scoring-only.** The test was rewritten to claim only what it proves, and now
says so in its own summary. Recorded because the mutation check caught a green test that proved
nothing — the fourth instance in this project of that shape, and the first where the mutation
discipline itself is what caught it rather than a later reader.

**Still unreviewed and unaccepted at that point.** That correction had not been back to Codex, and
neither had milestones 022–025.

### Measured — milestone 021 correction 2, `ab737e1` reviewed, two P1s and one P2, corrected once, still unaccepted

**Codex reviewed correction 1 (`ab737e1`) and returned FAIL with two P1 defects plus one P2
documentation defect.** All three are addressed by one further correction commit; nothing in
correction 1's design was rejected, and nothing accepted before it moved.

1. **`InformationRecord.Reconsidered` went stale the moment anything but `Revise` touched the
   record.** Correction 1 made `Revise` the only call that named an occasion. `Cognition.Receive` —
   a structurally different mechanism, an account arriving rather than the holder revising his own
   reading — moves `LastReconsideredAt` in three branches (fresh agreement, a non-reversing
   restatement, and disagreement) and left `Reconsidered` untouched in all three, so each silently
   carried forward whatever cause the record already had via the unnamed `with` property. A belief
   revised for a delegated outcome and then argued over to his face kept naming the delegated outcome
   under a timestamp that had nothing to do with it. `ReconsiderCause` gains `GivenAnAccount`, computed
   once per `Receive` call and named in all three branches. Two regression tests, mutation-checked
   branch by branch: the required one — direct revision, then a contradictory report, final record
   naming the report — and a second covering the other two branches, staged so each is caught starting
   from the cause it must replace rather than from an already-correct one left over by the branch
   before it.
2. **`CapabilityBar.Ladder` was a mutable array behind an `IReadOnlyList<string>` reference** —
   castable back to `string[]` or `IList<string>` by anything holding the interface, letting a caller
   reorder or overwrite the one ladder every reader shares. Now an `ImmutableArray<string>`, a value
   type with no mutating members. Mutation-checked against the pre-correction declaration: casting the
   old array-backed field to `IList<string>` and setting an index succeeded silently — no exception —
   confirming the reachable defect the fix closes.
3. **The canonical claim "only a scenario fixture can seed an incoherent pair" overstated what was
   proved**, in both `DESIGN_DECISIONS.md` and a test's own docstring. `Cognition.Revise` alone cannot,
   because it preserves stance — that is all the cited test proves, and it is renamed to say only that.
   `Learn` and `Receive` establish or move one bar's stance without consulting the other, so either can
   build the pair independently of any fixture — this file's own `BuildAngeloWorld` test helper already
   does, through `Learn`, for several other tests in the same file. `CapabilityBar.Read` was never in
   question and resolves the pair correctly regardless of how it arose; `DESIGN_DECISIONS.md` is
   corrected in place to say so.

**Verification.** Build 0/0 on both target frameworks; tests **654** (636 + 3 new — corrections 2 and
3 above renamed and corrected existing material rather than adding tests). `--verify` on baseline
(`83D59F6D099B840A`), `disloyal-vincent` (`33F3C92F3DB9250C`), `resentful-tommy` (`2899736537AF3BE3`)
and `capable-angelo` (`34E6AF60C2673B95`) all identical to milestone 026's accepted figures, checked by
direct comparison rather than assumed. `--compare` at seed 42: 6 configurations, 6 distinct traces, 6
distinct chosen-action sequences, every digest unmoved. Both required viewpoint runs and all five Godot
self-tests plus the two-process restart proof exit 0. Five mutation checks — the three `Receive`
branches' `Reconsidered` assignment reverted independently, and the ladder's declaration reverted to
its pre-correction mutable form — each confirmed to fail only its own intended assertion, each
reverted afterward.

No hash moved on any variant. `Reconsidered` is read by nothing that reaches a rendered trace or a
chosen action, so correcting a diagnostic-only field was expected, and measured, to move nothing.

**Still unreviewed and unaccepted at that point.** That correction had not been back to Codex, and
neither had milestones 022–026.

### Measured — milestone 021 correction 3, `b02b003` reviewed, one further P1, corrected once, accepted

**Codex reviewed correction 2 (`b02b003`) and confirmed all three of its fixes correct, returning one
further P1:** the same timestamp/cause pairing invariant, unaddressed in the one writer neither
correction 1 nor correction 2 had touched.

`Cognition.Learn`'s overriding branch builds its replacement from a brand-new `InformationRecord`,
which defaults `Reconsidered` to null since nothing passes it, and then advances
`LastReconsideredAt` through a `with` expression that never names a cause either — a timestamp saying
something moved next to a cause saying nothing did. Distinct from correction 2's `Receive` defect
(there, a genuinely stale cause was carried forward from an earlier record; here, there was never an
old cause to carry — `Learn` builds fresh and simply never named the new one).

`ReconsiderCause` gains `AcquiredAgain`, carrying the overriding call's own `sourceKind` and
`sourceId` as `Via`/`AboutId` — the channel and identity already given to `Learn`, never a second,
free-text description of the same acquisition. One regression test: an existing record, then a second
`Learn` confident enough to override it through a different source; `AcquiredAt` stays the first
call's, `ReconsideredAt` and `Reconsidered` both identify the second. Mutation-checked: reverting the
assignment throws on the test's own access to a null `Reconsidered`.

**Verification.** Build 0/0; tests **655** (654 + 1). `--verify` on all four required configurations —
baseline (`83D59F6D099B840A`), `disloyal-vincent` (`33F3C92F3DB9250C`), `resentful-tommy`
(`2899736537AF3BE3`), `capable-angelo` (`34E6AF60C2673B95`) — all unmoved from corrections 1 and 2.
`--compare` at seed 42: 6 configurations, 6 distinct traces, 6 distinct chosen-action sequences, every
digest unmoved. Both required viewpoint runs, all five Godot self-tests, and the two-process restart
proof exit 0. One mutation check, confirmed and reverted.

No hash moved, despite `Learn` being called with far more `SourceKind`s across far more of the
simulation than `Receive` — every one of those call sites now runs through this fix on every accepted
variant, and none of them render `Reconsidered` into anything hashed.

**Codex reviewed `9fed181` and returned no findings. Matt accepted it and closed milestone 021.**
Milestone 021's accepted state is `9fed181` and nothing before it — `e65f0cd`, `ab737e1` and `b02b003`
were each reviewed and each returned at least one finding; every FAIL is what produced the correction
that follows it. Status of milestones 022–026 remains `CURRENT_MILESTONE.md`'s to state, not this
file's, except where reconciled elsewhere in this file.

### Measured — milestone 022, the street talks, the `Rng.ForOccasion` correction's first real review

**Codex reviewed `b4ce907` — the correction replacing `Rng.ForOccasion`'s GF(2)-linear finalizer and
repairing every natural-run test the fix moved — and returned three findings, all about the written
explanation rather than the fix itself. Matt accepted all three.** `Fnv1a` was wrongly called
GF(2)-linear in the archive and doc comments (it multiplies; the argument only needs it to be a fixed
function of the key); the claim that no two occasion keys could ever co-succeed at any seed overstated
what the algebra proves (it explains the demonstrated three-observer case, not a universal theorem
over every key pair); and the verification inventory's "only `Rng.cs` changed" line was stale by the
time it was written, since `Game.cs`'s self-test-only choice sequence had already been approved and
committed alongside it. Corrected by the commit that carries this row — documentation and comments
only, `git diff --stat` against `src/` empty, all 663 tests and all seven Godot invocations unchanged.
Full account: `docs/milestones/022-the-street-talks.md`'s final correction section.

**Correction: the row above is itself wrong about one thing.** "`git diff --stat` against `src/`
empty" describes this ledger's own commit at the time, but that commit was `7cbeb91`, and `7cbeb91`'s
`src/` diff is not empty — it is 28 additions and 11 deletions in `Rng.cs`'s XML documentation
comments, the corrected explanation itself. Codex found this as a P1 on `7cbeb91` and Matt accepted
it. The row above stands as written, per this file's practice of correcting rather than rewriting; the
accurate claim is that `7cbeb91` changed no method body and no runtime behaviour — the diff is real
and is entirely documentation. Corrected by the commit that carries this paragraph, which is itself
documentation-only and whose own `src/` diff is genuinely empty. Full account:
`docs/milestones/022-the-street-talks.md`'s own correction section for this same finding.

### Measured — milestone 023, the roster reads, a history entry with no movement behind it

**Codex reviewed `6738200` — milestone 023's implementation — and returned one P1, accepted by
Matt: `Relations.RecordAccountConflict` and `RecordAccountAgreement` remembered a `StandingChange`
unconditionally, even when the clamped `Trust` value did not actually move at the floor or ceiling.**
`Relations.Frighten`, in the same file, was already written to guard against exactly this for fear;
these two newer call sites did not carry the same guard. Fixed the identical way: capture `Trust`
before the clamp, remember only when the clamped value differs from it. Two new production-path
tests added (`A_contradiction_at_the_trust_floor_is_not_remembered`,
`A_corroboration_at_the_trust_ceiling_is_not_remembered`), both mutation-checked and reverted.
Nothing in the accepted fixture reaches either clamp, so no accepted hash moved. Corrected by the
commit that carries this row — production code plus tests, 665 tests passing (663 + 2 new), all
seven Godot invocations unchanged. Full account: `docs/milestones/023-the-roster-reads.md`'s
correction section.

**Codex reviewed `4da1e66` — the same-day follow-on — and returned two P2s, both accepted by Matt.**
First: the `PersonIsCapable` narration fix shipped with no regression coverage, so a later edit could
remove it again undetected — answered with two tests, one pinning both bars' exact prose against
`PlayerNarration.Describe`, one driving every `ClaimKind` through it generically so a future kind
added without a narration arm fails automatically rather than reaching a player first. Both
mutation-checked (the `PersonIsCapable` arm removed, both failed for their own reason) and reverted;
`git diff --stat` against `src/` for the correcting commit is empty — test-and-documentation-only, no
production behaviour changed. Second: `4da1e66` left `PlayerNarration.Standing`'s class-level doc
comment still arguing the position Matt reversed on 2026-09-04 — confirmed, by reading the live
source, that `1a7bcc6` already corrected it, so no source edit was needed, only the accurate history
recorded append-only. 667 tests passing (665 + 2 new). Full account: `docs/milestones/023-the-
roster-reads.md`'s second correction section.

**Codex reviewed `2dec7ff` and returned one P2, accepted by Matt: the focused `PersonIsCapable`
test used `"tommy"` as its subject id with an identity resolver, so it could not distinguish "the
resolver was called" from "the raw internal id was printed" — the recurring false-assurance shape,
since `"tommy"` reads as plausible either way.** Fixed by using an unmistakably internal id
(`"char-000e7f"`) resolved to a genuine display name (`"Tommy Nardo"`) and asserting the display
name appears while the internal id does not. Mutation-checked by changing the production arm's two
`Subject()` calls to `c.Subject`, bypassing the resolver: the focused test failed on the leaked
internal id, the exhaustive `ClaimKind` test was unaffected, then reverted — `git diff --stat`
against `src/` for the correcting commit is empty. Test count unchanged at 667 (rewritten, not
added to). Full account: `docs/milestones/023-the-roster-reads.md`'s third correction section.

**Codex reviewed `15d7c92` — "Show what he takes a man for on the roster," which added
`StandingChange.About` and `PlayerAttitude.TakenFor` — and returned two P2s, both accepted by
Matt.** First: nothing proved `.About` carries the exact originating claim, or that two same-cause
entries about different claims stay distinguishable through the roster projection — answered by two
assertions added to existing tests plus one new test reading `PlayerView.Build`'s own rendered
lines; mutation-checked independently (the conflict claim dropped, then separately the agreement
claim), each confirmed to fail only its own expected test, then reverted. Second: `TakenFor` had no
dedicated coverage — answered by three new tests (viewpoint-derived and not another actor's,
structurally never the target's own `Capabilities`; no-view versus rejected-view preserved, staged
for the same reason `ExecutorSuitabilityTests.cs`'s identical-shaped test is staged; natural reach
into `IntelligenceWriter` via `capable-angelo`, the one variant that seeds this belief naturally)
plus an explicit, honestly-caveated structural argument for the Godot roster panel's reach, since no
existing self-test uses that variant and none was added. The raw ladder reader `15d7c92` introduced
was not reopened — already superseded by the accepted `9fed181` — and the stale
`CURRENT_MILESTONE.md` statements present at that commit are recorded as historical rather than
corrected in the live file, which has long since moved past them. 671 tests passing (667 + 4 new);
`git diff --stat` against `src/` for the correcting commit is empty; all accepted hashes, both
viewpoints, and all seven Godot invocations unchanged. Full account: `docs/milestones/023-the-
roster-reads.md`'s fourth correction section.

**Codex reviewed `53694a2` and returned two P2s, both accepted by Matt.** First:
`TakenFor_reaches_the_runners_viewpoint_render`'s `Assert.Contains("hard man", rendered)` was false
assurance — the unrelated belief-list section already contains those words regardless of whether
`TakenFor` renders anything, confirmed by asserting the phrase's presence there too. Fixed by
locating the `HOW HE TAKES THEM` header and asserting only against what follows it; mutation-checked
by removing `IntelligenceWriter`'s `TakenFor` lines, confirmed to fail, reverted. Second: nothing
drove `TakenFor` through the live Godot screen — answered with a new sixth self-test,
`--selftest-capability`, against the real `capable-angelo` fixture, isolating the roster's own
attitude-panel section the identical way; mutation-checked by removing `Game.cs`'s `TakenFor` lines,
confirmed to fail, reverted. Also corrected in the same commit: the prior correction's own test
count ("three plus one" and "two wholly new" both miscounted `53694a2`'s actual one About test plus
three `TakenFor` tests, four total — the arithmetic was already right, the prose was not), and
`Two_contradictions_about_different_claims_read_as_different_lines_on_the_roster`'s doc comment,
which claimed "the same day" while the code staged the two contradictions three days apart —
restaged at the identical instant, which production permits and which is the sharper case
`15d7c92`'s own commit message actually names. 671 tests unchanged (both corrected tests rewritten,
not added to); `git diff --stat` against `src/` shows only the new, additive self-test; all accepted
hashes, both viewpoints, and all eight Godot invocations (seven plus the new one) unchanged. Full
account: `docs/milestones/023-the-roster-reads.md`'s fifth correction section.

### Measured — milestone 024, the operation reads, first review

**Codex reviewed `f993386` — milestone 024's implementation, adding the operation projection — and
returned three findings, all accepted by Matt.** First, and the only behavioural one: `Operating`
read only the viewpoint character's own `Execution.Strategy`, which is the owner's field alone —
`StrategyInstance` is never copied onto the executor, only pointed to from the owner's record via
`DelegatedToId` — so a man actually carrying work delegated to him read his own field as null and
saw no operation at all, the identical information-rule failure this milestone exists to prevent,
facing the other way. Fixed by scanning `world.Characters` for the one other character, if any,
whose own instance names the viewpoint as `DelegatedToId`, then deriving `ExecutorName`/`Progress`
by "who is actually doing the work" (`who.Id == (s.DelegatedToId ?? s.OwnerId)`) rather than by
which field held the instance. Second and third were documentation claims that turned out to need
verification rather than a fix: neither `PlayerSnapshot.cs` claiming `Since` as a delegation time
nor `PlayerNarration.cs`'s step-phrase count were actually wrong by the time this correction was
made — the first was never present in `f993386` (the milestone's own archive already records that
false rendering was caught and fixed before that commit), and the second (`f993386`'s accurate
"seven" against a seven-entry table) was independently brought current to "nine" by milestones 025
and 026 extending the same table without disturbing the count beside it — both confirmed by reading
the live source rather than assumed. Two production-path tests added against the natural day-20
baseline (Tommy's own view of the operation Vincent delegated to him; an unrelated character's null
view of the same run), plus one focused test isolating `IntelligenceWriter`'s "WHAT HE HAS OUT"
section from the unrelated belief list before asserting within it, plus a new Godot self-test
(`--selftest-operation`) doing the same against the live `DOING` panel. All three new production
tests and both new presentation-surface tests were independently mutation-checked and reverted; a
fourth mutation check confirmed reverting `Operating` to the owner-only lookup fails exactly the new
executor-view test and nothing else. 674 tests passing (671 + 3 new); all four required hashes, the
six-configuration `--compare` figure, both required viewpoints, all seven Godot self-tests, and the
two-process restart proof unchanged. Full account: `docs/milestones/024-the-operation-reads.md`'s
correction section.

### Measured — milestone 020, the right person for the job, corrected twice, accepted on a weaker basis than 019

**What it built.** A second organisational subordinate for Vincent, in one bounded variant
(`capable-angelo`: Angelo Conti, Coercion 0.80 against Tommy's 0.55, trusted at 0.35 against Tommy's
0.70), so `Generators.FromRelationship`'s delegation choice is genuinely comparative rather than a
foregone pick of the single highest-trust subordinate. One `DelegateStrategy` candidate per
subordinate; one new "executor capability" `Utility` component, emitted only where there is a real
choice and therefore never for any pre-existing variant; and `Strategies.ResolveViolence`'s force
outcome scaled by the executor's own Coercion rather than a flat constant, calibrated at Tommy's 0.55
— the only value that call has ever been exercised against in an accepted run — so no accepted hash
moved. Full account, both flagged judgment calls, and both corrections:
`docs/milestones/020-the-right-person-for-the-job.md`.

**Reviewed by Codex on implementation commit `f468e19`, returned FAIL with one P1.** The
"executor capability" component read each subordinate's exact `Capabilities[Skill.Coercion]` straight
off `World`, via a `GeneratorContext.SubordinateCoercion` dictionary `Pipeline.Prepare` built from the
roster. `Pipeline.SubordinatesOf` reading that roster to learn *who* reports to Vincent is the settled,
legitimate authority scan; *how good* that man is at the job is a fact about the person, not the
org chart, and scoring it from `World` violates the rule `Decision/Utility.cs`'s own header states —
`Score` "receives a `PerceivedSituation` and never a `World`". The nine tests shipped with the
milestone pinned the omniscient read rather than disproving it.

**Correction 1 (`436f6c7`) closed it** with a new `Relations.AssessedCoercion` dimension on
`IRelationship`, alongside `Trust`/`Obligation`/`Fear` — the delegator's own held belief about a
subordinate's Coercion, seeded at scenario construction to the pre-correction figures exactly (Tommy
0.55 in `Cast.Build`, Angelo 0.80 in `Variants.Apply`) so every accepted hash and the natural run's
own preference stayed unmoved. `FromRelationship` reads it through `ctx.Actor.Social.Toward(sub)`,
the same non-creating channel `Utility.Loyalty` already uses; the `World`-sourced dictionary and its
`GeneratorContext` field are gone. `Strategies.ResolveViolence` was deliberately untouched — committed
force resolution reads the executor's real, objective Coercion, which is not scoring an option.
Three new tests (`ExecutorSuitabilityTests.cs`, 9 → 12) prove the assessment drives scoring in both
directions and that a missing assessment neither falls back to the objective figure nor reads as zero.

**Reviewed by Codex on `436f6c7`, returned FAIL again: one P1 and two P2s.** The P1 was the same
class of defect one layer out, and correction 1 had not touched it: `FromRelationship`'s loop still
iterated `ctx.SubordinateIds` — the raw authority scan — to decide *who to offer* as a delegate.
`Acquaintance.KnownTo`'s own header forbids exactly that ("a soldier holding no office is therefore
not knowable this way, however senior he is"), and `DESIGN_DECISIONS.md` settled `KnownTo` as the
single derivation for any candidate's target after milestone 009 got the same thing wrong twice.
P2s: no staged proof for an organisationally-subordinate but unacquainted man; and
`Relations.AssessedCoercion` — correction 1's own new persistent relationship state — was missing
from **both** of `SimulationReplayTests.cs`'s comparators, so a run that corrupted it would have
compared equal.

**Correction 2 (`34cd117`) closed all three.** `SubordinateIds` is now filtered through
`ctx.AcquaintedIds` before any delegate candidate is generated, and the "genuine choice"
`comparative` flag is computed from the filtered set rather than the raw organisational count — a
no-op for every accepted variant, since `Cast.Build` and `Variants.Apply` already establish a
relationship with each real subordinate, which puts them in `AcquaintedIds` via `SocialState.Others`
independently of rank. One staged test proves the negative (an unacquainted subordinate is never
offered), its positive acquaintance control (an acquainted one still is), and that the comparative
gate reads the filtered set. `AssessedCoercion` was added to both `Snapshot` and `BehavioralSnapshot`,
formatted to keep `null` (no assessment) distinct from `0.0` (assessed at the floor), with a test that
perturbs nothing else — mutation-checked per comparator, each confirmed independently load-bearing.
`c25129a` is a documentation-only follow-up recording `34cd117`'s own hash, which a commit cannot
state from inside itself.

- Build: 0 warnings, 0 errors at every commit.
- Tests: `f468e19` **573** (564 milestone-019 baseline + 9 new, `ExecutorSuitabilityTests.cs`);
  `436f6c7` **576** (+3); `34cd117` **578** (+2: the acquaintance-boundary test and the comparator
  test); `c25129a` **578** (docs only).
- `--verify` deterministic at every commit. `baseline` `9AF57665067AEA11` — unmoved from milestone
  019's accepted baseline, and from 018's before it. `capable-angelo` `2060465B4F31E6DD`.
- `--compare` at seed 42: **6 configurations · 6 distinct traces · 6 distinct chosen-action
  sequences.** `baseline` `9AF57665067AEA11`/`7716CDDE3D0CA3A6`, `cautious-vincent`
  `86EC1ADA4A4E9179`/`7506045DDEB2DE14`, `watchful-boss` `84AC3F65E4102EBA`/`955921AA69ABA44C`,
  `disloyal-vincent` `9A6E0E518294532F`/`BECCA9ED2E4E7137`, `resentful-tommy`
  `3C4483640153DA88`/`B9B6D3BBE6A69200`, `capable-angelo` `2060465B4F31E6DD`/`CD9A30C1CD408F1D`. **The
  five pre-existing variants are byte-identical to milestone 019's accepted figures at every one of
  this milestone's four commits** — the new variant is additive and both corrections were
  behaviour-preserving, verified rather than argued.
- Viewpoint runs `disloyal-vincent`/`salvatore`, `baseline`/`vincent`, and the new
  `capable-angelo`/`salvatore`: all exit 0.
- Godot `--selftest`, `--selftest-goldenpath`, `--selftest-directaction`, `--selftest-corroboration`,
  `--selftest-tribute`, and the two-process restart proof
  (`--selftest-restart-save`/`--selftest-restart-load`): all exit 0, all unchanged. **A correction to
  the record:** `f468e19`'s archive stated no Godot executable was available in this environment. That
  was an incomplete search, not a genuine absence — the executable was found under the user profile
  during correction 1, and every Godot check has been run at `436f6c7`, `34cd117` and `c25129a`. The
  archive's original note is left standing, per the append-only rule, and corrected in place here.

**Matt accepted `c25129a` on 2026-09-04 and closed milestone 020 — without a Codex round on either
correction-2 commit, because Codex ran out of usage.** That is a materially weaker basis than
milestone 019's close, and recording the difference is the point rather than a formality:

- **Both Codex rounds this milestone did get returned a P1, and each P1 was an information-boundary
  violation in the same generator.** The base rate for "a Codex round on this milestone finds
  something the author did not" is two out of two. `34cd117` has had zero such rounds.
- The second P1 sat *underneath* the first and was invisible until the first was fixed — correction 1
  changed what value a delegate candidate carried and never asked whether that candidate should have
  been generated at all. Nothing establishes that correction 2 has no third layer beneath it; what is
  established is that the author looked and did not find one, which this file's own rules call weak
  evidence.
- What stands behind `34cd117` independently of anyone's reading: three mutation checks that each
  reverted a specific fix and watched the intended test fail for the stated reason before being
  reverted; 578 passing tests; and every accepted hash and digest confirmed unmoved by direct
  measurement rather than by argument.

**Milestone 020's accepted state is `c25129a` and nothing before it.** `f468e19` and `436f6c7` were
each reviewed and each rejected; neither was ever accepted, and each FAIL is what produced the
correction after it. If Codex becomes available again, `34cd117` is the oldest unreviewed commit of
this milestone and takes its turn first, ahead of anything built on top of it — and a rejection there
is an ordinary outcome that everything later inherits.

**Post-acceptance correction 3, 2026-09-04 — and it is the third-layer defect the caution above said
was not ruled out.** Found the same day, by scoping milestone 021 rather than by a review: the
"executor capability" component was tagged `RelationshipFacet.None` throughout, which was true of the
original `World`-reading implementation and was silently invalidated by correction 1, whose whole
purpose was to make that component read the actor's own relationship state instead. Genuine
relationship state reporting that it reads none — milestone 008's "a label is not a derivation",
arriving from the opposite direction. The developer relationship channel was therefore blind to it,
and `TotalWithoutRelationships()` kept a term that a man with no relationships could not have,
**reversing which candidate that counterfactual named** on the `capable-angelo` delegation fork;
`--compare`'s "rel. chose" for that variant was computed from the mis-ranked comparison and moves from
**1** to **2**. Three tests had pinned the wrong facet rather than catching it, the same pattern as the
two Codex P1s. Full account: the milestone archive's "Correction 3".

The correction is diagnostic-only — `ScoreComponent.Value`, every `Total`, and every chosen action are
untouched, verified rather than asserted:

- All five pre-existing variants: **trace hash and chosen-action digest both unmoved**, again.
- `capable-angelo` chosen actions `CD9A30C1CD408F1D`: **unmoved** — identical choices.
- **`capable-angelo` trace hash moves, and this row is where that is recorded:
  `2060465B4F31E6DD` → `35BB0B8BE4219C6A`.** The rendered relationship channel is inside the hashed
  text, so the one variant that has a capability component is the one variant whose trace moves. The
  superseded figure stays written above rather than being edited out; it was correct for the state it
  described.
- Tests **579** (578 + 1); build 0/0; both mutation checks (facet reverted; `withoutRelationship`
  dropped) confirmed independently load-bearing; all Godot self-tests and the two-process restart
  proof exit 0.

**This does not re-open milestone 020 or change what was accepted at `c25129a`** — it corrects the
accepted state, in the ordinary way a defect found after acceptance is corrected, and it is
unreviewed for the same reason everything since `34cd117` is. It does sharpen the caution above: two
Codex rounds found a P1 each, and the first thing that looked hard at the result afterwards found a
third. Nothing here should be read as the sequence having converged.

**Update, 2026-09-08 — the backlog reviewed, oldest first, and the accepted state moves.** Codex
reviewed `34cd117`, `c25129a`, `826b1e2` and `8e6878e` in commit order. `34cd117` returned **FAIL, one
P1** — the same `RelationshipFacet.None` defect "Post-acceptance correction 3" above already found and
fixed by scoping the next milestone rather than by a review round; Matt accepted the finding as
historically real and authorized no further fix, since `8e6878e` already contains the correct repair.
`c25129a` and `826b1e2` returned **no findings** — `826b1e2`'s own text, which records Matt accepting
milestone 020 at `c25129a` without a Codex round, was itself checked and found to disclose that
honestly rather than overclaim a review that had not happened. `8e6878e` returned **no findings**, and
Matt accepted it.

**Milestone 020's accepted state is therefore `8e6878e`, superseding `c25129a`.** This is a genuine
change of standing, not a formality: `c25129a` was accepted on self-review alone, the "weaker basis
than milestone 019" the caution above exists to flag, and everything built on it since has carried
that caveat forward. `8e6878e` — self-found in the first place, exactly like the P1 it fixes — is now
independently confirmed by Codex, which the "third-layer defect" paragraph above says nothing had
established until this review. The three mutation checks, 579 tests and every hash and digest already
recorded above stand as they were measured; only the standing behind them has changed.

### Measured — milestone 019, controlled/autonomous actor parity is pinned, corrected three times, accepted

**Verification-only from the start.** Authorized to resolve a controlled-versus-autonomous decision
anomaly milestone 018's archive and `ROADMAP.md` had recorded: at Tommy's natural first
asked-to-account decision in the baseline seed-42 scenario, controlling him and immediately calling
`SimulationSession.ResolveAutomatically()` was said to pick a self-protective partial report while
the fully autonomous run answered candidly at the identical decision. Reproducing it end to end
through the real pipeline found it did not hold — not at the exact cited decision, not across a
bounded sweep of every variant and every character, not under a mismatched viewpoint, and not at
either of milestone 018's own earlier commits. The "candid" half traced to
`ScenarioReachTests.And_the_executor_gives_his_delegator_an_account_of_it`, which only asserts a
report exists whose `AnsweringClaim` matches the question — true of a partial report withholding
that exact claim as much as a candid one, since `Reporting.Compose` stamps `AnsweringClaim`
regardless of `Candor`. Matt ruled the milestone reframed as verification-only rather than have a
production change invented against a falsified premise: `ROADMAP.md`'s entry retired in place,
`docs/milestones/018-...md` gained an appended correction retracting its Finding 3 without touching
Findings 1–2, and the investigation promoted into a permanent regression suite,
`ControlledAutonomousParityTests.cs`. Full account, including all three corrections:
`docs/milestones/019-controlled-autonomous-actor-parity-is-pinned.md`.

**Reviewed by Codex on implementation commit `99db4de`, returned FAIL with three P2s.** The parity
comparator (`WorldFingerprint`) didn't cover enough persistent state; there was no true
viewpoint-only isolation, only a sweep that varied viewpoint alongside the autonomous/controlled
question; and a doc comment on the focused Tommy test contradicted its own assertion, claiming a
request "drops out of `AwaitingAnswers`" when — a Partial report withholding precisely the asked
claim being structurally indistinguishable from silence — it correctly stays outstanding.

**Correction 1 (`c9af6b6`) closed all three.** The comparator was rebuilt on
`SimulationReplayTests.Snapshot` (widened from `private` to `internal` and reused rather than
re-derived); a fifth test isolates viewpoint alone, holding the controlled character fixed across
two sessions differing only in `ViewpointCharacterId`; the comment was fixed. Required as proof for
the first two, not a finding of its own: both additions were mutation-checked and reverted, which
incidentally surfaced and fixed a real bug in the test harness's own driving loop (`StepEvent`'s
unbounded horizon let it process one event past the 90-day cutoff on the controlled side only —
fixed by driving through `AdvanceTo(end)` instead).

**Reviewed by Codex on `c9af6b6`, returned FAIL with two further P2s.** The comparator remained
incomplete — named specifically: `Capabilities.Cash`, `Execution.Intention`,
`ObservationOccasionKeys`, and deterministic queued-event contents rather than only `Queue.Count` —
and this archive's own account of round 1 was inaccurate, having mislabeled the required
mutation-check proof as a fourth Codex finding and demoted the real third finding (the
`AwaitingAnswers` comment) to an unlabeled paragraph.

**Correction 2 (`c335d7c`) closed the completeness gap via an explicit audit** of every mutable
`World`/`Character` collection against its production write sites, adding `Motivations` in full
(`Ambition`, `Responsibilities`, `Pressures`, `ImmediateNeeds` — none previously covered),
`Capabilities` in full, `Social.OrganizationId`, `Execution.Intention`, `ObservationOccasionKeys`,
and full queue contents plus `Queue.Cancelled`; a `Capabilities.Cash` mutation check, required as
proof for that same finding, confirmed and reverted. Reading queue contents this round added a new
`internal`, read-only `EventQueue.PendingEvents` accessor to production code — and separately
repeated the identical mislabeling the correction was meant to fix, numbering mutation-checking as
round 2's second finding rather than proof for its first, burying what the real second finding (the
archive's inaccuracy) actually was.

**Reviewed by Codex on `c335d7c`, returned FAIL with two P2s, confirming everything else correct.**
`EventQueue.PendingEvents` violated the milestone's explicit "production simulation code unchanged"
requirement regardless of being read-only and behaviourally inert — a milestone's own archive does
not get to grant itself that exception — and the archive had again mislabeled a review's findings.

**Correction 3 (`8b5e70f`) removed the accessor entirely** — `git diff` against the pre-milestone
baseline for `src/CrimeEmpire.Simulation/` is empty, byte-identical rather than merely
behaviourally unchanged — and reached the same queue state through a test-only
`System.Reflection` helper reading `EventQueue`'s private `_queue` field directly, verified once by
a temporary sanity-check test then deleted rather than kept as a permanent sixth test. It also
states plainly what each of the two review rounds' actual findings were, appended without rewriting
either prior account.

- Build: 0 warnings, 0 errors, across all four commits (implementation plus three corrections).
- Tests: `99db4de` **554 passed, 0 failed** (550 milestone-018 baseline + 4 new); `c9af6b6` **555**
  (554 + 1, the viewpoint-only test); `c335d7c` **555** (unchanged — comparator widened in place, no
  new test method); `8b5e70f` **555** (unchanged — the queue accessor was swapped for reflection, no
  new test method).
- `--verify` deterministic and byte-identical across all four commits: `9AF57665067AEA11`, unmoved
  from milestone 018's accepted baseline — this milestone changed no simulation behavior at any
  point, verified rather than merely claimed.
- `--compare` byte-identical across all four commits on every variant: `baseline` `9AF57665067AEA11`,
  `cautious-vincent` `86EC1ADA4A4E9179`, `watchful-boss` `84AC3F65E4102EBA`, `disloyal-vincent`
  `9A6E0E518294532F`, `resentful-tommy` `3C4483640153DA88`. Both required viewpoint runs
  (`disloyal-vincent`/`salvatore`, `baseline`/`vincent`) exit 0 at every commit.
- Godot `--selftest`, `--selftest-goldenpath`, `--selftest-directaction`,
  `--selftest-corroboration`, `--selftest-tribute`, and the two-process restart proof
  (`--selftest-restart-save`/`--selftest-restart-load`) all exit 0 at every commit.

**Codex independently reviewed correction commit `8b5e70f` and returned PASS with no P1 or P2
findings.** Matt accepted `8b5e70f` on 2026-08-27 on the strength of that clean review, and closed
the milestone. Milestone 019's accepted state is `8b5e70f` and nothing before it — `99db4de`,
`c9af6b6`, and `c335d7c` were never themselves accepted; each Codex FAIL is what produced the
correction that follows it. Production simulation code (`src/CrimeEmpire.Simulation/`) is
byte-identical to its state before this milestone began (`b2d7779`) — this milestone found no
defect and changed no simulation behavior; what it produced is a permanent regression suite and two
corrected documentation records.

### Measured — milestone 018, the player can see what their choice did, corrected three times, accepted

**Self-implemented; reviewed by Codex on implementation commit `ae06f61`, returned FAIL with two P1
findings and one P2 proof gap.** Projects a causal thread — immediate acknowledgement, unresolved
status while pending, perspective-limited resolution once known — entirely from already-authoritative
typed state (`World.Decisions`, `World.Requests`, `World.Businesses`,
`World.AccountConflicts`/`AccountAgreements`), through four new `PlayerSnapshot` fields and a widened
`PlayerOccasion.For`, no second event log or player-only action path. Full account, including the
original implementation and all three appended corrections:
`docs/milestones/018-the-player-can-see-what-their-choice-did.md`.

**Correction 1 (`b9dfa49`) fixed all three findings from `ae06f61`'s review:** pending and declined
requests had been indistinguishable; a forced tribute demand matched the demander alone rather than
demander-and-business, so the same man's violence at an unrelated business misread as "already used
force over this"; six required proof categories were missing. Tests: **543 passed, 0 failed** (523
pre-existing + 20 new, `CausalFeedbackTests.cs`).

**Codex reviewed `b9dfa49` and returned FAIL again: one P1 in the correction's own fix, two P2 gaps.**
(1) the pending/declined fix read `World.Decisions` for the *asked* character to tell whether his own
deliberation had resolved — itself a private-state leak, since the asker never receives any message
establishing that the asked person decided anything at all; (2) the action-kind audit called
`PlayerOption.Describe` directly on a post-hoc scan rather than exercising `PlayerView.Build`/
`LastAction`; (3) `InformationRequest.WakeEventId`, genuine new persistent state added by the first
correction, was missing from both independent replay comparators.

**Correction 2 (`f5246c0`) fixed all three.** Request disposition became `Pending`/`Answered` — two
values, not three — derived entirely from the asker's own `Cognition.Testimony`; a same-pass attempt to
keep a third `Declined` value for a communicated denial was itself caught and reverted by a test, since
the natural proof has Vincent give Salvatore a full, sincere account that happens to contradict him — an
answer, not a refusal. The action-kind audit was rewritten to drive each variant event by event and
assert on a real `PlayerView.Build` snapshot after every decision. Both replay comparators gained
`WakeEventId`, each with a focused proof that differing linkage identities cannot compare equal. Tests:
**552 passed, 0 failed** (548 prior + 4 new).

**Codex reviewed `f5246c0` and confirmed all three defects resolved, the demand/business fix still
valid, and full verification unchanged — but flagged one remaining P2:** `WakeEventId` had no
production consumer left once resolution stopped reading it (written, replay-compared, never
consumed), and `RequestDisposition`/`PlayerRequest.Disposition` were redundant, since `PlayerRequest`
objects are only ever built for requests that already fail the "answered" check, so every exposed
disposition was necessarily `Pending`.

**Correction 3 (`43379e0`) removed both abstractions.** `InformationRequest.WakeEventId`, the
`RequestDisposition` enum, and `PlayerRequest.Disposition` are gone; `Commit.cs`'s `SeekCorroboration`
case reverted to its pre-milestone-018 request-before-schedule ordering (the reorder existed solely to
capture `WakeEventId`); `AwaitingAnswers` now filters `World.Requests` directly against the asker's own
`Cognition.Testimony`, with the corrected information boundary unchanged in substance — private silence
and private refusal remain indistinguishable, neither is described as a communicated refusal, and a
communicated answer in either direction removes a request from the list. A grep across `src/` and
`tests/` found no genuine production consumer for either removed abstraction. Tests: **550 passed, 0
failed** (552 prior minus two tests that existed only to prove `WakeEventId` made two otherwise-identical
requests compare unequal).

- Build: 0 warnings, 0 errors, across all three corrections.
- `--verify` deterministic and byte-identical on all five variants throughout every correction:
  `baseline` `9AF57665067AEA11`, `cautious-vincent` `86EC1ADA4A4E9179`, `watchful-boss`
  `84AC3F65E4102EBA`, `disloyal-vincent` `9A6E0E518294532F`, `resentful-tommy` `3C4483640153DA88` — all
  unmoved from milestone 017's accepted baseline; this milestone changed no simulation behavior, only
  presentation. Both required viewpoint runs (`disloyal-vincent`/`salvatore`,
  `baseline`/`vincent`) exit 0 at every correction.
- Godot `--selftest`, `--selftest-goldenpath`, `--selftest-directaction`, the two new
  `--selftest-corroboration` and `--selftest-tribute`, and the two-process restart proof
  (`--selftest-restart-save`/`--selftest-restart-load`) all exit 0 at every correction.

**Codex independently reviewed correction commit `43379e0` and returned PASS with no P1 or P2
findings.** Matt accepted `43379e0` on 2026-08-26 on the strength of that clean review, and closed the
milestone. Milestone 018's accepted state is `43379e0` and nothing before it — `ae06f61`, `b9dfa49`, and
`f5246c0` were never themselves accepted; each Codex FAIL is what produced the correction that follows
it.

### Measured — milestone 017, direct action vs delegation, corrected once, accepted

**Self-implemented; reviewed by Codex on implementation commit `9de2c75`, returned FAIL with four
findings.** Forks the existing seed-42 `SecureTribute` operation at the pause where Vincent may
continue personally or delegate to Tommy, proving executor identity determines evidence attribution,
encounter/fear, first-hand knowledge, and investigation subject — through the owner/executor split
already built by milestones 007–011, no new production mechanism. Full account, including the original
implementation and the appended correction:
`docs/milestones/017-direct-action-vs-delegation.md`.

**Codex reviewed `9de2c75` and returned four findings, none behavioural — every trace hash and
chosen-action digest was unaffected by the correction that follows.** (1) the pause/fast-forward test
asserted only `SessionStatus.Ready` and the ending date, compatible with two different histories; (2)
the save/load proof compared only the immediate `DelegatedToId` change against the other loaded branch,
not a real consequence against an unsaved control; (3) the investigation-attribution test hand-typed
the `WitnessSawIncident` claim it fed to Kane rather than reading the one `Strategies.ResolveViolence`
actually produced, so a misattribution in production code would never have been exercised; (4) the
Godot fork-offering check read `session.Pending.Options` — internal state — rather than the rendered
interface, so a UI-only omission of a fork button would never have been caught.

**Corrected in the commit that follows.** (1) replaced with a real fast-forward-vs-event-by-event
equivalence test reusing `PlayerSessionTests`' own proven stepping-pattern idiom (`Settle`, the "last
offered option" policy) and comparing trace, snapshot, and `StrategyInstance` fingerprint; (2) extended
to continue each loaded branch to a real consequence and compare against a fresh, never-saved control
session driven the identical way; (3) rewritten to drain the real `ObservationOpportunity` event
`ResolveViolence` schedules for Kane and read its actual claim, bypassing only the discoverability
*roll*, never the claim's content; (4) rewritten to call `FindButton` against the live scene tree, the
same helper `Press` itself uses. Findings 3 and 4 each carried an explicit required mutation check,
both run and confirmed to fail the corrected test for the stated reason, then reverted:
- Finding 3: `ResolveViolence`'s `witnessClaim` changed to `owner.Id` (leaving `violenceClaim`
  correctly attributed to `executor.Id`) — the corrected test failed for the delegated case exactly as
  required (`Expected: "tommy"`, `Actual: "vincent"`).
- Finding 4: the Godot option-rendering loop given a one-line `continue` omitting the delegate button
  from the UI while the session still offered it internally — the corrected self-test failed
  (`delegate button present: False`), exit code 1.

Findings 1 and 2 were additionally self-checked by a temporary sanity mutation each (forcing a
mismatched choice on one side of the comparison), confirmed to fail the corrected assertion, then
reverted — not formally required by Codex's review but consistent with this project's own
mutation-checking practice.

- Build: 0 warnings, 0 errors, unchanged project count. Tests: **523 passed, 0 failed**, same total as
  before the correction (one 2-case theory replaced another of equal size).
- `--verify` deterministic and byte-identical on `baseline` (`9AF57665067AEA11`), `disloyal-vincent`
  (`9A6E0E518294532F`), `resentful-tommy` (`3C4483640153DA88`) — all unmoved from milestone 016's
  accepted baseline. `--compare` shows 5 distinct traces, 5 distinct chosen-action sequences, all
  digests unmoved. Both required viewpoint runs exit 0.
- Godot `--selftest`, `--selftest-goldenpath`, the corrected `--selftest-directaction`, and the
  two-process restart proof (`--selftest-restart-save` / `--selftest-restart-load`, genuinely separate
  headless invocations) all exit 0.

**Codex independently reviewed correction commit `0f56f1e` and returned PASS with no findings.** Matt
accepted `0f56f1e` on 2026-08-25 on the strength of that clean review, and closed the milestone.
Milestone 017's accepted state is `0f56f1e` and nothing before it — `9de2c75`, the original
implementation, was never itself accepted; Codex's FAIL on that commit is what produced the correction.

### Measured — milestone 016, trust can be earned, corrected twice, accepted

**Self-implemented; reviewed twice by Codex, on implementation commit `66917c7` and again on the
first correction commit, `380a241`; corrected each time in the commit that follows.** Adds
`AccountAgreement` and `Relations.RecordAccountAgreement` — the mirror image of milestone 006's
`AccountConflict`/`RecordAccountConflict` — reusing `Cognition.Receive`'s existing fresh-agreement
branch rather than a new state machine. Full account, including the original implementation and both
appended corrections: `docs/milestones/016-trust-can-be-earned.md`.

**Codex reviewed `66917c7` and returned four findings, none behavioural.** Corrected in `380a241`:
(1) the milestone's durable rule had no entry in `docs/DESIGN_DECISIONS.md` — added, alongside the
existing milestone 006 and 008 relationship sections; (2) the dedicated-coefficient test computed its
own expected value from the same live constant production read, so it passed under Codex's mutation
(swapping in `ConflictTrustCost`, which equals `AccountAgreementTrustGain`'s value today) — fixed, in
that commit, by changing `AccountAgreementTrustGain` from `const` to a plain mutable `static` field
and mutating its runtime value in the test; (3) the archive falsely claimed the eleven planning
rulings were "visible in this commit's diff" — they were not, since the mid-implementation version of
`CURRENT_MILESTONE.md` that held them in full was overwritten before anything was committed —
corrected by reproducing the rulings verbatim (recovered from the authorizing conversation, not from
git) in the archive's first appended correction, which is now the surviving contract; (4)
`Relations.RecordAccountAgreement`'s doc comment wrongly claimed provenance differences were "already
charged" in `Cognition.Receive`'s confidence raise, copying `RecordAccountConflict`'s reasoning without
checking it held — the agreement branch's raise is flat regardless of `SourceKind`, so nothing charges
the distinction anywhere; corrected to state plainly that milestone 016 applies one flat rule because
the weighting question is undecided, not because it is resolved elsewhere.

**Codex reviewed `380a241` and found one new P1: finding 2's own fix introduced a new defect while
closing the original one.** A plain mutable `public static double` is process-global mutable state
with no persistence or replay story, reachable by any other test or code in the same process — exactly
what this project's determinism guarantees exist to rule out. **Corrected in the commit that
follows**: `AccountAgreementTrustGain` is `static readonly` again (immutable, but — unlike `const` — a
genuine field with its own metadata token), and the test now proves which field
`RecordAccountAgreement` reads structurally, by walking the method's own compiled IL for `ldsfld`
instructions and resolving them to real `FieldInfo`s, with an opcode-to-operand-size table built from
`System.Reflection.Emit.OpCodes`' own metadata rather than hand-transcribed. Mutation-checked directly
against Codex's exact review mutation again; confirmed to fail (on an empty result set, since a
`const` read produces no `ldsfld` at all) and reverted. Findings 1, 3, and 4 above are unaffected by
this second correction. Full account, including every mutation check's exact result for both
corrections: the archive's two appended corrections.

- Build: 0 warnings, 0 errors across six projects (unchanged — no new project). Tests: **505 passed,
  0 failed** throughout both corrections (482 before this milestone; 23 in `AccountAgreementTests.cs`
  — one test replaced by two, net +1, from the coefficient proof; the second correction changed that
  proof's mechanism, not its count).
- **Three of five variants' trace hashes move, disclosed and accounted for exactly, not assumed
  stable**: `baseline` `FEE45FD886F18CA8` → `9AF57665067AEA11`; `disloyal-vincent` `45CCF5ADC6EC0302`
  → `9A6E0E518294532F`; `resentful-tommy` `F5BD93386DE04082` → `3C4483640153DA88`.
  `cautious-vincent` (`86EC1ADA4A4E9179`) and `watchful-boss` (`84AC3F65E4102EBA`) are unchanged — a
  checked scenario-timing fact (the natural chain's shape does not occur, or occurs in the wrong
  order, in those two), not an unexplained gap. **Every chosen-action digest is unchanged across all
  five variants** (`7716CDDE3D0CA3A6`, `7506045DDEB2DE14`, `955921AA69ABA44C`, `BECCA9ED2E4E7137`,
  `B9B6D3BBE6A69200`) — no decision anywhere picked a different winner. In each of the three moved
  traces, a full-trace `diff` against the pre-milestone baseline shows exactly 24 changed lines, all
  within the single 8 April Tommy-answers-Salvatore panel — see the archive for the exact lines.
- `--verify` deterministic (run A = run B) on all three moved variants at their new hashes;
  `--compare` shows 5 distinct traces, 5 distinct chosen-action sequences. Both required viewpoint
  runs exit 0.
- Godot `--selftest` and `--selftest-goldenpath` both unchanged (that thread completes by 1 April,
  before the 6–8 April window this milestone touches). The two-process restart proof unchanged on the
  restart self-tests' own isolated slot: `1 April 1987`, `cash on hand 6,840`. Production save slot
  hash confirmed unchanged before and after.
- **The natural seed-42 chain, demonstrated before authorization and proven again in the test suite**:
  Tommy already holds `TargetIsVulnerable(bellini-grocery)` from Vincent's delegation briefing; asks
  Salvatore on 6 April; Salvatore — the first and only account he ever gives Tommy about this claim —
  answers on 7 April; Tommy's trust in Salvatore rises from its scenario-established `0.30` by exactly
  `AccountAgreementTrustGain * Strength`; his 8 April decision about an unrelated claim
  (`PersonUsedViolence`) reads the changed trust through the existing, unmodified `AddLoyaltyParts`
  components of `ActionKind.ReportToSuperior`/`ReportCandor.Partial` in `Utility.cs`. The winner at
  that decision is unaffected in every variant where it fires.
- **Four mutation checks from the original review, each confirmed to fail for the intended reason and
  reverted**: missing emission (disabled `Runner.cs`'s block); repetition/reaffirmation farming (made
  the no-reversal branch also emit agreement); wrong relationship direction (wrote to `listener.Id`
  instead of `agreement.SpeakerId`); private-truth leakage (let `ReportedClaim.ActualBasis` move the
  emitted confidence). Plus one more from each correction, both re-running Codex's identical
  coefficient-swap mutation (`RecordAccountAgreement` reading `ConflictTrustCost`): the first
  correction's runtime-mutation test caught it by observing the trust delta track a wrong value; the
  second correction's structural IL-walk test catches it by finding no `AccountAgreementTrustGain`
  read at all, since the mutated line reads a `const` that is inlined rather than loaded via `ldsfld`.
  Exact failure messages in the archive.

**A self-review was also run against `809fe60`, and an earlier version of this section misdescribed
it.** The `implementation-fidelity-reviewer` agent is Claude reviewing its own work in an isolated
worktree. It has no adversarial standing and does not stand in for Codex, whatever its own checks
find — a prior version of this paragraph called it exactly that, and the mischaracterization is
corrected here rather than repeated. Its findings are recorded below as a self-review, not as the
review that established acceptance: independently confirmed rather than taken from the commit message,
`git diff 380a241..809fe60` touches exactly the five files claimed, with `Relations.cs`'s hunk limited
to the field declaration and its doc comment; `dotnet build` (0/0) and `dotnet test` (505/505)
reproduced directly; `--verify` on all three moved variants and `--compare` across all five reproduced
the exact hashes this section already records; a live reflection probe (`FieldInfo.SetValue` against
the `static readonly` field, in an isolated scratch project on the same target framework) threw
`FieldAccessException`, confirming the P1 — process-global mutable state — is genuinely closed rather
than relocated; the new `StaticFieldsReadBy` IL walker was read in full and assessed as correct for
`RecordAccountAgreement`'s specific branch-free body, including the two-byte opcode space and the
variable-length `InlineSwitch` operand its own opcode-size table has to account for even though this
method never uses one. Two NOTE-level observations, neither a defect: `static readonly`'s
cross-assembly behaviour differs from `const` in a direction that favours correctness rather than
undermining it; `docs/RELATIONSHIPS.md` still calls the field a "constant" in prose, pre-existing and
unrelated to this commit. Godot's headless self-tests and the two-process restart proof were not
independently re-run inside that review (no Godot binary in its isolated environment) — covered
instead by this author's own direct run of both, on this exact commit, immediately before it was made:
`--selftest` and `--selftest-goldenpath` unchanged, the two-process restart proof unchanged on the
isolated slot (`1 April 1987`, `cash on hand 6,840`), production save slot hash confirmed unchanged
before and after.

**Codex reviewed `809fe60` directly (reported by Matt) and returned no findings.** That is the review
that actually stands in this milestone's acceptance, not the self-review above.

**Matt accepted correction commit `809fe60` on 2026-08-25, on the strength of Codex's clean review,
and closed the milestone.** A prior commit, `4c65f34` ("Close milestone 016... Docs only"), recorded
this same acceptance on 2026-08-23 — two days before Matt gave it — on the strength of the self-review
above, which that commit also mischaracterized as standing in for Codex. `4c65f34` is not a valid
acceptance record and must not be read as one; this section is the corrected account. `4c65f34` further
claimed "no commit was made solely to record this acceptance," which was false in the same breath it
was written: `4c65f34` was itself exactly such a commit, and an invalid one, recording an acceptance
that had not occurred. This documentation-only correction is what fixes the record; it changes no
production code, no test, and none of the technical findings above.

### Measured — milestone 015, the operation survives a restart, corrected twice, accepted

**Self-implemented; reviewed twice by Codex, on implementation commit `9537b38` and again on the
first correction commit, `af7d34f`; corrected each time in the commit that follows.** Adds
`src/CrimeEmpire.Persistence` (replay-backed SQLite save/load), Godot Save/Load controls, and (from
the first correction) a small dedicated `CrimeEmpire.Persistence.InterruptedWriteHarness` project
used only by one test. `CrimeEmpire.Simulation` gained no new member and no behavioural change —
nothing in `Strategies.cs`, `Commit.cs`, `Filters.cs`, or `Generators.cs` changed, so every hash below
is unmoved from milestone 014's accepted baseline, confirmed by a full clean-tree re-run rather than
assumed. Full account, including the original implementation and both appended corrections:
`docs/milestones/015-the-operation-survives-a-restart.md`.

**Codex reviewed `9537b38` and returned four findings, two P1 and two P2, all about the strength of
this milestone's own verification rather than the persistence mechanism it verifies.** The first
correction (`af7d34f`) addressed all four, but **only fully fixed three of them** — see the second
review below. (1) the exact-internal-replay-state proof compared a hand-picked list of fields rather
than the actual state — the first correction replaced the `World`-counter portion of that list with a
deep reflective fingerprint of the complete `World` object graph, but left `_prepared`, `_optionIds`,
the clock/fast-forward state, and the pending decision checked through a second hand-picked list one
level up, which is what the second review found; (2) `--selftest-restart-save` wrote directly to the
production save slot — fixed with a per-process `_activeSavePath` resolved once at startup, so the
restart self-tests use a dedicated isolated slot while still exercising the real Save/Load button
handlers, verified both empirically against a real production file and via an isolated
`CE_SAVE_PATH_OVERRIDE` fixture that never touches Matt's actual save — **confirmed fixed, unaffected
by the second review**; (3) the loaded-snapshot cash-boundary test checked the leaked sentinel only
among numeric values, missing the nested-text leak shape milestone 014's own correction (`ff4213a`)
already found once — fixed and mutation-checked the same way — **confirmed fixed, unaffected by the
second review**; (4) the interrupted-write proof locked a file *before* a write began rather than
interrupting a write genuinely in progress — replaced with a real, separate OS process killed via
explicit named-event synchronization at a deterministic point after it opened a real write
transaction — **confirmed fixed, unaffected by the second review**.

**Codex reviewed `af7d34f` and found the P1 residual: the exact-internal-replay-state fix above still
checked `SimulationSession`/`PersistentSession` through a second hand-picked list** —
`_controlledId`, `ViewpointCharacterId`, `Seed`, `StartedOn`, and `PersistentSession._log` were not
checked at all, and the pending decision was checked through four of its seven properties, silently
missing `ActorName`, `ActorRole`, and `ActorPronouns`. **Corrected in the second correction
commit**, by pointing the same deep-fingerprint mechanism at the complete `PersistentSession` wrapper
itself as its sole completeness check, rather than at `World` alone plus a supplementary list —
mutation-checked both ways specified (a swapped `_controlledId` with `World` untouched; a removed
`PersistentSession._log` entry with `World` untouched), both confirmed to fail specifically on the
fingerprint comparison and reverted before committing. Full account, including every mutation check's
exact result for both corrections: the archive's two appended corrections.

- Build: 0 warnings, 0 errors across **six** projects. Tests: **482 passed, 0 failed** throughout —
  unchanged in count across the original implementation and both corrections (finding 4 of the first
  review replaced one test rather than adding one; every other finding strengthened an existing test
  in place, including the second correction's simplification of `AssertExactInternalIdentity`).
- `--verify` deterministic and byte-identical on `baseline` (`FEE45FD886F18CA8`), `disloyal-vincent`
  (`45CCF5ADC6EC0302`), `resentful-tommy` (`F5BD93386DE04082`) — all three unmoved from milestone
  014's accepted baseline. `--compare` byte-identical across all five trace hashes and chosen-action
  digests. Both required viewpoint runs exit 0.
- Godot `--selftest` (4 choices, 4 decision screens, exit 0) and `--selftest-goldenpath` (seven
  choices, `6,000` → `6,840`, exit 0) both unchanged.
- **The two-process restart proof, run as two genuinely separate OS processes against the restart
  self-tests' own isolated slot** (not the production one, per the first correction):
  `--selftest-restart-save` plays the first three choices (start, carry on, delegate to Tommy), saves,
  and exits; a second, independent Godot invocation, `--selftest-restart-load`, loads that save and
  plays the remaining four, reaching `1 April 1987` with `cash on hand 6,840` read off the live
  screen — the exact accepted milestone 014 consequence, reconstructed entirely through deterministic
  replay with `World` never serialized. The production save slot was confirmed byte-identical before
  and after both flags ran, both against a real file and via the isolated fixture proof. Full command
  transcripts in the milestone archive. Both of the authorizing message's stop conditions were cleared,
  not triggered: `Microsoft.Data.Sqlite` runs under Godot's .NET 8 headless host, and exact
  reconstruction needed no `World` serialization.

**Matt accepted correction commit `bc79425` on 2026-08-23 and closed the milestone**, with no further
commit made solely to record the review, per standing practice — recorded here, folded into
milestone 016's own documentation commit.

### Measured — milestone 014, one complete player-owned operation, corrected twice, accepted

**Matt accepted correction commit `ff4213a` on 2026-08-23 and closed the milestone**, with no further
commit made solely to record the review, per standing practice (e.g. `53e912e`, `b8e5ed4`) — recorded
here, folded into milestone 015's own documentation commit.

Adds one field to `PlayerSnapshot` (`Cash`, populated from the viewpoint character's own
`Capabilities.Cash`) and one display line in `Godot/Game.cs`. Nothing in `Strategies.cs`, `Commit.cs`,
`Filters.cs`, or `Generators.cs` changes, so every hash below is unmoved from the accepted baseline —
confirmed rather than assumed, by a full clean-tree re-run. Full account:
`docs/milestones/014-one-complete-player-owned-operation.md`.

**Reviewed by Codex on 2026-08-20 (implementation commit `712a125`), two P1 and two P2 findings.**
The implementation commit's own claim — quoted above until this correction, and false as stated — was
that the golden-path test played the operation "through every one of Vincent's own decisions... none
of them resolved automatically." That was true of the resolution mechanism (every pause did go
through `SimulationSession.Choose`) but not of how the *choice itself* was determined: the test read
`PreparedDecision.Scored[0]` through reflection into session-private state, information no Godot
button carries, to decide which option to press. Corrected in the commit following `712a125`: the
seven choices are now a pinned, independently-scripted sequence matched only against
`PendingDecision.Options`'s public `Description` text and opaque `Id` tokens, and a new Godot headless
check (`--selftest-goldenpath`) presses those same seven buttons for real and reads the rendered cash
off the live screen — the genuine end-to-end proof the original claim asserted but had not built. Full
account of the finding and fix: the archive's appended correction.

- Build: 0 warnings, 0 errors across four projects. Tests: **467 passed, 0 failed** (458 before this
  milestone; 9 in `PlayerOwnedOperationTests.cs`, rewritten by the correction).
- `--verify` deterministic and byte-identical on `baseline` (`FEE45FD886F18CA8`), `disloyal-vincent`
  (`45CCF5ADC6EC0302`), `resentful-tommy` (`F5BD93386DE04082`); `--compare` byte-identical across all
  five trace hashes and chosen-action digests; both required viewpoint runs exit 0.
- Godot general headless self-test: **4 choices, 4 decision screens, exit 0**, unchanged from the
  accepted baseline. Transcript shows `· cash on hand 6,000` on every screen, confirming the field
  renders — the general self-test's own "always take the first option" policy never starts the
  operation, so this confirms wiring, not the operation itself.
- **Godot golden-path headless self-test (`--selftest-goldenpath`): presses the seven pinned decision
  buttons for real, in order, on the correct dates (2/11/14/17/23/27 March, 1 April), asserts the
  *opening* screen reads `· cash on hand 6,000` before any button is pressed, and the screen after the
  seventh press reads `· cash on hand 6,840`, with no unaddressed eighth decision. Exit 0. The opening
  assertion was added in a second correction — see below — after Codex found the original check proved
  only the final value, which a toolbar hardcoded to 6,840 would also have passed.**
- **The operation, played through the corrected interactive mechanism, reaches the accepted 1 April
  consequence**: `Capabilities.Cash` rises from 6000 to 6840 — confirmed through
  `session.Snapshot().Cash`, not only `session.World` — `Business.PayingTribute` for Bellini's grocery
  becomes true, and this matches the same seed run autonomously byte for byte. Seven decisions occur
  on this thread before 1 April, not the five assumed at planning time — see the archive's "Important
  discoveries".
- **The other-character-cash negative test walks the complete public `PlayerSnapshot` value graph**
  reflectively rather than comparing the `Cash` property alone. Mutation-checked twice: the first
  check (superseded — see below) replaced Vincent's own `Cash` with Marco's, which the original,
  unstrengthened single-property test would already have caught; the second correction replaced it
  with a discriminating mutation that leaves Vincent's `Cash` correct and leaks Marco's sentinel into
  `PlayerAttitude.Standing`, a nested field the single-property test could never have reached, and
  confirmed this test fails specifically on the string check rather than the numeric one.

**Reviewed by Codex a second time on 2026-08-20 (correction commit `556f2b2`), two further P2
findings, both about the strength of the verification rather than the fix itself.** First: the golden-
path Godot check asserted only the final `6,840` reading, which a toolbar hardcoded to that value
would also satisfy — it never proved a *change* occurred. Second: the negative-cash mutation check
replaced Vincent's own `Cash` with Marco's sentinel, which is exactly the leak shape the *original*,
unstrengthened single-property test already caught before this milestone's own strengthening — it did
not exercise what the recursive `ValueGraph` walk specifically adds. Corrected in the commit following
`556f2b2`: the Godot check now asserts `cash on hand 6,000` on the opening screen, mutation-checked by
temporarily hardcoding the toolbar (confirmed to fail, reverted); the cash-graph test's mutation now
leaks Marco's sentinel into `PlayerAttitude.Standing` instead of `Cash` itself, confirmed to fail
specifically on the nested-string check, reverted. Full account: the archive's second appended
correction.

### Measured — milestone 013, coverage accounting, corrected twice, accepted

**Matt accepted correction commit `a75a54e` on 2026-08-19 and closed the milestone**, with no further
commit made solely to record the review. Codex reviewed `a75a54e` with no findings.

**Not a behavioural milestone — adds no simulation code, so nothing below is new regression evidence.**
Recorded here because it is the first time this project has measured and triaged its own line
coverage rather than carrying `coverlet.collector` as an unused package reference. Full accounting,
methodology, and per-region reasoning: `docs/COVERAGE_ACCOUNTING.md`.

**Reviewed by Codex on 2026-08-19 (implementation commit `a0c6be8`), four findings — two P1, two
P2.** The triage under-enumerated 20 of 315 lines (they were reasoned about but never actually
written into the accounting's tables, so the file's own totals didn't match what it enumerated), the
Cobertura report's scope was overstated as "no exclusions" rather than described as covering only the
two loaded assemblies, and two "apparently dead" lines (`Strategies.cs:149-151`,
`Utility.cs:736`) were reachable in principle through a mutable-state path the original reasoning had
missed. All four corrected in `af6e90e`.

**Reviewed by Codex a second time on 2026-08-19 (correction commit `af6e90e`), one further P2.** The
same mutable-state standard that reclassified `Strategies.cs:149-151` was not applied to
`Sim/Runner.cs:118` in that same correction: `World.Queue` is public, `EventQueue.Schedule(...)` is a
public method accepting a nullable owner with no validation, and `Runner.Step` is the public entry
point that would process whatever gets scheduled — so the line is reachable through the public API,
not merely undertested. Corrected in the commit following `af6e90e`; corrected totals below. Not yet
accepted — see `docs/milestones/013-coverage-accounting-not-vigilance.md`'s two appended corrections
for the full account.

- **92.10% line, 84.30% branch, 3676/3991 lines covered, 315 uncovered** — measured from a clean tree
  at `1046704`, which sits on the accepted baseline `3c86ba4` with no `src/` or `tests/` change
  between them. Repeatable via `dotnet test CrimeEmpire.sln --collect:"XPlat Code Coverage"
  --results-directory ./coverage-tmp` and reading the resulting Cobertura report. **Covers
  `CrimeEmpire.Simulation` and `CrimeEmpire.Runner` only** — `CrimeEmpire.Godot` is never loaded by
  `dotnet test` (a separate Godot-hosted executable, no project reference from the test assembly) and
  the test assembly itself is not instrumented; neither is a configured exclusion.
- **315 lines triaged into 186 legitimately uncovered, 5 apparently dead, 124 live edges** — the last
  bucket is a majority of everything outside `Program.cs`'s CLI entry point. See
  `docs/COVERAGE_ACCOUNTING.md` for the full per-region list, corrected and verified to enumerate all
  315 lines exactly; nothing found was fixed, tested, or removed in this pass, per this milestone's
  rulings 2-3.
- **Self-check (ruling 7): both live edges named at planning time (`Sim/Runner.cs:311-315`,
  `Decision/Utility.cs:563-570`) appeared in the measured report unprompted**, confirming the
  instrument's negative signal is sound on two known cases before the rest of the report was trusted.
- **Ruling 6, full verification re-run**: build 0 warnings/0 errors across four projects from a clean
  tree; 458/458 tests; `--verify` deterministic and byte-identical on `baseline`
  (`FEE45FD886F18CA8`), `disloyal-vincent` (`45CCF5ADC6EC0302`), `resentful-tommy`
  (`F5BD93386DE04082`); `--compare` byte-identical across all five trace hashes and chosen-action
  digests in the table below; **all 30 viewpoint renders** (5 variants × the 6-character cast)
  produced and inspected for exceptions, none found; Godot headless self-test: 4 choices, 4 decision
  screens, exit 0, transcript byte-identical to the recorded baseline. Nothing moved.

### Accepted — milestone 012, corrected by `3c86ba4`

**Codex reviewed milestones 009–012 on 2026-08-19 (reported by Matt) and returned corrective scope —
one finding predating this milestone, recorded in milestone 011's appended correction, and two
belonging to this one.** Both are corrected in a commit appended to
`milestones/012-a-shortfall-he-cannot-attribute.md`, mutation-checked, and fully re-verified: every
figure below is unchanged by the correction, and the archive states why. **Matt accepted the
correction (`3c86ba4`) on 2026-08-19 after Codex reviewed it with no findings, and authorized
milestone 013 to baseline on it.** Milestone 012 is accepted as corrected. See
`milestones/012-a-shortfall-he-cannot-attribute.md` for the full account, the mutation-check table,
and the reasoning behind every moved figure below.

- Build: **0 warnings, 0 errors** across four projects, measured after deleting every `bin`, `obj` and
  `.godot` directory.
- Tests: **454 passed, 0 failed** (440 before this milestone). 14 added in new
  `ShortfallAttributionTests.cs`.
- `--verify` deterministic on `baseline`, `disloyal-vincent`, `resentful-tommy`. Both required
  viewpoint runs exit 0.
- Godot headless self-test: **4 choices, 4 decision screens, exit 0**, unchanged from the accepted
  baseline — the scenario it drives (`baseline`, seed 42) is one of the three variants where this
  milestone's mechanism does not fire, so no forbidden string was ever at risk of appearing.

| Variant | Hash | Chosen actions | Decisions | Violence | Bakery |
|---|---|---|---|---|---|
| baseline | `FEE45FD886F18CA8` | `7716CDDE3D0CA3A6` | 47 | 1 incident | not paying (0.50) |
| cautious-vincent | `86EC1ADA4A4E9179` | `7506045DDEB2DE14` | 24 | none | paying (0.25) |
| watchful-boss | `84AC3F65E4102EBA` | `955921AA69ABA44C` | 51 | 1 incident | paying (0.25) |
| disloyal-vincent | `45CCF5ADC6EC0302` | `BECCA9ED2E4E7137` | 48 | 1 incident | not paying (0.50) |
| resentful-tommy | `F5BD93386DE04082` | `B9B6D3BBE6A69200` | 42 | 1 incident | not paying (0.50) |

**The mechanism fires in exactly the two variants where Salvatore's attribution is actually
contradicted before day 90** — `watchful-boss` and `cautious-vincent`, matching the pre-milestone
measurement of which variants erode his confidence at all. In the other three the bakery is untouched,
exactly as before this milestone; the gate is doing what it was built to do, not merely designed to.

**No second incident occurs anywhere.** Violence stays at 1 in every variant. Per ruling 4, that is
recorded as the result rather than chased: no coefficient governing Nunzio, the bakery's resistance, or
the escalation ladder was touched. Per ruling 7, milestone 010's witness-scoping and milestone 011's
incident-vs-location rules are therefore not newly load-bearing and stay exactly where milestone 011
left them — still real, still correct, still exercised only by staged tests.

**One baseline moved, and the reason is stated in full in the archive.**
`RelationalConsequenceTests.The_scenario_produces_the_expected_number_of_conflicts(watchful-boss)`:
4 → 3 — a redistribution between whose side holds the conflict (one Salvatore-side instead of two, two
Vincent-side instead of zero), not a disappearance of the underlying mechanism. No other pre-existing
test needed a value change.

**Ruling 1 checked directly: the second errand does not crowd out the delegator's question.** In both
firing variants, Vincent's delegator's question to Tommy fires in early April, well before the
shortfall suspicion is even disclosed to him (6 April in `watchful-boss`). Milestone 011's exchanges —
Kane's allegation, Salvatore's allegation to Vincent, Tommy's answers — all still fire in every variant.

### Accepted — milestone 011, complete, corrected by `3c86ba4`

**Reviewed by Codex on 2026-08-19 (reported by Matt); Matt accepted the correction the same day and
milestone 011 is accepted as corrected — see the `3c86ba4` row above.** The section below records
items 1–4 alone, taken before item 5 existed, because ruling 7 required the two to be measured
separately. **Item 5 is recorded here, against that intermediate state:**

- **It moved no developer trace at all** — byte-identical in all five variants, so every hash in the
  table below still stands at the end of the milestone.
- **It moved 11 of 30 viewpoint renders.** Kane's five now read as her. Six more change one line,
  where the lowest-trust standing band went from "he puts no weight on anything *the man* says" to
  "he would not take her word at all" — that phrase had to change because "the man" is itself
  gendered, and it is the only wording change item 5 makes to anybody else's view.
- Tests rise from 428 to **437**; build stays at 0 warnings, 0 errors; the Godot self-test still makes
  4 choices and exits 0.

### Accepted — milestone 011, items 1–4, corrected by `3c86ba4`

**Reviewed by Codex on 2026-08-19 (reported by Matt) and accepted by Matt the same day — see the
`3c86ba4` row above.** Figures taken by the author of the change.
Recorded here under milestone 011's ruling 7, which requires the behavioural items and the pronoun
item to be measured separately so that neither can mask the other: the pronoun item moves every
viewpoint render, the behavioural items move the trace hashes, and one combined diff would hide
whichever moved less. **This section is the intermediate state, before item 5.** See
`milestones/011-…` for the account.

- Build: **0 warnings, 0 errors** across four projects, after deleting every `bin`, `obj` and
  `.godot` directory. One warning was introduced and fixed on the way: scoping the canvass guard to
  the incident stopped narrowing `s.TargetId`, and the fix reads the location off the lead instead —
  the incident's facts come from the incident.
- Tests: **428 passed**, 0 failed (406 at `824f3fc`). 21 added in a new `InvestigationTests.cs`, plus
  one theory case added to `ExposureAndDenialTests` for the corrected `Revise` rule.
- `--compare`: **5 distinct traces, 5 distinct chosen-action sequences.**

| Variant | Hash | Chosen actions | Decisions | Conflicts | Rel. read | Rel. decided |
|---|---|---|---|---|---|---|
| baseline | `0B06A3983797B16A` | `9D014A2A94EC6487` | 44 | 2 | 23 | 2 |
| cautious-vincent | `A8A1BBD12D5334C2` | `124E8FE932DD5A89` | 21 | 3 | 12 | 3 |
| watchful-boss | `F4A61680B871B8F9` | `D35876B5A78C6074` | 48 | 4 | 23 | 4 |
| disloyal-vincent | `CC7D21F508221492` | `09238CEB3AA3B2E5` | 44 | 2 | 25 | 2 |
| resentful-tommy | `EFD549BC3EF89FA8` | `18A116A6855B9E2C` | 39 | 2 | 22 | 3 |

Reasons, against milestone 010's accepted baseline:

- **`cautious-vincent` is byte-identical on every figure.** No violence, so no incident, so no case
  and no allegation. It is the control, and it was the control for milestone 010 too.
- **The incident-scoping fixes moved nothing at all.** Items 1, 2 and 4 changed one character of
  rendered output between them — `PoliceInvestigating(tommy)` became `PoliceInvestigating(tommy#11)` —
  and left every decision, every hash column above except the trace, and **all 30 viewpoint renders**
  untouched. That is expected: the fixture contains exactly one incident, so location and incident
  coincide everywhere in it. The rules are pinned by staged tests instead, on the two-incidents case
  the fixture has never contained.
- **Everything else here is item 3, the allegation route**, and it is where the decision counts move:
  41 → 44, 44 → 48, 41 → 44, 37 → 39. Two characters gain a question they never had. Kane puts her
  case to Tommy — the exchange the milestone was for — and **Salvatore puts the policy breach to
  Vincent**, which was not anticipated: he infers the breach himself, so the corroboration route
  could never offer it and he had no way to ask the man who committed it.
- **`watchful-boss` rises from 2 perceived conflicts to 4.** Both new ones are Salvatore hearing
  Vincent reject `BusinessRefusesTribute(bellini-grocery)` — a position Vincent genuinely holds, and
  the milestone-006 edge firing more often because there are more accounts. `watchful-boss` is the
  variant where Vincent owes Salvatore most and reports to him most.
- **`rel. decided` is unchanged in every variant** except `watchful-boss`, which follows its conflict
  count from 3 to 4. The relationship channel decides no more winners than before.
- **21 of 30 viewpoint renders byte-identical** to `824f3fc`; the nine that move are Tommy's and
  Vincent's in four variants and Salvatore's in one, all following the new exchanges.

**A denial to a detective is cheaper than a denial to a delegator, and still loses.** Tommy answers
Kane on 9 April: partial `+1.47`, denial `−0.82`, margin **2.29** — against the same man's denial to
his own capo at `−1.27` against `1.66`, margin 2.93. The loyalty terms do fall away as milestone 011's
premise predicted, and they are worth about a fifth of the gap. Narrowest losing margin anywhere is
**1.01**, Vincent in `disloyal-vincent` on 18 April, essentially unchanged from 010's 1.083.

### Accepted — milestone 010, `824f3fc`

**Matt accepted milestone 010 on 2026-08-18, on the strength of `824f3fc`.** It needed no corrective
round, and it is the first milestone here accepted on a review nobody but its author performed —
which is a fact about the process rather than about the code, and is why the coverage row for it says
so. The figures below were recorded by the author under ruling 6, which required every moved figure to
carry its reason. See `milestones/010-a-denial-that-can-win.md`.

**Re-verified at `824f3fc` on 2026-08-18**, from a clean tree with every `bin`, `obj` and `.godot`
directory deleted, and every figure below reproduced exactly. That run was a *measurement repeated by
the author* — evidence about determinism rather than about correctness — and it is recorded separately
from the acceptance above because the two are different things and this file's whole history is of
them being run together.

The same pass audited the committed diff against the authorized scope, empirically rather than by
recollection, and the two probes worth keeping are:

- **The only source lines deleted in the entire commit are the four that were the global witness
  scan.** Nothing else in `Utility` was touched, so no existing term was retuned on the way past.
- **The only numeric literals added anywhere under `src/` are `0.2` and `0.1`**, the two magnitudes the
  concealment step already applies to `LegalExposure` on the adjacent line, for a mechanism that did
  not previously exist. `0.25`, `3.0`, the `0.15`/`0.45` erosion rates and the `0.45` discretion
  threshold appear in neither an added nor a removed line. That is ruling 3 checked rather than
  asserted.

Out-of-scope boundaries were probed the same way and all hold: `Claim.cs` gains no member,
`Scenario/`, `Session/`, `Domain/Relations.cs` and `src/CrimeEmpire.Godot` are untouched, and nothing
added names SQLite, `Tier`, promotion or demotion.

Milestone 010 made concealment act on the concealer's own belief about the incident he is concealing,
and scoped the denial's exposure term to that incident. **Baselines moved in four of five variants,
which ruling 6 stated in advance would happen.** The mechanism by which they moved is one edge:
`Cognition.Revise` moves the reconsideration stamp, `Reporting.NeedsConveying` reads it, so a concealer
whose view of his own exposure has changed has something to report again.

- Build: **0 warnings, 0 errors** across four projects — measured after deleting every `bin`, `obj`
  and `.godot` directory, not after `dotnet clean`.
- Tests: **406 passed**, 0 failed (380 at `7ca7819`). All 26 added are in a new
  `ExposureAndDenialTests.cs` — 15 facts and 11 theory cases. `SimulationReplayTests.cs` gains a
  snapshot field rather than a test.
- Godot headless self-test: **4 choices, 4 decision screens, exit 0**, unchanged from `7ca7819`, and
  its UI text still contains none of the forbidden strings listed in the 009 section below.
- **23 of 30 viewpoint renders byte-identical to `7ca7819`.** The seven that move are Tommy's in four
  variants — his own `WitnessSawIncident` confidence rises from "plausible" to "strongly supported"
  after a clumsy cleanup — and Salvatore's in three, which gain a line as the re-armed report reaches
  him through Vincent.

| Variant | Hash | Chosen actions | Decisions | Conflicts | Rel. read | Rel. decided |
|---|---|---|---|---|---|---|
| baseline | `5E95096F02A29E4E` | `EC21B3702A113A2B` | 41 | 2 | 21 | 2 |
| cautious-vincent | `A8A1BBD12D5334C2` | `124E8FE932DD5A89` | 21 | 3 | 12 | 3 |
| watchful-boss | `EED500CFCE722C8B` | `E95050F5E205106D` | 44 | 2 | 21 | 3 |
| disloyal-vincent | `2496418CEAF47C35` | `D42236DD11812D2A` | 41 | 2 | 23 | 2 |
| resentful-tommy | `0FE0FF37BE6E68CF` | `E33EF9BACC7BE7F8` | 37 | 2 | 20 | 3 |

Reasons, per variant, against the accepted milestone 008/009 baseline in the next section:

- **`cautious-vincent` is byte-identical on every figure.** It contains no violence, so no incident, so
  no concealment. That is the control.
- **`baseline` 38 → 41 decisions, `watchful-boss` 39 → 44, `disloyal-vincent` 39 → 41.** Tommy's clumsy
  cleanup raises his own witness belief 0.6 → 0.7, which re-arms his report to Vincent, which wakes
  Vincent, which re-arms Vincent's to Salvatore. Relationship reads rise with the decision count
  (19 → 21, 18 → 21, 20 → 23) and conflicts are unchanged at 2.
- **`resentful-tommy` 38 → 37 decisions.** The one variant that falls. Tommy conceals earlier there
  (9 April rather than 13 April, milestone 008's finding), so the same re-arming lands on a different
  part of his schedule and one later wake finds nothing to do.
- **`rel. decided` is unchanged in every variant**, so the relationship channel decides the same number
  of winners as before.

**A denial still loses in every variant**, which is the milestone's result under ruling 3 rather than a
failure of it. Narrowest losing margin **+1.083** — Vincent, `disloyal-vincent`, 18 April, against
"report to salvatore, leaving out his own part". The full counterfactual table, and the three reasons
the model is still shut, are in the milestone archive.

The two Godot verification commands are still **not in `AGENTS.md` §Verification** and are still
recorded only in the 009 section below.

### Accepted — milestone 009, `7ca7819`

**Matt accepted milestone 009 on 2026-08-16, on the strength of `7ca7819`.** It took five Codex
rounds — four of them rejections: `901d345` on three findings, `b4900aa` on one further P1,
`c447a23` on that same P1 again plus a documentation contradiction, `49b71a6` on two documentation
findings, and `0f52d75` clean — and then a self-review that found four more, of which two were that
same P1 a third time. **Nine findings across the milestone, on work declared verified each time.**

Read the acceptance with that shape in view: it rests on a commit Codex never saw, and the last
round of findings came from the author. Figures below are measured at `7ca7819`.

Milestone 009 added a Godot playable shell and an engine-neutral session boundary, and changed no
simulation behaviour doing it. Its first and third corrections changed none either. **Its second
correction does**, and deliberately: restricting corroboration targets to people the actor has heard
of removes a question `cautious-vincent`'s Salvatore had been putting to a man nothing had ever told
him about.

- Build: **0 warnings, 0 errors** across four projects — measured after deleting every `bin`, `obj`
  and `.godot` directory, not after `dotnet clean`, because `dotnet clean` on a multi-targeting
  solution is not obviously equivalent and the cheaper check is the one that has produced a false
  zero here twice.
- Tests: **380 passed**, 0 failed (369 at `c0bb60f`; 353 at `b4900aa`; 343 at `901d345`; 305 before
  the milestone).
- **29 of 30 viewpoint renders byte-identical to `3f08685`.** The exception is Marco's, in all five
  variants, which *gains* one line — `· Vincent Russo has not given him an account`. He can now name
  the man who stood in his shop demanding money, and that man has told him nothing.

**Every variant is byte-identical to milestone 008's accepted baseline again**, at `7ca7819`.
`cautious-vincent` moved under Correction 2 and moved back under Correction 4; the table below is the
accepted 008 state.

**It stopped being the current state on 2026-08-17**, when milestone 010 moved four of the five
variants — see the section above, which is the current measurement. This sentence used to end "which
is also the current state", and correcting it here rather than leaving it is the whole point of the
file: the claim was true of the commit it was written about and would have been false of the file it
was sitting in the moment anything moved. That is the failure recorded under "How this record has
failed", twice.

| Variant | Hash | Chosen actions | Decisions | Conflicts | Rel. read | Rel. decided |
|---|---|---|---|---|---|---|
| baseline | `6EB3F6B996CFC631` | `38B7183ED2EEF34A` | 38 | 2 | 19 | 2 |
| cautious-vincent | `A8A1BBD12D5334C2` | `124E8FE932DD5A89` | 21 | 3 | 12 | 3 |
| watchful-boss | `DCEDCFF27928266F` | `4F15ECD8B7A593BB` | 39 | 2 | 18 | 3 |
| disloyal-vincent | `E164E0A74E2EC7DC` | `3D7F2B79BA4DC3E3` | 39 | 2 | 20 | 1 |
| resentful-tommy | `982EC77BD5C253CB` | `18B507EBBE4FBA7E` | 38 | 2 | 19 | 3 |

Read Corrections 2 and 4 together rather than singly. Correction 2 took `cautious-vincent` to
`96EAE1A72850F3D7` / `1F660F63735133FC`, 19 decisions and 2 conflicts, by removing Salvatore's
5 April question to Tommy: nothing had ever named Tommy to him, and the generator was reading the
organisation roster. Right about the generator, wrong about the scenario — **Tommy had already
approached Salvatore with a question of his own**, and the model never recorded that being asked
something makes you able to name the asker. Correction 4's `Relations.Meet` records it, the question
returns with a cause behind it, and every figure goes back to 008. Full account in
`milestones/009-godot-playable-shell.md`, Corrections 2 and 4.
- **Four of the five variants are unchanged from the milestone 008 baseline below** on every figure —
  trace hash, chosen-action digest, decision, report, request and conflict counts, and both
  relationship columns. `cautious-vincent` is the exception and its new baseline is the table above.

  These two bullets said "Nothing moved" and "all 30 viewpoint renders are byte-identical" until
  2026-08-16, which was true of the shell and its first correction and **false from the second one
  onward** — the table above had already recorded the `cautious-vincent` move while these lines went
  on denying it. Corrected under Correction 3. The failure is the one this file is named for: a claim
  that stayed true of the commit it was written about and false of the file it was sitting in.
- **29 of 30 viewpoint renders are byte-identical to `3f08685`** — five variants × six characters,
  diffed against a scratch worktree. That comparison is what matters for `IntelligenceWriter` being
  rewritten to consume `PlayerView`'s snapshot rather than derive the source limit itself. The one
  exception is Marco's view, which gains a line under Correction 4 — a behaviour change rather than
  a rendering one.
- Debug and Release both build; `Release` maps to the Godot project's `Debug` configuration, because
  `Godot.NET.Sdk` defines `Debug;ExportDebug;ExportRelease` and has no `Release`.

Two verification commands are new and **are not in `AGENTS.md` §Verification**, which milestone 009
had no ruling to edit:

```powershell
dotnet build src/CrimeEmpire.Godot/CrimeEmpire.Godot.csproj
& "$env:USERPROFILE\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe" --headless --path src/CrimeEmpire.Godot -- --selftest
```

The second drives the real interface — the same panels through the same methods a person would see —
for ninety in-game days, taking the first offered option at every pause, and prints every string the
node tree contains between `== CE-UI-TEXT-BEGIN ==` and `== CE-UI-TEXT-END ==`. At the recorded
commit it makes 4 choices, renders 4 decision screens, exits 0, and its output contains none of:
`Dorato's bakery is holding back what it owes` (the fixture's designed hidden fact — the bakery really
is refusing and no character holds the claim), `dorato-bakery`, `Nunzio`, any rejected-candidate
wording, or any decimal number.

### Accepted — milestone 008, `7e0700e`

Codex reviewed it with **no findings**, and **Matt accepted the corrected milestone 008
implementation on 2026-08-16. Milestone 008 is closed.** See
`milestones/008-relationship-readers.md` and its two appended corrections.

Two earlier rounds were rejected, both with the findings accepted by Matt:

- **`7a9773b`** — one finding. Three of loyalty's four contributions were emitted fused under a
  `Trust | Obligation | Belonging` union flag, so they were separately computed and not separately
  inspectable as ruling 3 required. Corrected by `9a29342`, which splits them.
- **`9a29342`** — two findings. Its recorded verification hashes were false, and the unclamped bond it
  introduced rested on a documented range that nothing enforced. Corrected by the commit this
  baseline describes.

- Build: **0 warnings, 0 errors — measured after `dotnet clean`.**
- Tests: **305 passed**, 0 failed (292 at `9a29342`, 285 at `7a9773b`, 276 before the milestone).
- Five variants, deterministic on repeated runs.

| Variant | Hash | Decisions | Reports | Requests | Conflicts | Rel. read | Rel. decided |
|---|---|---|---|---|---|---|---|
| baseline | `6EB3F6B996CFC631` | 38 | 6 | 5 | 2 | 19 | 2 |
| cautious-vincent | `A8A1BBD12D5334C2` | 21 | 2 | 4 | 3 | 12 | 3 |
| watchful-boss | `DCEDCFF27928266F` | 39 | 7 | 5 | 2 | 18 | 3 |
| disloyal-vincent | `E164E0A74E2EC7DC` | 39 | 6 | 5 | 2 | 20 | 1 |
| resentful-tommy | `982EC77BD5C253CB` | 38 | 7 | 5 | 2 | 19 | 3 |

**These hashes replace the ones previously recorded here**, which were `20DD67E8CA4CB5AD` /
`D2D070005176426D` / `6FC6D3243B0020E1` / `5A91CFE9F3532E63` / `947BD13F07FE2AEA` and were wrong. They
were measured at `9a29342` before a late widening of the diagnostic listing and never re-measured
after it; the full account, including how the cause was established, is in the second correction of
`milestones/008-relationship-readers.md`. The chosen-action digests recorded alongside them were
correct throughout.

**`7e0700e` changed no behaviour, verified against the full rendered trace rather than a
filtered subset.** `9a29342` was built in a scratch worktree and every variant diffed in its entirety:
**byte-identical in all five.** That method is deliberate — the previous round diffed a subset that
excluded the diagnostic block, which was the only place its change appeared, and that is precisely how
the false hashes survived a verification step.

Hashes at `7a9773b` were `3BA97219464FC2E4` / `EC664E9FB52010B7` / `51AB00158218ACD0` /
`709F0B6E4B90A2F4` / `3D91B931EC2DAF3B`.

`--compare` reports **five distinct traces and five distinct chosen-action sequences**. That second
figure was four at milestone 007, and the change is the milestone's behavioural result: at seed 42
`resentful-tommy` no longer chooses identically to `baseline`.

**Read the two new columns as the milestone's central measurement.** "Rel. decided" is the number of
decisions whose winner would differ with relationship state removed, computed by re-ranking on
`TotalWithoutRelationships()`. It is 1–3 in every variant, so **the relationship channel is
load-bearing** — a stronger result than milestone 007's `0.0377` implied, because 007 could only see
the trust-to-partial-report path, which is the one place two loyalty reads nearly annihilate.

**All five hashes move, and mostly not for a behaviour reason**: the trace now carries the
relationship diagnostic block. The scoring change was measured separately, before the trace was
touched, and that is the more informative comparison — `watchful-boss` was **byte-identical** there,
and it is the only variant in which nobody holds a grievance against anybody. See
`milestones/008-relationship-readers.md` for both hash tables.

**Decision, request and conflict counts are unchanged** from milestone 007. `resentful-tommy`'s
reports rose 6 → 7, downstream of the fork.

### Superseded — milestone 007, `974a88a`

Superseded as the current accepted state by milestone 008 on 2026-08-16, and kept because it is the
last baseline before relationship contributions were split by facet and before grievance left the
clamped loyalty sum.

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

**The distinctness claim states its own caveat, and since milestone 008 the caveat no longer bites.**
Milestone 007 introduced a chosen-action digest computed from structured decision fields, so
`--compare` reports trace distinctness and behavioural distinctness separately and the weaker claim
cannot be read as the stronger one. At 007 those figures were five and four: `resentful-tommy` chose
identically to `baseline` throughout. **At milestone 008 they are five and five.** Tommy's grievance
against Vincent stopped being clamped away, and on 9 April he conceals the incident himself rather
than reporting it to the man he resents. The margin is 0.0279 against ±0.05 per-candidate noise and
the divergence is seed-dependent — present at 42 and 31337, absent at 1, 7, 99 and 2024 — so the
figure is honest rather than robust, and the archive says so. A future change that made two variants
converge behaviourally is still caught by the second figure.

The variant was added to stage an executor denying his own act to his delegator, and still does not
achieve it. The delegator now asks and the executor now answers, in play; he answers honestly, because
he believes the street saw him.

**Milestone 010 attacked exactly that and did not move it**, and the reason is now measured rather than
guessed. A concealer can now act on his own exposure and a denial is priced from its own incident, but
the denial still loses in every variant — narrowest margin 1.083 — and three things hold it shut: one
cleanup attempt is worth `-0.2` where roughly `-0.4` is needed; Tommy's Discretion of `0.30` cannot
clear the `0.45` threshold at any seed, so his cleanups are always clumsy and move his belief the wrong
way; and Vincent, whose denial comes closest, is never offered a cleanup because his dominant pressure
is `RevenueShortfall` and never `LegalExposure`. **The man who can clean up is not offered a cleanup,
and the man offered one cannot clean up.** Full counterfactual in
`milestones/010-a-denial-that-can-win.md`.

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
- A score component's relationship derivation is read from the facet it was tagged with at the point
  of computation, never from its component name. A term named "relationship effects" that reads only
  a trait must be tagged `None` and must not appear in the relationship channel.
- **No score component carries more than one facet.** Each of loyalty's four contributions — trust,
  obligation, Belonging, grievance — is emitted as its own component, and moving one dimension moves
  only its own component. Separately computed then summed is not separately inspectable.
- The bond is an unclamped sum of its parts, so emitting the parts equals emitting the sum. A clamp
  reintroduced there cannot be split honestly and must fail.
- **Every input to the bond is clamped where it enters its type**, and that is enforced rather than
  documented: `Relations` on relationship writes, `Psychology`'s constructor on traits and drives,
  with `With` delegating to it. Regression tests drive both ends out of range through the public API.
  Grievance is the deliberate exception and must stay able to exceed the bond.
- **A verification figure is measured after the last edit, not merely after an edit.** Hashes recorded
  from a run that predates a later trace-affecting change are false verification, whatever else in the
  commit was checked. A diff that filters out the region a change touches cannot substitute.
- Belonging appears in the diagnostic listing but contributes zero to gross, net and the
  counterfactual. It is a drive, not relationship state.
- Every retained relationship dimension is read by some decision in a natural run of all five
  variants. A dimension with no reader is removed rather than given a purpose to justify keeping it.
- Grievance is applied outside the clamped loyalty sum, as its own named component at each reader.
- A report's two relationship considerations — the standing reporting buys and the cost of the
  candour selected — remain separately identifiable. A change that preserves their net while merging
  them must fail.
- The relationship diagnostic applies no significance cutoff, so a cancelling pair stays visible.
- The relationship counterfactual reuses the breakdown's own noise draw rather than re-scoring.
- No relationship diagnostic reaches player-facing output.

Added by milestone 009:

- **A session with nobody controlled renders a byte-identical developer trace to the batch runner**,
  in every variant. Compared on the full rendered trace rather than a structural snapshot, because a
  snapshot is its own comparator and a forgotten field makes it blinder rather than failing.
- **Auto-resolving every pause in a *controlled* session reproduces the same trace**, in every
  variant. This is what fails if `Prepare` and `Resolve` drift — a belief update on the wrong side of
  the split, an id allocated in a different order, an RNG stream drawn twice.
- **Four stepping patterns under one player policy agree**: one call, day by day, week by week, and
  twenty-five single events then a fast-forward. The policy is a pure function of the offered options,
  so "the player choices are identical" holds by construction across the four.
- **Player options are ordered by candidate id, and the preferred option is demonstrably not always
  first.** The second half is the mutation check on the first: without it, sorting by id is satisfied
  by coincidence whenever rank and id happen to agree.
- **An option the character's own filters rejected cannot be chosen**, asserted against a candidate
  the production filter actually refused in that very deliberation — not an invented string — and the
  refusal leaves the session usable.
- **Time cannot move while a choice is outstanding.**
- **A prepared decision commits exactly once.**
- **A hidden fact reaches no player surface**, checked with two facts of different kinds: one planted
  in another character's cognition at full confidence for the whole run, and one true of the world and
  held by nobody. The forbidden wording is computed from the production narrator, so a renderer that
  changed its phrasing cannot slip past prose the test hardcoded.
- **No player-facing phrase carries a decimal number.** Confidence, trust, fear and grievance severity
  are hidden state; every phrase is qualitative.
- **`SimulationSession`, `PendingDecision` and `PlayerSnapshot` expose no `World`, `TruthLog`,
  `DecisionRecord`, `ScoreBreakdown`, `PreparedDecision`, `Candidate`, `Rejection`, `Report`,
  `Character`, `Cognition` or `Agenda`**, asserted by reflection over their public members.
- **The Godot interface's own text is checked, not just the data behind it** — collected from the live
  node tree across every screen the run builds, headlessly, through the same methods a person sees.

Added by milestone 009's first correction:

- **No string authored by a scheduler or a generator reaches a player-facing surface.** Asserted over
  natural runs against the whole vocabulary the run actually used — every `DecisionRecord.Trigger`,
  every generated `Candidate.Description`, every `Rejection.Reason`, every `Agenda.Reason` — never
  against a hand-written list of the ones somebody thought of.
- **The occasion vocabulary is closed and silent by default.** Every `EventKind` outside an explicit
  allow-list yields no occasion, so a new event kind is mute until somebody rules on it. A
  `RespondToTrigger` agenda's `Description` never passes, because it *is* the trigger cause.
- **A delegated operation's failure or completion tells its owner nothing until somebody does.**
  Staged with owner and executor distinct, driving the real `Runner.Step` and the real projection.
- **The hidden-fact check covers pending decisions and rendered UI strings**, at every pause, not only
  a snapshot at the end.
- **Every collection on the player boundary is genuinely read-only**, checked against real instances
  at runtime rather than declared types — a `List<T>` behind an `IReadOnlyList<T>` can be cast back.
- **The player-facing type graph is walked recursively**, not one level. `Claim` is forbidden in it:
  it carries `EventId`, a truth-log counter, and the boundary hands out `PlayerClaim` instead.
- **Option ids are opaque, stable tokens**, not candidate ids, and `Pipeline.Resolve` remains the sole
  authority on whether an action was open to the character.
- **The Godot self-test exits nonzero when it fails**, verified by sabotage rather than by reading the
  code.

Added by milestone 009's second correction:

- **A candidate's target must be somebody the actor has heard of.** `ctx.OrgMemberIds` is the
  authoritative roster and is not a list of people anybody knows about; anything that turns a member
  into a target intersects it with `ctx.AcquaintedIds` first. Staged with a member nobody has heard of
  whose id sorts first, so the generator's own ordering would pick him if the limit were absent.
- **The filter admits as well as excludes.** One claim naming a stranger makes him approachable, and a
  soldier can still name his own capo — a rule that narrowed what can honestly be expressed would be
  its own defect.
- ~~**"Who has this character heard of" has one derivation.** `Acquaintance.HeardOf` is it.~~
  **Superseded by the third correction's category below**, which names `Acquaintance.KnownTo`.
  `HeardOf` is the cognition-only half and is now `internal`; a live regression category pointing at
  it would be the rejected rule left standing as though it were the rule.
- **Requests are checked at the moment each one is made**, by stepping the run — never against the
  asker's acquaintance set at the end, which is a superset and made the check very nearly vacuous.

Added by milestone 009's third correction:

- **"Who could this character name" has exactly one public derivation**, `Acquaintance.KnownTo`, and
  both `PlayerView.KnownPeople` and `GeneratorContext.AcquaintedIds` read it. The cognition-only half
  is `internal`, because a test that compared the player view against *that* — while the generators
  used a wider set — is how a leak survived a correction written to close it.
- **Institutional knowledge comes from an institution.** The widening is the holders of
  `Organization.Offices` and `BossId`: named formal posts. Never `Pipeline.SuperiorOf` or
  `SubordinatesOf`, which are authority scans over `world.Characters` and are the roster under
  another name. *Naming a thing after its justification does not make it the justification.*
- **The staged stranger must be authority-adjacent and hold no office**, or the roster-derived route
  excludes him for a reason unrelated to knowledge and the test passes without exercising anything.
- **Divergence between the two readers must be stageable.** In the accepted scenario every character
  has a relationship with everybody he could ask, so the narrow and wide sets coincide and any
  natural-run test passes whichever one each reader uses. The check is staged on a newcomer who has
  heard of nobody, where they differ.

Added by milestone 009's fourth correction:

- **Every candidate's target is somebody the actor could name — checked over every `ActionKind`, not
  one.** Scoping this to `SeekCorroboration` left the same defect live in `Concede`, `Refuse` and
  `ReportToSuperior` through a correction written to close it. *A test shaped like the bug it was
  written for cannot find that bug's siblings.*
- **An encounter registers.** Having a demand put to you, or a question, makes the other man
  nameable — `Relations.Meet`, an all-zero relationship that scores exactly as a stranger does.
  Logged in `World.Encounters` so `A_full_run_creates_no_relationships_by_reading` keeps its
  invariant now that a legitimate route creates all-zero relationships.
- **The occasion says which act woke him**, keyed on the event's structured note. One phrase for all
  five `RoleReview` schedulers told a man he was doing his rounds when his soldier had just reported
  in — not a leak but its inverse, withholding what he knew and asserting something false instead.
- **The focus is phrased from his own state**, never passed through from `Agenda.Description`, which
  carried `StrategyInstance.Label` and raw `PressureKind` names.
- **The Godot self-test drives the interface through real button presses**, so the rebuild that
  frees the button its own signal came from is exercised rather than reasoned about.

Added by milestone 010:

- **A concealment names the incident it is concealing**, on the instance, as an event id — never the
  target, and never the default 0, which would make every unidentified claim share one incident.
- **Quieting the witnesses changes a belief and nothing else.** No trace removed, no truth-log entry
  altered, nobody else's cognition touched. Mutation-checked by making each of those happen in turn,
  which is the only way to check an assertion that nothing happened.
- **The belief that moves belongs to the executor, not the owner.** Concealment is never delegated in
  the fixture, so owner and executor coincide everywhere and a natural-run test cannot tell them
  apart; the split is staged.
- **A cleanup can be wrong in both directions.** Clean lowers his confidence that the street can place
  him, clumsy raises it, and the world is untouched either way — so a man who believes he has cleaned
  up and has not remains available, which is why `ResolveViolence` files that belief as an inference.
- **Only the first concealment step touches what he thinks about witnesses.** The second is about
  records.
- **A man can revise a conclusion he drew himself, and nothing else.** `Cognition.Revise` refuses a
  record he established by doing or seeing, and refuses one somebody gave him — the first is what
  `Provenance` defends, the second is an account he has to be argued out of. `Learn` still discards a
  less confident inference, and that is pinned, because routing a revision back through it is the
  simplification that would silently restore the defect.
- **A revision is a reconsideration**: stance and acquisition time stand, the reconsideration stamp
  moves, and `Reporting.NeedsConveying` therefore re-arms.
- **A denial is priced from the witnesses to the incident it is about**, by `Claim.EventId`, never
  from a maximum over every witness belief the actor holds. Asserted as a comparison between staged
  worlds so the test pins the scoping rather than a coefficient.
- **A suppressed claim with no event id is priced as though nobody saw it**, not against every other
  idless claim.
- **No viewpoint is shown a belief he does not hold**, over all six characters and all five variants —
  and asserted at the claim level, never against the narrator's phrasing appearing in rendered text.
  The phrasing form of this test is vacuous: the narrator's sentence for a witness claim does not
  appear in `IntelligenceWriter`'s output for *any* viewpoint, including the ones who hold it, so the
  assertion cannot fail. It duly survived a mutation that made the view read every character's
  cognition. *An assertion that a string is absent is worth nothing until you have seen it present.*

Added by milestone 011:

- **A case is about an incident, not an address**, at every point in the investigation path: the lead
  it picks up, whether it has closed, and which stale claims it demotes. Two beatings at one shop are
  two cases. The accepted scenario contains exactly one incident, so **a natural-run test passes
  whichever rule the code uses** — this is staged, and has to be.
- **`Generators.SameIncident` is the one predicate**, and event 0 is not an incident.
- **A failed canvass demotes the lead it was opened on, and only that one** — and does not demote a
  lead somebody *gave* her, because testimony is something she has to be argued out of. Unreachable
  in every variant at seed 42, so the staged test is the only thing standing behind it.
- **A man may revise his own reading as well as his own reasoning.** `Provenance.IsOwnReading` is the
  named rule; admitting `Inference` alone put `Discovery` in with Participant and Witness, which is
  the bundle that file exists to prevent. *A fifth rule keyed off a category has to answer the same
  question the other four did.*
- **Police interest names the incident the suspect is being looked at over.** A `PoliceInvestigating`
  claim with no event id cannot be answered, corroborated or made stale — it is where a heat bar
  starts.
- **A man may put an allegation to the person it names**, and that route is the exact complement of
  corroboration: what you were told you check with somebody else, what you worked out yourself you
  put to its subject. Neither generator may cover the other's provenance, or one act is offered twice
  out of a bounded six. It is bounded by `CanAsk`, by `Acquaintance.KnownTo`, and by whether he has
  already answered; it never fires on the actor himself.
- **Being asked by somebody who is not your superior still offers candid, partial and false.** The
  redirect must not confer rank, and a detective who cannot be lied to is not a detective.
- **A character is described as themselves on every player-facing surface**, checked over every
  viewpoint in every variant through the production renderer, and against the pronoun sets rather than
  against prose written in the test. **The attitude list cannot cover a woman in a natural run** —
  it filters to non-zero trust, fear or grievance and Kane's only relationship is all-zero — so that
  path is staged and the emptiness is asserted rather than left to look like coverage.
- **Verb agreement is real and is not faked.** `Pronouns.Verb` takes both forms because English
  agreement is not a suffix rule, so `They` is usable rather than decorative.
- **A mutation that does not compile is not a passing test.** The harness reported build failures as
  "no test failed" and hid two unpinned rules until it was fixed.

Added by milestone 012:

- **A boss's suspicion that a shortfall has another cause is gated on facts the model already
  records** — `InformationRecord.Contested` and the live `OrgCondition.RevenueLoss` — never a fresh
  confidence threshold invented for the purpose. Both halves of the gate are mutation-checked
  separately, each against a *named* test.
- **The suspicion names a domain and never a business.** Checked against both business ids by name in
  the fixture, not merely against the claim's own subject, which would pass vacuously if the claim
  happened to be built correctly but rendered wrong.
- **Only the organisation's leadership forms the suspicion.** Staged by giving a non-boss member the
  identical contradiction shape directly, so the only thing standing between him and the conclusion is
  the actor-scoping gate itself.
- **A mark-selection fallback that always resolves to the same business and is always then rejected is
  not a working fallback**, whatever it looks like in the generator's source — it produced a rejected
  candidate every time and a considered one never, in every variant, at every seed, for as long as the
  fixture has had a second business. Pinned directly: with no named refuser and no suspicion of a gap,
  nothing is proposed at all, not even the old rejected noise.
- **`SimulationSession.ResolveAutomatically` reproduces the batch-accepted history while a session-driven
  test still records what was offered at every pause** — the check ruling 6 needs. "Always choose the
  first option offered" is not equivalent to it: `PreparedDecision.Available`'s candidate-id ordering
  (milestone 009, ruling 5) is deliberately not rank order, so that policy drives a different, lower-
  ranked history and can silently fail to reach the state under test.
- **A relationship-conflict count that moves is read for its composition, not only its total.** The
  same total can hide a redistribution between which side of a pair holds the conflicts; the archive
  records both.

## Review checklist

### Architecture

- Does the simulation library remain engine-independent? Since milestone 009 it multi-targets
  `net8.0;net10.0` so the engine can load it — that is a packaging fact, and it must stay one. A
  `using Godot`, an engine-conditional `#if`, or a package reference to `Godot.NET.Sdk` anywhere under
  `src/CrimeEmpire.Simulation` is the thing this question is about.
- Does anything player-facing derive its own source limit rather than consuming `PlayerView`'s
  snapshot? Two answers to "what may this character see" is the failure this project produces most
  reliably.
- Does decision code read character-relative information rather than truth? Decisions must use
  `PerceivedSituation`, not `World`, for situational facts — do not add a world reference to the
  perceived view or expose raw cognition to candidate generators.
- Do traits influence salience and evaluation without triggering behaviour?
- Are strategies bounded and authored rather than unrestricted plans?
- Are policy breaches possible and consequential rather than mechanically forbidden?
- Does this assume the acting character is a criminal, or a member of one specific organisation,
  where it could instead be stated in terms any `Character` satisfies? `DESIGN_DECISIONS.md`'s
  "Player-neutral architecture and future institutional roles" names the shape this must not take —
  `PlayerHeatManager`, `PlayerWantedLevel`, `PlayerKnownInformation`, an `if player_is_cop` branch, or
  any new field keyed to "the player" rather than to a character. This is not scope for building other
  roles; it is a cheap check against quietly closing that door while building the one role that is
  scoped.

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
- **Does the commit carry a "where to look" note** — a short surface, written by the implementer, that
  names the claims in this diff that would be expensive if wrong? Introduced in milestone 013 because
  every defect Codex has ever found here was in a place the author had already convinced himself of;
  naming the load-bearing claims up front is where a reviewer's attention should land first, not a
  substitute for reading the rest of the diff.

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

Six patterns have produced repeat findings. The detailed cases are in the milestone archives; what
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

Milestone 008 found this one inside the instrument built to measure it, which is the variant worth
naming. The only way to ask how much of a score came from a relationship was to filter components by
the name `relationship effects` — and 36% of those components read no relationship state at all, being
`−0.45 × proud` wearing a relationship label. Two production tests already aggregated that way and the
new diagnostic was about to. It also swept the Belonging share of loyalty, which is a drive, into a
figure reported as relational. The fix was to record the derivation where the value is computed.
*Is this grouping by what the thing is, or by what it is called?*

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

**Rewriting an append-only archive at closure.** Twice, in consecutive milestones and by the same
reasoning both times: `6355347` replaced milestone 006's status line and rewrote its ruling-provenance
paragraph, and `46a5651` did the same to milestone 007's header. Neither was vandalism — each was
correcting something genuinely wrong, which is what makes the pattern worth naming. Closure and
correction both feel like updates to the top of the file, and both belong at the bottom of it. Note
also that in the 006 case the correction was **already appended** further down and the rewrite added
nothing but the loss.
*Is this an edit to what the record said, or an addition to what the record now says?*

**Recording a review that did not happen.** See below; it is the pattern this file exists to stop.

## How this record has failed

The predecessor of this file misreported status five times. It claimed verification that had not
happened, first for `b8fe921` and then for `e83dacf`; it wrongly recorded `fb2c84d` and `714fbc3` as
never reviewed; and its next-step gate went stale twice — first telling readers milestone 004 was
active and approved, then telling them it was blocked on three unfixed findings after all five had
been corrected and accepted.

**A sixth, in this file, at `1c6889f`.** It recorded `46a5651` as "status not established" — the same
shape as wrongly recording `fb2c84d` and `714fbc3` as never reviewed, and produced the same way. The
author wrote the row from what he had been told rather than from what had happened: nobody had
announced a review to him, so he wrote that none had occurred, when the review had happened, had
rejected the commit, and was the reason he was writing the row at all. The rejection was visible in
the work he was doing and invisible in the sentence he wrote about it.

**A seventh, at `9a29342`, and it is a false *verification* rather than a false review.** The archive
and this file both recorded five trace hashes that the commit does not produce. They were real
measurements from a real `--compare`, taken before a small, late, deliberate change to the diagnostic
listing and never re-taken. The commit was not unverified — it verified itself with a line-by-line
diff against its parent that **excluded the diagnostic block**, correctly for the question "did
behaviour change?" and fatally for the question "are these the hashes?". Two sound checks, and the
answer lived in the gap between them.

This is milestone 006's zero-warning claim in a new costume, and the milestone that reproduced it had
already written that lesson into its own archive. Neither vigilance nor a checklist caught it; what
would have caught it is a rule. **Re-measure after the last edit, and never report a figure from a run
that predates any change to what it measures.** Carrying question: *was this measured after the last
edit, or merely after an edit?*

That is worth separating from the two mechanics below, because neither explains it. Reviews were
being taken in order, and no gate went stale. **A row is a claim about the world, and "I was not told
about a review" is not evidence that none happened.** Absence of a review report is not a review
outcome, and the honest options when the outcome is genuinely unknown are to say the status is
unknown *to the author* or to go and find out — not to record it as though the world had been
checked.

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
