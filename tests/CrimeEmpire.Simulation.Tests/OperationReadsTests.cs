using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Scenario;
using CrimeSim.Session;
using CrimeSim.Sim;
using CrimeSim.Strategy;
using CrimeSim.Trace;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>
/// Milestone 024 — The Operation Reads. A player saw a decision prompt when his character paused and
/// nothing between pauses: he gave an order and the order disappeared. `ExecutionState.Strategy` held
/// what he had running the whole time — kind, target, method, who it was handed to, when it started,
/// how many attempts had come back empty — and none of it reached `PlayerSnapshot`.
///
/// <b>The milestone is one branch, and it is an information rule rather than a display preference.</b>
/// Progress is reported for work he is doing himself, because the step he has reached and the
/// refusals he has met are his own experience. It is withheld for work he handed to somebody, because
/// `StepIndex` on a delegated instance is the executor's state and milestones 017 and 022 both settled
/// that the man who ordered a job learns whether it was carried out "through a report or a discovery
/// roll like anyone else". Handing it over through a panel instead of through a belief makes it no
/// less a leak.
/// </summary>
public sealed class OperationReadsTests
{
    private const int Seed = 42;

    // ================================================================= the boundary

    /// <summary>
    /// The milestone's central claim, proved by difference rather than by inspection: two worlds
    /// identical but for how far a **delegated** operation has advanced produce byte-identical
    /// player-facing text, and the same difference on **own** work does not.
    ///
    /// Asserting only "delegated progress is null" would pass against an implementation that never
    /// showed progress at all. The own-work half is what makes the delegated half mean something.
    /// </summary>
    [Fact]
    public void How_far_a_delegate_has_got_never_reaches_the_player_but_his_own_progress_does()
    {
        var earlyDelegated = Operating(delegated: true, stepIndex: 1);
        var lateDelegated = Operating(delegated: true, stepIndex: 3);

        Assert.Equal(Render(earlyDelegated), Render(lateDelegated));
        Assert.Null(Snapshot(earlyDelegated).Operation!.Progress);

        var earlyOwn = Operating(delegated: false, stepIndex: 1);
        var lateOwn = Operating(delegated: false, stepIndex: 3);

        Assert.NotEqual(Render(earlyOwn), Render(lateOwn));
        Assert.NotNull(Snapshot(earlyOwn).Operation!.Progress);
    }

    /// <summary>
    /// The same rule from the other side: what he ordered, and who has it, are his own acts and are
    /// shown for both. Withholding those too would be a different and wrong milestone — a man does
    /// not forget who he sent.
    /// </summary>
    [Fact]
    public void What_he_ordered_and_who_is_carrying_it_are_always_his_to_know()
    {
        var op = Snapshot(Operating(delegated: true, stepIndex: 2)).Operation;

        Assert.NotNull(op);
        Assert.Contains("Bellini's grocery", op!.Description, StringComparison.Ordinal);
        Assert.Equal("Tommy Nardo", op.ExecutorName);
        Assert.Equal(Cast.Start, op.Since);
    }

    /// <summary>Nothing running, nothing claimed. Null rather than an empty-looking operation.</summary>
    [Fact]
    public void A_man_with_nothing_running_has_no_operation()
    {
        var world = Cast.Build(Seed, "baseline");
        Assert.Null(PlayerView.Build(world, "vincent", world.Now).Operation);
    }

    // ================================================================= what it is allowed to say

    /// <summary>
    /// No developer vocabulary reaches the panel. `StrategyInstance.Label` renders as
    /// `SecureTribute(harbour, target=bellini-grocery, method=Persuade)` and once reached the player
    /// as a decision's focus — milestone 009's first correction. The wording here comes from
    /// <c>PlayerOption.Work</c>, the shared phrasing that exists so there are not two of them.
    ///
    /// And no digits, for the same reason a standing never carries one: the step is named and the
    /// empty attempts are counted in words.
    /// </summary>
    [Fact]
    public void The_operation_never_speaks_in_developer_terms_or_numbers()
    {
        foreach (bool delegated in new[] { true, false })
        {
            var op = Snapshot(Operating(delegated, stepIndex: 2)).Operation!;
            string text = $"{op.Description} {op.ExecutorName} {op.Progress}";

            Assert.DoesNotContain("SecureTribute", text, StringComparison.Ordinal);
            Assert.DoesNotContain("bellini-grocery", text, StringComparison.Ordinal);
            Assert.DoesNotContain("tommy", text, StringComparison.Ordinal);
            Assert.DoesNotContain("StepIndex", text, StringComparison.Ordinal);
            Assert.All("0123456789", d => Assert.DoesNotContain(d.ToString(), text, StringComparison.Ordinal));
        }
    }

    /// <summary>
    /// Empty attempts are his own experience and are said in words. Staged across the range because
    /// the wording changes shape at one, two and more, and a count that read "3" would be the model's
    /// arithmetic rather than something that happened to him.
    /// </summary>
    [Theory]
    [InlineData(0, "made his demand")]
    [InlineData(1, "been refused once")]
    [InlineData(2, "been refused twice")]
    [InlineData(5, "refused again and again")]
    public void Refusals_on_his_own_work_are_counted_in_words(int failures, string expected)
    {
        var world = Operating(delegated: false, stepIndex: 2);
        world.Get("vincent").Execution.Strategy!.FailedAttempts = failures;

        Assert.Contains(expected, Snapshot(world).Operation!.Progress!, StringComparison.Ordinal);
    }

    // ================================================================= natural, and both surfaces

    /// <summary>
    /// It populates in an unmodified run, read through the real projection — and the delegated case
    /// is the one the accepted fixture actually produces, which is the case the boundary protects.
    ///
    /// <c>Since</c> is his own act — when he started the operation he is still watching — and stays
    /// true whether or not he later handed it off, milestone 024's second correction's own half of
    /// this boundary. The natural fixture starts the operation on 2 March and hands it to Tommy on
    /// the 14th, so this is a genuine, not a vacuous, proof: a delegation date being wrongly used as
    /// the start date would happen to read correctly for Vincent regardless (he holds the start
    /// either way), which is exactly why the executor's own half below is the one that falsifies it.
    /// </summary>
    [Fact]
    public void The_panel_is_populated_during_a_natural_run()
    {
        var world = Cast.Build(Seed, "baseline");
        Runner.Run(world, Cast.Start.AddDays(20));

        var op = PlayerView.Build(world, "vincent", world.Now).Operation;

        Assert.NotNull(op);
        Assert.Equal("Tommy Nardo", op!.ExecutorName);
        Assert.Null(op.Progress);
        // The date, not the exact instant — the same precision the rendered "running since 2 Mar"
        // ever carries; the operation actually starts partway through 2 March, not at Cast.Start's
        // own midnight-adjacent instant.
        Assert.Equal(Cast.Start.Date, op.Since!.Value.Date);
    }

    /// <summary>
    /// The other half of the natural proof above, and the reason `Operating` was corrected — Codex's
    /// review of `f993386` found it read only the viewpoint's own <c>Execution.Strategy</c>, which is
    /// the owner's record and stays null on a delegate for the instance's entire life, so Tommy —
    /// actually carrying out the identical operation the previous test reads from Vincent's side —
    /// saw nothing running at all. Same run, same operation, the other man's view of it: his own
    /// progress is his own experience, so it is shown, unlike the previous test's null.
    ///
    /// <c>Since</c> is null for him — milestone 024's second correction. He was delegated on 14
    /// March, not 2 March; showing him <c>StartedAt</c> (the owner's own act) would read as "you have
    /// had this since the 2nd," which he did not.
    /// </summary>
    [Fact]
    public void The_executor_sees_the_operation_he_is_carrying_with_his_own_progress()
    {
        var world = Cast.Build(Seed, "baseline");
        Runner.Run(world, Cast.Start.AddDays(20));

        var op = PlayerView.Build(world, "tommy", world.Now).Operation;

        Assert.NotNull(op);
        Assert.Contains("Bellini's grocery", op!.Description, StringComparison.Ordinal);
        Assert.NotNull(op.Progress);
        Assert.Null(op.Since);
    }

    /// <summary>
    /// Actor-neutral in the other direction too: a man who is neither the owner nor the one carrying
    /// it out sees no operation at all, in the identical natural run the two tests above read.
    /// </summary>
    [Fact]
    public void An_unrelated_character_sees_no_operation_in_the_same_natural_run()
    {
        var world = Cast.Build(Seed, "baseline");
        Runner.Run(world, Cast.Start.AddDays(20));

        Assert.Null(PlayerView.Build(world, "salvatore", world.Now).Operation);
    }

    /// <summary>
    /// The <see cref="IntelligenceWriter"/> counterpart to the panel proof above, read from its actual
    /// rendered text rather than the snapshot — the same discipline the TakenFor correction (`53694a2`)
    /// established for the roster surface. "Bellini's grocery" already appears in the unrelated
    /// "WHAT HE HAS" belief list for both men, so a check that did not isolate "WHAT HE HAS OUT" from
    /// "HOW HE TAKES THEM" would pass even if the operation section rendered nothing at all —
    /// demonstrated, not assumed, by the first assertion inside the helper below.
    /// </summary>
    [Fact]
    public void The_operation_section_is_isolated_in_the_runners_render_for_both_men()
    {
        var world = Cast.Build(Seed, "baseline");
        Runner.Run(world, Cast.Start.AddDays(20));

        string vincentRendered = IntelligenceWriter.Render(world, "vincent");
        CheckOperationSection(vincentRendered, mustContain: "Tommy Nardo is handling it", mustNotContain: "made his demand");

        string tommyRendered = IntelligenceWriter.Render(world, "tommy");
        CheckOperationSection(tommyRendered, mustContain: "made his demand", mustNotContain: "is handling it");
    }

    // ================================================================= one operation per executor
    //
    // Milestone 024's second correction. `Operating`'s own fallback scan assumed at most one match
    // could ever exist without anything enforcing it — a subordinate could be handed a second
    // operation while still carrying a first, or be running one of his own at the same time nobody
    // had checked. The rule is enforced twice: never offered as a delegate
    // (`Generators.FromRelationship`, reading `GeneratorContext.AvailableSubordinateIds`) and refused
    // if a candidate reaches commitment anyway (`Commit.Apply`, fail-closed) — both calling the one
    // shared definition, `Pipeline.AvailableToExecute`.

    /// <summary>
    /// A subordinate who already owns a strategy of his own is not offered as another operation's
    /// executor — through the candidate/filter path, the same way an unacquainted stranger is not
    /// offered (`An_organisationally_subordinate_but_unacquainted_stranger_is_not_offered_as_a_delegate`,
    /// `ExecutorSuitabilityTests.cs`): never generated, not generated-then-rejected.
    /// </summary>
    [Fact]
    public void A_subordinate_who_owns_a_strategy_is_not_offered_as_another_operations_executor()
    {
        var world = Cast.Build(Seed, "baseline");
        var vincent = world.Get("vincent");
        var tommy = world.Get("tommy");

        vincent.Execution.Strategy = NewStrategy(vincent, Cast.Grocery, vincent.StrategyCount++);
        tommy.Execution.Strategy = NewStrategy(tommy, Cast.Bakery, tommy.StrategyCount++);

        var ctx = Context(world, vincent, subordinateIds: new[] { "tommy" }, acquainted: "tommy");

        var delegateCandidates = Generators.GenerateAll(ctx)
            .Where(c => c.Kind == ActionKind.DelegateStrategy)
            .ToList();
        Assert.DoesNotContain(delegateCandidates, c => c.TargetId == "tommy");
    }

    /// <summary>
    /// The other way to already be busy: not owning a strategy at all, but carrying one delegated by
    /// somebody else. Salvatore delegates first (through the real `Commit.Apply` path, so the world
    /// state this reads is genuinely produced rather than hand-assembled); Vincent's own attempt to
    /// name Tommy as a second executor must not be offered.
    /// </summary>
    [Fact]
    public void A_subordinate_already_executing_delegated_work_is_not_offered_a_second_delegation()
    {
        var world = Cast.Build(Seed, "baseline");
        var vincent = world.Get("vincent");
        var salvatore = world.Get("salvatore");

        DelegateThroughCommit(world, salvatore, Cast.Bakery, "tommy");
        Assert.Equal("tommy", salvatore.Execution.Strategy!.DelegatedToId);

        // Vincent needs an undelegated strategy of his own, or FromRelationship's whole branch never
        // runs and the assertion below would pass vacuously regardless of availability.
        vincent.Execution.Strategy = NewStrategy(vincent, Cast.Grocery, vincent.StrategyCount++);
        var ctx = Context(world, vincent, subordinateIds: new[] { "tommy" }, acquainted: "tommy");

        var delegateCandidates = Generators.GenerateAll(ctx)
            .Where(c => c.Kind == ActionKind.DelegateStrategy)
            .ToList();
        Assert.DoesNotContain(delegateCandidates, c => c.TargetId == "tommy");
    }

    /// <summary>
    /// The fail-closed half: a hand-built <see cref="ActionKind.DelegateStrategy"/> candidate that
    /// skipped filtering — the same shape <c>Commit.StartStrategy</c>'s own <c>ConcealIncident</c>
    /// guard exists for — is refused at commitment, not merely left unoffered.
    /// </summary>
    [Fact]
    public void Commit_refuses_to_delegate_to_a_subordinate_who_owns_a_strategy()
    {
        var world = Cast.Build(Seed, "baseline");
        var vincent = world.Get("vincent");
        var tommy = world.Get("tommy");

        tommy.Execution.Strategy = NewStrategy(tommy, Cast.Bakery, tommy.StrategyCount++);
        vincent.Execution.Strategy = NewStrategy(vincent, Cast.Grocery, vincent.StrategyCount++);

        var ctx = Context(world, vincent);
        var delegateCandidate = DelegateCandidate(vincent.Execution.Strategy, "tommy");

        Assert.Throws<SimulationInvariantException>(() =>
            Commit.Apply(world, vincent, delegateCandidate, ctx.Agenda, ctx, new List<string>()));
    }

    /// <summary>The fail-closed half of the already-executing-elsewhere case above.</summary>
    [Fact]
    public void Commit_refuses_to_delegate_to_a_subordinate_already_executing_delegated_work()
    {
        var world = Cast.Build(Seed, "baseline");
        var vincent = world.Get("vincent");
        var salvatore = world.Get("salvatore");

        DelegateThroughCommit(world, salvatore, Cast.Bakery, "tommy");
        vincent.Execution.Strategy = NewStrategy(vincent, Cast.Grocery, vincent.StrategyCount++);

        var ctx = Context(world, vincent);
        var delegateCandidate = DelegateCandidate(vincent.Execution.Strategy, "tommy");

        Assert.Throws<SimulationInvariantException>(() =>
            Commit.Apply(world, vincent, delegateCandidate, ctx.Agenda, ctx, new List<string>()));
    }

    // ================================================================= through Pipeline.Prepare
    //
    // Milestone 024's third correction. The four tests above prove the rule against a hand-built
    // `GeneratorContext`, which exercises `Generators`/`Commit` directly but never `Pipeline.Prepare`
    // itself — the one place `AvailableSubordinateIds` is actually computed and wired onto the
    // context a real deliberation uses. These four drive Vincent through the genuine pipeline
    // (`Runner.Step`, which calls `Pipeline.Prepare` for the controlled character) to his own
    // delegation fork, and read `PreparedDecision.Available` — the same surface
    // `ExecutorSuitabilityTests.Both_subordinates_are_independently_eligible_at_the_fork` reads.

    /// <summary>
    /// The positive control every negative case below depends on: with nothing making him busy,
    /// Tommy is genuinely offered. Without this, the three negative tests would be unfalsifiable —
    /// Tommy being absent could as easily mean the pipeline never offers him at all.
    /// </summary>
    [Fact]
    public void A_free_nameable_subordinate_is_offered_through_pipeline_prepare()
    {
        var world = Cast.Build(Seed, "baseline");
        var prepared = AdvanceVincentToDelegationFork(world);

        var delegateCandidates = prepared.Available.Where(c => c.Kind == ActionKind.DelegateStrategy).ToList();
        Assert.Contains(delegateCandidates, c => c.TargetId == "tommy");
    }

    /// <summary>Tommy owns an undelegated operation of his own before Vincent ever reaches his fork.</summary>
    [Fact]
    public void A_subordinate_owning_an_undelegated_operation_is_not_offered_through_pipeline_prepare()
    {
        var world = Cast.Build(Seed, "baseline");
        var tommy = world.Get("tommy");
        tommy.Execution.Strategy = NewStrategy(tommy, Cast.Bakery, tommy.StrategyCount++);

        var prepared = AdvanceVincentToDelegationFork(world);

        var delegateCandidates = prepared.Available.Where(c => c.Kind == ActionKind.DelegateStrategy).ToList();
        Assert.DoesNotContain(delegateCandidates, c => c.TargetId == "tommy");
        Assert.DoesNotContain(prepared.Rejected,
            r => r.Candidate.Kind == ActionKind.DelegateStrategy && r.Candidate.TargetId == "tommy");
    }

    /// <summary>Tommy is already carrying work Salvatore delegated to him, through the real commit path.</summary>
    [Fact]
    public void A_subordinate_carrying_delegated_work_is_not_offered_through_pipeline_prepare()
    {
        var world = Cast.Build(Seed, "baseline");
        var salvatore = world.Get("salvatore");
        DelegateThroughCommit(world, salvatore, Cast.Bakery, "tommy");

        var prepared = AdvanceVincentToDelegationFork(world);

        var delegateCandidates = prepared.Available.Where(c => c.Kind == ActionKind.DelegateStrategy).ToList();
        Assert.DoesNotContain(delegateCandidates, c => c.TargetId == "tommy");
        Assert.DoesNotContain(prepared.Rejected,
            r => r.Candidate.Kind == ActionKind.DelegateStrategy && r.Candidate.TargetId == "tommy");
    }

    /// <summary>
    /// The broader rule, pinned rather than merely implied: Tommy owns a strategy of his own and has
    /// already handed it onward to a third man (Kane) — he is neither running it himself nor free,
    /// he is the <em>owner</em> of a delegated instance. <c>AvailableToExecute</c>'s own check is
    /// "owns a strategy," full stop, not "owns an undelegated one," so this must exclude him too, and
    /// is the one case among the four that a narrower "not currently delegated to" reading would
    /// wrongly pass.
    /// </summary>
    [Fact]
    public void A_subordinate_who_delegated_his_own_operation_onward_is_still_not_offered_through_pipeline_prepare()
    {
        var world = Cast.Build(Seed, "baseline");
        var tommy = world.Get("tommy");
        DelegateThroughCommit(world, tommy, Cast.Bakery, "kane");
        Assert.NotNull(tommy.Execution.Strategy);
        Assert.Equal("kane", tommy.Execution.Strategy!.DelegatedToId);

        var prepared = AdvanceVincentToDelegationFork(world);

        var delegateCandidates = prepared.Available.Where(c => c.Kind == ActionKind.DelegateStrategy).ToList();
        Assert.DoesNotContain(delegateCandidates, c => c.TargetId == "tommy");
        Assert.DoesNotContain(prepared.Rejected,
            r => r.Candidate.Kind == ActionKind.DelegateStrategy && r.Candidate.TargetId == "tommy");
    }

    // ================================================================= helpers

    private static void CheckOperationSection(string rendered, string mustContain, string mustNotContain)
    {
        int opStart = rendered.IndexOf("WHAT HE HAS OUT", StringComparison.Ordinal);
        Assert.True(opStart >= 0, "the render has no \"WHAT HE HAS OUT\" section");

        int nextSection = rendered.IndexOf("HOW HE TAKES THEM", opStart, StringComparison.Ordinal);
        Assert.True(nextSection >= 0, "no \"HOW HE TAKES THEM\" marker found after the operation section");

        string section = rendered[opStart..nextSection];

        Assert.Contains("Bellini's grocery", rendered[..opStart], StringComparison.Ordinal);

        Assert.Contains(mustContain, section, StringComparison.Ordinal);
        Assert.DoesNotContain(mustNotContain, section, StringComparison.Ordinal);
    }

    /// <summary>
    /// A world with one operation running, optionally handed to Tommy, advanced to a given step.
    /// Built by assignment rather than by driving the pipeline, because the point is to vary
    /// <c>StepIndex</c> alone while holding everything else identical — which a real run cannot do.
    /// </summary>
    private static World Operating(bool delegated, int stepIndex)
    {
        var world = Cast.Build(Seed, "baseline");
        var vincent = world.Get("vincent");

        vincent.Execution.Strategy = new StrategyInstance
        {
            OwnerId = vincent.Id,
            LocalSequence = vincent.StrategyCount++,
            Kind = StrategyKind.SecureTribute,
            Domain = Cast.Harbour,
            TargetId = Cast.Grocery,
            Method = CoercionMethod.Persuade,
            StartedAt = Cast.Start,
            Deadline = Cast.Start.AddDays(30),
            StepIndex = stepIndex,
            DelegatedToId = delegated ? "tommy" : null,
        };

        return world;
    }

    private static PlayerSnapshot Snapshot(World world)
        => PlayerView.Build(world, "vincent", world.Now);

    /// <summary>Everything about the operation the player can see, as one string to diff.</summary>
    private static string Render(World world)
    {
        var op = Snapshot(world).Operation;
        return op is null ? "" : $"{op.Description}|{op.ExecutorName}|{op.Since:O}|{op.Progress}";
    }

    // ================================================================= helpers — one operation per executor

    /// <summary>An undelegated SecureTribute instance, staged rather than driven through the pipeline.</summary>
    private static StrategyInstance NewStrategy(Character owner, string targetId, int localSequence)
        => new()
        {
            OwnerId = owner.Id,
            LocalSequence = localSequence,
            Kind = StrategyKind.SecureTribute,
            Domain = Cast.Harbour,
            TargetId = targetId,
            Method = CoercionMethod.Persuade,
            StartedAt = Cast.Start,
            Deadline = Cast.Start.AddDays(30),
        };

    private static Candidate DelegateCandidate(StrategyInstance? s, string executorId)
        => new($"delegate:{s!.Kind}:{executorId}", ActionKind.DelegateStrategy, "test", $"have {executorId} take it on")
        {
            TargetId = executorId,
            Strategy = s.Kind,
            Method = s.Method,
            Domain = s.Domain,
            RequiredCrew = 1,
        };

    /// <summary>
    /// Starts a strategy for <paramref name="owner"/> and hands it to <paramref name="executorId"/>,
    /// both through the real <see cref="Commit.Apply"/> path — mirroring
    /// <c>DirectActionVsDelegationTests.OpenTributeCase</c> — so the executor's busy state this
    /// stages is genuinely produced rather than hand-assembled.
    /// </summary>
    private static void DelegateThroughCommit(World world, Character owner, string targetId, string executorId)
    {
        var ctx = Context(world, owner);
        var start = new Candidate($"start:tribute:{targetId}", ActionKind.StartStrategy, "test", $"lean on {targetId}")
        {
            TargetId = targetId,
            Strategy = StrategyKind.SecureTribute,
            Domain = Cast.Harbour,
            Method = CoercionMethod.Persuade,
        };
        Commit.Apply(world, owner, start, ctx.Agenda, ctx, new List<string>());
        Commit.Apply(
            world, owner, DelegateCandidate(owner.Execution.Strategy, executorId), ctx.Agenda, ctx, new List<string>());
    }

    /// <summary>
    /// Drives Vincent through the real pipeline (<see cref="Runner.Step"/>, which calls
    /// <see cref="Pipeline.Prepare"/> for the controlled character) to his own first pause, mirroring
    /// <c>ExecutorSuitabilityTests.AdvanceToVincentsNextPause</c> — a private copy, per this project's
    /// practice of not sharing test-local helpers across files that check the same assumption.
    /// </summary>
    private static PreparedDecision AdvanceVincentToFirstPause(World world)
    {
        for (int guard = 0; guard < 5000; guard++)
        {
            var step = Runner.Step(world, DateTime.MaxValue, "vincent");
            if (step.Status == StepStatus.AwaitingChoice) return step.Awaiting!;
            if (step.Status == StepStatus.Exhausted)
                throw new InvalidOperationException("queue exhausted before Vincent ever paused");
        }
        throw new InvalidOperationException("guard exceeded before Vincent ever paused");
    }

    /// <summary>
    /// Starts Vincent's own SecureTribute against Bellini's grocery, then advances to his next
    /// pause — the same fork <c>ExecutorSuitabilityTests.AdvanceToVincentsFork</c> reaches, reproduced
    /// locally so this file's own coverage of the delegation-eligibility rule does not depend on
    /// that file's helper.
    /// </summary>
    private static PreparedDecision AdvanceVincentToDelegationFork(World world)
    {
        var first = AdvanceVincentToFirstPause(world);
        var startCandidate = first.Available.Single(c =>
            c.Kind == ActionKind.StartStrategy && c.Strategy == StrategyKind.SecureTribute
            && c.TargetId == Cast.Grocery && c.Method == CoercionMethod.Persuade);
        Pipeline.Resolve(first, startCandidate.Id);

        return AdvanceVincentToFirstPause(world);
    }

    /// <summary>
    /// Mirrors the <c>Context</c> helper every other Decision-layer test file carries its own copy
    /// of (per this project's practice of not sharing the same test-local constant or helper across
    /// files that check the same assumption). <c>AvailableSubordinateIds</c> is computed through
    /// <see cref="Pipeline.AvailableToExecute"/> itself, the one production definition, rather than
    /// re-derived by hand — so a test staging a busy subordinate exercises the real rule.
    /// </summary>
    private static GeneratorContext Context(
        World world, Character actor, IReadOnlyList<string>? subordinateIds = null, params string[] acquainted)
        => new(
            actor.View,
            Salience.Perceive(actor, world.Now),
            new Agenda(AgendaKind.DischargeResponsibility, "clear the family's business", "test", Cast.Harbour),
            world.Now,
            new ScheduledEvent
            {
                Id = 0,
                Time = world.Now,
                Kind = EventKind.RoleReview,
                OwnerId = actor.Id,
                Cause = "test",
            },
            MyOffice: null,
            MyAssignment: null,
            KnownPolicies: Array.Empty<Policy>(),
            SuperiorId: null,
            SubordinateIds: subordinateIds ?? Array.Empty<string>(),
            OrgMemberIds: Array.Empty<string>(),
            AcquaintedIds: acquainted,
            ReportsSent: Array.Empty<Report>(),
            RequestsMade: Array.Empty<InformationRequest>(),
            VisibleTargets: Array.Empty<string>(),
            AvailableSubordinateIds: (subordinateIds ?? Array.Empty<string>())
                .Where(id => Pipeline.AvailableToExecute(world, id)).ToList(),
            CurrentExecution: Strategies.CurrentExecution(world, actor));
}
