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

---

## Correction from Codex's review of `a0c6be8`, 2026-08-19

Appended, not folded in. The account above — including its "119 live-edge findings" and "8
apparently-dead lines" figures — is preserved as originally written and is **superseded by this
section**. Corrected figures and the full corrected triage are in `docs/COVERAGE_ACCOUNTING.md`,
which was updated in place (it is a living reference, not an append-only archive); this section
records what Codex found and why each correction was made.

**Codex reviewed `a0c6be8` and returned two P1 and two P2 findings, all documentation/accounting
defects — no simulation code was touched by the implementation, so none of this concerns behaviour.**

**P1 — 20 of 315 uncovered lines were reasoned about but never actually written into the accounting's
tables**, so the document's own stated totals (188/8/119) did not match what its tables and prose
actually enumerated (which summed to 295, not 315). Missing: `Decision/Utility.cs:67`,
`Trace/TraceWriter.cs:79-83,105-107,177`, `Sim/Runner.cs:118`,
`Decision/PerceivedSituation.cs:50-54,148`, `Strategy/Strategies.cs:121-123`. Re-verified against a
fresh coverage run from a clean tree (deterministic — identical to the original measurement) and
against a line-by-line reconciliation of every file's raw uncovered-line list against
`COVERAGE_ACCOUNTING.md`'s tables, confirming these five regions were the only gaps. Each was read
against source, classified, and added: `Utility.cs:67` (legitimately uncovered, an unread record
getter); `TraceWriter.cs`'s three regions and `PerceivedSituation.cs`'s two (live edges — see the
corrected file for the specific gating conditions each never crosses); `Runner.cs:118` and
`Strategies.cs:121-123` are addressed under the P1 below and the "important discoveries" note,
respectively.

**P1 — "Exclusions: none" overstated what the Cobertura report covers.** The report's `<package>`
elements are `CrimeEmpire.Runner` and `CrimeEmpire.Simulation` only, confirmed directly against the
regenerated XML. `CrimeEmpire.Godot` is never loaded by `dotnet test` — it has no project reference
from the test assembly, so it was never a candidate for instrumentation — and the test assembly
itself is not instrumented either. Neither is a configured exclusion; the original phrasing implied a
completeness the run does not have. Corrected to state plainly what is and is not in scope.

**P2 — `Strategy/Strategies.cs:149-151` was misclassified as apparently dead.** The original reasoning
— "no code removes a business, so the guard is unreachable" — is a claim about current call sites, not
about what the type system permits. `World.Businesses` is `public Dictionary<string, Business> {
get; }`: the getter has no setter, but the dictionary it returns is fully mutable through its own
public API, so any code holding a `World` reference can call `world.Businesses.Remove(...)` directly.
Nothing enforces the "never removed" invariant the original classification relied on. Reclassified as
a live edge. Applying the same corrected standard on re-review surfaced one more instance of the same
error the reviewer's own "where to look" section had flagged as the highest-risk category:
`Decision/Utility.cs:736` (`SelfProtection`'s default arm, reachable if a `Candid`-labelled candidate
ever carried a non-empty `Suppressed` list — every current generator call site happens not to, but
nothing in the type declares that impossible) was reclassified the same way, unprompted by Codex, for
consistency.

**P2 — `Session/PlayerNarration.cs:47` needed reconciling against the rule used for `Utility.cs`'s
switch defaults.** `Describe`'s switch explicitly handles all ten members of `ClaimKind` (verified
against the enum in `Domain/Claim.cs`), so its default arm cannot be reached by any valid value —
the same shape as `Utility.cs`'s three `CoercionMethod` defaults, and unlike `Decision/Filters.cs`'s
own `Describe` helper, whose switch handles only seven of the ten members and stays a live edge.
Reclassified from legitimately-uncovered to apparently-dead.

**Applying the sharper apparently-dead standard to `Sim/Runner.cs:118` turned up a third
reclassification, found during this correction rather than named by Codex.** The line was originally
missing from the accounting entirely (the first P1 above); writing it in required deciding which
bucket it belonged to, and the original "defensive completeness" instinct is exactly what Codex's
review had just shown to be unreliable without checking. Verified instead: both `ObservationOpportunity`
and `AssignmentDelivered` have exactly one scheduling site each in `src/`, and both are explicitly
null-guarded before scheduling (`Strategies.cs:437`, `Runner.cs:190`) — so `Handle`'s fallback
`return null;` is provably unreachable given the current codebase, not merely undertested.
Classified as apparently dead.

**Corrected totals: 186 legitimately uncovered, 6 apparently dead, 123 live edges — verified to sum to
315 and to match the raw per-file uncovered-line list from a regenerated coverage run exactly, file by
file.** Full corrected accounting: `docs/COVERAGE_ACCOUNTING.md`.

**What this correction is not.** It is documentation and accounting only — no simulation code changed,
no test was added or removed, and the verification figures from the original account (build, test
count, hashes, viewpoint renders, Godot self-test) are unaffected and were not re-measured, since
nothing that produces them changed.

**Recurring-failure list, walked.** *False-assurance claim:* the "Exclusions: none" line and the
188/8/119 totals were both claims the document made about itself that a direct check (rereading the
Cobertura XML's package list; re-summing the tables against the raw uncovered-line list) disproved —
exactly the shape of defect this project's own review checklist exists to catch, and exactly why a
second reader found it. *Collapsing distinct states:* "unreachable" and "unexercised" were briefly
collapsed for `Strategies.cs:149-151`, `Utility.cs:736`, and (in the other direction, undercounted
before this correction) `PlayerNarration.cs:47` — all three now argued from a stated, checkable rule
rather than an unverified instinct about what "probably" never happens. *Recording a review that did
not happen:* this correction is Matt's report of Codex's review, acted on and re-verified, not a claim
to have observed the review itself.

**Status.** This corrective commit is implemented and self-checked (the reconciliation and the two
mutability checks above were re-run against the corrected file line by line). It is **not accepted** —
Matt's confirmation of this named commit is what that requires. `docs/CURRENT_MILESTONE.md` and
`docs/REVIEW_LEDGER.md`'s "Measured — milestone 013" section are updated to the corrected figures.
