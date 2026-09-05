# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**Nothing is active.** Milestone 025 — The Interface Stops Fighting the Player — is implemented,
committed, and corrected once from Matt's playtest (same day); he is still playtesting. Milestone
021's correction is committed and awaiting Codex re-review. Milestones 021 through 025 are all
**unreviewed and unaccepted**.

**Milestone 025** cleared milestone 018's presentation debt by subtraction and, widened by Matt
mid-milestone, rewrote every player-facing phrase into plain English in the second person. Its
first correction gave a decision its briefing, reworded the three answers to a question so they can be
told apart, and said "refused" where the screen said "went nowhere". 638 tests; all six hashes
unmoved; seven Godot invocations green. Full account: `docs/milestones/025-the-interface-stops-fighting-the-player.md`.

**Everything from `34cd117` onward is unreviewed.** Codex ran out of usage during milestone 020's
correction chain. `REVIEW_LEDGER.md` calls this *cleared to build on*, not *accepted*.

## Proposed next — awaiting Matt's authorization

**Not authorized. Drafted from Matt's playtest rulings of 2026-09-05 so the scope is in one place
when he decides.** The demo arc says what the playtest of 025 finds goes before 026; this is what it
found. If authorized it takes the next number and "the session has an ending" moves back one.

### In person, things come back

Two rulings from play, both about the same gap: the game has channels for reports and rumours and
none for what happens between two men in a room.

1. **"If a character is not informed of something they should just say so."** Vincent asked Tommy on
   2 March what he knew about the grocery. Tommy had never been told anything about it, holds no
   position, and `Generators` offers a man with no position nothing to say — so the request sits as
   "no answer yet" until June. Correct as silence; wrong as a scene. A man asked in person about
   something he knows nothing of says so, and the asker learns that he knows nothing.
2. **"It'd be helpful to get an indication about how characters feel."** You threaten Bellini and see
   nothing; you lie to Marco's face and see nothing. Fear moves in the model and disbelief is recorded
   in the model, and neither reaches the man standing there. This is `ROADMAP.md` candidate 7, "The
   lie has a face", scope-reviewed the same day.

**The rule this has to keep, stated up front because Matt's framing was "too much hidden info".** The
canon hides other people's minds on purpose: `DESIGN_DECISIONS.md` settles that a conflict is
perceived, never detected, and that the listener's reaction is his own state. What this milestone adds
is *channels* — a reply, a face — through which the viewpoint character forms his own belief about
another man, with a source and a confidence, and which can be wrong. It does not add disclosure. "Marco
did not look convinced" is Vincent's reading; "Marco does not believe you" would be a leak, and the
screen will never say it.

**Scope.**

- A *no-position reply*: when the asked man holds nothing on the claim, he answers that he knows
  nothing of it. Shape to be ruled on — the report vocabulary has affirm and deny and no "don't know";
  the asker's `Testimony` records affirm and deny only. The request drops out of WAITING TO HEAR BACK
  and the answer reads "Tommy Nardo says he knows nothing about it (3 Mar)". Actor-neutral: an NPC
  asker gets the same reply and the same record.
- A *reaction*: after an account delivered to a man's face, and after a threat made to one, the
  speaker acquires a belief about how it landed — believed or not, frightened or not — by a keyed RNG
  draw against the listener's Discretion and the speaker's Investigation (the skill `Runner.Observe`
  already uses for noticing). Derived from the listener's receipt and relationship, never from
  `Report.Candor` or `ActualBasis`. Reads under WHAT JUST HAPPENED: "Marco Bellini did not look
  convinced"; "Marco Bellini looked frightened".
- The narration for both, in the same milestone.

**Rulings needed before it starts** (from the scope review, plus one from the second ruling):

- (a) Which deliveries are in person. Every report today goes through one `Reporting.Deliver` with
  no channel field. The reply-to-a-question path and the tribute demand are the two natural cases.
- (b) The reaction's shape — a new `ClaimKind` about a person's stance, or a memory on the speaker's
  relationship — and the no-position reply's shape.
- (c) Whether seeing you were not believed moves the *speaker's* trust. Recommended: no.
- (d) Whether an NPC reader of the reaction is in scope. Recommended: defer, and say so.
- (e) Whether fear reads the same way as belief — one reaction channel with two subjects, or two.

**Expected costs, disclosed now.** Every variant's trace hash moves: a new belief prints in the trace,
and a reply is a new report. `Decision/` and `Org/` are touched. The information boundary tests for
024's shape apply: two worlds identical but for the listener's state must render differently on a
clean read and identically on a failed one, and a misread must be demonstrated in the fixture.

**Out.** The operation's pacing (three days a step, six to the first refusal) — Matt: "we can come
back to that". A history of finished operations — still 026's.

## Carried from 025

The prose has been rewritten once, by one reader, and corrected once from play. What still reads
badly is the playtest's to find.
