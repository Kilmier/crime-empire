# Procedural City and Urban Ecology

**Status: noncanonical long-term design proposal.** Matt accepted this direction for preservation on
2026-09-12. That acceptance does not make it canon, authorize implementation, establish milestone
order, or alter the first-playable scope. The exact district model, generation algorithm, content
catalogue, and presentation remain unresolved.

## Feature intent

The city should be a causal environment rather than a backdrop or a board divided into exclusive
territory colors. Geography, transport, economic class, employment, businesses, communities,
institutions, jurisdictions, organizations, and prior events should combine to create different
opportunities, pressures, information paths, and relationships.

Procedural generation earns its cost only when two cities create meaningfully different problems
under the same simulation rules. Different names, art, or payout modifiers are not enough.

> **Generate the city's starting causes, not its future story.**

## Preserved working direction

A campaign city is generated once at campaign creation and then persists. Generation uses a
deterministic seed to assemble authored components under systemic and historically coherent
constraints. Authored content supplies the available mechanics, economic activities, geographic
features, institution types, organization forms, historical-event patterns, and opportunity
requirements. The generator combines those components; it does not invent new game rules.

After the starting state exists, the generator stops controlling it. Businesses close, industries
change, officials are replaced, neighborhoods develop, organizations rise or collapse, and influence
moves because of simulation events and character decisions. Loading, revisiting, or advancing time
must never reroll the city around the player.

This is a hybrid direction:

- authored components provide meaning, historical plausibility, and testable behavior;
- constrained procedural composition provides variation and replayability;
- deterministic generation supports reproduction and replay;
- persistent simulation turns the generated starting conditions into a unique history.

## Layers of the generated starting state

These layers describe the intended causal coverage, not a required first implementation schema.

### Physical structure

- districts or neighborhoods and their connections;
- major geography such as waterfronts, rivers, or other barriers;
- transport anchors such as rail, roads, ports, and regional routes;
- industrial, commercial, residential, and civic concentrations;
- travel, access, and strategically important locations.

The authoritative simulation may begin with a district graph. Street-by-street geometry and a
specific flat or isometric rendering are separate presentation questions.

### Economic and population structure

- industries, employers, commerce, housing, and legitimate businesses;
- wealth distribution, employment, density, and changing local conditions;
- labor, consumer, illicit-market, and logistics opportunities;
- aggregate background populations from which relevant persistent characters may emerge.

A district should ordinarily contain a mixture rather than behave as one exclusive biome. Economic
class may affect resources, access, pressure, vulnerability, and institutional response. It must not
directly assign criminality, morality, personality, or inevitable allegiance.

### Municipal and institutional structure

- police, political, legal, administrative, labor, business, media, civic, and community presence;
- offices occupied by persistent characters where their decisions matter;
- overlapping jurisdictions and service boundaries that need not align with neighborhoods;
- institution-specific resources, access, constraints, and information.

Unions, political parties, courts, precincts, newspapers, and other institutions may eventually
participate in this layer, but this proposal does not settle their internal mechanics or authorize
them for the first generator slice.

### Organizations and influence

- criminal organizations, independent operators, legitimate businesses, and other relevant groups;
- offices, responsibilities, suppliers, clients, patrons, protectors, and recruitment networks;
- formal ownership, criminal presence, economic access, political influence, public tolerance, and
  institutional protection as overlapping rather than interchangeable relationships.

Generation must not flatten these into one district-control value. Organizations remain networks of
characters and offices, and unusual alliances or conflicts must still arise from people, information,
relationships, and interests.

### Bounded prehistory

A small generated prehistory may explain why the starting state exists. Examples include an industry
closing, a transport connection redirecting commerce, an officeholder being promoted, an organization
losing a leader, a political arrangement protecting a business, or an old conflict leaving a debt or
grievance.

These events earn inclusion only when they produce current causal state such as ownership,
relationships, claims, records, access, reputation, pressure, or institutional conditions. A random
backstory paragraph that no system reads is flavor, not simulated history.

### Starting knowledge

The generated authoritative city is not automatically the city any character knows. Broadly public
facts may be widely available, while an origin, occupation, location, relationship, office, or
participation in prehistory may justify more specific knowledge.

A dockworker may know a shipment routine; an organization subordinate may know an immediate superior;
a public official may know their own jurisdiction; a neighborhood resident may know local people and
rumors. None of them receives hidden operations, corrupt relationships, informal hierarchy, private
investigations, or the complete organization graph merely because the generator created those facts.

Player and non-player characters use the same authoritative city and action rules. Different beliefs,
access, authority, relationships, and capabilities may produce different available candidates without
creating a second player-only city model.

## Conceptual generation order

The intended causal order is:

```text
campaign and era context
  -> physical anchors and district connections
  -> economic and aggregate population conditions
  -> municipal institutions and overlapping jurisdictions
  -> businesses, organizations, offices, and important characters
  -> bounded causal prehistory
  -> character-relative starting knowledge
  -> deterministic viability validation
  -> persistent campaign simulation
```

This order is conceptual rather than an implementation prescription. Exact passes, data structures,
random-stream derivation, validation, and repair rules remain engineering questions until a smaller
behavioral proof identifies which generated variables are load-bearing.

## Viability without prescribing a story

An accepted starting city should satisfy bounded structural requirements, such as being connected,
having valid offices and locations, and providing the selected origin at least one plausible and
knowable low-tier opportunity. Those guarantees make a campaign playable; they must not guarantee a
particular success, rivalry, betrayal, coalition, or future narrative.

A quiet district, a failed alliance, an opportunity the player never discovers, or an organization
that remains stable can all be honest results. The generator establishes conditions. Characters and
systems determine what happens next.

## Smallest proof before procedural generation

Before implementing a city generator, create two authored city configurations using the same minimal
city vocabulary. Their economic, physical, or institutional differences must change at least one
eligible opportunity, access path, decision, response, or persistent consequence for an equivalent
character.

A harbor-oriented configuration and an inland industrial configuration are possible contrasting
fixtures, not settled content requirements. If the configurations differ only cosmetically, the
proposed variables have not earned procedural generation. Only distinctions proven to affect play
should enter a generator.

The canonical first-playable target remains one city with roughly six districts or neighborhoods.
This proposal does not enlarge that scope or require the first playable to generate its city.

## Potential milestone slices, not a sequence

1. Define the smallest city-context vocabulary only after naming the decisions or processes that
   read each field.
2. Build two authored contrasting fixtures and demonstrate a player-visible, causally explainable
   difference under shared actor rules.
3. Generate only the load-bearing physical and economic distinctions proven by those fixtures from
   a deterministic seed.
4. Project different starting knowledge to two character origins while keeping authoritative city
   truth unchanged.
5. Add one bounded historical event only after an existing relationship, information, ownership, or
   institutional reader can consume its result.

Each slice requires separate authorization through `CURRENT_MILESTONE.md`. None is part of milestone
027 merely because it is recorded here.

## Falsifiers

- The same seed, configuration, and build must reproduce the same authoritative starting city.
- Different seeds must be capable of changing an eligible opportunity, access path, institutional
  response, decision, or later consequence—not merely names or visual arrangement.
- Removing a geographic, economic, or institutional input must change the behavior claimed to depend
  on it; otherwise that input is decorative.
- Holding city truth constant while changing a character's justified starting knowledge must change
  what that character can see or consider without changing authoritative truth.
- An undiscovered operation, corrupt relationship, informal command edge, or private investigation
  must remain absent from an uninformed viewpoint.
- Removing a generated historical event must remove or alter the present relationship, claim,
  ownership, access, reputation, pressure, or condition attributed to it.
- Flat and isometric presentations must be able to consume the same city state without changing
  simulation outcomes.
- Changing aggregate population conditions must not require instantiating every resident as an active
  character.
- Demographic identity or economic class must not directly write criminal propensity, personality,
  guilt, or allegiance.
- Saving, loading, revisiting, or advancing time must not regenerate the starting city or erase later
  simulated changes.

## Explicitly deferred

- exact district count, district vocabulary, and variable ranges beyond existing first-playable canon;
- the generation algorithm, random-stream layout, deterministic repair, and generator versioning;
- street, parcel, building-interior, tilemap, art, and navigation generation;
- flat 2D versus isometric presentation;
- the depth and number of generated prehistory events;
- exact public knowledge and origin-specific starting-knowledge packages;
- fictional-composite versus historically reconstructed cities;
- detailed demographic categories and their representation;
- full unions, political parties, courts, precincts, and other institution simulations;
- multi-city and state-scale simulation;
- historical technology regimes and century-long urban transformation;
- procedural tactical crime or heist maps.

## Human rulings before implementation

1. Which city variables produce the first authored contrast worth testing?
2. What exact structural viability must every starting seed guarantee?
3. Which facts are public at campaign start, and which depend on origin or relationships?
4. How much prehistory is required before the city feels inhabited without becoming an unobservable
   lore generator?
5. Should the setting use fictional composite cities, historically reconstructed cities, or both as
   different campaign modes?
6. Which population and community distinctions create legitimate gameplay readers without reducing
   identity or class to criminal propensity?

## Related documents

- `docs/GAME_VISION.md` — overlapping territory, one-city first-playable scope, incomplete
  intelligence, background populations, and long-term state-scale direction.
- `docs/SIMULATION_ARCHITECTURE.md` — relevance tiers, aggregate populations, deterministic time, and
  actor-neutral candidate generation.
- `docs/INFORMATION_AND_LEGIBILITY.md` — traces, character-relative knowledge, reports, public
  information, and player-facing projection.
- `docs/proposals/CRIME_AND_LOCATION_OPERATIONS.md` — criminal origins, on-ramps, location operations,
  and opportunity requirements.
- `docs/proposals/ORGANIZATIONS_ALLIANCES_AND_CULTURE.md` — organizational affinities, territory,
  culture, diplomacy, and cross-institutional relationships.
- `docs/proposals/INSTITUTIONS_INVESTIGATIONS_AND_COOPERATION.md` — fragmented institutional
  knowledge and authorization.
- `docs/proposals/WORLD_ERAS_AND_DYNASTY.md` — persistent history and technological change across eras.
