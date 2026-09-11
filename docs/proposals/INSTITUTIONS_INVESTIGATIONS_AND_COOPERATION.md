# Institutions, Investigations, and Cooperation

**Status: noncanonical design proposal with a canonical cooperation dependency.** This document
preserves compatible but unsettled ideas about institutional knowledge and authorization. It does
not authorize a police organization, case system, warrant procedure, arrest, prosecution, playable
institutional career, or change to the current detective spike.

## Canonical foundation

The following constraints are already settled and are not proposals introduced here:

- truth, traces, observations, claims, beliefs, rumors, and evidence remain distinct
  (`INFORMATION_AND_LEGIBILITY.md` §Information States);
- information is actor-relative and moves only through legitimate observation, discovery, testimony,
  reporting, or inference (`INFORMATION_AND_LEGIBILITY.md` §Core Information Flow,
  §Sources and Channels, and §Reporting and Distortion);
- organizations coordinate through characters, offices, assignments, and reports rather than a hive
  mind (`GAME_VISION.md` §Characters and Organizations; `SIMULATION_ARCHITECTURE.md`
  §Organizational Intent and Coordination; `INFORMATION_AND_LEGIBILITY.md` §Organizations and
  Internal Reporting);
- authority is separate from capability, so an unauthorized action can remain possible and create
  consequences (`GAME_VISION.md` §Authority Is Not Capability;
  `SIMULATION_ARCHITECTURE.md` §Assignments and Interpretation);
- ambient attention, investigator belief, case evidence, and prosecution readiness are different
  (`INFORMATION_AND_LEGIBILITY.md` §Investigations and Case Legibility);
- an arrested or pressured character decides whether to cooperate from their own beliefs,
  relationships, exposure, likely consequences, family pressure, fear, grievances, and the offer
  (`GAME_VISION.md` §Arrests, Pleas, and Cooperation);
- cooperation can transfer only what that character actually knows or plausibly believes. It never
  reveals an authoritative organization graph (`GAME_VISION.md` §Arrests, Pleas, and
  Cooperation).

## Proposal: fragmented institutional knowledge

An institution should not receive one automatically synchronized knowledge state. Individual
investigators, supervisors, prosecutors, units, offices, task forces, courts, banks, businesses, and
other institutions may hold different claims, observations, records, testimony, and evidence.

Information sharing should require a real access or transmission path. Relevant conditions may
include jurisdiction, office, assignment, case membership, authorization, relationships, source
protection, institutional priority, bureaucracy, competence, corruption, and available resources.
A report communicates only what its sender can and chooses to communicate; a database or case file
makes only its stored material available to actors permitted and able to consult it.

This proposal does not settle the storage model. Information might belong to a character, a durable
record or evidence item, a case accessible through an office, or some deliberately bounded
combination. An `InstitutionKnowledge` collection that silently copies everything known by every
member would fail the intent.

Transfers, promotions, elections, unit boundaries, joint operations, and leadership changes can
therefore alter which fragments are connected or acted upon without changing the underlying truth.
A correct detective may fail to persuade a supervisor; a corrupt supervisor may suppress a credible
request; a later task force may connect records that separately appeared unimportant.

## Proposal: observations can gain significance later

The canonical information states do not form a mandatory
`trace -> observation -> attribution -> evidence` pipeline. Attribution is ordinarily a claim or
inference connecting an actor, event, location, or organization to something observed. Evidence is
material that can support a claim before a particular audience and may exist before anyone has made
the relevant attribution or opened a case.

The proposed behavior is narrower:

- a trace may be observed and preserved without the observer recognizing why it matters;
- an observation or record may support no current inference;
- later testimony, evidence, or a second observation may trigger reconsideration;
- the actor who gains the new information may connect an older item only if they can legitimately
  access or learn about both;
- the resulting attribution remains a belief or claim that can be mistaken, contested, or still
  institutionally unusable.

This creates delayed investigative discoveries without retroactively granting knowledge to everyone
who once encountered a trace.

## Proposal: institutional authorization requests

Some institutional actions should require one character or office to request authorization from
another. Possible applications include surveillance resources, records access, searches, raids,
arrests, charging decisions, witness protection, asset action, or reassignment. This list is
illustrative, not an approved procedural catalogue.

A minimal authorization exchange contains:

- the requested action and target;
- requester and proposed executor;
- authorizing office or character;
- claims, testimony, observations, and evidence actually presented;
- claimed source and reliability where known;
- relevant institutional rule, priority, resource, or jurisdiction;
- the authorizer's own knowledge, relationships, motives, and potential corruption;
- approval, denial, limitation, delay, or a request for more support;
- a transmitted decision and any persistent consequence.

The authorizer must not consult world truth or the requester's uncommunicated reasoning. Approval is
not a global evidence threshold. Two equally informed authorizers may judge the same request
differently for character- and institution-grounded reasons.

Lack of authorization need not make action physically impossible. A capable actor may exceed or
evade authority, creating risk of suppression, dismissal, retaliation, scandal, prosecution, a
damaged case, or another consequence appropriate to the eventual abstraction.

## Canonical payoff: cooperation and flipping

Cooperation is not a new proposal. It is included here so a future arrest or institutional milestone
does not implement removal from play while forgetting the canonical information payoff.

An arrested or pressured character may remain silent, cooperate, lie, redirect, or offer only part
of what they hold. Their choice depends on their own perceived case, likely outcome, relationships,
fear, grievances, family pressure, trust, protection, ambition, and the actual offer. Any account
produced enters the same claim/testimony/evidence world as other information and can be incomplete,
false, sincere but mistaken, or difficult to corroborate.

## Potential milestone slices, not a sequence

1. **Two institutional holders:** two police characters or offices hold different claims about one
   event; one explicit report changes only its recipient's available information.
2. **One authorization request:** an investigator asks one authorizer for one bounded action using
   only material actually presented, with approval and denial both naturally reachable.
3. **One old observation reconsidered:** later information causes one eligible actor to form a new
   inference from a preserved observation that previously supported no such claim.
4. **One cooperation decision after arrest:** a pressured character chooses among bounded responses
   and can transmit only positions and sources they actually hold.

Each slice requires independent authorization. None is part of the current milestone merely because
it appears here.

## Required falsifiers

- Removing the sharing report must leave the recipient without the sender's claim while authoritative
  truth stays identical.
- Two institutions or offices exposed to different records must be able to reach different decisions
  about the same event without either reading the other's state.
- A later discovery must affect an old observation only for an actor who can legitimately access
  both; granting the connection globally must fail a negative control.
- Removing the preserved old observation must prevent the later attribution even when the later clue
  still arrives.
- An authorizer's result must change when presented support changes, but not when only hidden world
  truth or the requester's private belief changes.
- A denied request must not erase physical capability; an unauthorized action and its consequences
  must remain reachable where the scenario permits it.
- Cooperation must never transfer a claim, person, office, or organization edge the cooperating
  character neither knows nor plausibly believes.
- Controlled and autonomous requesters, authorizers, and cooperating characters must use compatible
  causal paths.

## Honest non-results

A justified request may be denied, an old observation may remain meaningless, two units may fail to
share, and an arrested character may reveal nothing useful. Those are acceptable outcomes when they
follow from the actors' information and circumstances. A fixture should not be tuned merely to force
the dramatic branch; staged boundary tests can prove reach while the natural run records its actual
result honestly.

## Human rulings before implementation

- What information belongs to individuals, durable records, cases, offices, or institutions?
- Which first institutional action requires authorization, and who may grant it?
- Which conditions belong in the first authorization decision rather than later legal depth?
- When and how is an old observation reconsidered without continuously rescanning all history?
- What minimal arrest pressure and offer make the first cooperation decision meaningful?
- Which unauthorized institutional actions remain possible, and which consequences can the first
  slice honestly represent?
