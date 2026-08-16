# Milestone 009 — Godot Playable Shell

**Closed 2026-08-16. Not yet reviewed.**

Append-only. Corrections go at the foot of this file; nothing above is rewritten.

Authorized by Matt on 2026-08-16. This milestone put a person inside the decision pipeline and a
window over the simulation, and changed no simulation behaviour doing it.

## The authorized scope, as given

Reproduced as issued, because the milestone lifecycle loses rulings when the archive and the reset
land in one commit and that has now happened three times.

> Matt authorizes Milestone 009: Godot Playable Shell.
> Read AGENTS.md and the canonical documents in their required order. Preserve milestones 006–008 as
> closed and accepted. Do not begin unrelated simulation, concealment, persistence, tiering, or
> relationship work.
> The milestone must produce a genuine playable vertical slice:
>
> 1. Establish and verify the Godot/.NET boundary.
>
> * Use the installed Godot 4.7.1 .NET executable.
> * Add a Godot C# project under `src/CrimeEmpire.Godot`.
> * Keep every Godot dependency out of `CrimeEmpire.Simulation`.
> * Preserve .NET 10 for the runner and tests. Use the smallest compatible approach for Godot,
>   preferably multi-targeting the simulation library for .NET 8 and .NET 10.
> * If this requires invasive coupling or cannot be verified, stop and report rather than forcing it.
>
> 2. Add an engine-neutral simulation session boundary.
>
> * Support pause, next event, advance one day, and advance seven days.
> * Preserve event-driven scheduling; do not introduce frame-based simulation ticks.
> * Expose immutable, source-limited player-view data. The UI must not receive unrestricted access to
>   `World`, `TruthLog`, developer decisions, hidden utility values, or global reports.
> * Refactor deliberation into prepare/resolve stages only as necessary to support player choice.
> * NPC deliberation must retain existing behavior.
>
> 3. Make one real player choice playable.
>
> * Vincent is the default controlled character.
> * NPCs continue acting autonomously.
> * When Vincent reaches a deliberation, pause and present the candidates that survived his normal
>   belief, salience, capability, and access filters.
> * Do not expose rejected candidates, numerical utility scores, unknown facts, or developer-only
>   explanations.
> * Let the player choose one available candidate and resolve it through the same commit and
>   consequence systems used for NPC decisions.
> * Actor parity must be preserved: no player-only action implementation.
>
> 4. Build a deliberately plain Godot interface.
>
> * Start screen: seed, scenario variant, controlled/viewpoint character.
> * Main screen: date, pause/advance controls, source-limited knowledge feed, and pending-decision
>   panel.
> * Show recent observable consequences only where the viewpoint character could know them.
> * No art pipeline, map, animation, persistence, save/load, or UI polish beyond a clear functional
>   layout.
>
> Required tests and falsification checks:
>
> * A session with no controlled character remains byte-identical to the accepted batch simulation.
> * Automatically choosing the top-ranked option through the new prepare/resolve boundary reproduces
>   existing behavior.
> * Different stepping and fast-forward patterns produce identical outcomes when player choices are
>   identical.
> * A deliberately hidden fact cannot appear in the player snapshot or Godot UI.
> * Choosing a non-default valid action produces an attributable history change.
> * Existing runner verification and all five variants remain deterministic.
> * The Godot project builds and starts headlessly.
> * The Godot UI must consume structured data, not parse console-rendered text.
> * If implementing the player boundary changes autonomous baseline behavior, stop and explain before
>   accepting new baselines.
>
> Write the complete authorized scope and these rulings into `docs/CURRENT_MILESTONE.md`, ensuring the
> final milestone archive preserves them. Implement, run the full required verification, archive
> milestone 009, and make one coherent implementation-and-archive commit. Stop for Codex review. Do
> not begin milestone 010.

## Rulings taken at planning time

Numbered so a review can cite one. Each is a decision this milestone made that the scope statement
left open, or a judgement call it is better to state than to bury.

**1 — The engine hosts .NET 8, so the simulation library multi-targets and the Godot project does
not.** `GodotSharp/Api/Release/GodotPlugins.runtimeconfig.json` in the installed
`Godot_v4.7.1-stable_mono_win64` declares `"tfm": "net8.0"`, and `Godot.NET.Sdk/4.7.1` builds against
`net8.0`. So `CrimeEmpire.Simulation` targets `net8.0;net10.0`, `CrimeEmpire.Godot` targets `net8.0`,
and the runner and tests stay on `net10.0` exactly as milestone 002 settled. This is the
"preferably multi-targeting" option in the scope statement, taken as written.

**2 — `Directory.Build.props` stops assigning `TargetFramework` and publishes named TFM properties
instead, and this supersedes a note in `DESIGN_DECISIONS.md` §Stack.** That note records that
redundant per-project `TargetFramework` entries were deliberately *not* added because
`Directory.Build.props` was the single source of truth. It cannot stay literally true: a
multi-targeting project must set `TargetFrameworks`, and MSBuild ignores `TargetFrameworks` whenever
`TargetFramework` is already assigned — and `Directory.Build.props` is imported before any project
body, so no condition in it can see what the project is about to declare. The centralisation is kept
where it is worth having: the props file now defines `CrimeEmpireHostTfm` (`net10.0`) and
`CrimeEmpireEngineTfm` (`net8.0`), and every project selects from those rather than hardcoding a
moniker. One place still decides what "the host framework" means. **This is a documented supersession,
not a silent departure** — `DESIGN_DECISIONS.md` is updated in the same commit.

**3 — The Godot project joins `CrimeEmpire.sln`.** `dotnet build CrimeEmpire.sln` and
`dotnet test CrimeEmpire.sln` are the verification commands in `AGENTS.md`; a Godot project outside
the solution would be a build that nothing in the accepted verification covers.
`Godot.NET.Sdk/4.7.1` restores from nuget.org, verified before any code was written, so this adds no
machine-local path or private feed.

**4 — Deliberation splits into `Pipeline.Prepare` and `Pipeline.Resolve`, and
`Pipeline.Deliberate` becomes exactly `Resolve(Prepare(...), null)`.** `Runner` still calls
`Deliberate` on the NPC path, so "NPC deliberation retains existing behavior" is true by construction
rather than only by test. `Resolve(prepared, null)` chooses the top-ranked breakdown, which is the
single line the old `Deliberate` contained. The candidate ordering handed to the scorer is
irrelevant to the result — `OrderByDescending(Total).ThenBy(Id)` is total — so exposing the surviving
candidates in a different order for the player cannot move a score.

**5 — Player-facing options are ordered by candidate id, deliberately not by rank.** `Filters` returns
survivors in salience order and `Pipeline` scores them into rank order; either would tell the player
which option the model prefers, which is a utility score with the number filed off. Ordinal id order
is deterministic and says nothing.

**6 — `Agenda.Description` reaches the player; `Agenda.Reason` does not.** `Reason` embeds a numeric
pressure value (`"Resentment had grown to 0.62"`) and the developer-facing trigger kind
(`"the event demanded a response (PressureThreshold)"`). `Description` is the objective he was given,
the strategy he is running, or the pressure by name.

**7 — The trigger cause reaches the player as the occasion of the decision, and this is the ruling
most worth a reviewer's attention.** `SIMULATION_ARCHITECTURE.md` says developer traces record the
actual trigger and must not be presented to the player. Every cause that can wake a *deliberation* is
authored from the waking character's own side — "Salvatore handed him the harbour and a deadline",
"what he heard changed the picture", "Tommy reported in", "his patch came up for review again",
"Bellini's grocery held out against threaten" — so for the character the player controls it is not
hidden state, it is what just happened to him. Shown for that reason and no other. If Codex reads the
architecture line as absolute, this is the line to reject.

**8 — Candidate descriptions are shown verbatim, including their rough edges.** They are generated
only from the actor's `PerceivedSituation`, so they cannot carry a fact he lacks — but they name raw
ids (`have tommy handle …`) and embed `Claim.ToString()`, which prints a truth-log `EventId` as a
`#7` correlation suffix. Recorded as a cosmetic defect and **deferred, not fixed**: a player-facing
description vocabulary would be a second implementation to drift against the first, and changing
`Claim.ToString()` would move every accepted trace hash for a wording reason. See "Deferred".

**9 — `SimulationSession`'s `World` and its automatic-resolution path are `internal`, with
`InternalsVisibleTo` for the test assembly only.** "The UI must not receive unrestricted access to
`World`" is enforced by the compiler rather than by a convention the UI is trusted to keep. The Godot
project cannot name `World` through the session at all. A reflection test additionally asserts that
no public member of `SimulationSession`, `PendingDecision` or `PlayerSnapshot` exposes `World`,
`TruthLog`, `DecisionRecord`, `ScoreBreakdown`, `Report`, `PreparedDecision` or `Candidate`.

**10 — There is one source-limiting implementation, and the console renderer now consumes it.**
`PlayerView.Build` in the simulation library is the only code that decides what a viewpoint character
can see; `IntelligenceWriter` renders that snapshot instead of re-deriving the same rules. Two
independent implementations of "what may this character be shown" is exactly the shape this project's
recurring-failure list calls *a distinction drawn in one place and dropped on the way to the next*.
The in-fiction phrasing moves with it, into `PlayerNarration`, because the rules it enforces
("Discovery says *came across*, never *saw*") are information-safety rules and not layout.
`IntelligenceWriter`'s public surface — `Render`, `Describe`, `Standing`, `KnownPeople` — is preserved
so the accepted leak tests keep pinning the same strings, and its rendered output must stay
byte-identical.

**11 — The session's clock is a player-facing calendar distinct from `World.Now`.** `World.Now` is
the time of the last event actually processed; the session clock is how far the player has authorised
time to run. `AdvanceTo`/`AdvanceDays` set a horizon and pump the existing queue; nothing polls,
nothing ticks per frame, and no new scheduled-event kind is introduced. A pause for player choice
retains the outstanding horizon, so choosing resumes the fast-forward the player asked for.

**12 — A deliberation with nothing open to him does not pause.** If the controlled character's
candidates are all rejected — nothing occurred to him, or everything that did was ruled out on
knowledge, capability or access — the decision resolves on the autonomous path, recording "nothing
was open to him" and committing nothing. Presenting an empty list and demanding an acknowledgement
is a dead end wearing a decision's clothes, and it would break the invariant that a pause always
offers a real choice. A single surviving option still pauses: one option is a decision, none is not.

**13 — Out of scope and untouched.** No persistence or save/load. No art, map, animation, or tilemap.
No tiering. No coefficient, no candidate generator, no filter, no scoring term, no scenario fixture,
and no variant is changed. `--verify` and `--compare` keep their accepted output exactly; no runner
flag is added.

## What was built

- **`Directory.Build.props`** — publishes `CrimeEmpireHostTfm` / `CrimeEmpireEngineTfm`; no longer
  assigns `TargetFramework`. Each project selects one (ruling 2).
- **`CrimeEmpire.Simulation`** — multi-targets `net8.0;net10.0`. No Godot reference.
- **`Decision/Pipeline.cs`** — `Prepare` / `Resolve` / `PreparedDecision`; `Deliberate` retained as
  their composition (ruling 4).
- **`Sim/Runner.cs`** — `Step(world, until, controlledId)` returns `Advanced` / `AwaitingChoice` /
  `Exhausted`; `Run` is the same loop over `Step(..., null)`.
- **`Session/`** — `SimulationSession`, `PendingDecision`, `PlayerSnapshot`, `PlayerView`,
  `PlayerNarration`. Engine-neutral; no `Godot` using anywhere in the library.
- **`CrimeEmpire.Runner`** — `IntelligenceWriter` rewritten to render a `PlayerSnapshot`; output
  byte-identical, public surface unchanged.
- **`src/CrimeEmpire.Godot`** — `project.godot`, `Game.cs`, `Main.tscn`; start screen, main screen,
  pending-decision panel, and a `--selftest` headless mode that drives the real UI and dumps every
  string in the node tree.
- **`tests/…/PlayerSessionTests.cs`** — the falsification checks listed in the scope.

## What was verified, and against what

Every figure below was measured **after the last edit**, on a build produced by deleting every `bin`,
`obj` and `.godot` directory first. That rule exists because milestone 008 recorded five hashes its
own commit did not produce, having measured them before a late change and verified itself with a diff
that excluded the only region the change touched.

### The claim under test

**Milestone 009 changed no simulation behaviour.** Everything else is subordinate to that.

- **All five trace hashes are unchanged** from milestone 008's accepted baseline:
  `6EB3F6B996CFC631` / `A8A1BBD12D5334C2` / `DCEDCFF27928266F` / `E164E0A74E2EC7DC` /
  `982EC77BD5C253CB`, for baseline / cautious-vincent / watchful-boss / disloyal-vincent /
  resentful-tommy, each identical on both runs of `--verify`.
- **All five chosen-action digests are unchanged**: `38B7183ED2EEF34A` / `124E8FE932DD5A89` /
  `4F15ECD8B7A593BB` / `3D7F2B79BA4DC3E3` / `18B507EBBE4FBA7E`.
- **Decision counts 38 / 21 / 39 / 39 / 38**, conflicts 2 / 3 / 2 / 2 / 2, "rel. read"
  19 / 12 / 18 / 20 / 19, "rel. chose" 2 / 3 / 3 / 1 / 3 — every one unchanged.
- `--compare` still reports **five distinct traces and five distinct chosen-action sequences**.
- **All 30 viewpoint renders are byte-identical** — five variants times six characters, diffed
  against a scratch worktree at `3f08685`. This is the check that matters for `IntelligenceWriter`
  having been rewritten to consume a snapshot rather than derive the source limit itself.
- Build: **0 warnings, 0 errors** across four projects, Debug and Release.
- Tests: **343 passed**, 0 failed. 305 before the milestone; 38 added.

### The scope's own falsification list, item by item

| Required check | How it is met |
|---|---|
| A session with no controlled character is byte-identical to the batch simulation | `A_session_with_nobody_controlled_is_byte_identical_to_the_batch_simulation`, all five variants, full rendered developer trace |
| Auto-choosing the top-ranked option reproduces existing behavior | `Auto_resolving_a_controlled_session_reproduces_the_batch_history`, all five variants, and it asserts the run actually paused |
| Different stepping and fast-forward patterns produce identical outcomes | `Stepping_and_fast_forward_patterns_agree` — one call / day by day / week by week / 25 single events then a fast-forward, under one player policy, all five variants |
| A hidden fact cannot appear in the player snapshot or Godot UI | `A_hidden_fact_never_reaches_the_player_snapshot` over three viewpoints with two facts of different kinds, plus the headless Godot self-test's own UI text |
| Choosing a non-default valid action produces an attributable history change | `Choosing_a_non_default_action_produces_an_attributable_history_change` — identical up to the fork, the fork records the player's id, different from there on |
| Existing runner verification and all five variants remain deterministic | `--verify` on baseline, disloyal-vincent and resentful-tommy; `--compare`; both viewpoint runs |
| The Godot project builds and starts headlessly | `dotnet build CrimeEmpire.sln` covers the build; `--headless --path src/CrimeEmpire.Godot -- --selftest` exits 0 |
| The Godot UI consumes structured data, not console text | The Godot project references `CrimeEmpire.Simulation` and **not** `CrimeEmpire.Runner`, so this is a project-reference fact rather than a rule to keep |
| Stop and explain if the player boundary changes autonomous baseline behavior | It did not. No baseline moved, and the table above is the evidence rather than the promise |

### How "byte-identical" was made worth something

Compared on the **full rendered developer trace**, not on a structural snapshot. The existing replay
tests use a snapshot, which is right for what they do and wrong here: a snapshot is its own
comparator, so a field this milestone forgot to add would make the comparison blinder rather than
fail. The trace covers the truth log, every decision's trigger, agenda and beliefs used, every scored
candidate with its total, every rejection with its reason, and the relationship diagnostic.

### The Godot check is against the interface, not the data behind it

`--selftest` opens a session at seed 42 on `baseline` with Vincent controlled, plays ninety in-game
days taking the first offered option at every pause, and prints every string in the live node tree
after every screen it builds. It goes through the same `Refresh` and the same panel builders a person
sees; there is no separate headless rendering path, because a check against one would prove nothing
about the other. At this commit it makes 4 choices, renders 4 decision screens, and its output
contains none of: `Dorato's bakery is holding back what it owes`, `dorato-bakery`, `Nunzio`, any
rejected-candidate wording, or any decimal number.

The bakery is the right hidden fact for this because it is the fixture's own designed asymmetry
rather than something planted for the test: the shop really is refusing, the organisation's takings
really are short because of it, and **no character holds the claim**. The generator cannot reach it
either — `FromResponsibility` picks the first business he believes is refusing, falling back to the
first visible target, and `bellini-grocery` sorts before `dorato-bakery` — so a run that surfaced it
would mean something structural had changed, not that a string had slipped through.

## What was discovered

**The engine hosts .NET 8, and reading that off the install was the whole of the compatibility
question.** `ROADMAP.md` had carried "Godot 4 C# compatibility with `net10.0` is unverified" for four
milestones with a recorded fallback. The fallback is what happened, and it cost one line in each
`.csproj`: `GodotPlugins.runtimeconfig.json` declares `"tfm": "net8.0"`, so the library multi-targets
and everything else stays where it was. There is no invasive coupling to report, because there is no
coupling — the library gained no Godot reference, no `#if`, and no conditional code.

**Centralising a TFM and multi-targeting are mutually exclusive, and the reason is import order.**
Milestone 002 put `TargetFramework` in `Directory.Build.props` and recorded that per-project entries
were deliberately not added. That cannot survive a multi-targeting project: MSBuild ignores
`TargetFrameworks` whenever `TargetFramework` is assigned, and `Directory.Build.props` is imported
before any project body, so no condition written there can see what the project is about to declare.
What was worth keeping was kept — the props file now publishes the monikers as named values and each
project selects one — but the original sentence had to be superseded rather than quietly worked
around. Recorded in `DESIGN_DECISIONS.md` §Stack alongside the decision it supersedes.

**Splitting the pipeline at the fifth question is a smaller cut than it sounds.** The architecture
document separates five questions, and only the last — which available option do you prefer — is the
one a player answers differently from an NPC. Everything before it is identical for both, which is
what makes `Deliberate` reducible to `Resolve(Prepare(...), null)` with nothing lost. The consequence
worth stating: **"NPC deliberation is unchanged" is true by construction rather than by test**, and
the byte-identity tests are checking that the construction is what it appears to be, not establishing
the claim.

**The candidate ranking is total, which is what makes the player's list safe to reorder.** Scoring
sorts by descending total then by candidate id, so the order `Filters` hands the survivors over in
cannot affect the result. That is why exposing them in ordinal id order — deliberately not rank order,
which would leak the model's preference — cannot move a score. Had the tiebreak been partial, this
milestone would have had to choose between leaking the ranking and changing behaviour.

**Ordering by id had to be shown to be observable, not merely declared.** A test asserting the options
are id-sorted passes trivially whenever rank and id happen to agree, so there is a second one
asserting that across a full run the option the character would have taken is *not* always first. It
is the mutation check on the first, and it is the shape of test this project's ledger records as
missing twice before.

**Two surfaces showing a character what he knows is one derivation too many.** `IntelligenceWriter`
had held the source limit since milestone 003 and held it well. Adding a second surface would have
meant two answers to "what may this character be shown", which is precisely the recurring failure the
ledger names as *a distinction drawn in one place and dropped on the way to the next* — and the
divergence would have been invisible until one of them leaked. `PlayerView` now owns the derivation,
the in-fiction phrasing moved with it into `PlayerNarration` because the rules it enforces are
information-safety rules rather than layout, and the console renderer became a layout over the
snapshot. All 30 viewpoint renders are byte-identical, which is what makes that a refactor.

**Compiler-enforced beats convention-enforced, again.** `World` is `internal` on the session, so the
Godot project cannot name it. That is the same reasoning milestone 004 ended on and milestone 006
applied to `Relations`: a guarantee that is a property of the type survives a refactor, and a
convention about how to call something does not. **The residual gap is stated rather than papered
over** — nothing stops a future Godot script from calling `Cast.Build` and `Runner.Run` itself, and
closing that needs a separate player-contract assembly.

**A player choice is visible in the fixture immediately, and it is not a flourish.** With the
self-test's policy — take the first option every time — Vincent asks Tommy for an account, then twice
asks Salvatore to relax the no-violence rule, then answers his boss about the grocery. He never starts
a collection, so ninety days later he holds two claims and the harbour is exactly where it was. The
autonomous Vincent breaches the policy and the grocery ends up paying. The same character, the same
beliefs, the same options — a different history because somebody else answered the last question.

## Deferred, and where it is recorded

Everything below is also in `ROADMAP.md`'s technical-debt list.

- **Candidate descriptions are developer-shaped** (ruling 8): raw ids and a `#EventId` correlation
  suffix from `Claim.ToString()`. Honest but ugly. A player-facing description vocabulary is the fix,
  and it is a milestone-sized decision rather than a tidy-up.
- **The player cannot see why an option is unavailable.** Right for utility scores, arguably wrong for
  "he does not know that the bakery is holding out", which is the single most legible line in a
  decision trace and the one that proves the simulation is belief-limited rather than merely claiming
  to be. Whether a *belief-stage* rejection is player-facing is unanswered.
- **Nothing stops a future Godot script from calling `Cast.Build` and `Runner.Run` directly.**
- **`AGENTS.md` §Verification does not mention the Godot headless check.** No ruling authorized
  editing `AGENTS.md`, so the commands are in `REVIEW_LEDGER.md`'s baseline section instead. Same
  shape as the older `docs/RELATIONSHIPS.md` item, and it is now two.
- **One controlled character and one viewpoint character, chosen at the start screen and never
  changed.** No succession, no viewpoint switching mid-run.
- **No save/load**, so a session ends when the process does. SQLite remains selected and unbuilt.
- Every carried-forward item from milestone 008 is untouched and stays carried forward.

## Commits

One implementation-and-archive commit, as authorized. **Its hash is not knowable from inside it** —
this file is part of it — so, exactly as with `REVIEW_LEDGER.md`'s checkpoint, it is folded in by the
next change authorized on its own merits. What can be recorded here is what it is built on:
`3f08685`, which closed milestone 008 and is itself later than the review checkpoint and unreviewed.

Milestone 008's accepted state remains `7e0700e` and is untouched by anything here.

---

## Correction 1 — the occasion leaked, the DTOs were mutable, and the self-test could not fail

Appended 2026-08-16. Nothing above is rewritten; this is what the record now says.

Codex reviewed `901d345` and **rejected it on three findings**, all accepted by Matt. The Godot shell
builds and runs, and the autonomous behaviour was never in question — every hash in the section above
still stands unchanged. What was wrong was the boundary itself.

### Finding 1 (P1) — `Trigger.Cause` handed a delegated operation's outcome to an owner nobody had told

**Ruling 7 above is wrong, and it is worth stating why rather than quietly replacing it.** It argued
that every deliberation-waking cause is authored from the waking character's own side, and shipped
`ScheduledEvent.Cause` into `PendingDecision.Occasion` on the strength of that. The argument was made
by enumerating the schedulers and finding them safe. Two are not:

- `Strategies.Blocked` schedules `StrategyBlocked` **addressed to the strategy's owner** with the
  cause `"Bellini's grocery held out against force"`.
- `Strategies.Complete` schedules `StrategyComplete` with `"… finished: the cleanup made things
  worse"`.

When the work was delegated, the owner was not there, nobody has told him, and no discovery roll has
been made. Those sentences are the *executor's* operational outcome. Handing them over is precisely
the leak `Strategies.ResolveViolence` has a long comment refusing to commit — "authority does not
deliver knowledge" — arriving through the interface instead of through the simulation.

**And the same string reached the player through a second field.** `AgendaSelection.Select` sets a
`RespondToTrigger` agenda's `Description` to `trigger.Cause` verbatim, and `Focus` was
`Agenda.Description`. So the fix had to be a suppression of both, not a sanitising pass over one
property. That is the ledger's *stops halfway along the path a value travels*, and this milestone
produced it while its own ruling 6 was busy excluding `Agenda.Reason` for a numeric leak — the same
object, inspected for one hazard and passed for another.

**The fix is that nothing a scheduler authored crosses the boundary at all.** `PlayerOccasion` owns a
closed vocabulary keyed on `EventKind`, and **the default is silence**: a kind not named there
produces no occasion, so a future event kind is mute until somebody decides what a character
necessarily knows about it. Fail-closed rather than fail-open is the whole difference from the ruling
it replaces. `Focus` passes `Agenda.Description` only for the four agenda kinds whose description is
structurally the character's own, and never for `RespondToTrigger`.

`StrategyComplete` and `StrategyBlocked` are silent **unconditionally** rather than conditionally on
whether he executed the work himself. The condition is available for `StrategyBlocked`, where the
instance is still live, and not for `StrategyComplete`, where `Strategies.Complete` has already
cleared it by the time the event is handled. Two rules for one question is how the distinction gets
dropped between them.

Three tests, and the mutation check that matters: **reverting `Project` to pass `Trigger.Cause` and
`Agenda.Description` fails seven of them** — both staged proofs and the structural check in all five
variants.

- `A_delegated_failure_tells_its_owner_nothing_until_somebody_does` — stages Vincent as owner with
  Tommy executing, drives the real `Runner.Step` and the real projection, asserts the occasion and
  focus are null and the outcome wording appears nowhere.
- `A_delegated_completion_tells_its_owner_nothing_either` — the harder half, for the reason above.
- `No_authored_developer_string_reaches_a_player_surface` — structural, over natural runs of all five
  variants. Every decision record carries the exact cause text of the event that woke it, so the
  assertion is against **the whole vocabulary of authored strings the run actually used** — trigger
  causes, candidate descriptions, rejection reasons and agenda reasons — rather than a hand-written
  list of the ones somebody thought of. That is what makes it survive a new scheduler.
- `The_occasion_vocabulary_is_closed_and_silent_by_default` — enumerates `EventKind` and asserts the
  allow-list, so adding a kind cannot silently admit it.

**The hidden-fact check now covers every player surface, not the finished snapshot.**
`A_hidden_fact_never_reaches_any_player_surface` accumulates each pending decision — occasion, focus
and every option's wording — and a snapshot at every pause, and asserts over the union. The old
version only looked at the snapshot at the end, which is exactly why it never saw this.

**A residual this does not close, stated rather than left implied.** The *timing* of a pause is
observable: the player is stopped on the day a delegated operation fails, even though the interface
says nothing about why. Closing that would mean not waking the controlled character, which would
change autonomous behaviour. Recorded in `ROADMAP.md`.

### Finding 2 — the boundary was neither immutable nor opaque

Three things, all real:

**Castable collections.** Every `IReadOnlyList<T>` on the player-facing records was handed a
`List<T>`, so a consumer could cast it back and mutate the snapshot it was given. This is the defect
milestone 006 fixed on `IRelationship.Grievances`, reproduced on the new boundary — and the header
comment explaining why that one was wrapped was in the repository the whole time. Every collection is
now frozen in an `init` accessor, so the guarantee is a property of the type rather than a habit of
whatever built it.

**A one-level reflection test.** The original walked the public members of three types and stopped.
`Every_collection_on_the_player_boundary_is_genuinely_read_only` now walks real instances recursively
and checks runtime types, and `The_session_surface_exposes_no_developer_state` walks the type graph
transitively. The recursive version **caught something the author had not intended**: `PlayerClaim`
had a public `Matches(Claim)`, which put `Claim` back into the surface as a parameter. Made internal.

**Raw `Claim` and its `EventId`.** The DTO graph carried `Claim` directly, including
`Claim.EventId` — `WorldEvent.Id`, a monotonic counter over the truth log, which `REVIEW_LEDGER.md`
already treats as developer correlation data. There is no canon-supported reason for the counter:
`INFORMATION_AND_LEGIBILITY.md`'s "Player Intelligence Entry" makes *claims* player-facing, and the
counter is not part of a claim's meaning. So `PlayerClaim` carries the predicate and drops the
counter, and `Claim` is on the forbidden list.

The same counter was reaching the player as **text**, which ruling 8 deferred as cosmetic. The review
disagreed and is right — `answer:salvatore:PersonUsedViolence(tommy -> bellini-grocery#7)` was both
the option's id and, interpolated, its description. Both are fixed:

- `PendingOption.Id` is now an **opaque token**, a stable 48-bit hash of the candidate id.
  Deterministic, so an identical replay produces identical tokens; meaningless, so it reveals
  nothing. `SimulationSession.Choose` translates it back and then puts the candidate id to
  `Pipeline.Resolve`, **which remains the sole authority on whether an action was open to him** — the
  token map is an indirection in front of that question, never a second answer to it.
- `PendingOption.Description` is built by `PlayerOption` from the candidate's typed fields — kind,
  strategy, method, target, candour, and the claim a question or answer is about — with any claim
  going through `PlayerNarration.Describe`. Nothing a generator wrote reaches the player.

Ruling 8's reasoning was that a player-facing description vocabulary would be "a second
implementation to drift against the first". That was the wrong comparison: the developer description
and the player description are not two implementations of one thing, they are two different things,
and pretending otherwise is what let a truth-log counter reach a player. The side effect is that the
interface reads properly now — *have Tommy Nardo take it on*, not *have tommy handle SecureTribute(harbour, target=bellini-grocery, method=Force)*.

### Finding 3 — the self-test printed a failure and exited 0

`CE-SELFTEST FAILED` was a string in a log that no script could act on, which makes the check
decorative. It now exits 1, and so does an unhandled exception during the run. Verified by sabotage:
zeroing the loop bound makes the process exit 1.

### Verification of this correction

Measured after deleting every `bin`, `obj` and `.godot` directory.

- **Every accepted baseline is unchanged.** Trace hashes `6EB3F6B996CFC631` / `A8A1BBD12D5334C2` /
  `DCEDCFF27928266F` / `E164E0A74E2EC7DC` / `982EC77BD5C253CB`; chosen-action digests
  `38B7183ED2EEF34A` / `124E8FE932DD5A89` / `4F15ECD8B7A593BB` / `3D7F2B79BA4DC3E3` /
  `18B507EBBE4FBA7E`; decisions 38 / 21 / 39 / 39 / 38; conflicts 2 / 3 / 2 / 2 / 2; "rel. read"
  19 / 12 / 18 / 20 / 19; "rel. chose" 2 / 3 / 3 / 1 / 3. `--compare` still reports five distinct
  traces and five distinct chosen-action sequences.
- **All 30 viewpoint renders byte-identical to `901d345`**, which is what makes the `PlayerClaim`
  change a boundary change rather than a rendering one.
- Build 0 warnings, 0 errors across four projects. Tests **353 passed**, 0 failed (343 at `901d345`).
- Godot headless self-test exits 0, makes 4 choices, renders 4 decision screens; its UI text contains
  no `#`-suffixed counter, no decimal number, no scheduler wording (`held out against`, `finished:`,
  `handed him the harbour`), no raw entity id, and none of the fixture's hidden bakery fact.

### The lesson, which is the same one three milestones have now written down

**Ruling 7 was an enumeration presented as a structural guarantee.** It said "every cause is authored
from the waking character's own side" after checking the schedulers that existed, and that sentence
would have gone on being cited long after somebody added a scheduler that made it false. The
replacement does not enumerate anything: it names what may be said and stays silent about everything
else, so being wrong now requires somebody to add an entry rather than merely to add an event.

Carrying question, unchanged from milestone 008 and answered wrong here: **is this claim true of the
thing I am saying it about, or only of the instances of it I happened to look at?**
