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

## Review of `3db0cb1` and focused correction — 2026-09-14

This appendix supersedes the initial implementation's claimed retirement of natural success bars;
the original account above remains unchanged as history.

### Review, authority and disposition

Claude reviewed exact `3db0cb1ffba07292f1c89ebc313ed46275276e24`, Class A, **FAIL**:
three P1 and two P2 findings. The supplied report was `REVIEW_3db0cb1.md`. He authored no runtime
source, but authored proposal material and the bundled 027 closure text, which he excluded from
review. Independence is therefore partial and disclosed, not a blanket independent PASS.
The four pre-existing dirty roadmap/proposal files were documentation-only and excluded from his
exact-source runtime checks. He reproduced build, 725 tests, three deterministic verifications,
comparison and both viewpoint commands. He did not run Godot or the implementer's fault probes.

Matt authorized investigation and a focused correction in this task. On review access he explicitly
ruled: **“Keep free player review; add information-triggered NPC reviews.”** That settles the timing
question: equivalent consequences and information restrictions, not identical thinking frequency.
027 remains closed by Matt's previous owner exception; Claude's request for another review of its
closure text does not reopen that gate.

Finding dispositions in this correction:

1. **P1 stranded delegated work — fixed at the executor boundary.** Resolving an auxiliary choice
   after a block reschedules the same still-active execution only when no live step exists.
   Existing continuation, escalation, postponement, reassignment and cancellation remain authoritative.
   Seeking approval now applies its existing five-day execution delay to the actual executor,
   personal or delegated. The absent supervisor receives no private progress or wake from this repair.
   Claude's proposed hidden-pending-step owner remedy and relaxed assignment gate were not adopted:
   neither is necessary, and both would weaken existing information/assignment constraints.
2. **P1 inverted milestone-007 bar — restored, not retired.** The unmodified baseline now must collect
   both opening shops, issue at least two assignments, exhibit natural repeated briefings and a
   natural account conflict, and show the trust cost changing a later available candidate's score.
   No synthetic account substitutes for those natural tests. The all-owned-operations assignment
   gate is retained: valid outstanding work still keeps its assignment open.
3. **P1 early review access — corrected under Matt's timing ruling.** All three receipt paths
   (report, assignment, delegation briefing) can bring a supervised-order review forward when a
   relevant receipt is news, a conflict, or an agreement. Successful observation acquisition can
   also bring it forward. Relevance compares communicated/acquired claim subject/object against
   the recipient's own order target or named executor. Withholding, repeated inert accounts,
   unrelated news, failed observation and private delegate progress do not trigger it.
   A missing operation target never matches a claim's missing object.
   The same scheduler and pipeline handle free player requests, received-information occasions,
   and the provisional seven-day reviews. This is not a new general-purpose trigger taxonomy.
4. **P2 objective reachability — measured and pinned.** All six autonomous seed-42 variants remain
   Unmet, honestly. A baseline seed-42 Vincent playthrough using twenty explicit public option texts
   collects all three shops and ends **ObjectiveMet**, revenue loss 0, with no staged truth,
   fixture alteration, coefficient change or deadline extension. The choices are pinned in
   `OperationContinuityTests.The_unchanged_objective_is_reachable_through_legal_named_player_choices`.
   This proves legal player reachability, not autonomous success. The Godot positive ending render
   remains explicitly staged; it is not presented as this playthrough's UI proof.
5. **P2 alphabetical prospective target — removed.** Single-target belief order and the own-active-
   target exclusion remain; the six-candidate limit is unchanged. The first target is now the tailor
   in every accepted seed-42 variant.

The strong initial tailor belief was a non-blocking note, not authority to retune the fixture.
It remains unchanged. Discovery is not testimony eligible for corroboration, but the claim that it
cannot be argued against overstates the model: `Cognition.Receive` can contest direct knowledge.
Bounded attention, information topology, and deferred tuning remain open concerns.

### Corrected natural history and assurance

At baseline seed 42 Vincent starts the tailor, delegates it on March 14, and starts the grocery at
the hands-free occasion. Both genuinely advance during overlap. Tommy collects the tailor on
March 26; Vincent delegates the grocery that day after his own attempts; Tommy collects it on
April 13. Vincent reports April 17. The May 4 second assignment reintroduces Salvatore's stale
grocery account to a Vincent who has now seen the money arrive: a real conflict returns.
Salvatore's changed questions/report dates therefore follow changed received information and
completed obligations, not a boss-specific scoring or knowledge exception.

The thirteen-button golden path now ends at cash 7,460 (620 + 840 received), with owner-observable
discovery provenance for delegated collection. Both persistence copies and Godot follow measured
public choices. The personal-action Godot fork still renders continuation and delegation together;
choosing continuation then threats collects the tailor personally for 620. Threats succeed, so this
proof no longer demands subsequent force against an already-paying shop.

Other historical dialogue proofs were re-established through live choices: the natural Salvatore
question uses baseline March 27 rather than the now-inapplicable cautious-variant timing; its exact-
claim answer is deliberately selected and survives replay. Autonomous reporting of TributeCollected
does not answer BusinessRefusesTribute. Agreement coverage uses legal choices to finish the tailor
personally, delegate the grocery, ask Salvatore, and choose his candid response; no beliefs or
requests are injected. That is controlled-choice coverage, not fully autonomous emergence.
The existing staged investigation now produces multiple distinct incidents in some variants;
tests check each request's actual suspect and each answer's exact claim rather than falsely require
one incident. The earlier staged mechanism tests remain labelled as staged.

| Variant | Revenue loss | Conflicts | Agreements | Assignments | Vincent cash |
|---|---:|---:|---:|---:|---:|
| baseline | 0.55 | 1 | 4 | 2 | 7,460 |
| cautious-vincent | 0.50 | 1 | 2 | 2 | 7,460 |
| watchful-boss | 0.55 | 1 | 2 | 2 | 7,460 |
| disloyal-vincent | 0.55 | 2 | 0 | 2 | 7,460 |
| resentful-tommy | 0.55 | 1 | 4 | 2 | 7,460 |
| capable-angelo | 0.55 | 1 | 2 | 2 | 7,460 |

### Correction verification — implementer preflight, not independent review

Final full solution build: **0 warnings, 0 errors**. Full solution tests:
**751 passed, 0 failed, 0 skipped**. Commands:

```powershell
dotnet build CrimeEmpire.sln --verbosity quiet
dotnet test CrimeEmpire.sln --no-build --no-restore --verbosity quiet
dotnet run --no-build --project src/CrimeEmpire.Runner -- --verify --variant baseline --seed 42 --days 90
dotnet run --no-build --project src/CrimeEmpire.Runner -- --verify --variant disloyal-vincent --seed 42 --days 90
dotnet run --no-build --project src/CrimeEmpire.Runner -- --verify --variant resentful-tommy --seed 42 --days 90
dotnet run --no-build --project src/CrimeEmpire.Runner -- --compare --seed 42
dotnet run --no-build --project src/CrimeEmpire.Runner -- --variant disloyal-vincent --viewpoint salvatore --seed 42 --days 90
dotnet run --no-build --project src/CrimeEmpire.Runner -- --variant baseline --viewpoint vincent --seed 42 --days 90
git diff --check
```

Repeated verifications deterministic; both viewpoint commands exit zero. Comparison:
six distinct traces, five distinct action sequences. These hashes are correction measurements, not
independently reviewed baselines.

| Variant | Trace hash | Chosen-action hash | Decisions |
|---|---|---|---:|
| baseline | 94831B526FCA5761 | 2930C6612450A449 | 36 |
| cautious-vincent | A796B6CDA38EDBBF | B0DFFBEAEBD8F6F3 | 23 |
| watchful-boss | 78523E6CE5904631 | 7A9ACC3C59970142 | 28 |
| disloyal-vincent | 9A2F2987D4AE1544 | 6C88BE019F2CD0FA | 27 |
| resentful-tommy | 6B634CBCA3EACD2D | 2930C6612450A449 | 36 |
| capable-angelo | 0083E53CFF84A342 | A955423F0063E292 | 36 |

Godot 4.7.1 mono: all ten headless flags listed in the original verification section were rerun after
the final runtime edit and passed, including separate-process restart-save then restart-load.
The review/cancellation test preserves the personal sibling. Only the isolated self-test save slot
was used. This is actual scene-tree/button verification, not visual-layout or human-playtest evidence.

Fault probes, all reverted:

- Disable executor continuity: the staged real available corroboration choice strands the operation
  and `Asking_elsewhere_after_a_block_preserves_work_without_informing_the_absent_owner` fails.
- Remove report-receipt early review: both controlled/uncontrolled early-review cases and the genuine
  report-generated save/replay case fail (three failures).
- Restore alphabetical prospective selection: belief-order regression test fails.
- Remove the conflict trust cost: the natural conflict-to-later-score test fails.

Important limit: after restoring belief order, the natural block-continuity test alone still passes
with the fallback disabled; that changed natural route does not exercise the auxiliary-choice bug.
The staged **available-choice** probe is the discriminating check. No claim is made that reverting
the assignment gate catches this correction: the gate was deliberately not changed.

Additional coverage proves identical receipt-generated review instants/state across controlled and
autonomous resolution, silence for private/unreceived information, no wake for repeated accounts,
replay of a real pending early review, and equal explicit approval delays for actual executors.
No new persistent state or replay command kind was introduced.

The focused correction is the commit containing this appendix, directly after `3db0cb1`.
Independent verification and Matt's acceptance remain pending. No 029 implementation is authorized.
