# Personality and Character Profiles

**Status: noncanonical design proposal.** This document is a working specification for discussion.
It does not change the settled trait vocabulary in `DESIGN_DECISIONS.md`, authorize implementation,
or open a milestone. It is indexed by `docs/proposals/README.md`. Once rules are accepted, durable
decisions belong in `DESIGN_DECISIONS.md` and executable work belongs in `CURRENT_MILESTONE.md`.

## Purpose

The simulation already has psychology, drives, motivations, relationships, knowledge, and
capabilities. This document defines how an important character gets a recognizable personality
profile without turning personality into a list of scripted actions.

The design target is not a personality test or a universal morality score. It is a compact,
legible explanation for why two characters facing the same situation notice different options,
value different outcomes, and react differently to pressure.

## Design principles

1. **Traits shape choices; they do not fire actions.** A trait may change salience, evaluation,
   reaction, or execution. It never means `trait -> action` on a timer.
2. **Personality is only one cause.** Knowledge, beliefs, relationships, pressures, commitments,
   opportunity, capability, role, and current circumstances still gate what can happen.
3. **A trait is a behavioral contract.** Every trait must state what it affects and what it does not
   mean. A name without a distinct behavioral purpose does not belong in the vocabulary.
4. **Information remains actor-relative.** Perceptive means noticing signals more readily, not seeing
   the world truth or another character's private state.
5. **Shared agency remains intact.** The same personality rules apply to player and non-player
   characters; different roles and information produce different available choices.
6. **The vocabulary stays small.** New traits require a demonstrated behavioral distinction, not
   merely a desirable adjective.

## Profile structure

Named and otherwise decision-relevant characters receive a four-trait authored profile:

| Slot | Starting weight | Meaning |
|---|---:|---|
| Major trait 1 | 30% | A primary lens that strongly colors relevant situations |
| Major trait 2 | 30% | A second primary lens, which may reinforce or conflict with the first |
| Minor trait 1 | 20% | A secondary tendency that matters in its domain |
| Minor trait 2 | 20% | A second secondary tendency |

The 30/30/20/20 split is a starting point for tuning, not a claim that every action is 30% or 20%
caused by a trait. The weights describe the relative contribution of personality tendencies when a
trait is relevant. They sum to 100% **inside the personality profile only**; drives, relationships,
pressures, knowledge, capabilities, and circumstances remain independent causes.

The profile may contain two traits that pull in different directions. That is desirable when the
conflict is legible: a proud but cautious man may refuse humiliation while still avoiding a fight he
does not think he can win. A trait does not become a hard lock merely because it is major.

Background characters may use a reduced or default profile until they become relevant enough to
promote into the active decision model. The four-slot profile is primarily an authored identity and
legibility contract for named characters, not a requirement that every person in the city receive
the same computational depth.

## Separation from adjacent systems

| System | Question it answers | Example |
|---|---|---|
| Personality traits | How does he tend to interpret and evaluate a situation? | Collected keeps deliberation intact under pressure |
| Drives | What broad outcomes does he value? | Wealth, status, security, belonging |
| Motivations and pressures | What is pushing him now? | Legal exposure, resentment, revenue shortfall |
| Relationships | Who does he trust, fear, owe, or resent? | Trust in the player makes coordination more credible |
| Knowledge and belief | What does he think is happening? | He believes the police have identified the getaway car |
| Capabilities | What can he actually do? | Investigation, coercion, discretion, persuasion |
| Commitments and roles | What ongoing obligation or office constrains him? | He is responsible for a district's collections |

Traits must not become renamed skills, drives, or relationship dimensions. Perceptive is not
Investigation; Charismatic is not Persuasion; Cold-Blooded is not Coercion; Collected is not
invulnerability to fear.

## Trait dictionary

The four traits below are the current repository vocabulary. The four following traits are proposed
extensions for this design pass. All traits may be assigned as either major or minor; major/minor is a
profile role, not a separate trait type.

### Existing vocabulary

#### Aggressive

- **Meaning:** coercion, escalation, and force feel more available and less costly.
- **Primary channels:** coercive-option salience; perceived force effectiveness; escalation cost.
- **Does not mean:** automatic violence, high coercion skill, or inability to negotiate.
- **Failure expression:** chooses an escalatory option when a slower or quieter option would have
  served him better.

#### Cautious

- **Meaning:** uncertainty, exposure, and irreversible risk receive more weight.
- **Primary channels:** concealment, delay, and delegation salience; perceived risk; uncertainty
  penalty.
- **Does not mean:** cowardice, passivity, or always choosing safety.
- **Failure expression:** protects against a risk so aggressively that he gives up a valuable opening.

#### Proud

- **Meaning:** status loss, humiliation, and deference carry extra personal cost.
- **Primary channels:** cost of backing down; resistance to deference; reaction to disrespect.
- **Does not mean:** courage, competence, or hostility toward everyone.
- **Failure expression:** rejects a useful compromise because accepting it would lower his standing.

#### Suspicious

- **Meaning:** second-hand claims and unexplained motives deserve additional scrutiny.
- **Primary channels:** hostile-intent salience; corroboration requests; discounting weak testimony.
- **Does not mean:** accurate detection, omniscience, or permanent distrust.
- **Failure expression:** treats a truthful but poorly sourced account as a threat or misses a genuine
  ally while seeking proof.

### Proposed extensions

#### Collected

- **Meaning:** maintains deliberate thought when events become frightening, confusing, or urgent.
- **Primary channels:** panic and uncertainty evaluation; persistence of negotiation, retreat, and
  coordination options; reaction timing under immediate threat.
- **Does not mean:** fearlessness, bravery, competence, or immunity to pressure.
- **Example:** during a police standoff, Vito does not automatically surrender or flee if he still
  sees a credible route to coordinate with the player or negotiate an escape.

#### Perceptive

- **Meaning:** attends to weak signals, inconsistencies, and changes in other people's behavior.
- **Primary channels:** salience of traces, discrepancies, social cues, and corroboration; selection
  of observation or inquiry options.
- **Does not mean:** objective accuracy, investigative skill, or access to private information.
- **Example:** Vito notices that a witness's account does not fit the route the crew took, but he may
  still draw the wrong conclusion from that observation.

#### Charismatic

- **Meaning:** expects personal influence to be available and sees social approaches as valuable.
- **Primary channels:** salience and evaluation of persuasion, recruitment, reassurance, and
  relationship-building; willingness to spend time building rapport.
- **Does not mean:** guaranteed compliance, high Persuasion capability, or universal likability.
- **Example:** Vito tries to calm a frightened associate through personal contact when another man
  would threaten or abandon him.

#### Cold-Blooded

- **Meaning:** personally causing harm carries less emotional or moral cost.
- **Primary channels:** perceived cost of violence; reaction to injury or betrayal; willingness to
  consider irreversible solutions when they are otherwise available.
- **Does not mean:** Aggressive, fearless, cruel in every context, or capable of carrying out violence.
- **Example:** Vito may consider sacrificing a compromised asset without the hesitation another
  character would feel, while still rejecting the act if it creates unacceptable exposure.

### Unscreened ideation pool

The following words were proposed during brainstorming and are preserved so they are not lost. They
are **not** additions to the trait dictionary: several may be drives, capabilities, relationship
states, reputations, temporary pressures, cultural attitudes, opposites of another trait, or synonyms
that should be merged.

- Cruel, Generous, Greedy, Ambitious, Pragmatic, Impulsive;
- Arrogant, Modest, Confident, Independent, Loyal, Lazy, Respectful;
- Adaptable, Forgiving, Honest, Humble, Quiet, Careless, Stubborn, Anxious;
- Untrustworthy, Friendly, Sociable, Open-Minded, Prejudiced;
- Intelligent, Vigilant, Assertive, Outspoken, Insecure, Resentful, Egotistical, Gullible.

A word moves from this pool into the proposed dictionary only after it names a distinct behavioral
channel, states what it does not mean, avoids duplicating an existing system, and can be falsified in
a bounded scenario. `Loyal`, `Ambitious`, `Intelligent`, `Resentful`, and `Untrustworthy` require
particular care because current canon already models related meaning through relationships, Status,
capabilities, grievances, beliefs, and reputation-producing history.

## Example profile: Vito

| Trait | Profile role | Profile weight | Behavioral reading |
|---|---|---:|---|
| Collected | Major | 30% | Keeps deliberate options alive under pressure |
| Perceptive | Major | 30% | Notices relevant signals and inconsistencies |
| Charismatic | Minor | 20% | Values personal influence and social approaches |
| Cold-Blooded | Minor | 20% | Discounts the personal cost of harming someone |

If Vito's heist goes south, these traits do not select a fixed “stand off with the cops” action.
They change what remains salient and how it is evaluated. His relationship with the player may make
the player's plan credible; his knowledge may or may not include a viable escape route; his
capabilities determine what he can execute; and police pressure determines what choices are actually
available. The result can be that he holds steady, negotiates, retreats, or surrenders for reasons a
player can reconstruct from the state of the world.

A different character in the same situation may flee despite loyalty, surrender despite composure,
or escalate despite poor odds. The personality profile explains tendencies; it does not predetermine
the scene.

## Interaction and tuning model

Conceptually, a decision-relevant personality channel can be represented as a weighted combination
of the four trait contributions:

```text
channel tendency =
    0.30 × major trait 1 effect
  + 0.30 × major trait 2 effect
  + 0.20 × minor trait 1 effect
  + 0.20 × minor trait 2 effect
```

This is a design model, not an implementation prescription. Each trait must provide channel-specific
effects rather than one universal personality number. A trait can be relevant to salience but not
execution, or to immediate reaction but not long-term utility. Knowledge, relationships, pressure,
capability, and opportunity must gate the resulting candidates and outcomes.

The first executable tuning should compare characters facing the same situation, not attempt to tune
the entire vocabulary at once. A difference is valuable only if it is observable, causally
explainable, and not a disguised hard lock.

## Legibility requirements

The player should ordinarily encounter qualitative expressions rather than raw trait percentages:

- “He keeps his head when the room turns dangerous.”
- “He notices what other people miss, though he is not always right.”
- “He would rather talk someone around than force the issue.”
- “He is willing to make the hard cut when others hesitate.”

Trait names and exact weights may appear in a deliberate character-profile view, but ordinary play
should communicate personality through choices, reactions, accounts, and consequences. A character
should be interpretable without exposing an omniscient decision log.

## Validation scenarios

Before broad cast expansion, the model should be tested in a few bounded situations:

1. **Police standoff after a failed heist:** compare Collected, Cautious, Aggressive, and
   Cold-Blooded profiles while holding relationships and capabilities constant.
2. **A contradictory account:** compare Perceptive and Suspicious characters while preserving the
   same evidence and source limitations; neither may gain information it did not receive.
3. **A frightened subordinate:** compare Charismatic and Proud characters deciding whether to reassure,
   threaten, or abandon him.
4. **A promotion opportunity:** compare Status-driven characters with different traits and
   relationships; ambition must still require an available route and tolerable risk.
5. **Actor-parity replay:** run the same situation through controlled and autonomous paths and verify
   that personality affects both through the same causal rules.

Each scenario should record not only the final action, but the candidate set, information available,
the relevant relationship state, and the explanation visible to the player.

## Open questions before implementation

1. Should the existing four traits and the proposed four extensions form one final closed vocabulary,
   or should some be merged after scenario testing?
2. Should a trait have a separate intensity within its 30% or 20% slot, or is slot weight enough for
   the first prototype?
3. Are two opposing traits allowed freely, and how should the player-facing profile explain that
   tension?
4. Which traits are stable personality and which can change after trauma, prison, promotion, or a
   major relationship rupture?
5. Should organizational culture add a contextual modifier without rewriting a character's personal
   traits?
6. Which traits must affect immediate reaction, and which should affect only deliberate evaluation?
7. What is the smallest named cast that makes these differences visible in play?

## Implementation boundary

This proposal does not authorize code changes. A future implementation milestone should begin only
after the vocabulary, channel contracts, and first validation scenarios are accepted. It should start
with a small cast and one dramatic situation, then measure whether the personality differences produce
distinct, legible outcomes without violating information boundaries or actor parity.

## Related documents

- `docs/GAME_VISION.md` — emergent narrative, shared agency, imperfect information, and playable
  failure.
- `docs/SIMULATION_ARCHITECTURE.md` — traits, salience, bounded deliberation, and causal parity.
- `docs/DESIGN_DECISIONS.md` — current closed trait/drive vocabulary and settled constraints.
- `docs/INFORMATION_AND_LEGIBILITY.md` — truth, knowledge, belief, evidence, and player-facing
  explanation.
- `docs/OPEN_CONCERNS.md` — unresolved tuning and character-schema risks.
