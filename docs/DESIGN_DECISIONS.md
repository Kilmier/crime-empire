# Criminal Empire — Design Decisions

Settled decisions. Do not re-litigate without a concrete reason surfaced during actual
implementation. Unresolved items live in `OPEN_CONCERNS.md`. Active work lives in
`CURRENT_MILESTONE.md`; unbuilt work and candidate scopes live in `ROADMAP.md`.

This file supersedes the "Decision history" section of `PROJECT_CONTEXT.md` and the resolved
portions of the retired `design-doc-concerns_1.md`. Each entry below cites the doc/section that
settled it, so a future session can verify the claim instead of taking it on faith.

## Project shape

- **Genre/scope**: persistent-world, character-driven criminal empire sim. Player freedom, no
  mandatory path, no universal win condition. Relationship graph + character decision-making
  *are* the game, not a management sim with NPCs bolted on. — `GAME_VISION.md`,
  High Concept / Pillar 1.
- **Scope discipline is a standing constraint**, not a one-time correction. Original ambition
  (20-city state, full laundering/politician/lawyer depth, procedural heist maps day one) was
  walked back to v2+/long-term vision. Default to the smaller, faster-to-test version whenever
  scope is ambiguous. — `PROJECT_CONTEXT.md`, Decision history.

## Succession and persistence

- On player-character death/incapacitation: if a viable heir or sufficiently loyal capo exists,
  control transitions to them. The new POV character has **independent stats** — they inherit
  territory/relationships/standing (CK3 model), not the predecessor's personal skill sheet. This
  is now written into the vision doc itself, not just conversation history. —
  `GAME_VISION.md`, "Succession and Continuity."
- If no viable successor: game over for that dynasty, but the world persists; a new character can
  start in the same city, in the visible legacy/ruins of the old empire. Flagged as the project's
  strongest differentiator — cheap to fake with flavor text/news/dialogue, high narrative payoff.
  — `PROJECT_CONTEXT.md`, Decision history.

## Heists

- MVP resolution is abstract-roll (crew skill, prep/intel, guard density, stealth vs. aggression,
  risk modifier → outcome table), with named variables so a later procedural mini-map can be a
  presentation layer over proven logic rather than a parallel system to debug. Greed/time/risk
  push-your-luck tension during an unfolding op is a deliberate design goal. —
  `GAME_VISION.md`, "Heists"; `PROJECT_CONTEXT.md`, Decision history.

## Actor parity and simulation tractability

- Actor parity means **causal parity, not computational parity**: comparable actions follow
  comparable requirements/costs/consequences regardless of actor, but NPCs do not get the
  player's full interface, planning depth, or update frequency. This is the resolution to what
  was flagged as the single most expensive commitment in the vision doc if taken literally. —
  `SIMULATION_ARCHITECTURE.md`, "Causal Parity, Not Computational Parity."
- Rejected on purpose, not by oversight: unrestricted GOAP/general planning, continuous
  deliberation for all characters, identical AI/player interfaces, minute-resolution updates
  during fast-forward. Do not reintroduce without a concrete demonstrated need from
  implementation. — `SIMULATION_ARCHITECTURE.md`, "Why the MVP Does Not Use Unrestricted
  Planning" / "MVP Architecture Commitments."
- Traits/personality modify perception, salience, and evaluation — **never fire actions
  directly**. Rejected pattern: `Aggressive → monthly chance to attack`. Treated as the single
  most important anti-pattern in the architecture; violating it is the fastest way to make
  characters feel like slot machines. — `SIMULATION_ARCHITECTURE.md`, "Traits and Causality."
- Simulation depth is relevance-tiered (Active / Supporting / Background) with a strict Tier 1
  population budget and explicit promotion/demotion rules that preserve causally important state.
  — `SIMULATION_ARCHITECTURE.md`, "Simulation Relevance Tiers."
- **The trait and drive vocabulary is closed, and this is the list.** Traits: `Aggressive`,
  `Cautious`, `Proud`, `Suspicious`. Drives: `Wealth`, `Status`, `Security`, `Belonging`. Every
  entry has a stated behavioural purpose; a trait that cannot name one does not belong, and two
  that move the same numbers should be merged. Two omissions are deliberate: **`Loyalty`** is
  derived per relationship from trust, obligation and Belonging rather than stored as a universal
  scalar, because one loyalty number collapses "loyal to whom, and for which reason"; and
  **`Ambition`** is what a Status weight already is, so a separate advancement drive would be a
  tuning finding, not a default. — `Domain/Psychology.cs`, closed in milestone 001; this retires
  `OPEN_CONCERNS.md` #4.

## Information channel — settled invariants

Settled by milestone 003 and refined by 004; see `milestones/003-information-transmission.md` and
`milestones/004-provenance-precision.md` for the findings behind each. These are the contract the
report channel is reviewed against — `REVIEW_LEDGER.md` cites this section rather than restating it.

- **Deception is a candidate evaluated through the normal decision pipeline**, not a scripted
  branch. No code branches on a trait to produce a lie.
- **A report is composed only from positions the sender actually has.** Reporting code cannot read
  authoritative truth to invent content or to make the sender accurate.
- **A partial report distinguishes claims asserted, deliberately withheld, and omitted only because
  the bounded message was full.** Withholding settles a claim until the sender's own position moves;
  cap-omission leaves it outstanding.
- **Repeated identical accounts do not compound confidence — unless the listener independently
  reconsidered that claim after the speaker's preceding account.** After such intervening movement
  the repeated words may create **one** new conflict; further identical repetitions are inert again.
  Refined by milestone 007, which found the guard keyed on the speaker's words alone: a listener who
  had since disproved the claim for himself could be told it again and it counted as nothing, so a
  boss re-issuing a briefing after his capo had watched the shop start paying registered as somebody
  clearing his throat. Both halves are structural rather than second rules — the comparison is against
  the speaker's *latest* account, which set the reconsideration stamp itself, and the conflict branch
  re-stamps the record so the next identical account finds nothing new.
- **A source changing their account is meaningful**: recantation or contradiction updates
  reconsideration and remains communicable onward.
- **Corroboration counts distinct sources across the whole testimony history**, not the record's
  original attribution.
- **A request is scoped to a particular claim.** Asking a person one question does not permanently
  close the communication channel with them.
- **Asking is spent when the question is put**, not when the recipient chooses to answer. This
  bounds unanswered requests without forcing a reply.
- **A speaker's claimed basis is separate from what he privately holds.** Only the claimed basis may
  reach the listener; the actual basis is developer truth. Repeating someone's testimony makes it
  hearsay — a chain cannot launder itself back into first-hand at each hop.

- **Concealment is worth only the protection a report newly buys.** Settled by milestone 007; see
  `milestones/007-scenario-reach.md`. What a recipient has already been given about a claim is a fact
  about *messages*, recorded per `(sender, recipient, claim)` as never addressed / withheld /
  disclosed affirmatively / denied, and read from the asserted stance rather than from
  `Report.Candor` — a candid rejection and a deceptive denial both put the denying stance in front of
  him. The most recent treatment counts, so denial is not absorbing. Withholding again what he has
  already withheld buys nothing; escalating from silence to a denial still buys the difference;
  denying again what he has already denied buys nothing. Protection is completed per claim before the
  maximum is taken, never as separate maxima added. **Report eligibility is a separate question** —
  `NeedsConveying` may re-arm a report when the sender's own position moves, and that must not refund
  protection he has already spent.

The player-facing view is constrained to match: it reads only the viewpoint character's cognition,
testimony and known relationships; never enumerates the authoritative roster to reveal unknown
people; uses qualitative confidence; presents conflicting accounts with attribution; and does not
expose utility scores, hidden intentions, or the authoritative truth log.

**Amended by milestone 014, ruling 1: a snapshot may also expose the viewpoint character's own
private state directly, not only what he holds as a belief.** `PlayerSnapshot.Cash`, copied from his
own `Capabilities.Cash`, is the first instance — a character always knows his own balance without
needing a `Cognition` record to stand in for that self-knowledge, so this is not a widening of what
counts as "known" so much as a second, narrower category alongside it. The full rule, stated
precisely: a snapshot may expose the viewpoint character's own private state, cognition, and
legitimately known information; it must never expose **another** character's private state, world
truth, a utility score, or any reference or path back to mutable simulation state — every such field
is a value copied once at construction, never a handle onto the object it came from. See
`milestones/014-one-complete-player-owned-operation.md` and its correction.

## Relationships — settled by milestone 006

See `milestones/006-relational-consequence.md`. What is settled is the conflict rule and the shape of
the API around it; the relationship *schema* is explicitly not settled and stays open as
`OPEN_CONCERNS.md` #3.

- **The trigger is a perceived account conflict, never a detected lie.** Somebody asserting the
  opposite of a position a character holds is a conflict, and deception, sincere disagreement, faulty
  memory and a false prior belief all produce the identical shape. Nothing in the relationship path
  may consult `World.TruthLog`, `World.Reports`, `ReportedClaim.ActualBasis` or `Report.Candor`; the
  conflict record is assembled entirely from the listener's side, so the distinction is unavailable
  rather than merely unused.
- **The consequence is directional and is trust alone.** The listener's relationship toward the
  speaker moves. The speaker's does not, unless he separately observes a response. No grievance is
  raised — a conflict is not evidence of a wrong.
- **One rule regardless of the prior's provenance.** Contradicting what a man saw and contradicting
  what he was told cost the same socially, because `Cognition` already charges the epistemic
  difference through erosion rates and stance protection. The provenance is preserved on the conflict
  record so a later evidence-led pass can weight on it without reconstructing what was dropped.
- **A repeat is not a fresh conflict.** Emitted from the branch that sets `Contested`, which sits
  after the verbatim-repeat guard, so non-repetition is inherited from the same check that stops
  repeated denials compounding confidence loss.
- **All receipt paths apply it** — the report channel, delegation briefings, and assignment
  briefings. Being the man who issued the assignment does not make contradicting somebody free.
- **`Domain/Relations.cs` is the only code that can change relationship state**, enforced by the
  concrete type being private to it rather than by convention. Reads never create. Grievances live on
  the relationship.
- **The trait-vocabulary rule applies to relationship dimensions too**: `Affection` was removed for
  having no stated behavioural purpose rather than given one to justify keeping it.

## Relationships — the reader side, settled by milestone 008

See `docs/RELATIONSHIPS.md`, which is the prototype schema document `OPEN_CONCERNS.md` #3 asked for,
and `milestones/008-relationship-readers.md` for the measurements behind each entry. Milestone 008
changed **no coefficient**; everything here is about shape.

- **The executable relationship vocabulary is closed, and this is the list.** `Trust`, `Fear`,
  `Obligation`, and relationship-keyed `Grievances`. Each is retained only because a decision reads
  it, asserted by a test across all five variants rather than argued from the call sites. No
  speculative dimension is admissible on the strength of sounding like something people have —
  respect, resentment and attraction are out until a reader exists. This is the same rule that closed
  the trait vocabulary in milestone 001 and removed `Affection` in 006. — `Domain/Relations.cs`,
  `docs/RELATIONSHIPS.md`.
- **Grievance is outside the clamped loyalty sum.** `Loyalty.Value` is
  `clamp(0.45·Trust + 0.30·Obligation + 0.25·Belonging, 0, 1)`; grievance is applied by each reader as
  its own named component at `−0.50 × weight × that reader's coefficient`. Inside the clamp, a
  character whose grievance exceeded his bond floored at zero, so further grievance was free and
  further trust was worthless — a bitter subordinate and an indifferent one scored identically. The
  `0.50` coefficient is preserved exactly and remains provisional tuning. A cap on grievance was
  considered as the alternative remedy and **explicitly rejected**, so whether one is wanted is open,
  not answered.
- **Loyalty is derived, and each of its four contributions is emitted as its own score component
  carrying exactly one facet.** Trust, obligation, Belonging and grievance stay separately inspectable
  all the way through the scoring path — separately *computed* is not enough, and the first
  implementation fused three of them under a `Trust | Obligation | Belonging` union flag, which Codex
  found. `Belonging` is a drive, not a relationship dimension: it is listed in the diagnostic for
  completeness and excluded from gross, net and the counterfactual, because a man with no
  relationships still has a need to belong.
- **The bond is an unclamped sum, and every input to it is clamped at the point it enters its type.**
  The three weights total exactly `1.0`; `Relations` clamps trust and obligation on every write, and
  `Psychology`'s constructor clamps traits and drives, with both `With` overloads delegating to it. So
  the sum is always in `[0,1]` and a clamp on the bond cannot bind. It had to go because a clamp that
  binds cannot be split: there is no honest way to apportion a clamped total among its parts.
  **`Psychology`'s half of that guarantee did not exist when the clamp was first removed** — the range
  was stated on its indexers and enforced nowhere, so the public API admitted values that changed
  behaviour across the removal. A prose range is not an invariant; enforce it where the value enters.
- **Range enforcement clamps rather than throws**, matching `Relations`. Grievance is the deliberate
  exception: it is outside the bond, unbounded, and must stay able to exceed it.
- **A reader whose loyalty term is affine emits its constant separately, with no facet.** Retaliation
  risk and policy reluctance both read `−(a + b × loyalty) × …`; the `a` is not relational, because
  moving on anybody is a serious step and a rule weighs something whoever set it.
- **A report carries two distinct relationship considerations, and both are kept.** The standing
  reporting buys (`+0.7 × loyalty`) and the relationship cost of the candour selected (`+0.8` candid,
  `−0.5` partial, `−1.4` false) are not one effect. On a partial report they net to `0.2 × loyalty`,
  which is why a full collapse of trust was worth 0.0377 there. Merging them is forbidden: a mutation
  that preserves the net exactly is caught only by the test that asserts the distinction.
- **A score component records the facet it was derived from, set where the value is computed.**
  Aggregating by component name is forbidden and was measured to be wrong: of 168 components named
  `relationship effects` across the five variants, 61 — 36% — read no relationship state at all
  (`SeekCorroboration`'s "going behind X" is `−0.45 × proud`). A label is not a derivation.
- **The relationship diagnostic reports gross and net with no cutoff, and is developer-facing.**
  `Significant()`'s 0.15 threshold is right for a human-readable reason list and wrong for a
  measurement: on the decision milestone 007's finding was taken from, both halves of the report pair
  fall under it. A cutoff that hides a cancelling pair hides exactly the cancellation. The
  counterfactual reuses the breakdown's own noise draw rather than re-scoring, because a fresh ±0.05
  draw is larger than the effect being measured.
- **Negative trust and decay are deferred, not retired**, each with the condition that brings it
  back: a decision that reads distrust differently from indifference, and a calendar/tier timescale to
  decay against. — milestone 008 rulings 5 and 6.

## Relationships — the agreement direction, settled by milestone 016

See `milestones/016-trust-can-be-earned.md`. The mirror image of the milestone 006 conflict rule
above, reusing the same information-boundary and directionality guarantees rather than restating them
under a different name — a corroboration and a contradiction are the same kind of event, told apart
only by which way the account points.

- **The trigger is exactly `Cognition.Receive`'s existing fresh-agreement branch, not every
  same-direction account.** No prior position is news, and produces neither an agreement nor a
  conflict. A *new* voice asserting the direction the listener already holds is an agreement. A
  speaker who previously asserted the opposite and has now reversed into agreeing with what the
  listener still holds is also an agreement — the reversal is what makes it a fresh event rather than
  a continuation of a voice already on the record. The *same* speaker reaffirming a position he
  already gave, without an intervening reversal, is not an agreement, even though the direction still
  agrees — still one man's single voice, exactly as milestone 003's non-compounding rule already
  required for repeated denials. Verbatim repetition is not an agreement, inheriting the existing
  repeat guard structurally rather than through a second check that could disagree with it.
  Disagreement is a conflict and is never also an agreement — the two are mutually exclusive by
  construction, enforced at the point `Receipt` is built (`Cognition.MakeReceipt`), not claimed as a
  property the type signature itself guarantees.
- **The trigger and the consequence are both perceived, never detected, and the boundary is
  structural.** `AccountAgreement` is assembled entirely from the listener's own side of the exchange
  — his own prior stance and confidence, and what was asserted to him — with no field able to carry
  `World.TruthLog`, `World.Reports`, `ReportedClaim.ActualBasis`, or `Report.Candor`. Whether the
  speaker was sincere is unavailable to this path, not merely unused by it.
- **The consequence is directional and is trust alone.** Only the listener's relationship toward the
  speaker moves; the speaker's does not. No dimension besides trust is touched — there is no
  equivalent of a grievance for having been agreed with.
- **A separately named, separately valued provisional coefficient — not the conflict cost reused.**
  `Relations.AccountAgreementTrustGain` starts equal to `Relations.ConflictTrustCost` (both `0.35`),
  which is a provisional symmetric starting point and not a claim that corroboration and contradiction
  are worth the same thing socially. Naming them separately is what lets a later evidence-led pass
  move either without moving the other; milestone 016 tuned neither.
- **Provenance is preserved and deliberately left unweighted, not "already accounted for" by
  anything else.** Unlike the conflict rule above — where `Cognition`'s own erosion rates and stance
  protection genuinely do charge the epistemic difference between direct observation and testimony
  before the social layer ever sees it — nothing in `Cognition.Receive`'s agreement branch discounts a
  corroboration by how the listener came to hold the belief being corroborated. Milestone 016
  deliberately applies one flat social rule regardless of the prior's provenance, the same way the
  conflict rule does, but for a different and more honest reason: not because the distinction is
  charged elsewhere, but because nobody has yet decided it should be. `AccountAgreement` carries
  `PriorSourceKind`/`PriorSourceId` regardless, so a later pass can weight on it without reconstructing
  what was dropped — the mechanism for that pass exists; the decision to use it does not.
- **All three receipt paths apply it** — the report channel, delegation briefings, and assignment
  briefings — the identical set the conflict rule applies to, and for the identical reason: a rule
  applied where it was noticed and missing everywhere else the value travels is this project's most
  reliably recurring defect.
- **`Domain/Relations.cs` remains the only code that can change relationship state.** No new mutation
  surface was opened; `RecordAccountAgreement` is a second method on the same exclusive API.

## Operation ownership and execution — milestone 028, Matt's rulings

- **Supervision is distinct from execution.** An actor may own multiple operations but executes at
  most one, whether personally commissioned or delegated by another actor. Available direct
  subordinates supply initial capacity; no additional skill/trait management cap is settled here.
- **Identity and responsibility survive handover.** The commissioning owner and owner-local sequence
  identify an operation throughout its life. Reassignment changes its executor, not its progress,
  ownership, or recorded author of a policy breach. Existing attribution remains; this does not
  authorize new generic failure penalties for either participant.
- **Immediate cancellation is the prototype.** A selected owner's order can be cancelled through
  the shared decision pipeline; only that operation's pending work and commitments are released.
  Commands travelling in flight and a subordinate choosing whether to comply are deferred.
- **Bounded prospective selection remains.** Generate one prospective tribute target, excluding
  same-kind operations this actor already owns; other actors' unknown operations do not reserve a
  target globally. The six-candidate attention cap and coercion-method fan-out remain unchanged.
- **Orders are not telemetry.** An owner may see their issued order, target and assigned executor,
  but a delegate's private progress, changed method and failed attempts are not automatically
  disclosed. Existing report channels remain; richer progress inquiries are separate work.

Source: Matt's milestone-028 authorization and follow-up rulings in the implementation task;
implementation and prototype timing are recorded in `milestones/028-delegation-creates-bandwidth.md`.

## The player boundary — settled by milestone 009

See `milestones/009-godot-playable-shell.md`. What is settled is where a person enters the decision
pipeline and what an interface may be given; the interface itself is deliberately provisional and
settles nothing about presentation.

- **Focused operation review explicitly considers cancellation (milestone 028, Matt's ruling
  in the implementation task).** When an actor deliberately reviews an operation they own,
  its feasible cancellation receives a place in the bounded candidate set even if personality
  would normally suppress abandonment. This applies identically to player and NPC reviews:
  personality still affects utility/preference, knowledge and feasibility filters still apply,
  and the six-candidate cap is unchanged. Ordinary unfocused deliberation is unchanged by this
  exception. This is not authority to bypass salience for arbitrary player actions.
- **A player is a preference, not a second action implementation.** Deliberation splits into
  `Pipeline.Prepare` (trigger, beliefs, agenda, bounded generation, salience/knowledge/capability/
  access rejection, scoring) and `Pipeline.Resolve` (commit, schedule, record). `Pipeline.Deliberate`
  is exactly `Resolve(Prepare(...), null)` and is what `Runner` still calls for every autonomous
  character, so NPC behaviour is unchanged *by construction* rather than by test. A player answers
  only the pipeline's fifth question — which available option do you prefer — and resolving his answer
  runs the same `Commit` in the same order with the same consequences. **`Resolve` refuses an id that
  is not in `PreparedDecision.Available`, and throws before mutating anything.** There is no path by
  which a player takes an action his character could not have taken himself, which is what causal
  parity forbids.
- **What is offered is what survived his own filters, and nothing about the offer says which he would
  have taken.** Options are ordered by candidate id — never by rank and never by salience — because
  either ordering is a utility score with the number filed off. Rejected candidates, score components,
  the noise draw, salience notes and the agenda's `Reason` (which embeds a numeric pressure value) do
  not cross the boundary.
- **No string authored by a scheduler or a generator crosses the boundary, and the vocabulary that
  replaces them is silent by default.** Settled by milestone 009's first correction, which found the
  original arrangement leaking: `PendingDecision.Occasion` was `ScheduledEvent.Cause`, and
  `Strategies.Blocked` and `Strategies.Complete` address their events to a strategy's *owner* while
  describing what its *executor* did — so a delegated operation's outcome reached a man nobody had
  told. The same string arrived through `Focus` as well, because `AgendaSelection` sets a
  `RespondToTrigger` agenda's description to the trigger cause verbatim. `PlayerOccasion` now owns a
  closed vocabulary keyed on `EventKind` whose **default is null**, so an event kind is mute until
  somebody rules that a character necessarily knows about it, and `PlayerOption` builds each option's
  wording from the candidate's typed fields. The general rule: a player-facing surface names what it
  may say, rather than filtering what it must not.
- **A candidate's target must be somebody the actor could name, and "who could he name" has exactly
  one derivation: `Acquaintance.KnownTo`.** **Settled by milestone 009's third correction**; the second
  correction attempted it and was rejected, so do not read the rule off that one. `KnownTo` is:

  - **what the character holds** — whoever appears in a claim he holds, gave him an account, he has a
    relationship with, or he holds a grievance against;
  - **widened only by the holders of his own organisation's named posts** — `Organization.Offices`
    and `Organization.BossId`, on the precedent `Inference` sets for institutional facts.

  **Nothing else widens it.** In particular not `ctx.OrgMemberIds`, which is the authoritative
  organisation roster — rank-blind on purpose, and not a list of people anybody knows about — and not
  `Pipeline.SuperiorOf` or `Pipeline.SubordinatesOf`, which are authority scans over
  `world.Characters` and are that same roster under another name. The second correction widened by
  those and called them office relationships, which is why it was rejected: **an office relationship
  is only an office relationship if it comes from an office.** Rank is a property of a person; a post
  is a property of an institution.

  Both readers take this and nothing else — `PlayerView.KnownPeople` and
  `GeneratorContext.AcquaintedIds`. The cognition-only half is deliberately `internal`, because a test
  comparing the player view against *that* while the generators used a wider set is how the leak
  survived a correction written to close it. The roster keeps its rank-blindness where it is still
  used; what is added is the knowledge limit.

  **The rule is over every candidate's target, not the corroboration generator's.** Milestone 009's
  third correction fixed the one generator that had been reported and left `Concede`, `Refuse` and
  `ReportToSuperior` reading the trigger payload unchecked; the fourth found them, and the test now
  covers every `ActionKind`.
- **An encounter is knowledge.** Settled by milestone 009's fourth correction. Having a demand put to
  you in your own shop, or a question put to you by another character, makes that man nameable —
  `Relations.Meet` records it as a stored relationship at zero on every dimension. That is what
  `Establish` produces before anything is set and what `SocialState.Others` already fed into the
  acquaintance derivation, so it introduces no concept; it states something the model was relying on
  and could not express. **Scoring is untouched** — an all-zero relationship reads exactly as an
  absent one, so a man you have merely met is worth what a stranger is worth. `World.Encounters`
  logs them so the "no relationship was created by reading one" invariant stays assertable.
- **The boundary is opaque as well as immutable.** Every collection on a player-facing record is
  frozen in its `init` accessor, because an `IReadOnlyList<T>` backed by a `List<T>` is read-only by
  politeness — the same defect milestone 006 fixed on `IRelationship.Grievances`. `Claim` does not
  cross: its `EventId` is `WorldEvent.Id`, a truth-log counter with no player-facing meaning, so
  `PlayerClaim` carries the predicate and drops the counter. Option ids are opaque stable tokens
  rather than candidate ids, which embed `Claim.ToString()`. The claim *predicate* is canon-supported
  — `INFORMATION_AND_LEGIBILITY.md`, "Player Intelligence Entry" — and the counter is not.
- **A deliberation with nothing open to him does not pause.** It resolves on the autonomous path,
  recording "nothing was open to him". A pause always offers a real choice; one option is a decision,
  none is not.
- **`PlayerView.Build` is the only code that decides what a viewpoint character may be shown.** The
  console renderer consumes its `PlayerSnapshot` rather than deriving the same limits again, and so
  does the Godot interface. Two surfaces answering "what may this character see" independently is the
  ledger's *distinction drawn in one place and dropped on the way to the next*; there is one
  derivation and two layouts. The in-fiction phrasing lives with it, in `PlayerNarration`, because the
  rules it enforces — Discovery says "came across" and never "saw", confidence is words and never a
  number, standing never explains itself — are information-safety rules and not layout.
- **The snapshot is immutable and the session's `World` is `internal`.** A UI cannot reach the truth
  log, decision records, the report log, organisational conditions or anybody else's cognition through
  `SimulationSession`, because the type system will not name them. `InternalsVisibleTo` admits the test
  assembly and nothing else.
- **The player-facing calendar is not `World.Now`.** Advancing raises a horizon and drains the
  existing event queue; there is no tick and no polling. `World.Now` remains the time of the last event
  actually processed. Consequently the stepping pattern cannot change the outcome — the sequence of
  events handled is a property of the queue, and the horizon only decides where a call stops reading
  it. Asserted across all five variants for four different patterns under an identical player policy.
- **Time cannot move while a choice is outstanding.** A half-handled event is not a place the clock
  can pass through; letting later events resolve around one would make the history depend on how long
  somebody took to answer, which is the frame-rate dependence the determinism invariants forbid in a
  different hat.

## Causal feedback — settled by milestone 018

See `milestones/018-the-player-can-see-what-their-choice-did.md`. What is settled is that a causal
thread — acknowledgement, unresolved status, perspective-limited resolution — is a projection of
already-authoritative typed state, never a second record; the surfaces are provisional presentation,
not settled.

- **`PlayerSnapshot` reads four more `World` collections than milestone 014 left it reading, each
  filtered to the viewpoint alone and reduced to audited fields.** `World.Decisions` for
  `LastAction` (`ActorId == viewpoint`, only `.Chosen?.Candidate` rendered through the existing
  `PlayerOption.Describe`, `.Outcome` and everything else on `DecisionRecord` untouched);
  `World.Requests` for `AwaitingAnswers` (`AskerId == viewpoint`); `World.Businesses` for
  `MyBusiness` (`OwnerId == viewpoint`, `PayingTribute` only — `Resistance` stays hidden, per its own
  "objective; characters only estimate it"); `World.AccountConflicts`/`AccountAgreements` for
  `RecentTrustMovements` (`ListenerId == viewpoint`). This narrows, rather than repeals, the rule
  `PlayerSnapshot`'s own header states — see its "amended by milestone 018" paragraph — the same way
  milestone 014 amended it once already for `Cash`.
- **A request stays in `AwaitingAnswers` until the viewpoint's own `Cognition.Testimony` shows an
  actual account, never from elapsed calendar time and never from `World.Decisions` for anybody but
  the viewpoint himself.** Corrected three times after review. The first implementation resolved a
  request from testimony alone and so could not tell "not yet" from "he decided against answering" —
  both read as permanently pending, contradicting `InformationRequest`'s own settled rule that silence
  is itself an answer. The second correction read `World.Decisions` for the *asked* character — a
  `DecisionRecord` existing whose `TriggerEventId` equals a `WakeEventId` the request carried — to add
  a third `Declined` value, and review rejected that: the asker never receives any message
  establishing that the asked person decided anything at all, so surfacing that fact is a private-state
  leak regardless of how little of the record is read. A same-pass attempt to salvage `Declined` as "a
  communicated account whose stance denies the claim" was also wrong, caught by a test within the same
  correction: the natural proof has Vincent give Salvatore a full, sincere account that happens to
  contradict him, and that is an answer, not a refusal. This simulation's report vocabulary has no
  utterance distinct from "an account, possibly negative", so the second correction collapsed
  `RequestDisposition` to two values, `Pending`/`Answered`, both derived from the asker's own
  `Cognition.Testimony`. The third correction removed the `RequestDisposition` enum,
  `PlayerRequest.Disposition`, and `InformationRequest.WakeEventId` entirely: once resolution stopped
  reading `WakeEventId`, nothing else read it either (written, replay-compared, never consumed), and
  since `PlayerRequest` objects are only ever constructed for requests that fail the "answered" check,
  every exposed disposition was necessarily `Pending` — a field that could only ever hold one value.
  `AwaitingAnswers` now filters directly: a request is included iff no testimony from the asked person,
  of exactly the asked claim, exists at or after the moment it was asked
  (`!who.Cognition.Testimony.Any(t => t.SenderId == r.AskedId && t.Claim.Equals(r.About) && t.At >= r.At)`).
  Verified against `Org/Reporting.cs`: an answer to a `SeekCorroboration` is a `ReportToSuperior`
  candidate `Generators.FromRelationship`'s `"asked-to-account"` branch addresses back to the asker, so
  `Reporting.Deliver` calls `Cognition.Receive` on the asker's own cognition, appending to `Testimony`
  unconditionally before any classification branch — while a `Partial` report that withholds precisely
  the asked claim, or any other candidate the asked character prefers instead (`DoNothing` included),
  communicates nothing at all and is therefore structurally indistinguishable from silence, exactly as
  the asker himself could not tell them apart. Once answered, the account needs no further plumbing —
  it already reaches `Known`/`Recent`/`Disagreements` through the pre-existing `Cognition.Receive` →
  `PlayerView.Build` path, attributed the pre-existing way, and drops out of `AwaitingAnswers` entirely.
- **Qualitative trust movement is scoped to `AccountConflict`/`AccountAgreement` alone, not to fear,
  obligation, or grievance.** Those two are the only relationship-mutating events with an existing
  audit trail of "this moved, this way, toward this person" (`PerceivedConflict`/`PerceivedAgreement`,
  milestones 006/016). `Relations.Frighten` and `Relations.RaiseGrievance` have no equivalent, and
  milestone 018 added none — building one would be new persistent state a projection milestone does
  not need. `PerceivedConflict`/`PerceivedAgreement`'s own doc comments, previously "never rendered to
  the player", are amended accordingly.
- **A tribute demand's occasion names the demander, and states force only when the viewpoint holds a
  genuine held `PersonUsedViolence` claim naming both the demander and the business this demand is
  about.** `PlayerOccasion.For`'s signature widened from `(ScheduledEvent, Pronouns)` to
  `(ScheduledEvent, Character, Func<string,string>)` to make this possible; every other case in its
  closed vocabulary is unchanged. **Corrected after review**: the first implementation matched on the
  demander alone (`Claim.Subject`), so the same man's violence at a different business could read as
  "already used force over this." The business is now carried through `EventPayload.AboutClaim`
  (`Strategies.cs` sets it to `BusinessRefusesTribute(business)`, reusing the same claim shape the
  owner's own resistance belief already uses, rather than a new payload field) and matched against
  `PersonUsedViolence.Object`. Threaten (`Relations.Frighten`) leaves no claim at all, so a
  threatened-but-not-forced demand reads as a plain demand — proven as a deliberate mutation-guard, not
  an oversight: staging `Relations.Frighten` without a claim and asserting the occasion still reads "is
  demanding tribute" is one of the milestone's required tests.
- **No separate response log was introduced anywhere in `Commit.cs`, `Pipeline.cs`, or
  `Strategies.cs`.** This is narrower than the milestone's original implementation account claimed,
  corrected per review: `InformationRequest.WakeEventId` (added by the first correction) *is* new
  persistent, replay-reconstructed state — a field on an existing record, not a new collection or a
  new write path — and it is properly covered by both replay comparators
  (`SimulationReplayTests.Snapshot`, `InformationTransmissionTests.Channel`) since the second
  correction. What genuinely holds is the narrower claim: every player-facing surface is a read-time
  projection over collections that already existed for other reasons, no second event log or
  feedback-specific record was built, and the actor-neutrality this gives `LastAction` (a player's
  choice and an autonomous one write the identical `DecisionRecord` through the identical
  `Pipeline.Resolve`) is a consequence of that, not a separate guarantee that had to be built.

## Exposure and concealment — settled by milestone 010

See `milestones/010-a-denial-that-can-win.md`. What is settled is what a concealment acts on and what a
character may revise; the coefficients remain provisional tuning and **none of them was touched** —
ruling 3 forbade tuning anything to make the denial win, and the denial did not win.

- **A man may revise a conclusion he drew himself, and nothing else.** `Cognition.Learn` models
  acquiring information and `Cognition.Receive` models being told something; neither could lower a
  character's confidence in his own reasoning, because Learn's override rule is
  `OverridesPriorRecord() || confidence >= prior.Confidence` and an `Inference` arriving less sure was
  silently discarded. **A character's own conclusions could only ever firm up.** `Cognition.Revise` is
  the one route that closes it, and it refuses anything that is not the holder's own `Inference`: what
  he established by doing or seeing is what `Provenance.OverridesPriorRecord` and `ProtectsStance`
  exist to defend, and what somebody told him is an account he has to be argued out of rather than one
  he can set aside. A revision is a reconsideration — stance and acquisition time stand, only the
  reconsideration stamp moves — which is what re-arms `Reporting.NeedsConveying` for a position he has
  moved on.
- **Concealment acts on a belief, not on the world.** Quieting witnesses removes no `Trace`, alters no
  truth-log entry, and touches nobody else's cognition. What moves is the concealer's own confidence
  that he can be placed at that incident, and it moves in both directions — down on a clean job, up on
  a clumsy one, matching the direction the step's existing `LegalExposure` effect already took. So a
  man who believes he has cleaned up and has not remains exactly as possible as a man who has and does
  not believe it, which is why `ResolveViolence` files that belief as an `Inference` in the first
  place.
- **The belief that moves is the executor's.** He went out and did it and came away with a view; a
  delegator who sent him learns nothing here, the same rule the approach step and the beating itself
  already state. The `LegalExposure` applied beside it still goes to the owner — pressure is
  motivational state borne by whoever carries the consequence, and a belief is cognition that reaches
  nobody who was not there.
- **A concealment names its incident, and every step of it is scoped by that name.**
  `StrategyInstance.SourceEventId` carries the incident as an event id, set from
  `Candidate.AboutIncident` at commitment. Milestone 005's ruling one level down: the incident is the
  identity, never "whatever happened at this address". A claim carrying no event id yields null rather
  than `0`, because treating the default as a key puts every unidentified claim into one shared
  incident — which is the global-scan defect in miniature.
- **A denial is priced from the witnesses to the incident it is about**, by `Claim.EventId`, and never
  from a maximum over every `WitnessSawIncident` the actor holds. Same defect shape as the
  `SeekCorroboration` scan `404b416` fixed: a term scored from the worst unrelated thing in the actor's
  head. **The same shape survives in `Strategies.AdvanceInvestigation`**, which picks and demotes leads
  by location rather than by incident; it was outside milestone 010's authorized scope and is recorded
  in `ROADMAP.md` rather than silently fixed.

## The investigation and the allegation — settled by milestone 011

See `milestones/011-the-detective-has-no-next-move.md`. No coefficient was tuned.

- **A man may revise his own reading as well as his own reasoning, and nothing else.** This
  **corrects milestone 010's entry above**, which admitted `SourceKind.Inference` alone.
  `Provenance.IsOwnReading` is the named rule: `Inference` and `Discovery` are his — his reasoning and
  his reading of a trace, both defeasible, both his to think better of. `Participant` and `Witness`
  are refused because a man does not revise whether he did something or saw it, and the three
  testimony kinds are refused because somebody else's account is what he has to be *argued* out of
  through `Receive`; a quieter second route would let a wishful character discard testimony without
  the disagreement ever being recorded. The original guard put Discovery in with Participant and
  Witness, which is the exact bundle `Provenance.cs` exists to prevent — its other four predicates all
  say a discovery is a reading that can be weak, wrong and reconsidered. **Surfaced through
  implementation**: `AdvanceInvestigation`'s cold-trail branch demotes a lead the investigator found
  herself, so the repair written for it was still a no-op until this changed.
- **A case is about an incident, not an address.** Milestone 005's ruling, now applied at every point
  in the investigation path — which lead it picks up, whether it has closed, and which stale claims it
  demotes. `Generators.SameIncident` is the single predicate, and event 0 is not an incident. The
  accepted scenario contains exactly one incident, so location and incident coincide in it and only a
  staged two-incident case can tell the rules apart.
- **Police interest names the incident the suspect is being looked at over.** A `PoliceInvestigating`
  claim with no event id cannot be answered, cannot be corroborated against anything, and cannot go
  stale when the case does. That is where a heat bar starts, and
  `INFORMATION_AND_LEGIBILITY.md`'s anti-heat-bar tests are what forbids it.
- **What you were told, you check with somebody else; what you worked out yourself, you put to the
  man it names.** `FromAllegation` is the complement of `FromRelationship`'s corroboration branch and
  the two cover every provenance exactly once. The corroboration restriction — there is nothing to
  corroborate about your own eyes — is right and was not relaxed; a third generator is the answer,
  which is the same shape milestone 007 chose for `FromDelegation`. **It reads no role, title or
  skill**: it is the general act of putting something to its subject, and it matters most for an
  investigator only because an investigator's beliefs are self-acquired by construction.
- **Being asked by somebody who is not your superior still offers candid, partial and false.** The
  `asked-to-account` redirect changes who he answers to for one exchange and confers no rank. A
  detective who cannot be lied to is not a detective.
- **A character is described as themselves on every player-facing surface.** `Domain/Pronouns.cs`
  carries the forms and a plural-verb flag; `Verb(singular, plural)` takes both words because English
  agreement is not a suffix rule, so `They` is usable rather than decorative. Pronouns cross the
  boundary the way display names do — for the viewpoint, for a pending decision's actor, and for the
  people in the view. **The developer trace is deliberately excluded**: it is a debugging tool the
  architecture doc separates from player-facing accounts by name.

## A shortfall he cannot attribute — settled by milestone 012

See `milestones/012-a-shortfall-he-cannot-attribute.md`. No coefficient was tuned; the one new number,
`Organization.SignificantRevenueLoss`, is `Sim/Runner.cs`'s pre-existing leadership-review threshold
extracted to a shared constant, not a fresh one.

- **An organisational condition can be doubted, never resolved, by the character answerable for it.**
  Only the organisation's boss reads `OrgCondition.RevenueLoss` this way — it is a fact about the
  family's books, not about any one man's patch. The gate is `InformationRecord.Contested`
  (`Cognition.IsContested`), a fact `Cognition.Receive` already records at the moment of disagreement,
  and the condition's own liveness against the shared threshold — never a fresh confidence number
  invented for the purpose. **`Inference.cs` reads strictly less than its existing policy-breach
  inference**: `org.Condition` and the boss's own cognition, never `World.Businesses`, the truth log,
  or another character's cognition.
- **The conclusion names where to doubt, never what to find.** `ClaimKind.UnattributedShortfall`'s
  subject is a domain, resolved from `Organization.Offices` — the same institutional read the existing
  inference already performs to find who holds which office — never a business. `Stance.Suspects`,
  `SourceKind.Inference`, revisable through `Cognition.Revise` like any other reading of his own.
- **A suspicion reaches actionability through an existing channel, never a fact invented to close the
  gap.** The boss's suspicion travels to the officeholder who can act on it through the same
  assignment-disclosure route that already carries a named refusal — at his own stance and confidence,
  not firmed up the way a named refusal is disclosed. The officeholder's mark-selection generator reads
  it the same way it reads a named refusal: as a `RequiredKnowledge` claim gating an otherwise-ordinary
  candidate, never a special-cased action.
- **A generator's fallback that always resolves to the same candidate and is then always rejected is
  not a working fallback**, regardless of how it reads in isolation — `FromResponsibility`'s
  unconditional `VisibleTargets.FirstOrDefault()` produced a permanently-rejected candidate rather than
  ever proposing the business it was meant to reach. The fix is gated on the same suspicion claim above
  and skips whatever the actor has already concluded is paying, rather than any change to which
  business sorts first.
- **Actor parity is checked by driving a session, not argued from the shape of the code.**
  `SimulationSession.ResolveAutomatically` reproduces the batch-accepted history while a test still
  records what was offered at every pause — "always choose the first option offered" is not equivalent
  to this, because `PreparedDecision.Available`'s candidate-id ordering (milestone 009, ruling 5) is
  deliberately not rank order.

## Capability as belief — settled by milestone 021 and its correction

See `milestones/021-capability-is-a-belief-not-a-stat.md`, including the appended correction. Recorded
here because milestone 020 put the same distinction in the wrong place twice and two Codex rounds were
spent moving it, which is the signature of a rule that was never written down.

- **An assessment of somebody's capability lives in `Cognition`, not on the relationship.** Trust, fear
  and obligation are attitudes and have no truth value; how good a man is at a job is a fact about the
  world with a referent — his own `Capabilities[Skill.Coercion]` — so a character can be *wrong* about
  it, and things a character can be wrong about are beliefs. It is `ClaimKind.PersonIsCapable`, with a
  source, a confidence and a revision path, and it travels through the report and corroboration
  channels like any other claim. **Scoring may not consult the objective figure**: only committed
  resolution (`Strategies.ResolveViolence`) reads `Capabilities`, because that is computing what
  happened rather than weighing an option.
- **Magnitude is carried by graded propositions, never by confidence.** Which bars of
  `CapabilityBar`'s ladder a character holds says how good he takes the man to be;
  `InformationRecord.Confidence` says how sure he is of each, separately. Encoding "he is very good"
  as "I am very sure he is good" collapses two axes into one number — the conflation `LoyaltyReading`
  was unbundled to avoid and `RelationshipFacet` was built to detect. A man firmly believed to clear
  the low bar and firmly believed to fail the high one is a sharper statement than any scalar, and
  only a ladder can make it.
- **The ladder's implication is enforced on read, never on write.** Clearing `HardMan` implies
  clearing `RoughWork`. Storage still admits an incoherent pair, because a character is allowed to be
  wrong and nothing may tidy his beliefs behind his back — but every consumer resolves through
  `CapabilityBar.Read`, so no two readers can reach different tiers for the same man and no scorer can
  emit two components pulling opposite ways. **The rule is one sentence: the highest bar he holds sets
  his tier, and every bar below it is entailed.** That single rule covers both a gap and a
  contradiction; letting a rejection of the low bar win instead would need a second rule and would
  leave the gap case inconsistent with it. Entailment supplies a position and never overwrites one
  that already agrees, so an independently held bar keeps its own confidence. **Narrowed by a second
  correction, 2026-09-07: `Cognition.Revise` cannot build an incoherent pair, because it preserves
  stance and only ever moves confidence — but `Learn` and `Receive` can, since each establishes or
  updates one bar's stance without consulting the other's.** The `capable-angelo` scenario fixture is
  the only place that currently does, not the only place that structurally could; nothing about
  either method is fixture-only. What was never in question is that `CapabilityBar.Read` resolves the
  pair correctly regardless of how it arose — that is the guarantee the rule above states, and it
  does not depend on the pair being rare.
- **A belief moves only where information actually reached the character, and the record says what
  moved it.** Milestone 021 revised a delegator's read of his man on the blocked path, where the job
  came back empty and nobody told him — the owner reading world state he had no access to, which is
  the same omniscience the scoring term had already been corrected for twice. Waking a character is
  not informing him: `EventKind.StrategyBlocked` carries no claim. Every `Cognition.Revise` call now
  states its occasion (`Reconsideration`), kept **alongside** the acquisition source and never in
  place of it — overwriting `SourceKind`/`SourceId` with the revision's would make a March inference
  look like a May discovery, the same silent rewrite `AcquiredAt` and `ReconsideredAt` are kept apart
  to prevent. The parameter is required rather than optional, because the defect it answers was a
  confidence figure drifting with nothing on the record to say why.
- **Revising from confounded evidence is a deliberate attribution error, on the channels that carry
  information.** Whether a shakedown works turns on the mark's resistance, the method, the owner's own
  Persuasion and a roll; the executor's Coercion is one input among several and on the persuade path
  is not an input at all. A delegator who concludes something about his man from the takings arriving
  is reasoning from confounded evidence, and does it anyway, because people do and because a belief
  that can only ever become more accurate is not worth modelling. The milestone's proof obligation
  follows: an assessment must be able to end up *further* from the truth than it started. **The
  correction narrowed where this applies without weakening it** — an assessment may be confounded,
  wrong, and get wronger, but it may not move on information the character never received.

## Keyed stochastic opportunities can co-succeed — settled by the milestone 022 RNG correction, 2026-09-09

`Rng.ForOccasion(worldSeed, occasionKey)` gives distinct, causally local opportunities — a strategy
instance's Nth advance, a specific observer's chance to notice a specific trace — their own
deterministic stream, seeded from the key rather than from a shared position in one global sequence.
**Two or more distinct occasion keys' streams are no longer forced into the specific seed-independent
relationship that made one demonstrated case — three observers of one event — permanently unable to
co-succeed, and are free to land the same way, including all succeeding together, at a given seed.**

This was not true before the correction, and not by design. `Fnv1a` is not GF(2)-linear — it
multiplies — but nothing about the argument needs it to be: it only needs to be a *fixed* function of
the key, producing some fixed 32-bit constant per key. Everything applied to that constant afterward —
XOR-combining it with the world seed, the old single-linear-step finalizer (`h ^= h >> 15`), and every
subsequent `NextUInt` xorshift draw — is linear over GF(2), and linearity is what lets the seed cancel:
for two occasion keys sharing a seed, a GF(2)-linear map distributes over XOR, so the identical
`seed*constant` term drops out of the two streams' XOR difference at every corresponding draw
position, leaving a delta that depends only on the two keys, never on the seed. Milestone 022 found
the concrete symptom (three observers of one street-talk event, never landing together across several
thousand searched seeds) without knowing the cause; the correction found and fixed the cause,
replacing the finalizer with fmix32 (as used in MurmurHash3) — multiplication by an odd constant is
not linear over GF(2), which is what breaks the algebra the defect depended on. See `Rng.cs`'s doc
comment on `ForOccasion` for the full argument, and `docs/milestones/022-the-street-talks.md`'s
correction sections for the fix, its verification, and every natural-run test elsewhere in the suite
whose seed-42 history moved as a result.

**What this is not a claim of.** A fixed, seed-independent XOR relationship between two streams does
not by itself prove they can never jointly clear a probability threshold — whether it does depends on
what the specific fixed delta is. The argument above explains the demonstrated case; it is not a
theorem that every arbitrary pair of occasion keys was universally unable to co-succeed at every seed
under the old mapping, and this record does not claim that. Nor are two occasion-keyed streams claimed
to be statistically independent in the rigorous sense now that the fix is in — fmix32 is a strong
integer-hash avalanche, not a proof of independence. Design and test against "distinct keyed
opportunities may succeed together, at some seed" — not against any assumption of how often, or that
their outcomes never correlate at all.

**Scope of the fix.** `Rng.ForDecision` has the identical GF(2)-linear pipeline shape and therefore
the same structural correlation *risk* — but sharing the demonstrated failure for any concrete pair of
decisions has not itself been shown, so it is recorded as a risk, not a proven identical defect, and
was deliberately left unfixed: confirmed, not touched, in `OPEN_CONCERNS.md` #6. `Rng.ForWorld`,
occasion-key construction, and everything the keys are used
for (probabilities, traits, fixtures, scheduling) were untouched by this correction.

## Factions, if they are ever built — ruled 2026-09-05

Not scope, and nothing here authorizes a second organisation, diplomacy, or factional play. Recorded
because Matt ruled on the *shape* while reading CK3's faction design, and a ruling taken this far
ahead of the work is exactly the kind that gets lost between the conversation and the milestone.

- **A faction must emerge from relationship state, shared motive, and the traits that make a man
  likely to affiliate — never from an aggregate discontent score.** CK3's factions are objects
  carrying a military-power ratio and a discontent meter that ticks toward an ultimatum, with
  membership derived from an opinion threshold. That is the same shape as the global attention value
  this project rejects — `GAME_VISION.md`'s anti-heat-bar heuristics and
  `INFORMATION_AND_LEGIBILITY.md`'s anti-heat-bar tests — and it produces the failure
  `SIMULATION_ARCHITECTURE.md` names directly: N independent agents wearing a shared colour, rather
  than an organisation.
- **The inputs already exist and are the ones to use.** Grievances are itemised, dated and
  relationship-keyed; Trust and Obligation are directional; Belonging is a drive; and milestone 021
  established that a belief about another man is a claim in `Cognition` rather than a stat. A man
  throwing in with others against his boss should be the readable consequence of what he holds
  against that boss and what he believes about the men beside him — the derivation `Utility.Loyalty`
  already performs, pointed at a coordination question it does not currently ask.
- **The test it has to pass is the one every dimension passes.** Name the decision that reads
  affiliation, and the event that moves it. A faction object that exists because factions are a
  thing games have would fail the rule that removed `Affection` in milestone 006.

## Player-neutral architecture and future institutional roles — clarified 2026-08-19

Not a new commitment. This section names something the milestone 009 player boundary already made
true, and records a scope boundary Matt confirmed in chat rather than in a milestone.

- **The architecture is already role-neutral by construction, not by discipline.** "The player boundary
  — settled by milestone 009" above is the mechanism: **a player is a preference, not a second action
  implementation.** `Pipeline.Deliberate` is exactly `Resolve(Prepare(...), null)`, the same call
  `Runner` makes for every autonomous character, and `Resolve` refuses any id not in
  `PreparedDecision.Available`. Nothing in that boundary, in `Character`, `Cognition`, `Relations`, or
  `Claim` reads or assumes a criminal role, an organisation of any particular kind, or `RoleTitle`.
  A detective, a lawyer, or an officer occupying the same architectural position — a `Character` whose
  decisions are prepared and resolved through the same pipeline — is already representable. Nothing
  needs to be built to make this true; something would need to be *added carelessly* to make it stop
  being true.
- **v1 remains crime-first, with its own specific systems, and that is not in tension with the above.**
  The harbour scenario, its six characters, and everything built through milestone 012 stay the whole
  of the assigned scope. Nothing in this section authorises building another playable role, an
  investigation/case/warrant system, courts, prosecutors, multi-city simulation, or any institution
  besides the one organisation that exists today — see `ROADMAP.md`'s candidate scopes and
  `GAME_VISION.md`'s Long-Term Vision, neither of which grants permission on their own.
- **What is decided is a standing constraint on future work, not a future milestone.** Other playable
  institutional roles (detective, lawyer, prosecutor, police officer, politician) are a plausible v5+
  direction, built from the same `Character`/`Relations`/information/`Claim` systems the criminal
  scenario already exercises — not a parallel system, not a second implementation of belief, corruption,
  or reporting. When and whether to build any of it is unscoped and undecided.
- **Anti-patterns that would quietly close this door, named so review can check against them:**
  `PlayerHeatManager`, `PlayerWantedLevel`, `PlayerKnownInformation`, `PlayerCriminalReputation`, and
  any `if player_is_cop`-shaped branch. The existing pattern already avoids all of these —
  `SimulationSession`'s `World` is `internal`, `PlayerView.Build` is the sole derivation of what any
  viewpoint character may see, and nothing is keyed to "the player" rather than to a character. Keeping
  it that way as institutional variety is eventually added is cheaper than any redesign later would be.
  See `REVIEW_LEDGER.md`'s Architecture checklist, which now asks this directly.
- **Corruption, investigation, and cross-institutional leverage are not new mechanisms when they
  eventually get scoped — they are the existing relationship and information systems pointed at new
  institution types.** `RELATIONSHIPS.md`'s Trust/Fear/Obligation/Grievance model, each dimension
  required to name a decision reader, already is what a non-modifier corruption relationship looks
  like; `INFORMATION_AND_LEGIBILITY.md`'s claim/evidence/testimony model already is what cross-character
  leverage and mutual exposure look like. A future institutions milestone extending these to police,
  legal, or political characters should be reviewed as an *application* of settled systems, not a
  proposal for new ones — and if it turns out to need a genuinely new mechanism, that is itself a
  finding worth surfacing before building it.

## Stack

- **Simulation core**: C#, plain classes, engine-agnostic, unit-testable from the command line.
  No Godot/engine dependency in this layer.
- **Persistence**: SQLite — chosen specifically for the explainability requirements (decision
  traces need real queries) and promotion/demotion tiering, not JSON/binary blobs. **Executed
  2026-08-23** by milestone 015, `src/CrimeEmpire.Persistence` — but narrower than this paragraph's
  original vision, deliberately: a save is a replay log (seed, variant, controlled/viewpoint
  character ids, and the ordered session-input log), not a queryable decision-trace store. It
  satisfies "not JSON/binary blobs" and the choice of SQLite itself; it does not yet satisfy "decision
  traces need real queries" or tiering, both still open and still `ROADMAP.md` candidates. Read this
  paragraph's two reasons as separate claims: milestone 015 executed the storage-technology decision,
  not the querying capability that motivated it.
- **Rendering/engine**: Godot 4 with C# (not GDScript) — same language as the sim core, no FFI
  boundary. Chosen over Unity for licensing simplicity, 2D/tilemap support, and UI toolkit fit
  for a text/menu-dense management game. **Executed 2026-08-16** by milestone 009: the project is
  `src/CrimeEmpire.Godot`, on Godot 4.7.1 .NET.
- **Sequencing**: headless console sim core first, then a Godot project. Prove a small hardcoded
  cast produces believable decision traces in plain text before spending time on tilemaps,
  sprites, or UI. — `PROJECT_CONTEXT.md`, "Stack decision."
  **The gate has been passed, and passing it is not the same as retiring it.** Milestones 001–008
  validated the kernel in text, and Matt authorized the playable shell on 2026-08-16. What the
  sequencing still forbids is unchanged: no art pipeline, no map, no tilemaps, no animation. The
  shell exists to establish that the simulation is playable, not to present it. — milestone 009.
- **The engine boundary is one-way and multi-targeted.** Godot 4.7.1 hosts .NET 8
  (`GodotSharp/Api/Release/GodotPlugins.runtimeconfig.json` declares `"tfm": "net8.0"`), so
  `CrimeEmpire.Simulation` multi-targets `net8.0;net10.0`, `CrimeEmpire.Godot` targets `net8.0`, and
  the runner and tests stay on `net10.0`. The library gained no Godot reference, no engine-conditional
  code and no `#if`: multi-targeting is a fact about which runtime can load it, not a coupling. The
  Godot project references the simulation library and **not** the runner, so "the UI consumes
  structured data rather than parsing console-rendered text" is a project-reference fact rather than
  a rule somebody has to keep. — milestone 009.
- **Target framework: .NET 10 (LTS)**, not .NET 9. The kernel was originally scaffolded against
  .NET 9 because that was the SDK already on the dev machine; Matt confirmed the intent is to move
  to .NET 10 now that it's the current LTS. — decided in chat 2026-08-13; flagged originally by
  Codex during the docs/src/tests reorg. **Executed 2026-08-13** (milestone 002, see
  `docs/milestones/002-dotnet-10-migration.md`): SDK pinned via `global.json` to `10.0.400`
  with `rollForward: latestFeature`, and `TargetFramework` set to `net10.0`. Note that the
  original decision text said "update all three `.csproj` files"; in practice the TFM is
  centralized in `Directory.Build.props`, which `CrimeEmpire.Simulation` and `CrimeEmpire.Runner`
  inherit, so only `Directory.Build.props` and the one redundant override in
  `CrimeEmpire.Simulation.Tests.csproj` needed changing. Redundant per-project `TargetFramework`
  entries were deliberately *not* added — `Directory.Build.props` is the single source of truth
  for the TFM.

  **That last sentence is superseded, by milestone 009, for a mechanical reason rather than a change
  of taste.** A multi-targeting project must set `TargetFrameworks`; MSBuild ignores
  `TargetFrameworks` whenever `TargetFramework` has already been assigned; and
  `Directory.Build.props` is imported before any project body, so no condition written there can see
  what the project is about to declare. Assigning the TFM there and multi-targeting anywhere are
  mutually exclusive. `Directory.Build.props` now publishes `CrimeEmpireHostTfm` (`net10.0`) and
  `CrimeEmpireEngineTfm` (`net8.0`) as named values and assigns neither; each project selects one, so
  one place still decides what "the host framework" means. **The .NET 10 decision itself is unchanged**
  — the runner and the tests are on `net10.0` exactly as before, and the simulation library adds
  `net8.0` alongside it rather than leaving .NET 10.

## Operation staffing — settled by milestone 024's second and third corrections, 2026-09-11

- **A character may be involved in at most one active operation at a time, either as its owner or
  as its delegated executor.** Not "not currently the delegate of a second job" — owning a strategy
  he has never delegated, owning one he has since handed onward to somebody else, and carrying work
  delegated to him by somebody else are all the same disqualifying state: involvement, not merely
  execution. `Pipeline.AvailableToExecute(World, string)` is the one definition — a candidate is
  available only when he owns no `StrategyInstance` of his own (delegated onward or not) and no
  other character's own instance names him as `DelegatedToId`. Enforced at both places a delegation
  is created: `Generators.FromRelationship` never offers a busy subordinate as a delegate candidate
  at all, and `Commit.Apply`'s `DelegateStrategy` case refuses, fail-closed, calling the identical
  check directly against `World` — so a hand-built candidate that skipped filtering, or a future
  caller generating one outside this pipeline, cannot bypass the rule. — milestone 024's second
  correction (`00613ca`), closing a gap Codex found in the milestone's own implementation (`f993386`):
  nothing had ever enforced the uniqueness `PlayerSnapshot.Operating`'s own delegate-lookup scan was
  quietly assuming.
- **Operation staffing availability is authoritative organisational state, read for eligibility —
  not a belief, and not bounded by what the delegating character could perceive.** Who is currently
  free to staff is read directly off `World` (`AvailableToExecute` above), the same footing
  `Pipeline.SubordinatesOf`/`OrgMembersOf` already stand on for "who reports to whom" — a fact about
  the organisation's own bookkeeping, not something a character holds a belief about and could be
  wrong on. This is a distinct question from **identity/nameability**, which stays exactly where
  milestone 009's second correction settled it: a target must also appear in
  `Acquaintance.KnownTo` (`GeneratorContext.AcquaintedIds`) before a character can name him as a
  delegate at all, organisationally subordinate or not. `Generators.FromRelationship` applies both
  filters independently — a subordinate can be nameable but busy, or available but unacquainted, and
  either alone is enough to keep him off the offered list.

## Delegated-operation authority and information — settled by milestone 024's sixth correction, 2026-09-11

- **Execution standing follows the current executor, not the owner field that stores the instance.**
  An undelegated owner may Continue, Alter, or Postpone his operation. After delegation those choices
  belong to the named executor; the owner may neither continue nor alter the delegate's work merely
  because the instance remains in his `ExecutionState`. Delegate and Abandon remain owner-only until
  a later, separately authorized design says otherwise. `Strategies.CurrentExecution(World,
  Character)` is the shared derivation used by agenda, generation, filtering, scoring, and commit.
- **A delegated block wakes the executor and informs only the person who experienced it.** The
  executor learns the refusal as participant knowledge and may choose how to react. The absent owner
  gains no belief and no `RevenueShortfall` pressure synchronously; either may move later only through
  a real report, observation, discovery, or other already-authorized channel. A wake is not a report.
- **Postponement is an explicit operation-preserving choice.** At a genuine block, `PostponeStrategy`
  replaces the generic `DoNothing` candidate and schedules one later step on the same instance.
  `DoNothing` must not leave a displayed live operation with no event capable of advancing it.
- **Assignment coherence is domain-scoped.** Leadership does not issue a second assignment to an
  officeholder who still owns a live strategy in that office's domain, including one delegated for
  execution. A live strategy in a different domain does not suppress a legitimate assignment.
  Expired assignments remain historical until the existing completion/abandonment lifecycle closes
  them; this ruling does not authorize renewal-in-place or deletion merely because a deadline passed.
- **No live operation may be silently replaced.** A delegate already carrying another owner's work
  cannot start an operation of his own, and an owner cannot overwrite an operation he has delegated
  away; he must explicitly call it off first. Candidate filtering prevents ordinary offers and
  `Commit.Apply` independently re-reads authoritative `World` state and fails closed, rather than
  trusting a potentially stale prepared context.
- **Policy-breach identity follows the choice.** `PolicyBreachDecisionMakerId` names the actor who
  chose the currently operative prohibited method. Ownership, delegation, and execution do not move
  it. A later `AlterStrategy` moves it only when that actor genuinely changes which prohibited method
  is operative. This identity is persistent behavioral state and belongs in replay fingerprints.
- **Operation choice remains belief-limited; operation resolution remains authoritative.** A man may
  honestly begin a tribute operation from stale or mistaken information. Candidate generation must
  not consult `Business.PayingTribute` to erase that mistake from his decision. When the operation
  reaches the shop, however, objective state decides the consequence: finding an already-paying shop
  corrects the executor's belief and ends the redundant operation before a demand or payment. A
  mistaken belief may cost time; it may not manufacture money.
- **One continuous tribute agreement yields one initial collection.** Agreement and collection are
  separate steps, so `PayingTribute` alone cannot say whether the payment has already been awarded.
  `Business.TributeCollectedForCurrentAgreement` is authoritative persistent state and makes the
  collection boundary idempotent even if two operations began while the shop was refusing. A future
  transition back to non-payment must clear it when ending that continuous agreement; nothing in the
  current scenario changes a paying shop back yet.
- **Owner-observable success may be named without exposing delegated execution.** Money arriving and
  the identity of the man the owner assigned are both facts the owner already has. A completed
  `SecureTribute` occasion may therefore say that the money arrived after that executor handled the
  job. It still must not reveal method, intermediate progress, private refusals, or any other fact
  learned only by the delegate. Other strategy completions retain outcome-agnostic wording unless a
  separately established owner-observable consequence supports more.

Full implementation and correction history: `docs/milestones/024-the-operation-reads.md`.

## Concerns resolved since `design-doc-concerns_1.md` was written

The concerns doc was never updated after later doc revisions addressed several of its own
findings. Retired in favor of this entry; see `OPEN_CONCERNS.md` for what's still actually open.

- **NPC action → player-facing story.** Was concern #1 ("no plan for how NPC-driven action
  becomes visible to the player as story"). Resolved: `INFORMATION_AND_LEGIBILITY.md` now exists
  and is built specifically to answer this — Visibility vs. Legibility, trace/observation/claim
  model, source-limited player reports.
- **Organizational coordination without hive-mind or independent-agent chaos.** Was concern #3.
  Resolved structurally: `SIMULATION_ARCHITECTURE.md`, "Organizational Intent and Coordination,"
  gives the conditions → priorities → offices → assignments → interpretation → reports flow.
  Still flagged in that same doc as "a load-bearing target for the earliest behavioral prototype"
  — the design answer exists, it hasn't been validated by running code yet.
- **Actor parity's legibility (not just affordability).** Was concern #5. Resolved:
  `INFORMATION_AND_LEGIBILITY.md`, "Player-Facing Explanation," gives near-verbatim the
  dev-trace-to-in-fiction-explanation translation layer the concern asked for.
- **MVP proving too many hard things simultaneously.** Was concern #7. Resolved: the
  "Pre-MVP Simulation Kernel" and three-phase "Validation Sequence" (kernel → emergence prototype
  → MVP vertical slice) in `SIMULATION_ARCHITECTURE.md` is exactly the smaller-prototype-first
  structure the concern recommended.
- **Succession stat-inheritance not written down anywhere.** Was concern #9. Resolved: see
  "Succession and persistence" above — now explicit in the vision doc itself.
- **No heuristic test for a disguised heat bar.** Was concern #10. Resolved:
  `GAME_VISION.md`'s Design Heuristics and
  `INFORMATION_AND_LEGIBILITY.md`'s "Anti-heat-bar tests" both cover this directly now.
