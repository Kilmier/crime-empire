# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/004-provenance-precision.md` per
`AGENTS.md`'s milestone lifecycle, and this file is reset. This file is the sole handoff surface
between agents; do not create a separate handoff document.

## Status

**Milestone 004 is approved but not yet started.** It is blocked behind the milestone-003 fifth
correction — see "Review disposition" below. No provenance work begins until that corrective commit
is reviewed.

Milestone name: **Provenance Precision**

The milestone should be implemented by Claude, tested and committed as one focused change, then
reviewed by Codex. Do not begin milestone 005 after closeout.

## Goal

Make information provenance precise enough that simulation rules and player-facing language can
distinguish how a character acquired information.

Milestone 003 deliberately used the broad `SourceKind.Direct`, which currently conflates:

- participating in or authoring an event;
- witnessing an event;
- directly discovering a trace or consequence;
- receiving a first-hand account from a participant.

Neutral player-facing wording prevents false claims of attendance, but it is a workaround rather
than a complete model. This milestone replaces that workaround with the smallest provenance model
that preserves the distinctions already required by the simulation.

## In scope

- Refine or replace `SourceKind.Direct` with the minimum necessary provenance categories. The
  expected starting vocabulary is:
  - `Participant`;
  - `Witness`;
  - `Discovery`;
  - `FirstHandTestimony`;
  - existing `Inference`;
  - existing `Rumor`, which remains dormant unless an existing path already requires it.
- Audit every current claim-acquisition path and assign provenance from what actually occurred,
  not from confidence or narrative convenience.
- Update cognition, testimony, reporting, and replay state wherever provenance must be preserved.
- Update viewpoint-constrained intelligence wording so it can describe provenance accurately
  without exposing authoritative truth or inventing attendance.
- Preserve the separation between provenance and confidence.
- Add focused regression coverage for:
  - a participant knowing what they did or authorized;
  - a witness observing an event without automatically knowing hidden authorship;
  - discovery of a trace or consequence not implying physical attendance at the event;
  - first-hand testimony remaining testimony rather than becoming observation;
  - high confidence never implying witnessing;
  - reporting preserving the relevant acquisition method without upgrading it;
  - deterministic replay and pause/resume behavior after provenance becomes future-relevant state.
- Run the full verification contract and all current scenario variants.

The category names above may change during implementation if the existing acquisition paths reveal
a more precise minimal vocabulary. Any semantic change must be recorded here and surfaced for
review; do not silently collapse two acquisition modes back into one.

### Decided by Matt

**Provenance is allowed to change behavior.** Testimony behaves as testimony: a claim acquired by
being told is not protected the way a claim acquired by seeing or doing is. Baseline movement is
acceptable where it traces to a corrected acquisition category, per the section below; it is not
acceptable anywhere else.

**Authored participation is separate from being told that execution occurred.** These are two
different acquisitions and must not share a category. Vincent ordering a beating and Vincent hearing
from Tommy that the beating happened are not the same act of knowing: the first is his own decision
and is what he has to conceal, the second is an account he could disbelieve. Collapsing them would
either make a delegator's knowledge unshakeable or make his own order deniable to himself.

The practical consequence for the vocabulary is that `Participant` covers authorship and personal
involvement, while a delegator learning from his executor is `FirstHandTestimony` — currently
`Strategies.ResolveViolence`, which records both as `Direct`.

## Explicitly out of scope

- Generalized rumor propagation or rumor mutation.
- Evidence, prosecution, case-board, or full investigation systems.
- Broader surveillance-state implementation.
- Relationship-schema design or richer grievances.
- Rival organizations or expanded organization diplomacy.
- Relevance-tier promotion or demotion.
- SQLite persistence or save/load.
- Godot integration or UI work.
- New scenario content except the smallest fixture change required to test provenance distinctions.
- Retuning decision weights unrelated to provenance.
- Changing established milestone-003 behavior for any reason *other* than a corrected acquisition
  category. See "Behavior changes that provenance causes" below — this exclusion is about unrelated
  drift, not about the milestone's own consequences.

## Behavior changes that provenance causes

`SourceKind.Direct` is not only a label. It is read as a predicate in four places, and each one has
to be answered explicitly once the category is split:

- `Cognition.Learn` — direct acquisition overrides a prior record regardless of confidence;
- `Cognition.Receive` — contradiction erodes a directly-held belief by 0.15 rather than 0.45;
- `Cognition.Receive` — a directly-held belief keeps its stance below the acting threshold;
- `Salience.Perceive` — the `Suspicious` discount exempts directly-held claims.

Behavior changes that follow from assigning a claim its true acquisition category are **in scope and
expected**; this milestone cannot be delivered without them. What remains out of scope is behavior
drift with no provenance cause.

Each such change must be:

- traceable to a specific reassignment, stated as "this claim was X, is now Y, so this rule now
  applies to it differently";
- diffed against the milestone-003 baseline, with the differences in scenario output described
  rather than merely re-baselined;
- recorded in this file as it is discovered, and carried into the milestone archive at closeout.

An unexplained baseline movement is a defect. An explained one is the milestone working.

## Constraints and review hotspots

- Truth, observation, inference, testimony, rumor, and evidence remain distinct.
- Provenance describes acquisition method; confidence describes certainty. Neither may imply the
  other.
- Witnessing a consequence does not reveal who caused or authorized it.
- A report may transmit a claim, but transmission must not rewrite the original acquisition into
  personal observation by the recipient.
- Player-facing output may use only the viewpoint character's information and known identities.
- Decisions must continue to use perceived information rather than authoritative world truth.
- Existing distinctions among assertion, withholding, cap omission, repetition, reversal,
  recantation, contradiction, and unanswered requests must remain intact.
- Review provenance changes as state-machine and information-safety changes, not as an enum rename.
- Preserve deterministic ordering and include any new future-decision-relevant state in replay
  comparison.

## Success criteria

- No player-facing text invents participation, physical presence, witnessing, or first-hand access.
- A character cannot learn hidden authorship merely by witnessing a visible consequence.
- Participant, witness, discovery, and first-hand-testimony paths are distinguishable wherever the
  current scenario exercises them.
- Provenance and confidence remain independently represented and rendered.
- Existing milestone-003 reporting, recantation, withholding, request, contradiction, and bounded
  behavior remains intact in mechanism. Where a corrected acquisition category changes an outcome,
  the mechanism must still be the milestone-003 one and the change must be explained per "Behavior
  changes that provenance causes" — intact does not mean byte-identical output.
- New tests directly exercise production acquisition and rendering rules rather than duplicating
  their predicates in test code.
- Where practical, new regression tests are mutation-checked by temporarily restoring the defect
  and confirming the test fails.
- Full verification passes:

  ```powershell
  dotnet build CrimeEmpire.sln
  dotnet test CrimeEmpire.sln
  dotnet run --project src/CrimeEmpire.Runner -- --verify --seed 42 --days 90
  dotnet run --project src/CrimeEmpire.Runner -- --verify --variant disloyal-vincent --seed 42 --days 90
  dotnet run --project src/CrimeEmpire.Runner -- --compare --seed 42
  dotnet run --project src/CrimeEmpire.Runner -- --variant disloyal-vincent --viewpoint salvatore --seed 42 --days 90
  dotnet run --project src/CrimeEmpire.Runner -- --variant baseline --viewpoint vincent --seed 42 --days 90
  ```

- Claude self-reviews and commits the completed milestone as one coherent change, including
  `docs/milestones/004-provenance-precision.md`.
- Codex reviews that commit against the canonical documents and this milestone scope.
- After review and accepted corrections, `CURRENT_MILESTONE.md` is reset. No milestone 005 work
  begins automatically.

## Review disposition — Codex on `d142582`

All four findings accepted.

The two P2s — briefs stale in the commit that created them, and self-contradicting scope language —
are fixed above.

The two P1s were **initially rejected in error** and are now accepted. The rejection reasoned from
the milestone-003 archive's "no remaining findings" line and from the fact that the named defects
resembled the fourth correction's findings 12 and 13. Matt confirmed they are new downstream
findings against `b8fe921`: `b8fe921` scoped the request to a claim, but neither the reply nor the
already-asked guard was scoped with it. The archive line was itself the inaccuracy, and has been
withdrawn; the two defects are recorded as the fifth correction in
`docs/milestones/003-information-transmission.md` and fixed in the corrective commit.

The lesson worth keeping: the archive is authoritative for *what was recorded*, which is not the
same as authoritative for *what is true*. Where a reviewer's account of its own findings conflicts
with the record, the record can be the thing that is wrong, and resolving it needs Matt rather than
a closer reading of the file.

**Milestone 004 does not begin until that corrective commit is reviewed.**

## Deliberately carried forward

The following remain for later milestones:

- relationship-schema design, likely the next substantial design pass;
- relevance tiering and its continuous-calendar engineering risk;
- persistence and SQLite;
- Godot / `net10.0` compatibility;
- generalized rumor, evidence, prosecution, media, and public-information channels;
- broader organizations, diplomacy, careers, corruption, and surveillance systems;
- cleanup of stale `OPEN_CONCERNS.md` item 4 and the redundant test-project target framework,
  unless Matt separately authorizes a documentation/maintenance change.
