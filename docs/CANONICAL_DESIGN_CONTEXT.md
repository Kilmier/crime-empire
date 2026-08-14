# Crime Empire — Canonical Design Context

Status snapshot: 2026-08-13, after milestone 003 was completed and verified at commit `e83dacf`.
Verification took five corrective rounds, not four: review of `b8fe921` returned two further P1
defects, recorded as the fifth correction in `milestones/003-information-transmission.md`. An
earlier version of this file called `b8fe921` verified; that was wrong.

Milestone 004 (Provenance Precision) is active and unblocked: make information
provenance precise enough to distinguish participating, witnessing, discovering a trace, and being
given a first-hand account. See `CURRENT_MILESTONE.md` for its authoritative scope.

## Purpose and authority

This document is a compact context package for a design-focused workspace. It summarizes progress,
ideas, and settled choices without replacing the canonical design record.

When this summary conflicts with another document, use this authority order:

1. `DESIGN_DECISIONS.md` for settled decisions.
2. `GAME_VISION.md`, `SIMULATION_ARCHITECTURE.md`, and
   `INFORMATION_AND_LEGIBILITY.md` for the full design.
3. `OPEN_CONCERNS.md` for unresolved risks.
4. `CURRENT_MILESTONE.md` for the currently assigned scope.
5. `milestones/NNN-*.md` for completed work and appended corrections.

This is a navigation and continuity document. New durable decisions should still be written to
`DESIGN_DECISIONS.md`; new unresolved risks belong in `OPEN_CONCERNS.md`.

## Unique responsibility and maintenance

This is the **single continuity brief for the design, architecture, and ideas workspace**. It owns
the readable synthesis of:

- the current game concept and design principles;
- design progress and what has actually been validated;
- promising ideas that have not yet become requirements;
- unresolved design questions and risks;
- candidate future milestones, without selecting one automatically.

It does **not** own file-by-file implementation details, the commit ledger, regression baselines,
or the code-review checklist. Those belong in `CANONICAL_CODE_REVIEW_CONTEXT.md`.

Update this brief after a design decision is recorded, an open concern materially changes, or a
milestone changes what the project has validated. Do not use it to originate authority: first
update the appropriate canonical source, then refresh this synthesis. Label exploratory material
as an idea or candidate so it cannot be mistaken for settled design.

## High concept

Crime Empire is a persistent-world, character-driven criminal empire simulation. The player is not
managing abstract units with personalities attached; the relationship graph and the characters'
bounded decisions are the game.

The intended experience has these defining properties:

- No mandatory path and no universal win condition.
- A world that continues beyond the current player character.
- Characters act from limited information, personal motives, relationships, responsibilities, and
  available capabilities.
- NPCs operate under the same causal rules as the player without requiring the same interface,
  planning depth, or update frequency.
- Emergent events become stories only when they are visible and legible from a character's
  information—not because the player receives an omniscient event log.

## Scope discipline

Scope discipline is a standing design constraint. The original ambition—many cities, full
laundering/political/legal depth, and procedural heist maps immediately—was moved to later phases.
When scope is ambiguous, choose the smallest executable slice that can falsify the design.

The validation sequence is:

1. Simulation kernel.
2. Emergence prototype.
3. MVP vertical slice.

The first three milestones proved the kernel and the first narrow information slice of the
emergence prototype. They did not build the whole emergence prototype or begin the MVP.

## Core simulation philosophy

### Causal parity, not computational parity

Comparable actions must have comparable requirements, costs, and consequences regardless of who
acts. This does not mean every NPC receives continuous deliberation, a full planning interface, or
the same simulation depth as the player.

Simulation depth is intended to be relevance-tiered:

- **Active** characters receive the richest simulation.
- **Supporting** characters preserve meaningful causal state with less frequent work.
- **Background** characters use compressed outcomes until promoted.

The tier model is designed but not yet implemented.

### Bounded deliberation

The implemented decision pipeline is:

```text
trigger
  → update/perceive beliefs
  → select agenda
  → generate bounded candidates
  → reject unavailable candidates
  → score local utility
  → commit
  → schedule reconsideration
```

The system must be able to explain independently:

- what woke the character;
- what mattered;
- what occurred to them;
- what was actually available;
- why they preferred the chosen action.

Rejected approaches include unrestricted GOAP/general planning, continuous full deliberation for
all characters, and a universal action menu.

### Traits and causality

Traits modify perception, salience, and evaluation. They never fire actions directly. The rejected
pattern is behavior such as `Aggressive → monthly chance to attack`; that makes personality feel
like a slot machine rather than a causal influence.

The implemented closed vocabulary is:

- Traits: Aggressive, Cautious, Proud, Suspicious.
- Drives: Wealth, Status, Security, Belonging.

`Loyalty` is derived per relationship rather than stored as a universal scalar. `Ambition` is
currently represented through Status rather than duplicated as another drive. This means
`OPEN_CONCERNS.md` concern #4 is stale and should eventually be retired.

### Organizational coordination

The organization is not an omniscient hive mind and not merely a collection of unrelated agents.
The intended flow is:

```text
conditions
  → priorities and policies
  → offices and responsibilities
  → assignments
  → personal interpretation
  → action
  → source-limited reports
```

Policies influence anticipated consequences and salience; they are not hard action filters.
Characters may knowingly violate authority, conceal the violation, or reinterpret an assignment.

## Information and legibility

The information model keeps these concepts distinct:

```text
world truth
  → traces
  → observations
  → claims
  → character-relative knowledge/belief/suspicion
  → reports or messages
  → viewpoint-limited player account
```

Truth is used to resolve the world and create developer traces. It is not automatically player
knowledge. A high-confidence belief may be objectively wrong.

### Implemented information slice

Milestone 003 deliberately implemented only:

- direct, source-limited acquisition;
- one explicit organizational report/message channel;
- candid, incomplete, or false accounts as scored decisions;
- conflicting testimony retained alongside the current settled belief;
- requests for corroboration;
- a player-readable history constrained to one viewpoint character.

Generalized rumor propagation was explicitly excluded and remains unimplemented.

### Reporting design choices

- Deception is a candidate evaluated through the normal decision pipeline, not a scripted branch.
- A report is composed only from positions the sender actually has; reporting code cannot read
  authoritative truth to invent content.
- A partial report distinguishes claims that were asserted, deliberately withheld, or omitted only
  because the bounded message was full.
- Repeated identical accounts do not compound confidence.
- A source changing their account is meaningful: recantation or contradiction updates
  reconsideration and remains communicable.
- Corroboration counts distinct sources across testimony history.
- A request is scoped to a particular claim. Asking a person one question does not permanently
  close the communication channel with them.
- Asking is spent when the question is put, not when the recipient chooses to answer. This bounds
  unanswered requests without forcing a reply.

### Viewpoint design choices

Salvatore was selected as the milestone-003 viewpoint because he sets the policy, receives reports,
and must judge events without seeing the decision trace. Tommy's account was selected as the
conflicting source against Vincent's account, using the same organizational report channel.

The player-facing view:

- reads only the viewpoint character's cognition, testimony, and known relationships;
- never enumerates the authoritative global roster to reveal unknown people;
- uses qualitative confidence;
- presents conflicting accounts with attribution;
- does not expose utility scores, hidden intentions, or the authoritative truth log.

### Known provenance compromise

`SourceKind.Direct` is still too broad. It cannot distinguish:

- witnessing an event;
- participating in or authoring it;
- discovering a trace directly;
- receiving information first-hand from the participant.

Player-facing wording was made neutral so it does not invent physical attendance, but this is a
workaround rather than a full data-model solution. A provenance-precision milestone could split
participant, witness, discovery, and first-hand testimony.

## Current playable/provable scenario

The harbor scenario contains one organization, one contested district, one pressured business, and
a five-person cast. Vincent is aggressive, proud, under revenue pressure, and carrying a grievance;
whether he escalates is a scoring outcome rather than a scripted event.

The scenario variants are:

- `baseline` — Vincent as written.
- `cautious-vincent` — personality changes while the surrounding situation stays comparable.
- `watchful-boss` — the policy and Vincent's obligation to Salvatore are stronger.
- `disloyal-vincent` — Vincent owes Salvatore little and resents him.

The variants produce distinct histories, demonstrating that traits and relationships affect
behavior without directly triggering actions.

In the disloyal scenario, Salvatore can hold a source-limited inference that Vincent breached the
anti-violence policy while Vincent denies it. Observable violence and the hidden causal conclusion
that Vincent authorized it remain distinct.

## Progress by milestone

### Milestone 001 — decision-pipeline behavioral spike

Completed:

- Deterministic event queue and continuous calendar.
- Bounded character decision pipeline.
- Belief-limited perception and scoring.
- Organizational policies, priorities, offices, assignments, delegation, and commitments.
- Parameterized multi-step strategies.
- Developer decision traces.
- Harbor scenario and personality variants.
- Initial deterministic replay tests.

Important result: changing Vincent's personality changes the resulting history, while identical
inputs replay identically.

### Milestone 002 — .NET 10 migration

Completed:

- Target framework moved to `net10.0`.
- SDK pinned to `10.0.400` in `global.json` with `latestFeature` roll-forward.
- Cross-runtime comparison showed the same seeded history under .NET 9 and .NET 10 at migration
  time.

Still unverified: Godot 4 C# compatibility with this exact .NET target.

### Milestone 003 — information transmission slice

Completed and Codex-verified:

- Observation and inference remain distinct from hidden world truth.
- First-class reports, asserted and withheld claims, testimony, conflicts, and reconsideration.
- Scored candid/partial/false reporting.
- Source-limited corroboration requests.
- Viewpoint-constrained player history.
- Regression coverage for information leaks, report loops, duplicate accounts, recantation,
  request scope, pause/resume behavior, event ordering, and deterministic replay.

The milestone required four corrective rounds. The recurring lessons were:

1. A correctness fix can accidentally narrow what the system can express.
2. A correctness fix can collapse distinct states into one.
3. Tests must exercise the production rule, not a copied predicate in the test.
4. Behavioral budgets must measure the unit that can actually run away—reports as well as total
   decisions.

## Settled broader design choices

### Succession and persistence

If the player character dies or is incapacitated and a viable heir or sufficiently loyal capo
exists, control passes to that character. The successor has independent personal stats and inherits
the organization, territory, relationships, and standing—not the predecessor's skill sheet.

If there is no viable successor, that dynasty ends, but the world persists. A new character may
begin in the same city amid the visible legacy or ruins of the prior empire.

### Heists

MVP heists use abstract resolution driven by crew skill, preparation, intelligence, guard density,
approach, and risk. Named variables preserve a path to a later procedural tactical presentation
without building a second independent simulation. Push-your-luck tension during an unfolding job is
intentional.

### Technology

- Simulation: engine-independent C# classes.
- Runtime target: .NET 10 LTS.
- Persistence: SQLite, chosen for explainability queries and tier promotion/demotion state.
- Engine: Godot 4 with C#, after the simulation earns an interface.
- Current sequencing: headless simulation before rendering.

## Open concerns and deferred choices

### Still genuinely open

- **Tuning:** believable behavior depends on weights, candidate limits, thresholds, and trigger
  sparseness. Architecture alone cannot settle this.
- **Tiered calendar engineering:** promotion/demotion combined with discrete events under a
  continuous calendar is expected to be difficult and must be budgeted separately.
- **Relationship schema:** trust, fear, affection, resentment, attraction, and obligation still do
  not have a full update/decay/data-model treatment comparable to the information subsystem.
- **Provenance precision:** the current neutral wording avoids false claims, but the source model is
  underspecified.
- **Conflict by omission:** the system recognizes explicit disagreement. Whether one source's
  assertion against another source's conspicuous omission should count as first-class conflict is
  still a design question.
- **Godot compatibility:** Godot 4 C# compatibility with `net10.0` remains unverified.
- **Persistence:** SQLite is selected but not implemented.
- **Tiering:** relevance tiers are designed but not implemented.

### Stale concern to clean up

`OPEN_CONCERNS.md` #4 says the trait vocabulary is not closed. The code and milestone 001 already
closed it. This should be moved to the resolved decision record during an appropriate docs pass.

## Candidate scopes for milestone 004 and beyond

**Selected for milestone 004: option 1, provenance precision.** Approved by Matt and scoped in
`CURRENT_MILESTONE.md`, which is authoritative. Options 2–5 remain candidates for later milestones
and must not be inferred as next — confirm scope with Matt rather than reading an order into this
list.

1. **Provenance precision** — split the broad `Direct` source into meaningful acquisition modes.
   Small, understood, and directly connected to several review findings. *Now milestone 004.*
2. **Relationship design pass** — resolve the relationship schema before implementing richer
   relationships and grievances.
3. **Persistence/SQLite** — begin storing the information and decision data now worth querying.
4. **Godot/.NET compatibility spike** — cheaply settle an engine constraint before UI work.
5. **Another bounded emergence slice** — delegation, rival activity, or limited tier transitions,
   but not the entire remaining emergence prototype in one milestone.

## Design guardrails for future work

- Do not let traits trigger actions.
- Do not let decisions read authoritative truth instead of character-relative information.
- Do not make policies mechanically binding.
- Do not convert uncertainty into one universal heat/attention scalar.
- Do not introduce unrestricted planning because one bounded strategy is missing.
- Do not merge observation, inference, testimony, rumor, and evidence.
- Do not treat silence, withholding, denial, and message truncation as the same state.
- Do not treat a question as a permanent relationship-level communication lock.
- Do not begin all of the remaining emergence prototype as one milestone.

## Useful design review questions

For any proposed feature, ask:

1. What woke the actor?
2. What information did they actually possess?
3. What occurred to them, and why?
4. What was available, and what was ruled out?
5. How did traits, drives, relationships, commitments, and policy affect evaluation without firing
   the action directly?
6. What trace did the outcome leave?
7. Who can observe that trace, under what conditions?
8. How can the player learn it without receiving omniscient truth?
9. What distinct states might this implementation accidentally collapse?
10. What honest behavior might a proposed safety/correctness filter accidentally remove?
