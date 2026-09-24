# Narrative Interpretation and Dramatic Presentation

**Status: noncanonical design proposal.** This document preserves a design discussion for later
roadmap evaluation. It does not authorize implementation, establish new simulation state, canonize
the Bellini example, or place narrative work ahead of the active milestone. Any durable ruling must
be reconciled into the canonical source named by `AGENTS.md`; any implementation scope must first be
authorized in `CURRENT_MILESTONE.md`.

## Purpose

Crime Empire's simulation can produce correct decisions, information boundaries, relationships, and
consequences while still reading as an event log. The proposed narrative layer exists to make those
simulation-produced events legible, dramatic, atmospheric, and characterful without becoming a
second simulation.

The working boundary is:

> **The simulation authors reality; the narrative renderer authors expression.**

```text
SIMULATION
Produces an action, utterance, observation, reaction, or consequence
        ↓
NARRATIVE RENDERING
Chooses authorized voice, phrasing, tone, emphasis, pacing, and atmosphere
        ↓
PLAYER PRESENTATION
Shows only what the controlled viewpoint can legitimately receive or observe
```

This direction is compatible with the canonical distinction between causal output, legibility
output, and source-limited player explanation. The exact narrative architecture remains unapproved.

## Proposed guardrails

### The renderer does not simulate

The renderer may not independently create:

- an action or speech act;
- a claim, question, threat, promise, confession, order, or admission;
- a witness, recipient, observation, or information transfer;
- a belief, memory, relationship change, or consequence;
- visible fear, hesitation, confidence, or other behavior that the simulation did not make
  observable;
- knowledge of another character's private state or authoritative world truth.

Dialogue is often causal gameplay, not cosmetic prose. If a line communicates a claim, threatens a
character, grants an extension, changes an assignment, or can be repeated to somebody else, its
meaning and recipients must exist in simulation state before it is rendered.

The renderer may choose among expressions that preserve the same structured meaning. For example,
an authorized claim that Bellini cannot pay may be rendered as "I need more time" or "I haven't got
it this week." It may not be upgraded to "Glanton took the money" unless that is also part of the
speaker's authorized claim.

### Rendering contracts

A **rendering contract** is the bounded set of facts and freedoms supplied to one presentation. It
specifies:

1. what the simulation established;
2. who acted, spoke, observed, and received the event;
3. what the current viewpoint may know or perceive;
4. which meaning the output must preserve;
5. which facts, implications, and state changes the renderer must not invent; and
6. which expressive dimensions may vary, such as voice, wording, emphasis, pacing, and atmosphere.

The first implementation should prefer small, typed, situation-specific inputs over one global
"dramatic context packet" containing every actor's intentions, secrets, beliefs, emotions, and
memories. A report, witnessed confrontation, private conversation, rumor, and newspaper account have
different sources and information permissions.

### Viewpoint and uncertainty

Multiple interpretations should emerge through actual sources: what a participant says, what a
witness observed, what a newspaper reports, or what an adviser infers. The renderer must not combine
every participant's private understanding into an omniscient dramatic account.

A conflicting report creates suspicion or a contested claim, not automatic knowledge of the truth.
The player may act on uncertain information and confront someone with it without the interface
certifying that it is correct.

### Traits, relationships, and voice

Traits, drives, relationships, pressures, resources, beliefs, and history may affect which actions a
character evaluates and selects. Traits do not directly fire actions. The renderer expresses the
selected behavior; it does not choose behavior from a trait label.

Voice is a presentation concern and must not become a second personality system. A voice profile may
shape vocabulary, rhythm, restraint, or formality while preserving the same underlying speech act.
Relationship tone should use legitimately observable behavior and established relationship history,
not expose hidden values or introduce speculative dimensions merely to support prose.

### Significance and presentation intensity

Not every simulation event needs a full scene. Important events may receive a scene or focused
character moment; routine but relevant events may become reports, chronicle entries, news, rumors, or
compact updates.

Any future significance mechanism is a presentation priority, not a second decision system. It must
not reveal hidden importance to the player or infer private motives. Early implementation should
prefer explicit event categories and authored rules over a universal numerical drama score.

### Deterministic presentation first

Authored templates or constrained grammar should be the first implementation direction, not merely a
fallback. They preserve reproducibility, testing, offline play, debugging, consistent meaning, and
information-boundary checks. Live generative prose is deferred unless it later demonstrates that it
can preserve those requirements.

## Worked scenario: Bellini's collection

This example is a design probe, not authorized content or a scripted quest.

The player is told to collect money from Bellini. The player has learned that Bellini seems timid but
has not learned another disposition that makes deception more attractive to him. Bellini's response
is not fixed. Based on his resources, traits, pressures, beliefs, relationships, witnesses, prior
history, and assessment of the collector, eligible responses might include:

- pay in full or in part;
- honestly request more time;
- fabricate hardship;
- refuse;
- offer information or a favor instead;
- invoke genuine protection from the Glanton gang;
- bluff that Glanton protects him;
- flee, prepare resistance, or seek protection after the encounter.

A timid manner does not prove that Bellini is truthful. A deceitful disposition does not force him to
lie. Circumstances determine which actions are possible, and the character evaluates those actions
through the shared decision model.

### Example first encounter

The simulation might establish:

```text
Action: request an extension
Claim: Bellini cannot pay this week
Claimed reason: paying would deprive his family
Observable behavior: Bellini appears frightened
Speaker: Bellini
Recipient: the collector
Location: Bellini's shop
```

The renderer could express that event as Bellini cowering behind the counter and pleading for another
week, but only because the simulation made the fearful behavior observable. Granting the extension
and saying "one more week, or you're a dead man" are consequential player actions: one changes the
commitment and the other is a threat Bellini may remember or repeat.

### Conflicting information

The player may later ask Tommy what he knows. If Tommy replies that Bellini is wealthy, the player has
a new sourced claim that may conflict with Bellini's account. Tommy may be correct, mistaken,
exaggerating, repeating rumor, or describing wealth that Bellini cannot immediately access.

The player may then confront Bellini with Tommy's claim, investigate further, or act under
uncertainty. The game should not silently convert the conflict into "Bellini lied" unless the
controlled character legitimately establishes that conclusion.

### Protection as agreement, belief, claim, and enforcement

"Glanton protects me" can refer to several distinct things:

1. **Actual arrangement:** Bellini and a Glanton decision-maker established reciprocal or coerced
   commitments such as tribute and expected protection.
2. **Believed arrangement:** Bellini believes Glanton will intervene, whether or not Glanton shares
   that interpretation.
3. **Claimed arrangement:** Bellini asserts protection to the collector; the statement may be true,
   false, exaggerated, outdated, or technically misleading.
4. **Enforced arrangement:** Glanton learns what happened and independently decides whether to
   retaliate, negotiate, demand compensation, abandon Bellini, or do nothing.

Protection should therefore not collapse into a universal `ProtectedBy` modifier. Bellini and
Glanton need opportunities to offer and accept an arrangement, understand its terms, communicate
about breaches, and decide whether enforcement is worthwhile. Their interpretations may differ.

The player may investigate through real actions: ask Glanton or Tommy, watch the shop, follow money,
question neighboring businesses, demand proof, or collect anyway and observe what follows. Each path
produces observations and sourced claims rather than automatic truth.

### Violence and rule breaking

Physical violence remains an available collection method. It may obtain full payment, partial
payment, no money, resistance, or flight. It may also create injury, witnesses, traces, reports,
police attention, and relationship consequences.

If violence violates a harbor rule, the boss does not become angry through omniscient notification.
The boss must observe, receive a report, or infer the breach and then independently evaluate it. If
Bellini is genuinely protected, Glanton likewise must learn of the incident and decide whether and
how to respond. Concealment, misinformation, failed reporting, opportunism, or reluctance may prevent
either consequence.

This permits outcomes such as:

- Bellini's bluff succeeds and encourages future bluffs;
- investigation exposes the bluff;
- Bellini invents protection and then seeks a real agreement before the claim is checked;
- protection is real but Glanton refuses to intervene;
- Glanton demands compensation rather than immediate retaliation;
- violence begins an avoidable conflict and angers the player's boss after the breach is reported;
- Bellini pays two organizations until the burden becomes unsustainable; or
- Bellini plays competing organizations against one another.

No single script owns this story. It emerges from overlapping decisions and incomplete information.

## Variation and determinism

Bellini may respond differently across playthroughs because the seed, world state, history, resources,
relationships, beliefs, witnesses, and player approach differ. Variation must remain circumstance-led,
not an equal-probability dialogue roulette.

The reproducibility rule remains:

```text
same build + same seed + same initial state + same action history = same result
```

Randomness may separate close evaluations, but it should not erase strong circumstances. Changing a
fact Bellini cannot know must not change his decision.

## Proposed automated validation

Most combination coverage should be automated through the headless deterministic simulation rather
than manually playtested.

Candidate methods include:

- controlled scenario matrices varying cash, protection, fear, disposition, witnesses, history, and
  player approach;
- deterministic seed sweeps that test directional effects rather than prematurely fixing exact
  tuning percentages;
- exact invariants for eligibility, information receipt, persistence, and causal ordering;
- counterfactual pairs that alter one known input while holding the rest constant;
- negative controls proving hidden facts cannot affect decisions;
- renderer checks proving paraphrases preserve structured meaning and forbidden facts remain absent;
- save/load and same-seed reproduction.

Human playtests remain necessary for questions automation cannot answer: whether Bellini feels like a
person, whether uncertainty is understandable without becoming obvious, whether the scene is tense,
whether investigation options are discoverable, and whether repeated encounters remain coherent.
Automated coverage should identify a small set of representative seeds for those human sessions.

## Proposed design test

The Bellini scenario suggests a reusable test for future Crime Empire features:

- every actor decides independently from their own information and circumstances;
- agreements are commitments between actors rather than global status effects;
- statements may be true, false, mistaken, exaggerated, or outdated;
- investigation offers evidence and accounts without automatic certainty;
- violence remains possible and produces causal, persistent risk;
- rules matter through the people who know and enforce them;
- consequences travel through observation, reporting, rumor, and inference; and
- narrative presentation dramatizes those events without inventing them.

If the interaction collapses into a scripted quest branch, a trait-triggered response, a universal
protection modifier, or an omniscient heat penalty, it has failed this proposed test even if the
feature technically operates.

## Relationship to future roadmap work

This proposal records a direction for evaluation after the active milestone closes. It does not place
work on the roadmap now. A future shareable-demo plan may use the Bellini situation as a target
scenario spread across bounded milestones rather than authorize one large narrative-system build.
Likely dependencies include a rival organization, protection commitments, varied collection
responses, investigation, rule violations, information propagation, and constrained narrative
rendering.

The first useful narrative slice should prove one existing simulation interaction end to end: a
structured event, source-limited receipt, deterministic dramatic rendering, a meaningful player
response through the shared action pipeline, and a persistent consequence. It should not begin with
an unrestricted prose generator.

## Related documents

- `INFORMATION_AND_LEGIBILITY.md` — canonical information states and source-limited explanation.
- `SIMULATION_ARCHITECTURE.md` — canonical causal output, legibility output, and decision model.
- `GAME_VISION.md` — canonical pillars, problems-not-quests, and emergent storytelling.
- `proposals/UI_AND_PLAYER_LEGIBILITY.md` — proposed screens and player-facing information surfaces.
- `proposals/PERSONALITY_AND_CHARACTER_PROFILES.md` — noncanonical personality expansion.
- `proposals/ORGANIZATIONS_ALLIANCES_AND_CULTURE.md` — noncanonical rival, alliance, and protection-adjacent directions.
