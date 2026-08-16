# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**No milestone is active. Do not start one.**

Milestones 001–006 are complete and accepted. **Milestone 007 — Scenario Reach is implemented and
awaiting Codex review.** Its full record, including its scope, the ten rulings it was built to
reproduced verbatim, what it achieved, and the two things it deliberately did not, is in
`docs/milestones/007-scenario-reach.md`.

`REVIEW_LEDGER.md` alone defines review coverage; consult its checkpoint directly rather than
inferring status from prose anywhere else, including this file. Its "Current — milestone 007" baseline
section is explicitly **not** an acceptance, and the last accepted baseline remains milestone 006's.

**Milestone 008 has not been chosen.** Do not infer it from the candidate list or technical-debt list
in `ROADMAP.md`, or from the carried-forward items below. Confirm scope with Matt and write it here
before changing simulation behaviour.

## What milestone 007 found, because it should shape the next scope decision

**The edge now reaches a decision, and what it contributes there is very small.** Two perceived
account conflicts take Vincent's trust in Salvatore from 0.45 to 0.031. The `relationship effects`
component on his next report to that boss moves from **0.0440 to 0.0063** — non-zero, entirely
attributable to the conflicts, and about four hundredths of a point against decision margins in this
scenario of the order of one. Decision-relevant; emphatically not choice-changing.

Most of the movement is absorbed before it arrives: `Utility.Loyalty` weights trust at `0.45` and
subtracts `0.5 ×` any grievance, so Vincent's standing grievance of 0.35 eats more than a full
collapse of trust. **The open question has moved** from "can a relationship consequence be shown at
all" — which 004, 005 and 006 could not answer in play — to "is it worth anything once it is shown".
That is a schema question and it belongs to `OPEN_CONCERNS.md` #3, which now carries this evidence.

**Which relationship a mechanism lands on decides whether it matters.** 006's conflict fell on
Salvatore, who has no scored decision that reads a relationship, and changed nothing. The identical
mechanism landing on Vincent, who reports upward, is measurable. A relationship schema that says how
dimensions are stored and updated without saying which decisions read them will not predict this.

**And the recurring lesson took a new form.** Milestone 006's rounds all found a claim that was true
of the code and false of the record. This milestone found two tests asserting against a *relationship
the model did not have* — one inferring that a report was a reply from a two-day window, one requiring
testimony behind a belief seeded from a source outside the cast. Both passed for years and stopped the
moment surrounding behaviour moved. The question to carry: **is this assertion checking a link the
simulation actually records, or one the test is inferring?**

## Carried forward

Open items the next scope decision should see. Fuller versions live in the milestone archives and
`ROADMAP.md`'s technical-debt list.

- **The trust edge reaches a score and barely moves it** — the numbers above. The most useful open
  figure the project has.
- **Concealment does not quiet the witnesses it is named for.** `AdvanceConceal`'s first step is
  "quiet the witnesses" and moves only `LegalExposure`. `Utility` prices a denial almost entirely on
  whether the actor believes he was seen, so this is what stands between the executor answering his
  delegator — which now happens in play — and an executor *denying* to him, which still never does.
- **`believedWitnesses` is scanned globally**, not scoped to the incident being concealed. Same defect
  shape as the `SeekCorroboration` scan `404b416` fixed. Changes nothing today, which is why 007
  excluded it.
- **`resentful-tommy` still makes the same decisions as baseline.** Now measured rather than assumed:
  `--compare` reports five distinct traces and four distinct chosen-action sequences and names the
  convergence. Kept, untuned and un-recut.
- **Salvatore is no longer contradicted at all.** 006's conflict reached the page only on Vincent's
  second concealing report, which 007 correctly stopped him filing. Not a regression — the mechanism
  moved to a listener who does something with it — but the boss-side path is now covered only by
  staged unit tests.
- **The bakery is never collected from.** Nobody in the organisation knows it is refusing, which is
  the asymmetry that leaves the capo room to think; it does mean a second collection cycle sits in the
  fixture unexercised.
- **Trust cannot go negative.** Absence of trust and distrust are the same state.
- **The concealment MVP rule is a placeholder, not a permanent design.**
- **Tuning guesses**: the `FirstHandTestimony` suspicion discount of `0.15`, the `Discovery` discount
  of `0.10`, and `Relations.ConflictTrustCost` of `0.35` — the last now with a measured consequence.
- The empty-domain label, `ConcealIncident(, target=...)`.

## Longer-standing deferrals

- the relationship-design document — `OPEN_CONCERNS.md` #3, now carrying two milestones' worth of
  executable evidence. **Not authorized.**
- relevance tiering and its continuous-calendar engineering risk;
- persistence and SQLite;
- Godot / `net10.0` compatibility — cheap, gates nothing today, worth a standalone commit rather than
  a milestone;
- generalized rumor, evidence, prosecution, media, and public-information channels;
- broader organizations, diplomacy, careers, corruption, and surveillance systems;
- the redundant test-project target framework, unless Matt separately authorizes a
  documentation/maintenance change.

## Ordered review process

Review is **manual**. There is no monitor running on a timer, no checkpoint the repository keeps for
itself, and nothing that will notice a commit unless somebody points a review at it.

- Matt takes commits in order, oldest unreviewed first, one at a time.
- Each review names the exact commit whose diff was inspected.
- A later documentation commit does not stand in for the implementation commit beneath it. If two
  land back to back, both still need reviewing, in order.
- The coverage table in `REVIEW_LEDGER.md` is the record. It is maintained by hand, which is why it
  is the authority rather than the prose around it.

Two commits currently sit later than that table's checkpoint and have no row: the milestone-006
closeout, and milestone 007's implementation-and-archive commit. Both still need reviewing in turn.

Never write "verified" or "closed" from a review report alone. A report must name the exact commit
reviewed, and Matt must confirm acceptance. That rule exists because the record twice claimed a
verification that had not happened.
