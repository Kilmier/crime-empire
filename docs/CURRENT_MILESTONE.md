# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**No milestone is active. Do not start one.**

Milestones 001–007 are complete and accepted. **Milestone 008 — Relationship Readers and the
Executable Schema — is implemented and archived, and has been through two rejected review rounds.
`7a9773b` returned one finding and `9a29342` returned two; Matt accepted all three, and a second
corrective commit now awaits Codex review. Milestone 008 is not accepted and not verified.** Its full
record, including Matt's ten rulings reproduced verbatim and both corrections appended at the foot, is
in `docs/milestones/008-relationship-readers.md`.

**The three findings, because the pattern across them matters more than any one.**

- `7a9773b`: ruling 3 required loyalty's four contributions to be separately inspectable, and three
  were emitted fused under a union flag — separately computed, then put back together on the way to
  the score.
- `9a29342`, first: **the recorded verification hashes were false.** They were measured before a late
  widening of the diagnostic listing and never re-measured. The commit *did* verify itself, with a
  diff that deliberately excluded the diagnostic block — the only place the change appeared.
- `9a29342`, second: the unclamped bond rested on a range `Psychology` documented and never enforced,
  so the removal of that clamp was a real behaviour change for any out-of-range caller.

All three are the same family the milestone kept naming while producing it: **a claim that was true of
the fixtures and false of the API, or true of an earlier moment and false of the committed one.**

As with 006 and 007, the lifecycle reset this file inside the archiving commit, so **the archive is
the only copy of milestone 008's rulings**. Ruling 10 kept the existing lifecycle deliberately; the
mitigation was to reproduce them in the archive before the reset rather than to change `AGENTS.md`.

`REVIEW_LEDGER.md` alone defines review coverage; consult its checkpoint directly rather than
inferring status from prose anywhere else, including this file. The checkpoint stands at `6ba0737`.
`b8e5ed4` was reviewed with no findings and accepted, and remains beyond the checkpoint. Milestone
008's commit is later still, has no row, and needs reviewing in its turn.

## What milestone 008 found, because it should shape the next scope decision

**The relationship channel is load-bearing, and milestone 007's headline number was measuring the
weakest path in it.** Removing relationship state changes which candidate wins at **1–3 decisions in
every variant** — 2 / 3 / 3 / 1 / 3, reported by `--compare`. 007's `0.0377` stands as a figure and
was the wrong thing to generalise from: it measured the trust-to-partial-report path, which is the one
place in the model where two loyalty reads nearly annihilate. `Fear` reaches **1.44** on a decision
whose whole margin is 0.25.

**Two structural attenuators, both now removed or made visible, neither a coefficient.**

- A partial report carries `+0.7 × loyalty` for the standing reporting buys and `−0.5 × loyalty` for
  what an omission costs. They net to `0.2 × loyalty`, so the full range of `Trust` is worth `0.09` to
  that candidate — less than the spread between two candidates' `±0.05` noise draws. Both are
  legitimate and both were kept; what changed is that they are now separately identifiable.
- Grievance was subtracted **inside** the clamp that produced loyalty, so once it exceeded a
  character's bond the sum floored at zero: further grievance was free and further trust was
  worthless. It now sits outside, applied per reader at the same `0.50`.

**A soldier who resents his capo now conceals rather than reporting to him.** At seed 42
`resentful-tommy` diverges from `baseline` for the first time — 9 April, Tommy chooses
`ConcealIncident` at 0.4689 over reporting to Vincent at 0.4410, because his grievance takes 0.21 out
of what reporting to that particular man is worth. Nobody wrote a rule connecting resentment to
concealment. **Recorded with its fragility:** the margin is 0.0279 against ±0.05 noise, and the
divergence holds at seeds 42 and 31337 but not at 1, 7, 99 or 2024. Nothing was tuned; no coefficient
in the model was changed by this milestone.

**And the recurring lesson took a new form: the defect was inside the instrument.** The only way to
ask how much of a score came from a relationship was to filter components named `relationship
effects`, and 36% of them read no relationship state at all — `−0.45 × proud` wearing a relationship
label. Two production tests already aggregated that way and the new diagnostic was about to. The
question to carry: **is this grouping by what the thing is, or by what it is called?**

## Carried forward

Open items the next scope decision should see. Fuller versions live in the milestone archives and
`ROADMAP.md`'s technical-debt list.

- **Concealment does not quiet the witnesses it is named for**, and `believedWitnesses` is scanned
  globally rather than scoped to the incident. Untouched by 008 per ruling 4. Together they are what
  keeps an executor from ever denying to his delegator.
- **The `0.9 × Loyalty` versus `0.4` denial-premium question is unruled**, deliberately, per ruling 4.
  It is what gates the milestone-009 candidate above.
- **Obligation is read but never moves.** `Relations.Establish` is its only writer and that is
  scenario construction.
- **Nothing raises trust.** Conflicts lower it and no runtime path restores it, so a relationship can
  be damaged and never repaired. Nobody decided that; it is now written down.
- **Negative trust and decay are deferred, not retired**, each with a stated condition for return.
- **`GrievanceWeight` is unbounded.** A cap was considered as 008's remedy and explicitly rejected in
  favour of unbundling, so it is open rather than answered.
- **Tuning guesses**: `FirstHandTestimony` 0.15, `Discovery` 0.10, `Relations.ConflictTrustCost` 0.35,
  and `LoyaltyReading.GrievanceWeight` 0.50 — the last preserved exactly by 008 and still provisional.
- **`AGENTS.md` does not mention `docs/RELATIONSHIPS.md`.** Flagged, not taken: no ruling authorized
  editing `AGENTS.md`. A one-line addition to its conditional-reading list is the natural fix.
- **The cast is six and that is a ceiling, not a trend.**
- **The lifecycle loses rulings.** Unchanged and now demonstrated a third time. `AGENTS.md` is Matt's.
- The bakery is never collected from; the boss-side conflict path is covered only by staged unit
  tests; the empty-domain label `ConcealIncident(, target=...)`.

## Longer-standing deferrals

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
- A later documentation commit does not stand in for the implementation commit beneath it.
- The coverage table in `REVIEW_LEDGER.md` is the record. It is maintained by hand, which is why it
  is the authority rather than the prose around it.

Never write "verified" or "closed" from a review report alone. A report must name the exact commit
reviewed, and Matt must confirm acceptance. That rule exists because the record twice claimed a
verification that had not happened.
