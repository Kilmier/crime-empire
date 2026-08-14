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
emergence prototype, 004 made its provenance precise, and 006 closed the loop's return edge by giving
a perceived account conflict a social consequence. The rest of the emergence prototype is not built,
and the MVP has not begun. Milestone 002 was a framework migration, not a step along this sequence.
Full accounts are in `docs/milestones/`.

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
- **Tuning guesses.** The `FirstHandTestimony` suspicion discount of `0.15`, the `Discovery`
  discount of `0.10`, and milestone 006's `Relations.ConflictTrustCost` of `0.35` are not derived
  figures, and nothing yet distinguishes them behaviourally from neighbouring values.
- **The scenario is the binding constraint, and this is now the most important item on this list.**
  Three consecutive milestones have ended with a correct, mutation-checked mechanism the accepted
  scenario cannot demonstrate: 004's provenance distinction, 005's concealment termination, and 006's
  trust edge — which fires in every variant and moves Salvatore's trust in Vincent from 0.50 to 0.309,
  yet changes no decision because Salvatore never afterwards scores anything that reads that
  relationship. One organisation, five people and a single line of causation is running out of room.
  See `milestones/006-relational-consequence.md`.
- ~~**A delegator never receives an account from his own executor.**~~ **Corrected and partly
  addressed 2026-08-14.** The original claim was wrong: Tommy volunteers three Partial reports to
  Vincent. What never happens is a *contradiction*, because withholding asserts nothing. The
  redirect-to-the-asker behaviour is real but applies only to answers, not to volunteered reports.
  Milestone 006's correction added `Generators.FromDelegation`, so a delegator can now put a question
  to the man he sent, and the end-to-end path from that question to a trust consequence is proven.
- **The delegator's question never wins in the accepted scenario**, at 0.74 against 0.96. Two causes:
  Tommy would conceal rather than deny even if asked, because he believes he was seen — which is the
  model working — and the report that beats it is over-valued, which is not. See the next item.
- **Self-protection is re-priced for a concealment already decided.** `Reporting.LastAddressed`
  treats a withheld claim as settled for eligibility, but `Utility` still pays the full `+1.50`
  self-protection bonus for withholding it again on every later report. Same shape as the
  repeated-partial-report bug milestone 003 fixed, with the scoring half left undone. It is what
  keeps "report while hiding the same thing" winning indefinitely, and it is the single thing
  standing between the delegator's question and the scenario exercising it. **Deliberately not fixed
  inside a corrective pass**; it needs a ruling and would move every baseline.
- **`resentful-tommy` makes the same decisions as baseline.** It differs only in seeded state that
  reaches the trace summary, so `--compare`'s "five distinct histories" is a weaker signal than it
  reads. Kept because its directional asymmetry becomes live once the item above is addressed.
- **Trust cannot go negative.** Absence of trust and distrust are the same state, so a stranger who
  contradicts you is indistinguishable from a stranger. A schema question for the design pass.
- The test project redundantly declares `TargetFramework` despite the centralized build property in
  `Directory.Build.props`. Carried since milestone 002.

## Not yet implemented

- **Persistence.** SQLite is selected (`DESIGN_DECISIONS.md` §Stack) but not implemented. Save/load
  is absent.
- **Relevance tiering.** Active / Supporting / Background promotion and demotion are designed in
  `SIMULATION_ARCHITECTURE.md` and not implemented. The five-character cast makes this a non-issue
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

1. **Scenario reach** — give the fixture enough room to demonstrate the mechanisms already built,
   rather than adding another one it cannot show. Milestone 006's central finding argues this is now
   the highest-value scope; the delegator-to-executor question path in the debt list above is the
   smallest concrete instance of it.
2. **Relationship design pass** — settle the relationship schema (`OPEN_CONCERNS.md` #3). Milestone
   006 supplied the first executable evidence it was always conditioned on, and that item now records
   what the kernel showed and what a document would still have to decide. Not automatically next:
   whether to write it now or gather more evidence first is Matt's call.
3. **Persistence / SQLite** — begin storing the information and decision data now worth querying.
4. **Godot / .NET compatibility spike** — cheaply settle an engine constraint before any UI work.
   Gates nothing today and its fallback is recorded above; worth a standalone commit rather than a
   milestone.
5. **Another bounded emergence slice** — rival activity or limited tier transitions, but not the
   whole remaining emergence prototype in one milestone. Weigh against candidate 1: another mechanism
   the scenario cannot exercise is volume, not progress.

Provenance precision was a candidate and became milestone 004, which is closed. RNG keying and the
concealment runaway were a candidate and became milestone 005, which is closed. "A scenario variant
that contradicts a delegator's first-hand account" was candidate 5 and was attempted as part of
milestone 006 — it **did not succeed**, for the structural reason recorded in the debt list above,
and the underlying gap is still open.
