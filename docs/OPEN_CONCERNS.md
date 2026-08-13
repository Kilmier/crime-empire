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

### 4. Trait/value vocabulary must be closed, but the concrete list still isn't committed
The architecture doc now *mandates* a closed, small, enumerable, data-driven trait list — that
policy question is settled. The actual list is not: earlier drafts used Loyalty/Greed/Fear/
Ambition as a 4-stat starting point, but the current docs still use open descriptive language
("traits, skills, ambitions, resources, memories, secrets"). Needs to be closed before
implementation goes past the pre-MVP kernel, or it'll be unbalanceable and unexplainable via
player-facing opinion breakdowns.
