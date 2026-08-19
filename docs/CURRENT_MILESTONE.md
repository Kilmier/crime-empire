# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**Milestone 012 — Instruments, Not Vigilance — is planned and NOT authorized to begin.** Matt chose
the direction on 2026-08-18. The scope and rulings below are Claude's proposal and need his sign-off.

**Milestone 011 is implemented and awaiting review**, at `6a8a765`. Nobody has reviewed it and nobody
has accepted it. `REVIEW_LEDGER.md`'s coverage checkpoint stands at `824f3fc`, milestone 010.
**Milestone 012 must not begin before 011 is accepted.**

**Matt intends to have Codex review `6a8a765` at a later date.** So Codex is intermittent rather than
withdrawn, which is a correction to how this file read earlier on 2026-08-18 — it said "unavailable
for the foreseeable future", and a sentence like that decides what the next session does. Three
consequences, none of them optional:

- **011 will end up on a stronger basis than 010**, which was accepted on a self-review alone. Say so
  when it happens rather than treating the two as equivalent.
- **The commits after `6a8a765` — `40f0ded`, `520924b`, `3004d2f` — are documentation only and still
  need their turn.** Reviewing straight to `HEAD` is the mechanic that permanently and silently
  skipped `e83dacf`; `REVIEW_LEDGER.md` records it under "How this record has failed". Oldest
  unreviewed first, and a docs commit does not stand in for the implementation commit beneath it.
- **The review will arrive long after the work.** That is the actual problem this milestone addresses,
  and it is a different problem from Codex being gone.

Milestones 001–010 are complete and accepted.

**Scenario reach II — the shortfall the organisation cannot attribute — is deferred to milestone
013.** Its measured evidence and its two load-bearing rulings are preserved in `ROADMAP.md`; the full
plan is at `40f0ded` if it is wanted verbatim. It was displaced rather than dropped: it moves every
baseline again, and doing that on a third consecutive unreviewed milestone is the risk this one
exists to reduce first.

## What this milestone is for

**Review now lands long after the work, and sometimes not at all.** That is the premise. It is not the
same as Codex being gone — Matt intends a Codex round on `6a8a765` eventually — and the milestone is
worth doing either way, because a defect found by an instrument on the day it is written costs less
than the same defect found by an adversary three milestones later, and an adversarial round spent on
things a machine could have caught is a round wasted.

What Codex supplied was not diligence. It was **different priors**. `REVIEW_LEDGER.md` is specific:
across milestone 009 it returned nine findings on work declared verified each time, and every one was
a place the author had convinced himself. Re-reading found nothing; what broke it open was mechanical.

That pattern has held since. Milestone 010's self-review found three defects and milestone 011's found
three more, and **every one of the six came from a mutation check rather than from looking again**.

So the only adversaries available are the ones with no priors at all, and this milestone builds them.
It adds **no simulation behaviour**, which is the point: there is nothing new to review, and the
instruments are what the next behavioural milestone will be reviewed with.

### What is already there and has never been run

`coverlet.collector` has been a package reference in the test project for some time and produces a
report the first time it is asked for. Measured at `6a8a765`: **92.2% line, 84.2% branch, 376
uncovered lines across 45 types.** Sampling it immediately separates three different things:

- **Legitimately uncovered.** `Program.cs` alone is 118 of the 376 — the CLI entry point, exercised by
  the verification commands rather than by unit tests. It is the largest single number in the report
  and the least interesting, which is exactly how a naive coverage milestone would waste itself.
- **Live edges nothing has ever run.** `Runner.cs` 303–307 raises a grievance and resentment when a
  character observes somebody else's policy breach — a relationship-write path that **no test and no
  run in any variant at any seed has ever executed**. `Utility.cs` 563–570 prices a candid report made
  when the teller has something at stake — the direct counterpart of the denial milestones 010 and 011
  spent two milestones measuring, and nobody has ever taken it.
- **Vocabulary members with no exerciser.** `Filters.cs` 146–151: claim kinds that never appear in a
  rejection reason.

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
   The 10-seed sweep run while planning 013 took seconds and settled a question reading could not.
   Establish which existing assertions are seed-independent claims being checked at one seed, and
   sweep those.
5. **Write the "where to look and what to distrust" note** into the review process — a short per-commit
   surface naming the claims that would be expensive if wrong, so Matt's review lands on those rather
   than on a whole diff.
6. **Record in `REVIEW_LEDGER.md`, permanently, that milestone 011 onward rests on a weaker basis**,
   and what the instruments do and do not make up for.

**Out:** no simulation behaviour change of any kind — no new claim kinds, characters, variants,
generators, or coefficients; no scenario work (that is 013); no persistence; no tiering; no interface
change. **If an instrument finds a defect, it is recorded as a finding and not fixed here** unless
fixing it is a one-line correction to something this milestone itself added — a milestone that both
builds the instrument and acts on it cannot report honestly on either.

## Rulings taken at planning time

**1 — The deliverable is the accounting, not the percentage.** A coverage number is a floor and this
milestone must not treat it as a score. Every uncovered line ends in one of three named buckets, and
"legitimately uncovered" is a real answer that has to be argued rather than a way of avoiding one.

**2 — A live edge nothing has ever run is a finding, and it is recorded whether or not it flatters
anybody.** Two are already known: the grievance raised on observing a policy breach, and the price of
a candid report with something at stake. There will be more, and the count goes in the archive.

**3 — Dead code is removed, not tested.** A test written to cover something nothing needs is worse
than the uncovered line: it makes the surface look exercised and it defends code that should go. The
project has closed a trait vocabulary and a relationship vocabulary on exactly this rule — an entry
that cannot name a reader does not belong.

**4 — An instrument this project relies on must itself be checked, and this one has already failed
once.** Milestone 011's mutation harness reported build failures as "no test failed" and hid two
unpinned rules until that was noticed. Anything promoted out of scratch and into the repository gets
tests of its own, including a deliberately-broken case proving it reports failure.

**5 — Systematic means the tool chooses, not the author.** A mutation pass whose targets are a
hand-written list is the same instrument milestone 011 already had. If mechanical perturbation of the
real surface turns out to be impractical here, **that is the finding** — say so and describe what was
tried, rather than shipping a longer hand-written list and calling it systematic.

**6 — Nothing here claims to replace an adversary.** The instruments catch *the code does not do what
the author thinks*. They do not catch *the author's framing of the problem is wrong*, which is the
class Codex was best at. They shorten the interval before the first class is caught and they leave the
second class exactly where it was — with Matt, and with whatever Codex round eventually arrives. The
ledger says so in those terms.

**7 — No baseline may move.** This milestone adds no behaviour, so every trace hash, chosen-action
digest, decision count and viewpoint render must be **byte-identical** to `6a8a765`. That is the
milestone's own strongest check on itself, and any movement is a defect rather than a result — the
opposite of the standing ruling in 010, 011 and 013.

**8 — Self-review by the same method**, which is now also the thing under construction: enumerate the
real surface empirically and diff it; mutation-check every change by reverting it and watching a
*named* test fail; walk the recurring-failure list. A review returning no findings is weak evidence
and is recorded as such.

**9 — No new place in the repository. Ruled by Matt on 2026-08-18.** `AGENTS.md`'s boundaries stand as
written: `docs/`, the two `src/` projects, `tests/`. The mutation harness therefore lives inside
`tests/CrimeEmpire.Simulation.Tests/`, which is an existing boundary rather than a new concept.

**What that costs, stated now rather than discovered later.** A mutation run edits source files on
disk, rebuilds, and runs the suite — so it cannot be an ordinary test, because the ordinary suite must
never trigger it and a test that invokes `dotnet test` on itself is a bad idea. It has to be excluded
from the default run and invoked deliberately, which means **the one instrument that has found every
recent defect is also the one nothing runs automatically.** That is a real wart and it is the price of
this ruling, not an argument against it. If it turns out uglier in practice than a directory would
have been, the decision is cheap to revisit and this paragraph is why.

## Implementation plan

1. **Establish the coverage baseline and its exclusions.** Run it and capture the full uncovered list
   at `6a8a765`. Per ruling 9 nothing new is created for it: the exclusion list and the accounting go
   in `docs/` where the rest of the project's reasoning lives, and anything executable goes under
   `tests/CrimeEmpire.Simulation.Tests/`.
2. **Triage every uncovered line into the three buckets**, with the reason recorded per region rather
   than per line. Expect `Program.cs` to be the bulk and legitimate.
3. **Act on bucket 3 — dead code out, live-but-unrun edges tested.** The two already known are the
   starting point, not the list.
4. **Promote and harden the mutation harness**, with ruling 4's self-test. Then attempt ruling 5's
   mechanical target selection, and report honestly if it does not work here.
5. **Sweep the single-seed invariants.** Identify which are seed-independent claims, sweep those, and
   leave the genuinely seed-specific ones alone with a note saying which they are.
6. **Verify nothing moved**, per ruling 7 — full verification plus a byte-comparison of all five
   traces and all 30 viewpoint renders against `6a8a765`.
7. **Write the ledger entries**: the weaker-basis record, the coverage baseline, the new checks, and
   the per-commit review-surface habit.
8. **Archive as `docs/milestones/012-…md`, reset this file, one coherent commit, stop.**

## Open questions to settle during implementation, not now

- **How is the harness excluded from the default suite** without becoming invisible? A trait filter, a
  skip attribute, or a separate entry point are all workable and all slightly ugly; ruling 9 fixes
  *where* it lives and leaves *how* it is kept out of the way to implementation. Whichever is chosen
  has to leave it greppable, because an instrument nobody can find is one nobody runs.
- **Can coverage be collected over a natural run rather than the test suite?** That is the question
  that answers *what does the scenario never exercise*, which is 013's premise. `coverlet.collector`
  is a test collector; a console collector is not installed. Establish feasibility; if it is not
  available, say so plainly rather than approximating it.
- **How much of the 376 is `Program.cs`-shaped?** 118 lines are, and the answer changes how big this
  milestone actually is.

## Carried forward

Everything carried into milestone 011, plus what it added. Full list at the end of
`docs/milestones/011-the-detective-has-no-next-move.md`. Nothing in this milestone resolves any of it,
by design — but two items are likely to be *measured* by it for the first time:

- **The developer trace still says "he" for everybody**, 59 strings.
- **`AdvanceInvestigation` reads and writes `owner` throughout.**
- **Two incidents at one shop are only ever staged**; the cold-trail branch is unreachable at every
  seed; nobody holds a scored relationship with Kane. All three are 013's business.
- **Four known reasons the denial stays shut**, of which loyalty is the smallest.
- The tuning guesses; the cast ceiling of six; obligation read but never moved; nothing raises trust;
  negative trust and decay deferred; `GrievanceWeight` uncapped; no save/load; the empty-domain
  `ConcealIncident(, target=…)` label; four decisions in ninety days in the Godot demo.

## Ordered review process

Unchanged, and more load-bearing than before. Matt takes commits in order, oldest first; each review
names the exact commit whose diff was inspected; the coverage table in `REVIEW_LEDGER.md` is the
record. **Never write "verified" or "accepted" from a review report alone** — including one of
Claude's own. Matt's confirmation of a named commit is the only thing that counts, and with Codex
gone it is the only independent check the project has.
