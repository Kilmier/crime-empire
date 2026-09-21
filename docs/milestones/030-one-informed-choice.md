# Milestone 030 — One informed choice

## Authorization and scope

Matt authorized revision 2 of the One Informed Choice proposal on 2026-09-19 after Astra returned
**AUTHORIZE** and confirmed its earlier P2 findings were resolved. The active authorization was
recorded in `CURRENT_MILESTONE.md`; the proposal remained noncanonical evidence.

The milestone's central claim was deliberately narrow: when an assignment issuer discloses that a
business is refusing tribute, the issuer's held affirmative vulnerability assessment of that same
business should be captured beside the refusal at issuance and delivered only to the named recipient
through ordinary information channels. The account had to retain stance, confidence, claimed basis
and order; delayed delivery could not reread the issuer's later mind. The received assessment had to
affect the real decision pipeline while remaining qualitative and source-limited at the player
boundary.

The natural seed-42 opening also had to remain one genuine choice: both known jobs, all three methods
for each, Salvatore's uncertain Grocery assessment, Vincent's firsthand Tailor refusal, and the
standing no-public-violence rule, without hidden resistance, raw confidence, utility, success
percentages or a recommendation. Tuning, new consequences, new screens, fixture/RNG changes and
general UI architecture were excluded.

## Implemented behavior

### Captured assignment information

- Assignment issuance pairs each disclosed `BusinessRefusesTribute(Target)` account with the
  issuer's currently held affirmative `TargetIsVulnerable(Target)` account, if one exists. The
  reported stance, confidence and claimed basis are copied then, in disclosure order.
- Delivery validates `Assignment.RecipientId` and fails closed when the scheduled event is addressed
  to another actor. A valid recipient receives the captured account through `Cognition.Receive`, so
  testimony, provenance, agreement/conflict and trust consequences remain ordinary behavior rather
  than an assignment-only information path.
- A newly received relevant assessment schedules the existing supervisory review; repeated or
  unrelated information does not. Expected reward and the existing corroboration subject read the
  received assessment through the same perceived-information path as autonomous decisions.
- Controlled and autonomous openings retain the same generation, grouping, salience, feasibility,
  scores, concrete option identities and commit path. Persistence reconstructs the pre-delivery,
  pending-opening and consequence-branch states from public inputs.

At baseline seed 42, Salvatore's `Suspects` / `0.45` / `Inference` Grocery assessment is captured at
issuance. Six hours later Vincent holds the normal discounted `0.41625` `Report` record and testimony
naming Salvatore while the claimed basis remains `Inference`. Those numeric values are test and
developer evidence only; the live interface says that Salvatore suspects the Grocery would fold and
that Vincent is not certain.

### Bounded presentation correction

Matt's first neutral playtest did not pass the comprehension gate, so he explicitly authorized a
bounded correction on the existing surfaces:

- The assignment focus now connects Ferri's Tailor directly to the harbour shortfall while keeping
  sources separate: Salvatore named Bellini's Grocery; Vincent already knows Ferri's Tailor is not
  paying. The Tailor sentence is derived only from concrete tribute-operation targets already in
  Vincent's visible options, not from world truth or unrelated cognition.
- Each operation retains the owner's last target/method order. After delegation, the owner sees the
  delegate and that standing instruction in one sentence, for example “Tommy Nardo is handling it
  under your standing order: persuade Bellini's grocery to pay.” The executor sees his own current
  method. A private delegate-side method change and all private progress remain absent from the
  owner's snapshot.
- Each actual collection writes an owner-private cash receipt atomically with the cash change. The
  existing activity panel itemizes date, source business, amount and known executor. This reads the
  owner's own receipt history, never payer cash or the truth log.

The final measured public-input path displayed `+620` from Ferri's Tailor and `+840` from Bellini's
Grocery, totaling `7,460`, with the correct executor attached to each route.

## Human playtest

Matt played baseline seed 42 as Vincent on 2026-09-20.

The first run established that the simulation worked but the presentation did not yet establish the
claim. Matt understood that Bellini's Grocery required attention but not why Ferri's Tailor was
offered; could see that Tommy handled delegated work but not reconstruct the method he had ordered;
and noticed total cash rise without itemized attribution. Those findings triggered the explicit
presentation correction above rather than being recorded as a pass.

Focused replays then drove three wording iterations:

1. Matt confirmed the activity panel clearly attributed each amount to its business and executor.
2. Separate “standing order” and “Tommy is handling it” lines still failed. Joining them into one
   sentence made the method assigned to Tommy clear; Matt explicitly confirmed that it answered the
   question.
3. A generic sentence about any harbour business still did not directly explain Ferri. Naming Ferri
   as another part of the shortfall because Vincent already knew it was withholding tribute landed;
   Matt explicitly confirmed the revised opening.

The neutral human-comprehension gate therefore passed. This is evidence about the authorized slice,
not a claim that the prototype is already a complete game.

## Verification — implementer evidence, not independent review

Final verification after the last runtime change:

- `dotnet build CrimeEmpire.sln --no-restore`: **0 warnings, 0 errors**.
- `dotnet test CrimeEmpire.sln --no-build --no-restore`: **780 passed, 0 failed, 0 skipped**.
- Every seed-42 90-day variant repeated byte-identically:
  - baseline `D6F9F0DAC0B308DA`
  - cautious-Vincent `810FCB7835E4E95C`
  - watchful-boss `C7F693E2BF222FAD`
  - disloyal-Vincent `4CF63C356514998F`
  - resentful-Tommy `95CA45A72333EFEA`
  - capable-Angelo `CF75E6FB50BABC58`
- `--compare --seed 42` reported **six distinct traces and five distinct chosen-action sequences**.
- The required Salvatore/disloyal-Vincent and Vincent/baseline viewpoint runs exited zero.
- All nine individual Godot headless flags passed, including the live informed-opening proof,
  operation owner/executor boundary and itemized-income golden path. The isolated restart-save and
  restart-load pair passed in two fresh processes, for eleven Godot checks total.
- `git diff --check` passed before commit.

Load-bearing mutations were made and reverted:

- bypassing assignment-receipt supervisory review removed the scheduled review and failed;
- removing paired vulnerability capture failed exact payload and live-reader proofs;
- removing owner-order capture failed on `Threaten` versus null;
- hiding receipt recording left aggregate cash correct but failed the `620` / `840` itemization;
- removing the briefing relationship failed the player-surface proof; and
- replacing the visible relevant-target derivation with an empty list failed both briefing tests.

The restored tree passed the complete gate above. This is implementer preflight, not independent
acceptance.

## Important discoveries and deferred work

- Persuade/threaten currently reads like an hours-or-one-day encounter but advances as a multi-day
  mission sequence. `OPEN_CONCERNS.md` records the need to decide whether the operation represents
  one meeting, a pressure campaign or explicit phases before changing calendar timing.
- A free subordinate cannot be assigned directly at the opening. Vincent must start a job and later
  hand it over; once Tommy is busy, the other job's delegation option disappears without explanation.
  Direct start-with-delegate candidates would alter candidate construction, scheduling, bounded
  choice and actor parity, so this is recorded in `OPEN_CONCERNS.md` for a later ruling rather than
  folded into presentation work.
- After the two tribute jobs resolve, the current slice has no further operational content beyond
  reporting/social choices and the scenario clock. The milestone proves an informed decision and its
  consequences, not a complete campaign loop.
- Existing deferred tuning, relationship-schema, omission-conflict, RNG and stale-question concerns
  remain unresolved.

## Commit and review handoff

Implementation commit: the focused commit containing this archive, directly following `d908d8e`;
locate its exact hash with:

```powershell
git log --diff-filter=A --format=%H -- docs/milestones/030-one-informed-choice.md
```

The commit is a Class A implementation/presentation/persistence change. Read this archive and the
canonical documents named by `AGENTS.md`, inspect the exact diff, and independently reproduce the
relevant verification. No independent review or owner acceptance is claimed here. Stop for review;
do not begin Milestone 031.
