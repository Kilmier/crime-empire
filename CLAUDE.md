# Criminal Empire — Agent Instructions

Before doing anything, read `AGENTS.md`, then these files in this order:
1. `docs/PROJECT_CONTEXT.md` — project history, current phase, working style, immediate next step
2. `docs/GAME_VISION.md` — game vision and design pillars
3. `docs/SIMULATION_ARCHITECTURE.md` — simulation/AI architecture
4. `docs/INFORMATION_AND_LEGIBILITY.md` — canonical knowledge/belief/rumor/evidence model and
   player-facing legibility contract (referenced as canon by `SIMULATION_ARCHITECTURE.md`)
5. `docs/DESIGN_DECISIONS.md` — settled decisions, with citations to which doc/section settled them.
   Treat as authoritative; don't re-derive a decision from the docs above when it's already
   recorded here.
6. `docs/OPEN_CONCERNS.md` — known open risks and flaws not yet resolved. If you're about to make a
   call that touches one of these, say so explicitly rather than silently picking an answer.

`design-doc-concerns_1.md` is retired — superseded by `docs/DESIGN_DECISIONS.md` (resolved items,
with citations) and `docs/OPEN_CONCERNS.md` (still-open items). Don't treat it as current.

## Review workflow

This project uses a two-agent loop: Claude (implementation) and Codex (code review /
architectural-integrity review). The cycle is:

```
Claude implements
      ↓
Claude tests and commits
      ↓
Codex reviews that commit
      ↓
Owner (Matt) accepts/rejects findings
      ↓
Claude fixes accepted findings
      ↓
Codex verifies
      ↓
Next milestone
```

Implications for Claude when implementing:
- Every commit should be reviewable in isolation — keep commits scoped to one coherent change,
  with tests passing, so Codex's review has a clean unit to evaluate.
- Codex is reviewing against `docs/DESIGN_DECISIONS.md` and the canon docs above, not general
  best-practice opinion. If a Codex finding conflicts with a settled decision, that's a signal to
  surface to Matt, not to silently accept or silently dismiss.
- Don't wait for Codex's pass to self-review — run tests and check the change against the ground
  rules below before committing, so Codex's review is catching real issues, not basics.

## Project type
Solo hobby project. Not commercial, no deadline pressure, no team — optimize for the owner's
iteration speed and understanding over process/documentation overhead.

## Current phase
Building a headless C# console prototype of the character decision pipeline described in
`docs/SIMULATION_ARCHITECTURE.md`. No Godot/engine work, no rendering, no save/load yet.
See `docs/PROJECT_CONTEXT.md`'s "Immediate next step" section for the exact task and success criterion.

## Ground rules
- Don't re-litigate settled decisions (stack, succession model, tiering, pillars) without a
  concrete reason surfaced during actual implementation — they're recorded in `docs/PROJECT_CONTEXT.md`.
- Don't reintroduce anti-patterns explicitly rejected in `docs/SIMULATION_ARCHITECTURE.md`: traits firing
  actions directly, unrestricted GOAP/planning, full deliberation depth for every character
  regardless of narrative relevance.
- Scope discipline: this project has a repeated failure mode of maximalist scope. When in doubt,
  propose the smaller, faster-to-test version first.
- Be direct about problems you notice in the design or the code rather than deferring everything
  back as a question — but flag disagreements with existing decisions explicitly rather than
  silently working around them.
- Use Plan mode / propose an approach before writing code for anything nontrivial. This project is
  architecturally opinionated on purpose — check the plan against the docs above before executing.
