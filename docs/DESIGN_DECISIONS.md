# Criminal Empire — Design Decisions

Settled decisions. Do not re-litigate without a concrete reason surfaced during actual
implementation. Unresolved items live in `OPEN_CONCERNS.md`. Active work lives in
`CURRENT_MILESTONE.md`; unbuilt work and candidate scopes live in `ROADMAP.md`.

This file supersedes the "Decision history" section of `PROJECT_CONTEXT.md` and the resolved
portions of the retired `design-doc-concerns_1.md`. Each entry below cites the doc/section that
settled it, so a future session can verify the claim instead of taking it on faith.

## Project shape

- **Genre/scope**: persistent-world, character-driven criminal empire sim. Player freedom, no
  mandatory path, no universal win condition. Relationship graph + character decision-making
  *are* the game, not a management sim with NPCs bolted on. — `GAME_VISION.md`,
  High Concept / Pillar 1.
- **Scope discipline is a standing constraint**, not a one-time correction. Original ambition
  (20-city state, full laundering/politician/lawyer depth, procedural heist maps day one) was
  walked back to v2+/long-term vision. Default to the smaller, faster-to-test version whenever
  scope is ambiguous. — `PROJECT_CONTEXT.md`, Decision history.

## Succession and persistence

- On player-character death/incapacitation: if a viable heir or sufficiently loyal capo exists,
  control transitions to them. The new POV character has **independent stats** — they inherit
  territory/relationships/standing (CK3 model), not the predecessor's personal skill sheet. This
  is now written into the vision doc itself, not just conversation history. —
  `GAME_VISION.md`, "Succession and Continuity."
- If no viable successor: game over for that dynasty, but the world persists; a new character can
  start in the same city, in the visible legacy/ruins of the old empire. Flagged as the project's
  strongest differentiator — cheap to fake with flavor text/news/dialogue, high narrative payoff.
  — `PROJECT_CONTEXT.md`, Decision history.

## Heists

- MVP resolution is abstract-roll (crew skill, prep/intel, guard density, stealth vs. aggression,
  risk modifier → outcome table), with named variables so a later procedural mini-map can be a
  presentation layer over proven logic rather than a parallel system to debug. Greed/time/risk
  push-your-luck tension during an unfolding op is a deliberate design goal. —
  `GAME_VISION.md`, "Heists"; `PROJECT_CONTEXT.md`, Decision history.

## Actor parity and simulation tractability

- Actor parity means **causal parity, not computational parity**: comparable actions follow
  comparable requirements/costs/consequences regardless of actor, but NPCs do not get the
  player's full interface, planning depth, or update frequency. This is the resolution to what
  was flagged as the single most expensive commitment in the vision doc if taken literally. —
  `SIMULATION_ARCHITECTURE.md`, "Causal Parity, Not Computational Parity."
- Rejected on purpose, not by oversight: unrestricted GOAP/general planning, continuous
  deliberation for all characters, identical AI/player interfaces, minute-resolution updates
  during fast-forward. Do not reintroduce without a concrete demonstrated need from
  implementation. — `SIMULATION_ARCHITECTURE.md`, "Why the MVP Does Not Use Unrestricted
  Planning" / "MVP Architecture Commitments."
- Traits/personality modify perception, salience, and evaluation — **never fire actions
  directly**. Rejected pattern: `Aggressive → monthly chance to attack`. Treated as the single
  most important anti-pattern in the architecture; violating it is the fastest way to make
  characters feel like slot machines. — `SIMULATION_ARCHITECTURE.md`, "Traits and Causality."
- Simulation depth is relevance-tiered (Active / Supporting / Background) with a strict Tier 1
  population budget and explicit promotion/demotion rules that preserve causally important state.
  — `SIMULATION_ARCHITECTURE.md`, "Simulation Relevance Tiers."
- **The trait and drive vocabulary is closed, and this is the list.** Traits: `Aggressive`,
  `Cautious`, `Proud`, `Suspicious`. Drives: `Wealth`, `Status`, `Security`, `Belonging`. Every
  entry has a stated behavioural purpose; a trait that cannot name one does not belong, and two
  that move the same numbers should be merged. Two omissions are deliberate: **`Loyalty`** is
  derived per relationship from trust, obligation and Belonging rather than stored as a universal
  scalar, because one loyalty number collapses "loyal to whom, and for which reason"; and
  **`Ambition`** is what a Status weight already is, so a separate advancement drive would be a
  tuning finding, not a default. — `Domain/Psychology.cs`, closed in milestone 001; this retires
  `OPEN_CONCERNS.md` #4.

## Information channel — settled invariants

Settled by milestone 003 and refined by 004; see `milestones/003-information-transmission.md` and
`milestones/004-provenance-precision.md` for the findings behind each. These are the contract the
report channel is reviewed against — `REVIEW_LEDGER.md` cites this section rather than restating it.

- **Deception is a candidate evaluated through the normal decision pipeline**, not a scripted
  branch. No code branches on a trait to produce a lie.
- **A report is composed only from positions the sender actually has.** Reporting code cannot read
  authoritative truth to invent content or to make the sender accurate.
- **A partial report distinguishes claims asserted, deliberately withheld, and omitted only because
  the bounded message was full.** Withholding settles a claim until the sender's own position moves;
  cap-omission leaves it outstanding.
- **Repeated identical accounts do not compound confidence.**
- **A source changing their account is meaningful**: recantation or contradiction updates
  reconsideration and remains communicable onward.
- **Corroboration counts distinct sources across the whole testimony history**, not the record's
  original attribution.
- **A request is scoped to a particular claim.** Asking a person one question does not permanently
  close the communication channel with them.
- **Asking is spent when the question is put**, not when the recipient chooses to answer. This
  bounds unanswered requests without forcing a reply.
- **A speaker's claimed basis is separate from what he privately holds.** Only the claimed basis may
  reach the listener; the actual basis is developer truth. Repeating someone's testimony makes it
  hearsay — a chain cannot launder itself back into first-hand at each hop.

The player-facing view is constrained to match: it reads only the viewpoint character's cognition,
testimony and known relationships; never enumerates the authoritative roster to reveal unknown
people; uses qualitative confidence; presents conflicting accounts with attribution; and does not
expose utility scores, hidden intentions, or the authoritative truth log.

## Stack

- **Simulation core**: C#, plain classes, engine-agnostic, unit-testable from the command line.
  No Godot/engine dependency in this layer.
- **Persistence**: SQLite — chosen specifically for the explainability requirements (decision
  traces need real queries) and promotion/demotion tiering, not JSON/binary blobs.
- **Rendering/engine**: Godot 4 with C# (not GDScript) — same language as the sim core, no FFI
  boundary. Chosen over Unity for licensing simplicity, 2D/tilemap support, and UI toolkit fit
  for a text/menu-dense management game.
- **Sequencing**: headless console sim core first, no Godot project yet. Prove a small hardcoded
  cast produces believable decision traces in plain text before spending time on tilemaps,
  sprites, or UI.
  — `PROJECT_CONTEXT.md`, "Stack decision."
- **Target framework: .NET 10 (LTS)**, not .NET 9. The kernel was originally scaffolded against
  .NET 9 because that was the SDK already on the dev machine; Matt confirmed the intent is to move
  to .NET 10 now that it's the current LTS. — decided in chat 2026-08-13; flagged originally by
  Codex during the docs/src/tests reorg. **Executed 2026-08-13** (milestone 002, see
  `docs/milestones/002-dotnet-10-migration.md`): SDK pinned via `global.json` to `10.0.400`
  with `rollForward: latestFeature`, and `TargetFramework` set to `net10.0`. Note that the
  original decision text said "update all three `.csproj` files"; in practice the TFM is
  centralized in `Directory.Build.props`, which `CrimeEmpire.Simulation` and `CrimeEmpire.Runner`
  inherit, so only `Directory.Build.props` and the one redundant override in
  `CrimeEmpire.Simulation.Tests.csproj` needed changing. Redundant per-project `TargetFramework`
  entries were deliberately *not* added — `Directory.Build.props` is the single source of truth
  for the TFM.

## Concerns resolved since `design-doc-concerns_1.md` was written

The concerns doc was never updated after later doc revisions addressed several of its own
findings. Retired in favor of this entry; see `OPEN_CONCERNS.md` for what's still actually open.

- **NPC action → player-facing story.** Was concern #1 ("no plan for how NPC-driven action
  becomes visible to the player as story"). Resolved: `INFORMATION_AND_LEGIBILITY.md` now exists
  and is built specifically to answer this — Visibility vs. Legibility, trace/observation/claim
  model, source-limited player reports.
- **Organizational coordination without hive-mind or independent-agent chaos.** Was concern #3.
  Resolved structurally: `SIMULATION_ARCHITECTURE.md`, "Organizational Intent and Coordination,"
  gives the conditions → priorities → offices → assignments → interpretation → reports flow.
  Still flagged in that same doc as "a load-bearing target for the earliest behavioral prototype"
  — the design answer exists, it hasn't been validated by running code yet.
- **Actor parity's legibility (not just affordability).** Was concern #5. Resolved:
  `INFORMATION_AND_LEGIBILITY.md`, "Player-Facing Explanation," gives near-verbatim the
  dev-trace-to-in-fiction-explanation translation layer the concern asked for.
- **MVP proving too many hard things simultaneously.** Was concern #7. Resolved: the
  "Pre-MVP Simulation Kernel" and three-phase "Validation Sequence" (kernel → emergence prototype
  → MVP vertical slice) in `SIMULATION_ARCHITECTURE.md` is exactly the smaller-prototype-first
  structure the concern recommended.
- **Succession stat-inheritance not written down anywhere.** Was concern #9. Resolved: see
  "Succession and persistence" above — now explicit in the vision doc itself.
- **No heuristic test for a disguised heat bar.** Was concern #10. Resolved:
  `GAME_VISION.md`'s Design Heuristics and
  `INFORMATION_AND_LEGIBILITY.md`'s "Anti-heat-bar tests" both cover this directly now.
