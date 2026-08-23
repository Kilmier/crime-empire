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

---

## Correction from Codex's review of `9537b38`, 2026-08-23

Appended, not folded in. The account above is preserved as originally written and is **superseded by
this section** wherever it describes the four items below — none of the rest of the milestone's
account (the persistence architecture, the Godot integration, the schema/build compatibility design,
the two-process restart proof itself) is affected.

**Codex reviewed `9537b38` and returned four findings, two P1 and two P2, all about the strength of
this milestone's own verification rather than the persistence mechanism it verifies** — the save/load
architecture, the restart proof, and every baseline hash are unchanged by this correction.

**P1 — the exact-internal-replay-state proof checked a hand-picked list, not the state.**
`AssertExactInternalIdentity` compared only `TraceWriter.Render` output, `World.Queue.Count`, five
named `World` counters, and each active character's `StrategyInstance` fields — a list assembled at
planning time, not derived from the actual shape of `SimulationSession`/`World`. It could not have
caught a corrupted `_prepared`, a swapped `_optionIds` mapping (the actual data
`SimulationSession.Choose` trusts to translate a pressed button into a candidate id), an altered
queued event, or a moved `EventQueue._nextId` — all real state a wrong replay could plausibly get
wrong while every checked figure still matched.

**Fix.** `PersistenceTests.cs` gained `DeepFingerprint`, a test-only (never production) reflective
walker that reaches every field — public and private, recursively — from a root object: dictionaries
and `PriorityQueue<TElement,TPriority>` are unwrapped through their own public logical-content surface
(`IDictionary`, `UnorderedItems`) rather than their backing arrays, which can carry stale slots and
excess capacity unrelated to logical content; every other reference type falls through to raw field
reflection, which is what reaches `EventQueue`'s private `_queue`, `_cancelled`, and `_nextId`, and
every one of `World`'s private identifier counters, without naming any of them by hand.
`AssertExactInternalIdentity` now deep-fingerprints `World` in full (in addition to keeping
`TraceWriter.Render` as a second, independent instrument) and separately deep-fingerprints
`SimulationSession`'s private `_prepared` and `_optionIds` fields, which live on the session rather
than `World` and so are not reached by the `World` walk at all.

**Mutation-checked both ways ruling asked for, confirmed and reverted:**
- Swapped two `_optionIds` token→candidate mappings on a loaded session without touching the public
  `PendingDecision.Options` it points at. Both identity tests still passed on the public projection
  (confirmed identical before failing); `AssertExactInternalIdentity` failed specifically on the
  `_optionIds` comparison — `Collections differ`, position 3, the two swapped values — proving that
  check, not the `World` fingerprint, is what catches this class of defect.
- Incremented `EventQueue`'s private `_nextId` by one on a loaded session without changing
  `Queue.Count`. `AssertExactInternalIdentity` failed on the deep `World` fingerprint at the exact
  `_nextId` field (`"_nextId", 16` vs `"_nextId", 17`), confirming the walk reaches `EventQueue`'s
  internals through nothing more than generic field reflection.

Both mutations were reverted before this commit; neither is retained as a test (mutation checks in
this project are a verification activity performed and recorded, not permanent code — see e.g.
milestone 014's own corrections).

**P1 — the Godot restart self-test wrote to the production save slot.** `--selftest-restart-save`
deleted and overwrote `user://crime-empire-save.db` directly — the exact file a real player's own save
lives in. Running the self-test suite on a machine with a real save at that path would have destroyed
it.

**Fix.** `Game.cs` now resolves `_activeSavePath` once, in `_Ready`, before any button exists: a real
launch resolves it to `ProductionSavePath` (the actual `user://crime-empire-save.db`, unchanged);
launching with `--selftest-restart-save` or `--selftest-restart-load` resolves it to
`SelfTestRestartSavePath` (`user://crime-empire-selftest-restart-save.db`) instead. Every Save/Load
button handler reads `_activeSavePath`, never either constant directly, so the self-test flags press
the exact same production button-handler code a player uses while structurally never being able to
reach the player's own file. `ProductionSavePath` also gained a `CE_SAVE_PATH_OVERRIDE` environment
variable override, unset (and therefore inert) in every real launch, added solely so the negative
check below could exist without ever touching a real save file to prove it. `RestartLoadSelfTest`
(process B) now deletes its own self-test fixture — the save and any stray `.tmp` sibling — in a
`finally` block covering both its success and its thrown-exception paths.

**Verified directly, twice.** First, empirically, against the real production file: a real save
existed at `user://crime-empire-save.db` from earlier manual verification
(`sha256: 35937d3b...78e59`); both restart self-test flags were run in sequence; the production file's
hash was unchanged afterward, and the self-test's own fixture file was gone (cleaned up by process B).
Second, the negative check ruling 7 asked for, without touching that real file at all: a throwaway
fixture file was written with known dummy bytes, `CE_SAVE_PATH_OVERRIDE` was pointed at it, both
restart flags were run (using their own, unrelated, isolated self-test slot throughout), and the
fixture's hash was confirmed byte-for-byte unchanged afterward — `sha256: 04a700ec...50102` before and
after in the final recorded run.

**P2 — the loaded-snapshot cash-boundary test checked only the numeric case.** It mutated Marco's
`Capabilities.Cash` and then checked only `values.OfType<double>()` for the sentinel, so a leak that
reached the player as text — the exact shape milestone 014's own correction (`ff4213a`) already found
once, leaking into `PlayerAttitude.Standing` — would not have been caught here even though the
`ValueGraph` walk it runs already collects every string too.

**Fix.** The test now also converts the sentinel to its invariant-culture string form and asserts it
is not a substring of any string the walk reaches, matching the discriminating shape `ff4213a`
established. Mutation-checked directly: `PlayerSnapshot.cs`'s attitude construction was temporarily
changed to append the sentinel's text onto every rendered `Standing` line; this test failed
specifically on the new string-membership assertion (`Sub-string found`, `"he takes him as he finds
him 555444"`), confirming the added check — not the pre-existing numeric one — is what catches this
class of leak. Reverted before this commit; `PlayerSnapshot.cs` is otherwise unchanged.

**P2 — the interrupted-write proof locked a file before the write began, rather than interrupting one
in progress.** The original test opened `path.tmp` with `FileShare.None` and then called `Save`, so
the write it exercised failed at its very first attempt to open the file. That is a real failure mode,
but not the one ruling 7 names — "failed/interrupted writes" — and it is a materially easier case:
`SaveStore.Write` never got to create anything at all.

**Fix.** A new project, `tests/CrimeEmpire.Persistence.InterruptedWriteHarness` (a small,
single-purpose console app, not a general framework), is spawned by the replacement test as a
genuinely separate OS process. `SaveStore` gained one test-only synchronization seam,
`internal static Action? OnTransactionOpenedForTest`, invoked once inside `WriteDatabase` immediately
after the `.tmp` database's schema is created (auto-commit, already durable) and its meta/commands
transaction is opened — null, and therefore a no-op, in every real save; only the harness ever sets
it. The harness sets the hook to signal a named, manual-reset `EventWaitHandle` and then block forever
(`Thread.Sleep(Timeout.Infinite)`), so the parent test's synchronization is explicit rather than a
timing guess: it waits on the named event, and only once the event fires — proving the harness has
genuinely opened a real write transaction on the real `.tmp` file — does it call
`Process.Kill(entireProcessTree: true)`. The new test,
`A_genuinely_interrupted_writer_process_leaves_the_previous_valid_save_loadable`, confirms a `.tmp`
artifact exists (proving the interruption landed where intended) and that the real save path's bytes
and rendered trace are unchanged.

**Mutation-checked twice, for two different properties:**
- Changed `WriteDatabase`'s target from `tmpPath` to `path` (removing the tmp-file indirection
  entirely). The test failed — not inside the interruption logic, but at the earlier
  `first.Save(path)` baseline call, with `File.Move` throwing `FileNotFoundException` because
  `tmpPath` was never created — confirming this mutation breaks writing in general, as expected, and
  ruling out that this specific mutation could pass silently.
- Reverted that, then mutated `Write`'s startup cleanup from `TryDelete(tmpPath)` to `TryDelete(path)`
  — corrupting the guarantee under test directly, by having every write attempt delete the
  *previous* valid save up front rather than only a stale `.tmp` leftover. The test failed, as
  intended: the harness was killed as designed, but the real save path no longer existed
  (`FileNotFoundException` reading `afterBytes`), proving the test does notice when an interrupted
  write is allowed to reach the real path.

Both mutations were reverted before this commit and the full suite re-confirmed green.

**What this correction is not.** No change to the persistence architecture, the SQLite schema, the
Godot UI beyond the save-path indirection above, or any simulation behaviour. Tests: **482 passed, 0
failed**, same count as `9537b38` (one test was replaced, not added, for finding 4; the rest are
strengthened in place). Two new files:
`tests/CrimeEmpire.Persistence.InterruptedWriteHarness/CrimeEmpire.Persistence.InterruptedWriteHarness.csproj`
and its `Program.cs`.

**Full verification re-run from a clean tree:** build 0 warnings/0 errors across **six** projects (the
harness project is new); 482/482 tests; `--verify` deterministic and byte-identical on `baseline`
(`FEE45FD886F18CA8`), `disloyal-vincent` (`45CCF5ADC6EC0302`), `resentful-tommy` (`F5BD93386DE04082`)
— all unmoved from `ff4213a`; `--compare` byte-identical across all five trace hashes and
chosen-action digests; both required viewpoint runs exit 0; Godot `--selftest` and
`--selftest-goldenpath` both unchanged; the two-process restart proof re-run in full (process A saves
to the isolated slot after delegation, process B loads it and reaches `1 April 1987`,
`cash on hand 6,840`); the production save slot confirmed byte-identical before and after both restart
flags, both empirically against a real file and via the isolated `CE_SAVE_PATH_OVERRIDE` fixture
proof; `git diff --check` clean (only pre-existing CRLF-normalization notices, no whitespace errors).

**Recurring-failure list, walked.** *A hand-picked list standing in for a completeness claim:* the
original identity check named the fields planning-time reasoning expected to matter, which is exactly
the shape of gap a reviewer without the author's assumptions is positioned to find — and did. *A test
proving less than its name claims:* both the file-lock write test and the numeric-only cash check
were real, passing checks whose specific mechanism did not match the specific claim in their own name
or doc comment — the same failure pattern milestone 014's corrections found twice in its own
mutation-check shapes. *A production file at risk from a verification step:* the restart self-test's
use of the real save path is the kind of defect that would not show up in `dotnet test` at all (which
never loads `CrimeEmpire.Godot`), and was only visible to a reviewer reading the Godot-side commands
this milestone's own archive documented running.

**Status.** This correction is implemented, tested, and mutation-checked as described above. It is
**not accepted**, and milestone 015 as a whole remains **not accepted** — Matt's confirmation of this
named commit is what that requires. The account of milestone 015 above is not rewritten to read as
though it were correct from the start; this section is the record of what was wrong and what changed.

---

## Second correction from Codex's review of `af7d34f`, 2026-08-23

Appended, not folded in. The two corrections above are preserved as originally written. **This
section withdraws the first correction's closing claim that all four of Codex's `9537b38` findings
were fixed — three were; the exact-internal-replay-state P1 was not fully fixed, and this section is
that fix.** The other three items the first correction addressed (the isolated Godot self-test slot,
the textual cash-leak check, the genuinely interrupted writer process) are unaffected and not
redesigned here.

**Codex reviewed `af7d34f` and found the P1 residual: `AssertExactInternalIdentity`'s complete-`World`
fingerprint, added by the first correction, still left `SimulationSession` and `PersistentSession`
themselves checked by a second hand-picked list — `_controlledId` (which determines whose decisions
pause), `ViewpointCharacterId` (which determines the player projection), `Seed`, `StartedOn`, and
`PersistentSession._log` (what a later save would replay) were not checked at all; the complete
`_pending` record was checked through four named properties (`At`, `ActorId`, `Occasion`, `Focus`,
`Options`) rather than all seven — `ActorName`, `ActorRole`, and `ActorPronouns` were silently absent.
The exact failure shape ruling 7's "not a hand-picked list" instruction was written to rule out,
found again one level up from where the first correction fixed it.**

**Fix.** `AssertExactInternalIdentity` now calls `DeepFingerprint` on the complete
`PersistentSession` — `original` and `loaded` themselves, not `SimulationSession.World` beneath
them — as its sole completeness mechanism. Because `DeepFingerprint` already walks every field,
public and private, recursively, pointing it at the wrapper root rather than one field within it
requires no new mechanism: `PersistentSession._log`, `SimulationSession._controlledId`,
`ViewpointCharacterId`'s and `Seed`'s and `StartedOn`'s auto-property backing fields, `_prepared`,
the complete `_pending` record (all seven properties, via the same generic field reflection that
already reaches every property on every other record it encounters), `_optionIds`, `_clock`, and
`_runUntil` are all reached as a consequence of walking `PersistentSession`'s own two fields
(`_session`, `_log`) outward — not because any of them is named in the test. The five previously
separate hand-picked checks (`_prepared`, `_optionIds`, clock, fast-forward, and the four-property
`Pending` comparison) were removed rather than kept alongside the fingerprint, since keeping them
would have re-created exactly the "second list" shape this correction exists to close.
`TraceWriter.Render` is kept, per the review's instruction, as a second, independently-built
instrument; `Status` and `Date` are asserted first purely so a failure reads as "paused vs. running"
or "wrong date" before the much larger fingerprint diff — both are already implied by the
fingerprint (`Status` is a pure function of `_pending`'s nullness, `Date` of the fingerprinted
`_clock`), so neither is claimed as part of the completeness proof.

**Mutation-checked both ways requested, confirmed and reverted:**
- Reflectively set the loaded session's private `_controlledId` field to `"salvatore"` (from
  `"vincent"`) without touching `World`. `TraceWriter.Render` on both sessions' `World` was confirmed
  identical first, then `AssertExactInternalIdentity` failed on the fingerprint comparison
  specifically — the diff isolated to `"_controlledId", "vincent"` vs. `"_controlledId",
  "salvatore"` at the exact position in both fingerprints, nothing else differing.
- Reflectively removed the last entry from the loaded `PersistentSession`'s private `_log` field
  without touching its current `World`. `TraceWriter.Render` was again confirmed identical first,
  then `AssertExactInternalIdentity` failed on the fingerprint comparison — the diff showing the
  original's fingerprint carrying one more `Choose` command (with its `Kind`/`Days`/`OptionToken`
  backing fields) than the mutated loaded session's, proving a later save from the mutated session
  would silently replay a shorter, wrong history.

Both mutations were reverted before this commit and the full suite re-confirmed green. Neither
mutation is retained as a test, matching this project's standing mutation-check practice.

**What this correction is not.** No change to the persistence architecture, the Godot self-test
isolation, the cash-boundary check, or the interrupted-write harness — all three stand as the first
correction left them. No simulation behaviour changed. Tests: **482 passed, 0 failed**, unchanged in
count (the fix simplified `AssertExactInternalIdentity` and removed the now-unused `PrivateField`
reflection helper it no longer needed; no test was added or removed).

**Full verification re-run from a clean tree:** build 0 warnings/0 errors across six projects; 482/482
tests, including all 15 focused persistence tests; `--verify` deterministic and byte-identical on
`baseline` (`FEE45FD886F18CA8`), `disloyal-vincent` (`45CCF5ADC6EC0302`), `resentful-tommy`
(`F5BD93386DE04082`) — all unmoved from `ff4213a`; `--compare` byte-identical across all five trace
hashes and chosen-action digests; both required viewpoint runs exit 0; Godot `--selftest` and
`--selftest-goldenpath` both unchanged; the two-process restart proof re-run in full on the isolated
self-test slot, reaching `1 April 1987`, `cash on hand 6,840`; the production save slot's hash
confirmed unchanged before and after (`35937d3b...78e59` both times), and the self-test slot confirmed
absent both before the run and after process B's own cleanup, matching its state before any of this
correction's verification began; `git diff --check` clean.

**Recurring-failure list, walked.** *A hand-picked list standing in for a completeness claim, twice
in the same mechanism:* the first correction replaced one hand-picked list (a handful of `World`
counters and strategy fields) with a deep fingerprint, and in the same commit introduced a second one
one level up (the session/wrapper fields checked individually) — the fix this time was recognising
that the *root* the fingerprint starts from, not the fingerprint mechanism itself, was the thing still
hand-picked. *A fix that narrows scope just enough to look complete:* fingerprinting `World`
addressed everything Codex's first review named by name, which made the remaining gap easy to miss
without independently asking "what does `PersistentSession` itself carry that `World` does not."

**Status.** This second correction is implemented, tested, and mutation-checked as described above.
Milestone 015 remains **not accepted** — Matt's confirmation of this named commit is what that
requires. Neither this section nor either correction above rewrites the original account to read as
though it were correct from the start.
