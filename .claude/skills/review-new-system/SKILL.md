---
name: review-new-system
description: Review a proposed Crime Empire milestone or substantial simulation/gameplay system before implementation. Use whenever Matt or Claude is choosing scope, validating a mechanic against canon, defining actor-neutral behavior, checking information boundaries, or writing falsifiable success criteria. Do not use for reviewing completed commits.
---

# Review a New Crime Empire System

Produce a decision-ready scope review without editing the repository.

## 1. Establish authority and target

1. Read `AGENTS.md` first and follow its canonical read order and conflict rules.
2. Read `docs/ROADMAP.md` only because this task selects or proposes future scope; remember that it
   grants no permission.
3. Name the exact proposal being reviewed and whether `docs/CURRENT_MILESTONE.md` authorizes it.
4. Separate:
   - settled behavior from `DESIGN_DECISIONS.md` and the full canon;
   - unresolved risks from `OPEN_CONCERNS.md`;
   - current scope from `CURRENT_MILESTONE.md`;
   - assumptions introduced by the proposal.

Do not silently turn an open concern into a design decision or infer a new milestone from roadmap
order.

## 2. State feature intent as falsifiable behavior

Describe the smallest player/simulation situation the system must make possible:

- the meaningful decision or problem it creates;
- what the deciding actor can know at that moment;
- the observable success, partial success, failure, and persistent consequences;
- the natural scenario that should exercise it;
- any staged boundary proof needed in addition to the natural scenario;
- the honest non-result that is acceptable without coefficient or fixture tuning.

A feature whose state exists but cannot affect an eligible action, trace, player view, or later world
state has not yet demonstrated its intent.

## 3. Trace only the relevant causal surface

Follow the proposed path, using the applicable stages rather than inventorying every project system:

```text
occasion/trigger
  -> actor eligibility and available information
  -> candidate generation and scoring
  -> selection and commitment
  -> scheduled execution
  -> observation/report/evidence
  -> persistent consequence
  -> player-facing projection
```

For each relevant stage, name the authoritative state, writer, readers, and actor identity. Ask:

- Can the player and a qualified NPC reach the same underlying causal operation where canon intends
  parity? Different knowledge, access, authority, willingness, and simulation depth may yield
  different candidates without creating a second rules engine.
- Does any step consult world truth, an entire organization, rank adjacency, or a global collection
  where character-specific knowledge or evidence is required?
- Does delegation communicate only the order/report/result actually transmitted, or does it teleport
  beliefs, justifications, or complete organizational knowledge?
- Can organizational rules be violated with consequences, or has authority accidentally become
  capability?
- Does a hidden action create appropriate traces even when nobody immediately observes it?

## 4. Test scope and complexity

Classify each proposed element:

- `IN SCOPE`: required to falsify the milestone's core claim;
- `DEFER`: valuable but independently testable later;
- `REJECT`: duplicate, contrary to canon, or not needed for the claim;
- `HUMAN RULING`: a genuine design choice not settled by canon.

Do not recommend “cheap future-proofing” without naming the concrete rewrite it avoids. Do not add a
generic interface, manager, registry, planner, LOD layer, persistence layer, alternate career, or
state hierarchy merely because one might eventually exist. Prefer data and seams already justified
by the current behavior.

## 5. Define proof obligations

Require tests that can fail for the intended reason:

- natural-scenario evidence for the player-visible/emergent claim;
- focused staged tests for boundary cases;
- negative controls for omniscience, hive-mind access, wrong actor, and wrong recipient;
- actor-path comparison when player/NPC parity is intended;
- deterministic ordering and pause/fast-forward equivalence if time or scheduling changes;
- replay/snapshot coverage for state that can affect later decisions;
- mutation checks for load-bearing distinctions, including the tempting simplified implementation.

Tests that reproduce the implementation's helper logic, assert only type shape, or force the desired
winner do not prove intent.

## Report format

### Recommendation
`AUTHORIZE` / `AUTHORIZE AFTER REVISION` / `DO NOT AUTHORIZE`

### Canon and authorization
- Target proposal
- Active authorization
- Settled constraints
- Open concerns touched

### Executable feature claim
- Situation and decision
- Actor and information contract
- Persistent/observable consequence
- Natural proof, staged proof, and acceptable non-result

### Scope decisions
| Item | IN SCOPE / DEFER / REJECT / HUMAN RULING | Canon/evidence | Reason |
|---|---|---|---|

### Required tests and falsifiers
| Claim | Test | Mutation or negative control | What failure means |
|---|---|---|---|

### Findings
Include only proposal-specific findings. For each state:
`Priority` (`P1`/`P2`/`NOTE`), canon citation, evidence, consequence, smallest revision, and
falsifying test. `P1` is a material contradiction that must be resolved before authorization. `P2`
is a bounded defect or ambiguity requiring correction or an explicit human ruling. `NOTE` is an
optional observation, not a finding, and cannot block authorization.

### Human rulings required before implementation
List only choices the canonical record cannot answer. If none, say `None`.
