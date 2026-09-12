# Current Milestone

Mutable and intentionally short. This is the sole active handoff surface; completed history belongs
in `docs/milestones/`, review coverage in `REVIEW_LEDGER.md`, and future candidates in `ROADMAP.md`.

## Status

**No gameplay milestone is active. Milestone 024 is closed and accepted at `762210f`.** Claude
independently reviewed Codex-authored corrections `344f1e0` and `762210f`; Matt accepted the PASS and
one P2 documentation finding. The documentation-only closeout is `c7dd34b`. Full history:
`docs/milestones/024-the-operation-reads.md`.

Milestones 021 and 026 are also closed at `9fed181` and `abcffd5`. Milestone 020's independently
confirmed accepted state is `8e6878e`. These are historical anchors, not active scope.

## Authorized work — bounded review-process cleanup

Matt authorized this documentation-only cleanup on 2026-09-12, before introducing Astra into the
workflow.

In scope:

- define proportional Class A/B/C review in `AGENTS.md`;
- compact this file to current truth and authorization;
- compact `REVIEW_LEDGER.md` without losing established outcomes, reviewer identity, explicit
  unknowns, grouped rows, the current baseline, or recurring lessons;
- expose the real chronological review queue instead of copying stale milestone summaries.

Out of scope:

- no simulation, tests, fixtures, UI, persistence, canon, proposals, milestone archives, or roadmap
  changes;
- no reclassification of a proposal as canon;
- no milestone 027 planning or implementation;
- no claim that this cleanup reviewed itself.

## Completion gate

1. Codex prepares and validates the uncommitted documentation diff.
2. Claude independently compares it with the pre-cleanup ledger at `c7dd34b`, checking that every
   established outcome and explicit uncertainty survives.
3. Only after that comparison passes may the cleanup be committed.
4. The cleanup commit receives its own Class B review when its chronological turn arrives; its
   outcome is recorded in the next independently authorized change, never by a commit created only
   to record itself.

## What follows

After the cleanup is accepted, resume the ledger's oldest active-range unresolved commit using the
new proportional class. Do not infer the queue from milestone numbers or from this file.

Milestone 027 — **The session has an ending** — remains a roadmap candidate and is not authorized.
Carried forward for later scope: a reader of an impression has no resulting action yet; operation
pacing remains parked at Matt's direction; finished-operation history belongs to the proposed 027.
