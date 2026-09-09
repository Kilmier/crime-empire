# Milestone 026 — In Person, Things Come Back

Not in the demo arc as planned. Matt's playtest of milestone 025 found it on 2026-09-05, and the
arc's own rule is that what the playtest finds goes first; "the session has an ending" moves to 027.
Authorized the same day — "authorize it, go with your recommendations on all five" — and built the
same day on Fable 5.1.

## What this milestone was for

Two rulings from play, both about one gap: the game had channels for reports and rumours and none
for what happens between two men in a room.

1. *"If a character is not informed of something they should just say so."* Vincent asked Tommy on
   2 March what he knew about the grocery. Tommy had never been told anything about it, held no
   position, and `Generators` offered a man with no position nothing to say — so the request sat as
   "no answer yet" until June. Correct as silence; wrong as a scene.
2. *"It'd be helpful to get an indication about how characters feel."* You threatened Bellini and
   saw nothing; you lied to Marco's face and saw nothing. Fear moved in the model and disbelief was
   recorded in the model, and neither reached the man standing there.

**Channels, not disclosure.** Matt's framing was "too much hidden info", and the canon hides other
people's minds on purpose: `DESIGN_DECISIONS.md` settles that a conflict is perceived, never
detected, and that the listener's reaction is his own state — "the speaker's does not move, unless he
separately observes a response". This milestone builds that response. The viewpoint character forms
his own impression of another man's face, with a keyed roll behind it, and the impression can be
wrong. "Marco Bellini did not look convinced" is Vincent's reading; "Marco does not believe you" would
be a leak, and the screen never says it.

## The five rulings, as taken

(c) and (d) were recommended in the scope review; (a), (b) and (e) were Claude's calls under Matt's
"go with your recommendations", recorded as such in `CURRENT_MILESTONE.md` before a line was written.

- **(a) Every account is given in person.** The model has no messengers, so every `Report` and every
  tribute demand is one man in front of another, and the read runs on all of them. A channel field
  distinguishing a sent account from a spoken one would be a distinction nothing uses; a messenger
  channel, if one is ever added, is the one that opts out.
- **(b) Shapes.** A reaction is an `Impression` (kind, what it was about, when) on the speaker's
  relationship toward the listener, remembered as milestone 023 remembers why a standing moved — not a
  claim, because a `Claim`'s object cannot carry the claim the reaction was about, and a reading of a
  man's face is about the man. A no-position reply is a `Testimony` entry flagged `Disclaims`, and a
  `ReportCandor.Uninformed` on the report that carried it.
- **(c) The speaker's trust does not move on a bad reaction.** Remembered, never scored.
- **(d) No NPC reader.** The impression lands in an NPC's relationship identically; nothing generates
  or scores on it. A projection milestone with the interlocking half deferred, the shape of 018, 023
  and 024, and stated rather than argued away.
- **(e) One channel, two subjects.** Belief and fear are read by the same draw; a report yields a
  belief impression, a demand a fear one.

## What was completed

**The no-position reply.** `Generators` offers a man asked about a claim he holds nothing on "tell X
you know nothing about it" — a `ReportToSuperior` candidate with `ReportCandor.Uninformed`, scored as
candour is. `Reporting.Compose` produces an answering report asserting nothing; `Reporting.Deliver`
records a disclaimer in the asker's testimony. The request drops out of WAITING TO HEAR BACK, the man
leaves NOT HEARD FROM, and the reply reads under WHAT YOU KNOW: "3 Mar Tommy Nardo says he knows
nothing about whether Bellini's grocery is not paying its tribute". It happens naturally at seed 42,
on 3 March, the day after the question the playtest asked.

**The distinction that survives the trip.** A disclaimer is an account for "have I heard from him"
and for resolving the request, and never a position: `Cognition.AccountsOf` excludes it, so the
contested rule and the accounts list cannot read it as a denial; `Cognition.Receive`'s repetition rule
skips it, so a man's later real account is his first, not a repeat of nothing. Mutation-checked: making
`AccountsOf` include disclaimers fails two tests.

**The reaction.** `Org/Reactions.cs`. After `Deliver`, the sender draws — `Rng.ForOccasion`, keyed on
the exchange (sender, recipient, time, claim), never on a report id — against his Investigation and
the recipient's Discretion, and remembers whether what he said seemed taken: `SeemedConvinced`,
`SeemedUnconvinced`, or `GaveNothingAway`. The truth a clean read returns is the recipient's own
position on the claim *after* receipt, his side of the exchange, never `Report.Candor` or
`ActualBasis`. A failed read is wrong with probability 0.15 and blank the rest of the time. After a
threat or force at the demand step, the *executor* — the man in the room, never the owner who sent
him — reads the shopkeeper's fear the same way: `SeemedFrightened` or `SeemedUnmoved`. A report that
asserts nothing (a disclaimer) puts nothing to the listener and leaves no impression.

`CleanReadChance = clamp(0.55 + 0.6 × (reader − hider), 0.15, 0.95)`. With the cast as written,
Vincent (reads at 0.10) against Marco (hides at 0.30) reads cleanly 43% of the time; against Salvatore
(0.65), 22%. A misread is reachable in the fixture, and the boundary test searches seeds for both a
clean and a blank read rather than assuming either.

**Projection and both surfaces.** `PlayerAttitude.Impressions`, `PlayerSnapshot.Disclaimers`,
`PlayerNarration.Impression` and `.Disclaimer`. The roster shows movements and readings as one
timeline, oldest first, arrow for a movement and dot for a reading; WHAT JUST HAPPENED shows the
latest reading since the last action. From the direct-action self-test's real screen:

```
WHAT JUST HAPPENED
4 Apr  you chose to tell Salvatore Greco what you know about whether Bellini's grocery is not paying its tribute
4 Apr  Salvatore Greco gave nothing away when you spoke about whether Bellini's grocery is not paying its tribute

Marco Bellini
    you have had no dealings with him to go on
    17 Mar  · Marco Bellini looked frightened
    23 Mar  · Marco Bellini looked frightened
```

**Replay.** Both comparators cover impressions (the behavioural one by kind/subject/object, never
`Claim.ToString()`) and the disclaimer flag on testimony.

## Findings

**Men the player has only read a face off appeared as men whose word he would not take.** Putting
Marco on the roster because Vincent had read his face meant a zero-trust relationship with no
history rendered as "you would not take his word on anything" — distrust, when nothing had ever moved
it. `OPEN_CONCERNS.md` #3 records that the range cannot tell absence of trust from distrust; the
standing history can, and `PlayerNarration.Standing` now says "you have had no dealings with him to go
on" when it is empty. A presentation-level distinction from state the model keeps; the concern stands
and is annotated.

**"The outfit's rule: …" is not a proposition.** The reaction line reads "when you spoke about
whether {claim}", and a noun phrase after "whether" does not parse. `PolicyIssued` now describes as
"the outfit has a rule: no public violence in the harbour", which reads in every position it appears.

**A test that passed against the mutation it was written for.** The listener-side test compared the
impression from a candid and a false delivery of the same denial on one seed; the two share one keyed
draw, and on seed 42 that draw came back blank for both, so a reaction that read the candour still
produced two blanks. Caught by running the mutation. The test now searches for a seed where the read
returns something, and the mutation fails it.

**Four existing tests pinned "a reply asserts what was asked" and "a relationship is accounted for by
a recorded event".** Re-pointed, not weakened: an uninformed reply asserts nothing and is given
exactly when the sender holds no position (a biconditional now, where before it was one direction);
an impression is the fourth legitimate origin of a relationship, alongside movement, conflict and
encounter.

## Verification

- Build 0 warnings / 0 errors. Tests **648 passed, 0 failed** (638 + 10, `InPersonTests.cs`).
- **Every variant's trace hash and chosen-action digest moved, as the scope said they would** — a reply
  is a new report and every report leaves an impression. Recorded, not treated as a breach:

| Variant | Trace | Chosen actions | Decisions | Conflicts |
|---|---|---|---|---|
| baseline | `83D59F6D099B840A` | `DF483465CDA9DB19` | 49 | 2 |
| cautious-vincent | `4B5326A5B9B977AF` | `735B8AB6B9E4421D` | 26 | 3 |
| watchful-boss | `545AABE8A0690ED9` | `5B6A14605B732302` | 55 | 3 |
| disloyal-vincent | `33F3C92F3DB9250C` | `85B45C4BDF91D2CE` | 50 | 2 |
| resentful-tommy | `2899736537AF3BE3` | `B8BA1D1057FE5834` | 44 | 2 |
| capable-angelo | `34E6AF60C2673B95` | `5C37EA18B8CDDF71` | 55 | 2 |

- `--verify` deterministic on baseline, disloyal-vincent and resentful-tommy; `--compare` six distinct
  traces and six distinct action sequences; both required viewpoint runs exit 0.
- All seven Godot invocations exit 0, run after the last change; every screen with a new line read.
- **Mutation checks**, each reverted: making the reaction read `Report.Candor` fails the listener-side
  and boundary tests; removing the draw (always the truth) fails the can-be-wrong and boundary tests;
  letting a disclaimer count as an account fails the disclaimer test and the natural Tommy scene.

## Deferred

- A reader of an impression (ruling d): a man who saw he was not believed has reason to do something
  about it, and nothing offers him anything yet.
- Movement of the speaker's own trust on a bad reaction (ruling c).
- The operation's pacing — Matt: "we can come back to that."
- Reactions to a briefing and to rumours; a messenger channel that would opt out of the read.
- 027, the session having an ending, with the history of finished operations.

## The authorization

`CURRENT_MILESTONE.md` as authorized was written and reset within one session and never committed
on its own. Its substance — the two rulings from play, the channels-not-disclosure rule, the five
rulings as taken, the scope, what was out, and the falsifiers — is reproduced in the sections above
rather than appended verbatim; nothing in it was dropped on the way.

## Commit

One implementation-and-archive commit.

## Correction, same day — he knows what he is good at

Matt, from the second playtest: "I think it should be ok for a player character to be aware of
their own stats, so for instance whether my character is better at persuasion or threatening." Ruled
yes with one line drawn: his own skills are his to know the way his cash is (milestone 014, ruling 1),
and the number is not. Matt supplied the register — "You can talk and communicate with people fairly
well; you are not physically threatening; you have a hard time reading people."

**`PlayerSnapshot.SelfKnowledge`**, four clauses from his own `Capabilities` on three bands (strong
at 0.6, weak under 0.35, fair between), rendered by `PlayerNarration.SelfKnowledge` and shown as the
first entry of WHAT YOU THINK OF PEOPLE, above the men he deals with, because the last clause explains
the readings beneath. Vincent as written:

```
You
    you can talk and communicate with people fairly well
    you are physically threatening
    you keep a straight face well enough
    you have a hard time reading people
```

His actual numbers make him threatening and a fair talker, which is not the example line Matt wrote;
the words follow the numbers. What he *believes* about himself and may be wrong about stays milestone
021's deferred item.

**And the reaction attaches to the claim that landed.** The first version read the impression
against the first claim a report asserted, and a capo who opened his report by repeating the boss's
own rule back to him came away with "Salvatore Greco gave nothing away when you spoke about whether
the outfit has a rule". `Reactions.Landed` now picks, from the listener's receipts, something he
pushed back on, else something that was news to him, else something he already held and heard again,
else what the report led with — news above corroboration, because the boss's own rule repeated back
to him *is* corroboration. The 1 April golden-path read is now about "Tommy Nardo got violent at
Bellini's grocery", which is what Vincent went to tell him. The test for this was staged wrong
once: the "news" chosen was the grocery's weakness, which the scenario seeds the boss with, so it was
corroboration too. Fixed to a claim he genuinely lacks, and the fixture asserts that before relying
on it.

**The Godot editor regenerated the project again.** Matt ran the game from the editor, which left
the three artifacts milestone 018 recorded — the csproj target framework overwritten with `net8.0`,
the `project.godot` header replaced with boilerplate, `Game.cs` re-tabbed with no content change —
plus `Game.cs.uid` and `CrimeEmpire.Godot.csproj.old`. All reverted to `HEAD` and the sidecars
deleted, per the 018 precedent; `git diff -w` confirmed `Game.cs` carried no content change.

Verification: build 0/0; **650 tests** (648 + 2); hashes unchanged from `e58dbcc` — impressions
are not in the trace, and the reaction target is not a decision input; both viewpoint runs exit 0;
all seven Godot invocations exit 0; both rosters read.

## Second correction, same day — human context for human decisions

Matt, from the third playtest, at a 20 April pause offering "cover it up", "tell Salvatore Greco it
did not happen" and "report to Salvatore Greco, leaving out your own part": *"nothing tells me
'Salvatore Greco found out you got violent' or who even caught me, Greco, the police? that info and
context needs to be clearer when making decisions."* And on the readings: *"is 'he gave nothing away'
like 'he didn't say how he knows'?"* — no, and the phrase was the problem.

**Nobody had caught him.** Those options come from his own held claims that name him — he got
violent, he broke the rule — and the generator offers a man who did that the things such a man can
do. They appear whether or not anybody knows. The screen never joined those facts up, so the options
read as an accusation when they were his own conscience listing exits.

**`PlayerSnapshot.Exposure`** — "what hangs over you", shown above the options at a pause and as a
section on the console. Every clause has a source on his side: the acts, from held claims naming him;
the witness, from a held claim that somebody saw him; what he told whom, from his own sent reports —
told, kept from, or denied, each his own act; the reading he took at the time; and what has come
back, from questions put to him and accounts given to him, or "Nobody has raised it with you", which
is a statement about his own testimony log. It cannot say "Salvatore knows"; that is Salvatore's
state, and it reaches him only if somebody says so, in which case it appears in WHAT YOU KNOW like
anything else. From the natural run, Vincent's view:

```
WHAT HANGS OVER HIM
  Vincent Russo broke the rule: no public violence in the harbour.
  He kept it from Salvatore Greco on 1 April.
  Salvatore Greco asked him about it on 2 April.
```

**The silent wakes say the fact and not the outcome.** The blank date line on Matt's screen was a
job finishing: milestone 009 made `StrategyComplete` and `StrategyBlocked` silent because the event's
authored cause carries the outcome of work that may have been delegated. Right about the outcome,
wrong about the fact. A completion now reads "the job you had running has come to an end, one way or
another" — his own state, since `Strategies.Complete` cleared his instance — and a block reads "X has
turned you down" only when the live instance shows he was the man in the room; a delegate's block
stays silent, as milestone 024 keeps a delegate's progress silent. The two tests that pinned the
silence now pin the fact and the absence of the outcome: "cleanup" and "worse" never cross.

**The readings are worded as readings of you.** "Gave nothing away" sounded like a fact about the
other man. Now: "Salvatore Greco seemed to believe you when you spoke about…", "did not seem to
believe you…", "you could not tell whether Salvatore Greco believed you…", "Marco Bellini looked
frightened", "did not look frightened", "you could not tell whether Marco Bellini was frightened".

**Logged, not fixed:** "ask Tommy Nardo what he knows about whether the outfit has a rule" is a
pointless question the corroboration generator allows because a rule told to him in person is
acquired by testimony. A decision-quality defect; on `ROADMAP.md`'s known-debt list.

Verification: build 0/0; **651 tests** (650 + 1); hashes unchanged from `e58dbcc`; three viewpoint
runs read; all seven Godot invocations exit 0. The Godot editor's regeneration artifacts appeared
again and were removed again.

## Third correction — Codex's review of `3a45a27` and `e4df2ff`

Two defects, one per correction above, both a shortcut standing in for a link the model does not
actually have — the exact false-assurance shape this project's own review culture keeps naming.

### `Reactions.Landed` inferred "news" from a coincidence of dates

`3a45a27` introduced `Landed`'s news-outranks-corroboration rule as `receipt.Record.AcquiredAt ==
at`: true for a claim this exact `Receive` call just created, but also true for any claim the
listener already held whose acquisition happens to date to the same day this unrelated report lands
— acquired that morning through some other channel entirely, or on a prior visit that simply fell on
the same date. The equality test could not tell "created just now, by this call" from "already on
the books, dated today by chance," and in a report asserting both an old, coincidentally-dated claim
and a genuinely fresh one, the old claim could out-rank the news — deterministically so when it was
asserted first, since the loop returns its first match.

`Receipt` gains `IsNews`, set only by the one branch of `Cognition.Receive` that finds no prior
record and creates one; every other branch — verbatim repeat, reaffirmation, agreement, disagreement
— sets it `false` regardless of what the timestamps say. `Landed` reads `receipt.IsNews` in place of
the date comparison; the now-unused `at` parameter is removed from `Landed` and its call site rather
than left to warn.

One regression test, `The_reaction_is_about_the_claim_that_is_actually_new_not_one_sharing_its_timestamp`
(`InPersonTests.cs`): Salvatore already holds a claim staged to have been acquired, through some other
channel, on the exact date a new report will land; that report asserts the old claim first and a
genuinely new one second. Mutation-checked by reverting `Landed` to the old date comparison — the new
test failed, picking the old claim, while the pre-existing sibling test proving "news outranks what
the report led with" kept passing, confirming the mutation exercises this specific coincidence rather
than breaking the mechanism generally. A second mutation flipped the fresh branch's `isNews: true` to
`false`, which failed both that test and the pre-existing sibling — `IsNews` is load-bearing for both.

### `PlayerSnapshot.Exposure` attached a reaction to a report that never earned one

`e4df2ff` added a per-recipient "what he told whom" line to `Exposure`, ending in the reading he took
off that man's face — looked up as `who.Social.Toward(recipient).Impressions.Where(i => i.About is
{} about && actClaims.Contains(about)).OrderByDescending(i => i.At).FirstOrDefault()`. Scoped only by
recipient and "about some act claim of his," never by which report the line is actually describing.
A man exposed on two separate incidents to the same recipient — one told, earning a real reaction;
a later one only withheld, earning none, since `Reactions.AfterReport` only ever reacts to what a
report actually asserts — had the later, withheld-only line silently borrow the earlier incident's
reaction, because the lookup never checked that the impression came from the report `Exposure` was
describing at all.

The lookup now requires the impression's own timestamp to match the selected report's (`i.At ==
latest.At`) and its claim to be one that report actually asserted, computed from `latest.Asserted`
rather than the broader `actClaims`. A report that only withheld therefore matches nothing, exactly
as it should: nothing was ever read off a face for a claim never put to it.

One regression test, `A_withheld_only_report_does_not_inherit_an_older_reaction_to_a_different_incident`
(`InPersonTests.cs`): two incidents, two reports to the same recipient — the earlier told and given a
staged reaction, the later withholding a different incident entirely. Mutation-checked by reverting
the lookup to the old, unscoped form — the test failed, the withheld-only line acquiring the earlier
incident's "seemed to believe you" clause it must not have.

### What did not move

No scoring, chosen action, or fixture touched — both fixes are presentation-layer selection logic
reading state that already exists, never inventing or moving a belief, a relationship dimension, or
an event. Actor neutrality, information boundaries and determinism are unaffected: `IsNews` reports
a fact `Receive` already establishes about its own call rather than reading anyone else's cognition,
and `Exposure`'s narrower lookup still reads nothing but the viewpoint character's own reports and his
own relationship state. All four required hashes measured identical to milestone 026's accepted
figures (`baseline` `83D59F6D099B840A`, `disloyal-vincent` `33F3C92F3DB9250C`, `resentful-tommy`
`2899736537AF3BE3`, `capable-angelo` `34E6AF60C2673B95`) — neither defect was reachable by any
accepted variant's natural run, so nothing hashed depended on either bug's presence.

Verification: build 0 warnings / 0 errors on both target frameworks. Tests **657** (655 + 2). `--verify`
on all four required configurations, unmoved. `--compare` at seed 42: 6 configurations, 6 distinct
traces, 6 distinct chosen-action sequences, every digest unmoved. Both required viewpoint runs and all
seven Godot invocations (five self-tests, the two-process restart proof) exit 0. Four mutation checks
this round, each a real temporary production edit, each confirmed to fail only the test staged
against it and nothing else, each reverted with `git diff` confirmed clean afterward.

### Commit

One correction commit. Still unreviewed and unaccepted — this correction has not been back to Codex.

## Fourth correction — Codex's review of `c644b30`

**Appended, not rewritten.** Everything above stands. Codex confirmed the `Receipt.IsNews` correction
and the different-incident `Exposure` regression test as correct, and returned one further P1: the
`Exposure` fix's own three-way match — recipient, timestamp, claim — was still not unique.

### Timestamp and claim together do not identify a report

Nothing forbids two distinct reports to the same recipient, about the same claim, delivered at the
exact same instant — a delegate reporting the same incident twice in one pass, or two independent
accounts of it landing in the same tick, are both ordinary shapes this simulation can produce.
`Impression` carried no reference back to the report that produced it, so `Exposure`'s lookup — match
on recipient (via `who.Social.Toward(recipient)`), `i.At == latest.At`, and the claim being one
`latest` actually asserted — could not tell two such reports apart. Codex reproduced it directly: the
newest of two same-instant reports was the one `Exposure` described, while the reaction it displayed
had come from the older one.

`Impression` gains `ReportId` (`long?`, default null): the originating `Report.Id` for a reading
produced by `Reactions.AfterReport`, and null for `Reactions.AfterDemand`'s reading, which answers to
no report at all. `Exposure`'s lookup now requires `i.ReportId == latest.Id` — the one check the
coincidence cannot fool — alongside the timestamp and claim checks, which stay rather than being
dropped; they are simply no longer load-bearing on their own.

**Both replay comparators, and why they diverge.** `SimulationReplayTests.Snapshot`, the comprehensive
comparator, gains `ReportId` on its `Impressions` fingerprint — persistent state a faithful replay has
to reproduce exactly, the same reasoning `WakeEventId` was added under in milestone 018. Its own
narrower sibling, `BehavioralSnapshot`, does **not** — its header already names `Report.Id` as one of
the monotonic-counter classes it deliberately excludes, because that raw number legitimately shifts
whenever an unrelated report is scheduled elsewhere in the run, which the insertion-stability suite
built on it must not mistake for a behavioural difference. Adding `ReportId` there would have reopened
exactly the false-difference class that comparator exists to close.

One regression test, `Two_reports_at_the_same_instant_do_not_share_a_reaction`: two reports, same
recipient, same claim, same timestamp, different ids, different reactions, staged in arrival order so
the first report's reading sits earlier in the list — the order a `ReportId`-blind lookup would fall
back to among tied timestamps. Asserts the exposure line carries only the newer report's reaction.
Mutation-checked by removing the `ReportId` term: the test failed, picking the older report's "seemed
to believe you" over the newer report's "did not seem to believe you," while the other fifteen
`InPersonTests` kept passing — the mutation isolates this specific coincidence rather than breaking
the mechanism generally.

### What did not move

No scoring, chosen action, or fixture touched. `ReportId` is read only by `Exposure`'s own selection
logic and the comprehensive comparator; nothing in the decision path consults it, so actor neutrality
and information boundaries are unaffected. All four required hashes measured identical to the figures
the third correction already stood on — the coincidence was never reachable by any accepted variant's
natural run.

### Verification

Build 0 warnings / 0 errors on both target frameworks. Tests **658** (657 + 1). `--verify` on all four
required configurations (`baseline` `83D59F6D099B840A`, `disloyal-vincent` `33F3C92F3DB9250C`,
`resentful-tommy` `2899736537AF3BE3`, `capable-angelo` `34E6AF60C2673B95`), all unmoved. `--compare` at
seed 42: 6 configurations, 6 distinct traces, 6 distinct chosen-action sequences, every digest unmoved.
Both required viewpoint runs and all seven Godot invocations exit 0. One mutation check, confirmed and
reverted.

### Commit

One correction commit. Still unreviewed and unaccepted — this correction has not been back to Codex.

## Fifth correction — Codex's review of `ab235b1`

**Appended, not rewritten.** Everything above stands; Codex confirmed the runtime correction — the
explicit `ReportId` linkage and both replay comparators — as correct. One P2 remained: a test-integrity
gap, not a runtime defect.

### The regression test proved the reader, never the writer

`Two_reports_at_the_same_instant_do_not_share_a_reaction` (the fourth correction's own test) stages
its two impressions by hand, passing `ReportId` directly to the `Impression` constructor. That proves
`Exposure`'s consumer-side match is exact — it does not, and structurally cannot, prove that
`Reactions.AfterReport`, the one production writer that is supposed to populate the field, actually
does. Codex mutated `AfterReport` to omit `report.Id` from the `Impression` it records; every one of
the 658 tests then passing, all sixteen `InPersonTests` included, is what proving the gap looks like —
a green suite that could not have caught the writer regressing.

One further regression test, `AfterReport_stamps_the_impression_with_its_own_report_id`: delivers a
report through the real pipeline (`Reporting.Deliver`, not staged) and asserts the resulting
`Impression.ReportId` equals the report's own `Id`. Mutation-checked against exactly the production
call Codex named — reverting `Reactions.AfterReport`'s `new Impression(read, about, report.At,
report.Id)` back to the three-argument form fails the new test while the other sixteen `InPersonTests`,
including the hand-staged same-instant test, keep passing: the two tests now cover consumer and writer
independently, and neither alone would have caught both directions.

The hand-staged same-instant test is kept rather than replaced — it independently proves the consumer
side's exact-id selection among impressions that share every other field, a claim the new writer-side
test does not make and is not staged to make.

### What did not move

Test-only. No production file touched; no scoring, chosen action, hash, or fixture moved.

### Verification

Build 0 warnings / 0 errors on both target frameworks. Tests **659** (658 + 1). `--verify` on all four
required configurations, byte-identical to the fourth correction's figures. `--compare` at seed 42: 6
configurations, 6 distinct traces, 6 distinct chosen-action sequences, every digest unmoved. Both
required viewpoint runs and all seven Godot invocations exit 0. One mutation check — reverting
`AfterReport`'s `report.Id` argument — confirmed and reverted.

### Commit

One correction commit, test-only. Still unreviewed and unaccepted — this correction has not been back
to Codex.
