# Organizations, Alliances, and Culture

**Status: noncanonical design proposal.** This preserves future design candidates. It does not
authorize a milestone, add a second organization, or alter the current organization model.

## Feature intent

Organizations should become recognizable because particular people, offices, relationships,
activities, norms, and histories reinforce one another—not because every member receives one global
gang modifier. Growth should create internal power centers and external obligations as well as reach.

The smallest future proof is a second organization whose members prefer different opportunities for
understandable reasons, while an eligible member can still cross the apparent archetype when his own
knowledge, relationships, capabilities, and pressures justify it.

## Archetypes are affinities, not locks

An organization archetype may seed or summarize:

- familiar crime and business domains;
- recruitment networks and the kinds of capabilities it can find easily;
- territorial or institutional access;
- public reputation and expected methods;
- established suppliers, fences, officials, professionals, or communities;
- inherited norms about secrecy, violence, tribute, family, and independence.

It must not hard-lock crimes or recruits. A burglary crew can enter trafficking after learning about
a route and recruiting the necessary people; a traditional organization can hire an outsider when
need, trust, leverage, or capability outweighs custom. The unusual choice should carry believable
costs and reactions rather than an unavailable button.

## Culture and internal subcultures

Culture is a pattern of expectations transmitted through leaders, offices, recruitment, rewards,
punishments, stories, and repeated practice. It may affect which actions seem normal, prestigious,
shameful, dangerous, or disloyal. It does not overwrite personal traits, knowledge, or relationships.

Subcultures can form around crews, offices, neighborhoods, generations, professions, prisons, or
influential people. Two crews under one boss may differ because they learned different norms and
answer to different lieutenants. A leadership change may alter policy immediately while culture
changes slowly and unevenly.

A culture value earns implementation only by naming a candidate, evaluation, interpretation, or
consequence that reads it. An aggregate `OrganizationCultureScore` with no character-level writer and
reader would repeat the stat-container model canon rejects.

## Hierarchy, offices, and ordering relationships

Formal offices define responsibility and authority; personal relationships determine how orders are
interpreted and enforced. A character may obey, negotiate, delay, exceed authority, conceal failure,
refuse, or delegate again. Capability remains separate from permission.

Knowledge of hierarchy is actor-relative. A boss may know every formal office but misunderstand an
informal faction. A rival or detective may know that one lieutenant reports upward without knowing
the original issuer. UI projections should therefore show known, reported, or suspected command
edges rather than the authoritative organization graph.

## Delegating territory and creating internal power

At sufficient scale, a leader may assign a district, route, business cluster, or crew responsibility
to a trusted subordinate. The assignment grants an area of responsibility and whatever resources,
authority, constraints, and reporting expectations were actually communicated. It does not transfer
perfect knowledge or guaranteed loyalty.

Delegated territory can create:

- greater reach and insulation for the leader;
- local autonomy and incomplete reporting;
- personal loyalty between the territory's people and its administrator;
- opportunities to skim, build alliances, or conceal problems;
- rivalry between officeholders;
- resentment when an ambitious or status-driven character is repeatedly passed over;
- succession tension if a subordinate's practical power exceeds his formal rank.

Frustration must arise from a character valuing the appointment, believing it was available, and
interpreting the decision through his relationships and information. `Ambitious -> mutiny chance` is
not an acceptable implementation.

## Diplomacy and anti-snowball behavior

Potential relationships include alliances, coalitions, rivalries, truces, joint ventures, secret
cooperation, tribute, client/protector arrangements, and temporary coordination against a shared
threat. These relationships carry specific participants, understood terms, obligations, information,
and consequences for breach.

Smaller organizations may coalition against a dominant power only when relevant decision-makers
perceive a threat, know or can contact plausible partners, and believe cooperation is preferable to
submission, neutrality, flight, or opportunism. Objective map share or a hidden global strength score
must not automatically summon a balancing coalition. A coalition may fail to form because the threat
was hidden, the members distrust each other, or no actor can coordinate them; that is an honest result.

## Cross-institutional relationships and corrupt origins

Criminals may form reciprocal relationships with police, politicians, lawyers, businesspeople,
journalists, labor figures, prison officials, and other institutional actors. Corruption should use
the existing relationship and information language: trust, fear, obligation, grievance, claims,
evidence, testimony, secrets, access, and mutual exposure. It is not a permanent purchased modifier.

A dirty police officer, compromised lawyer, political fixer, or business owner is a compelling origin
because an office changes access, authority, duties, and risks while the character remains part of the
same causal world. Fully playable institutional origins remain post-MVP; the crime-first game should
first prove these characters as allies, threats, and institutions around the criminal protagonist.

## Potential milestone slices, not a sequence

1. **Second-organization contrast:** add one rival crew with a different opportunity/recruitment
   profile and prove that affinities affect availability or evaluation without hard locks.
2. **Known hierarchy projection:** show one formal and one inferred command edge to two viewpoints
   with different knowledge; no organization truth graph reaches either.
3. **Territory responsibility:** after territory exists, assign one area to a subordinate and prove
   the leader gains reach while losing direct progress knowledge.
4. **One reciprocal alliance:** two decision-makers negotiate one bounded joint activity with an
   explicit obligation and a reachable breach consequence.
5. **One subculture reader:** introduce culture only when a concrete character decision can read a
   norm differently from personal preference.
6. **One institutional relationship:** apply existing relationship/information mechanics to one
   police, political, legal, or business character before considering a playable career.

Each slice requires separate authorization and a natural scenario. None is part of milestone 027.

## Falsifiers

- Removing archetype affinity must change at least one eligible opportunity or evaluation while the
  atypical path remains reachable.
- A coalition decision must change when the deciding actor lacks knowledge of the threat, even though
  authoritative world power is identical.
- Removing a boss must not automatically transfer every relationship, belief, or informal allegiance
  to the replacement.
- A territory delegate must not give the leader free access to the delegate's progress or local
  knowledge.
- A cultural norm and a personal trait must be capable of pulling one character in different
  directions; merging them should fail a focused test.
- Controlled and autonomous actors must use the same underlying organization action path.

## Human rulings before implementation

- Which organization contrast is worth proving first?
- Which archetype affinities change actual candidate availability, and which change only salience or
  evaluation?
- What information and authority does a territory appointment communicate?
- What is the smallest alliance object that cannot be represented by existing assignments,
  relationships, and testimony?
- Which culture distinction has a named decision reader?
