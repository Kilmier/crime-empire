# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
"Milestone lifecycle" section, and this file is reset for the next one.

This file is also the handoff surface between agents: status, scope in and out, decisions taken at
planning time, and what is deliberately carried over. There is no separate handoff document.

## Status

**No milestone is active.** `docs/milestones/003-information-transmission.md` — the information
transmission slice — is the most recently completed one, verified by Codex on 2026-08-13 against
`b8fe921` with no remaining findings.

Milestone 004 has not been scoped or assigned. **Confirm scope with Matt rather than inferring
it.** The candidates below are recorded so the choice is informed, not so it can be made
automatically.

## Where the project stands

- 63 tests passing; `--verify` deterministic on baseline and `disloyal-vincent`; four variants
  produce four distinct histories at 13/16/13/43 decisions and 2/2/2/6 reports.
- The decision pipeline (001), .NET 10 (002) and the information loop (003) are all in place and
  reviewed. Still no Godot project, no persistence, no save/load, no tiering.

## Candidate scopes for 004

Not ranked, and not a recommendation — the point of listing them is that each has a known reason
to be chosen or deferred.

1. **Provenance precision.** `SourceKind.Direct` cannot distinguish observing something, doing it,
   and being told first-hand by the person who did. Both the player-facing attribution wording and
   the confidence label are currently working *around* that rather than solving it, and two review
   findings landed on it. A `Participant`/`Witness` split is the real fix. Small, well-understood,
   and clears a known compromise.
2. **The rest of the emergence prototype** — richer relationships and grievances, delegation,
   rival activity, limited tier transitions. Note that "richer relationships" is still blocked by
   `OPEN_CONCERNS.md` #3 (relationships remain a shape, not a schema), so this one needs a design
   pass before it can be implementation work.
3. **Persistence / SQLite**, per the stack decision. Nothing depends on it yet, but every milestone
   so far has been in-memory only, and `DESIGN_DECISIONS.md` chose SQLite specifically for the
   explainability queries that the information model now actually produces data for.
4. **Godot / `net10.0` compatibility spike.** Carried unverified since milestone 002. Cheap to
   settle, and the answer constrains when engine work can start.

## Carried over, still open

- Provenance imprecision (candidate 1 above).
- `OPEN_CONCERNS.md` #3 — relationships have no data-model answer.
- `OPEN_CONCERNS.md` #4 is **stale**: it claims the trait vocabulary is not closed, but milestone
  001 closed it in `Domain/Psychology.cs`. Worth a docs pass to retire it.
- From milestone 002: the redundant `TargetFramework` override in
  `CrimeEmpire.Simulation.Tests.csproj`, and unverified Godot/`net10.0` compatibility.
