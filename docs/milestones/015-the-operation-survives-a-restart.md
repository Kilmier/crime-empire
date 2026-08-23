# Milestone 015 — The Operation Survives a Restart

Authorized by Matt on 2026-08-23, in the same message that accepted correction commit `ff4213a` and
closed milestone 014. Ten rulings, quoted in full in this milestone's `docs/CURRENT_MILESTONE.md`
entry before implementation began; the milestone-scope-reviewer agent was run against the exact
proposal first, per ruling 1, and found no blocking contradiction — its one procedural finding
(milestone 014's acceptance not yet being recorded in canon) is what this archive and the
accompanying `REVIEW_LEDGER.md` update resolve, and its one open question (the two-process harness's
mechanism) is settled by ruling 5a, below.

## What this milestone is for

Every milestone through 014 proved its claims within one continuous process. Nothing in the design —
`SIMULATION_ARCHITECTURE.md`'s determinism and scheduling invariants, or the per-decision RNG seeding
`Sim/Rng.cs` already used — required that, but nothing had proven otherwise either. This milestone
proves the existing, unmodified baseline seed-42 Vincent `SecureTribute` operation against Bellini's
grocery — the same operation milestone 014 walked end to end — survives a genuine operating-system
process boundary: saved by one process, loaded and finished by a second one that shares no memory
with the first.

## Rulings taken at planning time

The complete ten rulings are recorded in this milestone's entry in the version of
`docs/CURRENT_MILESTONE.md` this commit replaces (visible in this commit's diff). In summary: (2)
replay-backed SQLite persistence, not `World` serialization; (3) a dedicated engine-neutral
`CrimeEmpire.Persistence` project, multi-targeted for both hosts, with `CrimeEmpire.Simulation`
staying free of SQLite/Godot/filesystem; (4) one fixed Godot save slot, minimal controls, no
picker/browser/autosave/cloud; (5) prove the existing baseline survives a genuine two-process
restart, split after delegation; (6) save and load work in both `Ready` and `AwaitingChoice`; (7) the
required test list, in full below; (8) same-build-only compatibility, no migrations; (9) the standing
exclusions (no `World` serialization, no new behaviour, no candidate ids or scores exposed, no scope
broadening); (10) baseline hashes stay byte-identical when persistence is unused. Two
implementation-time choices ruling 5 and ruling 8 explicitly left open were settled in
`CURRENT_MILESTONE.md` before implementation, not improvised during it: the two-process harness is
two new Godot headless self-test flags, verified manually like the two that already exist; and "same
build" means `CrimeEmpire.Simulation`'s own assembly `ModuleVersionId`.

## What was completed

**`src/CrimeEmpire.Persistence/` (new project).** Multi-targeted `net8.0;net10.0`, the same scheme
`CrimeEmpire.Simulation` has used since milestone 009, referencing only `CrimeEmpire.Simulation` and
`Microsoft.Data.Sqlite` (10.0.11 — the version that restored and built with zero warnings on both
target frameworks; 9.0.0 pulled a `SQLitePCLRaw.lib.e_sqlite3` version NuGet flags as a known high
severity vulnerability, which this project's zero-warnings standard does not tolerate).

- **`SessionCommand.cs`.** The three shapes a save's input log can hold — `StepEvent`, `AdvanceDays`,
  `Choose` — matching exactly the mutators `Godot/Game.cs` exposes. `Choose` carries the same opaque
  token `PendingOption.Id` already is; nothing here names a candidate id or score.
- **`SaveData.cs`, `SimulationBuild.cs`.** The save's whole payload — schema version, build id, seed,
  variant, controlled/viewpoint ids, the ordered command list — and the build-id fingerprint itself:
  `typeof(SimulationSession).Assembly.ManifestModule.ModuleVersionId`.
- **`SaveStore.cs`.** Reads and writes exactly one SQLite file at exactly one caller-supplied path.
  Two tables, `save_meta` (one row) and `save_commands` (ordinal, kind, arg). Writes are atomic by
  construction: a complete new save is built in a sibling `.tmp` file inside one transaction, and only
  once that file exists whole and closed is it moved onto the real path — a failure anywhere before
  that move leaves the real path untouched. Reads validate schema version, build id, and command
  ordinal contiguity, and throw `SaveFormatException` rather than substitute anything on any
  violation.
- **`Session/PersistentSession.cs`.** Wraps a `SimulationSession`, recording one `SessionCommand`
  after each of `StepEvent`/`AdvanceDays`/`Choose` returns normally (never before — every one of
  those calls throws before mutating anything on a bad input, so the log never records a step that
  would fail on replay for a reason replay itself introduced). `Save` writes the complete log;
  `Load` starts a genuinely fresh `SimulationSession` from the save's own seed and variant and
  replays every command back through the session's real public mutators, in order. The inner session
  is exposed only `internal`, to the test assembly — the same treatment
  `SimulationSession.World` already gets — so nothing beyond `PlayerSnapshot`/`PendingDecision`
  reaches any consumer of this type either.

**`Godot/Game.cs`.** `_session` is now a `PersistentSession`. The toolbar gained "Save" and "Load"
buttons, both enabled in either `SessionStatus` (ruling 6) unlike the three clock controls beside
them; the start screen gained a "Load saved game" button, disabled when no save exists. The one fixed
slot is `user://crime-empire-save.db`, globalized once via `ProjectSettings.GlobalizePath`. A status
line reports the last save/load outcome, including a caught `SaveFormatException`'s message, rather
than crashing the shell on a bad save. `SevenChoiceSequence` (the golden path's seven pinned option
texts) was lifted out of `GoldenPathSelfTest` into a shared field, and the pressing loop into a shared
`PressChoicesInOrder` helper, so `--selftest-goldenpath` and the two new restart flags below all
press from the same pinned sequence rather than three independently-typed copies of it — verified
behaviour-preserving by re-running `--selftest-goldenpath` before and after and confirming identical
output.

**Two new Godot headless self-test flags, per ruling 5a.**

- `--selftest-restart-save`: starts the baseline seed-42 session as Vincent, asserts the opening
  screen reads `6,000`, presses the golden path's first three choices (start/persuade, carry on,
  delegate to Tommy) through real buttons, presses the real "Save" button, confirms the status line
  reads "saved" and the file exists, and exits.
- `--selftest-restart-load`: asserts a save exists, builds the start screen and presses the real
  "Load saved game" button, presses the remaining four choices (threaten, force, the reaffirmed
  carry-on when Kane's investigation interrupts, the report to Salvatore) through real buttons,
  confirms no eighth pause follows, and reads `6,840` off the live screen.

**`tests/CrimeEmpire.Simulation.Tests/PersistenceTests.cs`** (new, 15 tests): exact internal
replay-state identity at both a `Ready` and an `AwaitingChoice` save point (clock, outstanding
fast-forward, the full rendered developer trace, event queue size, every `World` identifier counter,
each active character's ongoing `StrategyInstance` fingerprint, and — at the awaiting-choice point —
the reproduced pause's date/actor/occasion/focus/options field for field); loaded-versus-uninterrupted
golden-path equivalence; deterministic repeated loads; counterfactual valid choices from one save
diverging naturally; the structural information-boundary walk (extended from milestone 009's, now
rooted at `PersistentSession`, with `SimulationSession` itself added to the forbidden-type list);
another character's cash never appearing in a loaded snapshot; six malformed-input cases (a
non-database file, a database with no save tables, an unrecognised command kind, a gapped command
ordinal, an option token nothing offers, a mismatched schema version, a mismatched build id), each
failing with `SaveFormatException` and none autoplaying or falling back; and a genuinely
OS-level-interrupted write (the `.tmp` path locked by another file handle) leaving the previous valid
save loadable.

**Verification.** Full clean-tree re-run: build 0 warnings/0 errors across five projects; 482/482
tests (467 before this milestone, 15 new in `PersistenceTests.cs`); `--verify` deterministic and
byte-identical on `baseline` (`FEE45FD886F18CA8`), `disloyal-vincent` (`45CCF5ADC6EC0302`),
`resentful-tommy` (`F5BD93386DE04082`) — all three unmoved from milestone 014's accepted baseline;
`--compare` byte-identical across all five trace hashes and chosen-action digests; both required
viewpoint runs exit 0; Godot `--selftest` (4 choices, 4 decision screens, exit 0) and
`--selftest-goldenpath` (seven choices, `6,000` → `6,840`, exit 0) both unchanged from milestone 014.
**The two-process restart proof itself, run for real:**

```
Godot_v4.7.1-stable_mono_win64_console.exe --headless --path src/CrimeEmpire.Godot -- --selftest-restart-save
  CE-RESTART-SAVE decision 1 on 1987-03-02 — pressing "talk Bellini's grocery round"
  CE-RESTART-SAVE decision 2 on 1987-03-11 — pressing "carry on getting Bellini's grocery to pay"
  CE-RESTART-SAVE decision 3 on 1987-03-14 — pressing "have Tommy Nardo take it on"
  CE-RESTART-SAVE saved to .../crime-empire-save.db on 1987-03-14
  CE-RESTART-SAVE ok

Godot_v4.7.1-stable_mono_win64_console.exe --headless --path src/CrimeEmpire.Godot -- --selftest-restart-load
  CE-RESTART-LOAD loaded at 1987-03-14, status=Ready
  CE-RESTART-LOAD decision 1 on 1987-03-17 — pressing "change tack with Bellini's grocery — threats instead"
  CE-RESTART-LOAD decision 2 on 1987-03-23 — pressing "change tack ... force instead ..."
  CE-RESTART-LOAD decision 3 on 1987-03-27 — pressing "carry on getting Bellini's grocery to pay"
  CE-RESTART-LOAD decision 4 on 1987-04-01 — pressing "report to Salvatore Greco, leaving out his own part"
  · cash on hand 6,840
  CE-RESTART-LOAD ok
```

Two genuinely separate OS processes (two fresh headless Godot invocations, no shared memory), reading
and writing the real fixed save slot a player would use. Both stop conditions in the authorizing
message were cleared rather than triggered: `Microsoft.Data.Sqlite` runs under Godot's .NET 8 headless
host (this is that run), and exact reconstruction was achieved through deterministic replay alone,
with `World` never serialized.

## Important discoveries

**Deterministic compilation makes the assembly `ModuleVersionId` a workable, zero-maintenance
same-build fingerprint.** Ruling 8 asked for a same-build check but left the identifier open. A
hand-maintained version string needs remembering to bump; a `git` revision id needs `git` present at
build time. `CrimeEmpire.Simulation`'s own compiled `ModuleVersionId` needs neither: identical source
and references reliably produce an identical MVID under the SDK's default deterministic build, and a
behavioural change reliably produces a different one. This was a genuine design choice, not a detail —
recorded in `CURRENT_MILESTONE.md` before implementation per this project's own standing practice for
open implementation-time questions.

**A locked `.tmp` file is a real, not simulated, interrupted-write fault.** The obvious way to test
"failed writes leave the previous save loadable" is a test-only hook that throws partway through a
write. `SaveStore`'s atomic-swap design made a better test possible instead: opening the `.tmp` path
with `FileShare.None` before calling `Save` produces a genuine OS-level sharing violation, exercising
the real failure path — the actual `SqliteException` a locked file produces, not a synthetic
substitute for it — with no test-only code in the production type at all.

**Sharing `SevenChoiceSequence` across three self-tests found nothing wrong, but the alternative was
worse.** Splitting the pinned sequence for the two new restart flags without also sharing it with
`GoldenPathSelfTest` would have left three independently-typed copies of the same seven strings in one
file — exactly the shape of thing that drifts silently. The refactor was verified behaviour-preserving
before being trusted: `--selftest-goldenpath`'s output was captured before and after and found
byte-identical.

## Deferred work

Everything carried into milestone 014, unresolved by this milestone: the 124 live-edge findings and 5
apparently-dead lines catalogued in `docs/COVERAGE_ACCOUNTING.md`; systematic mutation automation and
seed-sweep promotion; the allegation option naming the same person twice; the developer trace's
uniform "he"; nobody holding a scored relationship with Kane; the tuning guesses; the cast ceiling of
six; obligation read but never moved. This milestone adds, explicitly excluded rather than
accidentally omitted: slot management, autosave, cloud save, a save-browser UI (ruling 4); and
cross-build save migrations (ruling 8). `ROADMAP.md` candidate 3's original framing — decision data
worth querying — remains open; this milestone executed the storage-technology half of
`DESIGN_DECISIONS.md` §Stack's persistence entry, not the querying-capability half. See
`docs/DESIGN_DECISIONS.md` §Stack and `docs/ROADMAP.md` for both corrected accordingly.

## Where to look and what to distrust

The claim most expensive if wrong is "replay reconstructs exact internal state" — if
`PersistentSession.Load` silently diverged from the original session in some field the tests don't
check, later play could drift in a way nothing would explain. `AssertExactInternalIdentity` in
`PersistenceTests.cs` was mutation-checked directly: truncating the replayed command list by one and
re-running the two identity tests failed both, on `Status` mismatches (`Ready` vs `AwaitingChoice` and
the reverse) rather than passing coincidentally. The schema-version and build-id checks in
`SaveStore.Read` were likewise mutation-checked by disabling each and confirming the corresponding
test failed with "No exception was thrown" before reverting. The information-boundary walk was
mutation-checked by temporarily making `PersistentSession.InnerSession` public and confirming the
walk caught it by name (`PersistentSession.get_InnerSession exposes SimulationSession`) before
reverting. A reviewer should re-run these four mutations rather than take this paragraph on faith —
each is a small, local, single-line change and reverts cleanly.

The two-process restart proof (ruling 5, the natural-run claim this whole milestone is for) is a
manual verification, not part of `dotnet test` — see ruling 5a's reasoning, and re-run the two exact
commands above to reproduce it directly; both are cheap and deterministic.

## Commit

One implementation-and-archive commit, per `AGENTS.md`'s milestone lifecycle. Status is not
established by this file — `docs/CURRENT_MILESTONE.md` says what is active, and Matt's confirmation
of this named commit is what acceptance requires. Milestone 014's acceptance of `ff4213a` is recorded
in this same commit's `docs/CURRENT_MILESTONE.md` and `docs/REVIEW_LEDGER.md` updates, per Matt's own
instruction not to spend a commit solely recording it.
