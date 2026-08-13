# Criminal Empire — Design Decisions

Settled decisions. Do not re-litigate without a concrete reason surfaced during actual
implementation. Unresolved items live in `OPEN_CONCERNS.md`. Active work lives in
`CURRENT_MILESTONE.md` (or `PROJECT_CONTEXT.md` until that split happens).

This file supersedes the "Decision history" section of `PROJECT_CONTEXT.md` and the resolved
portions of the retired `design-doc-concerns_1.md`. Each entry below cites the doc/section that
settled it, so a future session can verify the claim instead of taking it on faith.

## Project shape

- **Genre/scope**: persistent-world, character-driven criminal empire sim. Player freedom, no
  mandatory path, no universal win condition. Relationship graph + character decision-making
  *are* the game, not a management sim with NPCs bolted on. — `criminal-empire-design-doc-revised.md`,
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
  `criminal-empire-design-doc-revised.md`, "Succession and Continuity."
- If no viable successor: game over for that dynasty, but the world persists; a new character can
  start in the same city, in the visible legacy/ruins of the old empire. Flagged as the project's
  strongest differentiator — cheap to fake with flavor text/news/dialogue, high narrative payoff.
  — `PROJECT_CONTEXT.md`, Decision history.

## Heists

- MVP resolution is abstract-roll (crew skill, prep/intel, guard density, stealth vs. aggression,
  risk modifier → outcome table), with named variables so a later procedural mini-map can be a
  presentation layer over proven logic rather than a parallel system to debug. Greed/time/risk
  push-your-luck tension during an unfolding op is a deliberate design goal. —
  `criminal-empire-design-doc-revised.md`, "Heists"; `PROJECT_CONTEXT.md`, Decision history.

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
  `criminal-empire-design-doc-revised.md`'s Design Heuristics and
  `INFORMATION_AND_LEGIBILITY.md`'s "Anti-heat-bar tests" both cover this directly now.
