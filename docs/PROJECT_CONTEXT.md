# Criminal Empire — Project Context

Purpose of this file: give a new coding session the background it needs, without re-deriving decisions already made in design chat. Treat the canonical documents in this directory as authoritative; treat this file as the narrative of *why* they say what they say. It is not the handoff surface — `CURRENT_MILESTONE.md` is.

Primary companion documents: `GAME_VISION.md` (vision), `SIMULATION_ARCHITECTURE.md` (architecture), `INFORMATION_AND_LEGIBILITY.md` (information model), `DESIGN_DECISIONS.md` (settled decisions), and `OPEN_CONCERNS.md` (unresolved risks).

## What this project is

A persistent-world, character-driven criminal empire simulation. Solo hobby project, built for love of crime cinema (Sopranos, Breaking Bad, Ozark, Heat) crossed with grand-strategy-style systemic character simulation (CK3 is the closest reference point). 16-bit presentation, top-down city/district maps, eventual topdown heist mini-maps. Not a management-sim with NPCs bolted on — the relationship graph and character decision-making *are* the game.

## Five (now seven) Design Pillars
1. **Freedom** — no mandatory path to power, no single win condition.
2. **Interconnection** — systems constantly affect each other, no isolated minigames.
3. **Emergence** — "generate problems, not quests." Stories come from character motives + systems colliding, not authored plot.
4. **Persistence** — the world remembers and keeps changing during death, prison, absence.
5. **Uncertainty** — imperfect information for player and NPCs alike.
6. **Shared Agency / Actor Parity** — player is one character among many operating under the same causal rules, not a unique-verb-set management god. Capability parity, not identical roles.
7. **Playable Failure** — arrest, imprisonment, betrayal, loss of territory should usually produce a new chapter, not a game over. Hard endings (death with no heir) remain possible.

Full detail lives in the vision doc — this is the compressed version.

## Decision history (why the docs say what they say)

- Started as a generic 16-bit business-sim idea, evolved into a criminal-empire concept once the person cited Breaking Bad/Ozark/Sopranos and Torn/Drug Wars as references.
- Scope was walked back repeatedly: original ambition included a 20-city state, full laundering/politician/lawyer depth, procedural heist maps day one. All of that was correctly identified as v2+/long-term vision, not v1. **This scope discipline is a recurring failure mode to watch for** — the person (and any collaborating tool) tends toward maximalist scope and needs to be pulled back toward "smallest thing that tests the core loop."
- Succession/persistence model settled early: on player-character death/incapacitation, if there's a viable heir (child) or sufficiently loyal capo, the player transitions to controlling that character. New POV character has **independent stats**, inherits territory/relationships/standing (CK3 model) — deliberately not a stat-sheet reskin, so successions produce real variance (competent-but-untrusted heir vs. loved-but-mediocre capo). If no viable successor: game over for that dynasty, but the world persists and the player can start a new character in the same city, in the visible ruins/legacy of the old empire. This "new character in a world that remembers the old one" branch was flagged as the project's strongest differentiator — cheap to fake with flavor text/news/NPC dialogue, high narrative payoff.
- Heists: abstract-roll resolution (crew skill, prep/intel, guard density, stealth vs. aggression, risk modifier → outcome table) is the MVP approach. Named variables now, so a later topdown procedural mini-map can be a presentation layer over proven logic rather than a parallel system to debug. Greed/time/risk push-your-luck tension during an unfolding op is a deliberate design goal (Ocean's Eleven / Heat framing).
- Actor parity was identified as the single most expensive commitment in the vision doc — if taken literally (every character fully deliberating), it's an unaffordable general-agent simulation. The **Simulation Architecture doc exists specifically to resolve this** via "causal parity, not computational parity" — full causal consistency, rationed deliberation depth via a 3-tier relevance-budgeted system (Active / Supporting / Background).
- Rejected explicitly in the architecture doc, on purpose: unrestricted GOAP/general planning, continuous deliberation for all characters, identical AI/player interfaces, minute-resolution updates during fast-forward. These were deliberate cuts, not oversights — don't reintroduce them without a concrete demonstrated need.
- Traits/personality must modify perception, salience, and evaluation — never fire actions directly (explicitly rejected pattern: `Aggressive → monthly chance to attack`). This is treated as the most important single anti-pattern in the whole design; violating it is the fastest way to make characters feel like slot machines instead of motivated people.

## Historical concerns that motivated the current architecture

This list explains what drove the architecture work. For current status, use `OPEN_CONCERNS.md`;
several items below have since received a design answer but still require executable validation.

Ranked by severity:
1. **No plan yet for how NPC-driven action becomes visible to the player as story.** The architecture doc solves tractability, not player-facing legibility. A scheme that resolves only in an internal decision trace might as well not have happened. Needs a surfacing layer: rumor, news, informant reports, dialogue.
2. **All tuning (weights, candidate-set sizes, thresholds) is deferred to prototyping**, correctly — but this means neither doc is evidence the resulting behavior will feel good. The architecture is a map to the starting line, not proof the race is winnable.
3. **"How organizations assign goals without every member deliberating independently"** is currently an open question in the architecture doc but is load-bearing for faction behavior feeling organizational rather than like N independent agents. Should be an early prototyping target, not a backlog item.
4. Character schema (identity/capabilities/psychology/cognition/social/motivations/execution) is a shape, not a real schema yet — `knowledge` especially needs to implement the Truth/Knowledge/Belief/Rumor/Evidence distinction concretely.
5. Trait/value list is currently open-ended in the docs; should be closed to a small, fixed, tunable set before implementation (earlier drafts used Loyalty/Greed/Fear/Ambition as a 4-stat starting point).

## Stack decision and implementation status

- **Simulation core:** C#, plain classes, engine-agnostic and unit-testable from the command line. The first headless behavioral spike now exists in `src/CrimeEmpire.Simulation/`, with its console host in `src/CrimeEmpire.Runner/`.
- **Persistence (planned, not implemented):** SQLite (not JSON/binary blobs) — chosen specifically because the architecture doc's explainability requirements and the promotion/demotion tiering need real queries ("show every decision X made and why," "which dormant Tier 2 characters have unresolved grievances against active characters").
- **Rendering/engine (planned, not implemented):** Godot 4, using C# (not GDScript) — same language as the sim core, no FFI boundary. Chosen over Unity for licensing simplicity, strong 2D/tilemap support, and a UI toolkit suited to a text/menu-dense management game rather than an action game.
- **Sequencing remains explicit:** validate the headless simulation and its traces before creating a Godot project. If the sim is not compelling in text form, presentation cannot repair its foundation.

## Where to find current status

This file is narrative background and deliberately holds no status. What is being worked on is in
`CURRENT_MILESTONE.md` and nowhere else; completed work is in `docs/milestones/`; unbuilt work and
candidate scopes are in `ROADMAP.md`; review coverage is in `REVIEW_LEDGER.md`.

An earlier version of this section carried an "Immediate next step" describing the milestone-001
task as though it were still pending. It was three milestones stale by the time anyone noticed,
which is the argument for keeping status in one file.

## Working style notes for the coding agent

- The person wants direct, opinionated engagement — flag design/architecture problems plainly rather than deferring everything back as a question, but implementation-level ambiguity should default to the choices already recorded in this file and the two attached docs rather than re-litigating settled decisions.
- Scope discipline is an ongoing concern for this project specifically — when in doubt, build the smaller/testable version first.
- Anti-patterns to actively avoid, per the architecture doc: trait-fires-action-directly logic, unrestricted planning/GOAP, giving every character full deliberation depth regardless of narrative relevance.
