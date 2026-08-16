using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Scenario;
using CrimeSim.Sim;
using CrimeSim.Strategy;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>
/// Milestone 005: strategy-instance identity, stale-event rejection, and the concealment
/// redundancy rule. See docs/CURRENT_MILESTONE.md for the invariant each of these pins.
///
/// Insertion-stability itself — the proof that occasion keys are causally local rather than
/// derived from ScheduledEvent.Id or WorldEvent.Id — lives in SimulationReplayTests.cs, since it
/// needs the full-run comparator that file already owns.
/// </summary>
public sealed class StrategyLifecycleTests
{
    // ---------------------------------------------------------------- stale-event validation

    [Fact]
    public void A_valid_step_event_advances_without_throwing()
    {
        var (world, vincent, s) = FreshRunningStrategy();
        var ev = ValidStepEvent(vincent, s);
        long originalPending = ev.Id;

        var exception = Record.Exception(() => Strategies.Advance(world, vincent, ev));

        Assert.Null(exception);
        Assert.Equal(1, s.NextAdvanceOrdinal);
        Assert.NotNull(s.PendingStepEventId);
        Assert.NotEqual(originalPending, s.PendingStepEventId);
    }

    [Fact]
    public void A_step_event_naming_a_nonexistent_owner_throws()
    {
        var (world, vincent, s) = FreshRunningStrategy();
        var ev = StepEvent(vincent, s, ownerId: "nobody");
        Assert.Throws<SimulationInvariantException>(() => Strategies.Advance(world, vincent, ev));
    }

    [Fact]
    public void A_step_event_with_a_stale_sequence_throws()
    {
        var (world, vincent, s) = FreshRunningStrategy();
        var ev = StepEvent(vincent, s, sequence: s.LocalSequence + 1);
        Assert.Throws<SimulationInvariantException>(() => Strategies.Advance(world, vincent, ev));
    }

    [Fact]
    public void A_step_event_with_a_stale_ordinal_throws()
    {
        var (world, vincent, s) = FreshRunningStrategy();
        var ev = StepEvent(vincent, s, ordinal: s.NextAdvanceOrdinal + 1);
        Assert.Throws<SimulationInvariantException>(() => Strategies.Advance(world, vincent, ev));
    }

    [Fact]
    public void A_step_event_that_is_not_the_pending_event_throws()
    {
        var (world, vincent, s) = FreshRunningStrategy();
        var ev = StepEvent(vincent, s, id: s.PendingStepEventId!.Value + 999);
        Assert.Throws<SimulationInvariantException>(() => Strategies.Advance(world, vincent, ev));
    }

    [Fact]
    public void A_step_event_delivered_to_the_wrong_executor_throws()
    {
        var (world, vincent, s) = FreshRunningStrategy();
        var tommy = world.Get("tommy");
        var ev = StepEvent(vincent, s); // built as though addressed to Vincent, the owner
        // Delivered as though Tommy were the one who woke — nobody delegated this to him.
        Assert.Throws<SimulationInvariantException>(() => Strategies.Advance(world, tommy, ev));
    }

    /// <summary>
    /// A stale event that never reaches Advance at all — the everyday case. Abandonment cancels
    /// the pending step, and EventQueue.Next skips a cancelled event before delivery, so running
    /// the loop past when it was due must not throw.
    /// </summary>
    [Fact]
    public void A_properly_cancelled_step_is_skipped_and_never_throws()
    {
        var (world, vincent, s) = FreshRunningStrategy();
        world.Queue.Cancel(s.PendingStepEventId!.Value, "test: abandonment");
        vincent.Execution.Strategy = null;

        var exception = Record.Exception(() => Runner.Run(world, world.Now.AddDays(10)));

        Assert.Null(exception);
    }

    /// <summary>
    /// A stale event that reaches Advance for a genuine reason: the owner started a replacement
    /// before the originally scheduled event fired. Constructed by hand rather than through the
    /// real queue, standing in for whatever upstream failure could leave a stale delivery.
    /// </summary>
    [Fact]
    public void A_stale_event_from_a_replaced_instance_throws_rather_than_advancing_it()
    {
        var (world, vincent, s) = FreshRunningStrategy();
        var stale = ValidStepEvent(vincent, s);

        vincent.Execution.Strategy = new StrategyInstance
        {
            OwnerId = vincent.Id,
            LocalSequence = vincent.StrategyCount++,
            Kind = StrategyKind.ConcealIncident,
            Domain = Cast.Harbour,
            TargetId = Cast.Grocery,
            StartedAt = world.Now,
            Deadline = world.Now.AddDays(30),
        };

        Assert.Throws<SimulationInvariantException>(() => Strategies.Advance(world, vincent, stale));
    }

    // ---------------------------------------------------------------- explicit executor resolution

    /// <summary>
    /// Codex correction, finding 3. A StrategyStep with no owner used to fall through Runner's
    /// `when actor is not null` guard silently — the strategy would simply sit inert forever with
    /// no trace of why. Exercised through Runner and the real EventQueue, not by calling
    /// Strategies.Advance directly, since the defect was in dispatch, not in Advance itself.
    /// </summary>
    [Fact]
    public void A_strategy_step_event_with_no_owner_throws_through_the_runner()
    {
        var world = Cast.Build(seed: 1, "baseline");
        world.Queue.Schedule(world.Now, EventKind.StrategyStep, null, "test: no owner",
            new EventPayload { StrategyOwnerId = "vincent", StrategySequence = 0, AdvanceOrdinal = 0 });

        Assert.Throws<SimulationInvariantException>(() => Runner.Run(world, world.Now.AddDays(1)));
    }

    /// <summary>Same defect, the other way an owner can fail to resolve: naming nobody real.</summary>
    [Fact]
    public void A_strategy_step_event_naming_an_unknown_executor_throws_through_the_runner()
    {
        var world = Cast.Build(seed: 1, "baseline");
        world.Queue.Schedule(world.Now, EventKind.StrategyStep, "nobody", "test: unknown owner",
            new EventPayload { StrategyOwnerId = "vincent", StrategySequence = 0, AdvanceOrdinal = 0 });

        Assert.Throws<SimulationInvariantException>(() => Runner.Run(world, world.Now.AddDays(1)));
    }

    // ---------------------------------------------------------------- continue preserves scheduled work

    /// <summary>
    /// Codex correction, finding 2. ScheduleNextStep always cancels-and-reschedules, which is
    /// correct for Alter/Delegate/Postpone/SeekApproval — each deliberately changes the timing —
    /// but wrong for Continue: a character woken early by an unrelated trigger who simply chooses
    /// to carry on was silently pushing the strategy's next step a full interval further out every
    /// time, which could delay it indefinitely under repeated early wakes.
    /// </summary>
    [Fact]
    public void Continuing_leaves_an_existing_pending_step_untouched()
    {
        var (world, vincent, s) = FreshRunningStrategy();
        long originalPending = s.PendingStepEventId!.Value;

        var ctx = MinimalContext(world, vincent);
        var continueCandidate = new Candidate(
            $"continue:{s.Kind}:{s.TargetId}", ActionKind.ContinueStrategy, "test", "carry on")
        {
            TargetId = s.TargetId,
            Strategy = s.Kind,
        };
        Commit.Apply(world, vincent, continueCandidate, ctx.Agenda, ctx, new List<string>());

        Assert.Equal(originalPending, s.PendingStepEventId);
        Assert.False(world.Queue.Cancelled.ContainsKey(originalPending),
            "continuing must not cancel the step that is already due");
    }

    /// <summary>Positive control: Continue must still schedule a step when nothing is currently pending.</summary>
    [Fact]
    public void Continuing_schedules_a_step_when_none_is_pending()
    {
        var (world, vincent, s) = FreshRunningStrategy();
        world.Queue.Cancel(s.PendingStepEventId!.Value, "test: simulate nothing pending");

        var ctx = MinimalContext(world, vincent);
        var continueCandidate = new Candidate(
            $"continue:{s.Kind}:{s.TargetId}", ActionKind.ContinueStrategy, "test", "carry on")
        {
            TargetId = s.TargetId,
            Strategy = s.Kind,
        };
        Commit.Apply(world, vincent, continueCandidate, ctx.Agenda, ctx, new List<string>());

        Assert.NotNull(s.PendingStepEventId);
        Assert.False(world.Queue.Cancelled.ContainsKey(s.PendingStepEventId!.Value));
    }

    // ---------------------------------------------------------------- observation key uniqueness

    /// <summary>
    /// Codex correction, finding 5 — the run-wide test the original plan promised but never wrote.
    /// (instance, ordinal, traceKind, observer) must identify at most one observation opportunity;
    /// this asserts it directly against every key actually used across a full run, rather than only
    /// arguing for it in prose.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("cautious-vincent")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    public void No_two_observation_opportunities_reuse_an_occasion_key(string variant)
    {
        var world = Cast.Build(seed: 42, variant);
        Runner.Run(world, Cast.Start.AddDays(90));

        // Not asserted non-empty: cautious-vincent never has Vincent use force, so ResolveViolence
        // never fires and this variant schedules no observation opportunities at all. An empty list
        // is trivially unique, and that is a fact about the variant, not a gap in this check.
        var duplicates = world.ObservationOccasionKeys
            .GroupBy(k => k, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        Assert.Empty(duplicates);
    }

    // ---------------------------------------------------------------- delegate commitment cleanup

    [Fact]
    public void Delegate_commitments_are_removed_when_the_strategy_completes()
    {
        var (world, vincent, s, tommy, id) = RunningDelegatedStrategy();

        Strategies.Complete(world, vincent, s, "test: forced completion");

        Assert.DoesNotContain(vincent.Execution.Commitments, c => c.Id == id);
        Assert.DoesNotContain(tommy.Execution.Commitments, c => c.Id == id);
    }

    [Fact]
    public void Delegate_commitments_are_removed_on_abandonment()
    {
        var (world, vincent, s, tommy, id) = RunningDelegatedStrategy();

        var ctx = MinimalContext(world, vincent);
        var abandon = new Candidate($"abandon:{s.Kind}:{s.TargetId}", ActionKind.AbandonStrategy, "test", "drop it")
        {
            TargetId = s.TargetId,
            Strategy = s.Kind,
            Domain = s.Domain,
        };
        Commit.Apply(world, vincent, abandon, ctx.Agenda, ctx, new List<string>());

        Assert.DoesNotContain(vincent.Execution.Commitments, c => c.Id == id);
        Assert.DoesNotContain(tommy.Execution.Commitments, c => c.Id == id);
    }

    // ---------------------------------------------------------------- concealment redundancy

    /// <summary>
    /// Commit records the attempt; Filters enforces the MVP rule in both the running and the
    /// completed state. Driven directly through Commit and Filters rather than through the full
    /// pipeline's utility competition, which is a separate question (whether concealment is the
    /// character's *preferred* option) from the one this milestone is about (whether it can be
    /// offered again once he has already tried).
    /// </summary>
    [Fact]
    public void The_same_incident_starts_concealment_once_across_running_and_completed_states()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var tommy = world.Get("tommy");
        var incident = new Claim(ClaimKind.PersonUsedViolence, tommy.Id, Cast.Grocery, 7);
        var ctx = MinimalContext(world, tommy);

        var candidate = new Candidate("conceal:test", ActionKind.StartStrategy, "test", "clean it up")
        {
            Strategy = StrategyKind.ConcealIncident,
            TargetId = Cast.Grocery,
            AboutIncident = incident,
        };

        Commit.Apply(world, tommy, candidate, ctx.Agenda, ctx, new List<string>());
        Assert.NotNull(tommy.Execution.Strategy);
        Assert.Equal(StrategyKind.ConcealIncident, tommy.Execution.Strategy!.Kind);
        Assert.Contains(incident, tommy.Execution.AttemptedConcealments);

        // Still running: offering it again must be refused, not merely lose on salience or utility.
        var whileRunning = Filters.Apply(ctx, new[] { candidate }, new SalienceProfile());
        Assert.Empty(whileRunning.Passed);
        Assert.Contains(whileRunning.Rejected, r => r.Stage == RejectionStage.Redundancy);

        Strategies.Complete(world, tommy, tommy.Execution.Strategy!, "test: forced completion");
        Assert.Null(tommy.Execution.Strategy);

        // Finished, not running — the incident is still spent. A (Kind, TargetId) match to a
        // running instance could not have expressed this; nothing is running any more.
        var afterCompletion = Filters.Apply(ctx, new[] { candidate }, new SalienceProfile());
        Assert.Empty(afterCompletion.Passed);
        Assert.Contains(afterCompletion.Rejected, r => r.Stage == RejectionStage.Redundancy);
    }

    /// <summary>
    /// Codex correction, finding 1. A location is not an incident: two separate beatings at the
    /// same shop are two different things to cover up. A (Kind, TargetId) match to the running
    /// instance would wrongly treat the second as a restart of the first and refuse it; the
    /// redundancy check for ConcealIncident must be scoped to AboutIncident instead, and a
    /// genuinely different incident at the same target must remain eligible and may replace the
    /// running instance.
    /// </summary>
    [Fact]
    public void A_different_incident_at_the_same_target_remains_eligible_and_may_replace_the_running_one()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var tommy = world.Get("tommy");
        var firstIncident = new Claim(ClaimKind.PersonUsedViolence, tommy.Id, Cast.Grocery, 3);
        var ctx = MinimalContext(world, tommy);

        var first = new Candidate("conceal:first", ActionKind.StartStrategy, "test", "clean up the first one")
        {
            Strategy = StrategyKind.ConcealIncident,
            TargetId = Cast.Grocery,
            AboutIncident = firstIncident,
        };
        Commit.Apply(world, tommy, first, ctx.Agenda, ctx, new List<string>());
        var firstInstance = tommy.Execution.Strategy!;

        // Same target, different EventId — a later beating at the same shop.
        var secondIncident = new Claim(ClaimKind.PersonUsedViolence, tommy.Id, Cast.Grocery, 9);
        var second = new Candidate("conceal:second", ActionKind.StartStrategy, "test", "clean up the second one")
        {
            Strategy = StrategyKind.ConcealIncident,
            TargetId = Cast.Grocery,
            AboutIncident = secondIncident,
        };

        // Not blocked at Filters, despite matching the running instance's (Kind, TargetId).
        var result = Filters.Apply(ctx, new[] { second }, new SalienceProfile());
        Assert.Contains(second, result.Passed);
        Assert.DoesNotContain(result.Rejected, r => r.Stage == RejectionStage.Redundancy);

        // And starting it replaces the running instance.
        Commit.Apply(world, tommy, second, ctx.Agenda, ctx, new List<string>());
        var secondInstance = tommy.Execution.Strategy!;
        Assert.NotEqual(firstInstance.LocalSequence, secondInstance.LocalSequence);
        Assert.Contains(firstIncident, tommy.Execution.AttemptedConcealments);
        Assert.Contains(secondIncident, tommy.Execution.AttemptedConcealments);
    }

    /// <summary>Positive control: the redundancy rule is scoped to the incident, not to the strategy kind.</summary>
    [Fact]
    public void A_different_incidents_candidate_is_not_rejected_as_redundant()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var tommy = world.Get("tommy");
        tommy.Execution.AttemptedConcealments.Add(
            new Claim(ClaimKind.PersonUsedViolence, tommy.Id, Cast.Grocery, 3));

        var different = new Candidate("conceal:different", ActionKind.StartStrategy, "test", "clean up the other one")
        {
            Strategy = StrategyKind.ConcealIncident,
            TargetId = "another-shop",
            AboutIncident = new Claim(ClaimKind.PersonUsedViolence, tommy.Id, "another-shop", 4),
        };

        var ctx = MinimalContext(world, tommy);
        var result = Filters.Apply(ctx, new[] { different }, new SalienceProfile());

        Assert.DoesNotContain(result.Rejected, r => r.Stage == RejectionStage.Redundancy);
    }

    /// <summary>
    /// The redundancy stage runs before candidates are ranked and capped, so a redundant duplicate
    /// can never take one of the bounded slots away from a genuinely different option — it is
    /// excluded before ranking even begins, regardless of how salient it would otherwise be.
    /// </summary>
    [Fact]
    public void A_redundant_candidate_cannot_crowd_out_a_legitimate_one_at_the_cap()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var tommy = world.Get("tommy");
        tommy.Execution.Strategy = new StrategyInstance
        {
            OwnerId = tommy.Id,
            LocalSequence = tommy.StrategyCount++,
            Kind = StrategyKind.ConcealIncident,
            Domain = Cast.Harbour,
            TargetId = Cast.Grocery,
            StartedAt = world.Now,
            Deadline = world.Now.AddDays(30),
        };

        var redundant = new Candidate("conceal:redundant", ActionKind.StartStrategy, "test", "start it again")
        {
            Strategy = StrategyKind.ConcealIncident,
            TargetId = Cast.Grocery,
        };

        // More equally-salient legitimate options than the cap allows, so a redundant candidate
        // that were still competing on salience could take a slot one of these should have had.
        var others = Enumerable.Range(0, SalienceProfile.MaxCandidates + 3)
            .Select(i => new Candidate($"other:{i}", ActionKind.DoNothing, "test", $"option {i}"))
            .ToList();

        var ctx = MinimalContext(world, tommy);
        var candidates = others.Append(redundant).ToList();

        var result = Filters.Apply(ctx, candidates, new SalienceProfile());

        Assert.DoesNotContain(result.Passed, c => c.Id == redundant.Id);
        Assert.Contains(result.Rejected, r => r.Candidate.Id == redundant.Id && r.Stage == RejectionStage.Redundancy);
        Assert.Equal(SalienceProfile.MaxCandidates, result.Passed.Count);
    }

    /// <summary>
    /// Codex correction, finding 4, filtering side. A ConcealIncident candidate with no incident
    /// attached cannot be checked against AttemptedConcealments at all, which would let it start
    /// without ever being recorded — the exact gap the MVP rule exists to close. Must fail closed:
    /// refused, not merely unscored.
    /// </summary>
    [Fact]
    public void A_conceal_candidate_with_no_incident_is_rejected_at_filtering()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var tommy = world.Get("tommy");
        var ctx = MinimalContext(world, tommy);

        var unlabelled = new Candidate("conceal:unlabelled", ActionKind.StartStrategy, "test", "clean up something")
        {
            Strategy = StrategyKind.ConcealIncident,
            TargetId = Cast.Grocery,
            // AboutIncident deliberately left unset.
        };

        var result = Filters.Apply(ctx, new[] { unlabelled }, new SalienceProfile());

        Assert.Empty(result.Passed);
        Assert.Contains(result.Rejected, r => r.Candidate.Id == unlabelled.Id && r.Stage == RejectionStage.Redundancy);
    }

    /// <summary>
    /// Codex correction, finding 4, commit side — the second layer for whatever might reach Commit
    /// without going through Filters first, e.g. a hand-built candidate. Must throw rather than
    /// start an untracked concealment, and must leave no partial state behind.
    /// </summary>
    [Fact]
    public void A_conceal_candidate_with_no_incident_throws_at_the_commit_boundary()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var tommy = world.Get("tommy");
        var ctx = MinimalContext(world, tommy);

        var unlabelled = new Candidate("conceal:unlabelled", ActionKind.StartStrategy, "test", "clean up something")
        {
            Strategy = StrategyKind.ConcealIncident,
            TargetId = Cast.Grocery,
        };

        Assert.Throws<SimulationInvariantException>(
            () => Commit.Apply(world, tommy, unlabelled, ctx.Agenda, ctx, new List<string>()));
        Assert.Null(tommy.Execution.Strategy);
        Assert.Empty(tommy.Execution.AttemptedConcealments);
    }

    /// <summary>Wiring check: the generator that proposes concealment actually names the incident it is about.</summary>
    [Fact]
    public void FromPressure_names_the_incident_a_concealment_candidate_is_about()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var tommy = world.Get("tommy");
        var incident = new Claim(ClaimKind.PersonUsedViolence, tommy.Id, Cast.Grocery, 7);
        tommy.Cognition.Learn(incident, Stance.Knows, 1.0, SourceKind.Participant, tommy.Id, world.Now);
        tommy.Motivations.AddPressure(PressureKind.LegalExposure, 0.5);

        var ctx = MinimalContext(world, tommy);
        var generated = Generators.GenerateAll(ctx);

        var conceal = generated.FirstOrDefault(c => c.Strategy == StrategyKind.ConcealIncident);
        Assert.NotNull(conceal);
        Assert.Equal(incident, conceal!.AboutIncident);
    }

    // ---------------------------------------------------------------- helpers

    private static (World World, Character Vincent, StrategyInstance Strategy) FreshRunningStrategy()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var vincent = world.Get("vincent");
        var s = new StrategyInstance
        {
            OwnerId = vincent.Id,
            LocalSequence = vincent.StrategyCount++,
            Kind = StrategyKind.SecureTribute,
            Domain = Cast.Harbour,
            TargetId = Cast.Grocery,
            StartedAt = world.Now,
            Deadline = world.Now.AddDays(30),
        };
        vincent.Execution.Strategy = s;
        Strategies.ScheduleNextStep(world, s, "test: first step");
        return (world, vincent, s);
    }

    private static (World World, Character Vincent, StrategyInstance Strategy, Character Tommy, string CommitmentId)
        RunningDelegatedStrategy()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var vincent = world.Get("vincent");
        var tommy = world.Get("tommy");
        var s = new StrategyInstance
        {
            OwnerId = vincent.Id,
            LocalSequence = vincent.StrategyCount++,
            Kind = StrategyKind.SecureTribute,
            Domain = Cast.Harbour,
            TargetId = Cast.Grocery,
            StartedAt = world.Now,
            Deadline = world.Now.AddDays(30),
            DelegatedToId = tommy.Id,
        };
        vincent.Execution.Strategy = s;
        string id = $"strategy:{s.OwnerId}:{s.LocalSequence}";
        vincent.Execution.Commitments.Add(new Commitment(id, "test", null, world.Now, 0.6));
        tommy.Execution.Commitments.Add(new Commitment(id, "test: delegate", vincent.Id, world.Now, 0.7));
        return (world, vincent, s, tommy, id);
    }

    private static ScheduledEvent ValidStepEvent(Character executor, StrategyInstance s) => StepEvent(executor, s);

    private static ScheduledEvent StepEvent(
        Character executor, StrategyInstance s,
        string? ownerId = null, int? sequence = null, int? ordinal = null, long? id = null)
        => new()
        {
            Id = id ?? s.PendingStepEventId!.Value,
            Time = default,
            Kind = EventKind.StrategyStep,
            OwnerId = executor.Id,
            Cause = "test",
            Payload = new EventPayload
            {
                StrategyOwnerId = ownerId ?? s.OwnerId,
                StrategySequence = sequence ?? s.LocalSequence,
                AdvanceOrdinal = ordinal ?? s.NextAdvanceOrdinal,
                Strategy = s.Kind,
            },
        };

    /// <summary>
    /// A minimal but valid GeneratorContext for testing Filters/Generators directly rather than
    /// through the whole pipeline — the same rationale as Generators.CanAsk being public and
    /// parameterised: a rule buried inside end-to-end utility competition is a rule that can only
    /// be pinned by getting lucky with a seed.
    /// </summary>
    private static GeneratorContext MinimalContext(World world, Character actor)
    {
        var perceived = Salience.Perceive(actor, world.Now);
        var trigger = new ScheduledEvent
        {
            Id = 0,
            Time = world.Now,
            Kind = EventKind.RoleReview,
            OwnerId = actor.Id,
            Cause = "test",
        };
        return new GeneratorContext(
            actor.View,
            perceived,
            new Agenda(AgendaKind.Idle, "test", "test"),
            world.Now,
            trigger,
            MyOffice: null,
            MyAssignment: null,
            KnownPolicies: Array.Empty<Policy>(),
            SuperiorId: null,
            SubordinateIds: Array.Empty<string>(),
            OrgMemberIds: Array.Empty<string>(),
            AcquaintedIds: Array.Empty<string>(),
            ReportsSent: Array.Empty<Report>(),
            RequestsMade: Array.Empty<InformationRequest>(),
            VisibleTargets: Array.Empty<string>());
    }
}
