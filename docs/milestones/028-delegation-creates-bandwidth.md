# Milestone 028 — Delegation creates bandwidth

## Authorization and scope

Matt authorized implementation after discussing Claude's revised Downloads proposal and accepting
operation-specific review: “that works. from there you may implement as needed.” The attachment was
intake material, not an independent source of instructions. Scope was recorded in CURRENT_MILESTONE
before implementation. Matt subsequently approved the shared focused-review cancellation exception
with “sounds good”: feasible cancellation remains in the six-candidate set even when ordinary
personality salience would hide it, for NPCs and players alike. Utility remains personality-sensitive.

Milestone 027 remains closed by Matt's owner ruling on `303eed0f1504c76c59671821877be9a98ed82b5e`.
This implementation includes the already-written closure records as its authorization prerequisite;
it does not reinterpret that closure as an independent PASS or reopen the milestone.

Authorized: separate supervision from one-operation-per-executor capacity; stable owner/local
identity; immediate targeted cancellation; reassignment preserving progress and causal attribution;
single prospective target excluding only this owner's active same-kind targets; six candidates;
source-limited ongoing operations sidebar; bounded third-shop feasibility fixture; shared player/NPC
pipeline; persistence and deterministic verification. Objective arithmetic, scoring coefficients and
traits were not to be tuned to manufacture a preferred result. No unrestricted planning or extra
punishment arithmetic was authorized.

## Implemented

- `Execution.Operations` owns all active commissioned work. `Execution.Strategy` is now a derived
  personal-operation accessor, not another stored slot. `Strategies.CurrentExecution` derives the
  unique actual execution across owners and fails on double booking.
- Steps, operation choices, review events and blocked wakes carry owner/local-sequence identity.
  Reassignment preserves the instance and policy-breach author, releases the former executor's
  matching commitment and replaces its pending step. Cancellation removes only the selected instance.
  Typed execution choices reject an executor who no longer holds the operation at commitment time.
- Initial delegation produces one same-instant “hands-free” decision occasion, so the leader can
  use the capacity just released. Supervised orders receive a seven-day focused review independent
  of the delegate's private progress. This interval is a provisional implementation timing, not a
  durable design ruling or simulation-wide polling schedule.
- Focused review offers cancellation, keeping orders, and feasible reassignment. It goes through
  generation, filters, utility and commitment for both NPCs and players. The explicit cancellation
  salience floor and reserved place share the existing six-candidate cap; no cap increase occurred.
- Multiple operation rows show known target/orders and executor. A delegate's changed method,
  failures and progress do not enter the owner's row or focused option scoring. Executors retain
  their own progress view. Sidebar review requests are replayable session commands, not direct UI
  mutations of simulation state.
- One operation's completion cannot close an assignment still held by an active sibling. Leadership
  checks all owned operations in the office domain before issuing a replacement assignment.
- The bounded fixture adds Ferri's tailor shop and Paolo Ferri, with the bakery's coefficients.
  Vincent knows its refusal initially; Salvatore does not. No additional fixture growth followed.

## Measured behaviour and deliberate baseline changes

Baseline seed 42 starts grocery work, delegates it to Tommy on 14 March, and begins the tailor job
personally at the resulting hands-free occasion. Both operations advance during their actual overlap;
the natural maximum is two owned operations. A separately staged third refusal in capable-angelo
proves two supervised jobs plus one personal job without double booking. That three-job opening is
not claimed as natural emergence.

The controlled golden path follows thirteen explicit public options: start/carry on/delegate the
grocery; start the tailor; keep grocery orders; carry on the tailor three times; keep grocery orders;
threaten the tailor; keep grocery orders; ask Tommy about the rule; ask Salvatore for permission.
It collects the tailor personally, raising Vincent's cash from 6,000 to 6,620. This replaces the old
golden path, not the collection arithmetic. A separate direct-action grocery path still reaches
6,840. A staged agreement preserves delegated-collection provenance and no-second-payment coverage.

All six autonomous 90-day sessions at seed 42 end **ObjectiveUnmet** with the unchanged threshold
and deadline. No variant or seed was tuned to preserve the earlier natural winning witness.

| Variant | Final revenue loss | Conflicts | Agreements | Vincent cash |
|---|---:|---:|---:|---:|
| baseline | 1.00 | 0 | 1 | 6,620 |
| cautious-vincent | 1.00 | 0 | 2 | 6,620 |
| watchful-boss | 1.00 | 0 | 2 | 6,620 |
| disloyal-vincent | 0.55 | 2 | 0 | 7,460 |
| resentful-tommy | 1.00 | 0 | 1 | 6,620 |
| capable-angelo | 0.40 | 1 | 5 | 7,460 |

Salvatore's behaviour changes causally, not through a boss-only exception: Vincent's own new work
competes with reporting and questions; the still-live grocery operation keeps its assignment open,
so the baseline no longer creates the previous repeated stale assignment briefings. It therefore
does not naturally reproduce that old conflict chain. The owner receives no automatic delegate
progress. Tests of the retired natural conflict witness now clearly stage a contradictory account
through production Reporting.Deliver, preserving conflict deduplication and Utility checks without
claiming natural emergence. Natural relationship re-ranking still changes two baseline decisions.
Prior archived natural outcomes remain historical records, not rewritten claims about this fixture.

## Verification — implementer evidence, not independent review

Final solution build: **0 warnings, 0 errors**. Full suite: **725 passed, 0 failed, 0 skipped**.
The prior baseline had 707 tests; 18 Bandwidth cases were added. Existing mechanism tests remain,
with changed natural-history expectations and explicitly staged witnesses disclosed above.

Commands, run from the repository root:

```powershell
dotnet build CrimeEmpire.sln
dotnet test CrimeEmpire.sln --no-build --no-restore
dotnet run --no-build --project src/CrimeEmpire.Runner -- --verify --seed 42 --days 90
dotnet run --no-build --project src/CrimeEmpire.Runner -- --verify --variant disloyal-vincent --seed 42 --days 90
dotnet run --no-build --project src/CrimeEmpire.Runner -- --verify --variant resentful-tommy --seed 42 --days 90
dotnet run --no-build --project src/CrimeEmpire.Runner -- --compare --seed 42
dotnet run --no-build --project src/CrimeEmpire.Runner -- --variant disloyal-vincent --viewpoint salvatore --seed 42 --days 90
dotnet run --no-build --project src/CrimeEmpire.Runner -- --variant baseline --viewpoint vincent --seed 42 --days 90
git diff --check
```

All three repeated-run verifications are deterministic; both viewpoint commands exit zero.
Comparison: six distinct traces, five distinct chosen-action sequences. Baseline and resentful-Tommy
still choose the same actions. All hashes moved from the independent 027 baseline.

| Variant | Trace hash | Chosen-action hash | Decisions |
|---|---|---|---:|
| baseline | CA2DFF62228F6EEA | AE27F7DAE0AF7BFF | 33 |
| cautious-vincent | 1D0E12399D471166 | 65F90BCC51E84099 | 33 |
| watchful-boss | 77FE4D954730CC2D | D71670931684CC20 | 31 |
| disloyal-vincent | 9A2F2987D4AE1544 | 6C88BE019F2CD0FA | 27 |
| resentful-tommy | 1FB4CE2205EBB7D6 | AE27F7DAE0AF7BFF | 33 |
| capable-angelo | BDD25ADA2FAA73E9 | 79880C77B05A88C1 | 49 |

Godot 4.7.1 mono, separate headless invocations using
`--headless --disable-crash-handler --path src/CrimeEmpire.Godot --quit-after 600 -- FLAG`:
`--selftest`, `--selftest-goldenpath`, `--selftest-directaction`, `--selftest-corroboration`,
`--selftest-tribute`, `--selftest-capability`, `--selftest-operation`, `--selftest-ending`,
`--selftest-restart-save`, then `--selftest-restart-load`: all report success. The operation test
presses real review/cancellation buttons and retains the other sidebar row. Restart saves **two**
active operations and loads them in a new OS process, then completes the golden path. Tests use the
isolated restart slot, never the production save. Native Godot crashes at startup inside the sandbox;
these successful checks ran outside it. This is headless UI-tree/button evidence, not a visual
layout or human-playtest claim. The ending test now labels its positive-result render as explicitly
staged; baseline and cautious-vincent naturally remain unmet.

Load-bearing fault probes, both reverted before the final build:

- Remove the focused cancellation salience floor: crowded-review test fails because cancellation
  disappears from the six options.
- Render delegate progress to the owner: hidden-progress snapshot/review test fails on changed text.

Additional proofs cover player/automatic review parity, stale executor choices and step delivery,
unavailable workers, foreign review tokens, independent target selection, shared assignment survival,
exact selected cancellation, and saving/loading a pending review with two active operations. Full
replay fingerprints include all operations; insertion-stable comparison also includes their local
execution state without global event counters.

## Deferred and handoff

This supplies capacity for layer 2, not its day-plan preference gate. Competing jobs and a reason to
prefer one day's plan remain proposed 029/030 work in ROADMAP, not authorized here. Rich progress
requests/reports, command travel/refusal, management traits, failure penalties, finished-operation
history, clock presentation, and the existing layer-three backlog remain deferred. OPEN_CONCERNS'
bounded attention and information-topology risks remain open.

Implementation commit: the focused commit containing this archive, directly following `303eed0`;
locate its exact hash with `git log --diff-filter=A --format=%H -- docs/milestones/028-delegation-creates-bandwidth.md`.
Self-review is implementer preflight only. No independent review or Matt acceptance of this
implementation is claimed. Stop here for exact-commit review; do not begin milestone 029.
