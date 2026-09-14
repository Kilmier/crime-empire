using CrimeEmpire.Persistence.Session;
using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Scenario;
using CrimeSim.Sim;
using CrimeSim.Strategy;

namespace CrimeEmpire.Simulation.Tests;

public sealed class InformationTriggeredReviewTests
{
    private static (World World, StrategyInstance Operation) Setup()
    {
        var world = Cast.Build(42, "baseline");
        while (world.Queue.Next(DateTime.MaxValue) is not null) { } // isolate the staged receipt
        var owner = world.Get("vincent");
        var operation = new StrategyInstance
        {
            OwnerId = owner.Id, LocalSequence = owner.StrategyCount++, Kind = StrategyKind.SecureTribute,
            Domain = Cast.Harbour, TargetId = Cast.Grocery, DelegatedToId = "tommy",
            StartedAt = world.Now, Deadline = world.Now.AddDays(30),
        };
        owner.Execution.Operations.Add(operation);
        Strategies.ScheduleNextStep(world, operation, "staged standing work");
        Strategies.ScheduleReview(world, operation);
        return (world, operation);
    }

    private static void Deliver(World world, Claim claim, bool withheld = false)
        => Reporting.Deliver(world, new Report(world.NextReportId(), "tommy", "vincent", world.Now,
            withheld ? ReportCandor.Partial : ReportCandor.Candid,
            withheld ? Array.Empty<ReportedClaim>() : new[]
                { ReportedClaim.Honest(claim, Stance.Believes, 0.8, SourceKind.Participant) },
            withheld ? new[] { claim } : Array.Empty<Claim>(), "test account"), world.Get("vincent"));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Received_news_causes_a_real_review_before_the_weekly_occasion(bool controlled)
    {
        var (world, operation) = Setup();
        long oldReview = operation.PendingReviewEventId!.Value;
        long execution = operation.PendingStepEventId!.Value;
        Deliver(world, new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery, 101));
        Assert.True(world.Queue.Cancelled.ContainsKey(oldReview));
        long early = operation.PendingReviewEventId!.Value;
        var step = Runner.Step(world, world.Now, controlled ? "vincent" : null);
        Assert.Equal(early, step.Event!.Id);
        Assert.Equal("operation-review", step.Event.Payload.Note);
        Assert.Equal(execution, operation.PendingStepEventId);
        if (controlled)
        {
            Assert.NotNull(step.Awaiting);
            Assert.Contains(step.Awaiting.Available, c => c.Kind == ActionKind.AbandonStrategy);
        }
        else Assert.Contains(world.Decisions, d => d.ActorId == "vincent" && d.At == world.Now);
    }

    [Fact]
    public void Same_received_information_and_same_review_choice_produce_identical_player_and_npc_state()
    {
        var (automatic, _) = Setup();
        var (controlled, _) = Setup();
        var claim = new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery, 101);
        Deliver(automatic, claim);
        Deliver(controlled, claim);
        var pause = Runner.Step(controlled, controlled.Now, "vincent");
        var decision = Assert.IsType<PreparedDecision>(pause.Awaiting);
        string chosen = decision.Scored[0].Candidate.Id;
        Pipeline.Resolve(decision, chosen);
        var npc = Runner.Step(automatic, automatic.Now, null);
        Assert.Equal(pause.Event!.Time, npc.Event!.Time);
        Assert.Equal(ControlledAutonomousParityTests.ComprehensiveFingerprint(automatic),
            ControlledAutonomousParityTests.ComprehensiveFingerprint(controlled));
    }

    [Fact]
    public void Hidden_progress_withholding_and_unrelated_news_do_not_wake_the_owner()
    {
        var (world, operation) = Setup();
        long scheduled = operation.PendingReviewEventId!.Value;
        operation.FailedAttempts = 12;
        operation.StepIndex = 2;
        world.Get("tommy").Cognition.Learn(new Claim(ClaimKind.TributeCollected, Cast.Grocery),
            Stance.Knows, 1, SourceKind.Participant, "tommy", world.Now);
        Deliver(world, new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery, 101), withheld: true);
        Deliver(world, new Claim(ClaimKind.TributeCollected, Cast.Bakery));
        Assert.Equal(scheduled, operation.PendingReviewEventId);
        Assert.Equal(StepStatus.Exhausted, Runner.Step(world, world.Now, null).Status);
    }

    [Fact]
    public void An_operation_without_a_named_target_does_not_match_an_unrelated_subject_only_claim()
    {
        var (world, _) = Setup();
        while (world.Queue.Next(DateTime.MaxValue) is not null) { }
        world.Get("vincent").Execution.Operations.Clear();
        var operation = new StrategyInstance
        {
            OwnerId = "vincent", LocalSequence = world.Get("vincent").StrategyCount++, Kind = StrategyKind.ConcealIncident,
            Domain = Cast.Harbour, TargetId = null, DelegatedToId = "tommy",
            StartedAt = world.Now, Deadline = world.Now.AddDays(30),
        };
        world.Get("vincent").Execution.Operations.Add(operation);
        Strategies.ScheduleReview(world, operation);
        long scheduled = operation.PendingReviewEventId!.Value;
        Deliver(world, new Claim(ClaimKind.TributeCollected, Cast.Bakery));
        Assert.Equal(scheduled, operation.PendingReviewEventId);
    }

    [Fact]
    public void Repeated_account_does_not_schedule_another_review()
    {
        var (world, operation) = Setup();
        var claim = new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery, 101);
        Deliver(world, claim);
        var review = Runner.Step(world, world.Now, "vincent").Awaiting!;
        Pipeline.Resolve(review, review.Available.Single(c => c.Kind == ActionKind.ContinueStrategy).Id);
        long next = operation.PendingReviewEventId!.Value;
        Deliver(world, claim);
        Assert.Equal(next, operation.PendingReviewEventId);
        Assert.Equal(StepStatus.Exhausted, Runner.Step(world, world.Now, null).Status);
    }

    [Fact]
    public void Successful_observation_opens_review_but_an_unreceived_opportunity_does_not()
    {
        foreach (bool received in new[] { false, true })
        {
            var (world, operation) = Setup();
            long scheduled = operation.PendingReviewEventId!.Value;
            world.Queue.Schedule(world.Now, EventKind.ObservationOpportunity, "vincent", "test opportunity",
                new EventPayload { OccasionKey = "test|observation", Discoverability = received ? 10 : 0,
                    AcquiredAs = SourceKind.Rumor,
                    Claims = new[] { new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery, 101) } });
            Runner.Step(world, world.Now, null);
            if (received)
            {
                Assert.NotEqual(scheduled, operation.PendingReviewEventId);
                Assert.NotNull(Runner.Step(world, world.Now, "vincent").Awaiting);
            }
            else Assert.Equal(scheduled, operation.PendingReviewEventId);
        }
    }

    [Fact]
    public void Actual_report_generated_early_review_survives_save_and_replay()
    {
        var session = PersistentSession.Start(42, "baseline", "tommy");
        for (int guard = 0; session.Pending is null && guard < 1000; guard++) session.StepEvent();
        session.Choose(session.Pending!.Options.Single(o =>
            o.Description == "report the situation to Vincent Russo").Id);
        var world = session.InnerSession.World;
        var operation = Assert.Single(world.Get("vincent").Execution.Operations, s => s.DelegatedToId == "tommy");
        Assert.NotNull(operation.PendingReviewEventId);
        string path = Path.Combine(Path.GetTempPath(), $"m028-early-review-{Guid.NewGuid():N}.db");
        try
        {
            session.Save(path);
            var loaded = PersistentSession.Load(path);
            Assert.Equal(ControlledAutonomousParityTests.ComprehensiveFingerprint(world),
                ControlledAutonomousParityTests.ComprehensiveFingerprint(loaded.InnerSession.World));
            var prior = operation.PendingReviewEventId;
            session.StepEvent();
            loaded.StepEvent();
            Assert.Contains(world.Decisions, d => d.ActorId == "vincent" && d.At == session.Date);
            Assert.NotEqual(prior, operation.PendingReviewEventId);
            Assert.Equal(ControlledAutonomousParityTests.ComprehensiveFingerprint(world),
                ControlledAutonomousParityTests.ComprehensiveFingerprint(loaded.InnerSession.World));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}
