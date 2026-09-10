# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**Nothing is active.** Codex is reviewing again, and three milestones' standing changed on 2026-09-08
as a result — see `REVIEW_LEDGER.md` for the full accounting behind each.

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
  `docs/milestones/023-the-roster-reads.md`'s correction section. Awaits its own Codex re-review.

**Paused before milestone 027, on Matt's word, while Codex works through the remaining milestone
023–025 backlog in order: `6738200` (corrected above), `4da1e66`, `15d7c92`, `f993386`, `1a7bcc6`,
`95e60b5`.** Nothing here authorizes starting 027 until that backlog is cleared and Matt says so.

## Next, per the demo arc

`ROADMAP.md`'s "The demo arc" — layer 1 finishes with **027 The session has an ending**, and then
the layer ends in a playtest. Nothing is authorized; scope goes into this file one milestone at a
time, and what the playtest finds goes first.

**Carried into whatever comes next:** a reader of an impression (a man who saw he was not believed
has reason to act, and nothing offers him anything yet); the operation's pacing, parked at Matt's
word; a history of finished operations, which is 027's.
