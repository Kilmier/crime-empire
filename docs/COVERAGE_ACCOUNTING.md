# Coverage Accounting

Durable record for milestone 013. Not mutable status — `docs/CURRENT_MILESTONE.md` and
`docs/milestones/013-coverage-accounting-not-vigilance.md` carry that. This file is the accounting
itself: the measurement, what it does and does not cover, and where every uncovered line landed.

**Corrected 2026-08-19 after Codex reviewed `a0c6be8` and returned four findings — see the
correction appended to `docs/milestones/013-coverage-accounting-not-vigilance.md` for the account.**
This file states the corrected totals directly rather than carrying the wrong ones alongside a note;
the wrong version is preserved in git history at `a0c6be8`.

## Measurement

Taken from a clean tree (`bin`, `obj`, `.godot` deleted, then rebuilt) at commit `1046704`, which
sits directly on the accepted baseline `3c86ba4` with no `src/` or `tests/` change between them
(`git diff --stat 3c86ba4 HEAD -- src tests` is empty).

```powershell
dotnet test CrimeEmpire.sln --collect:"XPlat Code Coverage" --results-directory ./coverage-tmp
```

This is the whole measurement — no config file, no extra package, no committed helper. `coverlet.collector`
has been a `<PackageReference>` in `tests/CrimeEmpire.Simulation.Tests/CrimeEmpire.Simulation.Tests.csproj`
since before milestone 009 and produces a Cobertura report the first time it is asked. Read
`line-rate`, `branch-rate`, `lines-covered`, `lines-valid` off the root `<coverage>` element in the
resulting `coverage.cobertura.xml`; uncovered lines are every `<line hits="0" .../>` under every
`<class>`, grouped by the class's `filename`. That is the entire repeatable procedure — anyone can
run the command above and read the same two numbers off the same element.

**Result: 92.10% line, 84.30% branch, 3676/3991 lines covered, 315 uncovered.** Matches the figure
measured at planning time exactly (see `docs/CURRENT_MILESTONE.md`'s prior text, preserved in the
archive) — no test was added or removed between planning and implementation.

**What this run does and does not cover.** The report's `<package>` elements are
`CrimeEmpire.Runner` and `CrimeEmpire.Simulation` — nothing else. That is not a configured exclusion;
no `[ExcludeFromCodeCoverage]` attribute appears anywhere in `src/`, and no `.runsettings` file exists
in this repository. It is a structural consequence of what `dotnet test` loads: the test project
(`tests/CrimeEmpire.Simulation.Tests`) references `CrimeEmpire.Simulation` and `CrimeEmpire.Runner`
directly, so both are loaded into the test host and instrumented. **`CrimeEmpire.Godot` is never
loaded by `dotnet test` at all** — it is a separate Godot-hosted executable with no project reference
from the test assembly, so it was never a candidate for instrumentation, not a file this milestone
chose to leave out. **The test assembly itself (`CrimeEmpire.Simulation.Tests`) is likewise absent**
from the report; coverlet does not instrument the assembly running the tests. Every one of the 315
lines below is drawn from the two packages actually present in the report and accounted for on its
own terms — a `Program.cs` legitimately uncovered by a CLI entry point counts the same as everything
else and is argued the same way, per ruling 1 — but "315 uncovered" is a statement about
`CrimeEmpire.Simulation` and `CrimeEmpire.Runner` only, not about the whole repository.

## Self-check against a known omission (ruling 7)

Before triaging, two regions were already known to be live and unexercised, named in the milestone's
own planning text: `Sim/Runner.cs:311-315` (the grievance/resentment raised on observing a policy
breach) and `Decision/Utility.cs:563-570` (pricing a candid report made with something at stake).
**Both appear in the measured report as uncovered, unprompted** — the coverage collector was not told
where to look and found them anyway. That is the demonstration ruling 7 asks for: the instrument
contains at least one already-confirmed uncovered region without being pointed at it.

**What this does and does not establish.** Per ruling 5, this shows the instrument's *negative*
signal is sound on two known cases — it does not silently report "covered" where there is no
coverage. It does not establish the reverse (that everything it reports covered is meaningfully
exercised, only that it was executed at least once) and it does not validate the line-classification
judgment calls below, which are argued reasoning, not mechanical output — Codex's review of the first
version of this file is direct evidence of that: the classification judgment calls needed a second
reader, and the first pass under-enumerated 20 of 315 lines and mis-classified two regions as
unreachable that a mutable-state check shows are not. See the archive's appended correction.

## The three buckets

Per ruling 1: **legitimately uncovered** (a real reason the line should stay untested), **apparently
dead** (no reachable path exercises it under the current code, a candidate for removal later), or **a
live edge nothing has ever run** (reachable, meaningful behaviour, simply never triggered by any
accepted scenario or test). Per rulings 2 and 3, buckets 2 and 3 are findings recorded here, not work
performed in this milestone — nothing below was fixed, tested, or removed.

**A sharper rule for "apparently dead," fixed by Codex's review.** A switch default arm is only
"apparently dead" when the enum it switches over is closed and every other member is handled
explicitly above it — a compile-time fact (`CoercionMethod`'s three members, `SourceKind`'s
`ExemptFromSuspicion` pair against its other five members). A guard against a *runtime* condition —
a dictionary that could lose an entry, a data-construction convention nothing currently violates but
the type system does not forbid — is reachable in principle even when nothing today reaches it, and
belongs in the live-edge bucket instead. `Strategy/Strategies.cs:149-151` and
`Decision/Utility.cs:736` were both moved from apparently-dead to live-edge on this basis; see their
entries below for why.

Totals, corrected: **186 legitimately uncovered, 6 apparently dead, 123 live edges.** Outside
`Program.cs` (which alone accounts for 118 of the 186), the remaining 197 "interesting" lines split
68 legitimate / 6 dead / **123 live edge** — the large majority of everything worth looking at in this
report is real, reachable behaviour nothing has ever run, not dead weight.

Confidence varies by region and is stated where it is lower than the rest: most entries below were
read against the actual source and cross-checked against the 5-variant/90-day accepted fixture (the
recorded `--compare` table, the 30 viewpoint renders taken for this milestone, and the Godot
self-test transcript); a handful of single-line record-property entries are classified by visible
pattern (an unread positional-record getter on an otherwise-exercised type) rather than individually
traced through a call graph, and are marked accordingly.

### Legitimately uncovered (186)

| File : lines | Reason |
|---|---|
| `Runner/Program.cs` : 118 (whole `Main`) | The CLI entry point. `dotnet test` never invokes it; it is exercised by the manual verification commands in `AGENTS.md` §Verification, which `dotnet test --collect` does not capture. Unchanged from the figure measured at planning time. |
| `Scenario/Roster.cs` : 9 (whole file body) | New-game-screen support — "who a start screen may list before a session exists." No such screen exists yet; per `CLAUDE.md`'s current-phase note, milestone 009 built a playable shell, not a presenting one. `Cast.Build`, which `Roster.Characters` wraps, is otherwise fully exercised. |
| `Scenario/Variants.cs` : 8 (`Describe` switch) | Consumed only by `Roster.Variants()`, which is itself unused for the reason above. The `Apply(...)` half of this file, which actually configures each variant, is fully covered — every variant runs. |
| `Session/SimulationSession.cs` : 13 of 18 (`Start()`'s three id-validation throws at 111-113/118-119/129-130; the `Seed`/`Variant`/`ControlledCharacterId`/`StartedOn` getters at 135-136/139/146; `ResolveAutomatically()`'s guard at 253-254) | Input validation on a public factory method, never triggered because every call site (tests, the Godot self-test) passes ids drawn from `Cast`/`Roster`; and read-only properties that exist for a consuming UI layer to display session metadata, which nothing currently dot-accesses outside the class. |
| `Domain/Relations.cs` : 7 (`Relationship.ToString()` at 116-117; `Writable()`'s fail-closed guard at 152-156) | A developer-trace debug helper, and a guard the surrounding comment already documents as unreachable via any current mutating route ("every route in Relations" goes through `SocialState.Ensure`, which never returns the unstored reading this guard rejects). |
| `Decision/PerceivedSituation.cs` : 3 of 9 (`ActorId`/`Now`/`Testimony` getters at 34-35/42) | Unread positional-property accessors on a type whose constructor and other members are fully exercised. The remaining 6 lines of this file are a live edge — see below. |
| `Domain/Character.cs` : 6 (`Character.ToString()` at 57; `CharacterView.ToString()` at 84; `Pronouns`/`Capabilities`/`Tier` getters at 75-76/81-82) | Debug helpers plus unread projection-property getters — `CharacterView`'s whole point (per its own doc comment) is restricting what a generator may see, and most of its properties are read that way; these three specifically aren't yet. |
| `Domain/Cognition.cs` : 4 of 6 (`Testimony.ToString()` at 39; `AccountConflict.ClaimedBasis` getter at 65; `AccountConflict.ToString()` at 82-83) | Debug helpers and one unread positional-property getter, on types (`Testimony`, `AccountConflict`) whose other members and construction sites are fully exercised. |
| `Domain/Report.cs` : 4 (`ReportedClaim.ToString()` at 94; `InformationRequest.ToString()` at 167; `Report.ToString()` at 203-204) | Debug helpers on the file's own stated pattern — "DEVELOPER TRUTH... nothing under `Runner/` may render this type." |
| `Decision/Generators.cs` : 1 of 3 (`GeneratorContext.Now` getter at 19) | Unread positional-property getter; the record's other ten members are read throughout the generator functions. |
| `Decision/Utility.cs` : 1 of 13 (`ScoreBreakdown.Noise` getter at 67) | Unread positional-property getter; `Total`, `Components`, and `Candidate` — its three siblings — are all read elsewhere in the same record. |
| `Sim/Rng.cs` : 2 (`ForWorld` at 31; `RangeInt` at 70) | Two of the class's five public entry points. Every call site in the simulation uses `ForDecision`, `ForOccasion`, `Range`, or `Chance`; `ForWorld` and `RangeInt` are complete, correct utility surface nothing currently needs. |
| `Sim/ScheduledEvent.cs` : 2 (`Trace.DistrictId` getter at 44; `ScheduledEvent.ToString()` at 109) | Unread positional-property getter (the `Trace` type is constructed but this one field never read back) and a debug helper. |
| `Org/Organization.cs` : 2 (`Assignment`'s `Domain` getter at 59; `PolicyById` at 106) | Unread positional-property getter and an unused query helper with an obvious, correct purpose (`Policies.FirstOrDefault(p => p.Id == id)`) that nothing currently calls — `PoliciesForDomain`, its neighbour, is what the fixture uses instead. |
| `Sim/World.cs` : 1 (`BusinessesIn(districtId)` at 138) | An unused district-scoped query helper, ahead of the map/district UI that doesn't exist yet. |
| `Session/PlayerSnapshot.cs` : 1 (line 35) | Pattern-classified, lower confidence: an unread positional-record-property getter on `PlayerDisagreement` or a neighbouring type, not individually traced. |
| `Domain/Pronouns.cs` : 1 (`ToString()` at 49) | Debug helper. |
| `Domain/Provenance.cs` : 1 (`SourceKind.Label()`'s default arm at 129) | Reachable only for `SourceKind.Inference`; a cosmetic short-form used solely in developer traces, never rendered in the runs this milestone's viewpoint sweep or the Godot self-test exercised. |
| `Decision/Agenda.cs` : 1 (`Agenda.Weight` getter at 23) | Unread positional-property getter; every other `Agenda` field is read by `AgendaSelection` and the trace layer. |
| `Decision/Candidate.cs` : 1 (`ToString()` at 113) | Debug helper — `Candidate.Description` is what player- and developer-facing code actually reads. |

### Apparently dead (6)

| File : lines | Why it looks dead |
|---|---|
| `Decision/Utility.cs` : 3 (148, 156, 164 — the `_ => 0.0` default arms of `BaseRisk`/`BaseEffect`/`Exposure`) | Each switches over `CoercionMethod`, which has exactly three members (`Persuade`, `Threaten`, `Force`), all three handled explicitly above the default in every one of the three methods — a closed enum with no other member the default could ever match. |
| `Decision/Salience.cs` : 1 (97, the discount switch's `_ => 0.0` default) | Reachable only for `SourceKind.Participant` or `.Witness` — verified against `Provenance.cs`'s `ExemptFromSuspicion()`, which is `kind is SourceKind.Participant or SourceKind.Witness`, a compile-time pattern match. The line immediately above (`r.SourceKind.ExemptFromSuspicion() || suspicion <= 0`) already returns early for exactly those two, and the switch's five explicit cases (`Discovery`, `FirstHandTestimony`, `Report`, `Rumor`, `Inference`) account for `SourceKind`'s remaining members — confirmed against the full seven-member enum in `Domain/Claim.cs`. Unreachable given the guard directly above it. |
| `Sim/Runner.cs` : 1 (118, the switch's trailing `return null;`) | Reachable only if `Handle`'s switch is entered for an `ObservationOpportunity` or `AssignmentDelivered` event with a null `OwnerId` (the two `when actor is not null` guards on those cases). Both event kinds have exactly one scheduling site in `src/`: `Strategies.cs:437` explicitly guards `if (observerId is null) return;` before scheduling `ObservationOpportunity`, and `Runner.cs:190` guards `if (office?.HolderId is null) return;` before scheduling `AssignmentDelivered`. Neither scheduler can produce a null owner, so this fallback cannot currently be reached. |
| `Session/PlayerNarration.cs` : 1 (line 47, `Describe`'s default arm `_ => c.ToString()`) | Reclassified from legitimately-uncovered in the prior version of this file, on the same rule as `Utility.cs` above: `Describe`'s switch explicitly handles all ten members of `ClaimKind` (`BusinessRefusesTribute` through `UnattributedShortfall`, verified against the full enum in `Domain/Claim.cs`), so the default cannot be reached by any valid value. `Decision/Filters.cs`'s own `Describe` helper (143-153, in the live-edge section below) has the same shape of default arm but handles only seven of the ten members, so *its* default stays live-edge rather than dead — the two look similar and are not the same finding. |

### Live edges nothing has ever run (123)

Grouped by what they'd do if triggered, not file order — several correlate across files, which is
itself informative: the same missing exercise produces near-simultaneous zero-hit lines in the
generator, the filter, and the trace renderer that describes it.

**Filter rejections that have never fired (`Decision/Filters.cs`, 28 lines: 67-71, 103-106, 110-113,
117-120, 143-153).** Of `Filters.Apply`'s five rejection stages, three fire regularly in the fixture
(the `ConcealIncident`-specific redundancy check, salience, authority, and domain-reach — all
covered). Four never have: the generic "already handling that" redundancy check for any
non-`ConcealIncident` strategy (67-71); the required-knowledge rejection (103-106) and its sole
caller, the `Describe(Claim)` helper that renders what was missing (143-153, whose own default arm
handles only 7 of `ClaimKind`'s 10 members and so is reachable in principle, unlike
`PlayerNarration.cs`'s — see the apparently-dead entry above); the required-skill rejection
(110-113); and the required-crew rejection (117-120). **No generator anywhere in the accepted
fixture currently proposes a candidate that fails knowledge, skill, or crew** — everything offered to
a character already satisfies those three gates, so the checks that exist specifically to catch a
generator overreaching have never had anything to catch. Corroborated independently:
`Trace/TraceWriter.cs`'s "`and {missed.Reason}`" line (96), which only prints when a chosen
candidate's rival was knowledge-rejected, is uncovered for the identical reason — see the
`TraceWriter.cs` entry below for its other nine. This is the region the milestone's planning text
flagged as "disproportionate... worth its own look"; this is that look.

**The two named-at-planning-time edges, re-confirmed** (`Sim/Runner.cs` 311-315, `Decision/Utility.cs`
563-570) — see the self-check above. Nothing new to add beyond what planning already established.

**`TraceWriter.cs`'s other nine lines (`Trace/TraceWriter.cs`, 79-83, 105-107, 177 — the 96 above
already covered).** `TraceWriter.Render` is exercised by two tests that always call it with `full:
false`; `Program.cs`'s CLI passes `full: true` on request via `--full`, but that path is CLI-only and
this milestone's verification did not confirm anyone has actually run it, so it is recorded as
never-run rather than assumed exercised. **79-83** is the `full`-gated "never occurred to him" block,
never reached because no test sets `full: true`. **105-107** is `AppendRelationshipDiagnostic`'s
detail block, gated on `RelationshipGross() > 1e-9` for some scored candidate — no decision in the
specific seed-1 runs the two calling tests exercise crosses that threshold. **177** is `Summary()`'s
grievance-printing line, gated on `c.Social.Grievances.Any()` — no character has an active grievance
at the moment `Summary()` runs in those same runs.

**A third rejection-adjacent edge in `Decision/Generators.cs` (893-894).** The policy-breach check
inside `Coercive()` recognises two policy kinds, `NoPublicViolence` and `ProtectBusiness`. The
fixture's only configured policy (`world.Org.Policies[0]` in `Cast.Build`, adjusted by
`watchful-boss`) is `no-violence-harbour`, a `NoPublicViolence` policy. No variant ever configures a
`ProtectBusiness` policy, so that half of the check has never matched anything.

**Session-boundary misuse guards that nothing has ever tripped (`Session/SimulationSession.cs`, 5
lines: 225-226, 369-371).** `Choose(optionId)`'s guard against an option token that names nothing the
prepared decision actually offered (225-226), and `RequireReady()`'s guard against advancing time
while a decision is still pending (369-371). Both are real invariants a careless caller — a future
Godot UI sending a stale token, or code that forgets to resolve a choice before fast-forwarding —
could trip; nothing that currently drives a session (the automatic-resolution tests, the Godot
self-test) has ever been careless enough to.

**`Pipeline.Allows(candidateId)` (`Decision/Pipeline.cs`, 5 lines: 88-92).** A convenience pre-check —
"is this candidate id one he could actually take" — that nothing currently calls. `Choose()` doesn't
use it; it goes straight to `Pipeline.Resolve` and lets the resulting exception carry the same
information. Plausible future consumer: a UI wanting to grey out an option before the player commits
to it.

**Actions and phrasings the fixture's decisions have never rendered (`Session/PlayerOption.cs`, 20
lines: 54, 56-57, 62, 68-70, 72-76, 96, 98-100, 111, 113-114, 150).** Phrasing branches in `Describe`/
`Body`/`Work`/`Start`/`Speak` for target-less fallbacks and less-common action/strategy/method
combinations — `AlterStrategy` and `DelegateStrategy`'s no-target forms, `RequestHelp`,
`Retaliate`/`Concede`/`Refuse`'s no-target forms, `PostponeStrategy`'s phrasing in `Work()`. Each
corresponds to a real `ActionKind`/`StrategyKind`/`CoercionMethod` value; several of these actions do
occur in the fixture in their with-target form (confirmed against the Godot self-test transcript and
the recorded `--compare` table), so this is not "the action never happens," only "the specific
phrasing branch shown here has not." Lower per-line confidence than the rest of this section — read
against the source but not individually traced to a specific decision in a specific variant the way
`Filters.cs` and the two named edges were.

**Strategy steps the fixture never takes (`Decision/Commit.cs`, 8 lines: 177-182, 189, 272).**
`PostponeStrategy` — "leave it for now" — is never the chosen action in any variant. `AbandonStrategy`
happens, but always for a strategy with no pending scheduled step to cancel (189). `SeekApproval`
happens, but never while the asking character also holds a running strategy that would need its next
step rescheduled around the wait (272).

**A strategy running out of authored steps (`Strategy/Strategies.cs`, all 6 lines: 121-123,
149-151).** `Advance`'s "ran out of steps" completion (121-123, guarded by `s.StepIndex >=
steps.Length`) never fires — every strategy in the fixture resolves some other way (blocked,
abandoned, or its more specific completion condition) before exhausting its authored step list
(`TributeSteps` has 4, `ConcealSteps` 2, `InvestigateSteps` 3). **`AdvanceTribute`'s "the target no
longer exists" branch (149-151) — reclassified from apparently-dead in the prior version of this
file.** It guards against `s.TargetId` naming a `Business` no longer in `world.Businesses`,
`get`-only but backed by a plain `Dictionary<string, Business>` — nothing stops any code holding a
`World` reference from calling `world.Businesses.Remove(...)` directly; the type system does not
enforce the "nothing removes a business" invariant the prior version of this file relied on. No
current code happens to call it, which is a live-edge fact ("nothing has ever run this"), not a
dead-code fact ("nothing can").

**Belief-conflict detection paths the fixture's testimony never reaches (`Domain/Cognition.cs`, 2
lines: 350, 476).** `AttributionRank`'s default arm (350), reachable for `SourceKind.Participant`,
`.Witness`, `.Discovery`, or `.Inference` — none of which any character's testimony-reattribution in
the fixture currently supplies. And the "two sources simply disagree" half of contestedness detection
(476, the `denied = true` branch) — no account of any claim in the fixture is ever recorded as a
denial, consistent with the already-carried-forward finding that "four known reasons the denial stays
shut."

**Pressure kinds that are added but never become the dominant one rendered
(`Session/PlayerOccasion.cs`, 4 lines: 143-146).** `Resentment`, `Fear`, and `OrganizationalInstability`
pressure is added to characters repeatedly in the fixture (e.g. `Runner.cs:314`, both
`disloyal-vincent` and `resentful-tommy`'s setup in `Variants.cs`), but never becomes the *dominant*
pressure driving a rendered `RelievePressure` occasion — only `RevenueShortfall` and `LegalExposure`
ever have, in the runs this milestone measured.

**A written-but-never-read commitment weight (`Domain/ExecutionState.cs`, 4 lines: 21-23, 119).**
`Commitment`'s positional properties and `ExecutionState.CommitmentWeight` — `Commitments.Add(...)`
does execute (a delegated `SecureTribute` strategy adds one; confirmed against `Commit.cs`'s
`DelegateStrategy` case, which is fully covered), but nothing downstream currently reads the weight
back. The same shape as the already-carried-forward "obligation read but never moved" finding: state
written on one side of a mechanism with no consumer yet on the other.

**`Psychology.With(Trait, value)` (`Domain/Psychology.cs`, 4 lines: 113-116).** A copy-with-one-trait-
changed helper nothing currently calls — `Variants.Apply` constructs whole new `Psychology` objects
for each variant rather than using it. A plausible, smaller way to write a future variant.

**Repeat-question and unread-belief accessors on `PerceivedSituation` (`Decision/PerceivedSituation.cs`,
6 lines: 50-54, 148).** `HasAccountFrom(senderId)` (50-54) — "has this person already given him an
account at all" — a real gate against an unbounded ask loop, matching the concern
`REVIEW_LEDGER.md`'s report-channel checklist names, that nothing currently calls; the claim-scoped
sibling `HasAccountFromForClaim`-style check the doc comment above it describes as the one actually in
use is presumably what generators consult instead. `BelievesRefusing(targetId)` (148) — symmetric to
the neighbouring, exercised `BelievesVulnerable`, unused by any current generator.

**`SelfProtection`'s two never-taken branches (`Decision/Utility.cs`, 9 lines: 563-570, 736).**
563-570 is the milestone's own named edge (see the self-check above). **736 — reclassified from
apparently-dead in the prior version of this file** — `SelfProtection`'s `_ => 0` default, reachable
if a `Candid`-labelled report candidate carried a non-empty `Suppressed` list. Every current
`Candidate` construction in `Generators.cs` that sets `Candor = ReportCandor.Candid` leaves
`Suppressed` at its default, so no generator produces the combination today — but `Suppressed` is a
plain settable record property with no invariant enforcing that pairing, the same shape of gap as
`Strategies.cs:149-151` above. A convention observed in every current call site, not a rule the type
system holds.

**`SelfProtection`'s `Rumor` discount branch (`Decision/Salience.cs`, 1 line: 95).** Reachable when a
suspicious character (`suspicion > 0`) holds a `Rumor`-sourced belief; no character in the fixture is
ever both suspicious and in possession of a rumour at the same time.

**The "nothing was open to him" decision signature (`Decision/DecisionRecord.cs`, 1 line: 54).**
Correlates directly with `Sim/Runner.cs`'s `Think()` method (148-150, part of the `Runner.cs`
live-edge total above alongside the named 311-315 edge): when `prepared.Available.Count == 0`,
`Runner.cs` resolves with no candidate and returns without pausing, and `ChosenActionSignature()`'s
corresponding `"nothing-was-open"` branch is the player-facing signature form for exactly that case.
Neither has ever fired — every deliberation in the fixture has always had at least one candidate
survive `Filters.Apply`.

## What this milestone did not do

Per the "Out" scope and rulings 2-3: nothing above was fixed, no dead code was removed, no behavioural
test was added for anything this report found. `docs/ROADMAP.md`'s technical-debt list and this
file are where that work is proposed for later, scoped separately.
