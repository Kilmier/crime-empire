# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**Milestone 010 — A Denial That Can Win — is active.** Authorized by Matt on 2026-08-16, from
`ROADMAP.md`'s candidate 2.

Milestones 001–009 are complete and accepted. **Milestone 009 was accepted on 2026-08-16 on the
strength of `7ca7819`**, after five Codex rounds and one self-review — nine findings in total, on work
declared verified each time. `REVIEW_LEDGER.md`'s checkpoint stands at `7ca7819`.

**Codex has been withdrawn from the review loop until further notice.** Claude implements and reviews
its own work. See ruling 8 and `REVIEW_LEDGER.md` §"From milestone 010 onward, review is
self-assessment" for what that changes and what it cannot replace.

## What this milestone is for

The model has had, since milestone 004, a precise distinction between what a man privately holds and
what he claims — built specifically so a subordinate could lie to the person who sent him. **No
accepted run has ever produced that exchange.** Tommy answers his delegator honestly every time, in
every variant, at every seed tried.

The reason is not that lying is scored too harshly in the abstract. It is two defects that together
make a denial unaffordable for the one man in a position to tell one.

### Defect 1 — quieting the witnesses does not quiet anything

`Strategies.AdvanceConceal` runs two steps, `"quiet the witnesses"` and `"tidy the paperwork"`, and
does the same thing for both: a discretion roll, and `LegalExposure` up or down by a tenth. **The
concealer's own belief that he was seen is untouched.** `Strategies.ResolveViolence` gives the
executor `WitnessSawIncident` at 0.6 as an `Inference` — he reasons that doing it in the street means
somebody watched — and nothing in the game can ever move it.

So the strategy named for silencing witnesses silences nobody, not even in the mind of the man
running it.

### Defect 2 — the scan is global

`Utility` prices a denial almost entirely on that belief: a maximum over **every**
`WitnessSawIncident` he holds, whatever incident it belongs to. A man concealing one thing is priced
on the most-witnessed thing he knows about. Same defect shape as the `SeekCorroboration` scan
`404b416` fixed, and on `REVIEW_LEDGER.md`'s load-bearing list under the same heading.

Together they are what stands between an executor answering his delegator — which happens — and an
executor *denying* to him, which does not.

## Scope

**In:**

1. **Carry the concealed incident on the strategy instance.** `Candidate.AboutIncident` knows which
   incident a `ConcealIncident` is about; `Commit` records it in `AttemptedConcealments` and then
   drops it. `StrategyInstance.SourceEventId` is declared and has never been set by anything. The
   steps cannot act on an incident the instance cannot name.
2. **Make "quiet the witnesses" act on the concealer's belief about *that* incident**, as a function
   of the discretion roll already there. It may fail.
3. **Scope `believedWitnesses` to the incident the suppressed claim belongs to**, by `Claim.EventId`.
4. **Measure whether a denial now wins**, in the accepted scenario and across the five variants, and
   report the answer whichever way it falls.

**Out:** no new claim kinds, no new characters, no new scenario variants, no persistence, no tiering,
no relationship-schema work, no Godot or interface change beyond what the player boundary needs to
keep its guarantees.

## Rulings taken at planning time

**1 — The incident is the identity, not the target.** Milestone 005 settled this for concealment
redundancy after Codex found `(Kind, TargetId)` treating two beatings at one shop as one thing. The
same rule applies here: a step acts on the incident the instance is concealing, never on "whatever
happened at this address".

**2 — Quieting witnesses changes a belief, not the world.** It must not delete a `Trace`, alter the
truth log, or touch anybody else's cognition. What moves is the concealer's own confidence that he
can be placed at that incident. **Being wrong must stay possible in both directions** — a man who
believes he has cleaned up and has not is the interesting case, and `ResolveViolence` deliberately
records his witness belief as `Inference` rather than something he established, precisely so it can
be wrong.

**3 — No coefficient is tuned to make the denial win.** Not `0.25`, not `3.0`, not the erosion rates,
not the discretion threshold. The two defects are structural, and fixing them either lets a denial
compete on the numbers already there or it does not. **If it still loses, that is the milestone's
result** — measured, with the margin stated, recorded as a finding rather than chased.

**4 — Deception stays a candidate, scored through the ordinary pipeline.** No code branches on a
trait to produce a lie, no scripted denial, no special case for the executor. `DESIGN_DECISIONS.md`
§"Information channel" settles this and nothing here reopens it.

**5 — Actor parity.** Whatever becomes available to Tommy is available to a player controlling him,
through the same candidate set. And the player boundary keeps its guarantees: a delegator must not be
shown the concealer's private belief about who saw him, and the pending decision must not start
carrying a fact its holder lacks.

**6 — Baselines will move, and that is expected.** A change to what concealment does will change
histories. Every moved figure goes into `REVIEW_LEDGER.md` with its reason. Milestone 009 ended
byte-identical to 008; this one will not, and that is not a defect.

**7 — The bakery stays uncollected and the cast stays at six.** Both are carried-forward items with
rulings behind them. Nothing here is a licence to touch either.

**8 — Self-review is the review, and it is recorded as weaker than what it replaces.** The method is
the one that actually found defects rather than the one that felt thorough: enumerate the real
surface empirically and diff it; mutation-check every fix by reverting it and watching a named test
fail; test for the *kind* of defect rather than the reported instance; walk `REVIEW_LEDGER.md`'s
recurring-failure list as an explicit checklist before declaring done. **A self-review that returns no
findings is weak evidence and will be recorded as such**, not as a clean bill.

## The question this milestone actually answers

Not "can we make Tommy lie" — that is a coefficient away and worth nothing. It is: **once a man can
act on his own exposure, does lying to the person who sent him become a thing he would choose?** If
yes, milestone 004's provenance distinction finally has a run to justify it, and the delegation
topology the vision doc describes starts producing the problem it was built to produce. If no, the
model owes an explanation of what else is holding it shut, and that explanation is the deliverable.

## Carried forward

Everything from milestone 009 is still carried; this milestone touches none of it except the two
defects named above.

- **The timing of a pause is observable even when the occasion is not.**
- **Whether an outfit whose boss cannot name his own soldiers is the right model.** Membership is not
  knowledge; a named office and an encounter are.
- **The player cannot see why an option is unavailable.**
- **Nothing prevents a Godot script from calling `Cast.Build` and `Runner.Run` directly.**
- **`AGENTS.md` mentions neither `docs/RELATIONSHIPS.md` nor the Godot headless check.**
- **One controlled character, one viewpoint character, chosen at the start screen and never changed.**
- **No save/load**, so a session ends when the process does.
- **Four decisions in ninety in-game days** is the demo's honest weakness, surfaced by 009 and not on
  the candidate list.
- Obligation is read but never moves; nothing raises trust; negative trust and decay deferred;
  `GrievanceWeight` uncapped; the tuning guesses; the cast ceiling of six; the empty-domain
  `ConcealIncident(, target=…)` label — **which this milestone may incidentally fix**, since a
  concealment that knows its incident may also know its domain.

## Ordered review process

Unchanged in shape, changed in who performs it. Matt takes commits in order, oldest first; each
review names the exact commit whose diff was inspected; the coverage table in `REVIEW_LEDGER.md` is
the record. **Never write "verified" or "accepted" from a review report alone** — including one of
Claude's own. Matt's confirmation of a named commit is the only thing that counts, and that rule
matters more now that the reviewer and the author are the same.
