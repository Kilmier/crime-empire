# Milestone 013 — Coverage Accounting, Not Vigilance

Authorized by Matt on 2026-08-19 after Codex reviewed correction commit `3c86ba4` with no findings,
narrowed from an earlier, broader "instruments, not vigilance" proposal that also included systematic
mutation automation and seed-sweep promotion. Both were deliberately deferred as unnumbered
`docs/ROADMAP.md` candidates before implementation began — this milestone is coverage accounting only.

## What this milestone is for

`coverlet.collector` has been a package reference in the test project since before milestone 009 and
had never been asked for a report. Across milestones 009-012, every defect Codex found was in a place
mechanical inspection reached and re-reading did not — including `AdvanceInvestigation`'s cold-trail
branch, found by accident ten milestones after it was written, unreachable in every variant at every
seed tried. The smallest new mechanical check available was the coverage report already sitting
unread. This milestone makes it repeatable, triages what it finds, and adds no simulation behaviour so
its own work can be separated cleanly from anything it discovers.

## What was attempted

Per the plan in the version of `docs/CURRENT_MILESTONE.md` this file replaces:

1. Establish the coverage baseline and its exclusions.
2. Triage every uncovered line into three buckets — legitimately uncovered, apparently dead, or a live
   edge nothing has ever run.
3. Record bucket 2 and bucket 3 findings without repairing them.
4. Make the measurement repeatable and self-check the instrument against a known omission.
5. Verify nothing moved.
6. Write the ledger entries.
7. Archive, reset, one coherent commit, stop.

## What was completed

**Measurement.** `dotnet test CrimeEmpire.sln --collect:"XPlat Code Coverage"` from a clean tree at
`1046704` (docs-only since the accepted baseline `3c86ba4` — confirmed via
`git diff --stat 3c86ba4 HEAD -- src tests`, empty). **92.10% line, 84.30% branch, 3676/3991 lines
covered, 315 uncovered** — matches the figure measured at planning time exactly, no test added or
removed since. No exclusions: every line is accounted for on its own terms rather than filtered out.

**Triage.** All 315 uncovered lines sorted into **188 legitimately uncovered, 8 apparently dead, 119
live edges**. Full accounting, per-region reasoning, and file/line references:
`docs/COVERAGE_ACCOUNTING.md`. Outside `Program.cs`'s CLI entry point (118 of the 188 legitimate), the
remaining 197 lines split 70/8/119 — a majority of everything worth looking at in this report is real,
reachable behaviour nothing has ever run, not dead weight. Filters.cs, flagged at planning time as
"disproportionate... worth its own look," turned out to be four of `Filters.Apply`'s five rejection
stages that have simply never fired: no generator in the accepted fixture currently proposes a
candidate that fails knowledge, skill, or crew, so the checks guarding against that have never had
anything to catch. Several findings correlate across files — the same unexercised gap producing a
zero-hit line in a generator, the filter that would reject it, and the trace line that would describe
the rejection, independently and simultaneously.

**Nothing found was fixed, tested, or removed** — per the milestone's "Out" scope, findings are
recorded in `docs/COVERAGE_ACCOUNTING.md` for later, separately-scoped correction.

**Repeatability and self-check (ruling 7).** The measurement is a single documented `dotnet test`
command plus reading two attributes off the resulting Cobertura XML — no new source, no committed
tooling, nothing to unit-test in its own right per ruling 4, since there is no custom instrument code
promoted into the repository. The self-check itself: the two live edges named at planning time
(`Sim/Runner.cs:311-315`, `Decision/Utility.cs:563-570`) both appeared in the measured report
unprompted, confirming the instrument's negative signal before the rest of the report was trusted on
it.

**Verification (ruling 6).** Full re-run from a clean tree: build 0 warnings/0 errors across four
projects; 458/458 tests; `--verify` deterministic and byte-identical on `baseline`
(`FEE45FD886F18CA8`), `disloyal-vincent` (`45CCF5ADC6EC0302`), `resentful-tommy`
(`F5BD93386DE04082`); `--compare` byte-identical across all five trace hashes and chosen-action
digests; **all 30 viewpoint renders** (the 6-character cast × 5 variants) produced and inspected,
none exceptioned — the first time this project has run the full 30 rather than the two AGENTS.md
names as required; Godot headless self-test: 4 choices, 4 decision screens, exit 0, transcript
byte-identical to the recorded baseline. Nothing moved, as expected of a milestone that adds no
simulation behaviour.

**Ledger entries (`docs/REVIEW_LEDGER.md`).** Three stale "not yet accepted"/"not yet reviewed"
section headers corrected to reflect Matt's 2026-08-19 acceptance of `3c86ba4` (milestones 011 and 012
are accepted as corrected — this was previously true and unrecorded, a gate-staleness failure of
exactly the kind this file's own "How this record has failed" section warns against). A permanent
record that milestones 011 and 012 rest on a weaker basis than "accepted" alone conveys — self-reviewed
first, corrected only after an adversary arrived four milestones late — and what this milestone's
instruments do and do not make up for that (an unexercised region, not a wrong computation in an
exercised one). The coverage baseline itself, under "Verification baselines". A new "where to look"
checklist item under "Documentation and process". `docs/ROADMAP.md`'s `AdvanceInvestigation` debt item
struck, matching `3c86ba4`. `docs/milestones/012-...md`'s stale "paused pending" close note corrected
by append, not rewrite.

## Important discoveries

- **A live edge is not rare.** 119 of 315 uncovered lines — over a third of everything, and the
  large majority of everything outside one CLI entry point — are reachable, meaningful behaviour
  nothing has ever exercised. The two named at planning time were not an unusually bad pair found by
  luck; they were representative of what a first coverage pass on an unmeasured project actually
  looks like.
- **Correlated findings are more informative than isolated ones.** Several live edges surfaced
  together across a generator, a filter, and the trace line describing the rejection — the same
  missing exercise, visible from three independent angles at once, which is stronger evidence than any
  one of the three alone.
- **A defensive guard commented as unreachable is a different finding from a switch default that is
  provably unreachable.** `Relations.cs`'s `Writable()` guard is documented, fail-closed, and kept;
  `Utility.cs`'s three `CoercionMethod` switch defaults and `Salience.cs`'s discount-switch default are
  unreachable given an exhaustively-handled closed enum or a guard directly above them — genuinely
  dead, not merely undertested. Collapsing that distinction would have put live safety nets and dead
  weight in the same bucket.
- **Some "live edges" are lower-confidence than others, and the accounting says so.** `PlayerOption.cs`'s
  20 lines were read against the source and classified as reachable phrasing branches, but not
  individually traced to a specific decision in a specific variant the way `Filters.cs` and the two
  named edges were — recorded at that confidence level rather than overstated.

## Deferred work

Everything carried into milestone 012, unresolved by this milestone and not itemized again here — see
`docs/milestones/012-a-shortfall-he-cannot-attribute.md`'s own carried-forward list, plus the item it
struck (`AdvanceInvestigation`, now fixed by `3c86ba4`). This milestone adds to that list rather than
resolving any of it:

- **119 live-edge findings**, listed by region in `docs/COVERAGE_ACCOUNTING.md`, each a candidate for a
  future, separately-scoped test or correction. Not triaged by priority — that is future scoping work,
  not this milestone's.
- **8 apparently-dead lines**, candidates for removal, also not acted on here.
- **Systematic mutation automation** and **seed-sweep promotion** — deferred as unnumbered
  `docs/ROADMAP.md` candidates, deliberately kept out of this milestone's scope.
- **Whether coverage can be collected over a natural run rather than the test suite** — raised as an
  open question at planning time, not settled here. `coverlet.collector` is a test collector; nothing
  in this milestone's scope required resolving whether a console-run collector exists.

## Where to look and what to distrust

The claim in this commit most expensive if wrong is the triage in `docs/COVERAGE_ACCOUNTING.md` — 315
one-line judgment calls, most argued from reading the source once rather than from a test proving the
classification. Two categories carry the most risk: the **apparently-dead** bucket (8 lines) claims a
line is *unreachable*, a stronger claim than "unexercised," and getting it wrong risks a future reader
deleting code that could still fire; and the **live-edge** entries collectively re-verified against the
5-variant/90-day fixture and the 30 viewpoint renders carry more confidence than the handful marked
explicitly as pattern-classified or lower-confidence in the accounting (`PlayerOption.cs`'s 20 lines,
`PlayerNarration.cs:47`, `PlayerSnapshot.cs:35`). The verification numbers (hashes, test count, the
92.10%/315 figures) are mechanically reproduced and low-risk if wrong — reviewing those is a matter of
re-running the documented commands, not judgment. The bucket assignments are where re-reading, not
re-running, matters.

## Commit

One implementation-and-archive commit, docs-only. Status is not established by this file —
`docs/CURRENT_MILESTONE.md` says what is active, and Matt's confirmation of a named commit is the only
thing that counts as acceptance.
