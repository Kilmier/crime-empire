# Criminal Empire — Open Concerns

Genuinely unresolved items only. Ranked roughly by severity. This file supersedes
`design-doc-concerns_1.md` — six of its ten original items turned out to already be resolved by
later revisions to the vision/architecture docs and are recorded (with citations) in
`DESIGN_DECISIONS.md` instead of repeated here. Don't re-add them without new evidence they've
regressed.

## Load-bearing

### 1. Tuning is 100% deferred, and tuning is where this design lives or dies
The architecture doc is a correct, well-reasoned map of structure — trigger types, tiering,
decision pipeline, anti-patterns to avoid — with almost no committed numbers (weights,
candidate-set sizes, promotion thresholds, trigger sparseness). That's the right call for a doc
at this stage, but it means neither doc is evidence the resulting behavior will feel believable.
Good architecture + bad weights still produces characters that feel insane or inert.
**Implication:** resolved by process, not content — the next artifact should be executable (the
pre-MVP kernel), not another written spec. Stays open until the kernel produces trace output that
can be judged.

### 2. Tiered continuous-calendar engineering cost is a bigger commitment than it reads as
The discrete-event-under-continuous-calendar model is the right technical answer, but combined
with 3-tier promotion/demotion, this is genuinely one of the harder parts of the project —
not because any single piece is exotic, but because bugs in event scheduling across tiers will be
non-deterministic and hard to reproduce. Budget real time for this specifically, separate from
"AI behavior" time. Not addressed by any doc revision; it's an engineering-effort risk, not a
design gap, so a document can't resolve it — only implementation experience can.

## Moderate

### 3. Character schema — `relationships` branch is still a shape, not a schema
`knowledge` got a real subsystem treatment via `INFORMATION_AND_LEGIBILITY.md`. `relationships`
did not receive the same treatment and is still just a bullet list (directional, trust/respect/
fear/affection/resentment/attraction/obligation as separate dimensions) with no data-model
answer for how those dimensions are stored, updated, or decayed. Should get the same kind of
dedicated document `INFORMATION_AND_LEGIBILITY.md` gave to knowledge, once the kernel shows which
distinctions actually change decisions.

**Still open. Updated 2026-08-14 with milestone 006's evidence** — which is the "once the kernel
shows which distinctions actually change decisions" this item has always been conditioned on.
Storage and update now have an answer for one dimension; decay, the remaining dimensions, and the
document itself do not.

What the kernel has now shown:

- **Trust moves, and the movement reaches a decision.** A perceived account conflict costs the
  listener trust in the speaker. Because `Utility.Loyalty` derives from trust and retaliation risk is
  priced at `-(1.3 + 2.2 * loyalty)`, contradicting a man measurably lowers what it costs him to move
  against you. That connection was not written; it fell out of one edge feeding a derived value.
- **`Affection` was purposeless and is gone.** Declared from the first commit, never read or written
  anywhere. A dimension list should be closed the way the trait vocabulary was closed.
- **Absence of trust and distrust are the same state**, because the range is `[0,1]`. A stranger who
  contradicts you is indistinguishable from a stranger. Whether that distinction earns its cost is a
  real schema question and the clearest one this milestone surfaced.
- **Grievances belong on the relationship**, not beside it: `AgainstId` was always a relationship key.
- **The centralized API is worth having before the schema is settled.** One place that can change
  relationship state made the read-creates hazard visible and made the conflict edge a three-line
  change at each of three call sites rather than a convention.

**Updated again 2026-08-15 with milestone 007's evidence, and still not retired.** 006 showed trust
moves; 007 shows how far that movement carries, which is the more useful number and the less
flattering one.

- **The movement reaches a score, and is roughly a fortieth of a decision margin.** A capo
  contradicted twice by his boss goes from 0.45 trust to 0.031, and the `relationship effects`
  component on his next report to that boss moves from 0.0440 to 0.0063. Decision-relevant, and
  nowhere near choice-changing. `Utility.Loyalty` weights trust at 0.45 and subtracts half of any
  grievance, so a standing grievance of 0.35 absorbs most of a full trust collapse before it reaches
  a score at all. Whether that is the right shape is a schema question, not a tuning one.
- **Which relationship a mechanism lands on decides whether it matters at all.** 006's conflict fell
  on a character with no relationship-reading decisions and changed nothing; the identical mechanism
  landing on one who reports upward is measurable. A schema that says how dimensions are stored and
  updated without saying which decisions read them will not predict this.

What is still unanswered, and is what a document would have to settle: decay and its rate; whether
respect, resentment and attraction are separate dimensions or derived; whether provenance should
weight the social consequence (milestone 006 deliberately used one rule and preserved the provenance
so this can be decided on evidence); negative trust; and whether loyalty's weights leave trust enough
room to matter once a grievance exists.

**Narrowed 2026-08-16 by milestone 008, and not retired.** The document this item asked for exists as
`docs/RELATIONSHIPS.md`, written from measured results rather than ahead of them. What it settles:

- **The vocabulary is closed** — Trust, Fear, Obligation, relationship-keyed Grievances — with every
  dimension required to name a decision that reads it, asserted by a test.
- **Storage, update paths and readers are documented per dimension**, which is the half milestone 007
  said a schema would be useless without.
- **"Whether loyalty's weights leave trust enough room once a grievance exists" is answered, and the
  answer was the clamp rather than the weights.** Grievance was subtracted inside
  `clamp(…, 0, 1)`, so a character whose grievance exceeded his bond floored at zero and further
  trust was worth nothing. It now sits outside the clamp as its own named contribution, at the same
  `0.50`. No coefficient was tuned.
- **The premise behind the question was too narrow.** Removing relationship state changes which
  candidate wins at 1–3 decisions in every variant. The channel is not weak; the trust-to-partial-report
  path is, because two loyalty reads on that candidate nearly annihilate. Fear reaches 1.44 on a
  decision whose margin is 0.25.

What remains open here, each now with a stated condition for return rather than an open question:
**decay and its rate** (needs a calendar/tier timescale); **negative trust** (needs a decision that
reads distrust differently from indifference); **whether respect and resentment are separate
dimensions or derived** (needs a reader); **whether provenance should weight the social consequence**
(unchanged since 006); and **whether `GrievanceWeight` should be capped** — considered as milestone
008's remedy and explicitly rejected in favour of unbundling, so it is open rather than answered.

**Updated 2026-08-23 with milestone 016's evidence, and still not retired.** Trust could previously
only fall; it can now rise, through the mirror-image mechanism (`AccountAgreement`/
`Relations.RecordAccountAgreement`) reusing `Cognition.Receive`'s existing fresh-agreement branch. The
same finding milestone 008 recorded for the downward direction holds for the upward one, demonstrated
rather than assumed: Tommy's trust in Salvatore, raised by one fresh corroboration on 7 April, measures
as a real but small change in the `relationship effects` component of his 8 April report decision
(`+0.0945` to `+0.1031`) — decision-relevant, not choice-changing on this seed. None of the items listed
above as still open are resolved by this: decay, negative trust, dimension separateness, provenance
weighting, and the `GrievanceWeight` cap are all unaffected and unchanged.

- **Milestone 026 read the standing history to tell the two apart on the roster, without resolving
  this.** Putting men on the roster the player has only read a face off — a shopkeeper he threatened,
  a man he lied to — meant a zero-trust relationship with no history rendered as "you would not take
  his word on anything", which is distrust, when nothing had ever moved it. `PlayerNarration.Standing`
  now says "you have had no dealings with him to go on" when the standing history is empty. A
  presentation-level distinction drawn from state the model does keep (whether anything ever moved),
  not a change to the range; the concern stands.

### 4. ~~Trait/value vocabulary must be closed, but the concrete list still isn't committed~~
**Retired 2026-08-14.** Milestone 001 closed the list in `Domain/Psychology.cs` and it is now
recorded in `DESIGN_DECISIONS.md` under "Actor parity and simulation tractability" — traits
Aggressive/Cautious/Proud/Suspicious, drives Wealth/Status/Security/Belonging, with Loyalty derived
per relationship and Ambition folded into Status. The number is kept rather than reused: the
milestone archives are append-only and cite this item as #4.

### 5. Conflict by omission is not recognized as conflict
`IsContested` requires an actual denial. In the baseline both Vincent and Tommy omit rather than
deny, so their accounts differ without formally conflicting. Whether one source's assertion against
another source's conspicuous omission deserves first-class treatment is a real design question, not
an oversight — a boss who notices that two men's stories cover different ground is doing something
the model currently cannot represent. Surfaced by `milestones/003-information-transmission.md`.

### 6. `Rng.ForDecision` has the same GF(2)-linear finalizer shape `ForOccasion` was fixed for
Corrected 2026-09-09 (`docs/milestones/022-the-street-talks.md`'s second correction) after Codex's
review found the first write-up of this item overstated the argument. `ForOccasion`'s old finalizer
was a single linear step, `h ^= h >> 15`. `Fnv1a` itself is not GF(2)-linear — it multiplies — but the
argument does not need it to be: it only needs to be a *fixed* function of the key. Everything applied
to that fixed value afterward — XOR-combining it with the world seed, that finalizer, and every
subsequent `NextUInt` xorshift draw — is linear over GF(2), and linearity is what lets the seed cancel
out of two keys' XOR difference at every corresponding draw position, leaving a delta that depends
only on the two keys, never the seed. That relationship is what made the *demonstrated* case — three
street-talk observers of one event — unable to co-succeed across tens of thousands of searched seeds;
it is not a proof that every arbitrary pair of keys under this shape was universally unable to
co-succeed at every seed, and this item does not claim that. `ForDecision` combines its inputs
(`characterId`, `worldSeed`, `decisionIndex`) the identical way and finalizes with the identical
single linear step, so it carries the identical *structural risk* — the same seed-cancelling relationship
would arise between any two `ForDecision` streams sharing a seed — but no concrete pair of decisions
has been shown to actually fail to co-vary the way milestone 022's three observers were shown to.
It seeds `Utility.Score`'s per-decision noise, so if the risk is ever realized, two characters' (or two
decisions') noise draws would not be free to co-vary the way a genuine independent draw would be.

**Why this correction did not fix it.** Authorization was explicit and narrow: replace
`ForOccasion`'s finalizer only, and record `ForDecision`'s identical shape as deferred rather than
fold a second RNG change into one correction. `ForDecision` has no known concrete symptom the way
`ForOccasion` did (the milestone 022 joint-observation exclusion, found by exhaustive seed search) —
this is a structural risk carrying the same shape as a demonstrated defect elsewhere, not itself a
demonstrated behavioral defect, which is exactly why it belongs here rather than in a second
correction commit.

**What would surface it.** Two characters' (or two decisions') scored candidates that should be able
to win together under some seed, provably never doing so across a wide seed search — the same shape
of falsifier that found the `ForOccasion` defect. Nothing in the current test suite specifically
searches for this; it would most likely surface the same way `ForOccasion`'s did, as an unexplained
non-result recorded honestly in a milestone archive before anyone connected it to the RNG.
**Fix, if this is ever prioritized:** the identical fmix32 finalizer swap `ForOccasion` now uses —
see `Rng.cs`'s doc comment on `ForOccasion` for the full algebraic argument.

### 7. A logically mooted question can remain displayed forever
`DESIGN_DECISIONS.md`'s settled rule is exact and correct: `AwaitingAnswers` resolves a request only
from testimony by the asked person, of exactly the asked claim
(`t.SenderId == r.AskedId && t.Claim.Equals(r.About) && t.At >= r.At`). Found 2026-09-11 while tracing
a test failure during milestone 024's sixth correction: a request can become moot — its real-world subject
resolved by events, and reported on by the asked person — without ever being answered under this
rule, if the report he eventually sends carries a different claim than the one he was asked about.

Concretely: Salvatore asks Vincent whether Bellini's grocery is refusing tribute
(`BusinessRefusesTribute`). By the time Vincent next reports to anyone, the grocery has been brought
to heel and pays — his report asserts `TributeCollected`, `PolicyIssued`, `TargetIsVulnerable`, never
`BusinessRefusesTribute` itself, because there is no longer any reason for him to independently
re-litigate a question events have already settled. Salvatore's original request, checked against the
exact-claim rule above, stays in `AwaitingAnswers` for the rest of the run — confirmed directly,
extended to a full 90 days with no resolution. Vincent has not gone silent, has not declined to
answer, and has in fact told the truth about the very situation the question was about; the request
simply never matches the narrow claim it was filed against.

**This is correctly out of scope for the correction that found it.** Milestone 018's own three-times-
corrected chain never considered "the subject moved on" at all — what it tried and rejected was
narrower: reading the asked character's own `World.Decisions` to distinguish silence from a genuine
decline (rejected as a private-state leak the asker never receives any message establishing), and
separately, treating a sincere contradicting account as a distinct "Declined" outcome rather than a
real answer (rejected because a sincere denial is an answer, not a refusal). Neither alternative is
"resolve when a different, superseding claim moots the question," which is a genuinely new case this
correction's own tracing surfaced, not one `DESIGN_DECISIONS.md`'s settled rule was ever asked to
weigh. Whether a stale, logically-mooted request deserves its own resolution path — and what would
have to recognize "mooted" without reintroducing the leaks the exact-claim rule was built to close —
is a real, undecided design question, not an oversight, and needs its own ruling rather than a fix
folded into whatever correction next happens to trip over it.

**Update, 2026-09-12 (milestone 024's eighth correction, `762210f`).** The concrete illustration
above no longer reproduces, though the general question it illustrates is still open. Fixing a
duplicate-payment defect the same playtest surfaced required raising the owner's and executor's
collection-time belief writes from confidence 0.9 to 1.0 (`Strategies.cs`'s collection branch),
which now clears `Cognition.Learn`'s override threshold against Vincent's prior 1.0-confidence
belief that the grocery refuses. One consequence: Vincent's own later, fully autonomous report now
genuinely asserts `BusinessRefusesTribute` in its resolved (rejected) direction — not merely
`TributeCollected` — so Salvatore's original request now matches the exact-claim rule and resolves
instead of sitting in `AwaitingAnswers` for the rest of the run. Confirmed directly:
`CausalFeedbackTests.cs`'s `The_autonomous_report_of_the_resolved_refusal_answers_the_original_request`
now asserts the request is answered for this exact scenario. This closes the one concrete case
recorded above as a side effect of an unrelated fix, not by design — it does not settle whether a
stale, logically-mooted request should resolve on some other basis when no future report ever
happens to restate its literal claim. That remains open, and this correction never considered it.

### 8. Operation duration does not yet match the apparent scale of the action

Matt's 2026-09-20 Milestone-030 playtest made a pacing mismatch concrete: choosing to persuade or
threaten a shopkeeper reads like an hours-or-one-day encounter, while the current operation advances
through multi-day scheduled steps and can present several days of apparent inactivity before a
visible refusal or result. The implementation currently bundles approach, conversation, pressure,
response and collection into a mission-sized sequence, but the option names only the immediate
social act. That makes elapsed time hard to interpret even when the scheduler is behaving exactly as
implemented.

This is not resolved by making progress omniscient, nor by shortening a timer inside a presentation
correction. It needs a design ruling on whether the operation represents one encounter, a campaign of
repeated contacts, or explicit travel/preparation/conversation/collection phases; then the calendar,
player wording and consequence cadence can be evaluated together. Until then, the current timings
are prototype timings rather than a durable pacing decision.

**Milestone 031 owner ruling:** SecureTribute represents a multi-phase pressure operation. A static
qualitative expectation will state its multi-day scale; it must never derive an ETA from hidden
execution state. This settles the meaning, not whether the prototype pacing feels right. The human
comprehension and pacing evidence remains pending; timings are unchanged.

### 9. Initial delegation is unavailable, and a busy subordinate disappears without explanation

Matt's 2026-09-20 Milestone-030 playtest exposed two connected management-flow gaps. A free Tommy
cannot be assigned either known tribute job at the opening: Vincent must personally start an
operation, wait for a later reconsideration, and only then hand the existing operation over. Once
Tommy holds one job, delegation on the other disappears without saying that he is already occupied.
The one-operation-per-executor rule is intentional; the start-then-handoff ceremony and silent
unavailability are not established design decisions.

Direct assignment at the opening would change the candidate model, scheduling and actor-parity
surface rather than merely rewording a panel. It needs a ruling on whether the initial choice names
target, method and executor together, how that stays bounded when several jobs and subordinates are
known, and how NPCs receive the same capability. Independently, the interface needs a principled way
to explain relevant unavailable actions without turning rejected candidates or hidden staffing
state into an omniscient menu. Neither change belonged in Milestone 030's bounded presentation
correction.

**Milestone 031 response:** Matt authorized direct commissioning through a bounded second executor
stage and a paused replayable draft, with known staffing explanations and present NPC reachability.
See `DESIGN_DECISIONS.md`, "Direct commissioning", for the rulings. Implementation evidence is in
`milestones/031-commission-the-job.md`; independent review and the human comprehension gate remain
outstanding. This concern is not recorded as closed by implementer test results.
