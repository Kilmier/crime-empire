# Milestone 004 — Provenance Precision

Status: **closed.** Codex reviewed `1fe8a15` with no findings and Matt accepted it on 2026-08-14.

The sequence, in order:

1. `714fbc3` — the implementation. Reviewed and **rejected**, three P1.
2. `c828bfa` — the first correction. Reviewed and **rejected**, three P1 and two P2.
3. `d783745` — the second correction. Reviewed and **rejected**, two findings.
4. `612bd50` — the third correction. Reviewed and **rejected**, two findings.
5. `1fe8a15` — the fourth correction. Reviewed with **no findings**. **Accepted.**

All four corrections are appended at the foot of this file, in order, along with a closing note.
Nothing between here and there has been rewritten: the account below is what the milestone
originally claimed, including the parts later findings contradict.

Milestone 003, which this was built on top of, has since closed.

This milestone was begun on the strength of a milestone-003 verification that had not happened. The
account below stands on its own measurements, which were real; what it lacked was anyone checking
the work, and that check has now come back negative.

## What was attempted

Replace the broad `SourceKind.Direct`, which conflated four different ways of coming to hold a
claim, with the smallest vocabulary that keeps them apart — and answer, explicitly, every rule that
had been keying off the broad category.

Matt's rulings at planning time:

- **Provenance may change behaviour.** Testimony behaves as testimony; a claim acquired by being
  told is not protected the way one acquired by seeing or doing is. Baseline movement is acceptable
  where it traces to a corrected acquisition category, and nowhere else.
- **Authored participation is separate from being told that execution occurred.** Vincent ordering
  a beating and Vincent hearing from Tommy that the beating happened are two acquisitions, not one.

## What was completed

**The vocabulary** (`Domain/Claim.cs`). `Direct` is gone, with no umbrella replacing it — an
umbrella is how the conflation returns. Four categories replace it:

- `Participant` — he did it, ordered it, or it is his own act. The only category that justifies
  knowing hidden authorship, because the author is him.
- `Witness` — he was there and saw it. Carries what was done, never who authorised it.
- `Discovery` — he came upon a trace or consequence afterwards. Explicitly implies he was *not*
  present.
- `FirstHandTestimony` — someone who was in it told him directly. Still testimony.

`Report`, `Rumor` and `Inference` are unchanged. `Rumor` remains dormant; no path produces it.

**The predicates** (`Domain/Provenance.cs`, new). `Direct` was not only a label — four rules
compared against it. Splitting it into four members would have meant four separate lists of enum
members free to drift apart, so the shared properties are named once:

- `IsUnmediated()` — established without anyone's account. Behind the `Learn` override rule, the
  0.15-vs-0.45 erosion under contradiction, and the stance protection below the acting threshold.
- `IsTestimony()` — somebody told him. Behind "worth seeking a second account of".
- `Label()` — short form for developer traces.

`Inference` is deliberately in neither group: a conclusion is not something he was told, and not
something he established by observation either.

The `IsTestimony()` rewiring of `Generators.FromRelationship` and `Utility` is not cosmetic. Those
sites tested `== SourceKind.Report`; without the change, reclassifying a report as first-hand
testimony would have silently made it uncorroboratable.

**The acquisition audit.** Every `Learn` call site reassigned from what actually occurred:

| Site | Now |
|---|---|
| `Cast.cs` Salvatore holds his own policy | `Participant` |
| `Strategies.cs` executor sizes up the shop | `Discovery` |
| `Strategies.cs` delegator gets that read from the executor | `FirstHandTestimony` (was `Report`) |
| `Strategies.cs` grocer holds his own shop is short-paying | `Participant` |
| `Strategies.cs` executor collected / came to terms | `Participant` |
| `Strategies.cs` after collection | executor `Participant`, delegator `Discovery` (was one loop) |
| `Strategies.cs` executor used violence | `Participant` |
| `Strategies.cs` executor believes people saw him | `Inference` (was unmediated) |
| `Strategies.cs` grocer, beaten | `Witness` |
| `Strategies.cs` delegator told by executor | `FirstHandTestimony` |
| `Strategies.cs` delegator authored the breach | `Participant` |
| `Strategies.cs` stale lead after a dead trail | `Inference` |
| `Runner.cs` observation opportunity | `Discovery` |

Two are worth calling out. The executor's belief that *people saw him* was previously recorded as
something he established; it is a guess — he did not turn round and check the street — and filing it
as observation made a fear that should be defeasible in both directions unshakeable. And
`Runner.cs`'s observation opportunity is the load-bearing one: it rolls against trace
discoverability a day later, so recording it as witnessing had the simulation assert a character's
whereabouts on the strength of a discovery roll.

**Rendering** (`IntelligenceWriter.cs`). `Attribute` gets one arm per category and the two long
comments explaining why it could not say "he saw it" or "he was there" are gone, because the record
now supports what the sentence says: "he had a hand in it himself", "he saw it himself", "he came
across it", "X was in it and told him so", "X told him", "he worked it out himself". The
conflicting-accounts block tested `!= Report` to mean "his own account", which would have filed
somebody else's first-hand testimony as his own; it now tests `IsUnmediated() || Inference` and
names the basis specifically. `ConfidenceLabel` stays certainty-only — its comment used to say
"personally witnessed" becomes sayable once provenance can establish witnessing, and it now can, but
it belongs in the provenance sentence rather than letting a number assert a method.

## Tests / success criteria and results

`dotnet test` — **86 passing** (was 73). Full contract passes: both `--verify` runs deterministic,
`--compare` reports four configurations and four distinct histories.

New `ProvenanceTests.cs`:

- the author holds his own order as `Participant`, and the man it was done to holds the violence as
  `Witness` while learning nothing about who authorised it;
- nothing acquired after the event is recorded as witnessing, across two variants, with a positive
  assertion that the discovery path really does produce `Discovery` so the negative is not vacuous;
- first-hand testimony erodes further under contradiction than observation does and loses its
  stance, where observation keeps it;
- certainty is never rendered as sight — no confidence label mentions witnessing, and no character
  who witnessed nothing has "he saw it himself" in his view;
- reporting neither upgrades nor downgrades acquisition: a fresh report is a `Report` however
  certain the sender sounded, and being told something he already had first-hand leaves it his own;
- **structural invariant**: every `IsUnmediated()` record is self-sourced, across all four variants;
- provenance survives pausing and resuming.

**Three mutation checks**, each caught by the intended test: recording the beaten grocer as
`Participant` fails the authorship test; treating `FirstHandTestimony` as unmediated fails both the
testimony test and the self-sourced invariant in three variants; recording the discovery roll as
`Witness` fails the after-the-event test.

One test caught a wrong assumption while being written. It originally asserted Salvatore holds the
violence as `Discovery`; in `baseline` he is *told* by Vincent before any discovery roll reaches
him, so his record is correctly a `Report`. The assertion was rewritten as the negative guarantee —
nothing acquired after the event is `Witness` — which is what the milestone actually promises.

## Important discoveries

**The predicted behaviour change did not happen, and that is the finding.** The plan expected
`disloyal-vincent` to move, because the delegator's knowledge of the beating became
`FirstHandTestimony` and so erodes three times faster under contradiction. It did not move. Diffing
the full decision and event stream against a stashed pre-change build, with provenance labels
normalised, shows **no chosen action changed in either variant** — only four score magnitudes, each
by about 0.05:

```
baseline          ask tommy for his own account            0.68 -> 0.73
baseline          give salvatore nothing on his own part   1.49 -> 1.54
disloyal-vincent  report to vincent, leaving out his part  1.61 -> 1.66  (x2 occurrences)
disloyal-vincent  report to salvatore, leaving out part    1.11 -> 1.15
```

Both deltas trace to named reassignments: the corroboration score now scans `IsTestimony()` rather
than `Report` alone, so a first-hand account is included in "how shaky is what I have"; and the
executor's witness-belief moved to `Inference`, which changes its suspicion discount and feeds the
candour decision. Neither was enough to flip a choice.

The reason the erosion change is invisible is worth recording: it only fires when somebody
contradicts that specific claim, and in no current variant does anyone contradict Vincent's
knowledge that the beating happened. **The scenario does not yet exercise the distinction the
milestone exists to draw.** The categories are correct and the rules read them correctly, but the
harbour scenario cannot currently demonstrate the difference in play. A variant where the executor
denies to the delegator — Tommy telling Vincent he never touched the place — would exercise it, and
is the obvious next scenario addition.

**Counts are therefore unchanged**: 13 / 16 / 13 / 45 decisions and 2 / 2 / 2 / 7 reports, exactly
as at `e83dacf`. Replay hashes move because the developer trace and the player-facing wording both
changed, not because the simulation did.

## Deferred work

- **Possible pre-existing runaway in `disloyal-vincent`.** The developer trace shows
  `began ConcealIncident(...)` chosen roughly fifteen times in that variant, restarting rather than
  continuing. It is present identically before and after this change, so it is not a regression
  here and was left alone under this milestone's scope rules — but it looks like the same class as
  the corroboration runaway fixed in `f97ef76`, and it is worth its own look. Note the empty domain
  in the label, `ConcealIncident(, target=...)`, which may be related.
- A scenario variant that contradicts a delegator's first-hand testimony, so the erosion
  distinction becomes observable in play rather than only in unit tests.
- The `FirstHandTestimony` suspicion discount is `0.15`, sitting between unmediated and `Report` on
  the reasoning that a participant's own account is harder to wave away than a filed report. It is
  a tuning guess and is recorded as one; nothing yet distinguishes it from `0.20` behaviourally.
- `Rumor` remains dormant.

## Relevant commits

- `714fbc3` — Split `Direct` into four acquisition categories. The implementation commit: vocabulary,
  predicates, acquisition sites, rendering, and tests.

## Correction — Codex findings on `714fbc3`

Status: **awaiting Codex review. Not verified.**

Three P1 findings, all accepted. Nothing above is deleted; the account of what the milestone
originally did stands, including the parts these findings contradict.

**1 (P1). Synthetic first-hand testimony.** `Strategies.cs` wrote `FirstHandTestimony` straight into
the delegator's cognition at the moment of the beating, sourced to the executor, with no report, no
message, no meeting and no trace behind it. The same thing happened for the executor's read of the
target. Ordering something is not a channel: authority does not deliver knowledge, and the
player-facing account said "Tommy told him" while Tommy had not yet opened his mouth.

Both inserts are gone. A participant still knows what he did — Vincent holds that he *gave the
order*, which is his own act — but that it was *carried out* is a separate fact, and it now reaches
him the way anything else does. The delegator is also no longer excluded from the discovery roll:
he was excluded only because his knowledge was being written in for free, and without that he had
no route to the thing he ordered at all.

**2 (P1). Provenance lost in transmission.** `ReportedClaim` carried no acquisition basis and
`Cognition.Receive` stamped every incoming claim `Report`, so first-hand testimony could not be
acquired honestly — which is precisely why it was being fabricated in finding 1. `ReportedClaim`
and `Testimony` now carry `SpeakerBasis`, and `Provenance.AsHeardFrom` decides what the listener
records: a participant or witness giving their own account arrives as `FirstHandTestimony`;
everything else — discovery, inference, an ordinary report, and *relayed* first-hand testimony —
arrives as `Report`. Repeating testimony makes it hearsay, and the chain cannot launder itself back
into first-hand at each hop. The speaker's own basis is kept verbatim on the testimony entry, so the
coarser settled belief loses nothing.

Two further routes were found by the new tests rather than by reading. `Runner.DeliverAssignment`
and `Commit`'s delegation branch both used `Learn` to put claims in a subordinate's head sourced to
his superior, leaving nothing on record of anyone having spoken. Briefing a man is telling him, so
both now go through `Receive` and leave testimony he can attribute and contest.

**3 (P1). The bundled predicate.** `IsUnmediated` covered Participant, Witness and Discovery, and
four separate rules keyed off it: override immunity, erosion resistance, stance protection, and the
Suspicious exemption. A category could not be given one property without inheriting the other
three, and Discovery inherited all four when it should have had none. Finding a wrecked shopfront
the next morning is an interpretation of a trace — it can be weak, wrong about who, and worth
arguing about.

Replaced with `OverridesPriorRecord`, `ResistsContradiction`, `ProtectsStance` and
`ExemptFromSuspicion`, plus a purely descriptive `IsSelfAcquired` wired to no rule at all. Their
memberships currently coincide at Participant and Witness, which is not the point: they are separate
questions with separate answers, so changing one can no longer quietly change the others. Discovery
now takes a `0.10` suspicion discount — a tuning guess, recorded as one, sitting below every
discount that applies to somebody else's word.

### Tests

Twenty-one added, **120 total**, in `KnowledgeRoutingTests.cs`. All three fixes mutation-checked by
restoring the defect: reinstating the synthetic insert fails six tests across four variants;
collapsing `AsHeardFrom` to `Report` fails the transmission and hearsay tests; re-bundling Discovery
into the four predicates fails the displacement-and-contest test.

Two existing tests needed repair, and both are recorded rather than quietly adjusted:

- `A_retraction_outranks_positions_the_recipient_already_has` built its retraction as a weak
  `Discovery`, which no longer displaces a firmer earlier record — finding 3 working as intended. It
  now uses `Participant`, which is what collecting the money actually is.
- `The_same_person_can_be_asked_about_more_than_one_matter` read the finished run for two questions
  to one person. With less information circulating, that stopped happening and the assertion went
  vacuous without the rule it guards having changed. It now drives `Pipeline.Deliberate` directly,
  so it proves the wiring rather than the scenario's luck, and a companion test asserts across the
  running simulation that no question is ever put twice.

### Behavioural movement

| | before | after |
|---|---|---|
| decisions | 13 / 16 / 13 / 47 | 13 / 16 / 13 / **19** |
| reports | 2 / 2 / 2 / 7 | 2 / 2 / 2 / **2** |
| `ConcealIncident` restarts, disloyal | 12 | **0** |

All four hashes move; all four runs remain deterministic and distinct. The movement traces to one
cause: the delegator no longer knows the beating happened, so he cannot report it upward, and the
information that used to circulate for free now has to travel or not arrive. In three of the four
variants the boss's discovery roll does not land and he never learns of the violence at all.

The concealment runaway flagged in the deferred list above — Tommy restarting `ConcealIncident`
twelve times — no longer occurs. That was not the object of this correction and no code was written
against it; it stopped because the beliefs feeding the legal-exposure pressure changed. It should be
treated as unexplained rather than fixed, and the deferred item stays until somebody understands why.

### What the player sees now

Vincent's own view used to say Tommy had told him about the beating. It now says
`Tommy Nardo has not given him an account`, and the policy he holds reads
`Salvatore Greco was in it and told him so` — first-hand testimony from the man who set the rule,
acquired through an assignment briefing that now leaves a record of having happened. On
`disloyal-vincent`, Salvatore still reaches the breach by finding the traces himself and reasoning
from them, with Vincent's denial listed beside his own conclusion.

## Second correction — Codex findings on `c828bfa`

Status: **awaiting Codex review. Not verified.** Nothing above is rewritten.

`c828bfa` attempted the correction to `714fbc3` and was itself rejected: three P1 and two P2.

**1 (P1). Claimed provenance was not separated from private provenance.** `Compose` transmitted the
sender's real `SourceKind` on every claim including a false denial, and `Receive` treated it as
information the listener had. So Vincent denied the beating and shipped `Participant` with the
denial: the listener filed first-hand testimony and the concealment announced the participation it
was concealing, through a field nobody was reading.

`ReportedClaim` now carries `ClaimedBasis` and `ActualBasis`. Claimed is what the account presents
itself as and the only thing the recipient may act on; actual is developer truth and lives in the
report log. `Testimony` — which sits in the *listener's* head — keeps only the claimed basis, since
anything stored there is something he knows. An honest account claims what it has; a denial
discloses a bare assertion. A liar who wanted to say "I was there and it did not happen" would have
to claim that basis deliberately, and none does yet.

**2 (P1). Provenance was decided only for new claims.** `AsHeardFrom` ran in the `prior is null`
branch alone, so a man who already held a generic report and was then told by the participant
himself kept the report's classification and the report-teller's name against it for good. The
verbatim-repeat test also ignored a changed basis, so a witness stepping forward was filed as
somebody clearing his throat.

`Upgrade` now runs on every path that keeps the belief's direction, moving both provenance and
attributed source when the incoming account genuinely outranks the current one. Ranking covers only
kinds a listener can hold — first-hand testimony over report over rumour — and returns nothing for
records he established himself, so being told what you already saw never re-attributes it to the
teller. Equal-ranked accounts leave attribution alone: two reports are two reports, and the first
man to say it keeps his name against it. The repeat comparison now includes the claimed basis.

**3 (P2). Assignment disclosures were read late.** `DeliverAssignment` looked up the issuer's mutable
cognition six hours after issuance, so a change of mind in the gap rewrote what had already been
said. `Assignment.Disclosed` is now `IReadOnlyList<ReportedClaim>`, fixed at issuance with stance,
confidence and claimed basis, and delivery transmits that snapshot.

**4 (P2). Snapshots did not cover the new fields.** Report lines in the replay snapshot and the
channel comparison now carry claimed and actual basis; testimony lines carry the claimed basis. A
run that classified beliefs differently can no longer compare equal.

### Tests

Thirteen added, **133 total**, in `DisclosedProvenanceTests.cs`. The two specifically required
mutation checks both fail as intended: restoring the private-basis transmission fails the denial
test, and disabling `Upgrade` fails both the attribution test and the changed-basis repeat test.

### Behavioural movement

**None.** All four replay hashes are byte-identical to `c828bfa` —
`B20C06E5838C0657` / `24A181B260F9C396` / `4B60DA962927A6F7` / `B274F395A61C5118` — with decisions
still 13 / 16 / 13 / 19, both `--verify` runs deterministic and pause/resume equivalent.

That deserves stating plainly rather than being presented as reassurance. The leak was real and
reachable through the API — the mutation check proves it — but it did not fire in any of the four
variants, because the one false denial that occurs reaches a recipient who already holds the claim
and therefore takes the contradiction path, which never rewrote provenance. The fix is correct and
currently invisible in play. The same is true of the assignment snapshot: no issuer presently
changes his mind in the six-hour gap.

### The `ConcealIncident` runaway — explained, not fixed

The previous account recorded the loop's disappearance as unexplained. It is now explained, and it
is **not** resolved.

Observation rolls are keyed from global event IDs (`Rng.ForDecision(seed, observerId, 5000 + ev.Id)`).
Removing the synthetic information events shifted every later event ID, which shifted Tommy's
observation seeds, and his police-interest rolls now all miss. He never comes to believe he is being
looked at, so the legal-exposure pressure that drove the concealment loop never rises.

The loop is therefore latent, not gone: it will return whenever those rolls land again, and the
underlying defect is that a per-character RNG stream is keyed off a global counter, so unrelated
changes anywhere in the simulation silently re-roll everybody's perception. That is a real
determinism-hygiene problem and it stays on the deferred list. It was deliberately not addressed
here — this is a provenance correction, and turning it into an RNG redesign is how a corrective pass
becomes a milestone.

## Third correction — Codex findings on `d783745`

Status: **awaiting Codex review. Not verified.** Nothing above is rewritten.

Two findings.

**1. The `ActualBasis = Report` default was a migration hazard.** Both bases were positional
parameters with defaults, so supplying only the claimed one — the natural thing to write — left the
actual one silently at `Report`. `Commit`'s delegation branch did exactly that, which meant a capo
briefing his own man about something he had done himself came out flagged as misrepresenting how he
knew it. A field whose wrong value is what you get by not thinking about it is a trap.

Neither basis is a constructor parameter now. `ReportedClaim` takes claim, stance and confidence;
the two bases are init properties set through `Honest(...)` or `Misrepresenting(...)`. Every
production construction is explicit: delegation, both assignment disclosures and the candid report
path use `Honest`; only the denial uses `Misrepresenting`, claiming a bare report while retaining
the participant basis as developer truth.

**2. The verbatim-repeat comparison collapsed Participant onto Witness.** It projected each claimed
basis through `AsHeardFrom` before comparing, and that maps both onto `FirstHandTestimony` — so a
man who first said he did it and then said he only watched it counted as having repeated himself.
Those accounts differ in whether he is confessing. The comparison now tests the claimed basis
itself.

The opposite error is guarded too: a changed basis makes it a new account, not a new voice. It lands
on the "firming up or softening a position he already gave" path, so the belief is marked
reconsidered and its confidence does not move.

### Tests

Five added, **138 total**. Both regressions the review asked for:

- an honest briefing is never marked misrepresented — across four variants, the only misrepresented
  claims in any run are denials, each a `Rejects` on a withheld claim in a `False` report; and
  `Honest(...)` is exercised for every member of `SourceKind`;
- `Participant` → `Witness` from one man is a new account that updates reconsideration without
  moving confidence, both accounts survive in the log distinguishable by basis, and repeating the
  second one is a genuine repeat that moves nothing.

Finding 2's fix is mutation-checked in the usual way: restoring the `AsHeardFrom` projection fails
the Participant→Witness test.

**Finding 1's fix cannot be mutation-checked at runtime, and that is worth stating rather than
glossing.** Delegation's `ReportedClaim` is passed straight to `Receive` and never persisted, so a
wrong `ActualBasis` there is invisible to every observable surface — reinstating the defect leaves
all 138 tests passing. The protection is instead at the type level, and that *was* verified: a probe
file using the old positional form fails to compile with CS1729. The runtime tests cover the
factory contract and the invariant that only denials misrepresent; the compiler covers the hazard
itself.

### Behavioural movement

**None.** All four replay hashes remain `B20C06E5838C0657` / `24A181B260F9C396` /
`4B60DA962927A6F7` / `B274F395A61C5118`, decisions 13 / 16 / 13 / 19, both `--verify` runs
deterministic. Neither defect had a live consequence in the current scenario: delegation's beliefs
are never re-read after transmission, and no character currently offers two accounts of the same
claim on different bases.

## Fourth correction — Codex findings on `612bd50`

Status: **awaiting Codex review. Not verified.** Nothing above is rewritten.

Two findings.

**1. Provenance was still settable by halves.** Removing the two bases as constructor parameters
closed the "supply one, silently default the other" hole. Leaving them as public `init` left the
same hole one step along: an object initializer could set one alone, and a `with` expression could
do worse — start from a correct pair and desynchronise it in passing.

Both accessors are now `private init`. Outside the type the only ways in are the three-argument
constructor, which yields a generic report honestly claiming to be one, and the two factories. A
probe using either route now fails to compile with CS0272, and a reflection test asserts the setters
are not public so the accessor cannot be quietly widened back.

**2. Live documentation described a review automation that does not exist.** `CURRENT_MILESTONE.md`
and this brief's sibling both described a Codex monitor keeping a checkpoint, waking on a timer and
advancing coverage by itself. There is no such thing: review is manual, ordered, and happens only
when Matt points one at a commit. Both now say so.

That correction matters more than a wording fix. A reader who believes coverage is automatic stops
asking whether a review happened — which is exactly the failure that produced two false
"verified" claims earlier in this milestone's history. The historical sections describing what went
wrong at the time are left as they were; only the live process description changed.

Stale "twice / second correction" language is brought up to date in the same pass. The count is now
four, because this correction is itself the fourth.

### Tests

One added, **139 total**: neither basis is publicly settable, and the three-argument form still
yields an honest generic report. The compile-level guarantee was verified separately with a probe
file exercising both the object-initializer and `with` routes; both are rejected with CS0272.

Note what changed since the last correction. `d783745`'s hazard could not be mutation-checked at
runtime at all — the wrong value was invisible to every observable surface. This one can be, because
the guarantee is now a property of the type rather than a convention about how to call it.

### Behavioural movement

**None.** All four replay hashes unchanged at `B20C06E5838C0657` / `24A181B260F9C396` /
`4B60DA962927A6F7` / `B274F395A61C5118`, decisions 13 / 16 / 13 / 19, both `--verify` runs
deterministic. Nothing in the simulation ever used the routes that were closed.

## Closed

Codex reviewed `1fe8a15` with no findings. Matt accepted it on 2026-08-14. **Milestone 004 is
closed.**

### What it delivered

`SourceKind.Direct` is gone, replaced by `Participant`, `Witness`, `Discovery` and
`FirstHandTestimony`, with no umbrella value. Each of the four behaviours the old bundle controlled
has its own named predicate, so a category cannot inherit a privilege nobody meant to give it.
Knowledge travels: nothing arrives by rank, by shared employer, or by having ordered it. What a
speaker claims is separated from what he privately has, so a denial no longer discloses the
participation it denies. Provenance survives transmission, and repeating testimony makes it hearsay.

### Accepted state

- Build clean, **139/139 tests**.
- Replay hashes `B20C06E5838C0657` / `24A181B260F9C396` / `4B60DA962927A6F7` / `B274F395A61C5118`,
  deterministic across repeated runs, four variants producing four distinct histories.
- Decision counts 13 / 16 / 13 / 19.

### Four corrective rounds, and what they have in common

| Round | Against | Outcome |
|---|---|---|
| Implementation | `714fbc3` | Three P1 |
| First | `c828bfa` | Three P1, two P2 |
| Second | `d783745` | Two findings |
| Third | `612bd50` | Two findings |
| Fourth | `1fe8a15` | No findings. Accepted. |

Every round but the last found the same *kind* of defect: a distinction drawn in one place and not
carried through to another. Authority implied knowledge because ordering something was treated as a
channel. Provenance was decided when a claim was new and never revisited. A speaker's private basis
travelled with his lie. Two fields that had to move together could still be moved apart. Milestone
003's lesson was "a correctness fix can stop halfway along the path a value travels"; this milestone
demonstrated it four times, and the reason is worth naming — each fix was correct where it was
written and incomplete everywhere the value went next.

The habit that eventually caught them was asking, of every new distinction: *where else does this
value get read, and does the distinction survive the trip?*

### Carried forward

- **No scenario variant contradicts a delegator's first-hand account**, so the distinction between
  authored participation and being told is provable in unit tests and invisible in play. A variant
  where Tommy denies to Vincent that he touched the place would exercise it.
- **The `ConcealIncident` runaway is latent, not fixed.** Observation rolls are keyed from global
  event IDs, so removing the synthetic information events shifted Tommy's seeds and his
  police-interest rolls now all miss. It returns whenever they land again. The underlying defect is
  the keying: a per-character RNG stream keyed off a global counter means an unrelated change
  anywhere silently re-rolls everybody's perception.
- The `FirstHandTestimony` suspicion discount of `0.15` and the `Discovery` discount of `0.10` are
  tuning guesses, not derived figures.

### Relevant commits

- `714fbc3` — implementation. `c828bfa`, `d783745`, `612bd50`, `1fe8a15` — the four corrections.
  `11c4a4a` and the interleaved documentation commits carry the record.
