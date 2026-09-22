# Milestone 032 — One Operation Becomes a Scene

## Authorized scope recorded at implementation start

**Milestone 032 — One Operation Becomes a Scene is authorized for implementation by Matt on
2026-09-22.** Matt accepted the independent confirmation review's AUTHORIZE recommendation and
accepted P2-1 and P2-2 as resolved. This records his ruling, not reconstructed review evidence. Astra's
independent `review-new-system` pass returned REVISE with two P2 findings and no P1 findings. Matt
accepted both corrections and the recommended belief-history ruling on 2026-09-22. This revision
keeps independent belief revisions out of the bounded chronicle and restores the repository's archive-
before-review lifecycle.

Matt previously reported reviewing exact Milestone 031 closure commit `2ce33e0` and authorized this
draft. Matt reaffirmed his acceptance of `2ce33e0` in the implementation authorization. This file
does not reconstruct an independent exact-commit verdict, reviewer evidence, or ledger entry.

Implement only the revised scope below, verify it, and archive it in one focused implementation
commit. Independent exact-commit review and Matt's human comprehension/playtest acceptance remain
pending. Do not push or begin Milestone 033.

This milestone is the first bounded step toward **Personal Demo v0.1 / Harbor Demo**, an internal
playable validation build. It does not satisfy, replace, or rename the canonical MVP / First Playable
defined in `GAME_VISION.md`.

## Feature intent

A player who commissions one existing `SecureTribute` operation should experience a coherent,
source-limited situation instead of reconstructing it from unrelated state rows.

In the smallest natural scene, Vincent commissions Bellini's grocery or Ferri's tailor shop, chooses
an existing approach, and assigns either himself or a known eligible subordinate. While the existing
operation advances, one prominent situation card explains the present player-visible situation and a
small chronicle preserves the source-bearing parts of the history Vincent could actually recount.

After a neutral direct or delegated run, Matt should be able to answer without the developer trace:

1. What did Vincent order, by what method, and who was to handle it?
2. What happened or was reported from Vincent's viewpoint?
3. How did Vincent learn each reported fact?
4. What remains unknown?
5. What changed — including money received or the order ending?

The simulation remains the author of actions, outcomes, knowledge, and consequences. This milestone
only gives existing structured meaning a deterministic, readable presentation.

## Proposed rendering boundary

All new player-visible semantic data must cross the existing `PlayerView.Build` boundary and be
frozen into `PlayerSnapshot` values before any renderer sees it. The situation card, chronicle, and
Godot UI may consume only the snapshot and the already source-safe `PendingDecision` contract. They
must not query `World`, mutable domain objects, developer records, or another actor's private state.

The structured presentation contract may contain only what the viewpoint can legitimately know:

- stable entry identity and entry kind;
- receipt or decision time;
- known event time only when the source actually supplies it;
- the real operation identity `(OwnerId, LocalSequence)` where a durable source supplies it;
- subject or target;
- speaker/source and the account's asserted stance, qualitative uncertainty, and claimed basis;
- an owner-visible consequence such as an itemized cash receipt;
- explicit uncertainty, including `reason unknown` and `no report yet`.

The contract must not contain report candor, withheld claims, actual basis, hidden truth, utility
scores, rejected options, another actor's decisions or cognition, unobserved progress, or an inferred
reason for silence or delay. The renderer may choose deterministic wording for a contract value; it
may not invent actions, speech acts, claims, emotions, witnesses, relationships, or consequences.
Rendered prose is neither authoritative nor persisted.

## Permitted chronicle entries and durable sources

If Matt accepts this scope, Milestone 032 authorizes only the entry families below. The chronicle is
a projection of named durable records, not a new omniscient event log.

| Entry | Durable production source | Fields permitted across `PlayerView.Build` | Stable identity |
|---|---|---|---|
| Commissioned or personally revised order | The viewpoint actor's own `DecisionRecord`, restricted to `ActorId`, `Id`, `At`, the chosen `Candidate`, chosen initial executor, and a production-written operation link | Existing action kind, `SecureTribute`, target, selected method, selected executor, and operation identity when the production commit supplies it. No trigger, agenda, beliefs used, scores, rejected candidates, outcome text, reconsideration, or salience notes | Decision id plus entry kind |
| Received operation account | The recipient's own `ExecutionState.OperationAccounts` | Operation identity, sender, receipt time, claim, asserted stance, qualitative uncertainty derived from asserted confidence, and claimed basis. The account is preserved as received even if cognition later changes | Source report id plus asserted-claim ordinal, or an equivalent collision-proof typed id retained at delivery |
| Itemized collection income | The viewpoint actor's own `Capabilities.CashReceipts` | Receipt time, amount, source business, executor, and the collection operation identity | Operation identity plus entry kind; it must not be guessed from target and time |
| Secure-tribute order ended | Minimal owner-owned completion retention, written by the existing production completion path | Operation identity, target, executor, completion time, and only an owner-known result: `money arrived` or `outcome unknown`. It must not copy the developer-only completion reason or a delegate's private progress | Operation identity plus entry kind |

The first row includes `StartStrategy`, `ContinueStrategy`, `AlterStrategy`, `PostponeStrategy`,
`DelegateStrategy`, and `AbandonStrategy` only when the chosen candidate itself carries the needed
typed fields. A missing operation link must remain absent; matching by target, timestamp, wording, or
proximity is forbidden.

An accepted `StartStrategy` candidate does not currently retain the operation owner/local-sequence
identity created by `Commit.StartStrategy`. This scope permits one typed operation link to be written
by that production commit and retained with the viewpoint actor's own committed decision (or an
equivalently narrow owner-owned order record). It does not permit `PlayerView.Build` to reconstruct
the link from target, time, `StrategyCount`, list position, or the operation that happens to be live.

The received-account row requires the smallest extension to the existing `OperationAccount` record:
retain collision-proof source identity and the recipient-visible asserted confidence and claimed
basis already delivered by the report. It must not retain or project `Report.Candor`, `Withheld`, or
`ReportedClaim.ActualBasis`. `PlayerView.Build` must convert the retained confidence into the
project's qualitative vocabulary; the raw scalar does not cross into the presentation contract.

An independent `Cognition.Revise` without a new account changes only the existing current-knowledge
view. It creates no Milestone 032 chronicle entry. A later contradiction or corroboration appends only
when a new source-bearing account is actually received. In either case, the original received account
remains unchanged. A history of independent belief revisions requires a separately authorized future
scope.

The income row requires the smallest extension to the existing `CashReceipt`: attach the real
operation owner/local-sequence identity at the production collection write. Do not infer it later
from business, executor, date, or whichever operation happens to be active.

The completion row permits one narrowly typed, append-only owner record for `SecureTribute` only.
For delegated work, the mere fact that the owner's order ended is known but its private reason is not;
money arrival may be named because it is separately owner-visible. Do not preserve the `why` string,
create a general operation-event history, or broaden this record to other strategy families.

`PlayerChronicleEntry` (or an equivalently named snapshot value) is presentation data assembled only
inside `PlayerView.Build`. It must not become a new source used by cognition, scoring, candidate
generation, operation resolution, reporting, or any other simulation behavior.

## Situation card

The situation card is present-tense orientation, not a second history. It may combine:

- the source-safe `PendingDecision.Occasion` and `Focus` already built for the controlled actor;
- the controlled actor's active `PlayerOperation` values;
- the most recent relevant permitted chronicle entry; and
- explicit absence such as `No report about this operation yet` or `The reason is unknown`.

For a personal refusal, the existing production `StrategyBlocked` occasion may say that the shop
turned Vincent down because he was the executor in the room. The same block during delegated work is
silent to Vincent until a legitimate account reaches him. A known concession may be presented from
the receipt/closure records when the money arrives. A delay without a source remains unexplained.
The personal refusal itself is intentionally not a retained chronicle family: after the current card
passes, later payment may remain in history while the earlier face-to-face refusal does not. This
milestone must not claim to provide a complete transcript of the encounter.

The detailed knowledge, operation, relationship, and decision views remain drill-down evidence. The
situation card summarizes those safe projections; it does not override them or create a second truth.

## Existing behavior that must remain unchanged

- Existing `SecureTribute` candidate generation, salience, scoring, methods, outcomes, probabilities,
  operation timing, payment amount, policy consequences, and completion rules.
- The shared player/NPC preparation, executor evaluation, commit, scheduling, and resolution paths.
- Milestone 024's executor capability, delegated-progress privacy, operation identity, owner/executor
  authority, and owner-observable-income rules.
- Milestone 027's inclusive terminal deadline.
- Milestone 029's grouped job/method attention and candidate presentation.
- Milestone 030's informed-choice and delegated-review boundaries.
- Milestone 031's paused, replayable commissioning stages, shared executor evaluation, direct initial
  commissioning, and save/replay behavior.
- Current information acquisition, cognition, testimony, reporting, and belief-revision semantics.
- Existing deterministic hashes and action histories. A change to either is a finding requiring
  review, not a baseline update hidden inside this presentation milestone.

## Explicitly out of scope

- New dialogue choices, character voices, random prose, or stylistic variation.
- Bellini family claims, deception, Glanton protection, rival organizations, or new narrative events.
- New actions, outcomes, reports, claims, operation types, characters, traits, relationships, or
  simulation decisions.
- A generic narrative engine, scripting language, event store, chronicle framework, or stored prose.
- Reading the truth log, raw report log, report candor/withheld/actual basis, another actor's decision
  record, cognition, operation progress, or private outcome.
- New owner interventions, live forecasts, delayed commands, questions about progress, or reporting
  vocabulary.
- Force-chain presentation or regression work reserved for Milestone 033.
- Continuous clock controls reserved for Milestone 034.
- The Harbor command screen, schematic board, map, art, portraits, or animation reserved for
  Milestone 035.
- Any claim that OPEN_CONCERNS #8 is resolved. This milestone directly tests that concern but closes
  it only if a later human ruling says so.

## Production-path proof and falsifiers

Tests must use the ordinary public session and production writers. A hand-staged snapshot or record
is useful only as a unit test and cannot satisfy the milestone's causal proof.

1. **Natural direct route.** Through public choices, Vincent commissions and personally carries one
   collection. The situation card and chronicle identify his order, current known state, and visible
   consequence without reading developer truth.
2. **Natural delegated route.** Through public choices, Vincent commissions Tommy. The order is
   visible immediately; private progress and refusal remain absent until a production report/account
   or owner-visible receipt supplies them.
3. **Shared actor behavior.** Exercise the new production retention through both controlled and
   autonomous resolution. The same writers and identity rules must apply; a mutation that conditions
   retention on player control must fail.
4. **All-channel negative control.** Hold authoritative events constant while withholding every
   legitimate path to Vincent. The corresponding card fact and chronicle account must disappear.
   Preserve a separate legitimate channel as a positive control and the information must remain.
5. **Append, never rewrite.** Deliver a later contradictory or corroborating account. The original
   account keeps its original source, stance, confidence, claimed basis, wording semantics, and
   receipt time; the later account becomes a separate entry. Separately, revise the recipient's belief
   twice without another report, skip intermediate refreshes, and restart: the current knowledge view
   must show the resulting belief, the original account must remain unchanged, and no revision entry
   may appear.
6. **Collision controls.** Two operations against the same target and two reports delivered at the
   same instant retain separate identities, deterministic order, and correct operation attachment.
   A target/time heuristic must fail this test.
7. **Writer and reader mutation checks.** Break the production attribution writer and separately
   break the projection reader. Each mutation must fail a load-bearing test; hand-populating correct
   identities in tests cannot substitute for production attribution.
8. **Renderer cannot simulate.** Change templates or phrasing while holding snapshot values constant.
   World fingerprints, decisions, action signatures, cash, relationships, and terminal outcome must
   remain identical.
9. **Refresh and replay stability.** Repeated renders, skipped intermediate refreshes, and differing
   presentation speeds produce the same ordered entry identities and semantic values. Save, exit the
   process, restart, and load during direct work, delegated work, and after cancellation; the complete
   snapshot and chronicle must reconstruct identically.
10. **Field-complete persistence.** Deep comparison must include every new source id, operation id,
   entry id, time, stance, confidence, basis, consequence, and uncertainty field. A visible-screen
   equality check alone is insufficient.
11. **Non-recipient privacy.** A character who did not make the decision, receive the report, own the
    receipt, or own the completed order gains no entry merely because the authoritative event exists.

## Verification required after implementation

The implementer must report exactly what was run. At minimum:

```powershell
dotnet build CrimeEmpire.sln
dotnet test CrimeEmpire.sln
dotnet run --project src/CrimeEmpire.Runner -- --verify --seed 42 --days 90
dotnet run --project src/CrimeEmpire.Runner -- --verify --variant cautious-vincent --seed 42 --days 90
dotnet run --project src/CrimeEmpire.Runner -- --verify --variant watchful-boss --seed 42 --days 90
dotnet run --project src/CrimeEmpire.Runner -- --verify --variant disloyal-vincent --seed 42 --days 90
dotnet run --project src/CrimeEmpire.Runner -- --verify --variant resentful-tommy --seed 42 --days 90
dotnet run --project src/CrimeEmpire.Runner -- --verify --variant capable-angelo --seed 42 --days 90
dotnet run --project src/CrimeEmpire.Runner -- --compare --seed 42
dotnet run --project src/CrimeEmpire.Runner -- --variant disloyal-vincent --viewpoint salvatore --seed 42 --days 90
dotnet run --project src/CrimeEmpire.Runner -- --variant baseline --viewpoint vincent --seed 42 --days 90
```

The pre-milestone baseline is 809 passing tests, build 0 warnings / 0 errors, six distinct traces,
five distinct action sequences, and these fingerprints:

| Variant | Fingerprint |
|---|---|
| baseline | `A99EC8C3720272B5` |
| cautious-vincent | `F5890C72AAA69948` |
| watchful-boss | `2A05F3A500CF0D74` |
| disloyal-vincent | `ED344BD31EF7B2E6` |
| resentful-tommy | `A4D7CEA8BE62B8C7` |
| capable-angelo | `F5C95925301DA227` |

Retain all existing Godot self-tests and add one dedicated live situation/chronicle self-test (name
chosen during implementation) that covers direct and delegated routes, save, process restart, load,
and non-recipient privacy through the production UI. The existing 24 Godot checks are the starting
baseline, not a substitute for the new scene.

## Human comprehension gate

After automated verification, Matt plays at least one direct and one delegated neutral route without
the developer trace. He must be able to answer the five questions under **Feature intent**, distinguish
an order from observed progress, identify the source of a report, and say what Vincent still does not
know. At least one direct route must include a personal refusal followed by eventual payment, and Matt
must not be led to believe the chronicle is a complete encounter transcript. Correct data that still
reads as scattered state fails this gate.

When implementation and its verification are complete, move the finished scope and evidence into
`docs/milestones/032-one-operation-becomes-a-scene.md` and include that archive in the same focused
implementation commit, before independent exact-commit review, as `AGENTS.md` requires. Replace this
full working scope in `CURRENT_MILESTONE.md` with a compact active-review gate stating that independent
review and human acceptance remain pending; do not claim closure in the implementation archive.
Append later correction, review, and acceptance evidence to the archive rather than rewriting its
earlier record. The milestone closes only after independent review, Matt's findings rulings, and
Matt's explicit acceptance of the human gate. Then stop. Do not automatically authorize Milestone 033.

## Deliberately carried work

- OPEN_CONCERNS #8 remains open for pacing and narrative legibility.
- Narrative explanations for genuinely unknown delays remain deferred; silence must not be filled by
  invented prose.
- Meaningful follow-up responses, post-completion question relevance, clearer permission wording, and
  the broader session-objective layout remain deferred unless this exact scope names them elsewhere.
- Milestone 030 remains operationally closed through its owner exception; retrospective independent
  review and the pre-existing ledger/check-out hash discrepancy remain carried.
- The Personal Demo v0.1 sequence after this milestone remains an unauthorised direction only:
  Milestone 033 presents the existing Force consequence chain; Milestone 034 presents the continuous
  calendar; Milestone 035 tests a schematic Harbor command screen. Each requires a fresh authorization.
- Existing unrelated ROADMAP and proposal working-tree changes remain untouched.

## Implementation and self-verification — 2026-09-22

Implementation commit: the single commit introducing this archive, based on `2ce33e0`.
Implementer and self-reviewer: Codex. This is **self-verification**, not independent review or
owner acceptance. Matt explicitly authorized the revised scope, accepted the confirmation review's
AUTHORIZE recommendation and P2-1/P2-2 resolution, and reaffirmed acceptance of `2ce33e0` in the
implementation request. No independent closure-commit evidence or ledger verdict was invented.
Independent exact-commit review and the human comprehension gate remain pending.

### What was completed

- `PlayerView.Build` assembles four frozen chronicle families: the actor's own typed tribute choices,
  received operation accounts, operation-linked cash receipts, and owner-retained tribute completion.
  `SceneNarration` accepts only frozen snapshot values and the existing safe pending decision.
- Godot displays a prominent situation card above the existing drill-down columns and a chronicle in
  the activity column. After collection, the card ties the recorded commission to money received and
  the ended order. Active delegated progress stays unknown until a legitimate account reaches the
  owner. The existing personally experienced refusal occasion can appear on the current card.
- The UI explicitly says this is not a complete encounter transcript. A personal refusal is not a
  new retained history family. No explanation for an unreported delay is invented.
- `Commit.Apply` returns the newly created tribute identity through a narrow retention callback to
  `Pipeline.Resolve`, which stores the link on the committed decision. Initial executor projection
  reads the existing chosen executor candidate; no duplicate executor-retention field was added.
  The start link is never reconstructed from a target, clock, counter, list position, or live work.
- Report delivery retains report id, asserted-claim ordinal, asserted confidence and claimed basis
  alongside the existing recipient-owned account. The projection uses qualitative confidence and
  retains the asserted stance. Actual basis, candor, withholding and truth-log event ids do not cross.
- Collection attaches the real owner/local-sequence identity to the existing cash receipt. Completion
  appends only a tribute-order record with identity, target, executor, time and an owner-visible
  money-arrived bit derived from that operation's receipt. Private completion reason is not retained.
  Cancellation remains the owner's own decision entry; it does not invent a completion-path event.
- Independent cognition revisions create no chronicle entries and never rewrite received accounts.
  No generator, scorer, cognition rule, scheduler, resolution rule, tuning value, fixture, fingerprint
  implementation, or action-signature implementation changed. No schema or replay command was added.
- The authorized, pre-existing narrative-rendering entry in DESIGN_DECISIONS is included unchanged.
  Unrelated ROADMAP and proposal edits/untracked files are preserved and excluded from the commit.

### Production-path and boundary evidence

`SceneTests` adds 17 test cases, including its contract/fresh-process probe. Tests use public
PersistentSession choices for direct/delegated commissioning, continuation, cancellation and
recommissioning, and ordinary writers for report delivery and completion.

- Baseline seed 42, Threaten Bellini, Vincent personally: the public route exposes a refusal,
  then collection on 23 March; 840 arrives and cash becomes 6,840. The start, receipt and completion
  share the actual production operation identity. Delegating the same commission to Tommy also
  reaches the owner-visible collection without revealing Tommy's private refusal or progress.
- Autonomous baseline resolution produces the same retention families. Assertions check the actual
  start's target, completion's operation/target/time/executor, and report assertion at its retained
  ordinal, rather than merely testing whether some operation id exists.
- A real delegated operation supplies privately learned information to `Reporting.Compose`.
  With its account undelivered, the card says no report and the chronicle has no account. Delivering
  that composed report through `Reporting.Deliver` exposes the attributed account. The pre-existing
  authoritative event history is held fixed (delivery itself appends its ordinary report record).
  Separately, real collection remains visible even when no operation account arrived. Kane gains no
  owner entry from either history; production Godot also tests his autonomous non-recipient view.
- Later counterfactual contradictory testimony is delivered through the production writer with
  different asserted stance, confidence and claimed basis. The original received entry remains
  byte-for-byte equal. The test distinguishes claimed inference from hidden actual participation.
  This counterfactual report is a boundary fixture, not a claim that the neutral playtest produces it.
- Two actual commissions on the same target retain different ids after cancellation/recommissioning.
  Reports composed from their real execution learning are delivered at one deliberately equalized
  timestamp: both keep the correct originating operation and distinct source identities. Autonomous
  production accounts also exercise nonzero asserted-claim ordinals.
- A focused test receives an account, acquires a revisable own inference, performs two independent
  `Cognition.Revise` calls without refreshing between them, and verifies unchanged received history
  alongside changed current knowledge. These injected cognition writes are not save commands.
- Complementary production/restart proof uses the existing capable-angelo variant and two sequential
  Tommy commissions, Bellini then Ferri, with no extra report between outcome revisions. Vincent's
  existing Tommy assessment changes from .75 to .85 to .95 through the existing collection writer.
  Intermediate snapshot refreshes are skipped; original testimony stays unchanged, no account or
  revision entry is invented, and current knowledge becomes qualitatively certain. Both an xUnit
  child process and separate Godot save/load processes reproduce this state. The natural route has
  no received operation account before those revisions; preservation of an existing operation
  account is established by the complementary received-account test above, not falsely attributed
  to that natural route.
- Frozen-snapshot tests remove raw reports/truth and poison developer words without changing the
  chronicle; clearing the live account list cannot mutate an earlier snapshot. A missing start link
  remains missing despite a matching live operation. Unknown completion reasons stay unknown, and
  other strategy families create no tribute-completion entry.
- Repeated rendering, altered output casing/formatting, skipped refreshes, and event versus weekly
  advance produce identical full world comparisons, ordered chronicle semantics and terminal result.
- The replay comparator now includes every retained source field, including operation ids, source
  report id/ordinal, raw asserted confidence/basis, initial executor, receipts and completions.
  Reflection-driven field probes verify each record property affects comparison. Fresh OS processes
  load real SQLite saves and compare complete snapshots, pending decisions, retained sources and the
  comprehensive world comparison during direct/delegated work, after their collection, after either
  cancellation, after two revisions, and with naturally delivered autonomous accounts.

### Mutation evidence

All mutations were temporary, compiled, and restored in `finally` blocks. Thirteen distinct source
mutations were ultimately killed by assertions (not counted as killed by compilation failures):

| Mutation | Load-bearing result |
|---|---|
| Start writer retains sequence + 1 | 5 scene cases failed |
| Start projection drops the retained link | 4 failed |
| Delivery writes report id 0 | 3 failed |
| Delivery retains actual rather than claimed basis | 1 failed |
| Chronicle reads current belief stance instead of received stance | 1 failed |
| Collection writes sequence + 1 | 3 failed |
| Income projection guesses the most recently created operation | 1 failed |
| Completion projection discards the money-arrived result | 2 failed |
| Pipeline retains starts only for explicitly selected controlled choices | Autonomous retention failed |
| Decision projection removes viewpoint filtering | 3 failed |
| Delivery writes assertion ordinal 0 | Natural account/restart precondition failed |
| Delivery writes operation sequence + 1 | Two-operation same-instant attribution failed |
| Completion writer shifts identity to sequence + 1 | 2 strengthened completion cases failed |

The last mutation initially survived a narrowly selected autonomous test because its assertion only
required membership in the set of valid start ids. That was insufficient: the wrong id could name
another real operation. The assertion was strengthened to verify target and receipt identity/time/
executor, and the unknown-completion writer test now pins the actual live identity directly. Repeating
that mutation failed both tests. The initial survivor is retained here rather than hidden behind the
final green result. Earlier ten-mutation runs used the first 15 scene cases; later fresh-process and
contract cases were added before the final full-suite run.

### Full verification actually run

- `dotnet build CrimeEmpire.sln`: final build **0 warnings / 0 errors**.
- `dotnet test CrimeEmpire.sln`: **826 passed, 0 failed, 0 skipped** (809 pre-milestone + 17).
- `dotnet run --no-build --project src/CrimeEmpire.Runner -- --verify --seed 42 --days 90` and the
  five additional `--variant` commands specified in the authorized scope: all six repeated exactly.
- `dotnet run --no-build --project src/CrimeEmpire.Runner -- --compare --seed 42`: six distinct traces,
  five distinct chosen-action sequences; no accepted baseline was edited.
- Both specified 90-day viewpoint runs, disloyal-vincent/Salvatore and baseline/Vincent, exited 0.
- `git diff --check`: no whitespace errors. Git reports existing CRLF-to-LF normalization notices.

| Variant | Unchanged trace fingerprint | Action fingerprint |
|---|---|---|
| baseline | `A99EC8C3720272B5` | `8B3E290193E26DEF` |
| cautious-vincent | `F5890C72AAA69948` | `6FAE398E8258F7BE` |
| watchful-boss | `2A05F3A500CF0D74` | `FAD9D030CCB0ACD8` |
| disloyal-vincent | `ED344BD31EF7B2E6` | `42444C3CB496E273` |
| resentful-tommy | `A4D7CEA8BE62B8C7` | `8B3E290193E26DEF` |
| capable-angelo | `F5C95925301DA227` | `B5F30EBD90A8465B` |

Godot 4.7.1 Mono, headless, **35 distinct checks passed**. Each invocation used
`--headless --path src/CrimeEmpire.Godot --` followed by its flag:

- All 24 existing checks: `--selftest`, `--selftest-ending`, `--selftest-goldenpath`,
  `--selftest-directaction`, `--selftest-corroboration`, `--selftest-tribute`, `--selftest-capability`,
  `--selftest-operation`, `--selftest-informed-choice`, `--selftest-commission`, the existing
  restart-save/restart-load pair, and commissioning-save/commissioning-load pairs for stages leaf,
  executor, back, committed, execution and report (`--commission-stage=...`).
- New `--selftest-scene` exercises direct commission, save/load, personal refusal, eventual payment,
  the actual situation/chronicle labels, and non-recipient privacy.
- Ten new process-separated invocations: `--selftest-scene-save` then `--selftest-scene-load`, with
  `--scene-route=direct`, `delegated`, `cancelled`, `revisions`, and `accounts`. The accounts route
  verifies naturally delivered source-bearing entries on the live Godot UI. Every pair uses the
  isolated `crime-empire-scene-test.db`, never the production save slot. Expected JSON is test
  comparison evidence; loading still reconstructs solely from the ordinary SQLite replay log.

The initial sandboxed build could not read the installed NuGet configuration. The required build,
tests and engine checks then ran successfully with approved tool escalation. No SDK, configuration,
baseline or dependency was changed to conceal that environment restriction.

### Self-review, limits and deferred work

Self-review traced each new reader back to its permitted writer and inspected the focused diff.
Simulation changes are retention-only; action and deterministic trace hashes remain unchanged.
New source fields are covered separately by field-complete replay comparisons; unchanged legacy
trace hashes are not claimed to cover those added fields themselves.

Headless engine checks verify controls, labels and restart behavior, not human readability at a real
window size. No manual playtest acceptance, independent exact-commit PASS, or milestone closure is
claimed. Matt must still judge whether the card reads as a coherent situation. The chronicle is
intentionally incomplete: personal refusals disappear with the current occasion, and unknown delays
remain unexplained. Existing persistence remains same-build only; start fresh for this build.

OPEN_CONCERNS #8 remains open. The carried M030 retrospective review/history discrepancy, follow-up
relevance/wording, broader objective layout, and future demo directions remain deferred as listed in
the authorized scope. No Milestone 033 work or push was performed.

### Direct and delegated neutral playtests

Open `src/CrimeEmpire.Godot/project.godot` with the installed .NET Godot and run the main scene.
Start a fresh baseline game, seed 42, controlling Vincent. Use Next event until the opening choices.

1. Direct: choose **threaten Bellini's grocery**, **Do it yourself**, **Confirm operation**. Continue
   with **carry on getting Bellini's grocery to pay** when offered. Read the personal refusal on the
   current situation card; keep advancing to payment. The tested route pays 840 on 23 March.
2. Delegated: start fresh with the same settings and choice, select **Assign Tommy Nardo**, then
   confirm. Choose **take no action** when hands are free and **leave these orders unchanged** if
   reviewing that order. Read the unknown progress while waiting, then the source of the money when
   it arrives. Do not interpret silence as a refusal or as proof of progress.
3. On both routes, save during work, close the game, restart and load. Without the developer trace,
   explain what was ordered, the method/executor, what happened or was reported, the source of each
   fact, what is still unknown, and what changed. The chronicle is not a transcript. Matt's explicit
   acceptance of these answers and of the presentation is still required.
