using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Scenario;
using CrimeSim.Sim;
using CrimeSim.Strategy;
using CrimeSim.Session;
using Xunit.Abstractions;

namespace CrimeEmpire.Simulation.Tests;

public sealed class OperationContinuityTests(ITestOutputHelper output)
{
    // Measured at seed 42, then pinned independently of candidate ranks. Every player
    // decision uses public button text; no fixture, truth, coefficient or deadline is changed.
    [Fact]
    public void The_unchanged_objective_is_reachable_through_legal_named_player_choices()
    {
        var choices = new[]
        {
            "use force on Ferri's tailor shop — breaking the rule: no public violence in the harbour",
            "use force on Bellini's grocery — breaking the rule: no public violence in the harbour",
            "carry on getting Bellini's grocery to pay",
            "carry on getting Bellini's grocery to pay",
            "carry on getting Bellini's grocery to pay",
            "report to Salvatore Greco, leaving out your own part",
            "ask Tommy Nardo what he knows about whether the outfit has a rule: no public violence in the harbour",
            "say nothing to Det. Iris Kane about it either way",
            "report the situation to Salvatore Greco",
            "report the situation to Salvatore Greco",
            "report the situation to Salvatore Greco",
            "cover it up before anyone finds out",
            "report the situation to Salvatore Greco",
            "take no action",
            "use force on Dorato's bakery — breaking the rule: no public violence in the harbour",
            "ask Tommy Nardo what he knows about whether somebody in the harbour still is not paying",
            "report to Salvatore Greco, leaving out your own part",
            "report the situation to Salvatore Greco",
            "say nothing to Det. Iris Kane about it either way",
            "report the situation to Salvatore Greco",
        };
        var session = SimulationSession.Start(42, "baseline", "vincent");
        foreach (string choice in choices)
        {
            for (int guard = 0; session.Pending is null && session.Status != SessionStatus.Resolved && guard < 1000; guard++)
                session.StepEvent();
            Assert.NotNull(session.Pending);
            session.Choose(session.Pending.Options.Single(o => o.Description == choice).Id);
        }
        session.AdvanceDays(90);
        Assert.Equal(SessionStatus.Resolved, session.Status);
        Assert.Equal(ObjectiveOutcome.ObjectiveMet, session.Result!.Outcome);
        Assert.True(session.World.Businesses[Cast.Grocery].PayingTribute);
        Assert.True(session.World.Businesses[Cast.Tailor].PayingTribute);
        Assert.True(session.World.Businesses[Cast.Bakery].PayingTribute);
    }

    [Fact]
    public void Asking_elsewhere_after_a_block_preserves_work_without_informing_the_absent_owner()
    {
        var world = Cast.Build(42, "baseline");
        var owner = world.Get("vincent");
        var executor = world.Get("tommy");
        var operation = new StrategyInstance
        {
            OwnerId = owner.Id, LocalSequence = owner.StrategyCount++, Kind = StrategyKind.SecureTribute,
            Domain = Cast.Harbour, TargetId = Cast.Grocery, DelegatedToId = executor.Id,
            StartedAt = world.Now, Deadline = world.Now.AddDays(30), StepIndex = 2, FailedAttempts = 1,
        };
        owner.Execution.Operations.Add(operation);
        executor.Cognition.Learn(new Claim(ClaimKind.BusinessRefusesTribute, Cast.Grocery),
            Stance.Knows, 1, SourceKind.Participant, executor.Id, world.Now);
        executor.Cognition.Receive(ReportedClaim.Honest(
            new Claim(ClaimKind.PolicyIssued, Cast.OrgId, "no-violence-harbour"),
            Stance.Believes, 0.8, SourceKind.Report), owner.Id, world.Now);
        string knownBefore = System.Text.Json.JsonSerializer.Serialize(owner.Cognition.Records);
        var prepared = Pipeline.Prepare(world, executor, new ScheduledEvent
        { Id = 234, Time = world.Now, Kind = EventKind.StrategyBlocked, OwnerId = executor.Id, Cause = "staged refusal",
          Payload = new EventPayload { StrategyOwnerId = owner.Id, StrategySequence = operation.LocalSequence } });
        var question = prepared.Available.First(c => c.Kind == ActionKind.SeekCorroboration && c.TargetId == "salvatore");
        Pipeline.Resolve(prepared, question.Id);
        Assert.NotNull(operation.PendingStepEventId);
        Assert.Same(operation, Strategies.CurrentExecution(world, executor));
        Assert.Equal(knownBefore, System.Text.Json.JsonSerializer.Serialize(owner.Cognition.Records));
        Assert.Null(operation.PendingReviewEventId);
    }

    [Fact]
    public void Natural_baseline_collects_both_opening_jobs_and_reaches_another_assignment()
    {
        var world = Cast.Build(42, "baseline");
        Runner.Run(world, Cast.Start.AddDays(90));
        Assert.True(world.Businesses[Cast.Grocery].PayingTribute);
        Assert.True(world.Businesses[Cast.Tailor].PayingTribute);
        Assert.True(world.TruthLog.Count(e => e.Kind == "assignment") >= 2);
        Assert.Contains(world.AccountConflicts, c => c.ListenerId == "vincent");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Seeking_approval_preserves_the_same_explicit_delay_for_a_personal_or_delegated_executor(bool delegated)
    {
        var world = Cast.Build(42, "baseline");
        while (world.Queue.Next(DateTime.MaxValue) is not null) { }
        var actor = world.Get("vincent");
        var owner = world.Get(delegated ? "salvatore" : "vincent");
        var operation = new StrategyInstance
        {
            OwnerId = owner.Id, LocalSequence = owner.StrategyCount++, Kind = StrategyKind.SecureTribute,
            Domain = Cast.Harbour, TargetId = Cast.Grocery, DelegatedToId = delegated ? actor.Id : null,
            StartedAt = world.Now, Deadline = world.Now.AddDays(30), FailedAttempts = 2, StepIndex = 2,
        };
        owner.Execution.Operations.Add(operation);
        var prepared = Pipeline.Prepare(world, actor, new ScheduledEvent
        { Id = 123, Time = world.Now, Kind = EventKind.StrategyBlocked, OwnerId = actor.Id, Cause = "test",
          Payload = new EventPayload { StrategyOwnerId = owner.Id, StrategySequence = operation.LocalSequence } });
        // Staged choice at the commitment boundary: this checks the scheduler's executor lookup,
        // not whether this particular fixture has received a restrictive policy yet.
        Commit.Apply(world, actor, new Candidate("approval", ActionKind.SeekApproval, "test", "ask")
            { TargetId = "salvatore", Domain = Cast.Harbour }, prepared.Agenda, prepared.Context, new List<string>());
        ScheduledEvent? step = null;
        while (world.Queue.Next(DateTime.MaxValue) is { } ev)
            if (ev.Id == operation.PendingStepEventId) step = ev;
        Assert.NotNull(step);
        Assert.Equal(world.Now.AddDays(5), step.Time);
        Assert.Equal(actor.Id, step.OwnerId);
    }

    [Theory]
    [InlineData("baseline")]
    [InlineData("cautious-vincent")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    [InlineData("resentful-tommy")]
    [InlineData("capable-angelo")]
    public void Measure_corrected_natural_outcomes(string variant)
    {
        var session = SimulationSession.Start(42, variant, null, "vincent");
        session.AdvanceDays(90);
        var world = session.World;
        output.WriteLine($"{variant}: {session.Result!.Outcome}; loss={world.Org.Condition(CrimeSim.Org.OrgCondition.RevenueLoss)}; " +
            $"conflicts={world.AccountConflicts.Count}; agreements={world.AccountAgreements.Count}; " +
            $"assignments={world.TruthLog.Count(e => e.Kind == "assignment")}; cash={world.Get("vincent").Capabilities.Cash}");
        Assert.Equal(SessionStatus.Resolved, session.Status);
    }

    [Theory]
    [InlineData("baseline")]
    [InlineData("cautious-vincent")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    [InlineData("resentful-tommy")]
    [InlineData("capable-angelo")]
    public void Resolving_a_block_cannot_strand_the_executors_still_active_operation(string variant)
    {
        var world = Cast.Build(42, variant);
        int blocks = 0;
        while (true)
        {
            var step = Runner.Step(world, Cast.Start.AddDays(90), null);
            if (step.Status == StepStatus.Exhausted) break;
            if (step.Event!.Kind != EventKind.StrategyBlocked) continue;
            blocks++;
            var executor = world.Get(step.Event.OwnerId!);
            if (Strategies.CurrentExecution(world, executor) is not { } live) continue;
            Assert.True(live.PendingStepEventId is { } id && !world.Queue.Cancelled.ContainsKey(id),
                $"{variant}: {executor.Id} stranded {live.OwnerId}/{live.LocalSequence} at {world.Now:O}");
        }
        Assert.True(blocks > 0, "The natural run must actually exercise the blocked path.");
    }

    [Fact]
    public void Prospective_target_keeps_belief_acquisition_order()
    {
        var world = Cast.Build(42, "baseline");
        var vincent = world.Get("vincent");
        vincent.Cognition.Learn(new Claim(ClaimKind.BusinessRefusesTribute, Cast.Grocery),
            Stance.Knows, 1, SourceKind.Discovery, vincent.Id, world.Now);
        var prepared = Pipeline.Prepare(world, vincent, new ScheduledEvent
        { Id = 123, Time = world.Now, Kind = EventKind.RoleReview, OwnerId = vincent.Id, Cause = "test" });
        var targets = prepared.Generated.Where(c => c.Kind == ActionKind.StartStrategy
            && c.Strategy == StrategyKind.SecureTribute).Select(c => c.TargetId).Distinct();
        Assert.Equal(new[] { Cast.Tailor, Cast.Grocery }, targets);
    }
}
