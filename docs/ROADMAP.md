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
emergence prototype, and 004 made its provenance precise. The rest of the emergence prototype is not
built, and the MVP has not begun. Milestone 002 was a framework migration, not a step along this
sequence. Full accounts are in `docs/milestones/`.

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
  neighbouring values.
- **The scenario does not exercise milestone 004's central distinction.** No variant contradicts a
  delegator's first-hand account, so the difference between authored participation and being told is
  provable in unit tests and invisible in play. A variant where Tommy denies to Vincent that he
  touched the place would exercise it.
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

1. **Relationship design pass** — settle the relationship schema (`OPEN_CONCERNS.md` #3) before
   implementing richer relationships and grievances. Likely the next substantial design pass.
2. **Persistence / SQLite** — begin storing the information and decision data now worth querying.
3. **Godot / .NET compatibility spike** — cheaply settle an engine constraint before any UI work.
4. **Another bounded emergence slice** — delegation, rival activity, or limited tier transitions,
   but not the whole remaining emergence prototype in one milestone.
5. **A scenario variant that contradicts a delegator's first-hand account** — makes milestone 004's
   distinction visible in play rather than only in unit tests.

Provenance precision was a candidate and became milestone 004, which is closed. RNG keying and the
concealment runaway were a candidate and became milestone 005, which is closed.
