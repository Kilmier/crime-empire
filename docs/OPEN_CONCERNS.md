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
