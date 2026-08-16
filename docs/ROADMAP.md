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
scenario. The rest of the emergence prototype is not built, and the MVP has not begun. Milestone 002
was a framework migration, not a step along this sequence. Full accounts are in `docs/milestones/`.

006 established where the difficulty sat: the mechanisms worked and the scenario could not show them.
007 was the scenario-reach answer to that, and its own finding is the next thing to weigh — the edge
now reaches a score, and what it contributes there is about four hundredths of a point against
decision margins of the order of one. The constraint has moved from "can it be shown at all" to
"is it worth anything once it is".

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
- **The trust edge reaches a score and barely moves it, and this is now the most useful open number.**
  Two conflicts take Vincent's trust in Salvatore from 0.45 to 0.031; the `relationship effects`
  component on his next report to that boss moves from 0.0440 to 0.0063. Decision-relevant,
  emphatically not choice-changing. `Utility.Loyalty` weights trust at `0.45` and subtracts `0.5 ×`
  any grievance, so a standing grievance of 0.35 absorbs most of a full trust collapse before it
  reaches a score. Whether the answer is a larger `ConflictTrustCost`, different loyalty weights, or
  neither is a schema question — `OPEN_CONCERNS.md` #3.
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
- **`resentful-tommy` still makes the same decisions as baseline**, and this is now measured rather
  than assumed: `--compare` computes a chosen-action digest from structured decision fields and
  reports "5 distinct traces · 4 distinct chosen-action sequences", naming the convergence. Kept,
  untuned and un-recut. Its asymmetry becomes live only when a denial can win, which is the
  concealment-step item above.
- **Trust cannot go negative.** Absence of trust and distrust are the same state, so a stranger who
  contradicts you is indistinguishable from a stranger. A schema question for the design pass.
- The test project redundantly declares `TargetFramework` despite the centralized build property in
  `Directory.Build.props`. Carried since milestone 002.
- **The cast is six, and six is a ceiling rather than a trend.** `nunzio` was added by milestone 007
  against that milestone's own written "no new characters" exclusion, because `AdvanceTribute`
  resolves a demand through the owner's own decision and `Commit` finds a business by owner, so two
  shops need two owners. Codex found the breach and Matt accepted it as a bounded scenario-fixture
  exception on 2026-08-16, stating that it authorizes neither broader cast growth nor relaxed scope
  discipline. A seventh character needs its own ruling first.
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
- **Godot.** Godot 4 C# compatibility with `net10.0` is unverified. No Godot project exists, so
  nothing is broken today; if it turns out incompatible, multi-targeting or keeping the
  Godot-facing layer on a lower TFM stays available because the simulation library is
  engine-independent.
- **Generalized rumor propagation.** Explicitly excluded from milestone 003 and still out.
  `SourceKind.Rumor` remains in the vocabulary; no path produces it.
- Media and public-information channels, the case-board investigation model, prosecution, broader
  organizations, diplomacy, careers, corruption, and surveillance.
- Attribution on a corroborated belief credits only the first source; the full picture lives in
  testimony. A `SourceChain` is the eventual answer `INFORMATION_AND_LEGIBILITY.md` gestures at.

## Candidate scopes

Candidates only. They are not ordered by priority and must not be read as a queue — confirm scope
with Matt and write it into `CURRENT_MILESTONE.md` before changing simulation behaviour.

1. **Relationship design pass** — settle the relationship schema (`OPEN_CONCERNS.md` #3). Milestone
   006 supplied the first executable evidence it was always conditioned on; 007 supplied the second
   and more pointed kind — how far a trust movement actually carries into a score, and how much of it
   a standing grievance absorbs first. Not automatically next: whether to write it now or gather more
   evidence first is Matt's call.
2. **A denial that can win** — the concealment step that does not quiet its witnesses, and the global
   `believedWitnesses` scan. Together they are what keeps an executor from ever denying to his
   delegator, which is the one exchange in the model that milestone 004's provenance distinction was
   built for and that no accepted run has produced.
3. **Persistence / SQLite** — begin storing the information and decision data now worth querying.
4. **Godot / .NET compatibility spike** — cheaply settle an engine constraint before any UI work.
   Gates nothing today and its fallback is recorded above; worth a standalone commit rather than a
   milestone.
5. **Another bounded emergence slice** — rival activity or limited tier transitions, but not the
   whole remaining emergence prototype in one milestone. Weigh against candidate 1: another mechanism
   the scenario cannot exercise is volume, not progress.

Provenance precision was a candidate and became milestone 004, which is closed. RNG keying and the
concealment runaway were a candidate and became milestone 005, which is closed. The relationship
design pass was candidate 1 and became milestone 006 in its executable form — the schema document it
was originally framed as remains unwritten and is candidate 1 above. Scenario reach was candidate 1
and became milestone 007. "A scenario variant that contradicts a delegator's first-hand account" was
candidate 5, was attempted inside milestone 006 and did not succeed, and is now **half** achieved:
milestone 007 makes the delegator ask and the executor answer, in play, but the executor answers
honestly. What remains of it is candidate 2 above.
