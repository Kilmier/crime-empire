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
