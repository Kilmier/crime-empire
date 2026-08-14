# Criminal Empire — Agent Instructions (Claude Code)

Read `AGENTS.md` first. It applies to every coding agent in this repo (Claude Code and Codex) and
holds the canonical doc read order, the Claude/Codex review workflow, and the milestone lifecycle
protocol. This file only adds what's specific to Claude Code — don't duplicate `AGENTS.md` here;
if it needs updating, update `AGENTS.md` and let this file keep pointing to it.

`design-doc-concerns_1.md` is retired — superseded by `docs/DESIGN_DECISIONS.md` (resolved items,
with citations) and `docs/OPEN_CONCERNS.md` (still-open items). Don't treat it as current.

## Project type
Solo hobby project. Not commercial, no deadline pressure, no team — optimize for the owner's
iteration speed and understanding over process/documentation overhead.

## Current phase
`docs/CURRENT_MILESTONE.md` is the only place that says what is active — read it rather than
inferring a phase from this file. Completed milestones are in `docs/milestones/`, what is not yet
built in `docs/ROADMAP.md`, review coverage in `docs/REVIEW_LEDGER.md`.

The sequencing that governs any of it is settled, not a status: headless console sim core first, no
Godot project yet — `docs/DESIGN_DECISIONS.md`, "Stack".

## Claude-Code-specific practice
- Use Plan mode / propose an approach before writing code for anything nontrivial. This project is
  architecturally opinionated on purpose — check the plan against `AGENTS.md`'s canon doc list
  before executing.
- Don't re-litigate settled decisions (stack, succession model, tiering, pillars) without a
  concrete reason surfaced during actual implementation — they're recorded in
  `docs/DESIGN_DECISIONS.md`.
- Be direct about problems you notice in the design or the code rather than deferring everything
  back as a question — but flag disagreements with existing decisions explicitly rather than
  silently working around them (see `AGENTS.md`'s review workflow for how that reaches Matt).
