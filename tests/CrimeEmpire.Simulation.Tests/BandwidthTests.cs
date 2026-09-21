using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Scenario;
using CrimeSim.Session;
using CrimeSim.Sim;
using CrimeSim.Strategy;
using Xunit.Abstractions;
using CrimeEmpire.Persistence.Session;
using System.Text.Json;

namespace CrimeEmpire.Simulation.Tests;

public sealed class BandwidthTests(ITestOutputHelper output)
{
    [Fact]
    public void An_old_executors_prepared_choice_cannot_change_a_reassigned_operation()
    {
        var session = Concurrent("capable-angelo");
        var world = session.World;
        var tommy = world.Get("tommy");
        var s = Strategies.CurrentExecution(world, tommy)!;
        var prepared = Pipeline.Prepare(world, tommy, new ScheduledEvent
        { Id = 1234, Time = world.Now, Kind = EventKind.RoleReview, OwnerId = tommy.Id, Cause = "test" });
        var continued = prepared.Generated.Single(c => c.Kind == ActionKind.ContinueStrategy);
        session.ReviewOperation("work-0");
        session.ChooseAndConfirm(session.Pending!.Options.Single(o => o.Description == "hand it to Angelo Conti").Id);
        string before = ControlledAutonomousParityTests.ComprehensiveFingerprint(world);
        Assert.Throws<SimulationInvariantException>(() => Commit.Apply(world, tommy, continued,
            prepared.Agenda, prepared.Context, new List<string>()));
        Assert.Equal(before, ControlledAutonomousParityTests.ComprehensiveFingerprint(world));
        Assert.Equal("angelo", s.DelegatedToId);
    }

    [Fact]
    public void Two_subordinates_allow_two_supervised_operations_and_a_personal_operation()
    {
        var session = Concurrent("capable-angelo");
        var owner = session.World.Get("vincent");
        session.ReviewOperation("work-1");
        session.ChooseAndConfirm(session.Pending!.Options.Single(o => o.Description == "hand it to Angelo Conti").Id);
        // The third known refusal is staged to test capacity, not claimed as a natural opening.
        owner.Cognition.Learn(new Claim(ClaimKind.BusinessRefusesTribute, Cast.Bakery),
            Stance.Believes, 1.0, SourceKind.Discovery, owner.Id, session.World.Now);
        while (session.Pending is null) session.StepEvent();
        session.ChooseAndConfirm(session.Pending.Options.Single(o => o.Description == "persuade Dorato's bakery to pay").Id);
        Assert.Equal(3, owner.Execution.Operations.Count);
        Assert.Equal(3, owner.Execution.Operations.Select(s => s.DelegatedToId ?? s.OwnerId).Distinct().Count());
        Assert.NotNull(Strategies.CurrentExecution(session.World, owner));
    }

    [Fact]
    public void A_crowded_review_keeps_cancellation_within_six_options()
    {
        var session = Concurrent();
        var owner = session.World.Get("vincent");
        for (int i = 0; i < 12; i++)
        {
            var worker = new Character
            {
                Id = $"worker-{i}", Name = $"Worker {i}", RoleTitle = "soldier",
                Capabilities = new Capabilities(crew: 1, authority: 1, districts: new[] { Cast.Harbour }),
                Psychology = session.World.Get("tommy").Psychology,
            };
            worker.Social.OrganizationId = Cast.OrgId;
            session.World.Characters.Add(worker.Id, worker);
            Relations.Establish(owner, worker.Id, trust: 0.5);
        }
        session.ReviewOperation("work-0");
        Assert.Equal(6, session.Pending!.Options.Count);
        Assert.Contains(session.Pending.Options, o => o.Description == "drop getting Ferri's tailor shop to pay");
        Assert.DoesNotContain(session.Pending.Options, o => o.Description.Contains("Bellini"));
    }

    [Fact]
    public void Review_player_and_automatic_preference_resolve_identically()
    {
        var auto = Concurrent();
        var player = Concurrent();
        auto.ReviewOperation("work-0");
        player.ReviewOperation("work-0");
        Assert.Equal(JsonSerializer.Serialize(auto.Pending), JsonSerializer.Serialize(player.Pending));
        auto.ResolveAutomatically();
        player.ChooseAndConfirm(player.Pending!.Options.Single(o => o.Description == auto.Snapshot().LastAction!.Description).Id);
        Assert.Equal(ControlledAutonomousParityTests.ComprehensiveFingerprint(auto.World),
            ControlledAutonomousParityTests.ComprehensiveFingerprint(player.World));
    }

    [Fact]
    public void Another_actors_unknown_operation_does_not_suppress_a_known_target()
    {
        var world = Cast.Build(42, "baseline");
        var owner = world.Get("vincent");
        var other = world.Get("salvatore");
        var operation = new StrategyInstance { OwnerId = other.Id, LocalSequence = other.StrategyCount++,
            Kind = StrategyKind.SecureTribute, Domain = Cast.Harbour, TargetId = Cast.Tailor,
            StartedAt = world.Now, Deadline = world.Now.AddDays(30) };
        other.Execution.Operations.Add(operation);
        var trigger = new ScheduledEvent { Id = 1234, Time = world.Now, Kind = EventKind.RoleReview,
            OwnerId = owner.Id, Cause = "review" };
        Assert.Contains(Pipeline.Prepare(world, owner, trigger).Available,
            c => c.Kind == ActionKind.StartStrategy && c.TargetId == Cast.Tailor);
        other.Execution.Operations.Clear();
        owner.Execution.Operations.Add(new StrategyInstance { OwnerId = owner.Id, LocalSequence = owner.StrategyCount++,
            Kind = StrategyKind.SecureTribute, Domain = Cast.Harbour, TargetId = Cast.Tailor,
            DelegatedToId = "tommy", StartedAt = world.Now, Deadline = world.Now.AddDays(30) });
        Assert.DoesNotContain(Pipeline.Prepare(world, owner, trigger).Generated,
            c => c.Kind == ActionKind.StartStrategy && c.TargetId == Cast.Tailor);
    }

    [Fact]
    public void Completing_one_sibling_does_not_close_the_shared_assignment()
    {
        var session = Concurrent();
        var world = session.World;
        var owner = world.Get("vincent");
        var personal = owner.Execution.Strategy!;
        var assignmentId = personal.AssignmentId!.Value;
        // Staged agreement and collection-ready step; actual step resolution must keep sibling obligations.
        world.Businesses[Cast.Grocery].PayingTribute = true;
        personal.StepIndex = 3;
        Strategies.ScheduleNextStep(world, personal, "collection ready", TimeSpan.Zero);
        session.StepEvent();
        Assert.DoesNotContain(personal, owner.Execution.Operations);
        Assert.Contains(world.Org.Assignments, a => a.Id == assignmentId);
        Assert.Contains(owner.Execution.Commitments, c => c.Id == $"assignment:{assignmentId}");
        Assert.Contains(owner.Execution.Operations, s => s.DelegatedToId == "tommy");
    }
    [Theory]
    [InlineData("baseline")]
    [InlineData("cautious-vincent")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    [InlineData("resentful-tommy")]
    [InlineData("capable-angelo")]
    public void Variant_outcomes_are_measured_without_tuning_the_objective(string variant)
    {
        var session = SimulationSession.Start(42, variant, null, "salvatore");
        session.AdvanceDays(90);
        output.WriteLine($"{variant}: {session.Result!.Outcome}; loss={session.World.Org.Condition(CrimeSim.Org.OrgCondition.RevenueLoss)}; " +
            $"conflicts={session.World.AccountConflicts.Count}; agreements={session.World.AccountAgreements.Count}; " +
            $"cash={session.World.Get("vincent").Capabilities.Cash}");
        foreach (var actor in session.World.Characters.Values)
            _ = Strategies.CurrentExecution(session.World, actor);
        Assert.Equal(SessionStatus.Resolved, session.Status);
    }
    [Fact]
    public void Focused_cancellation_releases_only_the_selected_operation_and_preserves_personal_work()
    {
        var session = Concurrent();
        var owner = session.World.Get("vincent");
        var delegated = owner.Execution.Operations.Single(s => s.DelegatedToId == "tommy");
        var personal = owner.Execution.Strategy!;
        long pending = personal.PendingStepEventId!.Value;
        long cancelled = delegated.PendingStepEventId!.Value;
        int ordinal = personal.NextAdvanceOrdinal;
        session.ReviewOperation($"work-{delegated.LocalSequence}");
        var choice = Assert.Single(session.Pending!.Options, o => o.Description == "drop getting Ferri's tailor shop to pay");
        Assert.InRange(session.Pending.Options.Count, 2, 6);
        session.ChooseAndConfirm(choice.Id);
        Assert.Same(personal, Assert.Single(owner.Execution.Operations));
        Assert.Equal(pending, personal.PendingStepEventId);
        Assert.Equal(ordinal, personal.NextAdvanceOrdinal);
        Assert.True(session.World.Queue.Cancelled.ContainsKey(cancelled));
        Assert.Null(Strategies.CurrentExecution(session.World, session.World.Get("tommy")));
        Assert.DoesNotContain(owner.Execution.Commitments, c => c.Id == $"strategy:vincent:{delegated.LocalSequence}");
        Assert.Contains(owner.Execution.Commitments, c => c.Id == $"strategy:vincent:{personal.LocalSequence}");
        Assert.DoesNotContain(session.World.Get("tommy").Execution.Commitments,
            c => c.Id == $"strategy:vincent:{delegated.LocalSequence}");
    }

    [Fact]
    public void Reassignment_preserves_identity_progress_and_breach_authorship()
    {
        var session = Concurrent("capable-angelo");
        var owner = session.World.Get("vincent");
        var s = owner.Execution.Operations.Single(s => s.DelegatedToId == "tommy");
        s.Method = CoercionMethod.Threaten;
        s.FailedAttempts = 3;
        s.BreachedPolicyId = "no-violence-harbour";
        s.PolicyBreachDecisionMakerId = "tommy";
        int ordinal = s.NextAdvanceOrdinal;
        var start = s.StartedAt;
        long oldStep = s.PendingStepEventId!.Value;
        session.ReviewOperation($"work-{s.LocalSequence}");
        session.ChooseAndConfirm(session.Pending!.Options.Single(o => o.Description == "hand it to Angelo Conti").Id);
        Assert.Same(s, owner.Execution.Operations.Single(o => o.LocalSequence == s.LocalSequence));
        Assert.Equal("angelo", s.DelegatedToId);
        Assert.Equal("tommy", s.PolicyBreachDecisionMakerId);
        Assert.Equal(CoercionMethod.Threaten, s.Method);
        Assert.Equal(3, s.FailedAttempts);
        Assert.Equal(ordinal, s.NextAdvanceOrdinal);
        Assert.Equal(start, s.StartedAt);
        Assert.True(session.World.Queue.Cancelled.ContainsKey(oldStep));
        Assert.Throws<SimulationInvariantException>(() => Strategies.Advance(session.World,
            session.World.Get("tommy"), new ScheduledEvent
            { Id = oldStep, Time = session.World.Now, Kind = EventKind.StrategyStep, OwnerId = "tommy",
              Cause = "stale delivery", Payload = new EventPayload { StrategyOwnerId = s.OwnerId,
                  StrategySequence = s.LocalSequence, AdvanceOrdinal = ordinal } }));
        Assert.Null(Strategies.CurrentExecution(session.World, session.World.Get("tommy")));
        Assert.Same(s, Strategies.CurrentExecution(session.World, session.World.Get("angelo")));
    }

    [Fact]
    public void Hidden_delegate_progress_does_not_change_owner_snapshot_or_review_options()
    {
        var a = Concurrent();
        var b = Concurrent();
        var hidden = b.World.Get("vincent").Execution.Operations.Single(s => s.DelegatedToId is not null);
        hidden.FailedAttempts = 90;
        hidden.StepIndex = 3;
        hidden.Method = CoercionMethod.Force;
        Assert.Equal("persuade Ferri's tailor shop to pay",
            b.Snapshot().Operations.Single(o => o.ExecutorName == "Tommy Nardo").Approach);
        Assert.Equal("use force on Ferri's tailor shop",
            PlayerView.Build(b.World, "tommy", b.World.Now).Operations.Single().Approach);
        Assert.Equal(JsonSerializer.Serialize(a.Snapshot()), JsonSerializer.Serialize(b.Snapshot()));
        a.ReviewOperation($"work-{hidden.LocalSequence}");
        b.ReviewOperation($"work-{hidden.LocalSequence}");
        Assert.Equal(JsonSerializer.Serialize(a.Pending), JsonSerializer.Serialize(b.Pending));
        Assert.Contains(b.Pending!.Options, o => o.Description.StartsWith("drop "));
    }

    [Fact]
    public void Invalid_or_foreign_reviews_do_not_mutate_the_session()
    {
        var session = Concurrent();
        string before = ControlledAutonomousParityTests.ComprehensiveFingerprint(session.World);
        Assert.Throws<ArgumentException>(() => session.ReviewOperation("not-your-operation"));
        Assert.Equal(before, ControlledAutonomousParityTests.ComprehensiveFingerprint(session.World));
        session.ReviewOperation("work-0");
        Assert.Throws<InvalidOperationException>(() => session.ReviewOperation("work-1"));
    }

    [Fact]
    public void Save_and_load_replay_a_selected_review_and_its_cancellation_with_two_operations()
    {
        var session = PersistentSession.Start(42, "baseline", "vincent");
        foreach (var text in Opening)
        {
            while (session.Pending is null) session.StepEvent();
            session.ChooseAndConfirm(session.Pending.Options.Single(o => o.Description == text).Id);
        }
        session.ReviewOperation("work-0");
        string path = Path.Combine(Path.GetTempPath(), $"bandwidth-{Guid.NewGuid():N}.db");
        try
        {
            session.Save(path);
            var loaded = PersistentSession.Load(path);
            Assert.Equal(JsonSerializer.Serialize(session.Pending), JsonSerializer.Serialize(loaded.Pending));
            Assert.Equal(ControlledAutonomousParityTests.ComprehensiveFingerprint(session.InnerSession.World),
                ControlledAutonomousParityTests.ComprehensiveFingerprint(loaded.InnerSession.World));
            foreach (var branch in new[] { session, loaded })
                branch.ChooseAndConfirm(branch.Pending!.Options.Single(o => o.Description.StartsWith("drop ")).Id);
            Assert.Equal(ControlledAutonomousParityTests.ComprehensiveFingerprint(session.InnerSession.World),
                ControlledAutonomousParityTests.ComprehensiveFingerprint(loaded.InnerSession.World));
            Assert.Single(loaded.Snapshot().Operations);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    private static readonly string[] Opening =
    {
        "persuade Ferri's tailor shop to pay",
        "carry on getting Ferri's tailor shop to pay",
        "hand it to Tommy Nardo",
        "persuade Bellini's grocery to pay",
    };

    private static SimulationSession Concurrent(string variant = "baseline")
    {
        var session = SimulationSession.Start(42, variant, "vincent");
        foreach (var description in Opening)
        {
            for (int guard = 0; session.Pending is null && guard < 1000; guard++) session.StepEvent();
            if (!session.Pending!.Options.Any(o => o.Description == description) && description.StartsWith("hand it to"))
            {
                session.ChooseAndConfirm(session.Pending.Options.Single(o => o.Description.StartsWith("carry on")).Id);
                session.ReviewOperation("work-0");
            }
            session.ChooseAndConfirm(session.Pending!.Options.Single(o => o.Description == description).Id);
        }
        Assert.Equal(2, session.Snapshot().Operations.Count);
        return session;
    }
    [Fact]
    public void Natural_fixture_has_parallel_personal_and_delegated_advances()
    {
        var session = SimulationSession.Start(42, "baseline", "vincent");
        int maxOwned = 0;
        var advancedTogether = new HashSet<string>();
        while (session.Status != SessionStatus.Resolved)
        {
            var priorAdvances = session.World.Get("vincent").Execution.Operations
                .ToDictionary(s => s.LocalSequence, s => s.NextAdvanceOrdinal);
            if (session.Pending is { } pending)
            {
                session.ResolveAutomatically();
                var chosen = session.World.Decisions.Last(d => d.ActorId == "vincent").Chosen!.Candidate;
                output.WriteLine($"{pending.At:MM-dd HH:mm}: {pending.Options.Single(o => o.Description ==
                    PlayerOption.Describe(chosen, PlayerView.NameIn(session.World), PlayerView.You,
                        id => session.World.Get(id).Pronouns, "vincent")).Description}");
            }
            else session.StepEvent();
            var operations = session.World.Get("vincent").Execution.Operations;
            maxOwned = Math.Max(maxOwned, operations.Count);
            if (operations.Count > 1)
                foreach (var s in operations.Where(s => priorAdvances.TryGetValue(s.LocalSequence, out int prior)
                    && s.NextAdvanceOrdinal > prior))
                    advancedTogether.Add(s.DelegatedToId ?? s.OwnerId);
            foreach (var actor in session.World.Characters.Values)
                _ = Strategies.CurrentExecution(session.World, actor); // throws on any double booking
        }
        output.WriteLine($"max={maxOwned}; result={session.Result}; revenue={session.World.Org.Condition(CrimeSim.Org.OrgCondition.RevenueLoss)}");
        Assert.Equal(2, maxOwned);
        Assert.Contains("vincent", advancedTogether);
        Assert.Contains("tommy", advancedTogether);
    }
}
