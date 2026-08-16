# Criminal Empire — Open Concerns

Genuinely unresolved items only. Ranked roughly by severity. This file supersedes
`design-doc-concerns_1.md` — six of its ten original items turned out to already be resolved by
later revisions to the vision/architecture docs and are recorded (with citations) in
`DESIGN_DECISIONS.md` instead of repeated here. Don't re-add them without new evidence they've
regressed.

## Load-bearing

### 1. Tuning is 100% deferred, and tuning is where this design lives or dies
The architecture doc is a correct, well-reasoned map of structure — trigger types, tiering,
decision pipeline, anti-patterns to avoid — with almost no committed numbers (weights,
candidate-set sizes, promotion thresholds, trigger sparseness). That's the right call for a doc
at this stage, but it means neither doc is evidence the resulting behavior will feel believable.
Good architecture + bad weights still produces characters that feel insane or inert.
**Implication:** resolved by process, not content — the next artifact should be executable (the
pre-MVP kernel), not another written spec. Stays open until the kernel produces trace output that
can be judged.

### 2. Tiered continuous-calendar engineering cost is a bigger commitment than it reads as
The discrete-event-under-continuous-calendar model is the right technical answer, but combined
with 3-tier promotion/demotion, this is genuinely one of the harder parts of the project —
not because any single piece is exotic, but because bugs in event scheduling across tiers will be
non-deterministic and hard to reproduce. Budget real time for this specifically, separate from
"AI behavior" time. Not addressed by any doc revision; it's an engineering-effort risk, not a
design gap, so a document can't resolve it — only implementation experience can.

## Moderate

### 3. Character schema — `relationships` branch is still a shape, not a schema
`knowledge` got a real subsystem treatment via `INFORMATION_AND_LEGIBILITY.md`. `relationships`
did not receive the same treatment and is still just a bullet list (directional, trust/respect/
fear/affection/resentment/attraction/obligation as separate dimensions) with no data-model
answer for how those dimensions are stored, updated, or decayed. Should get the same kind of
dedicated document `INFORMATION_AND_LEGIBILITY.md` gave to knowledge, once the kernel shows which
distinctions actually change decisions.

**Still open. Updated 2026-08-14 with milestone 006's evidence** — which is the "once the kernel
shows which distinctions actually change decisions" this item has always been conditioned on.
Storage and update now have an answer for one dimension; decay, the remaining dimensions, and the
document itself do not.

What the kernel has now shown:

- **Trust moves, and the movement reaches a decision.** A perceived account conflict costs the
  listener trust in the speaker. Because `Utility.Loyalty` derives from trust and retaliation risk is
  priced at `-(1.3 + 2.2 * loyalty)`, contradicting a man measurably lowers what it costs him to move
  against you. That connection was not written; it fell out of one edge feeding a derived value.
- **`Affection` was purposeless and is gone.** Declared from the first commit, never read or written
  anywhere. A dimension list should be closed the way the trait vocabulary was closed.
- **Absence of trust and distrust are the same state**, because the range is `[0,1]`. A stranger who
  contradicts you is indistinguishable from a stranger. Whether that distinction earns its cost is a
  real schema question and the clearest one this milestone surfaced.
- **Grievances belong on the relationship**, not beside it: `AgainstId` was always a relationship key.
- **The centralized API is worth having before the schema is settled.** One place that can change
  relationship state made the read-creates hazard visible and made the conflict edge a three-line
  change at each of three call sites rather than a convention.

**Updated again 2026-08-15 with milestone 007's evidence, and still not retired.** 006 showed trust
moves; 007 shows how far that movement carries, which is the more useful number and the less
flattering one.

- **The movement reaches a score, and is roughly a fortieth of a decision margin.** A capo
  contradicted twice by his boss goes from 0.45 trust to 0.031, and the `relationship effects`
  component on his next report to that boss moves from 0.0440 to 0.0063. Decision-relevant, and
  nowhere near choice-changing. `Utility.Loyalty` weights trust at 0.45 and subtracts half of any
  grievance, so a standing grievance of 0.35 absorbs most of a full trust collapse before it reaches
  a score at all. Whether that is the right shape is a schema question, not a tuning one.
- **Which relationship a mechanism lands on decides whether it matters at all.** 006's conflict fell
  on a character with no relationship-reading decisions and changed nothing; the identical mechanism
  landing on one who reports upward is measurable. A schema that says how dimensions are stored and
  updated without saying which decisions read them will not predict this.

What is still unanswered, and is what a document would have to settle: decay and its rate; whether
respect, resentment and attraction are separate dimensions or derived; whether provenance should
weight the social consequence (milestone 006 deliberately used one rule and preserved the provenance
so this can be decided on evidence); negative trust; and whether loyalty's weights leave trust enough
room to matter once a grievance exists.

**Narrowed 2026-08-16 by milestone 008, and not retired.** The document this item asked for exists as
`docs/RELATIONSHIPS.md`, written from measured results rather than ahead of them. What it settles:

- **The vocabulary is closed** — Trust, Fear, Obligation, relationship-keyed Grievances — with every
  dimension required to name a decision that reads it, asserted by a test.
- **Storage, update paths and readers are documented per dimension**, which is the half milestone 007
  said a schema would be useless without.
- **"Whether loyalty's weights leave trust enough room once a grievance exists" is answered, and the
  answer was the clamp rather than the weights.** Grievance was subtracted inside
  `clamp(…, 0, 1)`, so a character whose grievance exceeded his bond floored at zero and further
  trust was worth nothing. It now sits outside the clamp as its own named contribution, at the same
  `0.50`. No coefficient was tuned.
- **The premise behind the question was too narrow.** Removing relationship state changes which
  candidate wins at 1–3 decisions in every variant. The channel is not weak; the trust-to-partial-report
  path is, because two loyalty reads on that candidate nearly annihilate. Fear reaches 1.44 on a
  decision whose margin is 0.25.

What remains open here, each now with a stated condition for return rather than an open question:
**decay and its rate** (needs a calendar/tier timescale); **negative trust** (needs a decision that
reads distrust differently from indifference); **whether respect and resentment are separate
dimensions or derived** (needs a reader); **whether provenance should weight the social consequence**
(unchanged since 006); and **whether `GrievanceWeight` should be capped** — considered as milestone
008's remedy and explicitly rejected in favour of unbundling, so it is open rather than answered.

### 4. ~~Trait/value vocabulary must be closed, but the concrete list still isn't committed~~
**Retired 2026-08-14.** Milestone 001 closed the list in `Domain/Psychology.cs` and it is now
recorded in `DESIGN_DECISIONS.md` under "Actor parity and simulation tractability" — traits
Aggressive/Cautious/Proud/Suspicious, drives Wealth/Status/Security/Belonging, with Loyalty derived
per relationship and Ambition folded into Status. The number is kept rather than reused: the
milestone archives are append-only and cite this item as #4.

### 5. Conflict by omission is not recognized as conflict
`IsContested` requires an actual denial. In the baseline both Vincent and Tommy omit rather than
deny, so their accounts differ without formally conflicting. Whether one source's assertion against
another source's conspicuous omission deserves first-class treatment is a real design question, not
an oversight — a boss who notices that two men's stories cover different ground is doing something
the model currently cannot represent. Surfaced by `milestones/003-information-transmission.md`.
