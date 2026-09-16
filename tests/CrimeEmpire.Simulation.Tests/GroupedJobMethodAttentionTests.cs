using System.Reflection;
using CrimeEmpire.Persistence.Session;
using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Scenario;
using CrimeSim.Session;
using CrimeSim.Sim;
using CrimeSim.Strategy;
using Xunit.Abstractions;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>
/// Milestone 029: two eligible known tribute jobs may share attention with singleton actions while
/// their individually salient methods remain concrete choices. Every non-activating occasion keeps
/// the original flat pipeline.
/// </summary>
public sealed class GroupedJobMethodAttentionTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("baseline", 1)]
    [InlineData("cautious-vincent", 1)]
    [InlineData("watchful-boss", 1)]
    [InlineData("disloyal-vincent", 0)]
    [InlineData("resentful-tommy", 1)]
    [InlineData("capable-angelo", 1)]
    public void Every_natural_grouped_occasion_is_bounded_and_measured(
        string variant, int expectedActivations)
    {
        var world = Cast.Build(42, variant);
        int activations = 0;

        while (true)
        {
            var step = Runner.Step(world, Cast.Start.AddDays(90), "vincent");
            if (step.Status == StepStatus.Exhausted) break;
            if (step.Status != StepStatus.AwaitingChoice) continue;

            var prepared = step.Awaiting!;
            if (prepared.Attention.GroupingActivated)
            {
                activations++;
                Assert.InRange(prepared.Attention.RetainedAlternatives.Count, 1,
                    SalienceProfile.MaxCandidates);
                Assert.InRange(prepared.Attention.RetainedLeaves.Count, 1, 10);

                output.WriteLine($"{variant} {prepared.At:yyyy-MM-dd HH:mm} {prepared.Actor.Id}: " +
                    $"generated={prepared.Generated.Count}; alternatives=" +
                    $"{prepared.Attention.OrderedAlternatives.Count}; retained-top=" +
                    $"{prepared.Attention.RetainedAlternatives.Count}; evaluated=" +
                    $"{prepared.Attention.RetainedLeaves.Count}; available={prepared.Available.Count}");
                output.WriteLine("  retained: " + string.Join(" | ",
                    prepared.Attention.RetainedAlternatives.Select(Describe)));
                output.WriteLine("  rejected: " + string.Join(" | ",
                    prepared.Rejected.Select(r => $"{r.Candidate.Id}:{r.Stage}")));
            }

            Pipeline.Resolve(prepared, null);
        }

        Assert.Equal(expectedActivations, activations);
    }

    [Fact]
    public void Activation_is_decided_after_redundancy_before_salience_and_only_for_two_known_targets()
    {
        var two = Context(twoTargets: true);
        var allMethods = Methods(Cast.Tailor).Concat(Methods(Cast.Grocery)).ToList();
        var activated = Filters.Apply(two.Context, allMethods, new SalienceProfile());

        Assert.True(activated.Attention.GroupingActivated);
        Assert.Equal(new[] { Cast.Tailor, Cast.Grocery }, activated.Attention.EligibleTargetIds);

        var one = Context(twoTargets: false);
        var oneTarget = Filters.Apply(one.Context, Methods(Cast.Tailor).ToList(), new SalienceProfile());
        Assert.False(oneTarget.Attention.GroupingActivated);

        var gap = new Claim(ClaimKind.UnattributedShortfall, Cast.Harbour);
        var unattributed = Methods(Cast.Tailor, gap, replaceKnownRequirement: true).ToList();
        var gapResult = Filters.Apply(one.Context, unattributed, new SalienceProfile());
        Assert.False(gapResult.Attention.GroupingActivated);

        var noTargets = Filters.Apply(one.Context,
            new[] { Singleton("wait", ActionKind.DoNothing) }, new SalienceProfile());
        Assert.False(noTargets.Attention.GroupingActivated);

        var redundantWorld = Cast.Build(42, "baseline");
        var vincent = redundantWorld.Get("vincent");
        LearnRefusal(vincent, Cast.Grocery, redundantWorld.Now);
        vincent.Execution.Operations.Add(new StrategyInstance
        {
            OwnerId = vincent.Id,
            LocalSequence = vincent.StrategyCount++,
            Kind = StrategyKind.SecureTribute,
            Domain = Cast.Harbour,
            TargetId = Cast.Grocery,
            DelegatedToId = "tommy",
            StartedAt = redundantWorld.Now,
            Deadline = redundantWorld.Now.AddDays(30),
        });
        var prepared = Pipeline.Prepare(redundantWorld, vincent, Wake(vincent.Id));
        var afterRedundancy = Filters.Apply(prepared.Context, allMethods, new SalienceProfile());
        Assert.False(afterRedundancy.Attention.GroupingActivated);
        Assert.Equal(new[] { Cast.Tailor }, afterRedundancy.Attention.EligibleTargetIds);
    }

    [Fact]
    public void One_target_uses_the_original_flat_six_candidate_pipeline_from_identical_input_state()
    {
        var one = Context(twoTargets: false);
        var candidates = new List<Candidate>
        {
            Tribute("a", Cast.Tailor, CoercionMethod.Force),
            Tribute("b", Cast.Tailor, CoercionMethod.Persuade),
            Tribute("c", Cast.Tailor, CoercionMethod.Threaten),
            Singleton("d", ActionKind.DoNothing),
            Singleton("e", ActionKind.ReportToSuperior),
            Singleton("f", ActionKind.SeekApproval),
            Singleton("g", ActionKind.SeekCorroboration),
            Singleton("h", ActionKind.RequestHelp),
        };

        var result = Filters.Apply(one.Context, candidates, new SalienceProfile());

        Assert.False(result.Attention.GroupingActivated);
        Assert.All(result.Attention.OrderedAlternatives, a => Assert.False(a.IsGroup));
        Assert.Equal(new[] { "a", "b", "c", "d", "e", "f" },
            result.Attention.RetainedLeaves.Select(x => x.Candidate.Id));
        Assert.Equal(new[] { "a", "b", "c", "d", "e", "f" },
            result.Passed.Select(c => c.Id));
        Assert.Equal(new[] { "g", "h" },
            result.Rejected.Where(r => r.Reason.StartsWith("crowded out", StringComparison.Ordinal))
                .Select(r => r.Candidate.Id));
        Assert.All(result.Rejected.Where(r => r.Candidate.Id is "g" or "h"),
            r => Assert.Equal("crowded out — only 6 options held his attention", r.Reason));

        var left = OneTargetPrepared();
        var right = OneTargetPrepared();
        Assert.False(left.Prepared.Attention.GroupingActivated);
        Assert.False(right.Prepared.Attention.GroupingActivated);
        Assert.Equal(PreparedSignature(left.Prepared), PreparedSignature(right.Prepared));

        var leftRecord = Pipeline.Resolve(left.Prepared, null);
        var rightRecord = Pipeline.Resolve(right.Prepared, leftRecord.Chosen!.Candidate.Id);
        Assert.Equal(leftRecord.ChosenActionSignature(), rightRecord.ChosenActionSignature());
        Assert.Equal(leftRecord.Outcome, rightRecord.Outcome);
    }

    [Fact]
    public void Natural_opening_reaches_the_ten_leaf_maximum_and_preserves_player_id_order()
    {
        var session = SimulationSession.Start(42, "baseline", "vincent");
        var pending = RunToFirstPause(session);
        var prepared = PreparedOf(session);

        Assert.True(prepared.Attention.GroupingActivated);
        Assert.Equal(6, prepared.Attention.OrderedAlternatives.Count);
        Assert.Equal(6, prepared.Attention.RetainedAlternatives.Count);
        Assert.Equal(2, prepared.Attention.RetainedAlternatives.Count(a => a.IsGroup));
        Assert.All(prepared.Attention.RetainedAlternatives.Where(a => a.IsGroup),
            a => Assert.Equal(3, a.Leaves.Count));
        Assert.Equal(10, prepared.Attention.RetainedLeaves.Count);
        Assert.Equal(10, prepared.Scored.Count);
        Assert.Equal(10, prepared.Available.Count);

        var availableIds = prepared.Available.Select(c => c.Id).ToList();
        Assert.Equal(availableIds.OrderBy(id => id, StringComparer.Ordinal), availableIds);

        var optionIds = OptionIdsOf(session);
        Assert.Equal(availableIds, pending.Options.Select(o => optionIds[o.Id]));
        Assert.Equal(
            prepared.Scored.OrderByDescending(s => s.Total)
                .ThenBy(s => s.Candidate.Id, StringComparer.Ordinal)
                .Select(s => s.Candidate.Id),
            prepared.Scored.Select(s => s.Candidate.Id));

        var chosen = prepared.Available.Single(c => c.TargetId == Cast.Tailor
            && c.Method == CoercionMethod.Persuade);
        string token = optionIds.Single(kv => kv.Value == chosen.Id).Key;
        session.Choose(token);
        Assert.Equal(chosen.Id,
            session.World.Decisions.Last(d => d.ActorId == "vincent").Chosen!.Candidate.Id);
        Assert.Equal(CoercionMethod.Persuade,
            session.World.Get("vincent").Execution.Operations.Single(o => o.TargetId == Cast.Tailor).Method);
    }

    [Fact]
    public void Group_identity_anchor_and_trait_sensitive_membership_are_exact()
    {
        var two = Context(twoTargets: true);
        var strongAndWeak = new List<Candidate>
        {
            Tribute("a-force", Cast.Tailor, CoercionMethod.Force),
            Tribute("z-threat", Cast.Tailor, CoercionMethod.Threaten),
            Tribute("zz-persuade", Cast.Tailor, CoercionMethod.Persuade),
            Tribute("b-force", Cast.Grocery, CoercionMethod.Force),
            Tribute("y-threat", Cast.Grocery, CoercionMethod.Threaten),
            Tribute("zy-persuade", Cast.Grocery, CoercionMethod.Persuade),
            Tribute("aa-ineligible", Cast.Tailor, CoercionMethod.Force,
                new Claim(ClaimKind.UnattributedShortfall, Cast.Harbour),
                replaceKnownRequirement: true),
            Singleton("c-singleton", ActionKind.DoNothing),
        };
        var profile = new SalienceProfile();
        profile.Scale(CoercionMethod.Persuade, 0.40, "");

        var result = Filters.Apply(two.Context, strongAndWeak, profile);
        var groups = result.Attention.OrderedAlternatives.Where(a => a.IsGroup).ToList();

        Assert.True(result.Attention.GroupingActivated);
        Assert.Equal(2, groups.Count);
        Assert.Equal(
            new[]
            {
                new AttentionGroupKey(ActionKind.StartStrategy, StrategyKind.SecureTribute, Cast.Tailor),
                new AttentionGroupKey(ActionKind.StartStrategy, StrategyKind.SecureTribute, Cast.Grocery),
            }.OrderBy(k => k.TargetId, StringComparer.Ordinal),
            groups.Select(g => g.GroupKey!.Value).OrderBy(k => k.TargetId, StringComparer.Ordinal));
        Assert.All(groups, g => Assert.Equal(2, g.Leaves.Count));
        Assert.DoesNotContain(groups.SelectMany(g => g.Leaves),
            l => l.Candidate.Method == CoercionMethod.Persuade);
        Assert.Contains(result.Attention.OrderedAlternatives,
            a => !a.IsGroup && a.Anchor.Id == "aa-ineligible");
        Assert.Equal("a-force", groups.Single(g => g.GroupKey!.Value.TargetId == Cast.Tailor).Anchor.Id);
        Assert.Equal("b-force", groups.Single(g => g.GroupKey!.Value.TargetId == Cast.Grocery).Anchor.Id);
        Assert.All(groups, g => Assert.Equal(1.0, g.Salience, precision: 9));
        Assert.Contains(result.Rejected,
            r => r.Candidate.Id == "zz-persuade" && r.Stage == RejectionStage.Salience);
        Assert.Contains(result.Rejected,
            r => r.Candidate.Id == "zy-persuade" && r.Stage == RejectionStage.Salience);

        var withoutWeak = Filters.Apply(two.Context,
            strongAndWeak.Where(c => c.Method != CoercionMethod.Persuade).ToList(),
            new SalienceProfile());
        Assert.Equal(
            groups.Select(g => (g.GroupKey, g.Anchor.Id, g.Salience)),
            withoutWeak.Attention.OrderedAlternatives.Where(a => a.IsGroup)
                .Select(g => (g.GroupKey, g.Anchor.Id, g.Salience)));
    }

    [Fact]
    public void An_empty_job_group_reserves_no_slot_and_more_singletons_compete()
    {
        var two = Context(twoTargets: true);
        var candidates = new List<Candidate>
        {
            Tribute("tailor-force", Cast.Tailor, CoercionMethod.Force),
            Tribute("grocery-persuade", Cast.Grocery, CoercionMethod.Persuade),
            Singleton("s1", ActionKind.DoNothing),
            Singleton("s2", ActionKind.ReportToSuperior),
            Singleton("s3", ActionKind.SeekApproval),
            Singleton("s4", ActionKind.SeekCorroboration),
            Singleton("s5", ActionKind.RequestHelp),
            Singleton("s6-crowded", ActionKind.Refuse),
        };
        var profile = new SalienceProfile();
        profile.Scale(CoercionMethod.Force, 2.0, "");
        profile.Scale(CoercionMethod.Persuade, 0.40, "");

        var result = Filters.Apply(two.Context, candidates, profile);

        Assert.True(result.Attention.GroupingActivated);
        Assert.Single(result.Attention.RetainedAlternatives, a => a.IsGroup);
        Assert.Equal(5, result.Attention.RetainedAlternatives.Count(a => !a.IsGroup));
        Assert.Equal(6, result.Attention.RetainedAlternatives.Count);
        Assert.DoesNotContain(result.Attention.OrderedAlternatives,
            a => a.IsGroup && a.GroupKey!.Value.TargetId == Cast.Grocery);
        Assert.DoesNotContain(result.Passed, c => c.Id == "grocery-persuade");
        Assert.Contains(result.Rejected,
            r => r.Candidate.Id == "grocery-persuade" && r.Stage == RejectionStage.Salience);
        Assert.Contains(result.Rejected,
            r => r.Candidate.Id == "s6-crowded" && r.Stage == RejectionStage.Salience);
    }

    [Fact]
    public void Focused_cancellation_is_first_singleton_consumes_a_slot_and_never_backfills()
    {
        var review = ReviewContext();
        var missing = new Claim(ClaimKind.WitnessSawIncident, "missing");
        var cancellation = new Candidate("zz-cancel", ActionKind.AbandonStrategy, "test", "cancel")
        {
            IsOperationReview = true,
            OperationOwnerId = "vincent",
            OperationSequence = review.Operation.LocalSequence,
            RequiredKnowledge = new[] { missing },
        };
        var candidates = Methods(Cast.Tailor).Concat(Methods(Cast.Grocery)).Concat(new[]
        {
            cancellation,
            Singleton("z1-singleton", ActionKind.DoNothing),
            Singleton("z2-singleton", ActionKind.ReportToSuperior),
            Singleton("z3-singleton", ActionKind.SeekApproval),
            Singleton("z4-singleton", ActionKind.SeekCorroboration),
            Singleton("z5-singleton", ActionKind.RequestHelp),
        }).ToList();

        var result = Filters.Apply(review.Prepared.Context, candidates, new SalienceProfile());

        Assert.True(result.Attention.GroupingActivated);
        Assert.False(result.Attention.OrderedAlternatives[0].IsGroup);
        Assert.True(result.Attention.OrderedAlternatives[0].FocusedCancellation);
        Assert.Equal(cancellation.Id, result.Attention.OrderedAlternatives[0].Anchor.Id);
        Assert.Equal(6, result.Attention.RetainedAlternatives.Count);
        Assert.Equal(10, result.Attention.RetainedLeaves.Count);
        Assert.DoesNotContain(result.Passed, c => c.Id == cancellation.Id);
        Assert.DoesNotContain(result.Passed, c => c.Id == "z4-singleton" || c.Id == "z5-singleton");
        Assert.Contains(result.Rejected,
            r => r.Candidate.Id == cancellation.Id && r.Stage == RejectionStage.Knowledge);
        Assert.Equal(9, result.Passed.Count);
    }

    [Fact]
    public void Retained_leaves_restore_flat_order_and_late_feasibility_does_not_backfill()
    {
        var two = Context(twoTargets: true);
        var missing = new Claim(ClaimKind.WitnessSawIncident, "missing");
        var candidates = new List<Candidate>
        {
            Tribute("a-tailor-force", Cast.Tailor, CoercionMethod.Force),
            Tribute("z-tailor-threat", Cast.Tailor, CoercionMethod.Threaten),
            Tribute("zz-tailor-persuade", Cast.Tailor, CoercionMethod.Persuade),
            Tribute("b-grocery-force", Cast.Grocery, CoercionMethod.Force, missing),
            Tribute("y-grocery-threat", Cast.Grocery, CoercionMethod.Threaten, missing),
            Tribute("zy-grocery-persuade", Cast.Grocery, CoercionMethod.Persuade, missing),
            Singleton("c-singleton", ActionKind.DoNothing),
            Singleton("d-singleton", ActionKind.ReportToSuperior),
            Singleton("e-singleton", ActionKind.SeekApproval),
            Singleton("f-singleton", ActionKind.SeekCorroboration),
            Singleton("g-not-backfilled", ActionKind.RequestHelp),
        };
        var profile = new SalienceProfile();
        profile.Scale(CoercionMethod.Force, 2.0, "");
        profile.Scale(CoercionMethod.Threaten, 1.5, "");

        var result = Filters.Apply(two.Context, candidates, profile);
        var retainedIds = result.Attention.RetainedAlternatives
            .SelectMany(a => a.Leaves).Select(l => l.Candidate.Id).ToHashSet(StringComparer.Ordinal);
        var expectedFlat = candidates
            .Select(c => (Candidate: c, Salience: profile.For(c)))
            .OrderByDescending(x => x.Salience)
            .ThenBy(x => x.Candidate.Id, StringComparer.Ordinal)
            .Where(x => retainedIds.Contains(x.Candidate.Id))
            .Select(x => x.Candidate.Id);

        Assert.Equal(expectedFlat, result.Attention.RetainedLeaves.Select(x => x.Candidate.Id));
        Assert.All(result.Attention.RetainedAlternatives.Where(a => a.IsGroup),
            a => Assert.Equal(3, a.Leaves.Count));
        Assert.DoesNotContain(result.Passed, c => c.TargetId == Cast.Grocery);
        Assert.DoesNotContain(result.Passed, c => c.Id == "g-not-backfilled");
        Assert.Contains(result.Rejected,
            r => r.Candidate.TargetId == Cast.Grocery && r.Stage == RejectionStage.Knowledge);
        Assert.Contains(result.Rejected,
            r => r.Candidate.Id == "g-not-backfilled" && r.Stage == RejectionStage.Salience);
    }

    [Fact]
    public void Controlled_and_autonomous_preparation_and_commit_are_actor_neutral()
    {
        var left = TwoTargetPrepared();
        var right = TwoTargetPrepared();

        Assert.Equal(PreparedSignature(left.Prepared), PreparedSignature(right.Prepared));
        Assert.True(left.Prepared.Attention.GroupingActivated);

        var autonomous = Pipeline.Resolve(left.Prepared, null);
        var controlled = Pipeline.Resolve(right.Prepared, autonomous.Chosen!.Candidate.Id);
        Assert.Equal(autonomous.ChosenActionSignature(), controlled.ChosenActionSignature());
        Assert.Equal(autonomous.Outcome, controlled.Outcome);

        var leftOperation = left.World.Get("vincent").Execution.Operations.Single();
        var rightOperation = right.World.Get("vincent").Execution.Operations.Single();
        Assert.Equal(
            (leftOperation.Kind, leftOperation.TargetId, leftOperation.Method, leftOperation.DelegatedToId),
            (rightOperation.Kind, rightOperation.TargetId, rightOperation.Method, rightOperation.DelegatedToId));
    }

    [Fact]
    public void Save_load_replay_reconstructs_grouping_and_the_exact_pending_choice_surface()
    {
        string path = Path.Combine(Path.GetTempPath(), $"ce-grouped-m029-{Guid.NewGuid():N}.db");
        try
        {
            var original = PersistentSession.Start(42, "baseline", "vincent");
            for (int guard = 0; original.Status != SessionStatus.AwaitingChoice && guard < 1000; guard++)
                original.StepEvent();
            Assert.Equal(SessionStatus.AwaitingChoice, original.Status);

            original.Save(path);
            var loaded = PersistentSession.Load(path);

            Assert.NotNull(original.Pending);
            Assert.NotNull(loaded.Pending);
            Assert.Equal(original.Pending!.At, loaded.Pending!.At);
            Assert.Equal(original.Pending.ActorId, loaded.Pending.ActorId);
            Assert.Equal(original.Pending.ActorName, loaded.Pending.ActorName);
            Assert.Equal(original.Pending.ActorRole, loaded.Pending.ActorRole);
            Assert.Equal(original.Pending.ActorPronouns, loaded.Pending.ActorPronouns);
            Assert.Equal(original.Pending.Occasion, loaded.Pending.Occasion);
            Assert.Equal(original.Pending.Focus, loaded.Pending.Focus);
            Assert.Equal(original.Pending.Options, loaded.Pending.Options);
            Assert.Equal(
                PreparedSignature(PreparedOf(original.InnerSession)),
                PreparedSignature(PreparedOf(loaded.InnerSession)));
            Assert.Equal(10, PreparedOf(loaded.InnerSession).Available.Count);
            Assert.True(PreparedOf(loaded.InnerSession).Attention.GroupingActivated);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
            foreach (string suffix in new[] { "-journal", "-wal", "-shm" })
                if (File.Exists(path + suffix)) File.Delete(path + suffix);
        }
    }

    private static (World World, PreparedDecision Prepared) OneTargetPrepared()
    {
        var world = Cast.Build(42, "baseline");
        var actor = world.Get("vincent");
        return (world, Pipeline.Prepare(world, actor, Wake(actor.Id)));
    }

    private static (World World, PreparedDecision Prepared) TwoTargetPrepared()
    {
        var world = Cast.Build(42, "baseline");
        var actor = world.Get("vincent");
        LearnRefusal(actor, Cast.Grocery, world.Now);
        return (world, Pipeline.Prepare(world, actor, Wake(actor.Id)));
    }

    private static (GeneratorContext Context, PreparedDecision Prepared) Context(bool twoTargets)
    {
        var pair = twoTargets ? TwoTargetPrepared() : OneTargetPrepared();
        return (pair.Prepared.Context, pair.Prepared);
    }

    private static (PreparedDecision Prepared, StrategyInstance Operation) ReviewContext()
    {
        var world = Cast.Build(42, "baseline");
        var actor = world.Get("vincent");
        LearnRefusal(actor, Cast.Grocery, world.Now);
        var operation = new StrategyInstance
        {
            OwnerId = actor.Id,
            LocalSequence = actor.StrategyCount++,
            Kind = StrategyKind.SecureTribute,
            Domain = Cast.Harbour,
            TargetId = Cast.Bakery,
            DelegatedToId = "tommy",
            StartedAt = world.Now,
            Deadline = world.Now.AddDays(30),
        };
        actor.Execution.Operations.Add(operation);
        var trigger = new ScheduledEvent
        {
            Id = 123,
            Time = Cast.Start,
            Kind = EventKind.RoleReview,
            OwnerId = actor.Id,
            Cause = "grouped milestone test",
            Payload = new EventPayload
            {
                Note = "operation-review",
                StrategySequence = operation.LocalSequence,
            },
        };
        return (Pipeline.Prepare(world, actor, trigger), operation);
    }

    private static ScheduledEvent Wake(string actorId) => new()
    {
        Id = 123,
        Time = Cast.Start,
        Kind = EventKind.RoleReview,
        OwnerId = actorId,
        Cause = "grouped milestone test",
    };

    private static void LearnRefusal(Character actor, string target, DateTime at)
        => actor.Cognition.Learn(
            new Claim(ClaimKind.BusinessRefusesTribute, target),
            Stance.Knows,
            1,
            SourceKind.Discovery,
            actor.Id,
            at);

    private static IEnumerable<Candidate> Methods(
        string target,
        Claim? requirement = null,
        bool replaceKnownRequirement = false)
    {
        foreach (var method in new[]
                 { CoercionMethod.Persuade, CoercionMethod.Threaten, CoercionMethod.Force })
            yield return Tribute(
                $"job:{target}:{method}", target, method, requirement, replaceKnownRequirement);
    }

    private static Candidate Tribute(
        string id,
        string target,
        CoercionMethod method,
        Claim? additionalRequirement = null,
        bool replaceKnownRequirement = false)
    {
        var requirements = new List<Claim>
        {
            new(ClaimKind.BusinessRefusesTribute, target),
        };
        if (additionalRequirement is { } extra)
        {
            if (replaceKnownRequirement) requirements.Clear();
            requirements.Add(extra);
        }

        return new Candidate(id, ActionKind.StartStrategy, "FromResponsibility", id)
        {
            TargetId = target,
            Strategy = StrategyKind.SecureTribute,
            Method = method,
            Domain = Cast.Harbour,
            RequiredKnowledge = requirements,
        };
    }

    private static Candidate Singleton(string id, ActionKind kind)
        => new(id, kind, "test", id);

    private static PendingDecision RunToFirstPause(SimulationSession session)
    {
        for (int guard = 0; session.Pending is null && guard < 1000; guard++) session.StepEvent();
        Assert.NotNull(session.Pending);
        return session.Pending!;
    }

    private static PreparedDecision PreparedOf(SimulationSession session)
        => (PreparedDecision)typeof(SimulationSession)
            .GetField("_prepared", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(session)!;

    private static Dictionary<string, string> OptionIdsOf(SimulationSession session)
        => (Dictionary<string, string>)typeof(SimulationSession)
            .GetField("_optionIds", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(session)!;

    private static string PreparedSignature(PreparedDecision prepared)
        => string.Join('\n', new[]
        {
            $"active={prepared.Attention.GroupingActivated}",
            "targets=" + string.Join(',', prepared.Attention.EligibleTargetIds),
            "ordered=" + string.Join('|', prepared.Attention.OrderedAlternatives.Select(Describe)),
            "retained=" + string.Join('|', prepared.Attention.RetainedAlternatives.Select(Describe)),
            "leaf-order=" + string.Join('|', prepared.Attention.RetainedLeaves.Select(l => l.Candidate.Id)),
            "generated=" + string.Join('|', prepared.Generated.Select(c => c.Id)),
            "rejected=" + string.Join('|', prepared.Rejected.Select(r => $"{r.Candidate.Id}:{r.Stage}:{r.Reason}")),
            "scored=" + string.Join('|', prepared.Scored.Select(s => $"{s.Candidate.Id}:{s.Total:R}")),
            "available=" + string.Join('|', prepared.Available.Select(c => c.Id)),
        });

    private static string Describe(AttentionAlternative alternative)
        => $"{(alternative.IsGroup ? alternative.GroupKey!.Value.ToString() : "singleton")}:" +
            $"anchor={alternative.Anchor.Id}:salience={alternative.Salience:R}:" +
            $"cancel={alternative.FocusedCancellation}:" +
            $"leaves={string.Join(',', alternative.Leaves.Select(l => l.Candidate.Id))}";
}
