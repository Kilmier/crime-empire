# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**No milestone is active. Do not start one.**

Milestones 001–008 are complete and accepted. The most recent, **008 — Relationship Readers and the
Executable Schema**, settled the reader side of the relationship model and built the instrument that
measures it: loyalty's four contributions each arrive at every reader as their own facet-tagged score
component, grievance is out of the clamped sum it used to be buried inside, and a developer-facing
diagnostic reports gross, net, candidate margin and a like-for-like counterfactual with no significance
cutoff. `docs/RELATIONSHIPS.md` is the prototype schema, written last from measured results.
**No coefficient in the model was changed.**

Milestone 008's code went through **three review rounds, and its accepted status rests on `7e0700e` —
not on its implementation commit.** `7a9773b` was rejected on one finding, `9a29342` on two, and
`7e0700e` was reviewed with no findings and accepted by Matt on 2026-08-16. Full record, including
Matt's ten rulings reproduced verbatim and both corrections appended at the foot, is in
`docs/milestones/008-relationship-readers.md`.

`REVIEW_LEDGER.md` alone defines review coverage; consult its checkpoint directly rather than
inferring status from prose anywhere else, including this file. The checkpoint now stands at
`7e0700e`. The commit that moved it is itself later than it, has no row, and needs reviewing in its
turn.

**Milestone 009 has not been chosen.** Do not infer it from the candidate list or technical-debt list
in `ROADMAP.md`, or from the carried-forward items below. Confirm scope with Matt and write it here
before changing simulation behaviour.

## What milestone 008 found, because it should shape the next scope decision

**The relationship channel is load-bearing.** Removing relationship state changes which candidate wins
at **1–3 decisions in every variant** — 2 / 3 / 3 / 1 / 3, reported by `--compare`. Milestone 007's
`0.0377` stands as a figure and was the wrong thing to generalise from: it measured the
trust-to-partial-report path, which is the one place in the model where two loyalty reads nearly
annihilate. `Fear` reaches **1.44** on a decision whose whole margin is 0.25.

**Two structural attenuators, neither a coefficient.** A partial report carries `+0.7 × loyalty` for
the standing reporting buys and `−0.5 × loyalty` for what an omission costs, netting `0.2 × loyalty`;
and grievance was subtracted *inside* the clamp, so once it exceeded a character's bond, further
grievance was free and further trust worthless.

**A soldier who resents his capo now conceals rather than reporting to him.** At seed 42
`resentful-tommy` diverges from `baseline` for the first time. Nobody wrote a rule connecting
resentment to concealment. **Recorded with its fragility:** the margin is 0.0279 against ±0.05 noise,
and the divergence holds at seeds 42 and 31337 but not at 1, 7, 99 or 2024.

**And the lesson, from three rounds of findings that were all the same defect in different costumes:
a claim true of one context and false of the one it was stated in.** Four contributions separately
computed and not separately emitted. Hashes true of a build one edit earlier. A `[0,1]` range true of
every fixture and false of the public API, with an argument for removing a clamp resting on it. The
first correction *verified itself*, with a diff that excluded the only region its change touched.

Question to carry: **is this claim true of the thing I am saying it about, or only of something
adjacent to it?**

## Carried forward

Open items the next scope decision should see. Fuller versions live in the milestone archives and
`ROADMAP.md`'s technical-debt list.

- **Concealment does not quiet the witnesses it is named for**, and `believedWitnesses` is scanned
  globally rather than scoped to the incident. Untouched by 008 per ruling 4. Together they are what
  keeps an executor from ever denying to his delegator.
- **The `0.9 × Loyalty` versus `0.4` denial-premium question is unruled**, deliberately. It gates the
  candidate above.
- **Obligation is read but never moves.** `Relations.Establish` is its only writer and that is
  scenario construction.
- **Nothing raises trust.** Conflicts lower it and no runtime path restores it, so a relationship can
  be damaged and never repaired. Nobody decided that; it is now written down.
- **Negative trust and decay are deferred, not retired**, each with a stated condition for return.
- **`GrievanceWeight` is unbounded.** A cap was considered as 008's remedy and explicitly rejected in
  favour of unbundling, so it is open rather than answered.
- **Tuning guesses**: `FirstHandTestimony` 0.15, `Discovery` 0.10, `Relations.ConflictTrustCost` 0.35,
  and `LoyaltyReading.GrievanceWeight` 0.50 — the last two preserved exactly by 008 and still
  provisional.
- **`AGENTS.md` does not mention `docs/RELATIONSHIPS.md`.** Flagged, not taken: no ruling authorized
  editing `AGENTS.md`. A one-line addition to its conditional-reading list is the natural fix.
- **The cast is six and that is a ceiling, not a trend.**
- **The lifecycle loses rulings** when the archive and the reset land in one commit. Demonstrated a
  third time by 008, mitigated by reproducing them in the archive. `AGENTS.md` is Matt's.
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
verification that had not happened — and, at `9a29342`, once recorded real measurements taken before
the last edit as though they described the commit.
