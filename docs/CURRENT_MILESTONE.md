# Current Milestone

Mutable active handoff. Detailed implementation/correction history belongs in the milestone archive;
review coverage belongs in `REVIEW_LEDGER.md`.

## Status

**Milestone 030 — One informed choice — is implemented, archived and awaiting independent
exact-commit Class A review.** The focused implementation commit is the commit that adds
`docs/milestones/030-one-informed-choice.md`; locate its exact hash with:

```powershell
git log --diff-filter=A --format=%H -- docs/milestones/030-one-informed-choice.md
```

Matt's 2026-09-20 human-comprehension gate passed after the bounded presentation correction. That is
owner playtest evidence, not independent code review or final acceptance of the implementation
commit.

Milestone 029 remains accepted at exact `aa9f30cb439dfab9def8c7a68a4704869da18320`; its
documentation-only owner-closeout descendant is `d908d8e268231591fe5ec61890cee42356f60051`.

## Review gate

The reviewer must read `AGENTS.md`, `docs/REVIEW_LEDGER.md`, the canonical project documents and
`docs/milestones/030-one-informed-choice.md`, then review the exact implementation commit. Assign
Class A from the diff; check feature intent, actor-neutral behavior, recipient privacy, deterministic
persistent state, the player information boundary, the bounded presentation correction, test
assurance and scope. Record findings, limits and verdict in the milestone archive and the compact
coverage result in `REVIEW_LEDGER.md` according to repository process.

No subsequent milestone is authorized. Do not begin new runtime or design work while this review
gate is open.

## Deliberately carried work

- `OPEN_CONCERNS.md` records the newly observed operation-duration mismatch and the unresolved
  start-with-delegate / silent-busy-subordinate flow.
- Direct initial delegation, unavailable-option explanation, additional campaign content and all
  other post-Milestone-030 work require separate authorization.
- The primary checkout's existing `ROADMAP.md` and proposal edits are unrelated user work and must
  remain untouched and uncommitted by this milestone.
