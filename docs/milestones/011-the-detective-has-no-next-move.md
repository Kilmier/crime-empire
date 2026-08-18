# Milestone 011 — The Detective Has No Next Move

**Authorized by Matt on 2026-08-18**, who chose the direction from four candidates, kept item 5 when
offered the chance to strike it, and signed off the scope and rulings. Implemented 2026-08-18.
Append-only: corrections go at the bottom, never into the account above.

Codex remains withdrawn. Claude implemented and reviewed its own work — see "Self-review" below, and
`REVIEW_LEDGER.md` §"From milestone 010 onward, review is self-assessment".

## What this milestone was for

Milestone 010 found two defects in `Strategies.AdvanceInvestigation` by accident, while looking at
concealment, and left them as out of scope. Looking at the investigation path properly found
something larger than either.

**Det. Iris Kane opened a case, identified a suspect, and then had nothing to do for the rest of the
run.** At seed 42, all five variants: three decisions in ninety days. The third and every subsequent
wake offered her a candidate set of exactly one option — `let it lie` — and `--full`, which prints
candidates that were generated and rejected, showed **no rejections at all**. Nothing outscored.
Nothing filtered. Nothing generated.

Every route to a question was structurally closed to her:

- **The corroboration generator requires a belief acquired through testimony.** Every belief a
  detective forms is `Discovery` or `Inference` — she works things out and comes across things.
  **A detective's beliefs are self-acquired by construction**, so the model's main corroboration
  route could never fire for her.
- **`FromDelegation` requires people she has sent.** She delegates to nobody.
- **The report branch requires `ctx.SuperiorId`.** She has no `OrganizationId`, so no office, so no
  superior. She had nobody to report to at all.
- **The investigator branch correctly refuses to reopen a case she has already named** — the last
  thing that could have generated anything.

So the one actor who exists to apply pressure from outside the outfit could not ask anybody anything,
could not tell anybody anything, and could not act on the suspect she had named.

## Scope, as authorized

Six items. 1: scope the investigation to its incident. 2: repair the dead cold-trail branch. 3: give
the detective a next move — she can put her case to the person it names. 4: give `PoliceInvestigating`
the incident it belongs to. 5: render a character by their own pronouns, on player-facing surfaces
only. 6: measure.

**Out:** no new characters, no new variants, no arrest/charge/plea/cooperation/custody model, no case
object, no global attention value, no persistence, no tiering, no relationship-schema work, no change
to the developer trace's wording, no Godot change beyond what item 5 needed.

## The eight rulings, preserved

**1 — A case is about an incident, not an address.** Milestone 005 settled it for concealment
redundancy, milestone 010 applied it to concealment's steps, and it is the same rule here. Two
beatings at one shop are two cases.

**2 — An investigator's move is a candidate, scored through the ordinary pipeline.** No branch on
`RoleTitle`, no branch on `Skill.Investigation` to fire an action, no scripted questioning.

**3 — Nothing here creates a global attention value.** `World` has no heat scalar and must not gain
one. Police interest exists only as claims held by specific characters about specific incidents.

**4 — She acts on what she holds, and questioning must not hand her truth.** The answer is composed
only from positions the answerer has, it may be a lie, and she has no way to tell. **A detective who
cannot be lied to is not a detective.**

**5 — Actor parity.** Whatever becomes available to Kane is available to a player controlling her,
through the same candidate set and the same `Pipeline.Resolve`.

**6 — No coefficient is tuned to make her act, or to make the denial win.** If she still does
nothing, or the denial still loses, that is the result — measured, margin stated, recorded.

**7 — The two measurements are taken separately.** Item 5 moves every viewpoint render; items 1–4
move the trace hashes. A single combined diff would let either mask the other.

**8 — Self-review by milestone 010's method**, which is the only method that has found anything.

## What was completed

All six items. `Domain/Pronouns.cs` (new), `Domain/Provenance.cs`, `Domain/Cognition.cs`,
`Domain/Character.cs`, `Decision/Candidate.cs`, `Decision/Generators.cs`, `Strategy/Strategies.cs`,
`Scenario/Cast.cs`, the four `Session/` player-boundary files, `Trace/IntelligenceWriter.cs` and
`CrimeEmpire.Godot/Game.cs`. Tests in new `InvestigationTests.cs` and `PronounTests.cs`.

### 1, 2 and 4 — a case is about an incident, and the cold trail finally goes cold

`AdvanceInvestigation` picked its lead by `r.Claim.Subject == s.TargetId`, decided whether it had
closed by `v.Claim.Object == s.TargetId`, and demoted stale claims the same way — all three by
**location**. The instance now carries the incident (`SourceEventId`, from the lead the case was
opened on, through `Candidate.AboutIncident`) and all three match on `Claim.EventId`. The generator's
"a lead she has already put a name against" test moves to the same rule, expressed once as
`Generators.SameIncident`.

The cold-trail branch called `Learn(stale, Doubts, confidence * 0.5, Inference, …)` and **did
nothing**, for exactly the reason milestone 010's defect 1 existed: Learn discards a record arriving
less confident than the one held. It now calls `Cognition.Revise`.

`PoliceInvestigating` was `new Claim(PoliceInvestigating, suspect.Id)` with no event id, scheduled
with a hardcoded `relatedEventId: 0`. Both now carry the incident. A claim naming no incident cannot
be answered, cannot be corroborated against anything, and cannot go stale when the case does — which
is where a heat bar starts.

**A settled decision had to be corrected to make item 2 work, and it is the most interesting thing
here.** `Cognition.Revise`, added by milestone 010 and recorded in `DESIGN_DECISIONS.md`, admitted
`SourceKind.Inference` alone. Kane's leads are `Discovery`. So the repair written for the cold-trail
branch was **still a no-op** after it was written.

That guard put Discovery in with Participant and Witness — the precise bundle `Provenance.cs` exists
to prevent. That file's own docstring says an earlier `IsUnmediated` covered all three and that
"Discovery inherited all four when it should have had none of them", and its other four predicates
all say a discovery is a reading that can be weak, wrong and reconsidered. A fifth rule saying it was
unrevisable contradicted them. `Provenance.IsOwnReading` is now the named predicate — his own
reasoning and his own reading of a trace are his to revise; what he did, what he saw, and what
somebody told him are not. **Surfaced through implementation rather than inspection**, which is the
condition `CLAUDE.md` sets for reopening a settled decision.

### 3 — putting it to the man it names

`Generators.FromAllegation`, and it is **not a detective's action**: nothing in it reads a role, a
title or a skill. It is the general act of putting something you hold against somebody to them, and
every character with such a belief gets it.

**It is the exact complement of the corroboration route, and the two together cover every provenance
once.** What you were told, you check against somebody else — that route's restriction, and its
reasoning that there is nothing to corroborate about your own eyes, is right and is untouched. What
you worked out or came across yourself, you put to the man it names. This is the same shape milestone
007 chose when it added `FromDelegation` rather than relaxing the corroboration restriction:
*the restriction is right for corroboration, and the answer is a different generator rather than a
wider one.* Widening a limit by renaming its justification is what got milestone 009's second
correction rejected.

It runs last in generator order, so the existing `(kind, target, claim)` dedupe leaves questions with
an existing owner where they were — a delegator auditing his own executor keeps `FromDelegation`'s
version and its wording.

### 5 — a character is described as themselves

Every player-facing surface said "he". Kane's own view opened `WHAT HE HAS` and told her that
"everything here is something **he** saw or was told". That shipped from milestone 003, when the view
was built, to milestone 010. **No test caught it because no test had ever asserted that a character
is described as themselves.**

`Domain/Pronouns.cs` carries subject, object, possessive and reflexive forms plus a `PluralVerb`
flag; `Pronouns.Verb(singular, plural)` is how agreement is done, so `They` is usable rather than
decorative. Both forms are supplied by the caller because English agreement is not a suffix rule —
guessing would produce "he haves". The pronouns cross the boundary the way names do: on
`PlayerSnapshot.ViewpointPronouns`, `PendingDecision.ActorPronouns`, and `PlayerAttitude.PersonPronouns`
for the people in the view.

**The developer trace is deliberately untouched**, and stays on the carried-forward list. It is a
debugging tool that `SIMULATION_ARCHITECTURE.md` separates from player-facing accounts by name, and
changing 59 more strings there would move the trace hashes for no player-visible gain.

## 6 — what was measured

### The detective acts

Kane now opens the case, names a suspect, **puts it to him**, and then has nothing further — which is
correct rather than a shortfall: the question is spent when asked, he answers, and the model has no
arrest. Her decisions go from 3 to 4 in three variants.

**`FromAllegation` fires for two characters, and the second was not anticipated.** Salvatore infers
Vincent's policy breach himself, so the corroboration route could never offer it and he had no way to
put it to the man who committed it. He now does, on 2 April, and it beats asking Tommy — *the boss
puts the breach to his own capo.*

### A denial to a detective is cheaper, and still loses

Tommy answers Kane on 9 April: partial `+1.47`, denial `−0.82`, margin **2.29**. The same man's
denial to his own capo is `−1.27` against `+1.66`, margin 2.93. **The premise was directionally right
and quantitatively insufficient**: the loyalty terms do fall away against a stranger, and they are
worth about a fifth of the gap. Narrowest losing margin anywhere is **1.01** (Vincent,
`disloyal-vincent`, 18 April), essentially unchanged from milestone 010's 1.083.

Per ruling 6 that is the result. Taken with milestone 010's three reasons, there are now four things
known to hold the denial shut, and the loyalty cost is the smallest of them.

### The two measurements, separately, per ruling 7

**Items 1–4** moved the trace hashes in four variants and left **all 30 viewpoint renders** untouched.
The incident-scoping fixes on their own changed one character of rendered output —
`PoliceInvestigating(tommy)` became `PoliceInvestigating(tommy#11)` — because the fixture contains
exactly one incident, so location and incident coincide everywhere in it. Everything else is item 3.

**Item 5** moved **no developer trace at all** — byte-identical in all five variants — and moved 11 of
30 viewpoint renders: Kane's five, which now read as her, and six where the lowest-trust standing
band changed from "he puts no weight on anything **the man** says" to "he would not take her word at
all". That phrase had to change because "the man" is itself gendered.

| Variant | Hash | Chosen actions | Decisions | Conflicts | Rel. read | Rel. decided |
|---|---|---|---|---|---|---|
| baseline | `0B06A3983797B16A` | `9D014A2A94EC6487` | 44 | 2 | 23 | 2 |
| cautious-vincent | `A8A1BBD12D5334C2` | `124E8FE932DD5A89` | 21 | 3 | 12 | 3 |
| watchful-boss | `F4A61680B871B8F9` | `D35876B5A78C6074` | 48 | 4 | 23 | 4 |
| disloyal-vincent | `CC7D21F508221492` | `09238CEB3AA3B2E5` | 44 | 2 | 25 | 2 |
| resentful-tommy | `EFD549BC3EF89FA8` | `18A116A6855B9E2C` | 39 | 2 | 22 | 3 |

`cautious-vincent` is byte-identical on every behavioural figure — no violence, so no incident, no
case and no allegation. It was the control for milestone 010 too.

`watchful-boss` rises from 2 perceived conflicts to 4. Both new ones are Salvatore hearing Vincent
reject `BusinessRefusesTribute(bellini-grocery)` — a position Vincent genuinely holds, and the
milestone-006 edge firing more often because there are more accounts.

## Tests and results

- **437 passed, 0 failed** (428 after items 1–4; 406 before the milestone). 30 added: 21 in
  `InvestigationTests.cs`, 8 in `PronounTests.cs`, 1 theory case in `ExposureAndDenialTests`.
- **Build: 0 warnings, 0 errors** across four projects, after deleting every `bin`, `obj` and
  `.godot`. One warning appeared and was fixed on the way: scoping the canvass guard to the incident
  stopped narrowing `s.TargetId`, and the fix reads the location off the lead instead — *the
  incident's facts come from the incident.*
- `--verify` deterministic on all three variants. `--compare`: **5 distinct traces, 5 distinct
  chosen-action sequences.**
- Godot headless self-test: 4 choices, 4 decision screens, **exit 0**, no forbidden strings, no
  decimals.

## Self-review

Per ruling 8. **20 mutation checks**, each reverting one part of the change and requiring a *named*
test to fail. All 20 are caught now. Three were not at first, and all three were defects in this
milestone's own tests:

- **A test that could not distinguish the fix from its absence.** The boundary-pronoun check asserted
  over Vincent's attitudes — and everybody Vincent has a scored relationship with is a man, so a
  boundary returning "he" for everybody passed. Worse, **the accepted scenario cannot fail it at
  all**: the attitude list filters to non-zero trust, fear or grievance, and Kane's only relationship
  is the all-zero one `Relations.Meet` records when she questions Tommy. The test now asserts that
  emptiness explicitly and stages the one path the fixture cannot reach.
- **Two mutations that did not compile were reported as passing.** The harness looked for failing
  test names and a summary line, and a build error produces neither. It now distinguishes them.
- **An over-assertion.** `An_investigator_who_has_named_a_suspect_puts_it_to_him` required an answer
  in every variant. A question is spent when it is put and the reply is the other man's to give;
  `resentful-tommy` is the case where he never gets round to it. Asserting an answer there was
  asserting a link the simulation does not make — the ledger's own false-assurance shape. Split.

**Two existing tests had to change, and neither because the model got worse:**

- `The_relationship_change_is_not_large_enough_to_change_a_choice` asserted `winner − runnerUp > 0.5`
  as a **proxy** for "the relationship term did not decide this". The run reordered, the test landed
  on a different decision whose margin is 0.037, and it failed — while the relationship term there is
  *not* choice-changing: without any relationship state the same candidate still wins, and
  `--compare`'s own figure is unchanged. The proxy reported a change that had not happened and could
  equally have stayed silent about one that had. It now re-ranks through
  `ScoreBreakdown.TotalWithoutRelationships`, the production rule the runner's counterfactual uses.
  *Is this assertion checking a link the simulation records, or one the test is inferring?*
- `The_scenario_produces_the_expected_number_of_conflicts(watchful-boss)` moved 2 → 4, for the reason
  given above. The budget's purpose is unchanged.

**Recurring-failure list, walked:**

- *A fix that narrows what can be expressed.* `FromAllegation` deliberately excludes testimony, so a
  man who was told something still cannot put it to its subject directly — he corroborates instead.
  That is the division of labour rather than a loss, but it is a division, and it is stated.
- *A fix that collapses distinct states.* `Candidate.AboutIncident` now serves two strategies. The
  redundancy branch reading it stays guarded on the strategy kind, so an investigation naming its
  incident is not mistaken for a concealment attempt on it.
- *A fix that stops halfway along the path a value travels.* The incident reaches the lead pickup, the
  completion check, the stale demotion, the observation payload and the `PoliceInvestigating` claim.
  `Generators.SameIncident` is one predicate rather than four copies.
- *False-assurance tests.* Three, above.
- *Rewriting an append-only archive at closure.* No archive was edited.
- *Recording a review that did not happen.* This section describes checks that ran, and every figure
  was measured after the last edit to anything it measures.

**What this cannot replace** is an adversary who does not share the author's assumptions. Every one
of the three test defects was a place the author had convinced himself, and each was found by a
mechanical check rather than by looking again.

## Deferred / still carried

Everything carried into milestone 011, plus what it adds.

- **The developer trace still says "he" for everybody**, 59 strings across `Utility` and
  `TraceWriter`. Deliberately out of item 5's scope.
- **`Strategies.AdvanceInvestigation` reads and writes `owner` throughout**, so a delegated
  investigation would put its findings in the head of a man who was not there — the asymmetry
  milestone 010 resolved for concealment by moving the belief to the executor. Investigation is never
  delegated in the fixture, so nothing exercises it.
- **The cold-trail branch is still unreachable in every variant at seed 42.** Kane's canvass always
  turns up a name. The fix is real and is pinned by a staged test, and no natural run demonstrates it.
- **The developer trace renders a chosen `SeekCorroboration` as "did nothing because"**, then prints
  "→ went to X for his own account". Pre-existing wording, noticed here.
- **Nobody in the fixture holds a scored relationship with Kane**, so the attitude list can never
  describe a woman in a natural run.
- The timing of a pause; whether an outfit whose boss cannot name his own soldiers is right; the
  player cannot see why an option is unavailable; nothing stops a Godot script calling `Cast.Build`
  directly; `AGENTS.md` mentions neither `docs/RELATIONSHIPS.md` nor the Godot headless check; one
  controlled and one viewpoint character; no save/load; four decisions in ninety days in the demo.
- Obligation is read but never moves; nothing raises trust; negative trust and decay deferred;
  `GrievanceWeight` uncapped; the tuning guesses; the cast ceiling of six; the empty-domain
  `ConcealIncident(, target=…)` label.
- **Four things now hold the denial shut**, and the loyalty cost is the smallest: one cleanup is worth
  `−0.2` against an MVP rule permitting one attempt; Tommy cannot roll a clean cleanup at any seed;
  Vincent is never offered one; and removing loyalty entirely closes about a fifth of the gap.

## Commit

One implementation-and-archive commit. Status is not established by this file —
`CURRENT_MILESTONE.md` says what is active, and Matt's confirmation of a named commit is the only
thing that counts as acceptance.
