# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**Nothing is active.** Milestone 011 — The Detective Has No Next Move — was implemented on
2026-08-18 and is **awaiting Matt's review**. Confirm scope with Matt before starting anything; do
not begin milestone 012.

Milestones 001–010 are complete and accepted. **Matt accepted milestone 010 on 2026-08-18 on the
strength of `824f3fc`**, and `REVIEW_LEDGER.md`'s coverage checkpoint stands there. The milestone 011
commits are later than the checkpoint, have no row, and need reviewing in their turn.

**Codex remains withdrawn from the review loop.** Claude implements and reviews its own work — see
`REVIEW_LEDGER.md` §"From milestone 010 onward, review is self-assessment" for the method, and for
why a self-review is recorded as weaker evidence than what it replaced.

## What milestone 011 concluded

Full account in `docs/milestones/011-the-detective-has-no-next-move.md`.

**The detective had no move after naming a suspect** — a candidate set of exactly one option, with
nothing generated and nothing rejected, because every route to a question was structurally closed to
her. She now puts her case to the man it names, through the existing channel and nothing resembling
an arrest. So does Salvatore, to the capo whose policy breach he inferred, which was not anticipated.

The investigation path is now scoped to its incident rather than its address, its dead cold-trail
branch works, and police interest names the incident it is about. A character is described as
themselves on every player-facing surface, which had said "he" about the only woman in the cast since
milestone 003.

**A denial to a detective is cheaper than a denial to a delegator, and still loses** — margin 2.29
against 2.93. The loyalty terms fall away as predicted and are worth about a fifth of the gap. That
makes four known reasons the denial stays shut, and loyalty is the smallest of them.

**One settled decision was corrected**, with the reason surfaced through implementation rather than
inspection: `Cognition.Revise` admitted `Inference` alone, which put `Discovery` in with Participant
and Witness — the bundle `Provenance.cs` exists to prevent. `Provenance.IsOwnReading` is now the named
rule. `DESIGN_DECISIONS.md` records it.

## What is open, for whoever picks up next

Not a queue and not authorization. Candidate scopes live in `ROADMAP.md`; nothing there or here is a
licence to begin. The full carried list is at the end of milestone 011's archive. The items most
likely to matter next:

- **The developer trace still says "he" for everybody** — 59 strings, deliberately outside item 5.
- **`AdvanceInvestigation` reads and writes `owner` throughout**, so a delegated investigation would
  put its findings in the head of a man who was not there. Nothing exercises it.
- **The cold-trail branch is unreachable in every variant at seed 42**, so a staged test is the only
  thing standing behind it.
- **Nobody holds a scored relationship with Kane**, so the attitude list can never describe a woman
  in a natural run.
- ROADMAP candidates 3 (persistence/SQLite) and 6 (a runtime path that raises trust) are untouched,
  as is what remains of 5 — rival activity and tier transitions.

## Ordered review process

Unchanged. Matt takes commits in order, oldest first; each review names the exact commit whose diff
was inspected; the coverage table in `REVIEW_LEDGER.md` is the record. **Never write "verified" or
"accepted" from a review report alone** — including one of Claude's own. Matt's confirmation of a
named commit is the only thing that counts, and that rule matters more now that the reviewer and the
author are the same.
