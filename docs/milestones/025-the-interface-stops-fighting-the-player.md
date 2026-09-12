# Milestone 025 — The Interface Stops Fighting the Player

Third milestone of the demo arc's layer 1 (`ROADMAP.md`, "The demo arc"). Authorized by Matt on
2026-09-05 with three rulings; widened by him the same day, reversing the third. Built on Fable 5.1,
the model question the authorization left open.

## What this milestone was for

Milestone 018 recorded presentation debt it was scoped not to fix, and 023 and 024 added to the same
screen without touching the layout. The debt as recorded: a five-column layout never rebalanced after
a fourth was added, a vertically-wrapped date, an oversized toolbar, `PlayerNarration`'s prose, and
"You control" / "You see through" as two start-screen fields.

**The only milestone in layer 1 with no simulation behaviour in it.** Nothing under `Decision/`,
`Domain/`, `Org/`, `Sim/` or `Strategy/` was touched, and all six variant hashes are unmoved by
construction: every hash `--compare` reports is one already recorded in the milestone 021 archive and
`REVIEW_LEDGER.md`.

## The finding that reframed it

`LATELY` was a literal subset of `WHAT HE KNOWS` — the same `PlayerBelief` objects re-sorted by
`ReconsideredAt` — and `RECENTLY` said what the roster history (023) already said, without the cause.
The screen was showing three things in five columns. The fix was subtraction, and rebalancing five
columns would have preserved the duplication. Reading the real 4 April Salvatore screen showed the
same claim drawn four times: as a belief, as an "unsettled" line, as a `LATELY` entry, and as the head
of the disagreements block.

## Matt's rulings

1. **Delete `RECENTLY`.** Done, with its projection: `PlayerSnapshot.RecentTrustMovements`,
   `PlayerRelationshipMovement` and `PlayerNarration.Movement` are gone. The two staged tests that
   pinned the removed projection's listener-scoping went with it; the same fact — a movement is shown
   only to the man whose relationship moved — is pinned for the surviving surface by
   `RosterHistoryTests.A_movement_is_remembered_by_the_man_it_moved_and_nobody_else`.
2. **Merge `LATELY` into `WHAT HE KNOWS`.** Done. `PlayerSnapshot.Recent` is removed; the panel
   orders the one list by `ReconsideredAt`, newest first, and draws a faint "— earlier —" rule after
   the beliefs inside `PlayerView.RecentWindow`. Disagreements about a held belief nest under it as
   "X says so / says otherwise (date)"; a disagreement about a claim he does not hold, which the
   projection allows, draws on its own under "ACCOUNTS DIFFER".
3. **Cut the `PlayerNarration` prose rewrite** — *reversed by Matt the same day*, on reading the
   layout sketch: "the language used here is not player friendly … we need to figure out what it
   means and put that into plain english. as a part of this milestone." His condition for un-deferring
   it had been a named list of lines that read badly; he named them, and the whole vocabulary was
   treated as the list. Recorded as a reversal rather than quietly absorbed.

Two further decisions, Matt's, taken at the sketch: **second person** when the viewpoint is the
character being played, third when watching somebody else or on the console; and **"tribute" and "the
outfit"** stay as the words.

## What was completed

**Layout (`CrimeEmpire.Godot/Game.cs`).** A two-row strip along the top — date, who you are, cash,
the clock controls; then the paused/running state and, far right and faint, seed and variant — and
four weighted panels: WHAT YOU KNOW (1.25), WHAT YOU ARE DOING (1.0, the standing order at the top of
its own panel, then WHAT JUST HAPPENED and WAITING TO HEAR BACK), WHAT YOU THINK OF PEOPLE (1.0), and
A DECISION (1.25). Panel headings follow the viewpoint's pronouns, so Kane's screen no longer says
"WHAT HE KNOWS". The wrapped date was diagnosed rather than guessed: `Plain()` sets `ExpandFill`, the
old toolbar used it for every label, and the non-expanding date heading was squeezed to minimum
width; nothing on the strip expands now except the spacer.

**Start screen.** "Play as" is one field. "Watch only" keeps the old "nobody" mode. The divergent
viewpoint — a debugging capability the no-leak proofs depend on — sits behind a developer checkbox at
the foot of the screen, hidden until ticked.

**Plain English (`Session/PlayerNarration.cs`, `PlayerOption.cs`, `PlayerOccasion.cs`).** Every
player-facing phrase rewritten. Representative:

| Before | After |
|---|---|
| Bellini's grocery is holding back what it owes | Bellini's grocery is not paying its tribute |
| the rule "no-violence-harbour" stands | the outfit's rule: no public violence in the harbour |
| Vincent Russo went outside "no-violence-harbour" | you broke the rule: no public violence in the harbour |
| beyond doubt for him, he had a hand in it himself | you set it yourself |
| strongly supported, the books told him | the books say so; you are fairly sure of it |
| he takes him as he finds him | you would take his word within reason |
| talk Bellini's grocery round / lean on / strong-arm | persuade Bellini's grocery to pay / threaten / use force on |
| ask X for his own account of whether … | ask X what he knows about whether … |
| ask Salvatore Greco for room to move | ask Salvatore Greco for permission |
| let it lie | take no action |

The rule's text is the policy's own `Description`, resolved by `PlayerView.NameIn`, which the session
now shares so an option and a belief resolve an id to the same words. A `Voice` is one extra pronoun
set, `PlayerView.You`, threaded through the same `Pronouns` parameter every phrase already took;
`SimulationSession` passes it when the viewpoint is the controlled character, and always for a pending
decision, which is by construction the controlled character's own.

**Three rules found by reading, and now in the projection:**

- *No certainty on what he saw or did himself.* "You saw it yourself; you are certain of it" said one
  thing twice. `PlayerBelief.Certainty` is null for witness and participant sources.
- *No source line on an act of his own.* "You broke the rule — you had a hand in it yourself" named
  the author twice. `PlayerBelief.Attribution` is null when he is the subject and the participant.
- *A ledger is not a person.* "The books told him" read as though a Mr. Books had. A report from a
  source that is not a person "says so".

**The certainty phrase and the domain's `ConfidenceLabel` draw the same five bands**, and a test walks
the whole range to pin that they change at exactly the same points. The label reaches the trace and
was not reused, which would have moved every hash.

**Console (`Runner/Trace/IntelligenceWriter.cs`)** re-rendered with the same words so `--viewpoint`
and the panel agree.

## The duplicate-claim guard, and its limit

The milestone's one real regression guard: no claim is presented as an entry of its own twice on one
screen. `Game.ClaimEntry` is the only thing that draws such an entry and stamps the label with the
claim as node metadata; `AssertNoClaimDrawnTwice` walks the live tree and throws on a repeat, and every
self-test reads the screen through `Screen()`, which runs it — so it runs on every screen all seven
invocations look at, including the general self-test's several hundred. **Mutation check:** re-adding
a second rendering of the belief list, as the old `LATELY` column was, fails `--selftest` and
`--selftest-corroboration` at the first screen with a belief on it, both with "the claim
BusinessRefusesTribute(bellini-grocery) is drawn twice on one screen". Reverted.

The limit, stated: a duplicate drawn by a second belief-drawing path that bypassed `ClaimEntry` would
not be stamped and would escape. `BeliefEntry` is the only thing that accepts a `PlayerBelief`, so
writing such a bypass means writing the thing prohibited. It is a Godot self-test check rather than an
xunit test because the duplication is a rendering fact and the test project cannot load the Godot
project.

## Self-tests re-pointed, not weakened

Every self-test whose string moved was re-pointed at the same fact: the seven-choice and eight-choice
sequences at the same seven and eight choices, at the same pauses; "Accounts differ on whether…"
became a check that "Vincent Russo says otherwise" is drawn *after* the claim it disputes on the same
screen; "Vincent Russo put hands on Bellini's grocery" became "you got violent at Bellini's grocery",
since Vincent is the man playing. The test project's independently-typed copies of the sequences were
re-pointed the same way. One option was renamed twice: "do nothing" tripped the guard that no candidate
id reaches the player, because the floor candidate's id is the word "nothing"; it is "take no action",
and the guard was kept.

## Decisions worth their reasoning

**Fold "cannot settle" into the list.** `Unsettled` is also a literal subset of `Known` (held, and
either contested or under the confidence threshold). The certainty phrase on each entry already says
"not sure" or "disputed", so the Godot panel no longer draws those beliefs a second time; who has said
nothing keeps its place as NOT HEARD FROM, because that is not a belief. This was my recommendation at
the sketch, not one of the three rulings; Matt did not object. The console still renders the section,
recorded as still out.

**Recency computed in the renderer.** Ordering and the "— earlier —" rule use `ReconsideredAt` and
the public `PlayerView.RecentWindow`; the snapshot decides what may be shown, the renderer how it is
arranged, which is the documented split. `Recent` therefore had no consumer and went.

**`Sim/World.cs` left with a stale doc comment.** Its `PerceivedConflict` header cites the removed
`PlayerRelationshipMovement`. A one-line doc fix, left because the scope forbade any file under
`Sim/` in the diff. Recorded in `ROADMAP.md`.

## Lines caught by reading the output

Four, all after every test was green, which is the third milestone running in which that has been
the case:

- "you found the signs of it afterwards" for `TargetIsVulnerable` acquired by discovery — a weakness
  he sized up on an approach is not wreckage he came across. Discovery now says "found out for
  yourself" and nothing about what was found.
- "Salvatore Greco was in on it and told you" for the rule he issued. A rule is not something a man is
  in on: "told you in person".
- "the rule from the top" on Salvatore's own screen, when he is the top: "the outfit's rule".
- "Vincent Russo confirmed what you had heard: the outfit's rule: no public violence…" — two colons;
  the history line now uses a dash.

## Verification

- Build 0 warnings / 0 errors. Tests **623 passed, 0 failed** (608 − 2 removed with the projection
  + 17, `PlainLanguageTests.cs`).
- **All six variant hashes unmoved**, trace and chosen-action digest — every value `--compare` prints
  is already recorded in the milestone 021 archive and `REVIEW_LEDGER.md`. `--verify` deterministic.
  Both required `--viewpoint` runs exit 0.
- All five Godot self-tests and the two-process restart proof exit 0, run after the last wording
  change. The duplicate-claim mutation check above.
- The rendered screens of all seven invocations were read in full. The honest verification is a person
  playing it, which is what layer 1 ends in.

## Deferred

- **A history of finished operations** — 026.
- The console's "WHAT HE CANNOT SETTLE" duplication; the non-person report source's plural verb; the
  `Sim/World.cs` doc comment.
- The prose has been rewritten once, by one reader. What still reads badly in play is the playtest's
  to find.

## Commit

One implementation-and-archive commit.

## Appendix — the authorization as written

The text of `docs/CURRENT_MILESTONE.md` at the moment this milestone began, reproduced because it was
never committed on its own and would otherwise survive nowhere. The lifecycle does not durably record
rulings (`ROADMAP.md`, known debt); this is the workaround.

### Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

#### Status

**Milestone 025 — The Interface Stops Fighting the Player — is authorized and not started.**
Authorized by Matt on 2026-09-05 with three rulings, recorded below. Third milestone of the demo
arc's layer 1; layer 1 finishes with 026 and then a playtest.

Milestones 021 through 024 are implemented and committed, and all four are **unreviewed and
unaccepted** — Codex ran out of usage during milestone 020's correction chain. `REVIEW_LEDGER.md`
calls everything from `34cd117` onward *cleared to build on*, not *accepted*. Baseline at the start
of this milestone: **608 tests passing**, 0 warnings, six variant hashes, five Godot self-tests and
the two-process restart proof all green at `f993386`.

#### What this milestone is for

Milestone 018 recorded presentation debt it was explicitly scoped not to fix, and 023 and 024 both
added to the same screen without touching the layout — 024 deliberately declining a sixth column on
the grounds that 025 exists to clear this. The debt as recorded: a five-column layout never
rebalanced after a fourth was added, a vertically-wrapped date, an oversized toolbar,
`PlayerNarration`'s prose, and "You control" / "You see through" as two separate start-screen fields.

**This is the only milestone in layer 1 with no simulation behaviour in it.** It is also the one the
arc's "every milestone must be visible in play" rule is easiest to satisfy — and the one where a
regression is least likely to show up as a failing assertion.

#### The finding that reframes it, found while scoping

**`LATELY` is a literal subset of `WHAT HE KNOWS`, in code:**

```csharp
var recent = held.Where(b => b.ReconsideredAt >= asOf - RecentWindow)
```

`Session/PlayerSnapshot.cs`. Not similar content — the *same* `PlayerBelief` objects, same wording,
same confidence, same attribution, re-sorted by `ReconsideredAt` instead of `AcquiredAt`. Only
`Disagreements` is unique to that column. The console `--viewpoint` surface never renders `Recent`
at all, which is why nothing ever flagged it.

Together with the `RECENTLY` duplication already logged from milestone 023, that means **the screen
is not showing five things. It is showing three, and two of the five columns are copies.** The width
problem is therefore mostly a symptom: the fix is subtraction, and rebalancing five columns would
preserve the duplication while making it better-proportioned.

#### Matt's rulings, 2026-09-05

1. **Delete `RECENTLY`.** Milestone 023's roster history strictly subsumes it: both read
   `World.AccountConflicts`/`AccountAgreements`, but the roster line names the claim and is
   unwindowed where `RECENTLY` is capped at `PlayerView.RecentWindow` (14 days).
2. **Merge `LATELY` into `WHAT HE KNOWS`.** Recency becomes ordering and emphasis, not a column.
3. **Cut the `PlayerNarration` prose rewrite from this milestone.** It is the only listed item that
   reaches into the simulation library, it is read by three surfaces, and "a general rewrite" has no
   completion test. It stays deferred until there is a named list of lines that read badly in play.

#### Scope

**1. Subtraction, and what survives it.**

- `RECENTLY` is removed from `Game.cs`, and with it `PlayerSnapshot.RecentTrustMovements`,
  `PlayerRelationshipMovement` and `PlayerNarration.Movement`, all of which become dead. Removing
  the projection as well as the rendering is the point — leaving the field and not drawing it would
  keep a second, less informative derivation of the same events alive for a later surface to pick
  up, which is the failure shape `REVIEW_LEDGER.md` records as a distinction drawn in one place and
  dropped on the way to the next.
- `LATELY` is removed as a column. `PlayerSnapshot.Recent` is a projection the Godot shell is the
  only consumer of; whether it survives as a field is an implementation call, but nothing may render
  the same belief twice on one screen.
- `Disagreements` — the only content unique to that column — keeps a marked place inside
  `WHAT HE KNOWS`.

**2. The wrapped date.** Diagnosed, not guessed: `Plain()` sets `SizeFlagsHorizontal = ExpandFill`
and `Heading()` does not, so in the toolbar's `HBoxContainer` the four expanding labels take all the
slack and the non-expanding date is squeezed to its minimum width, where `AutowrapMode.WordSmart`
breaks it vertically.

**3. The toolbar.** Ten children in one row. `seed 42 · baseline` is developer metadata in the
player's chrome and should not be the fourth thing he reads.

**4. "Play as".** "You control" and "You see through" become one field. A viewpoint that differs from
the controlled character is a debugging capability and belongs behind something rather than on the
front door — but it must remain reachable, because `--viewpoint` divergence is load-bearing for the
no-leak proofs.

**5. What he has out gets its own place.** Milestone 024 put the standing order into
`WHAT JUST HAPPENED` deliberately, to avoid making the layout worse before it was fixed. Once the two
duplicate columns are gone there is room, and giving it one is the payoff for that restraint.

#### Explicitly out

- **The `PlayerNarration` prose rewrite** (ruling 3).
- **Any simulation behaviour whatsoever.** No file under `Decision/`, `Domain/`, `Org/`, `Sim/` or
  `Strategy/` is touched. If one appears in the diff, something has gone wrong.
- **Art, theme, map, animation.** `Game.cs`'s own header records that it is deliberately plain, and
  the sequencing in `DESIGN_DECISIONS.md` §Stack has not retired that.
- **A history of finished operations** — still 026's, see below.
- **New player-facing information of any kind.** This milestone shows what is already shown, in
  fewer places. Anything the player cannot see today, he cannot see tomorrow either.

#### The constraint that makes this not cosmetic

**Five Godot self-tests drive the interface by exact rendered string** — `Press("Advance a week")`,
`Contains("cash on hand 6,840")`, `Contains("no answer yet")`,
`Contains("Bellini's grocery: not currently paying")`, `Contains("chose to ask Vincent Russo")` —
and the test project holds 34 `Contains("…")` assertions besides. The one milestone whose purpose is
to change presentation is the one pinned to presentation text.

That is the work, not an obstacle to it. Two things follow. A self-test whose string moves must be
**re-pointed at the same fact**, never weakened to whatever the new screen happens to say — the
`Press` loop in particular proves that every step goes through a real button, which milestone 009's
fourth correction added precisely because bypassing the widgets proved nothing. And a self-test that
becomes easy to satisfy has stopped testing; if one can no longer fail, say so rather than keep it.

#### Verification

Full suite as usual, plus the five Godot self-tests and the two-process restart proof, which are the
actual proof here.

**All six variant hashes must be unmoved by construction, not by luck** — nothing under the
simulation's behaviour surface is touched, so any movement means the scope was breached rather than
that a number drifted.

**The mutation check is weak here and that will be stated rather than dressed up.** There is no
production edit that makes "the layout is legible" fail. What can be mutation-checked is the
subtraction: re-adding a second rendering of the same belief must fail a test asserting no claim is
drawn twice on one screen. That test has to be written, and it is the milestone's only real
regression guard against the duplication coming back.

The honest verification is a person playing it, which is what layer 1 ends in.

#### Open, and Matt's call

**A history of finished operations.** `Strategies.Complete` nulls `Execution.Strategy`, so a
completed operation leaves nothing behind: the panel goes empty the moment a job finishes, and there
is nowhere to read what he has already done. Honest but thin, and a natural fit for 026, where a
session that ends needs to be able to say what happened in it.

**Which model runs this.** Matt raised trying Fable 5.1 on 025 specifically, on the grounds that it
is the most forgiving milestone in layer 1 — mechanical, no simulation behaviour, entirely visible in
play. Undecided at the time of writing; milestone 020's cost is the argument for settling it before
implementation rather than switching partway.

#### What is deferred

Not authorization to start any of it — see `ROADMAP.md`.

**From 025:** the `PlayerNarration` prose rewrite (ruling 3), pending a named list of lines that read
badly in play. Recorded rather than dropped: "he takes him as he finds him" is the line milestone 018
singled out.

**From 024:** multiple simultaneous operations, which the model does not have — one `Strategy` per
character.

**From 023:** trust from completed work was considered and **declined** — trust means "would I take
his word" and moves on account conflicts and corroborations, so folding job outcomes into it would
collapse the distinction milestone 021 drew between reliability as an informant and reliability as an
executor.

**From 022:** rumour mutation and false rumours; strength growing with repetition; street talk
reaching civilians, which needs rumour-to-fear to matter. The honest lever for making rumour live is
**who is in earshot** — which layer 2's rival gang and extra shopkeepers supply for free.

**From 021:** assessments of skills other than Coercion; decay of an assessment; a capability belief
acquired by testimony being permanently unrevisable; a character's confidence in his own ability.

**From 020 and earlier:** crew, equipment, preparation, recruitment, payroll, resource transfer; a
third subordinate; a general suitability model. Territory, patrol, weekly planning; the known
pause-timing information leak.

**From 016 and earlier:** 124 live-edge findings and 5 apparently-dead lines
(`docs/COVERAGE_ACCOUNTING.md`); systematic mutation automation and seed-sweep promotion; a queryable
decision-trace store; nobody holding a scored relationship with Kane; the tuning guesses; obligation
read but never moved; save slots, autosave, a save browser, cross-build migrations. From
`OPEN_CONCERNS.md` #3: decay, negative trust, whether respect and resentment are separate dimensions,
whether provenance should weight the social consequence, and whether `GrievanceWeight` should be
capped.

## Correction, same day — the playtest's first findings

Matt played it on 2026-09-05 and sent back six points with screenshots. Four were wording or
projection and are corrected here; two are simulation behaviour and are drafted as the next
milestone's scope in `CURRENT_MILESTONE.md`, awaiting his authorization, with the candidate entry
in `ROADMAP.md` updated to carry his rulings.

**1. A decision arrived with no context but "restore the harbour tribute".** Everything a player
wants at that moment is in the `Assignment` record taken at issuance — who gave it, the deadline, what
he disclosed, the rule he attached — and the focus line showed the objective alone. `PlayerOccasion.Focus`
now takes a lookup for the assignment and composes the briefing from that record:

```
on your mind: restore the harbour tribute, for Salvatore Greco, by 1 April. Salvatore Greco told you:
Bellini's grocery is not paying its tribute. His standing rule: no public violence in the harbour.
```

Nothing reads the issuer's current mind. Matt's own suggested wording, "Greco is waiting to see how
you'll handle it", is Salvatore's state and the capo does not have it; "by 1 April" is the deadline he
was given, so that one is his. The rule is stated once, from the constraint, and the disclosed
awareness claim that says the same thing is skipped. Without the lookup — every older test — the
focus is the objective alone, as before.

**4. "You made your demand, and once it went nowhere."** A failed attempt is `Strategies.Blocked`
recording `tribute-refused`; the shop held out. It now reads "and been refused once", "twice", "again
and again". What the shopkeeper *said* — "I'm not paying" or "I don't have the money" — is a reason the
model does not hold, so no words are put in his mouth. The six days it takes to reach the first refusal
are the operation's own pacing (`StepInterval`, three days a step) and are on Matt's list to come back
to, not changed here.

**5. The footer** now reads "These are the things that occurred to you and that you could actually
do. Anything you did not think of is not here."

**6. The three answers could not be told apart.** Read from the generator and `Reporting.Compose`
rather than guessed: a *partial* answer to a question withholds the one claim the question is about,
so it is silence on the subject; a *false* answer is a bare denial; and the candid answer, when the
question is about his own act — the only case deception is offered for — is an admission. They now
read "admit it to Salvatore Greco: you got violent at Bellini's grocery", "say nothing to Salvatore
Greco about it either way", and "deny it to Salvatore Greco: tell him you did not get violent at
Bellini's grocery". "Leaving out your own part" described the mechanism, not the effect, and was a
mistranslation. `PlayerNarration.Deny` gives the two self-namable claim kinds a denial of their own;
anything else falls back to "it is not true that…".

**2 and 3 — not here.** "If a character is not informed of something they should just say so" (Tommy,
asked about the grocery, has no position and is offered nothing to say, so the request sits unanswered
for the whole run) and "an indication of how characters feel" (a threatened shopkeeper's fear, a lied-to
man's disbelief) are new channels — communicated replies and readable reactions — and touch `Decision/`
and `Org/`. Drafted as scope for Matt to authorize; see `CURRENT_MILESTONE.md`.

**Verification.** Build 0/0; **638 tests** (636 + 2, the briefing); all six hashes unmoved — the
`capable-angelo` values are those milestone 021's correction authorized the same day; `--verify`
deterministic; both viewpoint runs exit 0; all seven Godot invocations exit 0; the rendered briefing
read on every pause of the general self-test. Pins re-pointed at the same facts: the Tommy-to-Vincent
partial and false answers in `CausalFeedbackTests`, the refusal counts in `OperationReadsTests`.

## Historical-audit correction — additive work does not bypass review, 2026-09-12

Astra's independent historical audit found that the demo-arc rule introduced by `867922d` and carried
into milestone 025 described additive work as chaining safely without a gate and called layer 2 a
natural long code-ahead run. Matt accepted the P2 finding. The separate claim that a playtest could
stand in for review had already been corrected by `14ce1f5`; the additive-work exception remained live
at audited HEAD `891368d` and contradicted `AGENTS.md`'s current mandatory implementer/reviewer loop.

`ROADMAP.md` now keeps additive versus interlocking only as a forecast of integration risk. It states
that the label does not assign Class A/B/C, authorize scope, defer chronological exact-commit review,
or bypass Matt's acceptance, and it removes the later duplicate invitation to a long code-ahead run.
Every bounded milestone still waits at the same gate before the next begins.

This is a Class B documentation correction only. No simulation, UI, test, fixture, canon, persistence,
or baseline changed. Verification was the exact ROADMAP and append-only archive diff, a search for the
surviving gate-bypass language, and `git diff --check`; no runtime suite was rerun for this prose-only
change. It awaits Astra's independent exact-commit review.

## Historical-audit correction — projected incident collisions no longer crash the knowledge screen, 2026-09-12

Astra independently audited milestone-025 implementation commit `1a7bcc6` and found a valid crash:
`PlayerView` keeps domain claims distinct by incident id, while `PlayerClaim` deliberately removes
that truth-log id at the player boundary. Two contested incidents involving the same person and
business can therefore project to equal `PlayerClaim` values. `Game.BuildKnowledge` passed those
values to `ToDictionary`, which required uniqueness and threw. Matt accepted the P2 finding, and it
remained live at audited HEAD `891368d`.

The Godot renderer now groups disagreements at the deliberately lossy `PlayerClaim` boundary. It
draws one visible predicate for a collision and renders every source account from every underlying
projected disagreement. Held beliefs sharing the same visible predicate are likewise coalesced to the
freshest player-facing headline, so the existing no-duplicate-claim invariant still holds. No domain
`Claim`, incident id, truth-log reference, or mutable simulation object crosses into the UI.

Focused xUnit coverage drives the production `Cognition.Receive` and `PlayerView.Build` path with two
contested `PersonUsedViolence` claims that differ only by incident id. It proves that the boundary
still emits two disagreement records carrying the one permitted `PlayerClaim` and retains Vincent's
denial and Kane's affirmation. The existing Godot `--selftest` now first renders a synthetic snapshot
of that exact collision through `BuildKnowledge`, requires the visible predicate exactly once, and
requires both named account rows. A projection mutation that kept only one contested domain claim
failed the xUnit regression (`expected 2, actual 1`) and was reverted.

Verification after the final edit: build 0 warnings / 0 errors, including the Godot project; **691
tests passed** (689 + L1 + this regression); `baseline` `92F742E3CB85E54B`, `disloyal-vincent`
`455A684A29A5F717`, and `resentful-tommy` `ADC3F2DDF1A9D50C`, each deterministic; `--compare --seed
42` remained 6 distinct traces and 4 distinct chosen-action sequences; both required viewpoint runs
exited 0. Runtime behavior and the accepted baseline are unchanged. The Godot 4.7.1 executable was
not present on PATH or either local drive, so the compiled render regression and its render-side
mutation were not executed in this worktree; that is the disclosed verification limit for Astra.
The correction awaits Astra's independent exact-commit review.

## Correction to the projected-claim collision correction — incident positions and bases, 2026-09-12

Astra independently reviewed `8fdf2e5` and returned one P2, accepted by Matt. Grouping equal
`PlayerClaim` values stopped the crash and retained every named source account, but the held-belief
branch rendered only the freshest projected belief's headline and skipped each underlying
`PlayerDisagreement`'s own position and basis. Two same-predicate incidents could therefore disagree
with each other on what the viewpoint character thought, yet the screen would omit that distinction.

Both held and standalone disagreement groups now use one incident-row renderer. For every projected
incident it renders that incident's own `OwnPositionHeld` together with its own `OwnBasis`, followed
by that incident's source accounts. The visible predicate is still drawn once, and no incident id or
truth-log identity crosses the player boundary.

The production projection regression now stages one discovered belief held true and one inferred
belief held false and proves their distinct positions, bases, and named accounts all survive
`PlayerView.Build`. The Godot render regression uses two colliding disagreements with opposite own
positions and distinct bases and requires both lines, both source accounts, and one visible heading.
The focused projection test passed. Mutation-checking `PlayerView.Build` by temporarily dropping
every incident's `OwnBasis` made that test fail and was reverted. The full solution built with 0
warnings/errors and all 691 tests passed. The three verification hashes remained
`92F742E3CB85E54B`, `455A684A29A5F717`, and `ADC3F2DDF1A9D50C`; comparison remained 6 distinct
traces / 4 action sequences, and both required viewpoint runs completed. The Godot executable
remains unavailable in this worktree, so the compiled render regression and a renderer-side mutation
could not be executed here. This correction awaits Astra's independent review.
