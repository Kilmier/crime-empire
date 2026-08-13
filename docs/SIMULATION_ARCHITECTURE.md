# Criminal Empire — Simulation Architecture

## Purpose

This document defines how the game can produce a persistent, character-driven crime simulation without requiring every person to operate as a continuously thinking general-purpose agent.

It translates the vision's promises—causal actor parity, imperfect information, persistent consequences, playable failure, delegation, and continuous calendar time—into a tractable simulation model.

Exact formulas, thresholds, population limits, and evaluation frequencies remain provisional until tested. The architecture establishes responsibilities and invariants; implementation and balancing determine the numbers.

## Architectural Principles

### Causal Parity, Not Computational Parity

Comparable actions use comparable requirements, costs, resolution rules, information exposure, and consequences regardless of whether the player or an NPC performs them.

Parity does not require identical interfaces, decision-search breadth, planning resolution, update frequency, or simulation depth. NPCs need credible access to actions relevant to their circumstances, not continuous access to the entire global action catalogue.

### Bounded Cognition

No character continuously evaluates every action. A character considers a small candidate set generated from their role, goals, responsibilities, commitments, knowledge, relationships, pressures, and recent events.

### Belief-Based Decisions

Characters decide from what they know, believe, suspect, or misunderstand. Objective world state determines actual consequences; perceived world state determines expected consequences.

### Commitment Before Constant Replanning

Characters normally continue an intention, assignment, or strategy until it completes, fails, becomes impossible, or receives a meaningful interrupt. Switching has a cost. Small score fluctuations should not cause erratic behavior.

### Event-Driven Time

The calendar advances through scheduled events, ongoing processes, deadlines, interrupts, and periodic reviews. Empty time is skipped. Fine temporal resolution is local to situations that need it.

### Relevance-Budgeted Detail

Simulation fidelity is allocated according to causal relevance. Promotion increases decision and state detail; demotion reduces update detail without erasing facts needed for future consequences.

### Observable Causality

Consequential actions must produce not only state changes but also potential observable traces. The player experiences emergence through observation, reports, rumors, evidence, behavior, and changing world state—not through hidden decision records or omniscient event announcements. Internal causality and player-facing accounts are related but deliberately distinct.

> **Simulate causality at full fidelity; simulate deliberation only as deeply as gameplay can observe or affect it.**

## Character Decision Model

NPC behavior uses a hybrid model:

> **Events and commitments determine when a character thinks. Goals and context generate a bounded candidate set. Belief-based utility selects among those candidates.**

The game does not use unrestricted general-purpose planning for the MVP.

### Decision Pipeline

```text
Trigger or scheduled review
        ↓
Update knowledge, beliefs, pressures, and commitments
        ↓
Select the agenda, responsibility, or urgent need requiring attention
        ↓
Generate a small context-relevant candidate set
        ↓
Reject unknown, impossible, or unavailable candidates
        ↓
Score candidates using the character's perceived situation
        ↓
Choose and commit to an action or parameterized strategy
        ↓
Schedule steps, completion events, and reconsideration triggers
```

This pipeline separates five questions:

1. What caused the character to reconsider?
2. What currently matters to them?
3. Which possibilities occur to them?
4. Which of those possibilities are available?
5. Which available option do they prefer?

## When Characters Deliberate

Decision evaluation occurs because of a trigger, not because every simulated minute demands a universal AI update.

Typical triggers include:

- receiving an order, offer, threat, request, or new information;
- beginning, completing, or failing an assignment;
- an existing strategy becoming blocked or invalid;
- a deadline or scheduled meeting arriving;
- arrest, attack, exposure, betrayal, loss, or another urgent event;
- a relevant opportunity becoming known;
- a pressure such as debt, fear, resentment, or legal exposure crossing a meaningful threshold;
- an important relationship or organizational role changing;
- a periodic role or agenda review becoming due.

Triggers should be specific enough to explain why a decision occurred and sparse enough to keep fast-forward efficient.

## What Drives Decisions

### Values and Drives

Broad preferences that make outcomes more or less desirable, such as:

- wealth;
- status;
- security;
- family;
- loyalty;
- autonomy;
- revenge;
- legitimacy;
- belonging;
- excitement or restraint.

Values are weights and tendencies, not self-executing goals.

### Ambitions

Long-term desired states such as leading an organization, retiring safely, becoming wealthy, winning office, protecting a family, destroying a rival, or escaping a criminal past.

### Responsibilities

Role-based duties that repeatedly demand attention: lead a crew, operate a business, investigate a case, protect a person, supervise a district, or manage an organizational office.

### Commitments

Promises, orders, active operations, deals, scheduled meetings, and ongoing assignments. Commitments create continuity and social consequences for abandonment.

### Pressures

Problems that grow or become urgent: debt, prosecution risk, fear, resentment, financial need, family conflict, organizational instability, injury, or exposure.

### Immediate Needs

Short-horizon concerns such as escaping danger, responding to questioning, finding shelter, warning an ally, or stabilizing a failing operation.

## Candidate Generation

The shared action vocabulary does not become a universal menu for every NPC decision. Candidate actions are proposed by limited generators:

- current intention or commitment: continue, alter, delegate, postpone, or abandon;
- role and responsibility: perform expected duties or respond to failure;
- active ambition or pressure: pursue a relevant opportunity or reduce a threat;
- recent trigger: respond directly to the event;
- relationship: seek help, reward, protect, deceive, avoid, pressure, or retaliate;
- known opportunity: act on something the character actually knows or believes;
- strategy state: execute the next valid step or reconsider the strategy.

Generators produce actions or strategies with plausible targets. Capability and access checks then reject candidates the character cannot currently perform.

An active character should generally compare only a handful of meaningful candidates. The exact target range will be determined through profiling and behavioral testing.

### Salience

Traits and circumstances affect which candidates occur to a character before utility scoring. An aggressive character may notice coercive strategies more readily; a cautious character may notice concealment, delay, or delegation. This prevents every personality from considering the same options and merely assigning them slightly different scores.

## Local Utility Selection

Utility scoring chooses among the bounded candidates. It is not a global search over all possible actions.

A conceptual score may consider:

```text
perceived goal progress
+ responsibility or order compliance
+ relationship effects
+ personality and value alignment
+ expected reward
+ urgency
+ continuation or commitment value
- perceived personal risk
- resource and time cost
- legal and information exposure
- moral or emotional reluctance
- uncertainty
- switching and opportunity cost
```

Inputs use the character's perceptions. Traits may change salience, estimated outcomes, or weights. Skills affect perceived and actual capability but do not independently create desire.

The scorer should support understandable post-hoc explanations for debugging, such as:

```text
Vincent continued the harbor intimidation strategy because:
- it advanced his assigned responsibility;
- abandoning it would cost status;
- he believed resistance was weak;
- his aggression reduced the perceived cost of escalation;
- he did not know police surveillance had begun.
```

Controlled noise may break near-ties or represent inconsistency, but randomness should not substitute for motivation.

## Behavioral Layers

### Reactive Layer

Handles urgent, short-horizon responses such as fleeing, defending, calling for help, lying during questioning, or accepting an expiring offer.

### Commitment Layer

Maintains coherent ongoing behavior: operate a business, follow an order, continue surveillance, attend a meeting, hide after a crime, or fulfill an organizational responsibility.

### Deliberative Layer

At meaningful decision points, selects which agenda requires attention, generates relevant candidates, evaluates them, and commits to an action or strategy.

### Strategy Layer

Executes multi-step intentions through authored but parameterized procedures. Examples include:

- cultivate an informant;
- expand into a district;
- conceal a crime;
- investigate a suspect;
- undermine a superior;
- recruit an inside partner;
- prepare and perform a robbery.

Strategies contain steps, targets, resource requirements, decision points, completion conditions, and interrupts. Their steps invoke shared underlying actions and may be delegated.

## Organizational Intent and Coordination

An organization is neither an omniscient agent nor a collection of unrelated individual agents. Coordination flows through institutional state and character-held offices:

```text
organizational conditions and pressures
                ↓
leadership priorities and policies
                ↓
office responsibilities and constraints
                ↓
delegated objectives and assignments
                ↓
individual interpretation and strategy
                ↓
reports, consequences, and revised conditions
```

### Organizational Conditions

Organizations expose shared conditions such as revenue loss, territorial pressure, leadership instability, legal exposure, resource shortages, internal conflict, and external opportunities. These are inputs to leadership decisions, not desires belonging to every member.

### Priorities and Policies

Authorized leaders establish a small number of current priorities and policies. A priority states a desired organizational outcome; a policy establishes boundaries or preferences such as avoiding public violence or protecting a particular business.

Policies affect candidate salience, evaluation, and anticipated consequences. They do not make violations mechanically impossible.

### Offices and Responsibilities

Roles translate priorities into bounded areas of responsibility. A capo may be responsible for harbor revenue; a detective supervisor may allocate staff across cases; a business manager may be responsible for cash flow. Officeholders generate strategies within that domain rather than reconsidering the entire organization's future.

### Assignments and Interpretation

Assignments carry an objective, issuer, recipient, disclosed information, resources, constraints, authority, and deadline. Recipients interpret assignments using their own beliefs and motives. They may comply, negotiate, delay, exceed authority, conceal results, redirect resources, or refuse.

### Personal Agendas and Factions

Characters retain private ambitions, loyalties, grievances, and informal relationships. Factions emerge when several characters coordinate around interests or leaders that differ from formal policy. Organizational alignment changes utility and access; it does not replace individual cognition.

Organization-to-office-to-character assignment is a load-bearing target for the earliest behavioral prototype. The prototype must demonstrate coordinated action without either hive-mind omniscience or independent-agent chaos.

## Why the MVP Does Not Use Unrestricted Planning

A general goal-stack or GOAP system would require every action to expose formal preconditions and effects across social, informational, organizational, and physical state. The world is partially observed, outcomes are uncertain, and other characters continuously invalidate plans.

For the MVP, parameterized strategies provide coherent multi-step behavior with clearer authorial control, lower computational cost, and substantially better debugging. Planning depth can expand later if testing reveals a concrete need; it is not assumed as a foundation.

## Character Decision Data

The Character model must distinguish capability, motivation, cognition, social context, and execution state.

```text
Character
├── identity and history
├── capabilities
│   ├── skills
│   ├── resources
│   ├── access and contacts
│   └── formal authority
├── psychology
│   ├── traits
│   ├── values and drives
│   ├── risk tolerance
│   └── behavioral tendencies
├── cognition
│   ├── knowledge
│   ├── beliefs and suspicions
│   ├── confidence and source
│   └── perceived threats and opportunities
├── social state
│   ├── relationships
│   ├── affiliations
│   ├── obligations
│   └── grievances
├── motivations
│   ├── ambitions
│   ├── responsibilities
│   ├── pressures
│   └── immediate needs
└── execution state
    ├── current intention
    ├── active commitments
    ├── current strategy and step
    ├── deadlines
    └── reconsideration triggers
```

Important distinctions:

- **Capability** answers whether and how well a character can act.
- **Knowledge and belief** determine which actions and targets can be considered and how outcomes are estimated.
- **Motivation** determines which outcomes matter.
- **Authority** affects salience and social consequences but does not make disobedience physically impossible.
- **Intention** records what the character has chosen to pursue.
- **Commitment** supplies continuity and a cost for changing course.
- **Strategy** organizes a multi-step attempt without requiring free-form planning.

The tree above is an architectural shape, not a final storage schema. Knowledge, relationships, evidence, memories, and obligations require explicit subsystem models. Their initial schemas should remain minimal until the behavioral prototype demonstrates which distinctions actually change decisions. The canonical information model is developed in `INFORMATION_AND_LEGIBILITY.md`.

## Traits and Causality

Traits should modify perception, salience, evaluation, execution, and reaction. They should not usually fire actions directly.

Avoid:

```text
Aggressive → monthly chance to attack
Ambitious → monthly chance to betray
```

Prefer:

```text
Aggressive:
- coercive candidates become more salient;
- violence appears more likely to succeed;
- escalation costs are perceived as lower;
- slow strategies lose value more quickly.
```

An attack still requires a motive, trigger, known target, opportunity, capability, and decision. Ambition makes advancement valuable; it does not create betrayal without a perceived route, sufficient incentive, and tolerable risk.

The prototype trait and value vocabulary must be closed, small, enumerable, mechanically distinct, and data-driven. Every entry needs a stated behavioral purpose. Synonymous traits that change the same candidates or weights should be merged, and traits that fail to produce meaningfully different behavior should be removed.

Not every familiar concept belongs in the same category. Ambition may be a durable drive, fear may be threat- or relationship-specific, and loyalty may be derived from attachment, obligation, identity, trust, and expected consequences. The prototype should test these distinctions instead of prematurely reducing them to interchangeable scalar stats.

## Simulation Relevance Tiers

### Tier 1 — Active Characters

Active characters receive the fullest decision model:

- detailed goals, pressures, commitments, and intentions;
- source-aware knowledge and beliefs;
- relationship-aware candidate generation and utility;
- persistent strategy execution;
- event-driven reactions and scheduled reviews.

The tier has a strict population budget. Expected occupants include the player, immediate associates, active rivals, important family, lead investigators, consequential officeholders, and people involved in current crises.

### Tier 2 — Persistent Supporting Characters

Supporting characters retain identity and causally important state but use cheaper behavior:

- simplified traits, motives, and beliefs;
- one primary agenda or assignment;
- role-specific candidate templates;
- fewer candidates and less frequent reviews;
- compressed relationship and strategy evaluation.

They still obey shared action requirements and consequences when acting.

### Tier 3 — Background Populations

Background populations are represented through pools, distributions, rates, and district or institutional state. They do not perform individual deliberation.

When play requires a durable individual, the game may instantiate one from the relevant context with a plausible identity, history summary, traits, connections, and initial knowledge. The simulation does not claim that this person was previously making full individual decisions.

### Promotion

Promotion is triggered by direct causal relevance, including:

- repeated or deliberate player interaction;
- joining an immediate organization or family network;
- acquiring important information or evidence;
- responsibility for an active operation;
- involvement in an active investigation;
- gaining a consequential office or organizational role;
- developing a strong relationship, grievance, obligation, or conspiracy involving an active character;
- becoming central to an unresolved threat, opportunity, or succession.

A relevance score may rank candidates, but the active-tier budget remains authoritative.

### Demotion

Demotion becomes possible after active causal connections remain dormant for a sustained period. Hysteresis should prevent repeated promotion/demotion oscillation.

Demotion compresses transient state:

```text
detailed short-term schedule → current assignment and next milestone
minor memories → durable modifiers or history summary
multiple low-priority intentions → one continuing agenda
recent activity → summarized history
```

It must preserve knowledge, evidence, ownership, crimes, family ties, important relationships, debts, promises, obligations, grievances, and any fact needed for future consequences.

## Calendar and Process Model

The player-facing calendar is continuous. The implementation is a discrete-event simulation.

Examples:

```text
09:00 — business reporting event
11:30 — surveillance milestone
18:00 — scheduled meeting
next day 08:00 — investigator reviews lead
three days later — crew assignment completes
next week — supporting-character role review
```

### Ongoing Processes

Characters normally begin durable activities rather than scheduling every atomic movement:

```text
Investigate warehouse
Duration estimate: 5–12 days
Resources: two investigators
Milestones: records check, surveillance, witness contact
Interrupts: new evidence, target moves, interference, reassignment
Completion: findings, evidence, and possible next candidates
```

Other processes include operating a business, cultivating a contact, recruiting, hiding, traveling, surveilling, negotiating, planning an operation, and serving a sentence.

Processes schedule milestones and completion events. Interrupts may cause local deliberation without waking unrelated characters.

### Temporal Resolution

- focused scenes and major operations may use minutes or hours;
- active assignments and investigations may use hours or days;
- business, organizational, and relationship processes may use days or weeks;
- elections, imprisonment, aging, and long-term change may use months or years;
- supporting characters receive less frequent reviews;
- background populations update through aggregate system events.

Zooming into a heist does not require the whole world to update at heist resolution. Unrelated scheduled events remain queued or resolve at their appropriate abstraction.

### Determinism and Scheduling Invariants

The event system must obey these constraints:

- simulation outcomes do not depend on rendering frame rate;
- changing fast-forward speed does not change outcomes;
- seeded runs are reproducible under the same inputs and build;
- save/load preserves pending events, ongoing processes, random state, and identifiers;
- promotion does not duplicate, restart, or silently alter scheduled work;
- demotion does not discard state required by pending events or later consequences;
- every event has stable identity, scheduled time, cause, owner or source, and validation rules;
- invalidated events cancel or transform safely and leave a traceable reason;
- stale references fail visibly in development rather than silently corrupting history.

These invariants require dedicated automated tests and should be validated independently from character-behavior tuning.

## Delegation

Delegation creates a new commitment and transfers execution responsibility without transferring perfect control.

The delegator selects an objective, recipient, constraints, information to disclose, resources, and possibly a deadline. The recipient interprets the assignment through their beliefs, traits, relationship, authority, and motives. They may accept, refuse, delay, alter the method, misunderstand, exceed authority, conceal results, or delegate again.

Each layer affects:

- who knows the objective and its origin;
- execution control;
- delay and distortion;
- evidence and exposure;
- obligation and responsibility;
- opportunities for betrayal or independent action.

Delegated strategies use the same action-resolution rules as direct strategies while allowing cheaper decision detail when the actors are less relevant.

## Explainability and Testing

Every consequential NPC decision should be inspectable in development builds. A decision record should capture:

- trigger;
- beliefs and known sources used;
- agenda or pressure selected;
- candidates generated and rejected;
- major score components;
- chosen intention or strategy;
- commitment and reconsideration conditions.

This information supports debugging and retrospective narrative explanations without exposing exact hidden values to the player.

### Developer Explanation vs. In-Fiction Explanation

Developer traces record the actual trigger, perceived inputs, candidate set, score components, and chosen strategy. They exist for testing and must not be presented directly to the player.

Player-facing explanations are assembled from facts a source could know, observable traces, memories, reports, rumors, evidence, and attributed interpretations. They may be incomplete, delayed, biased, or wrong. A player should be able to infer why a character might have acted without automatically learning the true utility calculation, secret motive, or objective world state.

Every consequential action therefore needs two output contracts:

1. **Causal output:** authoritative state changes and internal decision records.
2. **Legibility output:** potential observations, evidence, reports, rumors, behavioral tells, and persistent world changes.

The information and legibility document defines how those outputs propagate and reach the player.

Behavioral tests should verify that:

- characters do not act on information they lack;
- traits influence choices without directly causing arbitrary events;
- commitments create stable behavior;
- urgent interrupts cause appropriate reconsideration;
- authority violations remain possible and consequential;
- promotion and demotion preserve causal state;
- fast-forward cost follows meaningful events rather than elapsed empty time;
- supporting-tier approximations remain compatible with active-tier outcomes;
- the same action produces comparable consequences for player and NPC actors.
- consequential actions produce appropriate observable traces;
- player-facing accounts never contain facts unavailable to their source;
- the same underlying event can support multiple incomplete or conflicting interpretations;
- high ambient attention can coexist with weak case evidence;
- a specific strong case can exist without a high global attention state.

## Pre-MVP Simulation Kernel

The next executable artifact is a deterministic, text-only behavioral spike, not the full game MVP. A suitable initial scenario uses roughly 8–12 fixed characters, two small organizations, one contested domain, a handful of roles, and three to five parameterized strategies.

It should test:

- the event queue and deterministic replay;
- triggers, bounded candidates, utility selection, and commitments;
- belief-limited decisions;
- organization priorities becoming officeholder assignments;
- individual compliance, reinterpretation, concealment, or violation;
- a minimal set of observable traces and source-limited reports;
- developer-readable causal histories.

It should omit graphics, a full economy, succession, detailed cases, extensive tier transitions, and production content. Its purpose is to falsify the decision and coordination model cheaply.

Success requires more than technically valid execution. Across repeated seeds, characters must remain coherent rather than inert or erratic; organizations must coordinate without hive-mind knowledge; decisions must be explainable from beliefs and motives; and identical seeds and inputs must reproduce identical histories.

## MVP Architecture Commitments

The MVP vertical slice should implement:

- event-triggered and periodic deliberation;
- a small library of role- and context-specific candidate generators;
- belief-based local utility selection;
- intentions, commitments, and switching costs;
- a limited set of parameterized strategies;
- scheduled events and ongoing processes;
- active and supporting character tiers plus aggregate populations;
- explicit promotion and demotion with causal-state preservation;
- development-only decision traces;
- source-limited player reports and observable action traces;
- strict population and evaluation budgets.

The MVP should not attempt:

- unrestricted GOAP or general-purpose agent planning;
- continuous deliberation for all characters;
- individual simulation of the entire population;
- identical AI and player decision interfaces;
- minute-resolution world updates during fast-forward;
- final universal scoring formulas before behavioral prototypes exist.

## Validation Sequence

1. **Simulation kernel:** event scheduling, bounded deliberation, commitments, belief limits, organizational assignments, and traces.
2. **Emergence prototype:** richer relationships and grievances, information transmission, delegation, rival activity, player-facing reports, and limited tier transitions.
3. **MVP vertical slice:** businesses and money, territory, evidence-based investigations, imprisonment and playable failure, succession, and the player interface.

Trait vocabulary, candidate limits, utility weights, knowledge schema, promotion thresholds, and final technology choices should be informed by executable results rather than additional speculative detail alone.

## Open Questions for Prototyping

- Which drives and traits are irreducible, and which can be derived or omitted?
- How many simultaneous ambitions, responsibilities, pressures, and commitments remain understandable?
- Which candidate generators are shared, role-specific, or system-owned?
- How large can candidate sets become before performance or explainability degrades?
- How should confidence, misinformation, and source trust alter perceived utility?
- When does a reactive response override an existing commitment?
- What active-character budget produces convincing stories on target hardware?
- Which state can be compressed safely during demotion?
- How closely must supporting-tier outcomes approximate active-tier decisions?
- When, if ever, does a system need limited dynamic planning beyond parameterized strategies?
