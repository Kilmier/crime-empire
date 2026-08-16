# Crime Empire — Roadmap

What is not built, what is known to be wrong or unfinished, and what could plausibly come next.

**Nothing here authorizes anything.** This file is read when scope is being selected or proposed,
not before ordinary work. The assigned scope lives in `CURRENT_MILESTONE.md` and nowhere else;
neither the debt list nor the candidate list below is a licence to begin. Settled decisions are in
`DESIGN_DECISIONS.md`, unresolved design risks in `OPEN_CONCERNS.md`, and review status in
`REVIEW_LEDGER.md`.

## Where the project stands

`SIMULATION_ARCHITECTURE.md`'s validation sequence is: simulation kernel → emergence prototype →
MVP vertical slice.

Milestone 001 proved the kernel. Milestone 003 built the first narrow information slice of the
emergence prototype, 004 made its provenance precise, and 006 — **closed and accepted 2026-08-15** —
gave a perceived account conflict a social consequence, which is the loop's return edge. Milestone
007 — **closed and accepted 2026-08-16** — made that edge reach a later decision in the accepted
scenario. Milestone 008 — **closed and accepted 2026-08-16** — settled the reader side of the
relationship schema and built the instrument that measures it. The rest of the emergence prototype is
not built, and the MVP has not begun. Milestone 002 was a framework migration, not a step along this
sequence. Full accounts are in `docs/milestones/`.

Milestone 009 — **implemented 2026-08-16, reviewed and rejected three times, corrected three times,
and not accepted** — is the first step off that sequence rather than along it: a Godot playable shell
over the same kernel, with a person answering one character's decisions. The shell changed no
simulation behaviour. **Its second correction did**, deliberately: `cautious-vincent`'s baseline moved
when corroboration targets were restricted to people the actor has heard of. The other four variants
are unmoved. Current figures are in `REVIEW_LEDGER.md`; the account is in
`milestones/009-godot-playable-shell.md`, Corrections 1–3.

006 established where the difficulty sat: the mechanisms worked and the scenario could not show them.
007 was the scenario-reach answer to that. 008 then answered 007's own finding, and the answer
reframed it: the trust-to-partial-report path really is worth about four hundredths of a point, but
that is the weakest path in the channel rather than the channel. Removing relationship state changes
which candidate wins at 1–3 decisions in every variant. The constraint has moved again — from "is it
worth anything once it is shown" to **which readers are worth strengthening, and on what evidence**.

## Known technical debt

- ~~**RNG keying.**~~ and ~~**`ConcealIncident` runaway.**~~ **Retired 2026-08-14, resolved by
  milestone 005.** Occasion keys are now built from causally local strategy-instance identity —
  `(owner, local sequence, advance ordinal, trace kind, observer)` — never from `ScheduledEvent.Id`,
  `WorldEvent.Id`, or a `Claim.EventId`, so an unrelated scheduling change can no longer re-roll
  anyone's perception. `ConcealIncident` has an explicit, tested termination rule enforced in
  `Filters`, corrected during review to key off the incident itself rather than the target so a
  genuinely different incident at the same location stays eligible. Full account, including the
  correction, in `milestones/005-stable-occasion-identity-and-strategy-lifecycle-safety.md`.
  **Separately, and not retired by the above:** the one-attempt concealment rule itself remains only
  an MVP placeholder, not a permanent design — see that archive's deferred work and
  `CURRENT_MILESTONE.md`'s carried-forward items. Retiring the keying defect and the runaway does not
  retire that provisional concern.
- **Tuning guesses.** The `FirstHandTestimony` suspicion discount of `0.15` and the `Discovery`
  discount of `0.10` are not derived figures, and nothing yet distinguishes them behaviourally from
  neighbouring values. `Relations.ConflictTrustCost` of `0.35` now has one measured consequence and it
  is a small one — see the item below.
- ~~**The scenario is the binding constraint.**~~ **Addressed by milestone 007, closed and accepted
  2026-08-16.** A second contested business keeps the organisational shortfall
  alive past the first collection, which produces a second assignment briefing; that briefing
  contradicts the capo, and the trust it costs is read by a decision he takes afterwards. The
  delegator's question, competitive since 006 and never chosen, now wins in play in four of five
  variants. See `milestones/007-scenario-reach.md`.
- ~~**The trust edge reaches a score and barely moves it.**~~ **Answered by milestone 008, and the
  question was wrong.** The figures stand — two conflicts take Vincent's trust from 0.45 to 0.031 and
  the net relationship contribution on his next partial report to that boss moves from 0.0440 to
  0.0063 — but they measure the weakest path in the channel, not the channel. Removing relationship
  state changes which candidate wins at **1–3 decisions in every variant**. The attenuation on that
  particular candidate is structural: `+0.7 × loyalty` for the standing a report buys and
  `−0.5 × loyalty` for what an omission costs net to `0.2 × loyalty`. And grievance was being clamped
  away, not merely outweighing trust. Both are now visible and separately measured. See
  `milestones/008-relationship-readers.md` and `docs/RELATIONSHIPS.md`.
- **Concealment does not quiet the witnesses it is named for.** `AdvanceConceal`'s first step is
  "quiet the witnesses" and moves only `LegalExposure`; the concealer's own belief that he was seen is
  untouched. `Utility` prices a denial almost entirely on that belief, so this is what stands between
  the executor answering his delegator — which now happens — and an executor *denying* to him, which
  still does not. Surfaced by milestone 007 and deliberately outside it.
- **`believedWitnesses` is scanned globally.** `Utility` maxes over every `WitnessSawIncident` the
  actor holds, regardless of which incident he would be concealing — the same defect shape as the
  `SeekCorroboration` scan `404b416` fixed, and the same load-bearing category in `REVIEW_LEDGER.md`.
  It changes nothing in the current scenario, which is why milestone 007 excluded it rather than
  folding a behaviour-neutral fix into a pass that already moved every baseline.
- **The bakery is never collected from.** Nobody in the organisation knows it is refusing — deliberate,
  and the asymmetry that leaves the capo room to think rather than a second errand — but it means a
  second collection cycle is present in the fixture and unexercised.
- ~~**A delegator never receives an account from his own executor.**~~ **Corrected and partly
  addressed 2026-08-14.** The original claim was wrong: Tommy volunteers three Partial reports to
  Vincent. What never happens is a *contradiction*, because withholding asserts nothing. The
  redirect-to-the-asker behaviour is real but applies only to answers, not to volunteered reports.
  Milestone 006's correction added `Generators.FromDelegation`, so a delegator can now put a question
  to the man he sent, and the end-to-end path from that question to a trust consequence is proven.
- ~~**The delegator's question never wins in the accepted scenario.**~~ and
  ~~**Self-protection is re-priced for a concealment already decided.**~~ **Retired by milestone 007,
  closed and accepted 2026-08-16.** Concealment is now worth only the
  protection a report newly buys, priced per `(sender, recipient, claim)` from message content, and
  the question wins in play in baseline, watchful-boss, disloyal-vincent and resentful-tommy —
  `cautious-vincent` has no delegation, so there is nobody to ask about. **Not retired by that:** the
  executor still answers rather than denies, for the concealment-step reason above.
- ~~**`resentful-tommy` still makes the same decisions as baseline.**~~ **Retired by milestone 008.**
  `--compare` now reports **5 distinct traces · 5 distinct chosen-action sequences**. At seed 42 Tommy
  conceals the incident himself on 9 April instead of reporting it to Vincent, because his grievance
  against Vincent is no longer clamped away and takes 0.21 out of what reporting to that particular
  man is worth. Nothing was tuned. **Recorded with its fragility:** the winning margin is 0.0279
  against ±0.05 per-candidate noise, and the divergence holds at seeds 42 and 31337 but not at 1, 7,
  99 or 2024.
- **Trust cannot go negative, and nothing raises it.** Absence of trust and distrust are the same
  state; separately, the only runtime path that moves trust is a perceived account conflict, which
  only lowers it, so a relationship can be damaged and never repaired. Both **deferred rather than
  retired** by milestone 008 — negative trust returns when a decision reads distrust differently from
  indifference. See `docs/RELATIONSHIPS.md`.
- **`GrievanceWeight` is unbounded**, and a cap was considered as milestone 008's remedy for grievance
  dominating loyalty and explicitly rejected in favour of unbundling the clamp. Open, not answered.
- **Obligation is read but never moves.** `Relations.Establish` is its only writer and that is
  scenario construction; it holds its seeded value for the whole of every run. Surfaced by writing
  `docs/RELATIONSHIPS.md`, not a defect introduced by it.
- ~~The test project redundantly declares `TargetFramework` despite the centralized build property in
  `Directory.Build.props`.~~ **Retired 2026-08-16 by milestone 009**, and not by deleting the line.
  `Directory.Build.props` no longer assigns a TFM at all — it publishes `CrimeEmpireHostTfm` and
  `CrimeEmpireEngineTfm` as named values, because assigning `TargetFramework` there and multi-targeting
  the simulation library are mutually exclusive. Every project now names the framework it wants, so
  the test project's declaration stopped being redundant rather than being tidied away. Carried since
  milestone 002.
- **The cast is six, and six is a ceiling rather than a trend.** `nunzio` was added by milestone 007
  against that milestone's own written "no new characters" exclusion, because `AdvanceTribute`
  resolves a demand through the owner's own decision and `Commit` finds a business by owner, so two
  shops need two owners. Codex found the breach and Matt accepted it as a bounded scenario-fixture
  exception on 2026-08-16, stating that it authorizes neither broader cast growth nor relaxed scope
  discipline. A seventh character needs its own ruling first.
- ~~**Candidate descriptions are developer-shaped, and the player now reads them.**~~ **Retired
  2026-08-16 by milestone 009's first correction, and the deferral was wrong.** The reasoning had been
  that a player-facing description vocabulary would be "a second implementation to drift against the
  first" — the wrong comparison, since the developer description and the player description are not
  two implementations of one thing but two different things. `PlayerOption` now builds the wording
  from the candidate's typed fields, so no generator string and no truth-log `EventId` reaches the
  player, and the interface reads properly besides.
- **The timing of a pause is itself observable, and the occasion fix does not close it.** The
  controlled character is woken when a delegated operation blocks or completes, so a player sees him
  stop on the day it happened even though the interface says nothing about why. Closing it would mean
  not waking him, which changes autonomous behaviour. Surfaced by milestone 009's first correction.
- ~~**`Generators.FromRelationship` draws a corroboration target from the whole organisation's
  membership.**~~ **Retired 2026-08-16 by milestone 009's second correction, and the deferral's
  reasoning was wrong.** It said "inert in a three-member outfit"; that was never measured and was
  false. In `cautious-vincent` there is no violence, so nothing ever names Tommy to Salvatore, and
  Salvatore was going to him for an account anyway. `Decision/Acquaintance.cs` is now the single
  derivation of who a character has heard of, `GeneratorContext.AcquaintedIds` carries it, and
  `PlayerView.KnownPeople` delegates to it instead of repeating it. `cautious-vincent`'s baseline
  moved and the new figures are in `REVIEW_LEDGER.md`.
- **Whether an outfit whose boss cannot name his own soldiers is the right model.** The line is drawn
  explicitly by milestone 009's third correction: a named post is knowledge — `Organization.Offices`
  and `BossId` — and a headcount is not. The model has held that position since milestone 003, when
  `IntelligenceWriter` recorded that "who else is in this outfit" is the kind of thing a boss can be
  wrong about, so following it was the conservative choice rather than a new decision. The consequence
  is visible: a soldier holding no office is unreachable for corroboration until somebody names him,
  which in `cautious-vincent` never happens for Tommy. Whether that is *correct* is unanswered.
- **The player cannot see why an option is unavailable.** Rejected candidates are hidden per milestone
  009's scope, which is right for utility scores and arguably wrong for "he does not know that the
  bakery is holding out" — the single most legible line in a decision trace, and the one that proves
  the simulation is belief-limited rather than merely claiming to be. Whether a *belief-stage*
  rejection is player-facing is a real design question and is unanswered.
- **`AGENTS.md` §Verification does not mention the Godot headless check**, and `docs/RELATIONSHIPS.md`
  is still absent from its conditional-reading list. Same shape as the older item: flagged, not taken,
  because no ruling authorized editing `AGENTS.md`. The commands milestone 009 added to the
  verification set are recorded in `REVIEW_LEDGER.md`'s baseline section instead.
- **Nothing prevents a future Godot script from calling `Cast.Build` and `Runner.Run` directly.** The
  session's own surface is closed — `World` is `internal` and a reflection test pins the public
  surface — but the library it lives in is not. A separate player-contract assembly would close it;
  milestone 009 did not have the scope for a fifth project.
- **The milestone lifecycle does not durably record rulings.** Written into `CURRENT_MILESTONE.md`
  before implementation and reset out of it by the archive-and-close commit, they survive only in the
  archive that reproduces them. Milestone 006 lost its set this way and milestone 007 nearly repeated
  it. Fixing it means changing `AGENTS.md`, which is Matt's call and has not been made.

## Not yet implemented

- **Persistence.** SQLite is selected (`DESIGN_DECISIONS.md` §Stack) but not implemented. Save/load
  is absent.
- **Relevance tiering.** Active / Supporting / Background promotion and demotion are designed in
  `SIMULATION_ARCHITECTURE.md` and not implemented. The six-character cast makes this a non-issue
  at present scale, which also means it is unvalidated.
- ~~**Godot.**~~ **Retired 2026-08-16 by milestone 009, and the fallback is what happened.** Godot
  4.7.1 hosts .NET 8, not .NET 10, so the simulation library multi-targets `net8.0;net10.0` and the
  Godot project is on `net8.0` while the runner and tests stay on `net10.0`. The library gained no
  Godot reference. `src/CrimeEmpire.Godot` builds as part of `CrimeEmpire.sln` and starts headlessly.
  **Not retired by that:** everything in the presentation list below is still absent — no map, no art
  pipeline, no animation, no save/load, and the interface is a deliberately plain functional layout.
- **Generalized rumor propagation.** Explicitly excluded from milestone 003 and still out.
  `SourceKind.Rumor` remains in the vocabulary; no path produces it.
- Media and public-information channels, the case-board investigation model, prosecution, broader
  organizations, diplomacy, careers, corruption, and surveillance.
- Attribution on a corroborated belief credits only the first source; the full picture lives in
  testimony. A `SourceChain` is the eventual answer `INFORMATION_AND_LEGIBILITY.md` gestures at.

## Candidate scopes

Candidates only. They are not ordered by priority and must not be read as a queue — confirm scope
with Matt and write it into `CURRENT_MILESTONE.md` before changing simulation behaviour.

1. ~~**Relationship design pass**~~ — **became milestone 008**, which narrowed `OPEN_CONCERNS.md` #3
   rather than retiring it. What is left of it is decay, negative trust, whether respect and
   resentment are separate dimensions or derived, whether provenance should weight the social
   consequence, and whether grievance should be capped — each now carrying a stated condition for
   when it becomes answerable, which is what makes them not-yet-candidates rather than open questions.
2. **A denial that can win** — the concealment step that does not quiet its witnesses, and the global
   `believedWitnesses` scan. Together they are what keeps an executor from ever denying to his
   delegator, which is the one exchange in the model that milestone 004's provenance distinction was
   built for and that no accepted run has produced.
3. **Persistence / SQLite** — begin storing the information and decision data now worth querying.
4. ~~**Godot / .NET compatibility spike**~~ — **subsumed by milestone 009**, which settled the
   constraint (the engine hosts .NET 8) and built the shell in the same pass rather than spiking it
   separately.
5. **Another bounded emergence slice** — rival activity or limited tier transitions, but not the
   whole remaining emergence prototype in one milestone. Another mechanism the scenario cannot
   exercise is volume, not progress.
6. **A runtime path that raises trust.** Surfaced by milestone 008 writing the schema down: conflicts
   lower trust and nothing restores it, so a relationship can be damaged and never repaired. Nobody
   decided that. Small, and it would give the trust dimension a second update path to be read
   against.

Provenance precision was a candidate and became milestone 004, which is closed. RNG keying and the
concealment runaway were a candidate and became milestone 005, which is closed. The relationship
design pass was candidate 1 and became milestone 006 in its executable form; the schema document it
was originally framed as became **milestone 008**, written from measured results rather than ahead of
them. Scenario reach was candidate 1 and became milestone 007. "A scenario variant that contradicts a delegator's first-hand account" was
candidate 5, was attempted inside milestone 006 and did not succeed, and is now **half** achieved:
milestone 007 makes the delegator ask and the executor answer, in play, but the executor answers
honestly. What remains of it is candidate 2 above.
