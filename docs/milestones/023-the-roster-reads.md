# Milestone 023 — The Roster Reads

First milestone of the demo arc's layer 1 (`ROADMAP.md`, "The demo arc"). Authorized by Matt on
2026-09-05.

## What this milestone was for

Twenty milestones of relationship machinery, and the interface showed the player five adjectives and
nothing about how any of it got that way.

Grudges were the only durable per-relationship history, and they run one direction only. Trust has
moved at runtime since milestone 006 and in both directions since 016; fear has moved since the first
coercion resolution; **neither left any trace of why**. Milestone 018 added `"his trust in Vincent
Russo cooled"`, but as a transient item in a recent-events feed: it scrolls past, says nothing about
the cause, and is not attached to the man it concerns.

So a relationship that cooled because somebody contradicted him to his face was indistinguishable, in
the interface, from one that had never been warm.

## The ruling, and what it reverses

**Matt, 2026-09-04, restated in authorizing this milestone: the interface surfaces why standing
moved.** Recorded as a reversal rather than quietly worked around, because
`PlayerNarration.Standing`'s own doc comment argued the opposite:

> "A relationship that cooled because an account did not match reads identically to one that was never
> warm, which is correct — the difference is a matter of history the player has to reconstruct from
> the accounts, not a label the interface hands over."

That is defensible for a developer reading a claim log and wrong for somebody playing a game. What the
comment was protecting still holds and is untouched: the standing *phrase* still leaks neither the
cause nor a number.

## What was completed

- **`Domain/SocialState.cs`** — `StandingCause` (one member per runtime mutator that moves a
  dimension) and `StandingChange(Cause, At)`.
- **`Domain/Relations.cs`** — `IRelationship.StandingHistory`, and a `Remember` write at each of the
  three runtime movement sites, alongside the movement itself.
- **`Session/PlayerNarration.cs`** — `WhyStandingMoved` and `Warmed`, rendering the typed cause.
- **`Session/PlayerSnapshot.cs`** — `PlayerStandingMoment` and `PlayerAttitude.History`.
- **`CrimeEmpire.Godot/Game.cs`** — the roster column prints each remembered reason, dated, with an
  arrow for direction.

Result, from the corroboration self-test's real rendered panel:

```
HOW HE TAKES THEM
Vincent Russo
    he has his reservations about him
    4 Apr  ↓ Vincent Russo told him the opposite of what he had
```

## Three decisions worth their reasoning

**History, not a fifth dimension.** A durable positive counterpart to `Grievance` that *fed* loyalty
would be a new relationship dimension, and `RELATIONSHIPS.md`'s rule for admitting one — it must name a
decision that reads it — is deliberately hard to meet. Nothing scores this. It is the history of the
four dimensions that already exist, which is why it moves no hash.

**Typed cause, not a written description.** The first draft had `Relations` compose the sentence. That
was wrong for a settled reason: `DESIGN_DECISIONS.md` requires that no simulation-authored string
crosses the player boundary, which is why `PlayerOption` builds its wording from typed fields. At the
`Relations` layer there are only ids and claims in scope, so a sentence written there would either
leak `bellini-grocery` into a line a player reads or force the domain to know about names. Caught
while writing it, not by a test.

**No stored direction.** Which way each cause moves things is fixed — a contradiction always costs
trust, a corroboration always adds it, being frightened always adds fear — so storing direction
alongside cause would be one fact in two places, free to disagree. The reader derives it.

## What it deliberately does not say

The remembered reason says *what happened*, never *what was true*. "He told him the opposite of what
he had" is a fact about the exchange; "he lied to you" is a fact about the speaker that the listener
has no access to. `RecordAccountConflict` structurally cannot reach the truth log or the speaker's
candour — that is enforced by `AccountConflict`'s own shape, assembled entirely from the listener's
side — so the memory inherits the same limit rather than restating it.

## Findings

**Matt's own example still does not occur, and this milestone does not make it occur.** He asked for
*"Don's opinion of Vincent is up because he completed a heist for him successfully."* Nothing raises
trust when a man completes work he was given: trust moves only through account conflicts and
corroborations. `Suitability.RecordDelegatedOutcome` fires at exactly that moment and moves a
*capability belief* instead. The history populates from real movements — the corroboration self-test
above is one — but never with that entry. Adding it is small in code and genuinely interlocking, since
trust feeds `Utility.Loyalty`, so it is a behaviour change and Matt's call rather than something to
fold in quietly.

**The roster and the RECENTLY feed now say the same thing twice**, and the history line is strictly
more informative — it gives the cause, which the feed does not. Milestone 018's
`RecentTrustMovements` is a candidate for removal or narrowing. Recorded for milestone 025, the
interface pass, rather than acted on here.

**The runner's viewpoint render does not show it.** `IntelligenceWriter` is a separate developer-facing
surface from the Godot panel and was left alone. Not a defect — but a reader checking this milestone
from `--viewpoint` output will not see it, and should look at the Godot self-test instead.

## Verification

- Build 0 warnings / 0 errors; tests **599 passed, 0 failed** (593 + 6, `RosterHistoryTests.cs`).
- **All six variants byte-identical on trace and chosen-action digest**, which was the milestone's own
  stated constraint: nothing here is scored, so any movement would have meant something leaked into
  the simulation.
- Both replay comparators cover `StandingHistory`; all five Godot self-tests and the two-process
  restart proof exit 0; both viewpoint runs exit 0.
- **Mutation check:** removing the `Remember` write from `RecordAccountConflict` fails
  `A_contradiction_costs_trust_and_records_why` and
  `A_movement_is_remembered_by_the_man_it_moved_and_nobody_else`, and nothing else — confirming the
  entries are written at the movement rather than reconstructed elsewhere. Reverted.

## Where to look and what to distrust

Unreviewed, like everything since `34cd117`. The claim most worth checking is that the end-to-end test
reads the *projection* rather than the domain: `The_roster_carries_the_reason_after_a_natural_run`
asserts on `PlayerView.Build`, because a test that read `Social` directly would pass with the panel
entirely unwired — the false-assurance shape this project's ledger names as recurring, and the exact
mistake three of milestone 020's tests made.

## Commit

One implementation-and-archive commit.

## Follow-on, same day — what he takes a man for

Matt asked on 2026-09-05 whether trust should rise when a man completes work he was given, so that
his original example — *"Don's opinion of Vincent is up because he completed a heist"* — would occur.

**It should not, and the example already occurs elsewhere.** In this model trust means *would I take
his word*: every band says so, and it moves on account conflicts and corroborations. It is reliability
**as a source of information**. Whether a man is any good at leaning on a shopkeeper is a different
question, and milestone 021 built it deliberately as a separate thing — a belief that can be wrong,
with a source and a confidence, rather than an attitude. Folding job outcomes into trust would
collapse exactly the distinction 021 existed to draw, and would make a good enforcer into a man whose
word you would take, which does not follow.

`Suitability.RecordDelegatedOutcome` has moved the capability belief on job completion since 021.
**What was missing was that the roster never showed it** — the column carried attitudes only, so what
the player thought a man was *good for* never appeared beside his name.

Added: `PlayerAttitude.TakenFor`, read from the viewpoint character's own `PersonIsCapable` beliefs on
the `CapabilityBar` ladder, rendered by `PlayerNarration.TakenFor`, and shown on both the Godot roster
and `IntelligenceWriter`. Null on both bars omits the line — having no view and having a poor view are
different states, and "he has no opinion of whether Vincent is any good" is a sentence about the model.
The attitude filter was widened so a man he has an opinion of the usefulness of is not dropped before
rendering.

The roster now reads:

```
Angelo Conti
   he would take his word
   he takes him for a hard man
   7 Apr  up — Angelo Conti backed him up: somebody on the street saw Angelo Conti at Bellini's grocery
   13 Apr  up — Angelo Conti backed him up: Bellini's grocery would not stand up to pressure
```

### Three defects this surfaced, two of them in this milestone's own work

**The history could not tell its own entries apart.** The first version stored the cause alone, and
the rendered roster showed three identical lines on one day — three genuinely different corroborations,
which milestone 016's freshness rule both permits and requires, rendered as one sentence repeated. A
history that repeats itself reads as a bug even when the state behind it is right. `StandingChange`
gained `About`, the claim the exchange was over. **Found by reading the output, not by a test** — which
is the argument for the arc's rule that a milestone must be visible in play.

**`Claim.ToString()` went into `BehavioralSnapshot` and embedded a `WorldEvent.Id`.** That comparator's
own doc comment names this exact mistake: it exists to exclude "every free-text field… that can embed
one indirectly through `Claim.ToString()` printing a nonzero EventId". Four insertion-stability tests
caught it immediately. The behavioural comparator now prints kind/subject/object, matching the
`AttemptedConcealments` precedent beside it; the full snapshot keeps the event id, where it belongs.

**A word collision in player text.** A tightened phrasing used "contradicted", which is a *confidence
label* that replaces the usual one on a belief still held —
`InformationTransmissionTests.A_contradicting_source_leaves_a_conflict_that_is_still_attributable`
pins its appearance to exactly that case, as a biconditional. Reusing the word on a roster line broke
it. The wording changed rather than the test: it was asserting something true.

### Also corrected here (`4da1e66`)

`PersonIsCapable` had been added to the claim vocabulary by milestone 021 and never added to
`PlayerNarration.Describe`, so it reached players as a raw `PersonIsCapable(angelo -> hard-man)` dump.
And this milestone listed rewriting `PlayerNarration.Standing`'s doc comment in its own scope and then
did not do it, leaving it still arguing the position Matt had reversed.

### Verification (follow-on)

Build 0/0; **599 tests**; all six variant hashes unmoved on trace and chosen actions; all five Godot
self-tests exit 0. `IntelligenceWriter` and the Godot roster now agree about what "how he takes them"
means — they had diverged, with the panel gaining both additions and the runner's viewpoint showing
neither.

## Correction — a history entry with no movement behind it, 2026-09-09

**Codex reviewed `6738200` and returned one P1, accepted by Matt: `RecordAccountConflict` and
`RecordAccountAgreement` remembered a `StandingChange` unconditionally, even when the clamped `Trust`
value did not actually move.** A man already floored at zero trust who is contradicted again, or
already ceilinged at full trust who is corroborated again, still acquired a fresh roster entry —
exactly the defect `Relations.Frighten` was already written to avoid for fear (`Being_frightened_is_
remembered_only_when_it_actually_moved`, this milestone's own test), and this milestone's two newer
`Remember` call sites did not carry the same guard.

**Fixed the same way `Frighten` already does it.** Both methods now capture `Trust` before applying
the clamp and remember only when the clamped value actually differs from it —
`if (rel.Trust < before)` for the conflict cost, `if (rel.Trust > before)` for the agreement gain.
Nothing about the movement itself, the clamp range, or `ConflictTrustCost`/`AccountAgreementTrustGain`
changed; only whether a null movement gets a memory.

**Two new production-path tests, mirroring the existing floor/ceiling proof for fear.**
`A_contradiction_at_the_trust_floor_is_not_remembered` establishes trust at 0.0, drives a genuine
conflict through `Cognition.Receive` (not a hand-built `AccountConflict`), and asserts
`StandingHistory` stays empty since trust never moves off the floor.
`A_corroboration_at_the_trust_ceiling_is_not_remembered` mirrors it at trust 1.0 for the agreement
side. Both mutation-checked: reverting either guard to an unconditional `Remember` makes its test fail
with a single stray `StandingChange` despite trust staying exactly at the clamp; both confirmed and
reverted before this commit.

**Nothing in the accepted fixture reaches either clamp**, so no accepted hash moved — the same
"inert in the natural run, provable only by construction" shape this milestone's own tests already
established for `Frighten`'s identical guard.

### Verification

- Build 0 warnings / 0 errors.
- Tests: **665 passed** (663 + 2 new in `RosterHistoryTests.cs`).
- `--verify` on all four required configurations, byte-identical to every prior accepted figure —
  `baseline` `7832105EC1F24154`, `disloyal-vincent` `6C23284BFD91C48D`, `resentful-tommy`
  `7A43D1AFB4A6E26F`, `capable-angelo` `5CACCFC566364BB7`.
- `--compare` at seed 42: 6 configurations, 6 distinct traces, 5 distinct chosen-action sequences
  (`baseline`/`resentful-tommy` converge, as recorded in milestone 022's own correction) — every
  digest unmoved.
- Both required viewpoint runs and all seven Godot invocations exit 0.
- Two mutation checks, each confirmed and reverted, as described above.

### Commit

One correction commit, production code plus tests. Awaits Codex re-review.

## Correction — two P2s on `4da1e66`, test-and-documentation-only, 2026-09-09

**Codex reviewed `4da1e66` — the same-day follow-on above — and returned two P2 findings, both
accepted by Matt.**

**First: the `PersonIsCapable` narration fix shipped with no regression coverage.** "Also corrected
here (`4da1e66`)," above, describes the fix (`PersonIsCapable` added to
`PlayerNarration.Describe`) but no test proved it, so a later edit could remove the arm again and
nothing would fail — exactly the gap that let the original defect ship in milestone 021 unnoticed.
Two tests added to `RosterHistoryTests.cs`:

- `PersonIsCapable_claims_render_as_prose_not_as_the_developer_predicate` pins the exact wording for
  both bars (`"tommy is a hard man"`, `"tommy can handle leaning on somebody"`) and asserts neither
  matches the raw `Claim.ToString()` predicate the defect actually produced.
- `Every_defined_claim_kind_has_its_own_narration` is the falsifier for the whole *class* of defect,
  not only this one instance: it drives every value of `ClaimKind` through `Describe` with a generic
  claim and asserts none of them falls through to the `_ => c.ToString()` fallback. A future
  `ClaimKind` added without a narration arm fails this test immediately, rather than reaching a
  player first and being found by scoping the next milestone the way this one was.

Both mutation-checked by removing the `PersonIsCapable` arm entirely: both tests failed (one on the
pinned wording, one on the fallback match), confirmed, then reverted — `git diff --stat` against
`PlayerNarration.cs` for this correction is empty; no production behaviour changed, only test
coverage was added, per the finding's own scope.

**Second: `4da1e66` left `PlayerNarration.Standing`'s class-level doc comment still arguing the
position Matt reversed on 2026-09-04** ("Follow-on, same day", above, records that this milestone
listed rewriting it in its own scope and then did not do it). **No source edit is needed for this
finding today: `1a7bcc6` (milestone 025's own work) already rewrote it.** `PlayerNarration.Standing`'s
doc comment at current HEAD reads "Milestone 023 put the cause on the roster beside this phrase... the
phrase itself still leaks neither the cause nor a number" — the corrected position, not the one Matt
reversed. Confirmed by reading the live source before writing this section, not assumed from the
commit message. Recorded here, append-only, as the accurate history: `4da1e66` left it wrong;
`1a7bcc6` fixed it; this correction did not need to touch it again.

### Verification

- Build 0 warnings / 0 errors.
- Tests: **667 passed** (665 + 2 new in `RosterHistoryTests.cs`).
- `git diff --stat` against `src/`: **empty** — this correction is test-and-documentation-only,
  confirmed rather than merely intended.
- Two mutation checks (the `PersonIsCapable` arm removed, both new tests confirmed to fail for their
  respective reasons), each reverted before this commit.
- `docs/PERSONALITY_AND_CHARACTER_PROFILES.md` and `docs/UI_AND_PLAYER_LEGIBILITY.md` untouched.
  Milestone 027 not begun.

### Commit

One correction commit, test-and-documentation-only. Awaits Codex re-review.
