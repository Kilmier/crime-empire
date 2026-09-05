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
