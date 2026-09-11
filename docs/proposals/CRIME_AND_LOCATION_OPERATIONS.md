# Crime and Location Operations

**Status: noncanonical design proposal.** This is a vocabulary and milestone-candidate record, not an
approved crime list, a requirement to add a `LocationOperation` type, or permission to build tactical
maps. Canon currently keeps MVP heists abstract.

## Feature intent

Different crimes should create different strategic problems, relationships, traces, institutional
responses, and uses for people. A new crime earns implementation by producing a new decision or
pressure—not by changing the name and payout of an existing operation.

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

1. Add one operation whose strategic pressure differs from extort/conceal/investigate and prove the
   difference in a natural scenario.
2. Add preparation that produces one fallible, source-bearing fact used by both controlled and
   autonomous execution.
3. Generalize a shared operation lifecycle only after two implemented operations expose the same
   concrete duplication or invariant.
4. Prototype one top-down scene over existing resolution state only after the abstract operation is
   interesting and the map criteria above are met.

Each slice is independently reviewable and requires authorization through `CURRENT_MILESTONE.md`.

## Falsifiers

- Removing the new crime's distinctive pressure should make its decision history indistinguishable
  from an existing operation; if not, the claimed distinction was not load-bearing.
- Preparation must change available information or a later decision without revealing world truth.
- Owner, executor, witness, and unrelated viewpoints must receive different operation knowledge where
  their participation differs.
- A tactical map prototype fails its purpose if the same outcome and meaningful choices are available
  after replacing it with one confirmation button.
- Fast-forward, pause, and save/load must preserve the same authoritative result when operation time
  or scheduling changes.

## Human rulings before implementation

- Which crime creates the next distinct day-level choice?
- Which preparation fact is worth acquiring, and who can legitimately learn it?
- Which first crime meets the top-down map rule strongly enough to justify a prototype?
- How much live control belongs to the player when the controlled character is absent?
