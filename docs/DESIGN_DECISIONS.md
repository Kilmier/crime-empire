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

## Stack

- **Simulation core**: C#, plain classes, engine-agnostic, unit-testable from the command line.
  No Godot/engine dependency in this layer.
- **Persistence**: SQLite — chosen specifically for the explainability requirements (decision
  traces need real queries) and promotion/demotion tiering, not JSON/binary blobs.
- **Rendering/engine**: Godot 4 with C# (not GDScript) — same language as the sim core, no FFI
  boundary. Chosen over Unity for licensing simplicity, 2D/tilemap support, and UI toolkit fit
  for a text/menu-dense management game.
- **Sequencing**: headless console sim core first, no Godot project yet. Prove a small hardcoded
  cast produces believable decision traces in plain text before spending time on tilemaps,
  sprites, or UI.
  — `PROJECT_CONTEXT.md`, "Stack decision."
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
