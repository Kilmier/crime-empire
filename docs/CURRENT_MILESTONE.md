# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `CLAUDE.md`'s
"Milestone lifecycle" section, and this file is reset for the next one.

## Status

**Milestone 003 — information transmission slice. Complete, awaiting Codex review.**

Archived at `docs/milestones/003-information-transmission.md`. 17/17 tests pass; all four variants
are deterministic. Nothing new should start until review lands.

The record of what was attempted follows.

Assigned by Matt as a deliberately narrow slice of `SIMULATION_ARCHITECTURE.md`'s emergence
prototype (step 2 of the Validation Sequence), which as written bundles six subsystems. This
milestone takes only the information half, and only part of that.

## Scope

In scope, as assigned:

- direct observation;
- one explicit report/message channel;
- one deceptive or incomplete report;
- one conflicting source;
- a player-readable history constrained to the viewpoint character's information.

**Explicitly not in scope: generalized rumor propagation.** Also out: media/public coverage, the
case-board investigation model, tier transitions, and relationship-schema work
(`OPEN_CONCERNS.md` #3, still open and still blocking "richer relationships and grievances").

This scope is near-verbatim `INFORMATION_AND_LEGIBILITY.md`'s own "Pre-MVP Kernel Scope", and that
doc's worked test scenario is the harbour scenario milestone 001 already built.

## Decisions taken at planning time

- **Viewpoint character: Salvatore**, matching the canon test scenario — he sets the policy,
  receives the reports, and must judge what happened without seeing the decision trace.
- **Conflicting source: Tommy's report against Vincent's**, both over the same single org report
  channel, so the channel count stays at one.
- **Deception is a scored outcome, not a script** — candid/partial/false become candidates scored
  by the existing utility pipeline, because traits must never fire actions directly.
- **Conflict is retained additively** — an append-only testimony log alongside the existing settled
  belief per claim, so no existing decision changes what it reads.
- `Pipeline.SuperiorOf` returns the *lowest* authority above an actor, so Tommy reports to Vincent,
  not Salvatore. Resolved by giving Salvatore a `SeekCorroboration` action, which
  `INFORMATION_AND_LEGIBILITY.md` explicitly sanctions ("Leaders can request audits, seek
  corroboration, cultivate independent sources").

## Carried over, untouched

From milestone 002: the redundant `TargetFramework` override in
`CrimeEmpire.Simulation.Tests.csproj`, and unverified Godot/`net10.0` compatibility.
