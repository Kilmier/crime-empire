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
/// Milestone 020 — The Right Person for the Job. `Generators.FromRelationship` used to delegate to
/// the single highest-trust subordinate available — deterministically Tommy, Vincent's only
/// organisational subordinate — with nothing about who would actually do the job better entering
/// the choice (`ROADMAP.md`, "Executor suitability/capability is not modelled"). The new
/// `capable-angelo` variant gives Vincent a second subordinate — Angelo Conti, harder-hitting than
/// Tommy (Coercion 0.80 vs. 0.55) but far less trusted (Vincent's trust in him is 0.35 against
/// Tommy's 0.70) — so the fork is a genuine comparison rather than a foregone one.
///
/// Three production changes, each minimal and each proven here:
///
///   1. <c>FromRelationship</c> now yields one <see cref="ActionKind.DelegateStrategy"/> candidate
///      per subordinate rather than pre-selecting one. With exactly one subordinate (every existing
///      accepted variant) this degenerates to exactly the one candidate it always produced.
///   2. <see cref="Utility"/> gained one new score component, "executor capability", reading
///      <see cref="Candidate.ExecutorCoercion"/> — set only when there are two or more subordinates
///      to compare, so it is <c>null</c> and never emitted for every existing accepted variant.
///   3. <see cref="Strategies.ResolveViolence"/> now scales its resistance reduction by the actual
///      executor's Coercion, calibrated so it reduces to exactly the old flat <c>0.3</c> at Tommy's
///      own Coercion (0.55) — the only value that call has ever been exercised against in an
///      accepted run — so no existing trace hash moves.
///
/// <b>What is deliberately not re-proven here.</b> Controlling every character (including Angelo)
/// with every pause auto-resolved, reproducing the fully autonomous history, and viewpoint identity
/// never changing simulation history are already covered — for `capable-angelo` specifically, not
/// merely in principle — by <c>ControlledAutonomousParityTests</c>'s and
/// <c>PlayerSessionTests</c>'s existing sweeps, both of which iterate <see cref="Variants.All"/>
/// generically. Adding <c>capable-angelo</c> to that array is what extends them; no new test code
/// was needed, and the full suite was confirmed still green after the addition. The same is true of
/// the "no player-facing phrase carries a decimal" regression. What remains genuinely new — the
/// fork itself, eligibility, the capability score's presence and absence, the executor-scaled force
/// outcome, save/load through the new fork, and a targeted check that the new score vocabulary
/// specifically never reaches a player surface — is what follows.
/// </summary>
public sealed class ExecutorSuitabilityTests
{
    private const int Seed = 42;
    private const string Variant = "capable-angelo";
    private const string Baseline = "baseline";
    private const string Vincent = "vincent";
    private const string Tommy = "tommy";
    private const string Angelo = "angelo";

    private static DateTime End => Cast.Start.AddDays(90);

    // Independently pinned, matching DirectActionVsDelegationTests' own copy, per this project's
    // practice of not sharing the same constant across files that check the same assumption.
    private const string StartPersuade = "talk Bellini's grocery round";
    private const string CarryOn = "carry on getting Bellini's grocery to pay";
    private const string DelegateToTommy = "have Tommy Nardo take it on";
    private const string DelegateToAngelo = "have Angelo Conti take it on";

    // ================================================================= natural + eligibility

    /// <summary>
    /// The executable feature claim: Vincent's fork now offers continuation and delegation to
    /// <em>either</em> subordinate together, in the same <see cref="PendingDecision"/>. Direct
    /// falsifier of mutation check 1 (highest-trust-only generation): reverting
    /// <c>FromRelationship</c> to its pre-020 <c>OrderByDescending(trust).First()</c> would offer
    /// only Tommy (0.70 trust beats Angelo's 0.35), and this fails.
    /// </summary>
    [Fact]
    public void Vincents_fork_offers_continuation_and_both_subordinates_together()
    {
        var session = SimulationSession.Start(Seed, Variant, Vincent);
        var pending = ReachFork(session, End);

        var descriptions = pending.Options.Select(o => o.Description).ToList();
        Assert.Contains(CarryOn, descriptions);
        Assert.Contains(DelegateToTommy, descriptions);
        Assert.Contains(DelegateToAngelo, descriptions);
    }

    /// <summary>
    /// Eligibility: both delegate candidates survive <see cref="Filters"/> — salience, knowledge,
    /// capability, access — independently, and both appear in
    /// <see cref="PreparedDecision.Available"/>, not merely among what was generated. Read through
    /// the real production pipeline (<see cref="Runner.Step"/>/<see cref="Pipeline.Resolve"/>), not
    /// a staged candidate set.
    /// </summary>
    [Fact]
    public void Both_subordinates_are_independently_eligible_at_the_fork()
    {
        var world = Cast.Build(Seed, Variant);
        var prepared = AdvanceToVincentsFork(world);

        var delegateCandidates = prepared.Available
            .Where(c => c.Kind == ActionKind.DelegateStrategy)
            .ToList();
        Assert.Equal(2, delegateCandidates.Count);
        Assert.Contains(delegateCandidates, c => c.TargetId == Tommy);
        Assert.Contains(delegateCandidates, c => c.TargetId == Angelo);

        // Neither is a stray rejection dressed up as availability — both are absent from Rejected.
        Assert.DoesNotContain(prepared.Rejected, r => r.Candidate.Kind == ActionKind.DelegateStrategy);
    }

    // ================================================================= scoring: capability present/absent

    /// <summary>
    /// Both delegate candidates carry their own executor's Coercion, and <see cref="Utility"/>
    /// scores them differently for it — a real, separately-named "executor capability" component,
    /// never folded into "relationship effects" (milestone 008's facet-tagging discipline). Direct
    /// falsifier of mutation check 2 (capability-free scoring): forcing
    /// <see cref="Candidate.ExecutorCoercion"/> to <c>null</c> for both would make the component
    /// absent from both breakdowns, and this fails.
    /// </summary>
    [Fact]
    public void Both_delegation_candidates_carry_their_own_executors_coercion_and_score_differently_for_it()
    {
        var world = Cast.Build(Seed, Variant);
        var prepared = AdvanceToVincentsFork(world);

        var tommyScored = prepared.Scored.Single(
            s => s.Candidate.Kind == ActionKind.DelegateStrategy && s.Candidate.TargetId == Tommy);
        var angeloScored = prepared.Scored.Single(
            s => s.Candidate.Kind == ActionKind.DelegateStrategy && s.Candidate.TargetId == Angelo);

        Assert.Equal(0.55, tommyScored.Candidate.ExecutorCoercion);
        Assert.Equal(0.80, angeloScored.Candidate.ExecutorCoercion);

        var tommyCapability = tommyScored.Components.Single(c => c.Name == "executor capability");
        var angeloCapability = angeloScored.Components.Single(c => c.Name == "executor capability");

        // Centered on 0.5: Tommy (0.55) reads barely above it, Angelo (0.80) reads well above it —
        // and the component is tagged None, never a relationship facet.
        Assert.True(angeloCapability.Value > tommyCapability.Value);
        Assert.Equal(RelationshipFacet.None, tommyCapability.Reads);
        Assert.Equal(RelationshipFacet.None, angeloCapability.Reads);
    }

    /// <summary>
    /// The regression that guarantees every existing accepted variant's hash cannot move: with
    /// exactly one subordinate, <see cref="Candidate.ExecutorCoercion"/> is <c>null</c> and the
    /// "executor capability" component is never emitted at all — not emitted-at-zero, genuinely
    /// absent, so <c>Utility.Add</c>'s near-zero guard never even has to fire.
    /// </summary>
    [Fact]
    public void A_single_subordinate_gets_no_executor_capability_comparison()
    {
        var world = Cast.Build(Seed, Baseline);
        var prepared = AdvanceToVincentsFork(world, Baseline);

        var tommyScored = prepared.Scored.Single(s => s.Candidate.Kind == ActionKind.DelegateStrategy);
        Assert.Null(tommyScored.Candidate.ExecutorCoercion);
        Assert.DoesNotContain(tommyScored.Components, c => c.Name == "executor capability");
    }

    // ================================================================= staged: attribution + capability-scaled resolution

    /// <summary>
    /// Force resolves through the real production path once with each subordinate as executor,
    /// from otherwise-identical staged state — mirroring milestone 017's Section B idiom exactly,
    /// applied to the second subordinate. Proves three things at once:
    ///
    ///   - attribution names the actual executor (milestone 017's rule, reprised for Angelo — the
    ///     direct falsifier of mutation check 4, attributing the witness claim to the owner);
    ///   - the owner, when he differs, holds no first-hand copy;
    ///   - the resistance drop is the executor's own Coercion run through the calibrated formula,
    ///     not a flat constant and not the owner's Coercion (the direct falsifier of mutation
    ///     check 3, reading <c>owner.Capabilities[Skill.Coercion]</c> instead of the executor's —
    ///     Vincent's 0.75 would give 0.38 for both cases below, matching neither expected value).
    /// </summary>
    [Theory]
    [InlineData(Tommy, 0.55, 0.30)]
    [InlineData(Angelo, 0.80, 0.40)]
    public void Force_resolution_is_attributed_to_and_scaled_by_the_actual_executor(
        string executorId, double expectedCoercion, double expectedReduction)
    {
        var world = Cast.Build(Seed, Variant);
        var owner = world.Get(Vincent);
        var executor = world.Get(executorId);
        var business = world.Businesses[Cast.Grocery];
        double startingResistance = business.Resistance;

        Assert.Equal(expectedCoercion, executor.Capabilities[Skill.Coercion]);

        var s = OpenTributeCase(world, owner, executor, CoercionMethod.Force);
        AdvanceTributeSteps(world, executor, s, steps: 3); // approach, demand, press (force resolves here)

        var violenceEvent = world.TruthLog.Single(e => e.Kind == "violence");
        Assert.Equal(executorId, violenceEvent.ActorId);

        // Scoped to a single observer (Kane — Investigation 0.70 clears the bystander-witness
        // threshold every time) because ResolveViolence can offer the identical claim to several
        // eligible observers at once; matching DirectActionVsDelegationTests' own precedent rather
        // than asserting on whichever happens to be first.
        var opportunity = Drain(world).SingleOrDefault(e =>
            e.Kind == EventKind.ObservationOpportunity && e.OwnerId == "kane"
            && e.Payload.Claims.Any(c => c.Kind == ClaimKind.WitnessSawIncident && c.EventId == violenceEvent.Id));
        Assert.True(opportunity is not null,
            "ResolveViolence did not schedule any observation opportunity for this incident, so " +
            "attribution cannot be checked against production output");
        var witnessClaim = opportunity!.Payload.Claims.Single(c => c.Kind == ClaimKind.WitnessSawIncident);
        Assert.Equal(executorId, witnessClaim.Object);

        var violenceClaims = executor.Cognition.OfKind(ClaimKind.PersonUsedViolence).ToList();
        Assert.Contains(violenceClaims, r => r.Claim.Subject == executorId);
        if (owner.Id != executor.Id)
            Assert.DoesNotContain(owner.Cognition.Records, r => r.Claim.Kind == ClaimKind.PersonUsedViolence);

        Assert.Equal(startingResistance - expectedReduction, business.Resistance, precision: 9);
    }

    /// <summary>
    /// The natural run's own consequence, not only a staged one: whichever subordinate Vincent
    /// actually delegates to at seed 42, that same man — never the owner, never the other
    /// subordinate — is the one whose name is on the violence when force is eventually applied.
    /// Combined with the staged proof above (which pins the exact numeric outcome each executor's
    /// own Coercion produces from identical state), this ties the natural run's choice to a real,
    /// production-computed consequence rather than asserting the arithmetic a second time across a
    /// full 90-day run, where other events (Marco's own concession) also move
    /// <see cref="Business.Resistance"/> and would make a hand-derived expected total fragile.
    /// </summary>
    [Fact]
    public void The_natural_runs_chosen_executor_is_who_throws_the_punch()
    {
        var world = Cast.Build(Seed, Variant);
        Runner.Run(world, End);

        var delegation = world.Decisions.Single(d =>
            d.Chosen?.Candidate.Kind == ActionKind.DelegateStrategy
            && d.Chosen.Candidate.TargetId is Tommy or Angelo);
        string executorId = delegation.Chosen!.Candidate.TargetId!;

        var violence = world.TruthLog.SingleOrDefault(e => e.Kind == "violence");
        Assert.True(violence is not null, "no force was applied in the natural run, so this proves nothing");
        Assert.Equal(executorId, violence!.ActorId);
    }

    // ================================================================= save/load through the fork

    /// <summary>
    /// The fork forced through a save and two independent loads, each continued to a real
    /// consequence and compared against an equivalent unsaved control — mirroring milestone 017's
    /// Section C exactly, applied to the Tommy-vs-Angelo choice instead of continue-vs-delegate.
    /// </summary>
    [Fact]
    public void Save_and_load_from_the_fork_reproduces_either_chosen_executor()
    {
        string path = Path.Combine(Path.GetTempPath(), $"ce-m020-fork-{Guid.NewGuid():N}.db");
        try
        {
            var setup = CrimeEmpire.Persistence.Session.PersistentSession.Start(Seed, Variant, Vincent);
            AdvanceToNextPause(setup);
            ChooseByDescription(setup, StartPersuade);
            AdvanceToNextPause(setup); // the fork: carry on / delegate-Tommy / delegate-Angelo
            setup.Save(path);

            var tommyLoaded = CrimeEmpire.Persistence.Session.PersistentSession.Load(path);
            ChooseByDescription(tommyLoaded, DelegateToTommy);
            SettleLastOption(tommyLoaded);

            var angeloLoaded = CrimeEmpire.Persistence.Session.PersistentSession.Load(path);
            ChooseByDescription(angeloLoaded, DelegateToAngelo);
            SettleLastOption(angeloLoaded);

            var tommyControl = CrimeEmpire.Persistence.Session.PersistentSession.Start(Seed, Variant, Vincent);
            AdvanceToNextPause(tommyControl);
            ChooseByDescription(tommyControl, StartPersuade);
            AdvanceToNextPause(tommyControl);
            ChooseByDescription(tommyControl, DelegateToTommy);
            SettleLastOption(tommyControl);

            var angeloControl = CrimeEmpire.Persistence.Session.PersistentSession.Start(Seed, Variant, Vincent);
            AdvanceToNextPause(angeloControl);
            ChooseByDescription(angeloControl, StartPersuade);
            AdvanceToNextPause(angeloControl);
            ChooseByDescription(angeloControl, DelegateToAngelo);
            SettleLastOption(angeloControl);

            AssertPersistentEquivalence(tommyLoaded, tommyControl);
            AssertPersistentEquivalence(angeloLoaded, angeloControl);

            var tommyWorld = tommyLoaded.InnerSession.World;
            var angeloWorld = angeloLoaded.InnerSession.World;
            Assert.True(
                tommyWorld.TruthLog.Count > 1 && angeloWorld.TruthLog.Count > 1,
                "the settled continuation barely moved past the fork, so this proves nothing about a " +
                "real consequence");
            Assert.NotEqual(
                TraceWriter.Render(tommyWorld, Variant, false),
                TraceWriter.Render(angeloWorld, Variant, false));

            Assert.Contains(Tommy, tommyWorld.Get(Vincent).Execution.DelegatedExecutorIds);
            Assert.Contains(Angelo, angeloWorld.Get(Vincent).Execution.DelegatedExecutorIds);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    // ================================================================= preference leakage

    /// <summary>
    /// The developer-only vocabulary this milestone introduces — the "executor capability" reason
    /// text and both subordinates' raw Coercion figures — never reaches the fork's player-facing
    /// options. Direct falsifier of mutation check 5: having <c>PlayerOption</c> or
    /// <c>PendingDecision</c> include the score or the capability reason text makes this fail.
    ///
    /// Narrower than, and additional to, <c>PlayerSessionTests</c>' existing generic "no player
    /// phrase carries a decimal" regression (already extended to this variant for free by adding it
    /// to <see cref="Variants.All"/>): that check catches a leaked <em>number</em>, not a leaked
    /// <em>phrase</em> with no digits in it, such as "better suited" or "not the man for rough
    /// work".
    /// </summary>
    [Fact]
    public void Neither_delegates_capability_reasoning_or_score_reaches_the_player()
    {
        var session = SimulationSession.Start(Seed, Variant, Vincent);
        var pending = ReachFork(session, End);

        string text = string.Join('\n', pending.Options.Select(o => o.Description));
        Assert.DoesNotContain("better suited", text, StringComparison.Ordinal);
        Assert.DoesNotContain("not the man for rough work", text, StringComparison.Ordinal);
        Assert.DoesNotContain("executor capability", text, StringComparison.Ordinal);
        foreach (var figure in new[] { "0.8", "0.55", "0.35", "0.65", "0.7", "0.75" })
            Assert.DoesNotContain(figure, text, StringComparison.Ordinal);

        // Each delegate option identifies its own candidate and nothing more — no ranking, no
        // recommendation. Both are present as distinctly-worded options; PreparedDecision.Available
        // is separately pinned (milestone 009 ruling 5) to be sorted by candidate id, never score,
        // which is what a player-facing surface reading Available verbatim inherits for free.
        Assert.Contains(DelegateToTommy, text);
        Assert.Contains(DelegateToAngelo, text);
    }

    // ================================================================= helpers — natural/session level

    private static PendingDecision RunToNextPause(SimulationSession session, DateTime horizon)
    {
        if (session.Status == SessionStatus.Ready) session.AdvanceTo(horizon);
        Assert.Equal(SessionStatus.AwaitingChoice, session.Status);
        return session.Pending!;
    }

    private static PendingDecision ReachFork(SimulationSession session, DateTime horizon)
    {
        RunToNextPause(session, horizon);
        ChooseByDescription(session, StartPersuade);
        return RunToNextPause(session, horizon);
    }

    private static void ChooseByDescription(SimulationSession session, string description)
    {
        var pending = session.Pending ?? RunToNextPause(session, End);

        int index = -1;
        for (int i = 0; i < pending.Options.Count; i++)
        {
            if (!string.Equals(pending.Options[i].Description, description, StringComparison.Ordinal)) continue;
            Assert.Equal(-1, index);
            index = i;
        }

        Assert.True(index >= 0,
            $"no offered option reads \"{description}\" on {session.Date:yyyy-MM-dd} — offered: " +
            string.Join(" | ", pending.Options.Select(o => o.Description)));
        session.Choose(pending.Options[index].Id);
    }

    // ================================================================= helpers — developer-facing pipeline level

    /// <summary>
    /// Drives a fresh world to Vincent's fork — his second pause, immediately after the start
    /// decision — through the real <see cref="Runner.Step"/>/<see cref="Pipeline.Resolve"/>
    /// boundary, returning the prepared decision (with <c>Scored</c>, <c>Available</c> and
    /// <c>Rejected</c> all populated) before anything at the fork itself resolves.
    /// </summary>
    private static PreparedDecision AdvanceToVincentsFork(World world, string variant = Variant)
    {
        var first = AdvanceToVincentsNextPause(world);
        // Matched on structured fields, not Candidate.Description — that string is the developer
        // trace's own wording ("talk bellini-grocery round"), not the player-facing StartPersuade
        // text ("talk Bellini's grocery round") ChooseByDescription matches against Options; the
        // two vocabularies are deliberately different (PlayerOption.cs's own header explains why).
        var startCandidate = first.Available.Single(c =>
            c.Kind == ActionKind.StartStrategy && c.Strategy == StrategyKind.SecureTribute
            && c.TargetId == Cast.Grocery && c.Method == CoercionMethod.Persuade);
        Pipeline.Resolve(first, startCandidate.Id);

        return AdvanceToVincentsNextPause(world);
    }

    /// <summary>
    /// Not the literal next event: <c>StrategyStep</c> auto-advances silently (never a
    /// <c>Think</c>/pause), so the events between one of Vincent's pauses and his next — his own
    /// approach step, Marco's own demand/concede/refuse decision, his own blocked-pressure step —
    /// are real intervening advances, not skipped. Loops the same way
    /// <c>ControlledAutonomousParityTests.AdvanceToTommysPause</c> does.
    /// </summary>
    private static PreparedDecision AdvanceToVincentsNextPause(World world)
    {
        for (int guard = 0; guard < 5000; guard++)
        {
            var step = Runner.Step(world, DateTime.MaxValue, Vincent);
            if (step.Status == StepStatus.AwaitingChoice) return step.Awaiting!;
            if (step.Status == StepStatus.Exhausted)
                throw new InvalidOperationException("queue exhausted before Vincent ever paused");
        }
        throw new InvalidOperationException("guard exceeded before Vincent ever paused");
    }

    // ================================================================= helpers — staged (Section B idiom)

    /// <summary>Independently copied from <c>DirectActionVsDelegationTests</c>, per this project's
    /// practice of not sharing test helpers across milestone-specific files.</summary>
    private static StrategyInstance OpenTributeCase(
        World world, Character owner, Character executor, CoercionMethod method)
    {
        var ctx = Context(world, owner);
        var start = new Candidate($"start:tribute:{Cast.Grocery}:{method}", ActionKind.StartStrategy, "test",
            $"lean on {world.Businesses[Cast.Grocery].Name}")
        {
            TargetId = Cast.Grocery,
            Strategy = StrategyKind.SecureTribute,
            Domain = Cast.Harbour,
            Method = method,
        };
        Commit.Apply(world, owner, start, ctx.Agenda, ctx, new List<string>());
        var s = owner.Execution.Strategy!;

        if (executor.Id != owner.Id)
        {
            var delegateCtx = Context(world, owner);
            var delegateCandidate = new Candidate($"delegate:{s.Kind}:{executor.Id}", ActionKind.DelegateStrategy,
                "test", $"have {executor.Name} take it on")
            {
                TargetId = executor.Id,
                Strategy = s.Kind,
                Method = s.Method,
                Domain = s.Domain,
                RequiredCrew = 1,
            };
            Commit.Apply(world, owner, delegateCandidate, delegateCtx.Agenda, delegateCtx, new List<string>());
        }

        return s;
    }

    private static void AdvanceTributeSteps(World world, Character executor, StrategyInstance s, int steps)
    {
        for (int i = 0; i < steps; i++)
        {
            if (s.PendingStepEventId is null) return;
            Strategies.Advance(world, executor, new ScheduledEvent
            {
                Id = s.PendingStepEventId!.Value,
                Time = world.Now,
                Kind = EventKind.StrategyStep,
                OwnerId = executor.Id,
                Cause = "test",
                Payload = new EventPayload
                {
                    StrategyOwnerId = s.OwnerId,
                    StrategySequence = s.LocalSequence,
                    AdvanceOrdinal = s.NextAdvanceOrdinal,
                    Strategy = s.Kind,
                    StepIndex = s.StepIndex,
                    TargetId = s.TargetId,
                },
            });
        }
    }

    private static List<ScheduledEvent> Drain(World world)
    {
        var drained = new List<ScheduledEvent>();
        while (world.Queue.Next(world.Now.AddYears(1)) is { } ev) drained.Add(ev);
        return drained;
    }

    private static GeneratorContext Context(World world, Character actor, params string[] acquainted)
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
            SubordinateIds: Array.Empty<string>(),
            OrgMemberIds: Array.Empty<string>(),
            AcquaintedIds: acquainted,
            SubordinateCoercion: new Dictionary<string, double>(),
            ReportsSent: Array.Empty<Report>(),
            RequestsMade: Array.Empty<InformationRequest>(),
            VisibleTargets: Array.Empty<string>());

    // ================================================================= helpers — save/load (Section C idiom)

    private static void AdvanceToNextPause(CrimeEmpire.Persistence.Session.PersistentSession session)
    {
        for (int guard = 0; guard < 5000 && session.Status != SessionStatus.AwaitingChoice; guard++)
            session.StepEvent();
        Assert.Equal(SessionStatus.AwaitingChoice, session.Status);
    }

    private static void ChooseByDescription(CrimeEmpire.Persistence.Session.PersistentSession session, string description)
    {
        AdvanceToNextPause(session);
        var pending = session.Pending!;

        int index = -1;
        for (int i = 0; i < pending.Options.Count; i++)
        {
            if (!string.Equals(pending.Options[i].Description, description, StringComparison.Ordinal)) continue;
            Assert.Equal(-1, index);
            index = i;
        }

        Assert.True(index >= 0,
            $"no offered option reads \"{description}\" on {session.Date:yyyy-MM-dd} — offered: " +
            string.Join(" | ", pending.Options.Select(o => o.Description)));
        session.Choose(pending.Options[index].Id);
    }

    private static void SettleLastOption(CrimeEmpire.Persistence.Session.PersistentSession session)
    {
        session.AdvanceDays(90);
        while (session.Status == SessionStatus.AwaitingChoice)
            session.Choose(session.Pending!.Options[^1].Id);
    }

    private static string StrategyFingerprint(StrategyInstance? s)
        => s is null
            ? "none"
            : $"{s.OwnerId}|{s.DelegatedToId}|{s.Kind}|{s.Method}|{s.StepIndex}|{s.TargetId}";

    private static IEnumerable<string> Phrases(PlayerSnapshot s)
    {
        foreach (var b in s.Known.Concat(s.Recent).Concat(s.Unsettled))
        {
            yield return b.Statement;
            yield return b.Confidence;
            yield return b.Attribution;
        }

        foreach (var d in s.Disagreements)
        {
            yield return d.Statement;
            if (d.OwnBasis is { } basis) yield return basis;
            foreach (var a in d.Accounts) yield return a.SourceName;
        }

        foreach (var a in s.Attitudes)
        {
            yield return a.PersonName;
            yield return a.Standing;
            if (a.Wariness is { } w) yield return w;
            foreach (var g in a.Grievances) yield return g;
        }

        foreach (var p in s.Silent) yield return p.Name;
    }

    private static string Flatten(PlayerSnapshot s) => string.Join('\n', Phrases(s));

    private static void AssertPersistentEquivalence(
        CrimeEmpire.Persistence.Session.PersistentSession loaded,
        CrimeEmpire.Persistence.Session.PersistentSession control)
    {
        Assert.Equal(
            TraceWriter.Render(loaded.InnerSession.World, Variant, false),
            TraceWriter.Render(control.InnerSession.World, Variant, false));
        Assert.Equal(Flatten(loaded.Snapshot()), Flatten(control.Snapshot()));
        Assert.Equal(
            StrategyFingerprint(loaded.InnerSession.World.Get(Vincent).Execution.Strategy),
            StrategyFingerprint(control.InnerSession.World.Get(Vincent).Execution.Strategy));
        Assert.Equal(
            loaded.InnerSession.World.Get(Vincent).Capabilities.Cash,
            control.InnerSession.World.Get(Vincent).Capabilities.Cash);
    }
}
