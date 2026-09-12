# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**Milestone 024 remains active after Matt's 2026-09-12 playtest.** Its seventh correction is commit
`344f1e0`; Matt then reproduced a missing delegated-success explanation and a second payment from the
same already-paying grocery. Codex implemented an eighth correction at Matt's request because Claude
was unavailable. It is complete and awaits independent implementation review; milestone 024 is not
closed. See `REVIEW_LEDGER.md` and the archive for the measured account.

- **Milestone 021 — Capability Is a Belief, Not a Stat — is closed.** Three corrections beyond its
  implementation, each answering a Codex review; the third (`9fed181`) returned no findings and Matt
  accepted it. Accepted state: `9fed181`. Full account:
  `docs/milestones/021-capability-is-a-belief-not-a-stat.md`.
- **Milestone 026 — In Person, Things Come Back — is closed.** Two corrections from the same-day
  playtests, then three more answering successive Codex reviews of those corrections and of each
  other; the fifth (`abcffd5`) returned no findings and Matt accepted it. Accepted state: `abcffd5`.
  Full account: `docs/milestones/026-in-person-things-come-back.md`.
- **Milestone 020's own outstanding backlog has been reviewed, oldest first.** `34cd117` returned one
  P1 — already superseded by `8e6878e`, which Codex separately confirmed. Milestone 020's accepted
  state moves from `c25129a` to `8e6878e`, now independently confirmed rather than resting on
  self-review alone.

- **Milestone 022 has a second, larger correction: the shared `Rng.ForOccasion` finalizer was
  GF(2)-linear and is now fixed.** The property the production-path correction above recorded "out of
  scope to act on" — Salvatore's, Vincent's and Kane's observation rolls on one event never landing
  together across thousands of searched seeds — was a proof, not a correlation quirk: the old
  finalizer let the world seed cancel out of any two occasion keys' XOR difference, locking every pair
  of occasion-keyed streams into one fixed, seed-independent relationship no seed could break. Fixed
  by replacing the finalizer with fmix32 (MurmurHash3's), which is not linear over GF(2). `ForDecision`
  has the identical structural shape and therefore the identical correlation *risk* — not a
  demonstrated identical failure — recorded as such in `OPEN_CONCERNS.md` #6.
  Running the full suite afterward surfaced 24 failures across seven files — every natural-run test
  that happened to read Vincent's or Kane's own observation roll on the same milestone-022 event.
  Each was individually traced and classified (natural-history claim / mechanic proof / cascaded
  count) before being touched; none classified as an unexpected defect. Repaired: `StreetTalkTests.cs`
  strengthened per the original scope (bounded co-success proof, positive end-to-end provenance,
  insertion stability, deterministic replay, mutation-checked); `ScenarioReachTests.cs`,
  `CausalFeedbackTests.cs`, `ControlledAutonomousParityTests.cs`, `InvestigationTests.cs` and
  `PronounTests.cs` moved their affected natural-run tests to a declared alternate seed (199) found by
  search over the unmodified production scenario; `RelationalConsequenceTests.cs`'s `watchful-boss`
  conflict count corrected from 3 to 2, traced to the redistribution rather than assumed; the Godot
  `--selftest-directaction` choice sequence re-derived live for the identical reason (milestone 017's
  own archive carries this specific correction, since it is that milestone's proof). The seed-42
  resentment test (`ScenarioReachTests`) is retracted rather than relocated, since its own claim was
  specifically about seed 42. Milestone archives `007`, `008`, `011`, `012`, `017`, `018`, `019` and
  `022` itself each carry an append-only correction section recording what moved and why. Full
  account: `docs/milestones/022-the-street-talks.md`.

  **One deviation from the authorized scope, flagged and approved rather than silently taken:**
  `src/CrimeEmpire.Godot/Game.cs`'s `DirectActionChoiceSequence` needed re-deriving alongside
  everything else for `--selftest-directaction` to keep passing — the authorization for this
  correction named `Rng.cs` as the only production file to touch, and this is a second one. Matt
  reviewed and approved it specifically as self-test-only scaffolding (a private constant array read
  only by the self-test, gated behind its own CLI flag, never reachable during normal play; confirmed
  no other line in `Game.cs` changed) before it was committed.

- **Codex reviewed `b4ce907` and returned three findings; Matt accepted all three.** All three were
  about the *explanation*, not the fix: `Fnv1a` was wrongly called GF(2)-linear (it multiplies; the
  argument only needs it to be a fixed function of the key), the claim that no two occasion keys could
  ever co-succeed at any seed overstated what the algebra proves (it explains the demonstrated
  three-observer case, not a universal theorem over every key pair), and the verification inventory's
  "only `Rng.cs` changed" line was already stale by the time it was written — `Game.cs` had been
  approved and committed alongside it. A documentation- and comment-only correction answers all three
  in `Rng.cs`, `DESIGN_DECISIONS.md`, `OPEN_CONCERNS.md`, `StreetTalkTests.cs`'s stale class-level
  comment, and an appended (not rewritten) correction section in
  `docs/milestones/022-the-street-talks.md`. No runtime behaviour, test behaviour, RNG method,
  probability, key, or fixture changed. **This commit's `src/` diff is not empty** — it is 28
  additions and 11 deletions in `Rng.cs`'s XML documentation comments, the corrected explanation
  itself; no method body changed.

- **Codex found a P1 on `7cbeb91`: the claim above ("`git diff --stat` against `src/` for this
  commit is empty") was false, and Matt accepted the finding.** The accurate claim, now stated
  correctly, is that no method body or runtime behaviour changed — the diff is real and is entirely
  documentation comments. Corrected in `docs/milestones/022-the-street-talks.md` (appended, not
  rewritten) and in `docs/REVIEW_LEDGER.md`, per that file's own convention for a false claim already
  on the record. This correction is itself documentation-only — `Rng.cs`, tests, and every other
  milestone archive are untouched.

- **Matt accepted `4ed58e3`. Milestone 022's `Rng.ForOccasion` correction chain is closed** — the full
  chain being `b4ce907` (the fix and every repaired natural-run test), `7cbeb91` and `4ed58e3` (two
  documentation-only corrections answering Codex's review of the written explanation).

- **Milestone 023 — The Roster Reads — has a correction: Codex reviewed `6738200` and returned one
  P1, accepted by Matt.** `Relations.RecordAccountConflict` and `RecordAccountAgreement` remembered a
  `StandingChange` unconditionally, even when the clamped `Trust` value did not actually move at the
  floor or ceiling — the same defect `Relations.Frighten` was already guarded against for fear, in the
  same file. Fixed the identical way, with two new mutation-checked production-path tests. Nothing in
  the accepted fixture reaches either clamp, so no accepted hash moved. Full account:
  `docs/milestones/023-the-roster-reads.md`'s correction section.

- **Codex reviewed `4da1e66` — the same-day follow-on — and returned two P2s, both accepted by
  Matt.** The `PersonIsCapable` narration fix shipped with no regression coverage, answered by two
  tests: one pinning both bars' exact prose, one driving every `ClaimKind` through
  `PlayerNarration.Describe` generically so a future kind added without a narration arm fails
  automatically. Both mutation-checked; `git diff --stat` against `src/` for the correcting commit is
  empty — test-and-documentation-only. Separately, `4da1e66` left `PlayerNarration.Standing`'s
  class-level doc comment still arguing the position Matt reversed on 2026-09-04; confirmed by reading
  the live source that `1a7bcc6` already corrected it, so no source edit was needed, only the accurate
  history recorded append-only. Full account: `docs/milestones/023-the-roster-reads.md`'s second
  correction section.

- **Codex reviewed `2dec7ff` and returned one P2, accepted by Matt: the focused `PersonIsCapable`
  test used `"tommy"` as its subject id with an identity resolver, so it could not distinguish "the
  resolver was called" from "the raw internal id leaked through" — the recurring false-assurance
  shape.** Fixed by using an unmistakably internal id resolved to a genuine display name
  (`"Tommy Nardo"`) and asserting the display name appears while the internal id does not.
  Mutation-checked by changing the production arm's resolved subject to the raw `c.Subject`: the
  focused test failed, the exhaustive `ClaimKind` test was unaffected, then reverted — `git diff
  --stat` against `src/` is empty. Full account: `docs/milestones/023-the-roster-reads.md`'s third
  correction section.

- **Codex reviewed `15d7c92` — "Show what he takes a man for on the roster" — and returned two P2s,
  both accepted by Matt.** First: nothing proved `StandingChange.About` carries the exact
  originating claim, or that two same-cause entries about different claims stay distinguishable
  through the roster projection — answered with two assertions on existing tests plus one new test
  reading `PlayerView.Build`'s own rendered lines, mutation-checked independently for the conflict
  and agreement writers. Second: `PlayerAttitude.TakenFor` had no dedicated coverage — answered by
  three new tests (viewpoint-derived and not another actor's; no-view versus rejected-view
  preserved, staged; natural reach into `IntelligenceWriter` via `capable-angelo`) plus an honestly
  stated structural argument for the Godot roster panel's reach, since no existing self-test uses
  that variant and none was added, per scope. The raw ladder reader `15d7c92` introduced was not
  reopened (superseded by `9fed181`); the stale `CURRENT_MILESTONE.md` statements present at that
  commit are recorded as historical in the archive rather than corrected here, since this file has
  long since moved past them. 671 tests passing (667 + 4 new); `git diff --stat` against `src/` is
  empty; all accepted hashes, both viewpoints, and all seven Godot invocations unchanged. Full
  account: `docs/milestones/023-the-roster-reads.md`'s fourth correction section.

- **Codex reviewed `53694a2` and returned two P2s, both accepted by Matt.** First:
  `TakenFor_reaches_the_runners_viewpoint_render`'s `Assert.Contains("hard man", rendered)` was
  false assurance — the unrelated belief-list section already contains those words regardless of
  whether `TakenFor` renders anything. Fixed by locating the `HOW HE TAKES THEM` header and
  asserting only against what follows it, with the risk demonstrated (the phrase does appear before
  the header too) rather than assumed. Mutation-checked by removing `IntelligenceWriter`'s
  `TakenFor` lines: failed, reverted. Second: nothing drove `TakenFor` through the live Godot
  screen — answered with a new sixth self-test, `--selftest-capability`, against the real
  `capable-angelo` fixture, isolating the roster's own attitude-panel section the identical way;
  mutation-checked by removing `Game.cs`'s `TakenFor` lines: failed, reverted. Also corrected in the
  same commit: the prior correction's own miscounted test tally (one new `.About` test plus three
  new `TakenFor` tests, four total — the arithmetic was already right, the prose was not) and a test
  doc comment claiming "the same day" for two contradictions actually staged three days apart —
  restaged at the identical instant, which production permits. 671 tests unchanged (both corrected
  tests rewritten, not added to); `git diff --stat` against `src/` shows only the new, additive
  self-test; all accepted hashes, both viewpoints, and all eight Godot invocations (seven plus the
  new one) unchanged. Full account: `docs/milestones/023-the-roster-reads.md`'s fifth correction
  section. Awaits its own Codex re-review.

- **Codex reviewed `f993386` — milestone 024's implementation — and returned three findings, all
  accepted by Matt.** First, the behavioural one: `Operating` read only the viewpoint's own
  `Execution.Strategy`, the owner's field, so an executor carrying work delegated to him saw no
  operation at all — the milestone's own leak, facing the other way. Fixed by scanning for the one
  other character whose own instance names the viewpoint as `DelegatedToId`, then deriving
  executor name and progress by who is actually doing the work rather than by which field held the
  instance. Second and third were documentation claims (a delegation-time claim in
  `PlayerSnapshot.cs`; a stale step-phrase count in `PlayerNarration.cs`) that, on reading the live
  source, were already correct — the first was never present in `f993386`, the second was brought
  current by milestones 025 and 026 extending the same table — recorded as verified rather than
  fixed. Two production-path tests added against the natural day-20 baseline, plus a focused
  `IntelligenceWriter` test and a new Godot self-test (`--selftest-operation`) each isolating the
  operation section from the unrelated belief list before asserting within it. All new tests
  mutation-checked and reverted, including a check that reverting `Operating` to the owner-only
  lookup fails exactly the new executor-view test. 674 tests passing (671 + 3 new); all four
  required hashes, the six-configuration `--compare` figure, both required viewpoints, all seven
  Godot self-tests, and the restart proof unchanged. Full account:
  `docs/milestones/024-the-operation-reads.md`'s correction section.

- **Codex reviewed `9ac569b` — the first correction above — and returned three findings, all
  accepted by Matt.** First: the executor inherited the owner's pre-delegation `StartedAt` — Tommy,
  delegated on 14 March, read "running since 2 Mar" from Vincent's own act of starting it. Fixed by
  making `PlayerOperation.Since` nullable, populated only for the owner, with both renderers changed
  to omit the line rather than print one wrongly. Second: nothing enforced the one-operation-per-
  executor rule the projection had been silently assuming. Fixed at both places a delegation is
  created — `Generators.FromRelationship` no longer offers a busy subordinate (reading a new
  `GeneratorContext.AvailableSubordinateIds`), and `Commit.Apply` refuses, fail-closed, calling the
  identical `Pipeline.AvailableToExecute` directly. Third: `Operating`'s own fallback scan now reads
  as `SingleOrDefault` rather than taking the first match, since uniqueness is genuinely enforced
  elsewhere now rather than merely assumed. Six new/strengthened tests, three mutation checks (the
  commit guard removed, the availability filter removed, `Operating`'s condition weakened — not
  four; this file's own count was corrected once by hand, since it is mutable rather than history,
  and once more below after Codex caught the same overstatement again), all confirmed and reverted
  — one of which caught a genuine gap in a new test's own setup (Vincent's own strategy never
  staged, so the assertion passed vacuously regardless of the filter under test) and was fixed
  before the mutation check was re-run and accepted. Also corrected in the same commit: this file's
  own prior entry above overstated its mutation-check count — recorded accurately in
  `REVIEW_LEDGER.md` rather than silently fixed here. 678 tests passing (674 + 4 new); all four
  required hashes, the six-configuration `--compare` figure, both required viewpoints, all seven
  Godot self-tests re-run against the live render, and the restart proof unchanged. Full account:
  `docs/milestones/024-the-operation-reads.md`'s second correction section.

- **Codex reviewed `00613ca` — the second correction above — and returned FAIL.** It confirmed the
  broader mechanic's implementation was already correct — a character may be involved in at most one
  active operation at a time, either as its owner or as its delegated executor — but found three
  documentation and coverage gaps. **Matt accepted those three findings and explicitly authorized
  the broader rule as stated.** No production behaviour changed. First: the rule had never been
  written down in `docs/DESIGN_DECISIONS.md` — recorded in a new "Operation staffing" section, alongside the
  related point that availability is authoritative organisational state for eligibility while
  identity/nameability remains the separate question `Acquaintance.KnownTo` already settles.
  Second: every existing test proved the rule against a hand-built `GeneratorContext`, never through
  `Pipeline.Prepare` itself — closed with four new tests driving Vincent through the real pipeline
  to his own delegation fork: a free subordinate offered, one owning an undelegated operation not
  offered, one carrying delegated work not offered, and one who owns an operation already delegated
  onward to a third man still not offered (pinning the broader involvement rule, not only "not
  currently a live delegate"). Mutation-checked against the shared `Pipeline.Prepare` wiring: all
  three negative tests failed, the positive control still passed, reverted. Third: the entry above
  overstated its own mutation-check count a second time ("four," immediately followed by a list of
  three) and overstated "no simulation behaviour changed" against what was actually verified — both
  corrected in place above, since this file is mutable, and recorded accurately (append-only) in
  `REVIEW_LEDGER.md` and the milestone archive. 682 tests passing (678 + 4 new); all four required
  hashes, the six-configuration `--compare` figure, both required viewpoints, all seven Godot
  self-tests, and the restart proof unchanged. Full account:
  `docs/milestones/024-the-operation-reads.md`'s third correction section.

- **Codex reviewed the documentation-only correction to the bullet above (`ed12b5e`) and returned
  one P1, accepted by Matt: that correction's own closing sentence in `REVIEW_LEDGER.md` claimed
  "Codex accepted this correction on its own re-review" before any such re-review had taken place.**
  `ed12b5e` was itself the correction; nothing in its own text could truthfully report Codex's
  verdict on it, since no review of it existed until Codex performed one afterward, and the
  fourth-review row it pointed to as evidence covers `dd59a1b`, a different, earlier commit. No
  mechanic, test, or production code changed. Corrected: the false sentence left standing
  (append-only) in `REVIEW_LEDGER.md` and the milestone archive, each followed by a correction
  naming it; fixed directly here, since this file is mutable. Full account:
  `docs/milestones/024-the-operation-reads.md`'s fifth correction section. **This correction itself
  awaits its own Codex re-review** — it is not accepted, verified, passed, or closed.

**Paused before milestone 027, on Matt's word, while Codex works through the remaining milestone
023–025 backlog in order: `6738200`, `4da1e66` and `15d7c92` (all corrected above), `f993386` (now
corrected three times above), `1a7bcc6`, `95e60b5`.** Nothing here authorizes starting 027 until that
backlog is cleared and Matt says so.

- **Codex reviewed milestone 024's sixth correction (`ed7d38a`) and returned FAIL with five
  findings; Matt accepted them and authorized this seventh correction.** Corrected: `Commit.Apply`
  now re-reads authoritative world state before a delegate can start a second operation; both replay
  comparators fingerprint `PolicyBreachDecisionMakerId`; production-path tests pin executor wake and
  choices, owner non-authority/non-information, explicit postponement, the domain-scoped leadership
  gate, stale-context refusal, and owner non-overwrite; the durable rulings now live in
  `DESIGN_DECISIONS.md`; and false verification/staging claims in the sixth archive account are
  corrected append-only. **The seventh correction is complete and awaits Codex review; it is not
  described as reviewed, passed, accepted, or closed.** Full account:
  `docs/milestones/024-the-operation-reads.md`'s seventh correction section.

- **Matt's playtest of the seventh-correction state found two connected defects, corrected together
  in milestone 024's eighth correction.** Tommy's delegated operation really succeeded on 5 April,
  but the completion occasion said only that the job ended “one way or another,” even though Vincent
  directly observed the money and knew whom he had assigned. Worse, Vincent's earlier confidence-1.0
  participant knowledge that the grocery had refused survived the later confidence-0.9 discovery
  that money arrived, so the real player path offered a second operation and eventually paid the same
  840 again (cash 6,000 → 6,840 → 7,680). Corrected: collection now gives the owner an equally certain
  Discovery rejection of the stale refusal; the bounded completion occasion names the arriving money
  and assigned executor but no private method/progress; a stale operation discovers an already-paying
  shop and closes before demanding; and collection is independently idempotent for the current
  agreement. The invalid second-cycle golden path was re-derived through real choices. Four
  load-bearing branches plus the collection-state replay fingerprint have focused assurance. **This
  correction was authored by Codex and awaits independent review; it is not accepted or closed.**

## Next, per the demo arc

`ROADMAP.md`'s "The demo arc" — layer 1 finishes with **027 The session has an ending**, and then
the layer ends in a playtest. Nothing is authorized; scope goes into this file one milestone at a
time, and what the playtest finds goes first.

**Carried into whatever comes next:** a reader of an impression (a man who saw he was not believed
has reason to act, and nothing offers him anything yet); the operation's pacing, parked at Matt's
word; a history of finished operations, which is 027's.
