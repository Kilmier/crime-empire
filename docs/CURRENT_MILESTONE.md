# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**Milestone 013 — Instruments, Not Vigilance — remains planned and NOT authorized to begin.** Paused
on 2026-08-19 when Codex's review of milestones 009–012 arrived (reported by Matt) with corrective
scope; that correction takes priority and is implemented, tested, and self-reviewed in a commit
appended to milestones 011 and 012's archives. **013's scope and rulings below are unchanged by the
correction and are not to be revised yet** — Matt's instruction is explicit that 013 is re-baselined
onto the accepted correction commit only after that commit is accepted, not before. Do not begin 013.

**The correction.** Three findings, addressed in one commit: `AdvanceInvestigation` read and wrote
the strategy owner's cognition throughout rather than the executor's — harmless while the accepted
scenario never delegates an investigation, wrong in general, corrected in milestone 011's archive
since that is where the affected state was added; a reconsideration-stamp test that never checked the
reconsideration stamp; a stale docstring on `StrategyInstance.SourceEventId`. All three mutation-
checked, and full verification re-run confirms milestone 012's own recorded baselines are unmoved —
see the appended corrections in `milestones/011-the-detective-has-no-next-move.md` and
`milestones/012-a-shortfall-he-cannot-attribute.md` for the complete account.

**Milestone 012 is complete, corrected, self-reviewed, and NOT accepted.** **Milestone 011 is
self-reviewed and cleared to build on, corrected, and NOT accepted.** Both corrections await Matt's
acceptance and a further Codex round. `REVIEW_LEDGER.md`'s coverage checkpoint stands at `824f3fc`.

Milestones 001–010 are complete and accepted.

**Codex is intermittent rather than withdrawn, and this correction is the case in point.** The plan
recovered from `520924b` for milestone 013 opened "Codex is gone and nothing replaces it"; that
premise was already corrected at `c7ae3d6`, and this pause is what the corrected premise predicted —
an adversary that arrives long after the work rather than not at all, so a self-review declared clean
can sit unchallenged across several milestones before anything contradicts it. It arrived here across
four.

## What this milestone is for

**What Codex supplied was not diligence. It was different priors.** `REVIEW_LEDGER.md` is specific:
across milestone 009 it returned nine findings on work declared verified each time, and every one was
a place the author had convinced himself. Re-reading found nothing; what broke it open was mechanical.

That pattern has held for three milestones since. Milestone 010's self-review found three defects,
011's found three, and 012's mutation pass confirmed five fixes and caught a false instrument — and
**every one of those came from a mechanical check rather than from looking again**.

So the only adversaries available are the ones with no priors at all, and this milestone builds them.
It adds **no simulation behaviour**, which is the point: there is nothing new to review, and the
instruments are what the next behavioural milestone will be reviewed with.

### What is already there and has never been run

`coverlet.collector` has been a package reference in the test project since before milestone 009 and
produces a report the first time it is asked for. **Measured at `HEAD` on 2026-08-19: 92.10% line,
84.30% branch, 315 uncovered lines.** The previously recorded figure — 92.2% / 84.2% / 376, taken at
`6a8a765` — is superseded: milestone 012's fourteen tests closed 61 uncovered lines as a side effect
of testing something else, which is itself a small argument for the report existing.

Sampling separates three different things:

- **Legitimately uncovered.** `Program.cs` alone is **118 of the 315** — the CLI entry point, exercised
  by the verification commands rather than by unit tests. It is the largest single number in the
  report and the least interesting, which is exactly how a naive coverage milestone would waste
  itself. It is now 37.5% of the remainder, up from 31%, because the interesting part shrank.
- **Live edges nothing has ever run. Both re-verified uncovered at `HEAD`, not assumed:**
  - **`Runner.cs` 311–315** raises a grievance and resentment when a character observes somebody
    else's policy breach — a relationship-write path **no test and no run in any variant at any seed
    has ever executed.**
  - **`Utility.cs` 563–570** prices a candid report made when the teller has something at stake — the
    direct counterpart of the denial that milestones 010 and 011 spent two milestones measuring, and
    **nobody has ever taken it.** The denial's cost is measured to four decimal places; its opposite
    number has never been evaluated once.
- **Vocabulary members with no exerciser.** `Filters.cs` carries 28 uncovered lines in a 154-line
  file — the second-largest block after `Program.cs`, and disproportionate enough to be worth its own
  look rather than an assumption.

The second category is the reason to do this. **Before milestone 011, `AdvanceInvestigation`'s
cold-trail branch was in it** — no test, unreachable in every natural run, inert since it was written.
It was found by accident, ten milestones late. This report would have named it.

## Scope

**In:**

1. **Account for every uncovered line**, as one of: legitimately uncovered and why; dead and removed;
   or **a live edge nothing has ever run**, which is a finding and gets a test. **The deliverable is
   the accounting, not the percentage.** A number driven up by testing `Program.cs` would be worse
   than the number it replaced.
2. **Make coverage a repeatable check**, with its exclusions written down rather than remembered, so
   the next milestone can be asked what it left untouched.
3. **Make mutation systematic rather than hand-picked.** Today the author chooses which mutations to
   try, which is itself assumption-sharing — the harness inherits the priors it exists to escape. A
   pass that mechanically perturbs guards, comparisons and boundaries across the changed surface and
   reports which survive a green suite is an adversary that chooses nothing.
4. **Promote single-seed invariants to sweeps.** Most behavioural assertions are pinned at seed 42.
   Establish which are seed-independent claims being checked at one seed, and sweep those.
5. **Write the "where to look and what to distrust" note** into the review process — a short
   per-commit surface naming the claims that would be expensive if wrong, so Matt's review lands on
   those rather than on a whole diff.
6. **Record in `REVIEW_LEDGER.md`, permanently, that milestones 011 and 012 rest on a weaker basis**,
   and what the instruments do and do not make up for.

**Out:** no simulation behaviour change of any kind — no new claim kinds, characters, variants,
generators, or coefficients; no scenario work; no persistence; no tiering; no interface change.
**If an instrument finds a defect, it is recorded as a finding and not fixed here** unless fixing it
is a one-line correction to something this milestone itself added — a milestone that both builds the
instrument and acts on it cannot report honestly on either.

## Rulings taken at planning time

**1 — The deliverable is the accounting, not the percentage.** A coverage number is a floor and this
milestone must not treat it as a score. Every uncovered line ends in one of three named buckets, and
"legitimately uncovered" is a real answer that has to be argued rather than a way of avoiding one.

**2 — A live edge nothing has ever run is a finding, and it is recorded whether or not it flatters
anybody.** Two are already known and re-verified. There will be more, and the count goes in the
archive.

**3 — Dead code is removed, not tested.** A test written to cover something nothing needs is worse
than the uncovered line: it makes the surface look exercised and it defends code that should go. The
project has closed a trait vocabulary and a relationship vocabulary on exactly this rule — an entry
that cannot name a reader does not belong.

**4 — An instrument this project relies on must itself be checked, and this one has already failed
twice.** Milestone 011's mutation harness reported build failures as "no test failed" and hid two
unpinned rules. Milestone 012's actor-parity harness drove the session with a policy that could not
reach the state it was testing for, and reported a false absence. **An instrument is not evidence
until it has been shown to report correctly.** Anything promoted out of scratch and into the
repository gets tests of its own, including a deliberately-broken case proving it reports failure.

**5 — Systematic means the tool chooses, not the author.** A mutation pass whose targets are a
hand-written list is the same instrument milestone 011 already had. If mechanical perturbation of the
real surface turns out to be impractical here, **that is the finding** — say so and describe what was
tried, rather than shipping a longer hand-written list and calling it systematic.

**6 — Nothing here claims to replace an adversary.** The instruments catch *the code does not do what
the author thinks*. They do not catch *the author's framing of the problem is wrong*, which is the
class Codex was best at and the class where Matt is now the only backstop. The ledger says so in
those terms.

**7 — No baseline may move.** This milestone adds no behaviour, so every trace hash, chosen-action
digest, decision count and viewpoint render must be **byte-identical to `c637092`**, milestone 012's
implementation commit and the current behavioural state. That is the milestone's own strongest check
on itself, and any movement is a defect rather than a result — the opposite of the standing ruling in
010, 011 and 012.

**8 — Self-review by the same method**, which is now also the thing under construction: enumerate the
real surface empirically and diff it; mutation-check every change by reverting it and watching a
*named* test fail; walk the recurring-failure list. A review returning no findings is weak evidence
and is recorded as such.

**9 — No new place in the repository. Ruled by Matt on 2026-08-18, and carried forward unchanged.**
`AGENTS.md`'s boundaries stand as written: `docs/`, the two `src/` projects, `tests/`. The mutation
harness therefore lives inside `tests/CrimeEmpire.Simulation.Tests/`, which is an existing boundary
rather than a new concept.

**What that costs, stated now rather than discovered later.** A mutation run edits source files on
disk, rebuilds, and runs the suite — so it cannot be an ordinary test, because the ordinary suite must
never trigger it and a test that invokes `dotnet test` on itself is a bad idea. It has to be excluded
from the default run and invoked deliberately, which means **the one instrument that has found every
recent defect is also the one nothing runs automatically.** That is a real wart and it is the price of
this ruling, not an argument against it. If it turns out uglier in practice than a directory would
have been, the decision is cheap to revisit and this paragraph is why.

## Implementation plan

1. **Establish the coverage baseline and its exclusions.** The `HEAD` measurement is already taken —
   92.10% line, 84.30% branch, 315 uncovered — and the per-file distribution is in hand. Capture the
   full uncovered list and decide the exclusion set. Per ruling 9 nothing new is created for it: the
   accounting and the exclusion list go in `docs/`, and anything executable goes under
   `tests/CrimeEmpire.Simulation.Tests/`.
2. **Triage every uncovered line into the three buckets**, with the reason recorded per region rather
   than per line. `Program.cs` is 118 and expected to be legitimate; `Filters.cs`'s 28 is the first
   real question.
3. **Act on bucket 3 — dead code out, live-but-unrun edges tested.** The two verified above are the
   starting point, not the list.
4. **Promote and harden the mutation harness**, with ruling 4's self-test. Then attempt ruling 5's
   mechanical target selection, and report honestly if it does not work here.
5. **Sweep the single-seed invariants.** Identify which are seed-independent claims, sweep those, and
   leave the genuinely seed-specific ones alone with a note saying which they are.
6. **Verify nothing moved**, per ruling 7 — full verification plus a byte-comparison of all five
   traces and all 30 viewpoint renders against `c637092`.
7. **Write the ledger entries**: the weaker-basis record, the coverage baseline, the new checks, and
   the per-commit review-surface habit.
8. **Archive as `docs/milestones/013-…md`, reset this file, one coherent commit, stop.**

## Open questions to settle during implementation, not now

- **How is the harness excluded from the default suite** without becoming invisible? A trait filter, a
  skip attribute, or a separate entry point are all workable and all slightly ugly; ruling 9 fixes
  *where* it lives and leaves *how* it is kept out of the way to implementation. Whichever is chosen
  has to leave it greppable, because an instrument nobody can find is one nobody runs.
- **Can coverage be collected over a natural run rather than the test suite?** That is the question
  that answers *what does the scenario never exercise*, which is a different and more interesting
  question than what the tests never exercise. `coverlet.collector` is a test collector; a console
  collector is not installed. Establish feasibility; if it is not available, say so plainly rather
  than approximating it.
- ~~**How much of the 376 is `Program.cs`-shaped?**~~ **Answered while scoping: 118 of 315, 37.5%.**
  The interesting remainder is under 200 lines, which makes this a smaller milestone than the original
  figure suggested.
- **Is `Filters.cs`'s 28 uncovered lines one thing or several?** Disproportionate for a 154-line file,
  and unexamined.

## Carried forward

Everything carried into milestone 012, plus what it added. Full list at the end of
`docs/milestones/012-a-shortfall-he-cannot-attribute.md`. Nothing here resolves any of it, by design —
but several are likely to be *measured* for the first time by it.

**From milestone 012:**

- **The bonus corroboration route** — Vincent asking Tommy about `UnattributedShortfall` — is
  unauthored and unscoped, a real consequence of routing the suspicion through ordinary testimony.
- **`Organization.Offices.Select(o => o.Domain).FirstOrDefault()`** is correct for a one-office
  fixture and needs a real rule the day a second domain exists.
- **Salvatore's stance on a claim his capo has personally disproven never self-revises**, however often
  it is contradicted; only being told again differently, or working something out himself, would move
  it, and neither happens for that belief.

**From the earlier list, one struck: `AdvanceInvestigation` reading and writing `owner` throughout was
corrected 2026-08-19** — see the correction appended to milestones 011 and 012's archives. The rest
stands: **the developer trace still says "he" for everybody, 59 strings; two incidents at one shop are
only ever staged; the cold-trail branch is unreachable at every seed tried; nobody holds a scored
relationship with Kane; four known reasons the denial stays shut, of which loyalty is the smallest; the tuning
guesses; the cast ceiling of six; obligation read but never moved; nothing raises trust; negative
trust and decay deferred; `GrievanceWeight` uncapped; no save/load; the empty-domain
`ConcealIncident(, target=…)` label; the timing of a pause is observable even when the occasion is
not; the player cannot see why an option is unavailable; nothing prevents a Godot script calling
`Cast.Build` directly; `AGENTS.md` mentions neither `docs/RELATIONSHIPS.md` nor the Godot headless
check.

## Ordered review process

Unchanged, and more load-bearing than before. Matt takes commits in order, oldest first; each review
names the exact commit whose diff was inspected; the coverage table in `REVIEW_LEDGER.md` is the
record. **Never write "verified" or "accepted" from a review report alone** — including one of
Claude's own. Matt's confirmation of a named commit is the only thing that counts, and a self-review
clears work to build on without establishing anything about correctness.
