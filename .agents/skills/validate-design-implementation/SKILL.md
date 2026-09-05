---
name: validate-design-implementation
description: Review one named committed Crime Empire implementation against its authorized milestone and canonical design. Use after Codex has tested and committed a feature or correction, before more work builds on it. Checks exact-diff fidelity, actor-neutral causality, information boundaries, deterministic persistent state, scope, and falsifying tests; never edits or records acceptance.
---

# Validate Crime Empire Design Against Implementation

Review one commit as a read-only adversarial pass. The goal is to find where code and tests agree
with each other but implement the wrong claim.

## 1. Fix the review target and coverage

1. Read `AGENTS.md` and follow its canonical read order.
2. Read `docs/REVIEW_LEDGER.md` before forming findings. Record the checkpoint and classify existing
   issues rather than presenting them as new.
3. Name the exact target commit. Require a clean worktree and inspect only `commit^..commit`.
4. Read the applicable `docs/milestones/NNN-*.md`, including every appended correction. Use
   `docs/CURRENT_MILESTONE.md` only for current status/scope; it does not rewrite archive history.
5. If the target is not `HEAD`, never check out, reset, or alter the main worktree. Verify at the
   exact commit from an isolated temporary export/worktree when safely available. Otherwise inspect
   the exact diff and label any tests run only at later `HEAD` as a limitation.
6. Do not infer that a commit was reviewed from a message, test result, archive adjective, or later
   correction. The ledger and Matt's acceptance are the record.

Stop and report a review limitation if the target is ambiguous, dirty, unavailable, or cannot be
isolated well enough to support the requested verdict.

## 2. Extract the contract before judging code

Build a compact requirement matrix from the authorized milestone and controlling canon:

| Requirement | Canon/spec citation | Expected observable proof | Status |
|---|---|---|---|

Statuses are `IMPLEMENTED`, `PARTIAL`, `MISSING`, `CONTRADICTED`, `NOT TESTED`, or `OUT OF SCOPE`.
Do not derive requirements from the implementation or its commit message.

## 3. Walk the causal path end to end

Inspect the stages the feature actually touches:

```text
occasion -> eligibility -> perceived inputs -> candidate -> score/selection
         -> commitment/scheduling -> execution -> observation/transmission
         -> persistent consequence -> player projection
```

At each stage identify the acting character and the authoritative state read or written. Check for:

- a player-only command or UI-owned mutation beside an NPC-only generator/executor when the feature
  intended one actor-neutral operation;
- NPC action unsupported by that NPC's beliefs, information, access, capability, resources,
  authority, or willingness;
- truth, omniscient rosters, office/rank scans, or global state substituted for what the actor knows;
- delegation or reporting that transfers unstated knowledge, evidence, confidence, or motivation;
- organization-wide cognition or coordinated action without character communication and authority;
- authority enforced as physical capability rather than a rule that can be violated;
- hidden actions leaving no persistent trace, or ambient attention implemented as a global heat bar;
- a consequence written to the strategy owner, boss, organization, or player when the rule names a
  different actor or relationship;
- a value whose distinction is created at one stage and discarded before the next reader.

## 4. Check deterministic and durable state

When the diff touches time, scheduling, commitments, random decisions, or future-decision state,
check:

- stable causal RNG/occasion identity rather than global event counters;
- deterministic event ordering and same-seed replay;
- pause/resume and fast-forward equivalence;
- stale-event rejection, replacement cancellation, and commitment stability rather than constant
  replanning;
- no read-time creation or unordered enumeration that changes outcomes;
- snapshots/replay comparators include state that can affect later decisions.

Serialization/save tests are required only when authorized scope includes persistence storage. The
absence of a save system is not itself a finding; losing or omitting future-decision-relevant state
inside the current replay model is.

## 5. Test feature intent, not just implementation shape

Separate evidence into:

- **Natural scenario:** the accepted run reaches the claimed behavior without tuning or a test-only
  shortcut.
- **Staged proof:** focused construction demonstrates a boundary or invariant.
- **Mutation/negative control:** the tempting wrong implementation makes the named test fail.
- **Honest non-result:** expected behavior that did not occur and is recorded without being forced.

Look for false assurance: copied scoring logic in tests, fixtures that cannot reach the asserted
branch, tests that assert a collection after a later action already populated it, a mock path unlike
production, snapshot fields nobody compares, or “deterministic” checks that never vary pacing.

For a new distinction, ask both “where else is this value read?” and “does the distinction survive
the whole trip?” For feature intent, mutate the implementation toward the plausible simplification
the review is meant to prevent.

## 6. Verify the exact commit

Run the commands required by `AGENTS.md`, plus focused tests relevant to the diff. For information or
relationship changes, run its extended variant, compare, and viewpoint commands. Add targeted
pause/fast-forward, seed, or Godot checks only when the changed surface requires them.

Report command, exit status, test counts, hashes/digests, and whether each result came from the exact
commit or only a later `HEAD`. Baseline movement is neither automatically success nor failure:
attribute each material movement to an authorized cause; unexplained movement is a finding.

## 7. Avoid duplicate findings

Classify every issue:

- `NEW`: introduced or first exposed by this commit;
- `REGRESSION`: a previously accepted invariant was broken;
- `KNOWN-OPEN`: already in `OPEN_CONCERNS.md`, `ROADMAP.md`, an archive correction, or the ledger and
  not worsened here;
- `ALREADY-RECORDED`: the same reviewed finding is already recorded for this commit.

Report `KNOWN-OPEN` only when the commit touches, worsens, relies on, or falsely claims to resolve it.
Do not pad the report with unrelated backlog.

## Report format

### Target and verdict
- Commit and subject
- Worktree/target isolation
- Ledger checkpoint
- Verdict: `PASS` / `PASS AFTER FIXES` / `FAIL`
- Safe to build upon: `YES` / `NO`

### Findings
List findings in priority order. Each finding must contain:

```text
[PRIORITY][STATUS] Short causal title
Canon: document §section and requirement
Evidence: exact file:line plus the observed path/test result
Consequence: player-visible or simulation-level failure
Smallest correction: bounded change, not a redesign
Required proof: focused test and mutation/negative control
```

Priority is `P1`, `P2`, or `NOTE`, matching `docs/REVIEW_LEDGER.md`:

- `P1`: a material correctness, canon, information-boundary, determinism, or review-integrity
  defect. It normally requires `FAIL` and `Safe to build upon: NO` until fixed or explicitly waived
  by Matt.
- `P2`: a bounded correctness, test, or documentation defect that still requires explicit
  disposition. If the report says the commit is safe to build upon despite a P2, explain why the
  defect cannot contaminate later work.
- `NOTE`: a genuinely useful optional observation. It is not a finding and cannot change the
  verdict or review status.

If there are no findings, say so explicitly. Do not add praise or generic best-practice sections.

### Requirement matrix
Include only requirements necessary to explain the verdict.

### Verification
| Command/check | Exact commit or later HEAD | Result |
|---|---|---|

### Review limits and known-open items touched
State limitations and relevant pre-existing risks without presenting them as new defects.
