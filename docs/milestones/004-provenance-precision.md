# Milestone 004 — Provenance Precision

Status: **twice corrected; the second correction is awaiting review. Not verified or accepted.**

The sequence, in order:

1. `714fbc3` — the implementation. Reviewed and **rejected**, three P1.
2. `c828bfa` — attempted the correction. Reviewed and **rejected**, three P1 and two P2.
3. This correction — fixes those five. **Awaiting review.**

Both corrections are appended at the foot of this file. Nothing between here and there has been
rewritten: the account below is what the milestone originally claimed, including the parts later
findings contradict.

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
