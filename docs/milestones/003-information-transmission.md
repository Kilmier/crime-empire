# Milestone 003 — Information Transmission Slice

Status: complete. Awaiting Codex review.

## What was attempted

A deliberately narrow slice of `SIMULATION_ARCHITECTURE.md`'s emergence prototype (Validation
Sequence step 2), which as written bundles six subsystems. Matt scoped this to the information half
only: **direct observation, one explicit report/message channel, one deceptive or incomplete
report, one conflicting source, and a player-readable history constrained to the viewpoint
character's information — with generalized rumor propagation explicitly excluded.**

That scope is near-verbatim `INFORMATION_AND_LEGIBILITY.md`'s own "Pre-MVP Kernel Scope", and that
document's worked test scenario is the harbour scenario milestone 001 already built.

## What was completed

- **`Report` as a first-class object** (`Domain/Report.cs`), modelling the doc's "Report or
  Message" contract: sender, recipient, asserted claims with the stance and confidence *as
  asserted*, a `Withheld` list, and a `ReportCandor` of Candid/Partial/False. Stored on
  `World.Reports` as developer truth.
- **`Reporting.Compose`/`Deliver`** (`Org/Reporting.cs`) — the single report channel. Reports are
  composed from the sender's own `PerceivedSituation`, never from world state, which makes "sources
  never communicate facts unavailable to them" structural rather than a discipline.
- **Candour as a scored decision.** Candid/Partial/False are three *candidates*, generated only
  when the actor holds a claim naming himself as having used force or breached a policy (nothing
  else is worth lying about), gated by salience and scored by utility against self-protection,
  believed witnesses, and loyalty to the recipient. No code branches on a trait to produce a lie.
- **Append-only testimony** (`Domain/Cognition.cs`). `Receive` records every account verbatim and
  never collapses it, while the settled belief keeps one stance per claim so no existing decision
  changed what it reads. Contradiction *erodes* confidence rather than letting the newer account
  overwrite the older; corroboration only counts from an independent sender; direct observation
  resists hardest but is not immune. `IsContested` reports disagreement.
- **`SeekCorroboration`** — a recipient-initiated use of the same channel, which
  `INFORMATION_AND_LEGIBILITY.md` sanctions directly ("Leaders can request audits, seek
  corroboration"). Needed because `Pipeline.SuperiorOf` returns the *lowest* authority above an
  actor, so Tommy reports to Vincent and could never reach Salvatore unaided.
- **`IntelligenceWriter`** (`Runner/Trace/`) plus a `--viewpoint` flag: a player-facing account
  built only from one character's beliefs and testimony, with qualitative confidence only,
  conflicting accounts side by side with attribution, and an explicit "what he cannot settle"
  section. Kept in a separate file from `TraceWriter` because the doc requires developer traces
  stay separate from player-facing information.
- **`disloyal-vincent` variant** — the mirror of `watchful-boss`, cutting the bond without touching
  a trait, which is what flips Vincent from omission to outright denial.
- Observation is now genuinely *direct observation*: discovery previously recorded claims as
  `SourceKind.Rumor` sourced to "the street", which is rumour vocabulary for a mechanic that is a
  single discovery roll with no network, no mutation and no re-transmission.

## Tests / success criteria and results

`dotnet test` — **17/17 passing** (was 6). `--verify` is DETERMINISTIC on all four variants, and
`--compare` reports 4 configurations, 4 distinct histories.

New tests in `InformationTransmissionTests.cs`:

- **No-leak (the load-bearing one).** For every claim held by anyone or asserted in any report, if
  the viewpoint character does not hold it, its exact rendered wording must not appear in his view.
  Runs across all four variants and calls the renderer's own `Describe`, so it cannot pass while
  the renderer drifts.
- Developer-only material (report framing, candour, utility component names) never reaches the view.
- A report conveys only what its sender holds, and withheld claims are genuinely absent.
- An incomplete report leaves the recipient without what was withheld.
- Candour distinguishes lying from omitting: a denial asserts `Rejects` on a claim its sender
  holds; an omission never asserts `Rejects` at all.
- A contradicting source leaves a conflict that is still attributable to a named sender.
- Being contradicted shakes a directly observed belief without erasing it, and does not rewrite
  its acquisition time.
- Corroboration counts only from a new source, not the same source repeating itself.
- `SimulationReplayTests.Snapshot` extended to cover reports and testimony.

**The no-leak test was mutation-checked.** Pointing the renderer at every character's cognition
instead of the viewpoint character's made it fail in all four variants; it was then restored. A
leak test that cannot fail is worth nothing, and this one was confirmed to fail for the right
reason before being trusted.

The real success criterion was reading `--viewpoint salvatore` and asking whether a person could
form a plausible account without being handed the true one. On `disloyal-vincent` at seed 42 he
ends up holding that Vincent went outside the rule — marked **contradicted**, with his own eyes and
Vincent's denial listed side by side — while never learning it with certainty. On `baseline` he
never learns of the breach at all: Vincent's partial report names Tommy as the man who used force
and quietly omits his own order.

## Important discoveries

- **A candidate that can never win is worse than no candidate.** `ReportCandor.False` was
  initially unreachable in every configuration because of a flat `0.9` risk penalty on lying.
  Re-derived so the risk is carried almost entirely by *believed witnesses*: a denial that nobody
  can contradict is genuinely better than an omission, because an omission leaves the fact
  retrievable. The man who thinks the street saw him now omits; the man who thinks nobody saw him
  denies. This was a modelling error surfaced only by trying to reach the behaviour, not by review.
- **The person who orders a breach must know he ordered it.** `ResolveViolence` created a
  `PersonBreachedPolicy` claim naming the strategy's owner but only offered it to the *boss* as a
  discovery. Vincent therefore held no claim naming himself, could only ever report candidly, and
  the delegate who carried out the violence absorbed all the exposure. Fixed by having the owner
  learn his own breach directly.
- **One event must mean one chance to notice per person.** Adding proximity-based observation gave
  the boss two independent rolls on the same event (one as boss owed an account of a breach, one as
  a man working that district). He noticed twice and deliberated twice at the same instant on the
  same news. Fixed by collecting observers into a map before scheduling, best access winning.
- **`acquired_at` and `last reconsidered_at` are different fields, and conflating them silently
  rewrites the player's timeline.** Corroboration was updating `AcquiredAt`, so something learned
  in March displayed as learned in May purely because somebody mentioned it again. The doc's
  Character Information Record lists both; now so does `InformationRecord`.
- **Report exchanges need a termination condition or they run until the calendar does.** Two
  separate loops appeared: a boss re-asking the same man forever (fixed — you cannot seek
  corroboration from someone who has already given you their account), and a subordinate
  volunteering the same report every few days (fixed — reporting requires having learned something
  since you last spoke). Both are instances of the same omission: nothing in the model said an
  exchange had been *spent*.
- Milestone 001's delegation makes the scenario richer than the doc's worked example assumed:
  Vincent delegates to Tommy, so the man who ordered the breach and the man who committed it are
  different people with different things to hide, and they conceal *different* claims from the same
  boss. This was emergent, not designed.

## Deferred work

- **Generalized rumor propagation** — explicitly out of scope, and still out. `SourceKind.Rumor`
  remains in the vocabulary but nothing now produces it.
- **A conflict of the form "one source asserts what another conspicuously omits" is not detected.**
  `IsContested` requires an actual denial. In the baseline both Vincent and Tommy omit rather than
  deny, so their accounts differ without formally conflicting. Whether that shape deserves
  first-class treatment is a real design question, not an oversight.
- Attribution on a corroborated belief still credits only the first source; the full picture lives
  in testimony. Fine for now, but a `SourceChain` is the eventual answer the doc gestures at.
- Media/public coverage, the case-board investigation model, tier transitions, and relationship
  schema (`OPEN_CONCERNS.md` #3, still open and still blocking "richer relationships and
  grievances").
- Carried over from milestone 002 and still untouched: the redundant `TargetFramework` override in
  `CrimeEmpire.Simulation.Tests.csproj`, and unverified Godot/`net10.0` compatibility.
- `OPEN_CONCERNS.md` #4 (trait vocabulary not closed) was already stale before this milestone —
  milestone 001 closed it in `Domain/Psychology.cs`. Not corrected here to keep this commit to one
  concern; worth a separate docs pass.

## Relevant commits

- The milestone is the single commit that introduced this file.
  `git log --diff-filter=A -- docs/milestones/003-information-transmission.md` resolves it.

---

## Correction — Codex review findings (appended after review)

The original commit above shipped five defects. Recorded here rather than edited into the account
above, per `AGENTS.md`: the milestone was not as clean as it was first written up, and the record
should say so. All five were accepted and fixed in a follow-up commit; the sections above remain
as originally written.

**1. The boss could *observe* who authorised a breach.** `ResolveViolence` offered the boss
`PersonBreachedPolicy` as part of an observation opportunity. That collapses the distinction the
whole milestone rests on: violence against a shop is observable, but who ordered it is a
conclusion about someone who may have been nowhere near the place, and a wrecked shopfront looks
identical whether the capo ordered it, tolerated it, or knew nothing. It also made the concealment
unfalsifiable in the other direction — there was nothing left for Vincent to hide. The boss now
observes only violence and witnesses, and reaches authorship by inference
(`Decision/Inference.cs`, a suspicion at `SourceKind.Inference` resting on violence he holds plus
a rule he knows) or by being told.

**2. The player view enumerated the authoritative character roster.** The "has not given him an
account" section listed organisation members straight out of world state, so it could name people
the viewpoint character had no way to know existed. Now assembled from his own head only —
whoever appears in a claim he holds, has given him an account, he has a relationship with, or he
holds a grievance against (`IntelligenceWriter.KnownPeople`).

**3. Provenance wording invented attendance.** Generic `SourceKind.Direct` was rendered as "he was
there when X did it" and "he saw it himself". Direct means *unmediated*, not *present*: it covers
a man who noticed something, a man who did it, and a man who had it first-hand from the person who
did. Vincent holds that he went outside his boss's rule because he decided to — nobody watched
that happen. Wording is now neutral pending more precise provenance.

**4. One source could corroborate itself.** Independence was decided by comparing the incoming
sender against the *record's* `SourceId`, which keeps its original value through revisions — so
the same man could confirm his own earlier report repeatedly and count as a fresh voice every
time, and a claim first acquired by observation names the observer, so a single sender never
matched at all. Independence is now decided against the whole testimony history, before the new
account is filed, and applies to repeated denials as well as repeated affirmations.

**5. Report eligibility saw only acquisition.** A character who had been contradicted since he
last spoke had, by the old test, nothing new to say — so he could sit on an account he knew was
disputed. Eligibility now keys off reconsideration. This is safe only because `Receive` was
changed in the same pass to leave a record completely untouched when nothing actually changed;
without that, "something has changed since I last spoke" would be permanently true and two
characters would report at each other until the run ended.

### Consequential change not in the findings

`IsContested` had to be reworked. It previously re-derived contestedness from the current stance
and only recognised a clash against a *directly observed* belief. Once the breach became an
inference rather than an observation, a denial of it stopped registering as a conflict at all —
and worse, a belief eroded until the character doubts it now *agrees* with the man who talked him
out of it, so re-deriving reported no conflict in exactly the case where the deception worked.
Contestedness is now recorded on the record at the moment of disagreement (`InformationRecord.Contested`).

### Verification

- `dotnet test` — 37/37 passing (was 17).
- `--verify --seed 42 --days 90` — DETERMINISTIC.
- All five findings have regression tests, and **each was mutation-checked**: the fix was reverted,
  the suite confirmed to fail, and the fix restored. Two gaps were exposed by doing this and would
  otherwise have shipped as false assurance:
  - the identity-leak test passed against the reverted code, because in the five-person cast
    Salvatore happens to know every organisation member, so the roster and his own knowledge
    produce identical names. A member nobody references had to be introduced to tell them apart —
    which is precisely why the original leak went unnoticed;
  - the finding-5 test only exercised the `Cognition` side and never the eligibility rule, which
    was private and therefore untested. It is now `Generators.HasSomethingToReport`, parameterised
    so the interesting cases can be staged directly.

### Still open after this pass

- Provenance remains imprecise: `SourceKind.Direct` cannot distinguish observing, doing, and being
  told first-hand by the doer. The wording is neutral rather than accurate, which is a workaround.
  A `Participant`/`Witness` distinction is the real fix.
- Everything under "Deferred work" above stands unchanged.

---

## Second correction — Codex verification findings on `cf22e5d`

Verification of the corrective commit found three more defects, two of them introduced *by* that
commit. Appended rather than folded in, for the same reason as above.

**6. "Personally witnessed" was emitted from confidence alone.** `ConfidenceLabel` returned it for
anything at or above 0.9, with no reference to how the claim was acquired — so the same provenance
invention that finding 3 removed from the attribution line was still arriving through the
confidence line beside it. Vincent holds that he went outside his boss's rule at full confidence
because he *decided* it, and the view told the player he had witnessed it. The top label is now
"beyond doubt for him"; confidence describes certainty, `SourceKind` is the only thing permitted to
speak about method. The doc's vocabulary still lists "personally witnessed" and it remains correct
to use — once provenance can actually establish witnessing.

**7. A retraction was unsayable, in three places at once.** Report eligibility filtered to held
beliefs, `Reporting.Compose` filtered to held beliefs, and `Compose` then hardcoded
`Stance.Believes` on everything it did include. A character who had been talked out of an account
he had already given his boss had, by these tests, nothing further to say — and had any one of the
three been fixed alone, the other two would still have swallowed it. All three now pass a position
through in the direction he actually holds it.

**8. Guarding against self-corroboration had also blocked recantation.** Finding 4's fix treated
any further account from a familiar sender as repetition, which is right for a man saying the same
thing twice and badly wrong for a man taking something back — it left a witness permanently unable
to change his story. The test is now same sender *and* same direction: repetition still compounds
nothing and does not touch the reconsideration stamp, while a reversal erodes the belief, marks it
contested, updates reconsideration, and so becomes reportable onward.

Findings 7 and 8 are the same mistake in two guises, and worth naming as one: each was a
correctness fix that quietly narrowed what could be *expressed*. Suppressing a bad case by
filtering the channel rather than by describing the case precisely will keep taking honest
behaviour with it.

### Verification

- `dotnet test` — 45/45 passing (was 37).
- `--verify --seed 42 --days 90` — DETERMINISTIC; `--compare` — 4 configurations, 4 distinct
  histories.
- All three mutation-checked. Finding 7 was reverted in both halves at once, confirming the
  end-to-end test catches what a per-stage test could not.
- Both viewpoint reruns re-read by hand.

---

## Third correction — Codex verification findings on `2a74a5d`

Three more, two of them P1. The pattern from the second correction repeated and should be read
alongside it.

**9. Runaway corroboration loop.** The disloyal run produced 323 decisions, 318 requests for an
account, and 5 replies. `SeekCorroboration` was suppressed only for people who had *already
replied*, so a request that went unanswered left no trace and the asker put the same question again
on every wake. The terminating condition cannot be the reply, because giving one is the other
man's decision and he is entitled to say nothing. Asking is now recorded as
`InformationRequest` on `World.Requests` at the moment the question is put — spent when asked, not
when answered. Disloyal is now 45 decisions.

This one is worth dwelling on: it was present in the original milestone commit, and both the
milestone write-up and the first correction claimed the exchange terminated. The claim rested on
reading the world log, where each *report* appears once, rather than on the decision count — which
`--compare` prints, and which I had in front of me and did not read.

**10. A retraction could still be dropped and then marked delivered.** Composition ordered held
positions first and capped output at three, so a character with three standing beliefs filled every
report with them and the retraction never reached the page — while the report's timestamp made
eligibility treat the matter as covered. Composition now leads with what is *news to this
recipient*, and eligibility is per claim (`Reporting.NeedsConveying`) rather than per report, so
anything squeezed out by the cap stays outstanding until it is actually said.

**11. Story changes were checked against all history rather than the sender's latest account.** In
an affirm → deny → affirm sequence the final affirmation matched the *first* one and was discarded
as repetition, so a source could be talked round and back and only the first two moves would
register. Same-direction changes of stance or confidence were also being ignored entirely.
`Receive` now compares against that sender's most recent account, and decides separately whether
the change should move confidence: a reversal does, firming up or softening the same position
updates reconsideration without compounding, and only a verbatim repeat changes nothing.

### Verification

- `dotnet test` — 57/57 passing (was 45).
- `--verify` DETERMINISTIC on baseline and disloyal; `--compare` — 4 distinct histories, 13/16/13/45
  decisions.
- All three mutation-checked.
- New coverage for the two gaps the review named: a **behavioural budget** test asserting no variant
  exceeds 100 decisions or one request per ordered pair, and **pause/resume** coverage over reports,
  requests and testimony for the disloyal path as well as baseline — the existing replay test only
  ever ran baseline, which is why a 323-decision runaway sat in a green suite.

### The recurring mistake

Findings 7, 8, 10 and 11 are one error in four costumes: a correctness fix that narrows what can be
*expressed* or *distinguished*, rather than describing the case precisely.

- 7 — filtered retractions out of the channel instead of representing a non-held position.
- 8 — blocked all repeat senders instead of distinguishing repetition from reversal.
- 10 — ordered by "held" instead of by "new to the recipient".
- 11 — matched against any past account instead of the latest one.

Each looked like a tightening. Each removed a legitimate behaviour along with the bad one. The
question to ask of the next fix of this shape is not "does this stop the bad case" but "what
honest case does this also stop".

### Still open after this pass

- Provenance imprecision (see the previous correction) stands unchanged.
- `docs/HANDOFF.md` does not exist and will not. Matt's call, and correct: `CURRENT_MILESTONE.md`
  already is the handoff surface — status, scope in and out, planning decisions, carried-over
  items — and a second copy would only drift out of sync with it. What the complaint did expose is
  that `AGENTS.md`'s canonical read list never mentioned `docs/milestones/`, so a reviewer
  verifying a corrective commit had nothing pointing them at the correction notes or the still-open
  items. Both entries in that list are now explicit.
