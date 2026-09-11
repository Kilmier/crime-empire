# UI and Player Legibility

**Status: noncanonical design proposal.** This document defines a target information architecture
and a representative UI prototype. It is indexed by `docs/proposals/README.md`; it does not authorize
a production UI milestone, choose a map projection, or require unimplemented simulation systems to
be built first.

## Purpose

The current Godot interface is a deliberately plain validation shell: panels, labels, buttons,
event text, and choices. That shell has proved that the simulation can be driven and that player
projections can be tested, but it is not yet the intended interface for the game.

Crime Empire needs a UI in which the strategic overview, character relationships, incomplete
information, emergent problems, and immediate decisions coexist. The player should feel both:

- the persistent world and organizational structure of a grand strategy game; and
- the personal, surprising, interpretable incidents of an emergent story game.

The first target is therefore a **representative vertical slice**, not a finished art pass. It should
show what the game means when all currently proven presentation rules are placed in one coherent loop.

## Design goals

The interface should let the player answer, quickly and repeatedly:

1. **Where am I?** What district, business, operation, or institution is relevant?
2. **Who matters?** Which people, organizations, and relationships are involved?
3. **What is changing?** What new trace, pressure, rumor, commitment, or consequence appeared?
4. **What do I know?** Which parts are observed, reported, suspected, contested, or unknown?
5. **What can I do?** Which actions are available now, and what risks or commitments do they create?

The UI should make a problem legible without turning it into a quest marker. It should make a choice
understandable without revealing the simulation's omniscient answer.

## Non-goals

This proposal does not yet decide:

- isometric versus flat 2D map presentation;
- final pixel-art style, animation, or sound direction;
- a tactical heist map;
- the exact number of districts or businesses;
- how every future institution is rendered;
- which hidden values are exposed as numbers;
- a complete dashboard for every simulation subsystem.

The interface structure comes before those choices. A flat prototype can prove the structure before a
map projection or art style is committed.

## Three connected views

The game should have three persistent views that share one player-relative world state rather than
presenting separate systems.

### 1. Strategic overview

The overview answers where events and opportunities are situated.

It should eventually show:

- districts and meaningful locations;
- businesses and known operations;
- organizations and their visible presence;
- institutional pressure or protection;
- influence, ownership, and contested space as distinct signals;
- incomplete intelligence with source and confidence cues;
- active problems or developing situations tied to locations.

The map is an information surface, not an omniscient board. Unknown operations should not appear as
known icons merely because the simulation has them. A location may show a rumor, a recent trace, a
reported police presence, or no information at all depending on the viewpoint.

### 2. People and organization view

The people view answers who matters and why.

For a known character it should combine:

- name, role, organization, and current location;
- the player's personality reading, not an omniscient personality sheet;
- relationship standing and its history;
- current pressures and visible commitments;
- known capabilities or uncertainty about them;
- active operations and responsibilities;
- recent observable behavior and attributed accounts;
- reasons a standing or impression changed.

Organization views should show people, offices, obligations, and influence as a network. An office is
not a substitute for the person holding it. A subordinate's behavior should remain attributable to
that character and their relationships rather than being flattened into an organizational stat.

#### Known hierarchy and command

The people view should be capable of showing who appears to answer to whom, which offices carry
authority, and which orders or responsibilities connect characters. This is a viewpoint-relative
organization chart, not the authoritative graph. A detective, outsider, subordinate, and boss may
each know different parts of the same hierarchy, and an informal power relationship may contradict
the formal chain of command.

The UI must distinguish a known office from a suspected influence, and formal authority from a
character's willingness or practical ability to obey. Unknown members and hidden intermediaries do
not appear merely because they exist in simulation state.

#### Individual and group conversations

A player should eventually be able to speak with one character privately or with several characters
present. These are different information situations, not cosmetic versions of one dialogue box:

- a private exchange limits immediate witnesses but places more weight on the two participants;
- a group exchange tells each present character only what was actually said in the room;
- participants may react differently, conceal their reaction, repeat the exchange later, or form
  conflicting interpretations;
- an order given publicly may create different obligation, humiliation, or deniability than the same
  order given privately.

Conversation should use the existing claim, testimony, impression, relationship, and assignment
language where it fits. This proposal does not authorize a separate dialogue simulation or free-form
language system.

### 3. Situation and decision view

The decision view answers what is happening and what can be done now.

Every important situation should present:

- a plain-language description of the immediate situation;
- what the player directly knows;
- what came from another person or institution;
- uncertainty, contradiction, or missing information;
- the people and locations implicated;
- available actions;
- visible immediate costs and commitments;
- risks or consequences that are plausible but not guaranteed.

The interface should not present a developer utility breakdown as the player's explanation. It should
translate causes into in-world language while preserving uncertainty.

## Proposed shell

The first representative prototype can use a stable shell like this:

```text
┌──────────────────────────────────────────────────────────────────────────────┐
│ DATE / TIME        CASH        CONTROLLED CHARACTER       SAVE / ADVANCE      │
├───────────────┬──────────────────────────────────────────┬───────────────────┤
│ CITY / DISTRICT│ CURRENT SITUATION                        │ PEOPLE / NETWORK  │
│               │                                          │                   │
│ map or        │ what is happening                        │ selected person   │
│ location view │ what you know                            │ relationship      │
│               │ what is uncertain                         │ pressures         │
│               │ what can be done                         │ commitments       │
├───────────────┴──────────────────────────────────────────┴───────────────────┤
│ RECENT TRACE / CHRONICLE: what changed, who reported it, what remains open   │
└──────────────────────────────────────────────────────────────────────────────┘
```

This is a structural wireframe, not a final layout. The key requirement is that the player can move
from a place, to a person, to a developing problem, to a decision without losing the context that
made the decision meaningful.

## Problems, not quests

The interface should surface **problems** rather than issue a mission checklist.

Examples:

- “Collections from the south shop have not arrived. Tommy says the route was watched. You have no
  account from Angelo.”
- “Salvatore believes someone is withholding information, but he has not named whom.”
- “A detective is asking about the same business another source says was untouched.”
- “Vito is waiting on your word. He believes the police have closed one route, but he may be wrong.”

Each problem can offer actions, but it should remain possible to ignore it, misunderstand it, or let
it develop. The UI should show what is hanging over the player and the people around them, not declare
the one correct quest response.

## Information and uncertainty language

Every displayed fact should belong to a player-facing information category:

- **Observed:** the viewpoint directly saw or experienced it.
- **Reported:** another source gave an account.
- **Rumored:** the information arrived through an indirect social channel.
- **Inferred:** the player can reasonably connect known traces, but the game should not claim certainty.
- **Contested:** sources disagree or the holder's account conflicts with another account.
- **Unknown:** the simulation may know it, but the player does not.

The UI should communicate source, recency, and confidence where they matter. A badge, icon, or short
phrase is preferable to exposing a raw internal enum. The same event may appear differently for
different viewpoints.

No map marker, relationship label, or character summary may silently promote world truth into player
knowledge.

## Character legibility

Personality and relationships should be shown through a mixture of qualitative profile language,
history, and behavior.

Good:

- “He keeps his head when the room turns dangerous.”
- “You have had little dealings with him to go on.”
- “He has started to doubt your account.”
- “She is waiting for you to decide whether the risk is worth it.”

Bad defaults:

- raw omniscient trait percentages;
- a universal loyalty bar with no direction or history;
- a hidden relationship state displayed as fact;
- a personality label that guarantees a behavior;
- a decision explanation that names private motives the viewpoint has not learned.

The player may receive more explicit profile information through conversation, reputation, dossiers,
or repeated observation. Legibility should grow through play rather than begin as an encyclopedia.

## Time and pacing

Advancing time is not merely pressing a skip button. Before advancing, the player should see:

- active commitments and operations;
- people who are waiting, exposed, or under pressure;
- unresolved problems likely to change;
- the next meaningful pause or decision boundary;
- whether an action will consume time or leave someone unattended.

After time advances, the interface should provide a compact consequence review:

- what changed in the world;
- what the player directly learned;
- what was reported or rumored;
- what new problem or opportunity emerged;
- which operations progressed or completed;
- which relationships or impressions visibly moved.

The review should be a chronicle of causes and consequences, not an unfiltered event dump.

## Emergent-story surface

The game needs a persistent history surface in addition to transient decision screens. This can begin
as a simple **chronicle** and grow later.

Each chronicle entry should answer:

- when it happened;
- what the player knows about it;
- who or what was involved;
- what trace or consequence remains;
- whether the account is confirmed, uncertain, or disputed.

The chronicle should support reconstruction without becoming an omniscient replay log. It should not
show every autonomous decision, only events and traces that reached the player's information world.

### News and headlines

A news surface can expose public, institutionally released, or widely reported events: a rival
organization's visible activity, a raid, a killing, a business opening, an election result, a trial,
or an unexplained disruption. A headline is a source-limited account, not world truth. It may be
incomplete, politically framed, mistaken, or based on an official statement.

News should create leads and context rather than quests. A report of unusual activity in another
district may give the player a reason to ask a contact, inspect a location, or ignore it. The surface
must retain publication time, source or outlet, and the claims actually made; it must not reveal the
rival's private motive, complete organization, or hidden operation.

## Representative vertical slice

The first UI prototype should use one bounded scenario and make the following coexist:

1. A district or location overview.
2. A roster of known characters.
3. A selected character's relationship and personality reading.
4. One active operation with visible progress or uncertainty.
5. One developing problem or pressure.
6. One account that may be incomplete or wrong.
7. One decision with multiple plausible options.
8. A time advance that produces a visible consequence.
9. A chronicle entry showing what the player learned and what remains unresolved.
10. One private or group exchange whose participants and information consequences are visible.
11. One headline or public report that provides a lead without certifying the hidden truth.

The failed-heist / police-pressure situation is a good future test for personality, relationship, and
playable-failure presentation, but the prototype should not wait for the full police system. A staged
or existing operation scenario can prove the information architecture first, with missing systems
clearly marked as future data.

## Visual language before final art

Before committing to flat or isometric presentation, establish a visual grammar:

- location and district identity;
- organization and relationship connection;
- operation state;
- uncertainty and source;
- pressure and urgency;
- player-controlled versus observed versus inferred information;
- selected character and selected problem.

Flat 2D is likely the cheapest first prototype because it keeps the information layout readable and
lets the map remain schematic. Isometric presentation can be evaluated later if it improves spatial
understanding rather than merely adding visual style.

## Prototype sequence

1. **Information architecture prototype:** static or fixture-backed shell with the three views,
   uncertainty language, selected person, active problem, and chronicle.
2. **Current-system binding:** connect the shell to `PlayerSnapshot`, `PlayerOccasion`, roster,
   operation, and relationship projections already proven by the simulation.
3. **Map and location layer:** add districts, businesses, and known operation markers without exposing
   hidden state.
4. **Story-density pass:** add more people, organizations, and institutions only after the shell can
   show how their actions create visible problems and consequences.
5. **Visual polish:** commit to the art direction, projection, animation, and final responsive layout
   after the information architecture survives playtest.

## Validation criteria

The UI prototype is successful if a new player can:

- identify the current situation without reading a raw event log;
- tell the difference between observation, report, rumor, inference, and unknown information;
- find the people and relationships relevant to a problem;
- understand why each available action is plausible without seeing hidden utility scores;
- advance time and identify what changed afterward;
- reconstruct at least one emerging story from the chronicle and visible traces;
- make a choice that is not obviously prescribed by a quest marker.

It is not successful merely because every simulation field can be displayed.

## Implementation boundary

This proposal does not authorize a production GUI rewrite. It defines the next design target: a
representative player-legibility prototype that can be reviewed before the project commits to a final
map style or a large presentation layer. Scope should be placed in `CURRENT_MILESTONE.md` only after
the information architecture and prototype slice are accepted.

## Related documents

- `docs/GAME_VISION.md` — visual direction, emergent narrative, imperfect information, and shared
  agency.
- `docs/SIMULATION_ARCHITECTURE.md` — bounded deliberation, causal parity, and information flow.
- `docs/INFORMATION_AND_LEGIBILITY.md` — player-relative knowledge and explanation requirements.
- `docs/ROADMAP.md` — current demo arc and deferred map/art/presentation work.
- `docs/CURRENT_MILESTONE.md` — the only authorized active scope.
