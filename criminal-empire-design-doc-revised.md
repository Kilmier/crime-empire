# Criminal Empire — Game Vision & Design Direction

## High Concept

An open-ended crime-drama simulation about living a criminal life inside a persistent, character-driven world. The player may rise, fall, retire, go legitimate, disappear, spend years in prison, rebuild, or die—but the world continues and remembers.

The fantasy is not simply “become the kingpin.” It is: **live a criminal saga that could have begun differently and may never resolve the same way twice.** A player might begin with enough money for a laundromat, cook for small dealers, work upward through an established gang, build a crew, stage a hostile takeover, become a political fixer, or attempt to leave crime behind. None of these paths is mandatory, and none is the universal victory condition.

The touchstones are character-rich crime stories such as *The Sopranos*, *Breaking Bad*, *Ozark*, *Heat*, and *Ocean's Eleven*, combined with the systemic character histories and emergent storytelling of grand-strategy games.

## Core Design Pillars

### 1. Player Freedom and Player-Defined Goals

There is no predetermined criminal career ladder and no universal win condition. The player defines what success means for the current character: wealth, revenge, control, notoriety, family security, political influence, legitimacy, survival, retirement, or something else.

Milestones and ambitions provide direction without declaring one canonical way to win. Going legitimate is a meaningful possible endgame, not *the* endgame.

### 2. Interlocking Systems

Crime, business, money, territory, information, relationships, family, politics, organizations, and law enforcement continuously affect one another. A robbery is not merely a payout. It can create witnesses, evidence, injuries, grudges, reputation, suspicion, opportunities, arrests, and changes in relationships.

Major actions should create consequences in several systems. A murder may remove one problem while creating three more.

### 3. Emergent Narrative

Stories arise from character motives, relationships, incomplete information, and systemic outcomes rather than predetermined plot chains. A lieutenant does not betray the player because the script requires a betrayal in Act III; they betray the player because their ambitions, grievances, loyalties, fears, relationships, opportunities, and assessment of the situation made betrayal seem worthwhile.

**Generate problems, not quests.** The simulation should create situations such as “your lieutenant is hiding income, two crews are personally loyal to him, and a detective believes he can flip him,” rather than issuing `MISSION: DEAL WITH VINCENT`.

Emergence must be **observable and interpretable**, not merely present in hidden state. Consequential NPC actions create world traces—changed behavior, missing money, witnesses, evidence, rumors, injuries, ownership changes, news, reports, or absences. The player learns about events only through personal observation and in-world sources, and may receive an incomplete, delayed, biased, or false account. The design should give the player enough evidence to form plausible explanations without revealing omniscient decision logs.

### 4. Persistent Consequences

The world remembers. Characters, organizations, families, and institutions retain the effects of the player's history, and they continue to change during imprisonment, exile, disappearance, or retirement. People age. Children grow up. Detectives are promoted. Businesses change hands. Territory is divided. Grudges survive their original participants.

### 5. Imperfect Information

The player and NPCs act on limited, fallible information rather than omniscient statistics. Reports come from people with different access, competence, loyalties, and motives. Confidence and source matter. Exact hidden probabilities should rarely be exposed when uncertainty, trust, and paranoia are the intended experience.

### 6. Shared Agency / Actor Parity

The player is one character inside the simulation, not an invisible management god whose actions obey entirely different rules. Actor parity means **causal parity**, not universal full-fidelity agency: comparable actions follow comparable requirements, costs, resolution rules, information exposure, and consequences regardless of who performs them.

NPCs need credible access to actions relevant to their role, goals, knowledge, assignments, and circumstances. They do not need the player's full interface, planning resolution, decision frequency, or entire theoretical action catalogue. Whether a character can perform an available action depends on skill, equipment, money, access, contacts, location, time, information, authority, willingness, and risk tolerance. A detective, capo, attorney, and burglar have different opportunities, but their actions belong to the same causal world rather than separate arbitrary simulations.

NPC deliberation must remain bounded. Characters evaluate small, context-generated sets of plausible actions rather than continuously comparing everything any actor could possibly do. **Simulate causality at full fidelity; simulate deliberation only as deeply as gameplay can observe or affect it.**

NPC behavior uses **event-driven bounded deliberation**. Events, commitments, and periodic reviews determine when a character reconsiders their situation. Roles, goals, responsibilities, knowledge, relationships, pressures, and current circumstances generate a small set of relevant actions or strategies. Characters choose among those candidates according to their own beliefs, values, personality, expected benefits, risks, and obligations—not omniscient world truth.

Characters normally commit to an intention rather than replanning constantly. Immediate reactions handle urgent danger; commitments preserve coherent ongoing behavior; deliberate choices select among relevant strategies; and parameterized multi-step strategies carry out larger aims. The design does not require unrestricted general-purpose planning or a global utility scan across every possible action.

### 7. Playable Failure

Failure should usually change the player's circumstances instead of ending the campaign. Arrest, imprisonment, bankruptcy, betrayal, injury, exile, loss of territory, and organizational collapse are potential new chapters.

A once-powerful boss leaving prison years later to find a fragmented organization, an estranged family, and a former lieutenant controlling the old territory is not a failed campaign. It is the campaign. Hard endings remain possible—death without a viable continuation, permanent removal when meaningful play is exhausted, or the player's decision to end the story—but setbacks should create play whenever possible.

### Succession and Continuity

When control passes to another character, the successor remains their own person. They retain or receive their own skills, traits, knowledge, relationships, ambitions, reputation, criminal history, and legal exposure. They do not inherit the previous character's personal statistics or relationships as if those were transferable resources.

A successor may inherit, receive, or contest offices, businesses, property, territory, organizational authority, debts, alliances, enemies, family reputation, and unresolved investigations. Control of the organization and acceptance of the successor are systemic outcomes, not automatic guarantees.

## Characters and Organizations

Criminals, detectives, police leaders, prosecutors, politicians, attorneys, businesspeople, hackers, journalists, family members, and informants share a common persistent Character model. Characters may have traits, skills, ambitions, resources, memories, secrets, relationships, family ties, affiliations, occupations, offices, reputations, and personal histories.

Relationships are directional and multidimensional. Trust, respect, fear, affection, resentment, attraction, and obligation are not interchangeable. A capo can hate the player, respect their competence, fear the consequences of disobedience, and remain loyal—for now.

Organizations are networks of characters rather than abstract stat blocks. Offices and formal roles grant access and influence, but the person holding an office matters. A new district attorney, police chief, boss, union leader, or mayor can change the strategic landscape without changing the institution itself.

Organizations coordinate through priorities, policies, offices, responsibilities, and delegated assignments rather than behaving either as omniscient hive minds or as unrelated individuals sharing a label. Leadership can set intent and constraints; officeholders interpret and execute them through their own knowledge, motives, relationships, and judgment. Personal agendas and informal factions may align with, distort, or oppose formal organizational goals.

Characters may rise from obscurity. A minor associate, beat cop, accountant, or family member can become important because of what occurs during play. This does not mean every background person is always running a complete agent simulation.

Simulation depth is relevance-tiered and budgeted:

- **Active characters** include the player, immediate associates, current rivals, important family, lead investigators, officeholders, and people involved in active crises. They retain detailed goals, relationships, knowledge, and intentions and make event-driven decisions from bounded candidate sets.
- **Persistent supporting characters** retain identity and causally important state but use cheaper role-specific heuristics, assignments, and less frequent updates.
- **Background populations** are represented primarily through pools, distributions, and aggregate district or institutional state. A persistent individual can be instantiated from that context when play requires one.

Promotion follows direct causal relevance: player interaction, organizational power, important knowledge, active-case involvement, responsibility for an operation, a consequential relationship, or participation in an unresolved conflict. Demotion reduces update detail only after those connections become inactive; it does not erase knowledge, evidence, ownership, family ties, major memories, debts, crimes, or other state needed for future causality. Exact thresholds, tier budgets, state compression, and promotion rules belong in the simulation architecture rather than this vision document.

### Authority Is Not Capability

Organizational rules are social rules, not hard simulation locks. A capo forbidden from ordering violence may still be capable of ordering it. They may disobey, conceal the act, misunderstand an instruction, exceed their mandate, or delegate it again. Consequences come from discovery, relationships, enforcement, and organizational politics—not from the action being mechanically impossible.

## Direct Action and Delegation

There is no hard transition from a “street phase” to a “management phase.” A skilled, well-equipped player character may personally perform dangerous work late in a campaign. A less capable character may delegate early if they have money, authority, contacts, or the right person.

Direct action and delegation create a recurring tradeoff:

- **Direct action** offers maximum control, first-hand knowledge, fewer intermediaries, and sometimes greater secrecy, but consumes the player's time and creates personal risk, exposure, injury, death, and arrest.
- **Delegation** provides time, distance, and insulation, and may employ someone more capable, but gives others knowledge of the plan, reduces control over execution, introduces delay and distortion, and creates opportunities for failure, evidence, betrayal, or further delegation.

Additional intermediaries can obscure who gave the original order, but every layer also adds people, delays, misunderstandings, and information leaks. NPC leaders should face these same choices.

## Information, Knowledge, and Secrets

Information is a simulated resource. The design should distinguish conceptually between:

- **Truth:** what actually happened or is true in the world.
- **Knowledge:** what a particular character directly knows or has learned.
- **Belief or suspicion:** what a character thinks is true, correctly or incorrectly.
- **Rumor:** information circulating through people and networks with uncertain origin or reliability.
- **Evidence:** information or material that can substantiate a claim to an institution, court, superior, public, or other audience.

These states do not automatically collapse into one another. A detective may correctly suspect the player but lack admissible evidence. A capo may sincerely believe a false rumor. A witness may know part of the truth but be unwilling or unable to prove it. A forged record may look like evidence without being true.

Information spreads through observation, conversation, surveillance, records, coercion, mistakes, investigation, and betrayal. Who knows a fact—and who knows that they know it—can be as important as the fact itself.

The information layer also carries the story to the player. The simulation distinguishes what truly occurred, which traces it left in the world, which characters observed or interpreted those traces, and which account eventually reached the player. Reports and explanations remain attributable to sources rather than appearing as omniscient narration.

### Family Knowledge

Family members have their own knowledge states concerning the player's criminal life. A spouse may suspect the source of the money without knowing specific crimes. One child may know nearly everything; another may sincerely believe the player is legitimate. These beliefs can change, spread, be concealed, or produce fear, complicity, estrangement, leverage, loyalty, and succession consequences.

## Law Enforcement and Investigations

Police, detectives, prosecutors, attorneys, judges, informants, and political officials are persistent characters with goals, relationships, knowledge, careers, institutional authority, and constraints.

**Ambient attention is distinct from a specific investigation.** Violence, notoriety, media coverage, political pressure, and visible wealth can increase general scrutiny or the resources devoted to organized crime. Attention makes danger more likely, but it does not convict anyone. Specific investigators must connect people to specific acts through witnesses, records, surveillance, physical evidence, testimony, and other case material.

Cases may overlap and share information, but case strength is not a reskinned global heat bar. Suspicion, institutional attention, investigative knowledge, and prosecutable evidence are related but separate.

### Arrests, Pleas, and Cooperation

Arrested characters evaluate whether to remain silent, plead, cooperate, lie, or attempt to redirect an investigation based on what they know and what motivates them. Relevant factors include evidence against them, likely sentence, loyalty, fear, grievances, ambition, family pressure, trust in the organization, belief that they will be protected, and the offer being made.

An informant can reveal only what they actually know or plausibly believe. Cooperation never grants investigators magical access to an entire organization. A low-level associate may expose a location and several contacts; a trusted intermediary may connect an order to senior leadership; a liar may provide false information that investigators must assess.

## Economy, Legitimacy, and Money

Clean and dirty wealth are fundamentally different. Illegal earnings may be abundant but difficult to spend safely, explain, invest, or pass to a legitimate family. Legitimate businesses can produce income, prestige, access, political relationships, property, employment, operational opportunities, and financial cover; they should not exist only as laundering meters.

For the MVP, money may use a readable clean/dirty distinction with laundering capacity and exposure. The long-term design may preserve **provenance**: where money came from, who handled it, which event or operation produced it, and how exposed that source is. Money from a notorious robbery may be more dangerous than equal revenue from an established illicit business. This is a future direction, not a v1 promise.

Legitimacy is not merely a resource conversion. A legitimate life can become valuable in its own right and may create tension with the organization, family, past crimes, and the player's original ambitions.

## Territory and Influence

Territory is an overlapping ecosystem, not a board painted in exclusive colors. Several organizations, independent operators, businesses, communities, and institutions may hold different forms of influence in the same district.

Control may come from crews, operations, reputation, protection, political ties, property, information, fear, or community relationships. Formal ownership, criminal presence, public tolerance, and institutional protection are separate. Rival activity inside “your” territory should create problems rather than being mechanically prohibited.

## Crime, Violence, and Operations

Violence is powerful, but it is not a universal clean solution. It can create fear and remove immediate threats while also producing witnesses, evidence, retaliation, succession crises, factional conflict, political pressure, public outrage, and lasting family grudges.

Operations combine objectives, actors, preparation, equipment, access, intelligence quality, time, and uncertain execution. Outcomes should be traceable to character capability and circumstances rather than opaque arbitrary rolls, while still allowing surprise and incomplete knowledge.

### Heists

For the MVP, heists and major crimes should be abstract planning-and-resolution operations: choose an objective, crew, leader, budget, preparation, equipment, and available intelligence, then resolve the operation through shared simulation rules with readable consequences.

The post-MVP vision can expand major heists into procedural mini-maps and event-driven sequences. As an operation unfolds, the player or delegated leader repeatedly weighs **greed, time, and risk**: leave with what has already been secured, or push deeper toward a more valuable objective while defenses, uncertainty, evidence, injuries, and police response accumulate. The plan can unravel, and incomplete information should make the decision genuinely tense.

## Progression

Progression is primarily the growth of **optionality, networks, influence, and insulation**, not raw stat inflation.

The broad fantasy is:

> personally capable → connected → staffed → networked → institutionally influential → insulated

Early power may come from personal competence. Later power comes from trusted people, businesses, information networks, attorneys, political favors, institutional relationships, and the ability to solve a problem in several different ways. Greater reach may reduce direct knowledge and control. The player becomes powerful by gaining choices, while the organization they built becomes harder to fully understand.

Skills still matter, but late-game identity should not collapse into `Skill 95` and percentage bonuses. A highly capable player may still act personally; an influential player may choose among negotiation, delegation, money, leverage, law, politics, or violence.

## Time and Persistence

The player experiences a **continuous calendar with pause, adjustable fast-forward, and event-driven interrupts**, rather than conventional fixed turns. Underneath, the world advances through scheduled events, ongoing processes, deadlines, and state changes—not universal minute-by-minute updates for every character.

Characters do not continuously reconsider every possible action. They evaluate decisions when relevant events occur: receiving information or orders, beginning or completing an assignment, encountering an opportunity, crossing an important threshold, reaching a deadline, or performing a periodic role-level update. Lower-relevance characters and systems update less often and at greater abstraction.

Systems operate at appropriate granularities:

- travel, surveillance, meetings, and major operations may use hours or minutes;
- businesses, relationships, investigations, and organizational activity may develop over days or weeks;
- elections, prison sentences, aging, family change, and long-term economic shifts may unfold over months or years.

The simulation can zoom into finer time during a heist or crisis, then return to broader calendar flow. This local increase in resolution does not require unrelated characters or systems to simulate at the same granularity. Quiet periods should fast-forward by processing meaningful scheduled events rather than iterating through empty time. Important events interrupt or slow time according to player settings. The world continues during absence, including imprisonment and exile.

## Tone and Presentation

The tone is a love letter to crime film and television: slick, intimate, dark, and endearingly grimy. Deals happen in back rooms. Expensive surfaces conceal frightened or compromised people. Power, loyalty, wealth, clever plans, and criminal freedom should be seductive, while their consequences are allowed to be destructive without becoming a moral lecture.

The visual direction is a readable 16-bit-inspired city and management interface. The map should communicate districts, businesses, known operations, influence, institutions, and incomplete intelligence without requiring every citizen to be physically rendered.

Systems should be believable enough that outcomes make intuitive sense, but cinematic drama, clarity, and player agency take priority over procedural realism.

## Pre-MVP Simulation Kernel

Before the first playable, a smaller deterministic, text-only prototype should validate the load-bearing behavior in isolation. It should use a fixed cast, one or two small organizations, a tiny strategy library, basic beliefs and relationships, organizational assignments, event-driven time, and developer-readable decision traces.

This kernel exists to test whether characters form coherent intentions, maintain commitments, respond to interrupts, obey information limits, coordinate through organizations, and produce reproducible histories. It does not need graphics, a full economy, succession, detailed investigations, or the complete tier system. Its interface may advance one event or one period at a time, but its underlying time model should exercise the scheduled-event architecture intended for the full game.

## MVP / First Playable Scope

The first playable should test whether the character and system interactions produce stories. It should be intentionally small and visually simple:

- one city;
- roughly six districts or neighborhoods;
- three criminal organizations;
- approximately 20–30 persistent major characters;
- a strict budget for fully active characters, with cheaper supporting characters and aggregate background populations;
- a shared foundational Character model;
- directional relationships, basic memories, goals, and limited knowledge;
- basic organizations, offices, and authority rules;
- four criminal operation types;
- three legitimate business types;
- simple clean/dirty money and laundering;
- overlapping territory influence;
- ambient police attention plus one or more evidence-based investigations;
- direct action and delegation through the same core resolution rules;
- a continuous calendar with pause, fast-forward, and interrupts;
- scheduled events and ongoing assignments rather than continuous universal NPC updates;
- simple imprisonment, succession, and playable setbacks;
- minimal presentation using boxes, icons, text, and placeholder art.

The prototype succeeds if it generates understandable histories such as: a capo acts without authorization, a rival retaliates, the capo's relative is arrested, that character cooperates based on personal pressure and reveals only what they know, and the resulting investigation fractures part of the organization.

The MVP does **not** require detailed tactical combat, procedural heist maps, twenty deeply simulated cities, individual simulation of the full population, deep political procedure, detailed money provenance, hundreds of business types, or finished pixel art.

## Long-Term Vision

If the core simulation proves compelling, the world can expand toward a state-scale map with a small number of deeply simulated major cities, more abstract towns and regional infrastructure, prisons, ports, highways, borders, and rural territory.

Long-term depth may include procedural heist spaces; unfolding greed/time/risk decisions; richer evidence chains; detailed money provenance and financial exposure; elections and institutional careers; deeper prison play; family generations; identities, exile, and relocation; more nuanced fronts and legitimate enterprises; factional politics; informant networks; and context-sensitive succession.

These are directions to preserve, not commitments for v1. New features should be added only when they strengthen the core fantasy and interlocking character simulation.

## Design Heuristics

When evaluating a feature, ask:

- Who knows about this, and how do they know?
- Who believes it, who doubts it, and who can prove it?
- Who benefits, who is threatened, and who is motivated to act?
- What relationships, organizations, and institutions does it affect?
- What evidence or exposure does it create?
- Can the player act directly, delegate, or solve it through another network?
- Can NPCs use the same underlying possibilities?
- Is causal parity preserved without requiring equal deliberation or simulation detail?
- Which relevance tier needs to represent this, and what causally important state must persist?
- What triggered this character to reconsider, and why would this option occur to them?
- Is the decision based on the character's beliefs, commitments, and motives rather than objective world truth?
- What happens if an authorized character refuses—or an unauthorized character acts?
- Does failure create a new situation worth playing?
- Does this generate a problem, or merely assign a quest?
- What observable traces does this create, who can perceive them, and how might the player learn about them?
- Can the player form a plausible causal explanation without receiving omniscient truth?
- Does progression add meaningful options rather than only larger numbers?
- Is this necessary for the MVP, or does it belong in the long-term vision?
- Could the specific investigations remain coherent if a global attention value were removed? If not, have cases collapsed into a disguised heat bar?
- Can attention be high while evidence is weak, and can a strong case exist before broad institutional attention develops?

The documents are canon; conversation is the workshop. The vision should protect the game's identity while implementation documents decide the exact formulas, data structures, and balance.
