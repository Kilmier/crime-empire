# Crime Empire Design Proposal Index

**Status: noncanonical intake and reconciliation index.** Nothing in this directory is a settled
design decision, an active milestone, or permission to implement. Canon remains in the documents
named by `AGENTS.md`; active scope exists only in `docs/CURRENT_MILESTONE.md`.

## Purpose

This directory preserves design ideas that are worth evaluating without turning conversation history
or a polished consolidation into a competing source of truth. Read only the proposal relevant to the
scope being considered. If Matt accepts a durable decision, update its canonical source first. If he
authorizes implementation, put the bounded scope in `CURRENT_MILESTONE.md`.

The source reviewed on 2026-09-11 was *Crime Empire Design Consolidation*, reconstructed from the
ChatGPT conversation *AI Collaboration in Game Dev*. The source remains provenance outside the
repository. This index maps it without copying the thousand-line reconstruction into `docs/`.

## Four statuses

- **ALREADY CANONICAL** — the consolidation restates current canon; do not duplicate it here.
- **CANONICAL, NOT YET IMPLEMENTED** — the direction is settled, but `ROADMAP.md` or the current code
  shows that the behavior is still absent or partial.
- **PROPOSAL — NEEDS HUMAN RULING** — compatible idea worth preserving, with no authority yet.
- **SUPERSEDED OR STALE** — a later decision, milestone, or current-status document is more precise.

A section can contain more than one status. The table splits mixed sections rather than pretending
one label governs every sentence.

## Consolidation map

| Consolidation section | Status | Authoritative destination or proposal | Reconciliation note |
|---|---|---|---|
| 1–2. Purpose and status tags | SUPERSEDED OR STALE | This index; `AGENTS.md` | The reconstruction remains provenance, not a promotable authority document. |
| 3. Executive design statement | ALREADY CANONICAL | `GAME_VISION.md` | Seven pillars and the core fantasy already exist in canon. |
| 4. Core fantasy, tone, player identity | ALREADY CANONICAL | `GAME_VISION.md` | No fixed campaign, player-as-character, and tone are settled. |
| 5. Foundational principles | ALREADY CANONICAL | `GAME_VISION.md`; `SIMULATION_ARCHITECTURE.md`; `DESIGN_DECISIONS.md` | Problems-not-quests, causal parity, authority/capability, delegation, optionality, and playable failure are settled. |
| 6. Shared character model and relevance tiers | ALREADY CANONICAL | `GAME_VISION.md`; `SIMULATION_ARCHITECTURE.md` | Relevance tiering remains unimplemented. |
| 6. Relationship dimensions | SUPERSEDED OR STALE | `DESIGN_DECISIONS.md`; `RELATIONSHIPS.md`; `OPEN_CONCERNS.md` | Executable vocabulary is Trust, Fear, Obligation, and relationship-keyed Grievances; other dimensions need a reader. |
| 6. Personality expansion | PROPOSAL — NEEDS HUMAN RULING | `PERSONALITY_AND_CHARACTER_PROFILES.md` | The current four-trait vocabulary remains canonical until a tested extension is accepted. |
| 7. Truth, trace, observation, claim, belief, rumor, and evidence | ALREADY CANONICAL | `INFORMATION_AND_LEGIBILITY.md`; `DESIGN_DECISIONS.md` | The reconstruction understates how much Claim/Cognition/Testimony/provenance behavior is already implemented. Attribution is a claim or inference, not a mandatory additional information state. |
| 7. Unrecognized observations gaining later significance | PROPOSAL — NEEDS HUMAN RULING | `INSTITUTIONS_INVESTIGATIONS_AND_COOPERATION.md` | Compatible with the current claim/inference model, but not explicitly settled. Evidence may exist before attribution and remains audience-relative. |
| 7. Fragmented institutional knowledge and sharing | PROPOSAL — NEEDS HUMAN RULING | `INSTITUTIONS_INVESTIGATIONS_AND_COOPERATION.md` | Actor-relative information implies compatibility, but canon does not yet specify unit, office, case, or institution knowledge ownership. |
| 7. Full surveillance environment | PROPOSAL — NEEDS HUMAN RULING | `INSTITUTIONS_INVESTIGATIONS_AND_COOPERATION.md`; `ROADMAP.md` | Current code has observations, claims, testimony, and evidence-shaped information; broad surveillance channels remain out. |
| 8. Organizations as networks and offices | ALREADY CANONICAL | `GAME_VISION.md`; `SIMULATION_ARCHITECTURE.md` | Current fixture has only one small organization; wider organization behavior remains unvalidated. |
| 8. Alliances, coalitions, archetypes, culture, territory delegation | PROPOSAL — NEEDS HUMAN RULING | `ORGANIZATIONS_ALLIANCES_AND_CULTURE.md` | Preserve as future candidates; no automatic rubber-band coalition rule. |
| 8. Cross-institutional power, corruption, and alternate origins | CANONICAL, NOT YET IMPLEMENTED | `DESIGN_DECISIONS.md` §Player-neutral architecture; `ROADMAP.md` | Existing relationship/information systems are the required foundation; playable alternate careers remain long-term. |
| 9. Criminal origins, on-ramps, crime categories, and strategic functions | PROPOSAL — NEEDS HUMAN RULING | `CRIME_AND_LOCATION_OPERATIONS.md` | Origins are initial conditions rather than classes; orders, jobs, favors, opportunities, and pressures are proposal vocabulary, not an approved feature list or quest chain. |
| 9. Clean/dirty money and legitimate businesses | CANONICAL, NOT YET IMPLEMENTED | `GAME_VISION.md`; `ROADMAP.md` | The broad distinction is settled; provenance depth remains deferred. |
| 10. Location operations and top-down maps | PROPOSAL — NEEDS HUMAN RULING | `CRIME_AND_LOCATION_OPERATIONS.md` | MVP heists remain abstract; a shared concept need not become a new type until behavior proves it. |
| 11. Actor-neutral cases, attention/evidence distinction, cooperation | CANONICAL, NOT YET IMPLEMENTED | `GAME_VISION.md`; `INFORMATION_AND_LEGIBILITY.md`; `ROADMAP.md` | Cooperation is already explicit canon: an arrested character decides from personal pressure and can reveal only what they know or plausibly believe. The detective spike is not a complete case or institution system. |
| 11. Institutional authorization gates and detailed prosecution/defense procedure | PROPOSAL — NEEDS HUMAN RULING | `INSTITUTIONS_INVESTIGATIONS_AND_COOPERATION.md` | General authority-is-not-capability is canonical; who authorizes which action from what institutional information is not. |
| 12. State/city hierarchy and simulation scale | PROPOSAL — NEEDS HUMAN RULING | `WORLD_ERAS_AND_DYNASTY.md` | State scale and LOD are long-term directions, not a current mandatory hierarchy. |
| 13. Continuous calendar and succession | ALREADY CANONICAL | `GAME_VISION.md`; `SIMULATION_ARCHITECTURE.md`; `DESIGN_DECISIONS.md` | Current prototype implements only part of the eventual persistence/failure arc. |
| 13. 1910s start and changing technology across eras | PROPOSAL — NEEDS HUMAN RULING | `WORLD_ERAS_AND_DYNASTY.md` | Exact start, duration, and historical abstraction are not settled. |
| 14. Crime-first, perspective-extensible architecture | ALREADY CANONICAL | `DESIGN_DECISIONS.md` §Player-neutral architecture | It is a standing constraint, not authorization for alternate careers. |
| 15. UI direction | PROPOSAL — NEEDS HUMAN RULING | `UI_AND_PLAYER_LEGIBILITY.md` | Information-boundary rules are canonical; screen family, news, conversations, and map treatment are proposals. |
| 16. Headless core and engine boundary | ALREADY CANONICAL | `AGENTS.md`; `DESIGN_DECISIONS.md`; source project boundaries | Godot and SQLite replay save/load now exist; the reconstruction is stale as progress reporting. |
| 17. Scope guardrails | ALREADY CANONICAL | `AGENTS.md`; `GAME_VISION.md`; `ROADMAP.md` | The current demo arc, not the reconstruction, controls sequencing. |
| 18. Long-term directions | PROPOSAL — NEEDS HUMAN RULING | Relevant focused proposals; `GAME_VISION.md` Long-Term Vision | Preserve possibilities without designing compatibility layers ahead of evidence. |
| 19. External references | PROPOSAL — NEEDS HUMAN RULING | Relevant focused proposal only | References are evidence and inspiration, never requirements. |
| 20. AI collaboration and review | SUPERSEDED OR STALE | `AGENTS.md`; `REVIEW_LEDGER.md`; milestone archives | Do not create a second review queue or standing permission to bypass the review gate. |
| 21. Reviewer checklist | SUPERSEDED OR STALE | `AGENTS.md`; repository review skills; `REVIEW_LEDGER.md` | Keep one maintained checklist surface. |
| 22. Anti-patterns | ALREADY CANONICAL | `AGENTS.md`; canon docs; `DESIGN_DECISIONS.md` | Useful summary, but duplicated rather than new. |
| 23. Open questions | SUPERSEDED OR STALE | `OPEN_CONCERNS.md`; focused proposals | Several listed questions have already been answered; unresolved proposal choices live beside their proposal. |
| 24. Canonization process | SUPERSEDED OR STALE | `AGENTS.md`; this index | Promote accepted decisions into existing sources; never promote the consolidation wholesale. |
| 25. North-star checklist | ALREADY CANONICAL | `GAME_VISION.md` Design Heuristics; `AGENTS.md` | Retain the canonical checklist rather than another copy. |

## Later proposal additions

These ideas were preserved after the 2026-09-11 consolidation review and therefore are not rows from
that source:

| Discussion | Status | Destination | Reconciliation note |
|---|---|---|---|
| Procedural city generation and urban ecology | PROPOSAL — NEEDS HUMAN RULING | `PROCEDURAL_CITY_AND_URBAN_ECOLOGY.md` | Generate one persistent city by constrained deterministic composition of authored physical, economic, institutional, organizational, historical, and information-bearing components. Prove load-bearing differences with authored fixtures before building the generator. |
| Narrative interpretation, dramatic rendering, and the Bellini collection scenario | PROPOSAL — NEEDS HUMAN RULING | `NARRATIVE_INTERPRETATION_AND_DRAMATIC_PRESENTATION.md` | Preserve the boundary that simulation authors reality while narrative authors expression; use the Bellini scenario to test actor-relative decisions, uncertain protection claims, causal consequences, and deterministic rendering without authorizing roadmap work. |
| Planning, exposure recognition, and unfinished aftermath | PROPOSAL — NEEDS HUMAN RULING | `CRIME_AND_LOCATION_OPERATIONS.md` | Tentative hypothesis: preparation may reduce expected exposure, participants may notice different complications, and unresolved traces or witnesses may matter later. This is not a universal cleanup phase or permission to erase evidence. |
| Cyberpunk conspiracy-era expansion | PROPOSAL — NEEDS HUMAN RULING | `WORLD_ERAS_AND_DYNASTY.md` | Long-term DLC idea: era-appropriate crime, surveillance, cult-shaped cross-institutional influence, and noir investigation using the same actor-relative information and organization systems; not a roadmap commitment. |
| Legitimate fronts and criminal logistics | PROPOSAL — NEEDS HUMAN RULING | `LEGITIMATE_FRONTS_AND_CRIMINAL_LOGISTICS.md` | Preserve separate production-cover and distribution-capacity problems; concrete business capabilities may offer options and create exposure. No universal front requirement or economy milestone is authorized. |

## Focused proposals

- `PERSONALITY_AND_CHARACTER_PROFILES.md` — two-major/two-minor profiles and a small proposed trait
  extension, preserving the rule that traits shape choices rather than fire actions.
- `UI_AND_PLAYER_LEGIBILITY.md` — strategic, people, and situation views; chronicle; news; private and
  group conversations; viewpoint-relative hierarchy.
- `ORGANIZATIONS_ALLIANCES_AND_CULTURE.md` — archetypes, culture and subcultures, diplomacy,
  cross-institutional relationships, and territory delegated to trusted people.
- `INSTITUTIONS_INVESTIGATIONS_AND_COOPERATION.md` — fragmented institutional knowledge,
  authorization requests, later reinterpretation of old observations, and the canonical cooperation
  behavior those proposed systems must preserve.
- `CRIME_AND_LOCATION_OPERATIONS.md` — criminal origins and on-ramps, quest-shaped in-world
  commitments, non-global standing and progression, expanded crime vocabulary, the shared operation
  direction, the tentative planning/exposure/unfinished-aftermath hypothesis, and the rule for when a
  top-down map earns its cost.
- `PROCEDURAL_CITY_AND_URBAN_ECOLOGY.md` — one persistent generated city, constrained composition of
  authored components, urban causal layers, character-relative starting knowledge, scope guardrails,
  and the authored-fixture proof required before procedural implementation.
- `NARRATIVE_INTERPRETATION_AND_DRAMATIC_PRESENTATION.md` — simulation-authoritative speech and
  events, viewpoint-bounded rendering contracts, the Bellini collection and protection scenario,
  circumstance-led variation, and proposed automated validation.
- `LEGITIMATE_FRONTS_AND_CRIMINAL_LOGISTICS.md` — legitimate enterprises as production cover and
  distribution capacity, with ownership, partnership, exposure, and scale left open for design.
- `WORLD_ERAS_AND_DYNASTY.md` — 1910s-start proposal, persistent generations, and changing technology
  and surveillance across decades.

## Authorization boundary

This index deliberately carries no current status. Read `CURRENT_MILESTONE.md` for what is active and
the relevant milestone archive for completed history. Inclusion here never places a proposal into the
current or next milestone.
