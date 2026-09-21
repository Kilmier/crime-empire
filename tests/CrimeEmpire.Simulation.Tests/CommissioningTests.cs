using System.Text.Json;
using CrimeEmpire.Persistence.Session;
using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Scenario;
using CrimeSim.Session;
using CrimeSim.Sim;
using CrimeSim.Strategy;

namespace CrimeEmpire.Simulation.Tests;

public sealed class CommissioningTests
{
    internal static SimulationSession Opening()
    {
        var session = SimulationSession.Start(42, "baseline", "vincent");
        for (int i = 0; i < 100 && session.Pending is null; i++) session.StepEvent();
        Assert.Contains(session.Pending!.Options, o => o.Description == "threaten Bellini's grocery");
        return session;
    }

    private static void Pick(SimulationSession session, string text)
        => session.Choose(Assert.Single(session.Pending!.Options, o => o.Description == text).Id);
    private static void Pick(PersistentSession session, string text)
        => session.Choose(Assert.Single(session.Pending!.Options, o => o.Description == text).Id);

    internal static PreparedDecision Opening(World world)
    {
        for (int i = 0; i < 100; i++)
        {
            var step = Runner.Step(world, Cast.Start.AddDays(2), "vincent");
            if (step.Awaiting is { } prepared) return prepared;
        }
        throw new Exception("No opening decision");
    }
    private static Candidate Leaf(PreparedDecision p, string target = Cast.Grocery,
        CoercionMethod method = CoercionMethod.Threaten)
        => p.Available.Single(c => Commissioning.IsOperation(c) && c.TargetId == target && c.Method == method);

    [Fact]
    public void Natural_direct_commission_has_no_personal_start_and_leaves_owner_free()
    {
        var session = Opening(); var world = session.World;
        var owner = world.Get("vincent"); var executor = world.Get("tommy");
        int strategyCount = owner.StrategyCount;
        var before = ControlledAutonomousParityTests.ComprehensiveFingerprint(world);
        Pick(session, "threaten Bellini's grocery");
        Assert.Equal(before, ControlledAutonomousParityTests.ComprehensiveFingerprint(world));
        Assert.Contains("several days", session.Pending!.Commissioning!.Expectation);
        Pick(session, "Assign Tommy Nardo");
        Assert.Equal(before, ControlledAutonomousParityTests.ComprehensiveFingerprint(world));
        Pick(session, "Confirm operation");
        var operation = Assert.Single(owner.Execution.Operations);
        Assert.Equal(strategyCount, operation.LocalSequence);
        Assert.Equal(strategyCount + 1, owner.StrategyCount);
        Assert.Equal("vincent", operation.OwnerId);
        Assert.Equal("tommy", operation.DelegatedToId);
        Assert.Equal("tommy", operation.CommissionedExecutorId);
        Assert.Equal(CoercionMethod.Threaten, operation.OwnerOrderedMethod);
        Assert.Null(owner.Execution.Strategy);
        Assert.True(Pipeline.AvailableToExecute(world, owner.Id));
        Assert.False(Pipeline.AvailableToExecute(world, executor.Id));
        Assert.NotNull(operation.PendingStepEventId);
        Assert.NotNull(operation.PendingReviewEventId);
        Assert.Equal(0, operation.NextAdvanceOrdinal);
        Assert.Equal(0, operation.StepIndex);
        Assert.Single(executor.Execution.Commitments, c => c.Id == $"strategy:vincent:{strategyCount}");
        Assert.DoesNotContain(world.Queue.Cancelled.Values, reason => reason.Contains("takes it on"));
        session.StepEvent();
        Pick(session, "persuade Ferri's tailor shop to pay");
        Assert.DoesNotContain(session.Pending!.Options, o => o.Description == "Assign Tommy Nardo");
        Assert.Contains(session.Pending.Commissioning!.Staffing, s => s.Contains("Bellini"));
        Pick(session, "Do it yourself"); Pick(session, "Confirm operation");
        Assert.Equal(2, owner.Execution.Operations.Count);
    }

    [Fact]
    public void Backtracking_is_inert_and_confirmation_is_once_only()
    {
        var session = Opening(); var original = JsonSerializer.Serialize(session.Pending);
        var before = ControlledAutonomousParityTests.ComprehensiveFingerprint(session.World);
        for (int i = 0; i < 3; i++)
        {
            Pick(session, "threaten Bellini's grocery");
            var draft = JsonSerializer.Serialize(session.Pending);
            Pick(session, "Assign Tommy Nardo"); Pick(session, "Go back");
            Assert.Equal(draft, JsonSerializer.Serialize(session.Pending));
            Assert.Throws<InvalidOperationException>(() => session.AdvanceDays(1));
            Pick(session, "Go back");
            Assert.Equal(original, JsonSerializer.Serialize(session.Pending));
            Assert.Equal(before, ControlledAutonomousParityTests.ComprehensiveFingerprint(session.World));
        }
        Pick(session, "threaten Bellini's grocery"); Pick(session, "Assign Tommy Nardo");
        var confirm = session.Pending!.Options.Single(o => o.Description == "Confirm operation").Id;
        session.Choose(confirm);
        var committed = ControlledAutonomousParityTests.ComprehensiveFingerprint(session.World);
        Assert.ThrowsAny<Exception>(() => session.Choose(confirm));
        Assert.Equal(committed, ControlledAutonomousParityTests.ComprehensiveFingerprint(session.World));
    }

    [Fact]
    public void Cached_executor_evaluation_does_not_repeat_or_change_first_stage()
    {
        var world = Cast.Build(42, "baseline"); var p = Opening(world);
        var original = JsonSerializer.Serialize(p.Scored); var count = p.Actor.DecisionCount;
        var first = Commissioning.Prepare(p, Leaf(p));
        Commissioning.Prepare(p, Leaf(p, Cast.Tailor));
        Assert.Same(first, Commissioning.Prepare(p, Leaf(p)));
        Assert.Equal(count, p.Actor.DecisionCount);
        Assert.Equal(original, JsonSerializer.Serialize(p.Scored));
        Assert.Equal(2, p.Commissions.Count);
        Assert.True(p.Attention.GroupingActivated);
        Assert.True(p.Attention.RetainedAlternatives.Count <= 6);
        Assert.True(p.Available.Count <= 10);
        Assert.DoesNotContain(p.Generated, c => c.Id.StartsWith("executor:"));
    }

    [Fact]
    public void Executor_cap_protects_self_and_excludes_unknown_busy_and_sixth_subordinate()
    {
        var world = Cast.Build(42, "baseline"); var opening = Opening(world);
        var owner = opening.Actor;
        for (int i = 0; i < 9; i++)
        {
            string id = $"extra-{i}";
            var sub = new Character { Id = id, Name = id, RoleTitle = "soldier", Psychology = new Psychology(),
                Capabilities = new Capabilities(authority: 1, districts: new[] { Cast.Harbour }) };
            sub.Social.OrganizationId = owner.Social.OrganizationId;
            world.Characters.Add(id, sub);
            if (i != 8) Relations.Meet(owner, id);
        }
        var busy = world.Get("extra-0");
        busy.Execution.Operations.Add(Work(busy.Id, Cast.Tailor));
        var p = Pipeline.Prepare(world, owner, opening.Trigger);
        var commission = Commissioning.Prepare(p, Leaf(p));
        Assert.Equal(6, commission.Available.Count);
        Assert.Single(commission.Available, c => c.Kind == ActionKind.StartStrategy);
        Assert.DoesNotContain(commission.Available, c => c.TargetId is "extra-0" or "extra-8");
        Assert.Equal(commission.Available.OrderBy(c => c.Id, StringComparer.Ordinal), commission.Available);
        Assert.Equal(new[] { "extra-1", "extra-2", "extra-3", "extra-4", "tommy" },
            commission.Available.Where(c => c.Kind == ActionKind.DelegateStrategy).Select(c => c.TargetId));
        // A held capability judgment can move a name into the five places. Objective skill was
        // never consulted, and the final menu still uses neutral id order.
        owner.Cognition.Learn(new Claim(ClaimKind.PersonIsCapable, "extra-7", CapabilityBar.HardMan),
            Stance.Believes, .95, SourceKind.Participant, owner.Id, world.Now);
        var informed = Pipeline.Prepare(world, owner, opening.Trigger);
        Assert.Contains(Commissioning.Prepare(informed, Leaf(informed)).Available, c => c.TargetId == "extra-7");
        owner.Execution.Operations.Add(Work(owner.Id, Cast.Tailor));
        var busyOwner = Pipeline.Prepare(world, owner, opening.Trigger);
        Assert.Equal(5, Commissioning.Prepare(busyOwner, Leaf(busyOwner)).Available.Count);
    }

    [Fact]
    public void Busy_owner_and_autonomous_leader_can_commission_without_overwriting_work()
    {
        var world = Cast.Build(42, "baseline"); var opening = Opening(world); var owner = opening.Actor;
        var prior = Work(owner.Id, Cast.Tailor); owner.Execution.Operations.Add(prior);
        Strategies.ScheduleNextStep(world, prior, "prior"); var pending = prior.PendingStepEventId;
        var p = Pipeline.Prepare(world, owner, opening.Trigger); var leaf = Leaf(p);
        var commission = Commissioning.Prepare(p, leaf);
        Assert.DoesNotContain(commission.Available, c => c.Kind == ActionKind.StartStrategy);
        // No player-supplied executor preference: the real automatic second stage must delegate.
        var record = Pipeline.Resolve(p, leaf.Id);
        Assert.Equal(ActionKind.DelegateStrategy, record.ExecutorChoice!.Candidate.Kind);
        Assert.Contains(prior, owner.Execution.Operations);
        Assert.Equal(pending, prior.PendingStepEventId);
        Assert.Equal(2, owner.Execution.Operations.Count);
        Assert.Equal("tommy", owner.Execution.Operations.Single(s => s.TargetId == Cast.Grocery).DelegatedToId);
    }

    [Fact]
    public void Stale_executor_revalidation_has_no_partial_effect()
    {
        var world = Cast.Build(42, "baseline"); var p = Opening(world); var leaf = Leaf(p);
        var option = Commissioning.Prepare(p, leaf).Available.Single(c => c.Kind == ActionKind.DelegateStrategy);
        world.Get("tommy").Execution.Operations.Add(Work("tommy", Cast.Tailor));
        var before = ControlledAutonomousParityTests.ComprehensiveFingerprint(world);
        Assert.Throws<SimulationInvariantException>(() => Pipeline.Resolve(p, leaf.Id, option.Id));
        Assert.False(p.IsResolved);
        Assert.Equal(before, ControlledAutonomousParityTests.ComprehensiveFingerprint(world));
        Assert.Throws<SimulationInvariantException>(() => Pipeline.Resolve(p, leaf.Id, "not-retained"));
        Assert.Equal(before, ControlledAutonomousParityTests.ComprehensiveFingerprint(world));
    }

    [Fact]
    public void Briefing_is_captured_bounded_and_recipient_checked()
    {
        var world = Cast.Build(42, "baseline"); var p = Opening(world);
        var captured = AssignmentBriefing.CaptureTarget(p.Actor, Cast.Grocery);
        Assert.Equal(new[] { ClaimKind.BusinessRefusesTribute, ClaimKind.TargetIsVulnerable }, captured.Select(c => c.Claim.Kind));
        var assessment = p.Actor.Cognition.Find(new Claim(ClaimKind.TargetIsVulnerable, Cast.Grocery))!;
        Assert.Equal(assessment.Stance, captured[1].AssertedStance);
        Assert.Equal(assessment.Confidence, captured[1].AssertedConfidence);
        Assert.Equal(assessment.SourceKind, captured[1].ClaimedBasis);
        p.Actor.Cognition.Learn(assessment.Claim, Stance.Rejects, 1, SourceKind.Participant, p.Actor.Id, world.Now);
        var wrong = world.Get("kane"); var wrongBefore = JsonSerializer.Serialize(wrong.Cognition.Records);
        Assert.False(AssignmentBriefing.Deliver(world, wrong, p.Actor.Id, "tommy", captured));
        Assert.Equal(wrongBefore, JsonSerializer.Serialize(wrong.Cognition.Records));
        var tommy = world.Get("tommy");
        Assert.True(AssignmentBriefing.Deliver(world, tommy, p.Actor.Id, tommy.Id, captured));
        Assert.Contains(tommy.Cognition.Testimony, t => t.Claim == assessment.Claim && t.AssertedStance == assessment.Stance);
        Assert.Empty(AssignmentBriefing.CaptureTarget(p.Actor, "unknown-target"));
        Assert.DoesNotContain(captured, c => c.Claim.Subject == Cast.Tailor);
    }

    [Fact]
    public void Unknown_work_does_not_reserve_a_target_and_hidden_state_does_not_change_expectation()
    {
        var session = Opening(); var world = session.World;
        var unknown = Work("salvatore", Cast.Grocery); world.Get("salvatore").Execution.Operations.Add(unknown);
        Pick(session, "threaten Bellini's grocery");
        string expectation = session.Pending!.Commissioning!.Expectation;
        unknown.StepIndex = 3; unknown.FailedAttempts = 90;
        world.Businesses[Cast.Grocery].Resistance = 0.99;
        Pick(session, "Assign Tommy Nardo");
        Assert.Equal(expectation, session.Pending!.Commissioning!.Expectation);
        Pick(session, "Confirm operation");
        Assert.Single(world.Get("vincent").Execution.Operations);
    }

    [Fact]
    public void Owner_progress_changes_only_on_operation_attributed_receipt()
    {
        var session = Opening(); Pick(session, "threaten Bellini's grocery");
        Pick(session, "Assign Tommy Nardo"); Pick(session, "Confirm operation");
        var world = session.World; var owner = world.Get("vincent");
        var s = owner.Execution.Operations.Single(); var before = JsonSerializer.Serialize(session.Snapshot().Operations);
        s.StepIndex++; s.Method = CoercionMethod.Force; s.FailedAttempts++;
        Assert.Equal(before, JsonSerializer.Serialize(session.Snapshot().Operations));
        var claim = new Claim(ClaimKind.BusinessRefusesTribute, Cast.Grocery);
        var report = new Report(900, "tommy", owner.Id, world.Now, ReportCandor.Candid,
            new[] { ReportedClaim.Honest(claim, Stance.Believes, .6, SourceKind.Participant) }, Array.Empty<Claim>(), "account");
        Reporting.Deliver(world, report, owner);
        Assert.Equal(before, JsonSerializer.Serialize(session.Snapshot().Operations));
        Reporting.Deliver(world, report with { Id = 901, Operations = new[] { new ReportedOperation(claim, owner.Id, s.LocalSequence + 1) } }, owner);
        Assert.Equal(before, JsonSerializer.Serialize(session.Snapshot().Operations));
        Reporting.Deliver(world, report with { Id = 902, Operations = new[] { new ReportedOperation(claim, owner.Id, s.LocalSequence) } }, owner);
        Assert.Contains("gave an account", session.Snapshot().Operations.Single().Progress);
        Assert.Equal(CoercionMethod.Threaten, s.OwnerOrderedMethod);
    }

    [Fact]
    public void Every_draft_boundary_and_committed_state_round_trips_through_real_replay()
    {
        var session = PersistentSession.Start(42, "baseline", "vincent");
        while (session.Pending is null) session.StepEvent();
        var path = Path.Combine(Path.GetTempPath(), $"commission-{Guid.NewGuid():N}.sqlite");
        try
        {
            foreach (var text in new[] { "threaten Bellini's grocery", "Assign Tommy Nardo", "Go back", "Go back",
                         "threaten Bellini's grocery", "Assign Tommy Nardo", "Confirm operation" })
            {
                Pick(session, text); session.Save(path);
                var loaded = PersistentSession.Load(path);
                Assert.Equal(JsonSerializer.Serialize(session.Pending), JsonSerializer.Serialize(loaded.Pending));
                Assert.Equal(ControlledAutonomousParityTests.ComprehensiveFingerprint(session.InnerSession.World), ControlledAutonomousParityTests.ComprehensiveFingerprint(loaded.InnerSession.World));
                session = loaded;
            }
            for (int i = 0; i < 30 && session.Status != SessionStatus.Resolved; i++)
            {
                if (session.Pending is null) session.StepEvent();
                else
                {
                    var pending = session.Pending;
                    var option = pending.Options.FirstOrDefault(o => o.Description.StartsWith("carry on"))
                        ?? pending.Options.First(o => o.Description != "Go back");
                    session.Choose(option.Id);
                }
                session.Save(path); var loaded = PersistentSession.Load(path);
                Assert.Equal(ControlledAutonomousParityTests.ComprehensiveFingerprint(session.InnerSession.World), ControlledAutonomousParityTests.ComprehensiveFingerprint(loaded.InnerSession.World));
                Assert.Equal(JsonSerializer.Serialize(session.Pending), JsonSerializer.Serialize(loaded.Pending));
            }
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Fact]
    public void Forged_leaf_fields_cannot_change_a_retained_commission()
    {
        var world = Cast.Build(42, "baseline"); var p = Opening(world); var leaf = Leaf(p);
        var forged = leaf with { TargetId = "unknown", Method = CoercionMethod.Force };
        Assert.Same(leaf, Commissioning.Prepare(p, forged).Operation);
        Assert.Equal(Cast.Grocery, Commissioning.Prepare(p, leaf).Operation.TargetId);
    }

    [Fact]
    public void Actual_execution_learning_gets_attribution_only_when_a_report_is_delivered()
    {
        var session = Opening(); Pick(session, "threaten Bellini's grocery");
        Pick(session, "Assign Tommy Nardo"); Pick(session, "Confirm operation");
        var world = session.World; var owner = world.Get("vincent"); var sub = world.Get("tommy");
        var operation = owner.Execution.Operations.Single();
        // Advance through the real scheduled calendar. Resolve further owner decisions normally.
        for (int i = 0; i < 100 && sub.Execution.OperationLearning.Count == 0; i++)
        {
            if (session.Pending is not null) session.ResolveAutomatically();
            else session.StepEvent();
        }
        var learned = Assert.Single(sub.Execution.OperationLearning.Values,
            p => p.OwnerId == owner.Id && p.Sequence == operation.LocalSequence);
        Assert.Contains("progress is unknown", session.Snapshot().Operations.Single(o => o.ExecutorName == sub.Name).Progress);
        var report = Reporting.Compose(world, sub, owner,
            new Candidate("account", ActionKind.ReportToSuperior, "test", "account")
            { TargetId = owner.Id, Candor = ReportCandor.Candid, AnsweringClaim = learned.Position.Claim },
            Salience.Perceive(sub, world.Now));
        Assert.Contains(report.Operations, a => a.OwnerId == owner.Id && a.Sequence == operation.LocalSequence);
        Assert.Empty(owner.Execution.OperationAccounts);
        var before = ControlledAutonomousParityTests.ComprehensiveFingerprint(world);
        Assert.Throws<SimulationInvariantException>(() => Reporting.Deliver(world, report, world.Get("kane")));
        Assert.Equal(before, ControlledAutonomousParityTests.ComprehensiveFingerprint(world));
        Reporting.Deliver(world, report, owner);
        Assert.Contains("gave an account", session.Snapshot().Operations.Single(o => o.ExecutorName == sub.Name).Progress);
        // A later unrelated source for the same claim cannot inherit the old operation address.
        sub.Cognition.Learn(learned.Position.Claim, Stance.Knows, 1, SourceKind.Participant, sub.Id, world.Now.AddDays(1));
        var later = Reporting.Compose(world, sub, owner,
            new Candidate("later", ActionKind.ReportToSuperior, "test", "account")
            { TargetId = owner.Id, Candor = ReportCandor.Candid, AnsweringClaim = learned.Position.Claim },
            Salience.Perceive(sub, world.Now.AddDays(1)));
        Assert.Empty(later.Operations);
    }

    [Fact]
    public void Natural_autonomous_commission_uses_same_retained_executor_evaluation()
    {
        var world = Cast.Build(42, "baseline");
        Runner.Run(world, Cast.Start.AddDays(20));
        var record = Assert.Single(world.Decisions.Where(d => d.ExecutorChoice is not null).Take(1));
        Assert.InRange(record.ExecutorOptions!.Count, 1, 6);
        Assert.Equal(record.ExecutorOptions.OrderByDescending(s => s.Total).ThenBy(s => s.Candidate.Id, StringComparer.Ordinal).First(),
            record.ExecutorChoice);
        Assert.Contains(record.ExecutorOptions, s => s.Candidate.Kind == ActionKind.DelegateStrategy);
        var trace = CrimeSim.Trace.TraceWriter.RenderDecision(record, full: true);
        Assert.Contains("initial executor (after operation selection", trace);
        Assert.Contains(record.ExecutorChoice!.Candidate.Id, trace);
    }

    [Fact]
    public void Direct_commission_preserves_review_reassignment_and_isolated_cancellation()
    {
        var session = SimulationSession.Start(42, "capable-angelo", "vincent");
        while (session.Pending is null) session.StepEvent();
        Pick(session, "threaten Bellini's grocery"); Pick(session, "Assign Tommy Nardo"); Pick(session, "Confirm operation");
        var world = session.World; var owner = world.Get("vincent"); var s = owner.Execution.Operations.Single();
        var firstStep = s.PendingStepEventId;
        session.StepEvent(); Pick(session, "take no action"); // existing hands-free occasion comes first
        session.ReviewOperation($"work-{s.LocalSequence}"); Pick(session, "leave these orders unchanged");
        Assert.Equal(firstStep, s.PendingStepEventId);
        Assert.NotNull(s.PendingReviewEventId);
        // Deliberately late information about the target still causes the existing early review.
        var receipt = owner.Cognition.Receive(ReportedClaim.Honest(
            new Claim(ClaimKind.TributeCollected, Cast.Grocery), Stance.Believes, .7, SourceKind.Participant),
            "tommy", world.Now);
        var previousReview = s.PendingReviewEventId;
        Strategies.ReviewAfterReceipt(world, owner, receipt);
        Assert.NotEqual(previousReview, s.PendingReviewEventId);
        s.StepIndex = 2; s.FailedAttempts = 1;
        var identity = (s.OwnerId, s.LocalSequence, s.StartedAt, s.AssignmentId, s.NextAdvanceOrdinal);
        session.ReviewOperation($"work-{s.LocalSequence}"); Pick(session, "hand it to Angelo Conti");
        Assert.Equal(identity, (s.OwnerId, s.LocalSequence, s.StartedAt, s.AssignmentId, s.NextAdvanceOrdinal));
        Assert.Equal(2, s.StepIndex); Assert.Equal(1, s.FailedAttempts);
        Assert.Equal("tommy", s.CommissionedExecutorId); Assert.Equal("angelo", s.DelegatedToId);
        Assert.Equal(CoercionMethod.Threaten, s.OwnerOrderedMethod);
        Assert.Null(Strategies.CurrentExecution(world, world.Get("tommy")));
        var prior = Work(owner.Id, Cast.Tailor); owner.Execution.Operations.Add(prior);
        Strategies.ScheduleNextStep(world, prior, "independent work"); var pending = prior.PendingStepEventId;
        session.ReviewOperation($"work-{s.LocalSequence}"); Pick(session, "drop getting Bellini's grocery to pay");
        Assert.Same(prior, Assert.Single(owner.Execution.Operations)); Assert.Equal(pending, prior.PendingStepEventId);
        Assert.Null(Strategies.CurrentExecution(world, world.Get("angelo")));
    }

    [Theory]
    [InlineData(ActionKind.ContinueStrategy)]
    [InlineData(ActionKind.AlterStrategy)]
    [InlineData(ActionKind.PostponeStrategy)]
    public void Direct_executor_keeps_existing_continuation_alteration_and_postponement(ActionKind kind)
    {
        var session = Opening(); Pick(session, "threaten Bellini's grocery");
        Pick(session, "Assign Tommy Nardo"); Pick(session, "Confirm operation");
        var world = session.World; var owner = world.Get("vincent"); var sub = world.Get("tommy");
        var s = owner.Execution.Operations.Single(); s.StepIndex = 2; s.FailedAttempts = 1;
        if (kind == ActionKind.AlterStrategy) s.Method = CoercionMethod.Persuade;
        if (kind == ActionKind.PostponeStrategy)
        {
            world.Queue.Cancel(s.PendingStepEventId!.Value, "staged block"); s.PendingStepEventId = null;
        }
        var pending = s.PendingStepEventId; var ordinal = s.NextAdvanceOrdinal;
        var prepared = Pipeline.Prepare(world, sub, new ScheduledEvent
        { Id = 234, Time = world.Now, Kind = EventKind.StrategyBlocked, OwnerId = sub.Id, Cause = "staged refusal",
          Payload = new EventPayload { StrategyOwnerId = owner.Id, StrategySequence = s.LocalSequence } });
        Pipeline.Resolve(prepared, prepared.Available.First(c => c.Kind == kind).Id);
        Assert.Same(s, Strategies.CurrentExecution(world, sub));
        Assert.Equal(ordinal, s.NextAdvanceOrdinal);
        Assert.Equal(CoercionMethod.Threaten, s.OwnerOrderedMethod);
        if (kind == ActionKind.ContinueStrategy) Assert.Equal(pending, s.PendingStepEventId);
        else Assert.NotEqual(pending, s.PendingStepEventId);
        Assert.Contains("progress is unknown", session.Snapshot().Operations.Single().Progress);
    }

    [Fact]
    public void Commissioning_does_not_require_the_owners_personal_crew_requirement()
    {
        var world = Cast.Build(42, "baseline"); var original = Opening(world);
        original.Actor.Capabilities.Crew = 1; // force personally needs two; delegation needs one
        var p = Pipeline.Prepare(world, original.Actor, original.Trigger);
        var leaf = Leaf(p, Cast.Grocery, CoercionMethod.Force);
        var commission = Commissioning.Prepare(p, leaf);
        Assert.DoesNotContain(commission.Available, c => c.Kind == ActionKind.StartStrategy);
        Assert.Contains(commission.Available, c => c.TargetId == "tommy");
        Pipeline.Resolve(p, leaf.Id);
        Assert.Equal("tommy", original.Actor.Execution.Operations.Single().DelegatedToId);
    }

    [Theory]
    [InlineData("crew")]
    [InlineData("subordination")]
    [InlineData("access")]
    [InlineData("knowledge")]
    public void Commit_revalidates_more_than_executor_capacity(string change)
    {
        var world = Cast.Build(42, "baseline"); var p = Opening(world); var leaf = Leaf(p);
        var choice = Commissioning.Prepare(p, leaf).Available.Single(c => c.Kind == ActionKind.DelegateStrategy);
        if (change == "crew") p.Actor.Capabilities.Crew = 0;
        if (change == "subordination") world.Get("tommy").Social.OrganizationId = "other";
        if (change == "access") p.Actor.Capabilities.Districts.Clear();
        if (change == "knowledge") p.Actor.Cognition.Learn(leaf.RequiredKnowledge.Single(), Stance.Rejects,
            1, SourceKind.Participant, p.Actor.Id, world.Now);
        var before = ControlledAutonomousParityTests.ComprehensiveFingerprint(world);
        Assert.Throws<SimulationInvariantException>(() => Pipeline.Resolve(p, leaf.Id, choice.Id));
        Assert.Equal(before, ControlledAutonomousParityTests.ComprehensiveFingerprint(world));
        Assert.False(p.IsResolved);
    }

    [Fact]
    public void Replay_comparator_observes_new_attribution_state()
    {
        var world = Cast.Build(42, "baseline"); var owner = world.Get("vincent");
        var record = owner.Cognition.Records.First();
        var before = ControlledAutonomousParityTests.ComprehensiveFingerprint(world);
        owner.Execution.OperationLearning[record.Claim] = new OperationLearning(owner.Id, 10, record);
        var learned = ControlledAutonomousParityTests.ComprehensiveFingerprint(world);
        Assert.NotEqual(before, learned);
        owner.Execution.OperationAccounts.Add(new OperationAccount(owner.Id, 10, "tommy", world.Now, record.Claim, record.Stance));
        Assert.NotEqual(learned, ControlledAutonomousParityTests.ComprehensiveFingerprint(world));
        var report = new Report(123, "tommy", owner.Id, world.Now, ReportCandor.Candid,
            Array.Empty<ReportedClaim>(), Array.Empty<Claim>(), "account");
        world.Reports.Add(report);
        before = ControlledAutonomousParityTests.ComprehensiveFingerprint(world);
        world.Reports[world.Reports.Count - 1] = report with
            { Operations = new[] { new ReportedOperation(record.Claim, owner.Id, 10) } };
        Assert.NotEqual(before, ControlledAutonomousParityTests.ComprehensiveFingerprint(world));
    }

    private static StrategyInstance Work(string owner, string target) => new()
    { OwnerId = owner, LocalSequence = 100, Kind = StrategyKind.SecureTribute, Domain = Cast.Harbour,
      TargetId = target, StartedAt = Cast.Start, Deadline = Cast.Start.AddDays(30),
      Method = CoercionMethod.Persuade, OwnerOrderedMethod = CoercionMethod.Persuade };
}
