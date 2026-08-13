# Criminal Empire — Information and Legibility

## Status and Purpose

This is an early design document. It defines the intended information flow and player-facing storytelling contract, but its data shapes, interface treatments, and tuning remain provisional until tested in the simulation kernel and emergence prototype.

The system must make a simulated world feel causally alive without granting the player omniscience. It answers two different questions:

1. **Visibility:** How can the player learn that something happened?
2. **Legibility:** How can the player form a plausible explanation for why it happened?

Emergent events that remain only in internal state are not player-facing stories. Conversely, perfect explanations would destroy uncertainty, secrecy, investigation, and paranoia.

## Core Information Flow

```text
authoritative event and world truth
                ↓
      state changes and traces
                ↓
 characters observe or discover traces
                ↓
  characters form claims and beliefs
                ↓
 claims spread through reports, rumors,
 dialogue, records, media, and testimony
                ↓
 the player receives a source-limited account
                ↓
 the player forms their own interpretation
```

The simulation never converts truth directly into player knowledge unless the player character personally and reliably observes it.

## Information States

### Truth

The authoritative world state: what actually happened, who acted, what they intended, and what consequences occurred. Truth is used by simulation resolution and developer traces. It is not automatically knowable.

### Trace

A change or artifact that may be observed or discovered. Examples include:

- a witness seeing a vehicle;
- a body, injury, weapon, or damaged property;
- missing money or inventory;
- a changed ledger or ownership record;
- a phone call, meeting, or travel record;
- unusual absence or behavior;
- new guards, reduced revenue, or a closed business;
- an altered relationship or organizational appointment.

Traces may decay, be concealed, destroyed, forged, misunderstood, or never discovered.

### Observation

A character's encounter with a trace or event. An observation records the observer, time, method, conditions, and what was perceptible—not necessarily the complete truth.

### Claim

A proposition a character can communicate or consider, such as “Vincent ordered the attack” or “a blue sedan left the alley.” Claims can be true, false, partially true, or too vague to evaluate.

### Knowledge and Belief

Knowledge represents information treated as directly established by a character. Belief or suspicion represents a claim the character considers possible or likely. Both are character-relative and include confidence, source, acquisition time, and supporting observations or claims.

The architecture should not assume that a high-confidence belief is objectively true.

### Rumor

A claim circulating through social or institutional networks without a fully reliable chain of support. Rumors may mutate, merge, gain credibility through repetition, or be deliberately seeded.

### Evidence

An observation, record, object, or testimony that can support a claim before a particular audience. Evidence has provenance, integrity, relevance, accessibility, and audience-specific usefulness. Evidence is not synonymous with truth: truthful information may be inadmissible or unusable, and fabricated material may initially appear convincing.

## Minimal Data Contracts

These are conceptual contracts, not final implementation schemas.

### World Event

```text
event_id
event_type
time and place
participants and roles
causes and parent events
authoritative outcome
state changes
generated traces
secrecy and exposure context
```

### Trace or Evidence Item

```text
trace_id
originating event
form and location
what it can support
visibility and discoverability
integrity and persistence
custody or possession
concealment, contamination, or forgery state
```

### Character Information Record

```text
character
claim
stance: knows / believes / suspects / doubts / rejects
confidence
source or source chain
acquired_at
supporting observations, evidence, or reports
secrecy and willingness to disclose
last reconsidered_at
```

### Report or Message

```text
sender
recipient or audience
claims conveyed
claimed sources
actual basis available to sender
confidence and framing
intent: inform / persuade / deceive / warn / reassure
delay, distortion, and transmission channel
```

### Player Intelligence Entry

```text
subject
claims currently available to the player character
source attribution
confidence or qualitative assessment
supporting and conflicting material
recency
unresolved questions
```

## Action Legibility Contract

Every consequential action or strategy should specify:

### Causal Outputs

- authoritative state changes;
- resource and relationship effects;
- new commitments or pressures;
- actual evidence and exposure;
- internal decision trace for development.

### Legibility Outputs

- immediately observable consequences;
- traces that may later be discovered;
- people positioned to observe or infer something;
- reports owed through organizational roles;
- possible rumors or public coverage;
- behavioral changes visible to connected characters;
- persistent map, business, relationship, or institutional changes.

An action does not need to generate every type of output. It must, however, deliberately answer how its consequences might become knowable rather than relying on an omniscient notification.

## Sources and Channels

The player may learn through:

- direct participation or personal observation;
- subordinates' operational reports;
- advisers' interpretations;
- informants and personal contacts;
- family conversations;
- business records and financial discrepancies;
- police, court, or government records;
- surveillance and investigation;
- witnesses and participants;
- neighborhood rumor;
- newspapers, radio, television, or other media;
- visible changes to locations, territory, staffing, or behavior.

Each channel affects speed, detail, reliability, and exposure. Requesting information may itself reveal interest or create a record.

## Reporting and Distortion

A report contains only claims the source knows, believes, fabricates, or chooses to communicate. Information can change through:

- limited observation;
- mistaken identity;
- poor memory;
- source distrust;
- exaggeration or simplification;
- self-protection;
- organizational politics;
- deliberate deception;
- intermediaries omitting or altering details;
- delay making information stale.

The system should preserve source chains when useful, but the player may see only the attributed source they have access to. “Vincent says an associate heard…” is meaningfully different from direct observation.

## Player-Facing Explanation

The interface should communicate information in fiction rather than expose utility calculations or objective hidden state.

Useful forms include:

- a lieutenant's report;
- an adviser offering an interpretation;
- a rumor attributed to a neighborhood or source;
- a timeline of known events;
- visible behavioral changes;
- relationship history and remembered incidents;
- a case board connecting claims and evidence;
- a business report showing unexplained losses;
- conflicting accounts presented side by side.

The player may learn:

> Vincent became distant after Marco's promotion, missed two meetings, and has been seen privately with two crew leaders.

The game should not automatically reveal:

> Vincent selected `UndermineBoss` because its utility score was 74.2.

An adviser may infer resentment or conspiracy. That interpretation is another sourced claim, not authoritative narration.

## Confidence and Presentation

Exact probabilities should usually remain hidden. Player-facing confidence may use qualitative language such as:

- personally witnessed;
- confirmed by independent sources;
- strongly supported;
- plausible;
- uncertain;
- contradicted;
- stale;
- source reliability unknown.

The interface should distinguish:

- observation from inference;
- current information from stale information;
- one source from corroboration;
- absence of evidence from evidence of absence;
- institutional proof from personal certainty.

## Organizations and Internal Reporting

Organizational roles create reporting expectations. A subordinate assigned an operation may owe a completion report; a manager may report revenue; an investigator may submit findings; a capo may summarize activity from their crews.

Reports are not automatic truth synchronization. Characters may conceal unauthorized acts, skim money, protect subordinates, exaggerate success, shift blame, or pass along mistakes. Leaders can request audits, seek corroboration, cultivate independent sources, or tolerate ambiguity.

Delegation changes information topology. The delegator chooses what to disclose; the recipient learns at least enough to interpret the assignment; intermediaries create distance but also distortion and additional knowers.

## Rumors and Public Information

Rumors provide low-cost ambient visibility but should not become omniscient exposition. A rumor needs an origin or plausible context, a transmission network, a claim, and mutation or reliability rules.

Public events may generate media coverage. Media reports reflect available sources, institutional statements, incentives, and incomplete knowledge. Public attention can influence politics and police resources without creating prosecutable evidence by itself.

## Investigations and Case Legibility

Ambient attention, investigator belief, case evidence, and prosecution readiness remain distinct.

A case should contain:

- one or more alleged events or offenses;
- suspects and claims linking them to those events;
- assigned investigators or institutions;
- evidence and testimony with provenance;
- investigative leads and unresolved questions;
- competing interpretations;
- legal or institutional constraints.

Anti–heat-bar tests:

- Can attention be high while prosecutable evidence is weak?
- Can a strong specific case exist while general attention remains low?
- Can two investigators hold different beliefs about the same suspect?
- Can evidence implicate one person in one event without revealing the entire organization?
- Does suppressing, discrediting, or losing evidence change the actual case rather than merely subtracting global points?
- If a global attention variable disappeared, would the individual cases remain coherent?

## Family Knowledge and Personal Life

Family knowledge uses the same claim and source model. A spouse may observe unexplained absences and money, hear a rumor, or receive a confession. One child may know specific events while another believes a legitimate cover story.

Family members decide what to ask, believe, conceal, disclose, or act upon according to their relationships, values, pressures, and access. “Knows the player is criminal” should not be a single universal switch when more specific claims matter.

## Developer Truth and Player Knowledge

Development tools require complete visibility:

- authoritative event history;
- generated traces;
- observers and discovery checks;
- claim transmission paths;
- belief updates;
- report content and omitted facts;
- player-accessible information at any moment.

These traces support debugging, deterministic replay, and evaluation of whether the player could reasonably understand an outcome. They must remain separate from production player-facing information.

## Pre-MVP Kernel Scope

The first behavioral spike needs only a minimal information loop:

- a small vocabulary of claims;
- events that generate a few explicit traces;
- direct observation and one report channel;
- beliefs with source and qualitative confidence;
- deliberate omission or deception in at least one scenario;
- a player-readable event history limited to what the player character learned;
- a separate developer truth log.

A useful test scenario is an unauthorized action by a subordinate:

1. A leader establishes a policy against escalation.
2. An officeholder independently orders or performs coercion.
3. The action creates witnesses, a revenue change, and police interest.
4. The officeholder reports a partial or false account.
5. Another source supplies conflicting information.
6. The player must decide what probably happened without receiving the internal decision trace.

The scenario validates organizational coordination, authority violations, traces, source-limited reporting, imperfect information, and player-facing legibility together.

## Success Criteria

- Consequential NPC actions become discoverable through plausible channels.
- The player can usually form at least one reasonable causal explanation for important changes.
- The player is not guaranteed the true explanation.
- Sources never communicate facts unavailable to them unless they are inventing or inferring those facts.
- Conflicting accounts remain traceable to different observations, beliefs, or motives.
- Hidden information affects NPC behavior without leaking through UI wording.
- Investigations depend on event-specific claims and evidence rather than a renamed global heat score.
- Developer tools can reconstruct the full causal and transmission chain.

## Open Questions

- What is the smallest useful claim vocabulary for the behavioral spike?
- Are claims represented as structured predicates, typed records, or domain-specific objects?
- How much source-chain history must persist after information spreads?
- When should confidence update automatically, and when should reconsideration require a trigger?
- How do rumors mutate without becoming arbitrary noise?
- Which traces decay, and at what abstraction?
- When does an inference become evidence for an institution or audience?
- How are lies distinguished from sincere false beliefs in development traces?
- How much uncertainty can the player tolerate before causality feels random?
- Which information deserves interruption, passive display, periodic reporting, or discovery only on demand?
