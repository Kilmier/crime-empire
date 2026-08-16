# Criminal Empire — Relationships

## Status and purpose

**This is the prototype relationship schema, not an irrevocable long-term list.** It records what the
behavioural kernel actually implements and what actually reads it, as measured at milestone 008. It is
the relationship counterpart to `INFORMATION_AND_LEGIBILITY.md`, which `OPEN_CONCERNS.md` #3 asked for
— with one deliberate difference in kind. That document was written ahead of implementation and
described an intended design. This one is written behind implementation and describes a working one.

The reason for that difference is the whole history of #3. It has always been conditioned on "once
the kernel shows which distinctions actually change decisions," and milestone 007 sharpened why: *a
relationship schema that says how dimensions are stored and updated without saying which decisions
read them will not predict behaviour.* Every dimension below therefore names its readers, and any
dimension that could not name one is not here.

Expect this to change. What should not change without a recorded decision is the **rule** that admits
a dimension: it must name a behavioural purpose and a decision that reads it. That rule closed the
trait vocabulary in milestone 001 and removed `Affection` in milestone 006.

## The vocabulary

Four things, and they are all there is: **Trust**, **Fear**, **Obligation**, and relationship-keyed
**Grievances**. Directional in every case — `A → B` is a separate object from `B → A` and they move
independently.

`Domain/Relations.cs` is the only code that can create or change any of it. This is enforced by the
concrete type being a `private sealed class` nested inside `Relations`, so outside that class the
type cannot be named, constructed, or cast to; everyone else holds a read-only `IRelationship`.
**Reading never creates** — an absent relationship reads as zero on every dimension and stores
nothing.

---

### Trust

**Purpose.** How far this character takes the other's word and backing. It is the dimension a
perceived account conflict moves, and it is the project's main experiment in whether a social
consequence can reach a decision.

**Range.** `[0, 1]`. Zero means *no trust*, which is currently indistinguishable from *distrust* —
see Deferred below.

**Update paths.**

| Path | When | Effect |
|---|---|---|
| `Relations.Establish` | scenario construction only | sets the starting value |
| `Relations.RecordAccountConflict` | somebody asserts the opposite of a position the listener holds | `−ConflictTrustCost × strength`, floored at 0 |

`ConflictTrustCost = 0.35` is **provisional tuning**, unchanged since milestone 006 and deliberately
not tuned since. `strength` is the listener's prior confidence times the asserted confidence — both
things he can perceive.

There is no path that *raises* trust at runtime. That is a real gap and is named as one.

**Decision readers.** Only through `Utility.Loyalty`, at weight `0.45`:

| Candidate | Loyalty coefficient | Sensitivity to one unit of trust |
|---|---|---|
| `Retaliate` — risk term | `−2.2 × loyalty` | 0.99 |
| `ReportToSuperior` — Candid | `+0.7` standing, `+0.8` candour = `1.5` | 0.675 |
| `DelegateStrategy` | `+0.9` | 0.405 |
| `ReportToSuperior` — False | `+0.7` standing, `−1.4` candour = `−0.7` | −0.315 |
| **`ReportToSuperior` — Partial** | **`+0.7` standing, `−0.5` candour = `0.2`** | **0.09** |
| policy-breach reluctance | within `−(0.6 + 1.2 × loyalty) × …` | varies with policy strength and pride |

**The last row is the finding milestone 008 exists to record.** On a partial report the two
considerations very nearly cancel, so the full range of Trust is worth `0.09` to that candidate —
less than the spread between two candidates' `±0.05` noise draws. Trust is read an order of magnitude
harder by retaliation and delegation, but a perceived account conflict lands on a character at moments
when the live candidates are reports. The mechanism that moves trust and the decisions that read it
hardest are not adjacent in time.

---

### Fear

**Purpose.** What somebody's capacity for violence does to this character's willingness to hold out.

**Range.** `[0, 1]`.

**Update paths.**

| Path | When | Effect |
|---|---|---|
| `Relations.Establish` | scenario construction only | sets the starting value |
| `Relations.Frighten` | coercion resolution in `Strategies.AdvanceTribute` | `+0.35` on a threat, `+0.55` on force |

**Decision readers.** `Concede` at `+1.6 × Fear`; `Refuse` at `−1.6 × Fear`.

**Fear is the strongest relationship reader in the model.** The largest relationship contribution
measured anywhere in the five variants is `1.44` — Marco conceding to Tommy after a beating, on a
decision whose whole margin is `0.25`. It is worth stating plainly against the Trust row above: the
relationship channel is not weak. One path through it is.

---

### Obligation

**Purpose.** A sense of owing — patronage, favours, formal subordination. Distinct from trust because
a man can owe somebody he does not believe, and believe somebody he owes nothing.

**Range.** `[0, 1]`.

**Update paths.** `Relations.Establish` only. **There is no runtime path that moves obligation.** It
is seeded by the scenario and never changes for the rest of a run. Recorded rather than glossed: it
earns its place by being read, not by being dynamic, and a dimension that only ever holds its seeded
value is a weaker part of the schema than the two above.

**Decision readers.** `SeekApproval` at `+0.4 × Obligation`; `Utility.Loyalty` at weight `0.30`.

---

### Grievances

**Purpose.** What this character holds against that one, itemised and dated rather than summed into a
mood. Kept as a list so a grievance can name its cause and its moment, which is what lets a later
player-facing account say *why* somebody moved.

**Shape.** `Grievance(AgainstId, Description, Severity, At)`, held on the relationship — `AgainstId`
was always a relationship key wearing a different name. `GrievanceWeight` is the sum of severities and
is **not bounded above**; nothing currently caps it.

**Update paths.**

| Path | When | Severity |
|---|---|---|
| `Relations.RaiseGrievance` (`Sim/Runner.cs`) | observing that somebody went outside your instruction | 0.45 |
| `Relations.RaiseGrievance` (`Decision/Commit.cs`) | somebody moved against you | 0.50 |
| `Relations.RaiseGrievance` | scenario construction | as seeded |
| `Relations.ClearGrievancesAgainst` | scenario construction only | clears |

**Decision readers.** `Retaliate` at `+0.8 × weight`, directly. And at **every** loyalty reader, as
its own named component at `−0.50 × weight × that reader's coefficient`.

**Grievance is not inside the loyalty clamp, and this is the substantive change milestone 008 made.**
It used to be subtracted inside `Math.Clamp(…, 0, 1)`, which meant that once a character's grievance
exceeded his bond the sum floored at zero — further grievance was free and further trust was
worthless. A bitter subordinate and an indifferent one scored identically. It now travels separately
and is applied by each reader at that reader's own coefficient, so the arithmetic is unchanged
wherever the old sum did not clamp and honest where it did.

The `0.50` coefficient is **provisional tuning** and was preserved exactly; milestone 008 tuned
nothing.

---

## Loyalty is derived, and is read as parts

There is no stored loyalty scalar and there should not be. "Loyal" collapses attachment, obligation, a
general need to belong, and accumulated grievance — which behave differently and should be able to
disagree.

```
Loyalty.Value = clamp(0.45·Trust + 0.30·Obligation + 0.25·Belonging, 0, 1)
Loyalty.GrievanceOffset = −0.50 · GrievanceWeight        (applied separately, never clamped away)
Loyalty.BareValue = clamp(0.25·Belonging, 0, 1)          (the same man with no relationships)
```

`Belonging` is a **drive**, not a relationship dimension: a man with no relationships still has a need
to belong. It is enumerated alongside the others so the four inputs stay separately inspectable
through the scoring path, and it is excluded from the relationship counterfactual.

## How a contribution is measured

Every score component records the facet it was **derived from**, set where the value is computed, and
**carries exactly one facet**. Each of loyalty's four contributions — trust, obligation, Belonging,
grievance — is emitted as its own component at each reader's own coefficient, so moving one dimension
moves one component and no other. Separately computed and then summed is not separately inspectable;
the first implementation fused three of them under a union flag and Codex found it.

The bond is an unclamped sum of its three parts. The weights total exactly `1.0` and every input is in
`[0,1]`, so the clamp it used to carry could never bind — and a clamp that binds cannot be split,
because there is no honest way to apportion a clamped total among its parts.

This exists because the obvious alternative was measured and found wrong. The only previous way to ask
"how much of this score came from a relationship" was to filter components named `relationship
effects`; across the five variants at seed 42 that name covered 168 components of which **61 — 36% —
read no relationship state at all**. `SeekCorroboration`'s "going behind X" is `−0.45 × proud`: a pure
trait term wearing a relationship label. **A label is not a derivation.**

`ScoreBreakdown` exposes, developer-facing only:

- `RelationshipComponents()` — every component that read relationship state, **with no cutoff**;
- `RelationshipGross()` / `RelationshipNet()` — before and after cancellation;
- `TotalWithoutRelationships()` — the same score, same noise draw, for a man with no relationships.

Gross is reported alongside net because only the net was ever visible, and a large pair that nearly
annihilates is not the same thing as a character who barely weighed the relationship. The cutoff is
excluded because on the decision milestone 007's central finding was measured on, *both* halves of the
report pair fall under it — so the reason list printed no relationship line at all for the candidate
whose relationship contribution was the number being reported. A cutoff that hides a cancelling pair
hides exactly the cancellation.

None of this reaches `IntelligenceWriter` or any player-facing surface.

## What is deliberately absent

- **`Affection`** — removed in milestone 006 for having no stated behavioural purpose. It had been
  declared since the first commit and was never read or written by anything. Nothing was invented to
  preserve it.
- **A stored `Loyalty`** — derived, for the reason above.
- **Respect, resentment, attraction** — no decision reads them, so they are not here. They may be
  right later; they are not admissible on the strength of sounding like things people have.

## What is deferred, and on what evidence it returns

Deferred is not retired. Each of these returns when the stated condition is met.

- **Negative trust.** Trust is floored at `0`, so absence of trust and distrust are the same state and
  a stranger who contradicts you is indistinguishable from a stranger. **Returns when a decision
  exists that would read distrust differently from indifference** — not before, because until then
  widening the range changes no behaviour and only adds a number to tune.
- **Decay.** No dimension decays. **Returns when the calendar and relevance tiers supply a timescale**
  to decay against; a rate chosen inside a 90-day fixture with no ageing would be a guess dressed as
  a schema.
- **A runtime path that raises trust.** Only conflicts move it, and only downward. A relationship that
  can be damaged and never repaired is a modelling choice nobody has made deliberately.
- **A cap on `GrievanceWeight`.** Unbounded today. Considered as milestone 008's remedy for grievance
  dominating loyalty and explicitly rejected in favour of unbundling the clamp, so the question of a
  cap is open rather than answered.

## Where the numbers live

Coefficients are in `Decision/Utility.cs`; the dimensions and their update paths in
`Domain/Relations.cs`. Provisional tuning is labelled as such at its definition:
`Relations.ConflictTrustCost` (0.35) and `LoyaltyReading.GrievanceWeight` (0.50).

Measured behaviour, and the counterfactual figures behind the claims above, are in
`milestones/008-relationship-readers.md` and `REVIEW_LEDGER.md`'s verification baselines.
