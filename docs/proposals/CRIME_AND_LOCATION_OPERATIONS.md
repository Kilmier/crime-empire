# Crime and Location Operations

**Status: noncanonical design proposal.** This is a vocabulary and milestone-candidate record, not an
approved crime list, a requirement to add a `LocationOperation` type, or permission to build tactical
maps. Canon currently keeps MVP heists abstract.

## Feature intent

Different crimes should create different strategic problems, relationships, traces, institutional
responses, and uses for people. A new crime earns implementation by producing a new decision or
pressure—not by changing the name and payout of an existing operation.

The game also needs credible ways for a character to enter crime and acquire more responsibility.
These should create quest-shaped experiences without becoming authored quest chains: an order, job,
favor, known opportunity, or immediate pressure originates with a character and a world condition,
then remains subject to refusal, reinterpretation, interruption, failure, and persistent consequence.

## Criminal origins are initial conditions, not classes

An origin establishes the circumstances from which a character begins. It may supply an occupation
or office, affiliations, capabilities, resources, access, acquaintances, relationships, obligations,
grievances, knowledge, reputation, legal exposure, and immediate pressures. It does not grant an
exclusive action vocabulary or prescribe a career ladder.

Candidate starting positions include:

- an organization subordinate who receives assignments, backing, and access while owing obedience
  and a share of the proceeds;
- a marginal entrant with nerve or practical capability but little money, protection, or trusted
  access;
- an independent operator who keeps greater control but must find clients, partners, buyers, and
  protection one relationship at a time;
- a legitimate worker or professional whose occupation supplies unusual knowledge, access, cover, or
  capability while making criminal exposure more personally costly;
- a compromised institutional actor whose office supplies authority and privileged access alongside
  supervision, records, duties, and institutional risk.

Every starting advantage should carry a corresponding vulnerability. A technically capable
professional may lack criminal contacts. An organization subordinate may have protection but little
freedom. An independent may keep the proceeds but have nobody obliged to help when a job fails.
These are starting configurations in the same causal world, not separate campaigns with exclusive
mechanics. Fully playable institutional origins remain post-MVP under the existing crime-first scope.

## Orders, jobs, favors, opportunities, and pressures

The strategic layer may present bounded commitments that resemble quests from the player's
perspective while remaining in-world actions:

| Source | Basis | Typical consequences |
|---|---|---|
| Order | Formal authority and organizational responsibility | Compliance, discipline, advancement, concealment, resentment |
| Job or contract | Negotiated exchange | Payment, access, commercial trust, debt, retaliation |
| Favor or request | Personal relationship | Trust, obligation, grievance, reciprocity |
| Known opportunity | The character's own information and initiative | Self-directed profit, new contacts, exposure |
| Immediate pressure | Debt, fear, family need, revenge, legal danger, or ambition | Character-driven action without an external issuer |

An order follows the canonical assignment contract: objective, issuer, recipient, disclosed
information, resources, constraints, authority, and deadline. A boss may establish a priority, a
capo may interpret it through an office responsibility, and a low-ranking member may receive only
the capo's resulting order. The recipient does not automatically learn the full chain of command or
the original reasoning.

The recipient may comply, negotiate, refuse, delay, alter the method, exceed authority, conceal the
result, or delegate when actually capable and permitted to do so. Authority changes the expected
social consequences; it does not make disobedience physically impossible. The issuer, superior, and
organization learn only what they observe, are told, can infer, or can establish through legitimate
institutional state. A wake, objective completion, or shared organization id must not synchronize
private knowledge.

The same crime-resolution path should serve an ordered operation and an independently initiated one.
Their difference lies in how the opportunity became available and which obligations, information,
relationships, proceeds, and reporting expectations surround it—not in a second rules engine.

## Standing without a global reputation meter

Performance may change a character's prospects, but `Standing` should not become one omniscient
organization or world score. The design distinguishes:

- **formal position:** membership, rank, office, assigned responsibility, and communicated authority;
- **personal standing:** what particular people trust, fear, owe, or resent;
- **audience-relative reputation:** claims and histories known within a crew, neighborhood, market,
  profession, institution, or other information network;
- **institutional confidence:** what relevant decision-makers are willing to entrust to the character;
- **public and legal exposure:** notoriety, attention, suspicion, and evidence, which are not criminal
  respect under another name.

A successful operation changes standing only through a legitimate reader and information path. A
capo may give a subordinate credit, take credit, protect him, shift blame, or report accurately and
still have the boss interpret the result differently. A secret policy breach produces no immediate
social penalty merely because authoritative truth contains it; discovery, testimony, evidence,
behavior, or another valid channel must bring it to somebody who can react.

An independent character has no internal rank to gain or lose, but does not affect an abstract
"world standing." Clients, partners, suppliers, fences, victims, neighborhoods, police, and criminal
organizations may each form different views from different information. Those views can later alter
access, prices, cooperation, recruitment, targeting, protection, or investigation.

## Low-to-mid-tier progression

There is no universal sequence in which repeating one petty crime unlocks a larger one. Early routes
may emphasize property crime, illicit markets, violence, fraud, information, corruption, or misuse
of legitimate employment, and a character may cross between them when knowledge, capability,
relationships, and opportunity permit.

The meaningful transition from low to mid tier is organizational rather than numerical. A low-tier
character mainly performs or assists with bounded crimes. A mid-tier character increasingly maintains
recurring activity, coordinates other people, controls access or distribution, carries an office or
territorial responsibility, and becomes accountable for what subordinates do.

Progression should therefore add optionality and responsibility:

```text
credible performance
  -> particular people trust or rely on the character
  -> more sensitive opportunities and information become available
  -> responsibility for an operation, business, crew, route, or territory becomes possible
  -> greater reach creates new obligations, intermediaries, rivals, and incomplete knowledge
```

None of these transitions is automatic. Advancement requires a decision-maker with the authority,
information, motive, and available office or responsibility to grant it. A capable subordinate may
remain blocked because a capo takes credit, fears competition, distrusts him, or has no position to
offer. That obstruction is a playable problem, not a failed progression check.

### Early progression-matrix sketch

The following matrix is preserved ideation, not a settled ontology, player-facing skill tree, or
approved content plan. It provides a way to compare how different criminal activities may grow from
personal action into recurring coordination. A character can occupy different scales in different
routes, and participating in a large operation does not mean controlling it.

| Scale | Behavioral meaning |
|---|---|
| Personally capable | Can perform or assist with one bounded crime |
| Connected | Has repeat access to work, buyers, information, protection, or useful people |
| Staffed | Coordinates several people and becomes responsible for their conduct |
| Networked | Maintains recurring operations, routes, businesses, or institutional relationships that function together |

| Candidate route | Personally capable | Connected | Staffed | Networked mid tier |
|---|---|---|---|---|
| Property crime | Lookout, burglary, vehicle theft, or moving one stolen item | Reliable fence, tools, and target information | Coordinate a driver, scout, entry team, and sale | Recurring hijacking, warehouse theft, or fencing network |
| Contraband and logistics | Courier, street seller, or small producer | Supplier, buyer, stash site, and dependable route | Coordinate producers, runners, transport, and distribution | Wholesale distribution or a protected smuggling corridor |
| Vice and gambling | Take bets, deal in a venue, or help run one game | Recurring customers, location access, and protection | Manage dealers, bookmakers, collectors, or guards | Several venues or a territorial vice network |
| Coercion and protection | Deliver a threat, collect a debt, or act as muscle | Sponsor, feared reputation, and recurring collection work | Direct collectors or enforcement personnel | Protection racket or enforcement service supporting several operations |
| Fraud and finance | Forge a record, alter an account, or conduct a small scam | Insider, account access, cover identity, or reliable victim pool | Coordinate accomplices and conceal proceeds | Shell entities, contract fraud, or a multi-business financial scheme |
| Information and infiltration | Sell a tip, observe a target, or conceal a record | Informant, client, records access, or trusted source | Maintain sources, blackmail material, or surveillance personnel | Intelligence brokerage, organizational infiltration, or counterintelligence |
| Corruption and access | Pay or accept one bribe or obtain one improper favor | Recurring reciprocal contact and mutual exposure | Coordinate intermediaries or several compromised actors | Influence part of an office, jurisdiction, contracting process, or enforcement chain |
| Legitimate enterprise | Misuse a job, vehicle, account, or workplace | Manage or own a useful business | Employ people in legitimate and illicit roles | Connected fronts, supply chains, laundering capacity, and commercial influence |

The rows may overlap. Cargo theft can read property, information, logistics, coercion, and a
legitimate transport business at the same time. Route labels should help designers identify distinct
dependencies and consequences; they must not become exclusive character classes or alternate action
implementations.

Movement across the matrix comes from causal state rather than experience points. A character needs
some relevant combination of knowledge, capability, access, relationships, resources, time,
authority or willingness to violate it, and tolerance for the expected exposure and obligations.
The opportunity must become known or be offered by somebody with a reason to offer it.

Operation scale and character progression remain separate. A low-ranking driver may participate in
a large robbery without acquiring the capo's crew, contacts, authority, or responsibility. A single
valuable success may create money or reputation while leaving the character without a repeatable
route. Conversely, a modest recurring operation may make a character meaningfully mid tier because
other people, businesses, and obligations now depend on them.

Different consequences should be capable of opening different cross-route opportunities. A discreet
debt collection may build trust and access to sensitive work; a violent success may create fear,
witnesses, and a policy breach; an honest report of failure may preserve a relationship; concealed
skimming may create personal capital and a future grievance. None is merely a different amount of
progress toward the same unlock.

Which rows remain distinct, which are cross-cutting functions, which belong in the first playable,
and whether any part of this matrix should appear directly in the UI are deliberately deferred.

## Candidate domains

- burglary, armed robbery, cargo theft, auto theft, fencing, and high-value theft;
- production, transport, wholesale distribution, and local dealing of contraband;
- gambling, bookmaking, nightlife, and other vice businesses;
- extortion, protection, labor manipulation, debt collection, and racketeering;
- smuggling and regional logistics;
- fraud, forged records, shell entities, false invoicing, contracting, zoning, and patronage;
- corruption, blackmail, information brokerage, informants, infiltration, and deliberate leaks;
- illicit manufacturing and counterfeiting at an appropriate abstraction;
- property schemes, prison economies, and outside coordination;
- abstracted cyber or information crime in eras where it belongs;
- kidnapping, coercion, sabotage, assassination, and other violence.

These are an idea pool. Similar domains should be merged when they do not produce different play.
They can also be classified by strategic function: cash, assets, access, information, control,
disruption, coercion, legitimacy, or escape.

## Shared operation direction

Location-centered crimes may eventually share an operation language containing an objective,
persistent location, participants, responsibilities, known and unknown conditions, entry and exit
options, time pressure, escalation, instructions, discretion, abort conditions, traces, and aftermath.

This is a behavioral direction, not a mandated class hierarchy. The present parameterized strategy
and abstract operation rules should be extended until two implemented crime types demonstrate a
specific duplication or missing invariant that a shared `LocationOperation` concept would remove.

Preparation should produce actor-owned information: observed schedules, known access, a reported
guard, a suspected route, or an insider's claim. It should not collapse into a universal success
percentage. Execution and aftermath must preserve actor identity, observation, reporting, evidence,
injury, proceeds, and relationship consequences.

## Tentative hypothesis: planning, exposure recognition, and unfinished aftermath

**Status: idea to preserve, not an approved cleanup system or a mandatory operation phase.** Some
crimes may become more interesting when preparation can reduce foreseeable exposure, execution can
create complications the participants do not all notice, and unresolved consequences remain in the
world rather than being converted immediately into a score.

Planning and recognition are different problems. A character with relevant tactical judgment might
prefer an assassination plan involving a quiet alley because the information available to them makes
fewer witnesses seem likely. That judgment cannot reveal whether the alley is truly empty: an unseen
resident, an unexpected pedestrian, a patrol, or era-appropriate surveillance may still observe the
act. Tactical foresight may ultimately belong to capability, personality, experience, or some
combination; this proposal does not settle that classification.

After execution, a perceptive character may notice a witness, overlooked object, suspicious vehicle,
bad sight line, or other weak signal that another participant misses. Perception supplies an
observation or makes a response salient; it does not expose authoritative truth or automatically
resolve the problem. A noticed complication may make several responses available—leave, warn the
issuer, threaten or bribe somebody, return later, assign follow-up work, conceal the problem, or lie
that the operation was clean—subject to the character's information, capability, relationships,
instructions, and circumstances. An unnoticed complication remains capable of producing later
testimony, evidence, suspicion, retaliation, or another persistent consequence.

Where an operation actually warrants it, instructions may assign responsibility for an aftermath
condition such as recovering a weapon, removing a vehicle, accounting for witnesses, treating an
injured participant, or reporting unresolved exposure. Failure, delay, concealment, or a false report
may then become socially and institutionally meaningful. This does not justify a universal cleanup
button, an automatic post-operation checklist, or a rule that successful cleanup erases every trace.

The smallest future proof should use one concrete operation in which planning changes expected
exposure, an actual witness or trace may still exist, different actors can notice different parts of
the aftermath, and leaving one consequence unresolved can change a later decision. Until such a
scenario earns implementation, the idea remains a hypothesis rather than part of the personal-demo
roadmap.

## Which crimes deserve a top-down operation map

A crime earns a top-down operation view only when all or most of these are true:

1. **Space changes the decision.** Entry, position, sight lines, access, movement, or escape routes
   create materially different choices.
2. **Timing changes the decision.** Waiting, coordinating, continuing, withdrawing, or responding to
   escalation matters during execution.
3. **Participants can interpret or improvise.** Who is present, what each knows, and how much
   discretion they received can change the plan.
4. **New information arrives while committed.** The interesting play is adaptation under pressure,
   not watching a predetermined resolution animate.
5. **Leaving is a real choice.** The player or executor can abandon an objective, escape with a
   partial gain, or push deeper and accept more risk.
6. **Aftermath depends on what happened inside.** Witnesses, traces, injuries, missing participants,
   evidence, and disputed decisions persist afterward.

A crime dominated by negotiation, records, long logistics, financial concealment, or relationship
management should ordinarily remain in the strategic layer. A visual map is not earned merely because
the crime occurred at a place.

## Potential milestone slices, not a sequence

1. Compare one low-ranking character ordered by a capo to solve a small criminal problem with an
   independent character learning of or being offered a comparable opportunity. Reuse the same
   operation resolution while proving that authority, proceeds, reporting, and consequences differ.
2. Prove one credit-and-blame path: the direct issuer reacts to what he legitimately learns, while a
   superior remains uninformed until an observation, report, record, or other valid channel reaches
   him.
3. Add one operation whose strategic pressure differs from extort/conceal/investigate and prove the
   difference in a natural scenario.
4. Add preparation that produces one fallible, source-bearing fact used by both controlled and
   autonomous execution.
5. Generalize a shared operation lifecycle only after two implemented operations expose the same
   concrete duplication or invariant.
6. Prototype one top-down scene over existing resolution state only after the abstract operation is
   interesting and the map criteria above are met.

Each slice is independently reviewable and requires authorization through `CURRENT_MILESTONE.md`.

## Falsifiers

- Changing the hidden source or purpose of an order while holding the recipient's briefing constant
  must not change the recipient's decision.
- A superior must not credit, blame, reward, or punish a subordinate for an outcome that never reaches
  that superior through observation, report, record, inference, or another authorized path.
- A concealed method or policy breach must not change another character's relationship or evaluation
  before that character can legitimately learn of it.
- An ordered and independently initiated instance of the same crime must share its underlying
  availability and resolution rules; replacing either actor with the other must not reveal a
  player-only or organization-only action implementation.
- Removing the information that carries credit upward must prevent the upper superior from crediting
  the actor even when authoritative world state is unchanged.
- An independent actor must acquire no fictional organizational rank consequence, while affected
  people and institutions remain able to react through their own knowledge and relationships.
- Giving a character money without the required knowledge, access, people, or relationships must not
  advance every route or create unavailable opportunities.
- Participating in a networked operation must not transfer ownership, authority, contacts, or private
  knowledge belonging to its organizer.
- Two different consequences from the same entry-level job must be capable of producing different
  later opportunities rather than only different amounts of one progression currency.
- Removing the designer-facing route labels must leave the underlying causal behavior intact; if it
  does not, the matrix has become a parallel progression system.
- Removing the new crime's distinctive pressure should make its decision history indistinguishable
  from an existing operation; if not, the claimed distinction was not load-bearing.
- Preparation must change available information or a later decision without revealing world truth.
- Holding the planner's information constant while changing only an unobserved witness must not
  change the selected plan; world truth is not tactical foresight.
- Changing perception or relevant capability may change which aftermath signals an actor notices or
  weighs, but must not create, erase, or disclose the underlying witness or trace.
- Removing the legitimate observation of a complication must remove any response that requires that
  observation unless another valid information channel supplies it.
- A report that an operation was clean must not erase an unresolved consequence or transmit its
  truth to the issuer merely because the executor said so.
- Owner, executor, witness, and unrelated viewpoints must receive different operation knowledge where
  their participation differs.
- A tactical map prototype fails its purpose if the same outcome and meaningful choices are available
  after replacing it with one confirmation button.
- Fast-forward, pause, and save/load must preserve the same authoritative result when operation time
  or scheduling changes.

## Human rulings before implementation

- Which low-tier crime should provide the first natural on-ramp scenario?
- Which origins belong in the first playable, rather than only in the long-term pool?
- Does an ordinary organizational order carry explicit compensation, an assumed share, or terms that
  vary by organization and relationship?
- Should uncertain organizational standing be presented as separate qualitative readings from known
  characters, an adviser or superior's attributed interpretation, or both?
- Which candidate routes are genuinely distinct, and which should remain cross-cutting functions?
- Which routes, if any, belong in the first playable proof?
- Is the progression matrix exclusively a design tool, or should some qualitative version appear in
  the player interface?
- Which crime creates the next distinct day-level choice?
- Which preparation fact is worth acquiring, and who can legitimately learn it?
- Should tactical foresight be modeled as personality, capability, experience, or a tested
  combination of those systems?
- Which concrete crime first earns a planning-and-unfinished-aftermath proof, and which unresolved
  consequence changes a later decision?
- When should responsibility for aftermath be explicit in an assignment, and what can the issuer
  legitimately learn about whether it was fulfilled?
- Which first crime meets the top-down map rule strongly enough to justify a prototype?
- How much live control belongs to the player when the controlled character is absent?
