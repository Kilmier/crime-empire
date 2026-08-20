# Milestone 014 — One Complete Player-Owned Operation

Authorized by Matt on 2026-08-19 after a read-only feasibility pass, with six rulings recorded in
full below. Every milestone since 009 had built or measured pieces of the player-facing boundary —
`PlayerSnapshot`, `PendingDecision`, the Godot shell, milestone 013's coverage accounting — without
walking a person through one complete operation: a real decision offered by the real pipeline, a real
wait while the calendar turns, a real consequence landing where the choice was made. This milestone
is that walk, using nothing that did not already exist.

## What this milestone is for

The operation was not invented for this milestone. Every accepted variant at seed 42 independently
reaches the identical event:

```
1987-04-01 15:00  Tommy Nardo collected from Bellini's grocery
```

Vincent Russo (capo, harbour), assigned to restore the harbour tribute, starts a `SecureTribute`
operation against Bellini's grocery (owned by Marco Bellini) through his own scored decision. In the
accepted baseline trace, persuasion fails, he carries on, delegates to Tommy, persuasion still fails,
he escalates to threaten and then to force, Marco concedes under fear, and `Strategies.AdvanceTribute`'s
collection step runs: `owner.Capabilities.Cash += business.MonthlyRevenue * 0.2`
(`Strategy/Strategies.cs:251`). Vincent's own cash rises by 840 — the owner/executor split milestone
011's correction was about, playing out exactly as designed, since Tommy executed the collection and
Vincent, the owner, is who is paid.

## Rulings taken at planning time

Matt's authorization, in full:

**1 — `PlayerSnapshot`'s contract is amended, precisely.** It may expose the viewpoint character's own
private state, cognition, and legitimately known information — his own `Capabilities.Cash` among it.
It must expose no other character's cash, private scores, world truth, or any reference or path back
to mutable simulation state.

**2 — "One operation" means the continuous player-owned arc.** Vincent receives every decision the
operation naturally produces — continuation, delegation, escalation included — through the interactive
path. None of his own decisions within that arc are resolved automatically.

**3 — Abandonment is a legitimate outcome.** The player may choose `let it lie` or any other option
that ends the operation short of collection. No coefficient is tuned to make collection more or less
likely.

**4 — The first-choice-plus-autonomous-continuation test is retained, but is not the complete playable
experience.** It proves causal attribution and actor parity, standing alongside, not in place of, the
full interactive playthrough ruling 2 requires.

**5 — All six previously specified tests are required, plus a negative test.** Natural-run,
counterfactual-choice, information-boundary, autonomous-equivalence, determinism, and pause/resume;
and a new test proving another character's cash cannot appear anywhere in Vincent's `PlayerSnapshot`.

**6 — The golden-path test is explicit and comparative.** It makes the five known seed-42 choices by
name, reaches 1 April, asserts both halves of the consequence, and compares the result against
autonomous execution.

**Out**, preserved without exception: no player-only action or action kind; Godot gains no reference
to `World`, a decision record, a score, or anything beyond `PlayerSnapshot`/`PendingDecision`; no
coefficient tuned and no fixture value changed to force any outcome; no new organization, no
persistence, no tiering, no cast expansion, no clean/dirty money.

## What was completed

**`Session/PlayerSnapshot.cs`.** Added `double Cash` to the record, populated in `PlayerView.Build`
as `who.Capabilities.Cash` — a value copied out at construction, never a reference to the mutable
`Capabilities` object it came from. The type's documented contract is rewritten to state the amended
rule from ruling 1 directly, replacing the prior "derived only from Cognition and SocialState"
statement, and explains why `Cash` is exempt from "no number reaches the player": it is a fact about
the character, not a measurement the model took of him.

**`Godot/Game.cs`.** Displays `snapshot.Cash` in the toolbar, one line, matching the shell's existing
plain style. No new panel, no new interaction, no reference to `World` added.

**`tests/CrimeEmpire.Simulation.Tests/PlayerOwnedOperationTests.cs`** (new, 9 tests). Natural-run,
the golden path (ruling 6), abandonment (ruling 3), counterfactual-choice, first-choice-plus-autonomous
(ruling 4), information-boundary, the cash negative test (ruling 5), determinism, and pause/resume —
all listed in Scope. Every multi-decision test drives the session through `PlayThroughPreferredChoices`,
which chooses the pipeline's own top-scored candidate at every pause via an explicit
`SimulationSession.Choose` call, never `ResolveAutomatically` — satisfying ruling 2 by construction
regardless of how many pauses the operation actually produces, rather than by hardcoding an assumed
count. The five named decisions (`start:tribute:bellini-grocery:Persuade`,
`continue:SecureTribute:bellini-grocery`, `delegate:SecureTribute:tommy`,
`escalate:bellini-grocery:Threaten`, `escalate:bellini-grocery:Force`) are asserted to occur, in order,
within whatever the full sequence turns out to be.

**`tests/CrimeEmpire.Simulation.Tests/PlayerSessionTests.cs`.** `Capabilities` added to the existing
structural boundary test's forbidden-types list, so the reflective walk over `PlayerSnapshot` and
`PendingDecision` now proves — not merely by convention — that the mutable object `Cash` is copied from
can never itself be reached through the player boundary.

**Verification.** Full re-run from a clean tree: build 0 warnings/0 errors across four projects;
467/467 tests (458 before this milestone); `--verify` deterministic and byte-identical on `baseline`
(`FEE45FD886F18CA8`), `disloyal-vincent` (`45CCF5ADC6EC0302`), `resentful-tommy` (`F5BD93386DE04082`);
`--compare` byte-identical across all five trace hashes and chosen-action digests; both required
viewpoint runs exit 0; Godot headless self-test: 4 choices, 4 decision screens, exit 0, transcript
shows `· cash on hand 6,000` on every screen (the self-test's own "always take the first option"
policy never starts the operation, so this confirms the field renders correctly without claiming the
self-test demonstrates the operation itself — the interactive tests do that). No trace hash or
chosen-action digest moved, as expected: nothing in `Strategies.cs`, `Commit.cs`, `Filters.cs`, or
`Generators.cs` changed, only a new, additive snapshot field.

## Important discoveries

**The operation naturally produces seven of Vincent's own decisions before 1 April, not five.** The
feasibility pass, working from a partial reading of the trace, identified the five decisions the
operation is named for (start, carry on, delegate, escalate, escalate) and Matt's ruling 6 was written
against that count. Implementing the golden-path test against the full trace found two more, both
still on the same thread: on 27 March an unrelated incident — Det. Kane's investigation "changing the
picture" — wakes Vincent and he reaffirms the escalation to force (top-scored again, not a new
choice in substance but a real pause requiring a real answer); and the moment collection lands, on 1
April, `StrategyComplete` wakes him again to decide how to report it to Salvatore, which he does,
leaving his own part out. Both are handled the same way as the five named ones — an explicit `Choose`,
never autoplayed — and the golden-path test was written to discover and play through however many
pauses actually occur rather than assume the five-decision count the planning pass had assumed. This
is recorded here rather than silently corrected, per this project's own standing practice: a count
stated at planning time that implementation shows to be wrong is a finding, not a detail to quietly
fix.

**Choosing the pipeline's own preference through `Choose` is not the same claim as calling
`ResolveAutomatically`.** The golden-path test's `PlayThroughPreferredChoices` helper reads
`PreparedDecision.Scored[0]` and translates it into an explicit `PendingOption` token, then calls
`SimulationSession.Choose` with it — the same path a Godot button press uses. This is a stronger proof
than it might first appear: it exercises the token-translation and `Choose`/`Resolve` path at every
single pause of a real operation, not just the one pause most tests stop at, while still recording
(and asserting on) exactly which candidate was chosen at each step.

**`Capabilities.Cash` needed a value/reference distinction the prior contract didn't have to draw.**
Every existing field on `PlayerSnapshot` before this milestone was either a belief (mediated,
qualitative, sourced) or an id/name (public knowledge). Cash is neither — it is the character's own
private state, known to him directly and without a source, and copied out as a plain value rather than
derived through `Cognition`. The amended doc comment and the strengthened structural test both exist
because that is a genuinely new kind of thing to let cross this boundary, not a minor addition to an
existing category.

## Deferred work

Everything carried into milestone 013, unresolved by this milestone: the 124 live-edge findings and 5
apparently-dead lines catalogued in `docs/COVERAGE_ACCOUNTING.md`, none triaged by priority or acted
on; systematic mutation automation and seed-sweep promotion, unnumbered `ROADMAP.md` candidates; and
everything carried into milestone 012 before that. This milestone adds nothing new to that list — it
touches no simulation behaviour, so nothing it did is itself deferred work.

## Where to look and what to distrust

The claim most expensive if wrong is the golden-path test's comparison against autonomous execution:
if `PlayThroughPreferredChoices` ever chose something other than the pipeline's true preference, the
"matches autonomous execution" assertion would still likely pass by coincidence on this one seed
rather than by the mechanism claimed. It is not coincidental here — `Scored[0]` is read directly from
the same `PreparedDecision` the production pipeline built for this exact pause, not recomputed — but a
reviewer should re-derive that guarantee from `Pipeline.cs` rather than take the test's own naming on
faith. The seven-vs-five decision count is mechanically reproduced (rerun `dotnet test --filter
PlayerOwnedOperationTests`) and low-risk to re-check. The snapshot contract amendment in
`PlayerSnapshot.cs`'s doc comment is prose, not enforced by the compiler beyond what the strengthened
structural test in `PlayerSessionTests.cs` covers — that test is the actual guarantee; the comment is
commentary on it.

## Commit

One implementation-and-archive commit. Status is not established by this file —
`docs/CURRENT_MILESTONE.md` says what is active, and Matt's confirmation of a named commit is the only
thing that counts as acceptance.

---

## Correction from Codex's review of `712a125`, 2026-08-20

Appended, not folded in. The account above — including its description of
`PlayThroughPreferredChoices`, its "five named decisions... asserted to occur, in order" framing, and
its "Where to look" paragraph's defence of that mechanism — is preserved as originally written and is
**superseded by this section** wherever it describes how the interactive choices were determined.

**Codex reviewed `712a125` and returned two P1 and two P2 findings, all about how "interactive"
the golden path actually was — no simulation code was touched by the implementation, so none of this
concerns behaviour.**

**P1 — the golden-path mechanism was not actually driving the interactive path.** `ChoosePreferred`
(named `PlayThroughPreferredChoices` in the account above) read `PreparedDecision.Scored[0]` through
reflection into `SimulationSession`'s private `_prepared` field to decide which option to press. Every
pause *was* resolved through an explicit `Choose` call rather than `ResolveAutomatically` — ruling 2's
letter was honoured — but the question "which button do I press" was answered with information no
Godot button carries: a candidate id, a utility score, session-private state reached by reflection.
The account's own "Where to look" section had already named this exact mechanism as the highest-risk
claim in the milestone and argued it was sound because `Scored[0]` was read from the real, unmodified
`PreparedDecision` — a true statement that answered the wrong question. Whether the read value happens
to equal the pipeline's true preference was never in doubt; whether reading it *at all* constitutes an
interactive playthrough is what needed to be true, and was not.

**Fix.** `PlayerOwnedOperationTests.cs` was rewritten: the seven decisions are now
`SevenChoiceSequence`, a hardcoded array of the exact `PendingOption.Description` text the accepted
trace renders at each pause, obtained by running the real interactive path once and reading what it
actually offered (not reconstructed from the developer trace's wording). `ChooseByDescription` matches
against that public text and the opaque `Id` token alone — the same two things a Godot button carries
— and `PlayGoldenPath` drives all seven in order. No candidate id, no score, and no reflection remain
anywhere in the choice-making path; `PreparedDecision` is used in exactly one place left in the file,
the natural-run test's structural check that the offered candidate really is a `SecureTribute`/
`Persuade` start — a developer-side assertion about what was offered, not a mechanism for choosing.

**P2 — Godot itself had no end-to-end proof, only the general self-test's incidental confirmation that
the `Cash` field renders.** Added `Game.cs`'s `--selftest-goldenpath`: it presses the same seven
option texts as real button clicks, using "Next event" between decisions rather than "Advance a week"
— a genuine bug this correction found in its first draft, where "Advance a week" leaves a fast-forward
horizon that survives across `Choose` calls and silently carried the run past the seventh choice into
an unaddressed eighth decision on a different thread. The check now asserts no eighth pause follows
the seventh, then reads `· cash on hand 6,840` off the live rendered screen. Godot still touches only
`PlayerSnapshot` and `PendingDecision`, as before.

**P2 — the negative cash test compared one property instead of the whole snapshot.** Rewritten to
walk the complete public `PlayerSnapshot` value graph reflectively (`ValueGraph`, mirroring the
structural-walk pattern already used in `PlayerSessionTests.cs`) and check both sentinel amounts
against every number and string reachable from it. Mutation-checked directly: `PlayerView.Build` was
temporarily changed to return `world.Get("marco").Capabilities.Cash` instead of the viewpoint
character's own, the strengthened test was confirmed to fail (`Collection: [913311], Not found: 6000`),
and the mutation was reverted before this commit.

**P1 — the simulation golden-path test asserted the consequence only through `session.World`.** Added
`Assert.Equal(6840, session.Snapshot().Cash);` alongside the existing `session.World.Get(...)
.Capabilities.Cash` and `Business.PayingTribute` assertions, so the test checks the same surface a
player would actually see, not only the internal state behind it.

**Documentation.** `Game.cs`'s class doc comment, which still described the snapshot as "bounded by
the viewpoint character's own cognition and relationships" — stale since `Cash` is neither — is
corrected to state ruling 1 precisely. `docs/DESIGN_DECISIONS.md`'s information-channel section is
amended the same way, citing this correction. `docs/CURRENT_MILESTONE.md` and
`docs/REVIEW_LEDGER.md`'s "Measured — milestone 014" section are corrected to withdraw the claim that
the original golden-path test was a complete, non-autoplay interactive playthrough — it resolved every
pause without autoplay, which is true and stays true, but it was not interactive in the sense of using
only information available to a real player, which is what "playthrough" implies and what the
correction now actually builds.

**What this correction is not.** Documentation and test/interface-boundary work only — no simulation
code changed, and the verification figures that depend on `Strategies.cs`/`Commit.cs`/`Filters.cs`/
`Generators.cs` (all five trace hashes, all chosen-action digests) are unaffected and were re-confirmed
by a full clean-tree re-run rather than assumed.

**Recurring-failure list, walked.** *False-assurance claim:* "the interactive path exercising itself"
was asserted about a mechanism that read internal ranking state to make its choices — the exact shape
this project's own review checklist exists to catch, and exactly why a second reader found it. *A fix
introducing the error it was meant to remove:* the first draft of the Godot check, written specifically
to prove a genuine end-to-end interactive path, initially overshot past the seventh choice for the same
underlying reason milestone 013's second correction diagnosed in itself — a stated rule ("stop after
the seventh choice") not re-verified against the mechanism actually used ("Advance a week" carries a
fast-forward horizon across choices) until a second check caught it. *Recording a review that did not
happen:* this correction is Matt's report of Codex's review, acted on and re-verified, not a claim to
have observed the review itself.

**Status.** This corrective commit is implemented, tested (9 tests in `PlayerOwnedOperationTests.cs`,
all rewritten to the description-based mechanism), and both Godot headless checks pass
(`--selftest`: 4 choices, 4 decision screens, exit 0; `--selftest-goldenpath`: seven choices pressed by
exact text on the correct dates, `cash on hand 6,840` read off the live screen, exit 0). It is **not
accepted** — Matt's confirmation of this named commit is what that requires.
