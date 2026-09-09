# Current Milestone

Mutable. Claude can update or replace this file freely while work is underway — it is not history.
On completion, its final content moves to `docs/milestones/NNN-short-name.md` per `AGENTS.md`'s
milestone lifecycle, and this file is reset. This file is the sole handoff surface between agents;
do not create a separate handoff document.

## Status

**Nothing is active.** Codex is reviewing again, and three milestones' standing changed on 2026-09-08
as a result — see `REVIEW_LEDGER.md` for the full accounting behind each.

- **Milestone 021 — Capability Is a Belief, Not a Stat — is closed.** Three corrections beyond its
  implementation, each answering a Codex review; the third (`9fed181`) returned no findings and Matt
  accepted it. Accepted state: `9fed181`. Full account:
  `docs/milestones/021-capability-is-a-belief-not-a-stat.md`.
- **Milestone 026 — In Person, Things Come Back — is closed.** Two corrections from the same-day
  playtests, then three more answering successive Codex reviews of those corrections and of each
  other; the fifth (`abcffd5`) returned no findings and Matt accepted it. Accepted state: `abcffd5`.
  Full account: `docs/milestones/026-in-person-things-come-back.md`.
- **Milestone 020's own outstanding backlog has been reviewed, oldest first.** `34cd117` returned one
  P1 — already superseded by `8e6878e`, which Codex separately confirmed. Milestone 020's accepted
  state moves from `c25129a` to `8e6878e`, now independently confirmed rather than resting on
  self-review alone.

- **Milestone 022's own remaining production-path test gap is now corrected, narrowly.** Matt
  authorized one bounded addition: a test proving street talk survives its complete production path —
  the real violence-operation scheduling route, the real simulation loop resolving the queued
  `ObservationOpportunity`, and the eligible street observer actually coming to hold the executor-
  naming claim as `SourceKind.Rumor`, attributed to the district. The mechanic is unchanged; the
  existing boundaries (owner and investigator stay `Discovery`; the district never becomes a known
  person) are re-checked against the real loop rather than only at the scheduling site. Test-only.
  Seed 42's honest non-result and every accepted hash are unmoved — the new test uses its own seed,
  found by search over the same staged scenario rather than by tuning discoverability, since the roll
  is a genuine Bernoulli draw with no lever to cast toward certainty. Full account:
  `docs/milestones/022-the-street-talks.md`. **This correction now awaits Codex review.**

## Next, per the demo arc

`ROADMAP.md`'s "The demo arc" — layer 1 finishes with **027 The session has an ending**, and then
the layer ends in a playtest. Nothing is authorized; scope goes into this file one milestone at a
time, and what the playtest finds goes first.

**Carried into whatever comes next:** a reader of an impression (a man who saw he was not believed
has reason to act, and nothing offers him anything yet); the operation's pacing, parked at Matt's
word; a history of finished operations, which is 027's.
