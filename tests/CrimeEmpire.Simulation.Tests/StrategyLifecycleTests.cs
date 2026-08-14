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
            ReportsSent: Array.Empty<Report>(),
            RequestsMade: Array.Empty<InformationRequest>(),
            VisibleTargets: Array.Empty<string>());
    }
}
