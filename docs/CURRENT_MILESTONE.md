# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**Milestone 021 — Capability Is a Belief, Not a Stat — is implemented and tested, awaiting Matt's
acceptance.** Direction and all rulings settled by Matt on 2026-09-04; implemented the same day.
`Relations.AssessedCoercion` is gone, along with `RelationshipFacet.Capability` and
`Candidate.ExecutorCoercion`; capability is `ClaimKind.PersonIsCapable` on a graded `CapabilityBar`
ladder in `Cognition`, scored from `perceived`, and revised at runtime by the outcome of delegated
work (`Domain/Suitability.cs`). Build 0/0, **588 tests**, five accepted variants byte-identical on
trace *and* actions, `capable-angelo` moved as ruling 4 authorized, all Godot self-tests and the
two-process restart proof green. Four mutation checks. Full account, including two discoveries that
were not designed — capability beliefs travelling through the report channels, and a testimony-
acquired one being permanently unrevisable — in
`docs/milestones/021-capability-is-a-belief-not-a-stat.md`.

**It is unreviewed, and that matters here more than usual.** Codex ran out of usage mid-milestone-020;
everything from `34cd117` onward has had no adversary. Milestone 020 returned a P1 on both Codex
rounds it did get, plus a third defect found while scoping this one.

### What milestone 020 left, and where it stands

**Milestone 020 — The Right Person for the Job — is accepted and closed at `c25129a`**, then
corrected once more after acceptance by `8e6878e`. Full account:
`docs/milestones/020-the-right-person-for-the-job.md`; review history and verification baselines:
`docs/REVIEW_LEDGER.md` §"Measured — milestone 020".

Its correction sequence is the reason this milestone exists, and is worth carrying forward as
evidence rather than as history:

| Round | Found by | Defect |
|---|---|---|
| `f468e19` | Codex, FAIL | Executor capability scored from `world.Get(id).Capabilities[Skill.Coercion]` — the objective figure, which the actor does not hold |
| `436f6c7` | Codex, FAIL | Delegate candidates still drawn from `ctx.SubordinateIds`, the raw authority scan, not `AcquaintedIds` |
| `8e6878e` | scoping this milestone | The component now read relationship state and still reported `RelationshipFacet.None`, blinding the developer channel and reversing which candidate the relationship counterfactual named |

Three rounds, three defects, all in one scoring path, and **each one was pinned by a passing test
rather than caught by it.** `34cd117` and everything after it are unreviewed — Codex ran out of usage
on 2026-09-04.

### The problem this milestone addresses

`Relations.AssessedCoercion` is a **belief-shaped field with no belief mechanics**. It is written only
by `Relations.Establish`/`SetAssessedCoercion` — scenario construction — and never revised, so a
delegator's read of how good his man is at the job is fixed for the whole run no matter what that man
then does in front of him.

Worse, it is in the wrong place. Trust, Fear and Obligation have **no truth value**: there is no fact
of the matter about how much Vincent trusts Tommy beyond Vincent's own state. `AssessedCoercion` has a
referent — Tommy's actual `Capabilities[Skill.Coercion]` — so it can be **wrong**, which makes it a
belief about the world rather than an attitude toward a person. `AGENTS.md` requires truth, knowledge,
belief and evidence be kept distinct, and `Cognition` is where this project keeps things that can be
wrong, with provenance, confidence and contestability. The relationship record has none of those.

## Rulings

### Settled

1. **Capability belief moves into `Cognition`.** Matt, 2026-09-04, choosing option (b) over keeping it
   as a relationship dimension. `AssessedCoercion` and `RelationshipFacet.Capability` are deleted
   together when it lands, and the "executor capability" component legitimately returns to reading no
   relationship state at all.
2. **The relationship vocabulary is not reopened.** `RELATIONSHIPS.md` stays at four dimensions; the
   provisional-fifth note added by `8e6878e` is removed by this milestone rather than promoted.
3. **`Strategies.ResolveViolence` keeps reading the objective figure.** Committed force resolution
   computes what actually happened; it is not scoring an option, and milestone 020 settled this.
4. **Deliberate hash movement is expected and bounded.** Only `capable-angelo` has a capability
   component, so only its trace hash — and, if the scoring shape changes, its chosen-action digest —
   may move. **The other five variants must stay byte-identical on both**, and that is a hard
   constraint, not an aspiration.

5. **Magnitude is carried by which propositions are held — graded threshold claims, option (b3).**
   Matt, 2026-09-04. The ruling exists because **certainty is not magnitude**: "I am sure Tommy is up
   to it" and "Tommy is very good at it" are different statements, and encoding the second as the
   first collapses two distinctions into one number — this project's signature defect, what
   `LoyaltyReading` was unbundled to avoid, and what `RelationshipFacet` was built to detect.

   So a capability belief is **more than one proposition at different bars** — up to rough work,
   exceptional at it — each an ordinary `InformationRecord` with its own stance and confidence.
   Angelo is not "0.80 capable"; he is a man Vincent believes is up to rough work *and* exceptional at
   it. Tommy may be firmly believed capable and firmly believed *not* exceptional, which is a sharper
   statement than any single scalar. Rejected alternatives: one threshold claim only (magnitude
   vanishes, and Angelo and Tommy would differ solely in how sure Vincent is — degenerate for this
   milestone's own fork), and extending `Claim`/`InformationRecord` with a magnitude field (a
   whole-system change to serve one reader).

   **Three things are being kept distinct, and the ruling is that all three stay distinct:**

   | | What it is | Where it lives |
   |---|---|---|
   | Skill | how good the man actually is | `Capabilities[Skill.Coercion]`, in `World`, never read by scoring |
   | Certainty | how sure the delegator is of what he believes | `InformationRecord.Confidence`, per proposition |
   | Self-confidence | how confident the *executor* is in his own ability | **does not exist, and is not being added** — see below |

6. **Revising an assessment from confounded evidence is a deliberate attribution error.** Matt,
   2026-09-04. Tribute success turns on the mark's resistance, the method, Persuasion and a roll, not
   on the executor's Coercion alone, so a delegator who revises his read of the man from how the job
   went is drawing a conclusion the evidence does not support. That is the interesting behaviour and
   is modelled on purpose. It follows that **the milestone must prove the assessment can end up
   further from the truth than it started**, not merely that it moves.

7. **The executor's own confidence in doing the job is out of scope and is not being added.** Raised
   by Matt on 2026-09-04 while settling ruling 5. It is a fourth concept, distinct from all three
   above, and no decision reads it — the same rule that closed the trait vocabulary in milestone 001
   and removed `Affection` in milestone 006 excludes it until one does. Recorded in `ROADMAP.md`
   rather than built.

## Executable feature claim

**Situation.** Vincent delegates the grocery job to the man he rates highest. That man does the work.
Something about how it went reaches Vincent through a channel he actually has — the money arriving,
the operation blocking, or the account he gets when he asks. His read of that man moves. A **later**
delegation is scored against the revised read.

**Information contract.** Vincent may not observe `Capabilities[Skill.Coercion]`, the resistance drop,
or the executor's own cognition. He revises from what reached him and nothing else — and because the
evidence is confounded, he may revise **wrongly**.

**Persistent consequence.** A second delegation decision, later in the same run, scored on a different
belief than the first — and that belief carries a source, so the trace can say where it came from.

**Natural proof.** `capable-angelo` at seed 42 already produces a delegation, a force resolution, a
blocked step and a second collection cycle. The raw material is present.

**Acceptable non-result, stated in advance.** If no channel in the accepted fixture carries enough for
a revision to fire naturally, the honest outcome is a staged proof plus a recorded finding that *the
fixture cannot exercise it* — the same result milestones 010 and 011 produced. **That is not a licence
to tune a coefficient, add a channel, or adjust the fixture until it fires.**

## Scope

**In scope.** The claim vocabulary addition for capability; seeding it in `Cast.Build`/`Variants.Apply`
with a real source rather than a bare number; the scoring read moving from
`Social.Toward(...).AssessedCoercion` to `Perceived`; a revision path from an existing channel;
deleting `AssessedCoercion`, `Relations.SetAssessedCoercion` and `RelationshipFacet.Capability`;
`RELATIONSHIPS.md` and `INFORMATION_AND_LEGIBILITY.md` reconciliation; both replay comparators
updated for whatever new persistent state exists.

**Out of scope.** Assessments of Persuasion, Discretion or Investigation — one skill proves the
mechanism. Decay of an assessment over time (`RELATIONSHIPS.md`: decay returns when tiers supply a
timescale). A third subordinate, crew, equipment, recruitment, roster, payroll, resource transfer.
Player-facing display of the assessment. Any change to `ResolveViolence`. Any new organization,
character, business or career.

## Proof obligations

- **Natural** — the revision fires in an unmodified `capable-angelo` run, or the non-result above is
  recorded honestly.
- **Staged** — the revision rule exercised directly for both directions, up and down.
- **Negative control, omniscience** — changing only the executor's objective `Capabilities` leaves the
  delegator's belief and every score untouched. (This exists today and must survive the move.)
- **Negative control, wrong actor** — the belief moves for the delegator who received the information
  and for nobody else.
- **Provenance** — the revised belief names where it came from, and a belief acquired one way is
  distinguishable from the same belief acquired another.
- **Wrongness is representable** — a staged proof that the assessment can end up further from the
  objective figure than it started. If the design cannot express that, ruling 6 was answered wrongly.
- **Replay** — both `SimulationReplayTests` comparators cover the new state, mutation-checked per
  comparator, exactly as `34cd117` did for `AssessedCoercion`.
- **Hashes** — five variants byte-identical on trace and actions; `capable-angelo`'s movement
  deliberate, measured and recorded.
- **Mutation checks** — on every load-bearing distinction, including the tempting simplification of
  reading the objective figure "just for seeding".

## What is deferred, for whoever scopes the milestone after this one

Not authorization to start any of it — see `ROADMAP.md`.

Carried from milestone 020: Persuasion's effect on tribute success; crew size, equipment, preparation;
recruitment, roster, payroll, resource transfer; personnel management generally;
escalation-capability ownership as a general rule; a third or later subordinate; a general suitability
model across strategy kinds; capability affecting anything beyond force resolution.

Carried from milestone 017 and earlier: the five-column layout and other playtest-discovered
presentation debt; the wrapped-date/toolbar debt; a `PlayerNarration` prose rewrite; "You control"/"You
see through" unification; territory, patrol, weekly planning; additional businesses or operations; an
eighth character; employee-stat displays or new UI panels; the known pause-timing information leak;
new organizations, careers, or alternate playable roles.

Carried from milestone 016 and earlier: **124 live-edge findings and 5 apparently-dead lines**
(`docs/COVERAGE_ACCOUNTING.md`); systematic mutation automation and seed-sweep promotion; a queryable
decision-trace store; the allegation option naming the same person twice; the developer trace's uniform
"he"; nobody holding a scored relationship with Kane; the tuning guesses; obligation read but never
moved; slot management, autosave, cloud save, a save-browser UI, and cross-build save migrations. From
`docs/OPEN_CONCERNS.md` #3: decay and its rate, negative trust, whether respect/resentment are separate
dimensions, whether provenance should weight the social consequence, and whether `GrievanceWeight`
should be capped.
