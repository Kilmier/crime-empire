# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**Milestone 011 — The Detective Has No Next Move — is active.** Authorized by Matt on 2026-08-18,
who chose the direction, kept item 5, and signed off the scope and rulings below.

Milestones 001–010 are complete and accepted. **Matt accepted milestone 010 on 2026-08-18 on the
strength of `824f3fc`**, and `REVIEW_LEDGER.md`'s coverage checkpoint now stands there. It needed no
corrective round and it is the first milestone accepted on a review nobody but its author performed;
read those together rather than singly.

**Codex remains withdrawn from the review loop.** Claude implements and reviews its own work — see
`REVIEW_LEDGER.md` §"From milestone 010 onward, review is self-assessment".

## What this milestone is for

Milestone 010 found two defects in `Strategies.AdvanceInvestigation` by accident, while looking at
something else, and left them because they were outside its authorized scope. Looking properly at the
investigation path turned up something much larger than either.

**Det. Iris Kane opens a case, identifies a suspect, and then has nothing to do for the rest of the
run.** Measured at seed 42, all five variants: she takes three decisions in ninety days
(`cautious-vincent`: none, since no violence occurs). The third and every subsequent wake offers her a
candidate set of exactly one option — `let it lie` — and `--full`, which prints candidates that were
generated and rejected, shows **no rejections at all**. Nothing is being outscored. Nothing is being
filtered. Nothing is generated.

### Why nothing is generated: every route to a question is structurally closed to her

- **`FromRelationship`'s corroboration generator requires a belief acquired through testimony**
  (`secondhand`). Every belief a detective forms is `Discovery` or `Inference` — she works things out
  and comes across things. **A detective's beliefs are self-acquired by construction, so the model's
  main corroboration route can never fire for her.**
- **`FromDelegation` requires `DelegatedExecutorIds`.** She delegates nothing.
- **`FromRelationship`'s report branch requires `ctx.SuperiorId`.** She has no `OrganizationId`, so no
  office, so no superior. **She has nobody to report to at all.**
- **`FromResponsibility`'s investigator branch correctly refuses to reopen a case she has already put
  a name to** — and that is the last thing that could have generated anything.

So the one actor in the model who exists to apply pressure from outside the outfit cannot ask anybody
anything, cannot tell anybody anything, and cannot act on the suspect she has named.

### The reason this is worth a milestone rather than a bug fix

The model already has the machinery for a question to become an exchange. `SeekCorroboration` puts a
question; the `asked-to-account` trigger redirects the answer; and the answering branch offers the
recipient **candid, partial and false** — the three-way choice milestone 004's provenance distinction
was built for and that milestone 010 measured as always losing.

**Milestone 010 measured that denial against a delegator, and the thing that kept it shut was partly
loyalty.** `Utility` charges `−1.4 × loyalty` for lying, and Tommy's loyalty to Vincent carries trust
and obligation. **Tommy has no relationship with Kane at all.** A denial put to a detective is priced
without the trust and obligation terms — leaving only the Belonging share, which a stranger does not
reduce.

**This is a hypothesis, not a plan's promise.** It is the reason the scope is worth taking and it must
be measured, not assumed; ruling 6 below governs what happens if it is wrong.

## Scope

**In:**

1. **Scope the investigation to its incident.** `AdvanceInvestigation` picks its lead by
   `r.Claim.Subject == s.TargetId` and demotes stale claims the same way — by **location**, not by
   incident. That is milestone 005's and 010's ruling 1 surviving in the investigation path: two
   beatings at one shop are one case to it. Match on `Claim.EventId`, as concealment now does.
2. **Repair the dead cold-trail branch.** `Learn(stale.Claim, Stance.Doubts, stale.Confidence * 0.5,
   Inference, …)` intends to let her stop treating dead street talk as actionable, and does nothing:
   `Learn` discards a less confident inference. `Cognition.Revise`, added by milestone 010, is exactly
   the method it needs. Unreachable in all five variants at seed 42, so **this fix must be shown to
   work on a staged case, and the fact that no natural run reaches it must be stated rather than
   hidden.**
3. **Give the detective a next move: she can put her case to the person it names.** One new candidate
   route, through `SeekCorroboration` and the existing `asked-to-account` channel — *not* an arrest, an
   interrogation subsystem, a charge, or a plea. `GAME_VISION.md` places arrests, pleas and cooperation
   in later scope and nothing here reaches for them. What she gains is the ability to ask, which
   everybody else in the model already has.
4. **Give `PoliceInvestigating` the incident it belongs to.** It is currently
   `new Claim(ClaimKind.PoliceInvestigating, suspect.Id)` with no `EventId`, scheduled through
   `ScheduleObservation(..., relatedEventId: 0, ...)` — a hardcoded zero where an event id belongs. A
   claim that names no incident is the beginning of a heat bar, which
   `INFORMATION_AND_LEGIBILITY.md`'s anti-heat-bar tests exist to prevent.
5. **Render a character by their own pronouns, on player-facing surfaces only.** Det. Iris Kane is the
   only woman in the cast and every surface calls her "he": her own viewpoint render says
   `WHAT HE HAS`, `WHAT HE CANNOT SETTLE`, and "Everything here is something **he** saw or was told".
   That is a shipping surface stating something false about a person. 56 strings across
   `PlayerNarration`, `PlayerOccasion`, `PlayerOption`, `IntelligenceWriter` and the Godot panels.
   **Matt kept this item on 2026-08-18** when offered the chance to strike it. It remains the one item
   that is not about the investigation mechanism, which is why ruling 7 keeps its measurement apart.
6. **Measure**, and report the answer whichever way it falls: does the detective now act, and does a
   denial put to her win?

**Out:** no new characters (ruling 7 of milestone 010 stands — the cast stays at six, so she gets no
superior officer and no partner); no new scenario variants; no arrest, charge, plea, cooperation or
custody model; no case object or case-file type; no global attention or heat value of any kind; no
persistence; no tiering; no relationship-schema work; no change to the developer trace's own wording;
no Godot change beyond what item 5 needs.

## Rulings taken at planning time

**1 — A case is about an incident, not an address.** Milestone 005 settled it for concealment
redundancy, milestone 010 applied it to concealment's steps, and it is the same rule here. Two
beatings at one shop are two cases.

**2 — An investigator's move is a candidate, scored through the ordinary pipeline.** No branch on
`RoleTitle`, no branch on `Skill.Investigation` to fire an action, no scripted questioning. Traits and
role change salience and evaluation; they never trigger behaviour —
`SIMULATION_ARCHITECTURE.md`, "Traits and Causality", and this project's most important anti-pattern.

**3 — Nothing here creates a global attention value.** `World` has no heat scalar and must not gain
one. Police interest exists only as claims held by specific characters about specific incidents, which
is what item 4 is for. The carrying question is
`INFORMATION_AND_LEGIBILITY.md`'s: *if a global attention variable disappeared, would the individual
cases remain coherent?*

**4 — She acts on what she holds, and questioning must not hand her truth.** A question is a question:
the answer is composed only from positions the answerer actually has, it may be a lie, and she has no
way to tell. Nothing in the new route may consult `World.TruthLog`, `World.Reports`,
`ReportedClaim.ActualBasis` or `Report.Candor`. **A detective who cannot be lied to is not a
detective**, and the whole point of pointing a question at her is that the answer might be false.

**5 — Actor parity.** Whatever becomes available to Kane is available to a player controlling her,
through the same candidate set and the same `Pipeline.Resolve`. The player boundary keeps every
guarantee milestone 009 settled.

**6 — No coefficient is tuned to make her act, or to make the denial win.** Same discipline as
milestone 010's ruling 3, and it applies to both halves of the measurement. If she still does nothing
after the structural fixes, or if the denial to her still loses, **that is the result** — measured,
with the margin stated, recorded as a finding rather than chased.

**7 — The two measurements are taken separately.** Item 5 changes 56 player-facing strings and will
move every viewpoint render; items 1–4 change behaviour and will move the trace hashes. **A single
combined diff would let either mask the other.** Implement and measure items 1–4 first, record those
figures, then item 5, and record the render movement against that intermediate state — not against the
parent commit.

**8 — Self-review is the review, by milestone 010's method**, which is the only method that has found
anything: enumerate the real surface empirically and diff it; mutation-check every fix by reverting it
and watching a *named* test fail; test for the kind of defect rather than the reported instance; walk
`REVIEW_LEDGER.md`'s recurring-failure list as an explicit checklist. Milestone 010's self-review found
three defects **in its own tests** and every one came from a mutation check rather than from re-reading.
A self-review returning no findings is weak evidence and is recorded as such.

## Implementation plan

Ordered. Each step is independently testable, and steps 1–4 land before step 5 so ruling 7 holds.

1. **`Strategies.AdvanceInvestigation` — incident scoping.** Lead pickup and the stale-claim demotion
   both match on `Claim.EventId`. The instance already has `SourceEventId` from milestone 010, but an
   investigation is started from a lead rather than from an incident claim, so establish where its
   incident identity comes from before writing the match — do not assume `SourceEventId` is populated
   on this strategy kind. *Tests: two incidents at one shop stay two cases; a lead for one does not
   demote the other.*
2. **The cold-trail branch, via `Cognition.Revise`.** Staged, because no natural run reaches it.
   *Test: a canvass that finds nothing demotes the lead it was opened on, and only that one. Mutation:
   revert to `Learn` and watch the named test fail — which is the check that would have caught the
   original defect.*
3. **`PoliceInvestigating` carries its incident**, and `ScheduleObservation`'s `relatedEventId: 0` gets
   the real id. *Test: the claim a suspect acquires names the incident he is suspected of; two
   incidents produce two claims. Check the player-facing rendering of the claim still reads sensibly —
   `PlayerNarration` may need the incident, and must not print the counter (`PlayerClaim` drops
   `EventId`, milestone 009).*
4. **The detective's question.** A generator that offers an investigator a `SeekCorroboration` about a
   `PersonUsedViolence` claim she holds, targeted at the person it names. Constraints that already
   exist and must be reused rather than reimplemented: `Generators.CanAsk` bounds it per
   `(asker, asked, claim)`; `Acquaintance.KnownTo` bounds who she can name; `PerceivedSituation.
   HasAccountFrom` stops her re-asking a man who has answered. **Check what the existing corroboration
   generator's `secondhand` restriction is protecting before relaxing anything** — milestone 009's
   second correction was rejected for widening a limit by renaming its justification, and the
   recurring-failure list's first entry is a fix that narrows what can honestly be expressed. *Tests:
   she asks the man her case names and nobody else; she asks once; being asked gives him candid,
   partial and false; the answer is composed only from what he holds.*
5. **Measure items 1–4.** Full verification, all five variants, plus: how many decisions Kane takes,
   what she is offered, whether she asks, whether the answer is a lie, and the margin either way.
   Record in `REVIEW_LEDGER.md` before touching item 5.
6. **Pronouns on player-facing surfaces.** A pronoun set on `Character`, carried across the boundary
   the way every other player-facing fact is, consumed by `PlayerNarration`, `PlayerOccasion`,
   `PlayerOption`, `IntelligenceWriter` and the Godot panels. The developer trace is explicitly out of
   scope: it is a debugging tool, it is stated to be non-player-facing, and touching it would move the
   trace hashes for no player-visible gain. *Tests: Kane's own render says "she" and never "he"; the
   men's renders are byte-identical to step 5's state; the check is computed from the production
   narrator, never from prose written in the test.*
7. **Measure item 5 separately**, against step 5's state.
8. **Archive as `docs/milestones/011-…md`, reset this file, one coherent commit, stop.**

## Open questions to settle during implementation, not now

- Where does an investigation's incident identity come from? A case opened from a lead has a lead, not
  an incident claim. This decides step 1's shape and is the first thing to establish.
- Does a suspect who is asked directly by a detective get the same three-way candour choice as a
  subordinate asked by his boss, or does the existing `namesHim && position.IsHeld` guard already
  cover it? Read it before writing anything.
- Should a detective's question leave a trace the suspect's own people can notice? It is the obvious
  legibility output and it is also scope creep. Answer only if step 4 makes it unavoidable.

## Carried forward

Everything carried into milestone 010, unchanged, plus what 010 added:

- **The timing of a pause is observable even when the occasion is not.**
- **Whether an outfit whose boss cannot name his own soldiers is the right model.**
- **The player cannot see why an option is unavailable.**
- **Nothing prevents a Godot script from calling `Cast.Build` and `Runner.Run` directly.**
- **`AGENTS.md` mentions neither `docs/RELATIONSHIPS.md` nor the Godot headless check.**
- **One controlled character, one viewpoint character, chosen at the start screen and never changed.**
- **No save/load.**
- **Four decisions in ninety in-game days** in the Godot demo.
- Obligation is read but never moves; nothing raises trust; negative trust and decay deferred;
  `GrievanceWeight` uncapped; the tuning guesses; the cast ceiling of six; the empty-domain
  `ConcealIncident(, target=…)` label, which milestone 010 confirmed it does *not* fix.
- **One cleanup is worth `−0.2` and the denial needs about `−0.4`**, against an MVP rule permitting one
  attempt. Milestone 010's finding 1.
- **Tommy cannot roll a clean cleanup at any seed, and Vincent is never offered one.** Milestone 010's
  findings 2 and 3. Both are cast and threshold facts, out of bounds under 010's rulings 3 and 7, and
  **milestone 011 does not touch them either** — it approaches the denial from the other end instead.

## Ordered review process

Unchanged. Matt takes commits in order, oldest first; each review names the exact commit whose diff
was inspected; the coverage table in `REVIEW_LEDGER.md` is the record. **Never write "verified" or
"accepted" from a review report alone** — including one of Claude's own. Matt's confirmation of a named
commit is the only thing that counts, and that rule matters more now that the reviewer and the author
are the same.
