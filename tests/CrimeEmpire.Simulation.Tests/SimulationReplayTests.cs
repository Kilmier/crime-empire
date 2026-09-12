using System.Globalization;
using CrimeSim.Domain;
using CrimeSim.Scenario;
using CrimeSim.Sim;

namespace CrimeEmpire.Simulation.Tests;

public sealed class SimulationReplayTests
{
    [Fact]
    public void Identical_inputs_produce_identical_histories()
    {
        var first = Run(seed: 42, variant: "baseline", days: 90);
        var second = Run(seed: 42, variant: "baseline", days: 90);

        Assert.Equal(Snapshot(first), Snapshot(second));
    }

    /// <summary>
    /// The snapshot has to name what each request was about.
    ///
    /// This cannot be mutation-checked the usual way — the snapshot is the comparator, so deleting
    /// a field from it makes the comparison blinder without failing anything. Asserting the content
    /// of the snapshot string is the only check that fails when the subject is dropped.
    /// </summary>
    [Fact]
    public void The_snapshot_names_the_subject_of_every_request()
    {
        var world = Run(seed: 42, variant: "disloyal-vincent", days: 90);
        Assert.NotEmpty(world.Requests);

        string snapshot = Snapshot(world);
        foreach (var request in world.Requests)
            Assert.True(snapshot.Contains(request.About.ToString(), StringComparison.Ordinal),
                $"the replay snapshot does not mention {request.About}, so a run that asked a " +
                "different question would compare equal");
    }

    [Fact]
    public void Pausing_and_resuming_does_not_change_the_history()
    {
        var uninterrupted = Run(seed: 42, variant: "baseline", days: 90);

        var resumed = Cast.Build(seed: 42, variant: "baseline");
        Runner.Run(resumed, Cast.Start.AddDays(30));
        Runner.Run(resumed, Cast.Start.AddDays(90));

        Assert.Equal(Snapshot(uninterrupted), Snapshot(resumed));
    }

    [Fact]
    public void Meaningful_character_variants_can_change_the_history()
    {
        var baseline = Run(seed: 42, variant: "baseline", days: 90);
        var cautiousVincent = Run(seed: 42, variant: "cautious-vincent", days: 90);

        Assert.NotEqual(Snapshot(baseline), Snapshot(cautiousVincent));
    }

    /// <summary>
    /// Milestone 021: a capability belief is replay-compared, and both comparators see a *revision*
    /// of one — the state this milestone's whole point is that the simulation now moves at runtime.
    ///
    /// <b>This replaces milestone 020's `AssessedCoercion` version of the same test, and the reason
    /// it needed no new comparator field is the argument for the move.</b> That correction had to add
    /// a bespoke line to both fingerprints, because the assessment was a novel scalar on the
    /// relationship record. As an ordinary `PersonIsCapable` belief in `Cognition` it is covered by
    /// the `knowledge|` lines both comparators already emit — kind, subject, object, stance,
    /// confidence, source and reconsideration stamp — so revision is replay-visible for free rather
    /// than by remembering to extend a comparator. A field that needs the comparator taught about it
    /// is a field in the wrong place.
    ///
    /// Perturbs confidence rather than stance because `Cognition.Revise` moves confidence and is the
    /// mechanism the runtime path uses; a test that perturbed something the production path cannot
    /// produce would prove the comparator sees an impossible state.
    /// </summary>
    [Fact]
    public void The_comparators_capture_a_revised_capability_belief()
    {
        var unperturbed = Cast.Build(seed: 42, variant: "capable-angelo");
        var perturbed = Cast.Build(seed: 42, variant: "capable-angelo");

        // Freshly built, unperturbed worlds compare equal — the control the mutation is measured
        // against.
        Assert.Equal(Snapshot(unperturbed), Snapshot(perturbed));
        Assert.Equal(BehavioralSnapshot(unperturbed), BehavioralSnapshot(perturbed));

        // Perturb nothing but how sure Vincent is that Angelo is a hard man, through the same
        // production call the runtime revision path uses. Its return value is asserted so that a
        // Revise silently refused — wrong provenance, wrong holder — cannot pass as a perturbation.
        var revised = perturbed.Get("vincent").Cognition.Revise(
            CapabilityBar.About("angelo", CapabilityBar.HardMan),
            confidence: 0.20,
            holderId: "vincent",
            at: Cast.Start.AddDays(1),
            because: new Reconsideration(
                ReconsiderCause.DelegatedOutcome, SourceKind.Discovery, "angelo"));

        Assert.NotNull(revised);
        Assert.Equal(0.20, revised!.Confidence, precision: 9);

        Assert.NotEqual(Snapshot(unperturbed), Snapshot(perturbed));
        Assert.NotEqual(BehavioralSnapshot(unperturbed), BehavioralSnapshot(perturbed));
    }

    /// <summary>
    /// Milestone 021's correction: the comparator sees the revision's <em>occasion</em>, not merely
    /// that a revision happened.
    ///
    /// Two worlds whose beliefs end up in the identical state — same claim, same stance, same
    /// confidence, same reconsideration stamp — differing only in what is recorded as having moved
    /// them. Before the correction there was nothing to differ by; after it, a replay that
    /// reproduced the number while attributing it to another channel would be reproducing the
    /// figure and not the history, and the comprehensive comparator must refuse that.
    ///
    /// <b>Deliberately absent from <see cref="BehavioralSnapshot"/>.</b> That comparator carries
    /// what changes behaviour, and no decision reads the occasion — it is a provenance record for
    /// developer traces and for this check. Asserting it stays equal there is the other half of the
    /// claim: the correction added an audit trail without adding a behavioural input.
    /// </summary>
    [Fact]
    public void The_comprehensive_comparator_sees_what_moved_a_belief_and_the_behavioural_one_does_not()
    {
        var discovered = Cast.Build(seed: 42, variant: "capable-angelo");
        var inferred = Cast.Build(seed: 42, variant: "capable-angelo");

        Assert.Equal(Snapshot(discovered), Snapshot(inferred));

        var claim = CapabilityBar.About("angelo", CapabilityBar.HardMan);
        var at = Cast.Start.AddDays(1);

        var a = discovered.Get("vincent").Cognition.Revise(
            claim, confidence: 0.20, holderId: "vincent", at: at,
            because: new Reconsideration(ReconsiderCause.DelegatedOutcome, SourceKind.Discovery, "angelo"));

        var b = inferred.Get("vincent").Cognition.Revise(
            claim, confidence: 0.20, holderId: "vincent", at: at,
            because: new Reconsideration(ReconsiderCause.CanvassFoundNothing, SourceKind.Inference, "angelo"));

        // The two records are otherwise identical, which is what makes the occasion the only thing
        // the comparator could be reacting to.
        Assert.NotNull(a);
        Assert.NotNull(b);
        Assert.Equal(a!.Confidence, b!.Confidence, precision: 9);
        Assert.Equal(a.Stance, b.Stance);
        Assert.Equal(a.SourceKind, b.SourceKind);
        Assert.Equal(a.SourceId, b.SourceId);
        Assert.Equal(a.AcquiredAt, b.AcquiredAt);
        Assert.Equal(a.ReconsideredAt, b.ReconsideredAt);
        Assert.NotEqual(a.Reconsidered, b.Reconsidered);

        Assert.NotEqual(Snapshot(discovered), Snapshot(inferred));
        Assert.Equal(BehavioralSnapshot(discovered), BehavioralSnapshot(inferred));
    }

    /// <summary>
    /// Milestone 005's load-bearing property: occasion keys are causally local, not derived from
    /// ScheduledEvent.Id, so a perturbation that only ever consumes an event id and is immediately
    /// cancelled must not change anything downstream. Before the fix, this shifted every later id
    /// and re-rolled every subsequent observation and strategy-step outcome; a cancelled event is
    /// the smallest perturbation that still consumes an id, which is what makes it the right proof.
    ///
    /// Compared with <see cref="BehavioralSnapshot"/>, not <see cref="Snapshot"/>. The full snapshot
    /// is the wrong tool here: it deliberately bakes in ScheduledEvent.Id (DecisionRecord's own
    /// trigger id) and lets Claim.ToString() print a WorldEvent-derived EventId inside report and
    /// candidate text, both of which are *expected* to shift when an extra event is scheduled — that
    /// shift is not a behavioural difference, and asserting against it would fail this test even
    /// when the fix is working.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("cautious-vincent")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    [InlineData("resentful-tommy")]
    public void An_unrelated_cancelled_event_does_not_change_the_history(string variant)
    {
        var unperturbed = Run(seed: 42, variant: variant, days: 90);

        var perturbed = Cast.Build(seed: 42, variant);
        var noise = perturbed.Queue.Schedule(Cast.Start.AddHours(1), EventKind.WorldTick, null, "test: noise");
        perturbed.Queue.Cancel(noise.Id, "test: causally inert perturbation");
        Runner.Run(perturbed, Cast.Start.AddDays(90));

        Assert.Equal(BehavioralSnapshot(unperturbed), BehavioralSnapshot(perturbed));
    }

    /// <summary>
    /// The same property from the truth-log side: WorldEvent.Id is a global counter too, and
    /// nothing keyed to it should exist any more. An extra recorded entry with no causal role must
    /// not move any subsequent roll. See <see cref="BehavioralSnapshot"/> for why the comparison
    /// deliberately does not use the full <see cref="Snapshot"/>.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("cautious-vincent")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    [InlineData("resentful-tommy")]
    public void An_unrelated_truth_log_entry_does_not_change_the_history(string variant)
    {
        var unperturbed = Run(seed: 42, variant: variant, days: 90);

        var perturbed = Cast.Build(seed: 42, variant);
        perturbed.Record("test-noise", "salvatore", null, "an entry with no causal role");
        Runner.Run(perturbed, Cast.Start.AddDays(90));

        Assert.Equal(BehavioralSnapshot(unperturbed), BehavioralSnapshot(perturbed));
    }

    /// <summary>
    /// The actor who chose a prohibited operative method is behavioral persistent state: that
    /// identity determines whose self-knowledge is written when violence resolves. Both replay
    /// comparators must distinguish it even when every scheduling-derived field agrees.
    /// </summary>
    [Fact]
    public void Policy_breach_decision_maker_identity_is_part_of_both_replay_comparators()
    {
        var vincentChose = Cast.Build(seed: 42, "baseline");
        var tommyChose = Cast.Build(seed: 42, "baseline");

        static StrategyInstance Breach(string decisionMakerId) => new()
        {
            OwnerId = "vincent",
            LocalSequence = 0,
            Kind = StrategyKind.SecureTribute,
            Domain = Cast.Harbour,
            TargetId = Cast.Grocery,
            Method = CoercionMethod.Force,
            StartedAt = Cast.Start,
            Deadline = Cast.Start.AddDays(30),
            BreachedPolicyId = "no-violence-harbour",
            PolicyBreachDecisionMakerId = decisionMakerId,
        };

        vincentChose.Get("vincent").Execution.Strategy = Breach("vincent");
        tommyChose.Get("vincent").Execution.Strategy = Breach("tommy");

        Assert.NotEqual(Snapshot(vincentChose), Snapshot(tommyChose));
        Assert.NotEqual(BehavioralSnapshot(vincentChose), BehavioralSnapshot(tommyChose));
    }

    /// <summary>
    /// Whether the current agreement's initial payment has already been taken changes what a later
    /// collection step may do. It is therefore behavioral persistent state, not presentation detail,
    /// and both replay comparators must see it.
    /// </summary>
    [Fact]
    public void Current_agreement_collection_state_is_part_of_both_replay_comparators()
    {
        var notYetCollected = Cast.Build(seed: 42, "baseline");
        var alreadyCollected = Cast.Build(seed: 42, "baseline");

        notYetCollected.Businesses[Cast.Grocery].PayingTribute = true;
        alreadyCollected.Businesses[Cast.Grocery].PayingTribute = true;
        alreadyCollected.Businesses[Cast.Grocery].TributeCollectedForCurrentAgreement = true;

        Assert.NotEqual(Snapshot(notYetCollected), Snapshot(alreadyCollected));
        Assert.NotEqual(BehavioralSnapshot(notYetCollected), BehavioralSnapshot(alreadyCollected));
    }

    private static World Run(int seed, string variant, int days)
    {
        var world = Cast.Build(seed, variant);
        Runner.Run(world, Cast.Start.AddDays(days));
        return world;
    }

    /// <summary>
    /// Internal rather than private, deliberately: this is the comprehensive replay comparator —
    /// truth log, decisions, reports, requests, businesses, and every character's tier/strategy/
    /// relationship/cognition/testimony state — and milestone 019's controlled/autonomous parity
    /// suite reuses it wholesale rather than re-deriving a second, narrower copy of the same
    /// comparison. See <see cref="ControlledAutonomousParityTests"/>.
    /// </summary>
    internal static string Snapshot(World world)
    {
        static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        var lines = new List<string>
        {
            $"now|{world.Now:O}",
            $"queue|{world.Queue.Count}",
        };

        lines.AddRange(world.TruthLog.Select(e =>
            $"truth|{e.Id}|{e.At:O}|{e.Kind}|{e.ActorId}|{e.TargetId}|{e.Summary}"));

        lines.AddRange(world.Decisions.Select(d =>
            $"decision|{d.Id}|{d.At:O}|{d.ActorId}|{d.TriggerEventId}|{d.TriggerKind}|" +
            $"{d.Agenda.Kind}|{d.Agenda.Domain}|{d.Chosen?.Candidate.Id}|" +
            $"{Number(d.Chosen?.Total ?? 0)}|{d.Outcome}"));

        // Report content and candour are simulation state, so determinism has to cover them —
        // otherwise a run could pass the replay test while quietly composing different accounts.
        lines.AddRange(world.Reports.Select(r =>
            $"report|{r.Id}|{r.At:O}|{r.SenderId}|{r.RecipientId}|{r.Candor}|" +
            string.Join(",", r.Asserted.Select(a =>
                $"{a.Claim}:{a.AssertedStance}:{Number(a.AssertedConfidence)}" +
                $":{a.ClaimedBasis}:{a.ActualBasis}")) +
            "|" + string.Join(",", r.Withheld.Select(w => w.ToString()))));

        // Requests too. They gate future candidate generation, so a run that made different
        // requests would go on to make different decisions — state that steers behaviour has to be
        // in the canonical comparison, not only in the focused pause/resume test.
        //
        // Note this line cannot be mutation-checked: the snapshot is the comparator, so deleting a
        // field from it makes the comparison blinder without making anything fail. The independent
        // assurance is that asking is also recorded in the truth log, which the runner's --verify
        // hash covers by a different route.
        lines.AddRange(world.Requests.Select(q =>
            $"request|{q.Id}|{q.At:O}|{q.AskerId}|{q.AskedId}|{q.About}"));

        foreach (var business in world.Businesses.Values.OrderBy(b => b.Id, StringComparer.Ordinal))
            lines.Add($"business|{business.Id}|{Number(business.MonthlyRevenue)}|" +
                      $"{business.PayingTribute}|{business.TributeCollectedForCurrentAgreement}|" +
                      $"{Number(business.Resistance)}|{business.Damaged}");

        foreach (var character in world.Characters.Values.OrderBy(c => c.Id, StringComparer.Ordinal))
        {
            // StrategyCount, LocalSequence, NextAdvanceOrdinal and PendingStepEventId are milestone
            // 005's occasion identity — a run that assigned a different instance sequence, drew a
            // different advance ordinal, or left a different step pending would go on to draw
            // different occasion keys, so this state has to be in the canonical comparison rather
            // than only in a dedicated test. AttemptedConcealments is the redundancy rule's own
            // state: a run that attempted a different set of incidents would reach different
            // candidates on every later wake. SourceEventId is milestone 010's: it decides which
            // witness beliefs the concealment's first step revises, so a run that carried a
            // different incident on the instance would leave the concealer holding a different view
            // of his own exposure and score every later report from it. Deliberately absent from
            // BehavioralSnapshot below, which excludes every field derived from a monotonic counter.
            lines.Add($"character|{character.Id}|{character.Tier}|{character.DecisionCount}|" +
                      $"{character.StrategyCount}|{character.Execution.Strategy?.Kind}|" +
                      $"{character.Execution.Strategy?.OwnerId}|{character.Execution.Strategy?.LocalSequence}|" +
                      $"{character.Execution.Strategy?.StepIndex}|{character.Execution.Strategy?.NextAdvanceOrdinal}|" +
                      $"{character.Execution.Strategy?.PendingStepEventId}|" +
                      $"{character.Execution.Strategy?.SourceEventId}|" +
                      $"{character.Execution.Strategy?.PolicyBreachDecisionMakerId}|" +
                      string.Join(",", character.Execution.AttemptedConcealments.Select(a => a.ToString())) +
                      // Who has executed work for him gates the delegator's account question, so a
                      // run that recorded a different set would go on to generate different
                      // candidates. Insertion-ordered and never removed, so no sort is needed.
                      "|" + string.Join(",", character.Execution.DelegatedExecutorIds));

            // Relationship state, milestone 006. Trust now moves during a run — a perceived account
            // conflict costs the listener trust in the speaker — and Utility.Loyalty reads trust,
            // obligation and grievance while Concede/Refuse read fear, so all of it steers later
            // decisions and belongs in the canonical comparison rather than only in a dedicated
            // test. Marco's fear was already moving before this milestone and was already outside
            // this comparator, which is the gap this line closes.
            //
            // Enumerated through SocialState.All, which is ordered by id: dictionary order is an
            // implementation detail the determinism rules forbid depending on.
            foreach (var rel in character.Social.All)
                lines.Add($"relationship|{character.Id}|{rel.OtherId}|{Number(rel.Trust)}|" +
                          $"{Number(rel.Fear)}|{Number(rel.Obligation)}|" +
                          // Milestone 023. Nothing scores it, so it cannot change a later decision —
                          // but it is persistent state that save/load has to reproduce, and a
                          // comparator that cannot see it would pass a run whose remembered history
                          // had diverged. The cause and the order are what carry meaning; the
                          // timestamp is included because a history that lost its dates would still
                          // compare equal without it.
                          string.Join(",", rel.StandingHistory.Select(h => $"{h.Cause}:{h.At:O}:{h.About}")) + "|" +
                          // Milestone 026: impressions are remembered state and read by the roster;
                          // a replay that lost one would show the player a different history.
                          // ReportId, added by a correction: two distinct reports can otherwise share
                          // recipient, timestamp and claim, and a replay that reattributed a reaction
                          // to the wrong one of them would still pass without this — the comprehensive
                          // comparator is exactly where that linkage identity belongs, unlike the
                          // narrower BehavioralSnapshot below, which deliberately excludes Report.Id.
                          string.Join(",", rel.Impressions.Select(i => $"{i.Kind}:{i.At:O}:{i.About}:{i.ReportId}")) + "|" +
                          string.Join(",", rel.Grievances.Select(g =>
                              $"{g.Description}:{Number(g.Severity)}:{g.At:O}")));

            // Milestone 021's correction added the revision occasion, and it is fingerprinted here
            // rather than left to the ReconsideredAt stamp: the stamp says a belief moved, and the
            // occasion says what moved it. A replay that reproduced the confidence and the date but
            // attributed the change to a different channel would be reproducing the number and not
            // the history, and this comparator exists to refuse exactly that kind of near-miss.
            //
            // Claim.Kind/Subject/Object rather than Claim.ToString(), as everywhere in this file:
            // ToString embeds WorldEvent.Id and would make the fingerprint depend on scheduling.
            lines.AddRange(character.Cognition.Records.Select(r =>
                $"knowledge|{character.Id}|{r.Claim.Kind}|{r.Claim.Subject}|{r.Claim.Object}|" +
                $"{r.Stance}|{Number(r.Confidence)}|{r.SourceKind}|{r.SourceId}|{r.AcquiredAt:O}|" +
                $"{r.ReconsideredAt:O}|{r.Contested}|" +
                $"{(r.Reconsidered is { } b ? $"{b.Cause}:{b.Via}:{b.AboutId}" : "-")}"));

            // Claimed basis, not actual: this is the listener's log, and what the speaker really
            // had never enters it. The distinction is decision-relevant — it decides whether the
            // belief is first-hand testimony — so a run that changed it must fail this comparison.
            lines.AddRange(character.Cognition.Testimony.Select(t =>
                $"testimony|{character.Id}|{t.SenderId}|{t.Claim}|{t.AssertedStance}|" +
                $"{Number(t.AssertedConfidence)}|{t.ClaimedBasis}|{t.At:O}|{t.Disclaims}"));
        }

        return string.Join('\n', lines);
    }

    /// <summary>
    /// A comparator deliberately narrower than <see cref="Snapshot"/>: it omits every field that is
    /// itself a monotonic counter derived from ScheduledEvent.Id, WorldEvent.Id, Report.Id or
    /// Request.Id — and every free-text field, such as Candidate.Id or DecisionRecord.Outcome, that
    /// can embed one indirectly through Claim.ToString() printing a nonzero EventId. Those ids
    /// legitimately shift when an extra event is scheduled or an extra truth-log entry is recorded;
    /// that shift is exactly what the insertion-stability tests above must not mistake for a
    /// behavioural difference. What must not shift is which candidate was chosen, in what shape, and
    /// what the world and every character's knowledge end up looking like.
    /// </summary>
    private static string BehavioralSnapshot(World world)
    {
        static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        var lines = new List<string>();

        foreach (var d in world.Decisions)
            lines.Add($"decision|{d.ActorId}|{d.TriggerKind}|{d.Agenda.Kind}|{d.Agenda.Domain}|" +
                      $"{d.Chosen?.Candidate.Kind}|{d.Chosen?.Candidate.Strategy}|{d.Chosen?.Candidate.Method}|" +
                      $"{d.Chosen?.Candidate.TargetId}|{d.Chosen?.Candidate.Candor}");

        foreach (var business in world.Businesses.Values.OrderBy(b => b.Id, StringComparer.Ordinal))
            lines.Add($"business|{business.Id}|{Number(business.MonthlyRevenue)}|" +
                      $"{business.PayingTribute}|{business.TributeCollectedForCurrentAgreement}|" +
                      $"{Number(business.Resistance)}|{business.Damaged}");

        foreach (var character in world.Characters.Values.OrderBy(c => c.Id, StringComparer.Ordinal))
        {
            lines.Add($"character|{character.Id}|{character.Tier}|{character.DecisionCount}|" +
                      $"{character.StrategyCount}|{character.Execution.Strategy?.Kind}|" +
                      $"{character.Execution.Strategy?.TargetId}|{character.Execution.Strategy?.StepIndex}|" +
                      $"{character.Execution.Strategy?.NextAdvanceOrdinal}|" +
                      $"{character.Execution.Strategy?.PolicyBreachDecisionMakerId}|" +
                      string.Join(",", character.Execution.AttemptedConcealments.Select(a =>
                          $"{a.Kind}:{a.Subject}:{a.Object}")) +
                      "|" + string.Join(",", character.Execution.DelegatedExecutorIds));

            // As above, minus the grievance timestamp: a DateTime is not derived from any global
            // counter, but it is free text as far as this comparator is concerned and the narrower
            // one keeps to what a perturbation could not legitimately move.
            foreach (var rel in character.Social.All)
                lines.Add($"relationship|{character.Id}|{rel.OtherId}|{Number(rel.Trust)}|" +
                          $"{Number(rel.Fear)}|{Number(rel.Obligation)}|" +
                          // Milestone 023. Nothing scores it, so it cannot change a later decision —
                          // but it is persistent state that save/load has to reproduce, and a
                          // comparator that cannot see it would pass a run whose remembered history
                          // had diverged. The cause and the order are what carry meaning; the
                          // timestamp is included because a history that lost its dates would still
                          // compare equal without it.
                          string.Join(",", rel.StandingHistory.Select(h =>
                              // Kind/subject/object, never Claim.ToString() — that prints a nonzero
                              // EventId, which is a WorldEvent counter, which is precisely the class
                              // of field this comparator exists to exclude. Same shape as
                              // AttemptedConcealments above. Caught by the insertion-stability test.
                              $"{h.Cause}:{h.At:O}:{h.About?.Kind}:{h.About?.Subject}:{h.About?.Object}")) + "|" +
                          // Impression.ReportId deliberately excluded, unlike in Snapshot above: it is
                          // exactly the class of field this comparator's own header names — a
                          // Report.Id-derived counter that legitimately shifts when an unrelated
                          // report is scheduled elsewhere in the run, which is scheduling noise here
                          // rather than a behavioural difference.
                          string.Join(",", rel.Impressions.Select(i =>
                              $"{i.Kind}:{i.At:O}:{i.About?.Kind}:{i.About?.Subject}:{i.About?.Object}")) + "|" +
                          string.Join(",", rel.Grievances.Select(g => Number(g.Severity))));

            lines.AddRange(character.Cognition.Records.Select(r =>
                $"knowledge|{character.Id}|{r.Claim.Kind}|{r.Claim.Subject}|{r.Claim.Object}|" +
                $"{r.Stance}|{Number(r.Confidence)}|{r.SourceKind}|{r.SourceId}"));
        }

        return string.Join('\n', lines);
    }
}
