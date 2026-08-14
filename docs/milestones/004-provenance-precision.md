# Milestone 004 — Provenance Precision

Status: **complete, awaiting Codex review.**

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
