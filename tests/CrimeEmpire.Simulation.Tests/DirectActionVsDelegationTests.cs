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
/// Milestone 017: personally executing an operation and delegating it are causally different choices,
/// even though both pursue the same objective through the same shared owner/executor rules —
/// <see cref="StrategyInstance.OwnerId"/> versus <see cref="StrategyInstance.DelegatedToId"/>, already
/// implemented by milestones 007–011 and exercised end to end by milestone 014's golden path. Nothing
/// here changes <c>Strategy/Strategies.cs</c>, <c>Decision/Commit.cs</c>, or <c>Decision/Generators.cs</c>
/// — this milestone forks and stages the existing mechanism, it does not add a second one.
///
/// <b>The fork point.</b> <c>Generators.GenerateAll</c> calls both <c>FromCommitment</c> (proposes
/// <see cref="ActionKind.ContinueStrategy"/>, "carry on...") and <c>FromRelationship</c> (proposes
/// <see cref="ActionKind.DelegateStrategy"/> to the highest-trust subordinate — deterministically
/// Tommy, the only character at Vincent's <c>Authority - 1</c> — "have Tommy Nardo take it on")
/// unconditionally whenever <c>Actor.Execution.Strategy</c> is running and undelegated. Both options
/// are therefore already offered together, in the accepted seed-42 baseline, at Vincent's very first
/// pause after starting the <c>SecureTribute</c> operation against Bellini's grocery — the same pause
/// <see cref="PlayerOwnedOperationTests.SevenChoiceSequence"/>'s second entry, "carry on getting
/// Bellini's grocery to pay", answers today.
///
/// <b>Section A</b> forks that pause in memory. <b>Section B</b> stages the owner/executor split
/// directly through the same production <c>Commit</c>/<c>Strategies</c> entry points
/// <c>InvestigationTests.cs</c> already uses for the identical class of claim on a delegated
/// investigation, so the natural-run proof (which cannot guarantee investigation or evidence discovery
/// on every seed) has a controlled-state fallback that still runs the real code. <b>Section C</b> forks
/// the same pause through a save and two independent loads, per the milestone's "save/load replay from
/// the shared fork through both branches" requirement.
/// </summary>
public sealed class DirectActionVsDelegationTests
{
    private const int Seed = 42;
    private const string Variant = "baseline";
    private const string Vincent = "vincent";
    private const string Tommy = "tommy";
    private const string Marco = "marco";

    private static DateTime End => Cast.Start.AddDays(90);

    private const string CarryOn = "carry on getting Bellini's grocery to pay";
    private const string DelegateToTommy = "have Tommy Nardo take it on";

    // Vincent's very first pause is the start decision itself (SevenChoiceSequence[0] in
    // PlayerOwnedOperationTests/PersistenceTests) — "talk Bellini's grocery round" for the Persuade
    // start. The fork this milestone is about (continue vs. delegate, offered together) is the pause
    // that follows it, not the first pause overall. Independently pinned here, matching those two
    // files' own copies, per this project's practice of not sharing the same constant across files
    // that check the same assumption.
    private const string StartPersuade = "talk Bellini's grocery round";

    // ================================================================= Section A: the natural fork

    /// <summary>
    /// The executable feature claim itself: Vincent's first pause after starting the operation offers
    /// both direct continuation and delegation to Tommy, in the same <see cref="PendingDecision"/> —
    /// not two decisions in sequence. Read only from public option text, exactly as a Godot button
    /// would see it. Requirement 1.
    /// </summary>
    [Fact]
    public void Vincents_first_pause_offers_both_direct_continuation_and_delegation_together()
    {
        var session = SimulationSession.Start(Seed, Variant, Vincent);
        var pending = ReachFork(session, End);

        var descriptions = pending.Options.Select(o => o.Description).ToList();
        Assert.Contains(CarryOn, descriptions);
        Assert.Contains(DelegateToTommy, descriptions);
    }

    /// <summary>
    /// The identical pre-decision state, established independently twice — two fresh sessions from the
    /// same seed, both stopping at the same fork, produce byte-identical histories and byte-identical
    /// Vincent-facing projections before either branch makes its choice. Requirement 2.
    /// </summary>
    [Fact]
    public void Both_branches_begin_from_an_identical_pre_decision_state()
    {
        var a = SimulationSession.Start(Seed, Variant, Vincent);
        var b = SimulationSession.Start(Seed, Variant, Vincent);
        ReachFork(a, End);
        ReachFork(b, End);

        Assert.Equal(
            TraceWriter.Render(a.World, Variant, false),
            TraceWriter.Render(b.World, Variant, false));
        Assert.Equal(Flatten(a.Snapshot()), Flatten(b.Snapshot()));
    }

    /// <summary>
    /// The headline proof. From the identical fork, one deliberate choice each — never hand-scripted
    /// beyond that single choice — then <see cref="SimulationSession.ResolveAutomatically"/> for
    /// everything after, so nothing downstream is picked to manufacture a result. This is the same
    /// legitimate "one choice then autonomous continuation" pattern milestone 014's ruling 4 already
    /// established, applied here to satisfy requirement 8 (player and autonomous selection share the
    /// same underlying resolution) as a structural consequence rather than an extra claim.
    ///
    /// Executor identity is asserted immediately, structurally, before any further advance: ownership
    /// never moves, execution does. This is requirement 7 (ownership distinct from execution
    /// responsibility) and the direct falsifier of the tempting wrong implementation "force every
    /// operation step to use OwnerId as executor".
    /// </summary>
    [Fact]
    public void Direct_action_and_delegation_diverge_from_the_identical_fork()
    {
        var direct = SimulationSession.Start(Seed, Variant, Vincent);
        ReachFork(direct, End);
        ChooseByDescription(direct, CarryOn);

        var delegated = SimulationSession.Start(Seed, Variant, Vincent);
        ReachFork(delegated, End);
        ChooseByDescription(delegated, DelegateToTommy);

        var directStrategy = direct.World.Get(Vincent).Execution.Strategy;
        var delegatedStrategy = delegated.World.Get(Vincent).Execution.Strategy;

        // Ownership never moves — Vincent owns the operation in both branches.
        Assert.NotNull(directStrategy);
        Assert.NotNull(delegatedStrategy);

        // Execution responsibility is exactly what diverged.
        Assert.Null(directStrategy!.DelegatedToId);
        Assert.Equal(Tommy, delegatedStrategy!.DelegatedToId);

        while (direct.Status == SessionStatus.AwaitingChoice) direct.ResolveAutomatically();
        if (direct.Status == SessionStatus.Ready && direct.Date < End) direct.AdvanceTo(End);
        while (direct.Status == SessionStatus.AwaitingChoice) direct.ResolveAutomatically();

        while (delegated.Status == SessionStatus.AwaitingChoice) delegated.ResolveAutomatically();
        if (delegated.Status == SessionStatus.Ready && delegated.Date < End) delegated.AdvanceTo(End);
        while (delegated.Status == SessionStatus.AwaitingChoice) delegated.ResolveAutomatically();

        string directTrace = TraceWriter.Render(direct.World, Variant, false);
        string delegatedTrace = TraceWriter.Render(delegated.World, Variant, false);
        Assert.NotEqual(directTrace, delegatedTrace);

        // Ownership determines proceeds regardless of who executed — the milestone's own
        // clarification, checked as a required negative: this is NOT where the two branches diverge.
        // (Only asserted when both branches actually reach collection; a natural run is permitted to
        // fail, delay, or take a different path in either branch.)
        var directGrocery = direct.World.Businesses[Cast.Grocery];
        var delegatedGrocery = delegated.World.Businesses[Cast.Grocery];
        if (directGrocery.PayingTribute && delegatedGrocery.PayingTribute)
        {
            Assert.True(direct.World.Get(Vincent).Capabilities.Cash > 6000);
            Assert.True(delegated.World.Get(Vincent).Capabilities.Cash > 6000);
        }
    }

    /// <summary>
    /// Vincent's own projection carries no delegated first-hand knowledge for free. Checked two ways:
    /// structurally, that nothing in Vincent's own cognition is sourced to Tommy as
    /// <see cref="SourceKind.Participant"/> or <see cref="SourceKind.Discovery"/> — the two source
    /// kinds that mean "I was there myself", which can only honestly be true of the man who was — and
    /// through the player-facing surface, walking the complete public <see cref="PlayerSnapshot"/>
    /// value graph the way <c>PlayerOwnedOperationTests</c> does, every string and number reachable,
    /// not one hand-picked field. Requirement 6, and the direct falsifier of "leak scheduler causes or
    /// truth-log facts into Vincent's projection".
    ///
    /// <b>Not tested via <c>TargetIsVulnerable</c>'s own <c>SourceKind</c> on Tommy.</b> An earlier
    /// version of this test asserted Tommy's own reading of the target was <c>Discovery</c>-sourced —
    /// wrong in general: in this natural run Vincent's own "approach" step already fires, and forms
    /// his own <c>Discovery</c>-sourced belief, <i>before</i> he delegates — so the delegation briefing
    /// carries Vincent's own already-held belief to Tommy as testimony, and Tommy's own approach step
    /// never re-runs (StepIndex has already advanced past it). That is itself a legitimate, honestly
    /// discovered natural-run behaviour, not a defect — Vincent is entitled to his own prior first-hand
    /// read — but it means this specific claim cannot isolate "Tommy's private knowledge" in the
    /// natural run. The staged proof
    /// (<see cref="The_delegator_learns_nothing_the_executor_forms_after_the_briefing"/>) isolates it
    /// cleanly instead; this test checks the general structural boundary the natural run can still
    /// prove regardless of which specific claim moved first.
    /// </summary>
    [Fact]
    public void Vincents_projection_never_carries_tommys_first_hand_knowledge()
    {
        var session = SimulationSession.Start(Seed, Variant, Vincent);
        ReachFork(session, End);
        ChooseByDescription(session, DelegateToTommy);
        while (session.Status == SessionStatus.AwaitingChoice) session.ResolveAutomatically();
        if (session.Status == SessionStatus.Ready && session.Date < End) session.AdvanceTo(End);
        while (session.Status == SessionStatus.AwaitingChoice) session.ResolveAutomatically();

        var vincent = session.World.Get(Vincent);

        // Sanity: Tommy really did go on to form some first-hand knowledge of his own, or this test
        // proves nothing.
        var tommy = session.World.Get(Tommy);
        Assert.Contains(tommy.Cognition.Records,
            r => r.SourceId == Tommy && r.SourceKind is SourceKind.Participant or SourceKind.Discovery);

        Assert.DoesNotContain(vincent.Cognition.Records,
            r => r.SourceId == Tommy && r.SourceKind is SourceKind.Participant or SourceKind.Discovery);

        // The player-facing surface exposes no developer text at all — the same guarantee
        // PlayerOwnedOperationTests checks, walked here across the whole delegated arc.
        var snapshot = PlayerView.Build(session.World, Vincent, session.World.Now);
        string text = string.Join('\n', ValueGraph(snapshot).OfType<string>());
        var authored = session.World.Decisions.Select(d => d.Trigger)
            .Concat(session.World.Decisions.SelectMany(d => d.Generated).Select(c => c.Description))
            .Concat(session.World.Decisions.SelectMany(d => d.Rejected).Select(r => r.Reason))
            .Where(s => s.Length > 12)
            .Distinct(StringComparer.Ordinal);
        foreach (var developerText in authored)
            Assert.DoesNotContain(developerText, text, StringComparison.Ordinal);
    }

    /// <summary>Determinism per branch, exactly the discipline <c>PlayerOwnedOperationTests</c> already
    /// applies to the delegated branch — required of the direct branch too. Requirement 9.</summary>
    [Theory]
    [InlineData(CarryOn)]
    [InlineData(DelegateToTommy)]
    public void Each_branch_is_deterministic(string firstChoice)
    {
        string RunOnce()
        {
            var session = SimulationSession.Start(Seed, Variant, Vincent);
            ReachFork(session, End);
            ChooseByDescription(session, firstChoice);
            while (session.Status == SessionStatus.AwaitingChoice) session.ResolveAutomatically();
            if (session.Status == SessionStatus.Ready && session.Date < End) session.AdvanceTo(End);
            while (session.Status == SessionStatus.AwaitingChoice) session.ResolveAutomatically();
            return TraceWriter.Render(session.World, Variant, false);
        }

        Assert.Equal(RunOnce(), RunOnce());
    }

    /// <summary>
    /// Pause/fast-forward equivalence per branch (requirement 10, first half).
    ///
    /// <b>Corrected per Codex's review of `9de2c75`.</b> The original version only asserted
    /// <see cref="SessionStatus.Ready"/> and the ending date, which is compatible with two
    /// completely different histories reaching the same status and date. This drives two sessions
    /// from the identical fork, making the identical fork choice, one advanced entirely through
    /// <see cref="SimulationSession.AdvanceTo"/> (bulk fast-forward) and one advanced through real
    /// single-event <see cref="SimulationSession.StepEvent"/> calls for its own early activity before
    /// a single bounded <c>AdvanceTo(End)</c> sweep — the exact "event by event, then fast forward"
    /// shape <c>PlayerSessionTests.Stepping_and_fast_forward_patterns_agree</c> already proves correct
    /// in general, reused here rather than re-derived. A raw, unbounded loop of
    /// <c>while (Date &lt; End) StepEvent()</c> was deliberately avoided: <c>StepEvent</c> is
    /// documented as unbounded (<c>Pump(DateTime.MaxValue, oneEventOnly: true)</c>), so such a loop can
    /// process one event past <c>End</c> that a horizon-bounded <c>AdvanceTo(End)</c> would never touch,
    /// which would make the two patterns genuinely disagree for a reason having nothing to do with this
    /// milestone's fork.
    ///
    /// Both runs resolve every pause after the fork choice with the same real, visible, deterministic
    /// policy <c>PlayerSessionTests.Settle</c> already established — the last offered option — rather
    /// than the hidden-score <see cref="SimulationSession.ResolveAutomatically"/>, so this is a genuine
    /// player policy exercised twice under two stepping patterns, not the pipeline's own preference.
    ///
    /// Compares the final trace, the final Vincent-facing snapshot, and the operation's own
    /// replay/future-decision-relevant state — <see cref="StrategyInstance"/>'s own identity fields,
    /// Vincent's cash, and the business's paying state — not merely session status and date. Also keeps
    /// the original version's "time cannot move mid-decision" check, folded in rather than dropped.
    /// </summary>
    [Theory]
    [InlineData(CarryOn)]
    [InlineData(DelegateToTommy)]
    public void Fast_forward_and_event_by_event_stepping_reach_equivalent_state_within_a_branch(string firstChoice)
    {
        // ReachFork itself uses AdvanceTo, which is correct for the fast-forwarded branch: the
        // resulting outstanding horizon (End) is exactly what "fast forward" means, and Choose already
        // resumes it — a second explicit AdvanceTo(End) call here would be redundant, not additive.
        var fastForwarded = SimulationSession.Start(Seed, Variant, Vincent);
        ReachFork(fastForwarded, End);
        ChooseByDescription(fastForwarded, firstChoice);
        Settle(fastForwarded);

        // The stepped branch must reach the fork by real single-event steps too, not via ReachFork's
        // AdvanceTo — StepEvent clears any outstanding fast-forward on every call, but ReachFork's own
        // AdvanceTo would otherwise leave one active, and the fork Choose below would silently resume
        // it, fast-forwarding this "stepped" branch exactly like the other one and proving nothing.
        var stepped = SimulationSession.Start(Seed, Variant, Vincent);
        while (stepped.Status != SessionStatus.AwaitingChoice) stepped.StepEvent();
        ChooseByDescription(stepped, StartPersuade);
        while (stepped.Status != SessionStatus.AwaitingChoice) stepped.StepEvent();

        ChooseByDescription(stepped, firstChoice);
        for (int i = 0; i < 25; i++)
        {
            if (stepped.Status == SessionStatus.AwaitingChoice)
            {
                Assert.Throws<InvalidOperationException>(() => stepped.AdvanceDays(1));
                Settle(stepped);
            }
            else
            {
                stepped.StepEvent();
            }
        }
        Settle(stepped); // the loop's own last iteration can itself land on a fresh pause
        stepped.AdvanceTo(End);
        Settle(stepped);

        Assert.Equal(SessionStatus.Ready, fastForwarded.Status);
        Assert.Equal(SessionStatus.Ready, stepped.Status);

        Assert.Equal(
            TraceWriter.Render(fastForwarded.World, Variant, false),
            TraceWriter.Render(stepped.World, Variant, false));
        Assert.Equal(Flatten(fastForwarded.Snapshot()), Flatten(stepped.Snapshot()));

        var ffStrategy = StrategyFingerprint(fastForwarded.World.Get(Vincent).Execution.Strategy);
        var stStrategy = StrategyFingerprint(stepped.World.Get(Vincent).Execution.Strategy);
        Assert.Equal(ffStrategy, stStrategy);

        Assert.Equal(
            fastForwarded.World.Get(Vincent).Capabilities.Cash,
            stepped.World.Get(Vincent).Capabilities.Cash);
        Assert.Equal(
            fastForwarded.World.Businesses[Cast.Grocery].PayingTribute,
            stepped.World.Businesses[Cast.Grocery].PayingTribute);
    }

    // ================================================================= Section B: staged boundary proof
    //
    // Mirrors InvestigationTests.cs's OpenDelegatedCase/RunToCompletion idiom for the identical class of
    // claim, scoped to SecureTribute against Bellini's grocery instead of InvestigateIncident. Every
    // call below goes through the real production Commit.Apply/Strategies.Advance entry points — no
    // test-only execution path. Method is pinned to Force so violence resolves deterministically on the
    // very first press, isolating executor identity from Marco's own concede/refuse decision (staged by
    // toggling Business.PayingTribute directly, the same category of staging InvestigationTests.cs's
    // Believe() already uses to seed cognition state directly).

    /// <summary>Violence and evidence identify the executor, not the owner. Requirement 3, and the
    /// direct falsifier of "attribute evidence... to the owner regardless of executor".</summary>
    [Theory]
    [InlineData(Vincent)] // direct: owner is executor
    [InlineData(Tommy)]   // delegated: executor differs from owner
    public void Violence_and_its_evidence_identify_the_actual_executor(string executorId)
    {
        var world = Cast.Build(seed: 1, Variant);
        var owner = world.Get(Vincent);
        var executor = world.Get(executorId);

        var s = OpenTributeCase(world, owner, executor, CoercionMethod.Force);
        AdvanceTributeSteps(world, executor, s, steps: 3); // approach, demand, press (force resolves here)

        var violence = world.TruthLog.Single(e => e.Kind == "violence");
        Assert.Equal(executorId, violence.ActorId);

        var violenceClaims = executor.Cognition.OfKind(ClaimKind.PersonUsedViolence).ToList();
        Assert.Contains(violenceClaims, r => r.Claim.Subject == executorId);

        // The owner, when he differs from the executor, holds no Participant-sourced copy — the
        // owner's own cognition is untouched by this step entirely (Strategies.ResolveViolence's own
        // documented rule: "the man who sent him is told nothing here, and learns nothing here").
        if (owner.Id != executor.Id)
            Assert.DoesNotContain(owner.Cognition.Records, r => r.Claim.Kind == ClaimKind.PersonUsedViolence);
    }

    /// <summary>The business owner's encounter and fear concern the executor. Requirement 4.</summary>
    [Theory]
    [InlineData(Vincent)]
    [InlineData(Tommy)]
    public void Marcos_encounter_and_fear_concern_the_executor_not_the_owner(string executorId)
    {
        var world = Cast.Build(seed: 1, Variant);
        var owner = world.Get(Vincent);
        var executor = world.Get(executorId);
        var marco = world.Get(Marco);

        var s = OpenTributeCase(world, owner, executor, CoercionMethod.Force);
        AdvanceTributeSteps(world, executor, s, steps: 3);

        Assert.Contains(world.Encounters, e => e.WhoId == Marco && e.MetId == executorId);
        Assert.True(marco.Social.Toward(executorId).Fear > 0);

        if (owner.Id != executor.Id)
        {
            Assert.DoesNotContain(world.Encounters, e => e.WhoId == Marco && e.MetId == owner.Id);
            Assert.Equal(0, marco.Social.Toward(owner.Id).Fear);
        }
    }

    /// <summary>Participant/first-hand knowledge of collection goes to the executor. Requirement 3 and 5.</summary>
    [Theory]
    [InlineData(Vincent)]
    [InlineData(Tommy)]
    public void Collection_is_first_hand_knowledge_for_the_executor_and_discovery_for_the_owner(string executorId)
    {
        var world = Cast.Build(seed: 1, Variant);
        var owner = world.Get(Vincent);
        var executor = world.Get(executorId);
        var business = world.Businesses[Cast.Grocery];

        var s = OpenTributeCase(world, owner, executor, CoercionMethod.Persuade);
        business.PayingTribute = true; // staged: Marco's own concession, out of scope for this proof
        // approach, demand, press-or-accept (records the agreement), collect (the 4th, StepIndex >= TributeSteps.Length)
        AdvanceTributeSteps(world, executor, s, steps: 4);

        var collected = executor.Cognition.Find(new Claim(ClaimKind.BusinessRefusesTribute, Cast.Grocery));
        Assert.NotNull(collected);
        Assert.Equal(SourceKind.Participant, collected!.SourceKind);
        Assert.Equal(Stance.Rejects, collected.Stance);

        if (owner.Id != executor.Id)
        {
            var ownerPosition = owner.Cognition.Find(new Claim(ClaimKind.BusinessRefusesTribute, Cast.Grocery));
            Assert.NotNull(ownerPosition);
            Assert.Equal(SourceKind.Discovery, ownerPosition!.SourceKind);
        }
    }

    /// <summary>
    /// The delegator receives no executor knowledge beyond the bounded briefing
    /// <c>Commit.Apply</c>'s <see cref="ActionKind.DelegateStrategy"/> branch already sends — nothing
    /// the executor separately comes to believe or do afterward. Requirement 5, and the direct
    /// falsifier of "copy the executor's new belief into the owner's cognition".
    /// </summary>
    [Fact]
    public void The_delegator_learns_nothing_the_executor_forms_after_the_briefing()
    {
        var world = Cast.Build(seed: 1, Variant);
        var owner = world.Get(Vincent);
        var tommy = world.Get(Tommy);

        var s = OpenTributeCase(world, owner, tommy, CoercionMethod.Force);
        int ownerRecordsAfterBriefing = owner.Cognition.Records.Count;

        AdvanceTributeSteps(world, tommy, s, steps: 3); // Tommy approaches, demands, and resolves violence

        // Nothing Tommy formed on his own afterward reached Vincent's cognition.
        Assert.Equal(ownerRecordsAfterBriefing, owner.Cognition.Records.Count);
        Assert.DoesNotContain(owner.Cognition.Records, r => r.Claim.Kind == ClaimKind.PersonUsedViolence);
        Assert.DoesNotContain(owner.Cognition.Records, r => r.SourceId == Tommy && r.SourceKind == SourceKind.Discovery);
    }

    /// <summary>
    /// Investigation names the actual executor as a suspect, not the owner — the same class of proof
    /// <c>InvestigationTests.A_delegated_investigations_lead_is_drawn_from_the_executors_own_belief</c>
    /// already establishes for a delegated *investigation*, applied here to the executor of the
    /// underlying incident a separate investigation canvasses. Requirement: "who may become an
    /// investigation subject."
    ///
    /// <b>Corrected per Codex's review of `9de2c75`.</b> The original version hand-constructed a fresh
    /// <c>WitnessSawIncident</c> claim naming <paramref name="executorId"/> directly and fed that to
    /// Kane, so a mutation that mis-attributed <c>ResolveViolence</c>'s own <c>witnessClaim</c> would
    /// never have been exercised — the test was checking a value this test itself typed, not what
    /// production actually produced. This now drains the real
    /// <see cref="EventKind.ObservationOpportunity"/> <c>ResolveViolence</c> scheduled for Kane through
    /// its own production <c>Offer</c>/<c>ScheduleObservation</c> path (Kane's Investigation skill of
    /// 0.70 clears the bystander-witness threshold, so she is genuinely offered one — confirmed by
    /// asserting the drain actually finds it, not assumed), and reads the real <see cref="Claim"/> off
    /// that event's own payload — never retyped. Only the discoverability *roll* inside
    /// <c>Runner.Observe</c> is bypassed (staged delivery straight into Kane's cognition, in the exact
    /// shape <c>Observe</c> itself would write: <c>Believes</c>, confidence 0.6, <c>Discovery</c>,
    /// sourced to Kane, at the current time) — the claim's *content*, including its executor
    /// attribution, is entirely production output.
    /// </summary>
    [Theory]
    [InlineData(Vincent)]
    [InlineData(Tommy)]
    public void An_investigation_names_the_true_executor_of_the_violence_not_the_owner(string executorId)
    {
        var world = Cast.Build(seed: 1, Variant);
        var owner = world.Get(Vincent);
        var executor = world.Get(executorId);
        var kane = world.Get("kane");

        var s = OpenTributeCase(world, owner, executor, CoercionMethod.Force);
        AdvanceTributeSteps(world, executor, s, steps: 3);
        var violenceEvent = world.TruthLog.Single(e => e.Kind == "violence");

        // The real claim ResolveViolence itself constructed, drained from the ObservationOpportunity
        // it scheduled for Kane through the production Offer/ScheduleObservation path — never retyped.
        var opportunity = Drain(world).SingleOrDefault(e =>
            e.Kind == EventKind.ObservationOpportunity && e.OwnerId == kane.Id
            && e.Payload.Claims.Any(c => c.Kind == ClaimKind.WitnessSawIncident && c.EventId == violenceEvent.Id));
        Assert.True(opportunity is not null,
            "ResolveViolence did not offer Kane an observation opportunity for this incident, so this " +
            "test cannot prove anything about production evidence attribution");
        var productionClaim = opportunity!.Payload.Claims.Single(c => c.Kind == ClaimKind.WitnessSawIncident);
        Assert.Equal(executorId, productionClaim.Object);
        Assert.Equal(violenceEvent.Id, productionClaim.EventId);

        // Staged delivery, bypassing only Observe's discoverability roll — never the claim's own
        // content — in the exact shape Observe itself writes.
        kane.Cognition.Learn(productionClaim, Stance.Believes, 0.6, SourceKind.Discovery, kane.Id, world.Now);

        var caseInstance = OpenInvestigation(world, kane, productionClaim);
        RunInvestigationToCompletion(world, kane, caseInstance);

        var suspicion = kane.Cognition.OfKind(ClaimKind.PersonUsedViolence)
            .Where(r => r.Claim.EventId == violenceEvent.Id)
            .ToList();
        Assert.Contains(suspicion, r => r.Claim.Subject == executorId);
        if (owner.Id != executor.Id)
            Assert.DoesNotContain(suspicion, r => r.Claim.Subject == owner.Id);
    }

    // ================================================================= Section C: save/load through the fork

    /// <summary>
    /// The same fork, forced through a save and two independent loads — the exact pattern
    /// <c>PersistenceTests.Counterfactual_valid_choices_from_the_same_save_diverge_naturally</c>
    /// already establishes for the start-vs-abandon fork, applied to the continue-vs-delegate fork.
    /// <see cref="CrimeEmpire.Persistence.Session.PersistentSession"/> exposes no autonomous-resolution
    /// path (only <c>StepEvent</c>/<c>AdvanceDays</c>/<c>Choose</c>, matching what a real save file can
    /// replay). Requirement 10 (second half) and the in-scope "save/load replay from the shared fork
    /// through both branches" item.
    ///
    /// <b>Corrected per Codex's review of `9de2c75`.</b> The original version compared only the
    /// immediate <c>DelegatedToId</c> change right after the fork choice — real, but not the "real
    /// operation consequence" requirement 10 actually asks the save/load proof to reach. This now
    /// continues each loaded branch with the same real, visible, deterministic "last offered option"
    /// policy <c>PlayerSessionTests.Settle</c> established (reused here as
    /// <see cref="SettleLastOption(CrimeEmpire.Persistence.Session.PersistentSession)"/>, since
    /// <see cref="Settle(SimulationSession)"/> is typed to the other session kind) far enough to reach a
    /// real consequence, and compares each loaded continuation against an <b>equivalent unsaved
    /// control</b> — a fresh session driven from the identical starting seed through the identical
    /// path and the identical fork choice, never touching disk — rather than only comparing the two
    /// loaded branches against each other. Matching a loaded run against its own unsaved control is
    /// what actually proves save/load fidelity; matching two branches that were both loaded proves only
    /// that they still disagree with each other, which the in-memory Section A tests already establish
    /// more directly.
    /// </summary>
    [Fact]
    public void Save_and_load_from_the_shared_fork_reproduces_both_branches()
    {
        string path = Path.Combine(Path.GetTempPath(), $"ce-m017-fork-{Guid.NewGuid():N}.db");
        try
        {
            var setup = CrimeEmpire.Persistence.Session.PersistentSession.Start(Seed, Variant, Vincent);
            AdvanceToNextPause(setup);
            ChooseByDescription(setup, StartPersuade);
            AdvanceToNextPause(setup); // now at the fork: continue vs. delegate offered together
            setup.Save(path);

            var goldenLoaded = CrimeEmpire.Persistence.Session.PersistentSession.Load(path);
            ChooseByDescription(goldenLoaded, CarryOn);
            SettleLastOption(goldenLoaded);

            var declinedLoaded = CrimeEmpire.Persistence.Session.PersistentSession.Load(path);
            ChooseByDescription(declinedLoaded, DelegateToTommy);
            SettleLastOption(declinedLoaded);

            // Equivalent unsaved controls: the identical path, from a fresh session, never saved or
            // loaded at all.
            var goldenControl = CrimeEmpire.Persistence.Session.PersistentSession.Start(Seed, Variant, Vincent);
            AdvanceToNextPause(goldenControl);
            ChooseByDescription(goldenControl, StartPersuade);
            AdvanceToNextPause(goldenControl);
            ChooseByDescription(goldenControl, CarryOn);
            SettleLastOption(goldenControl);

            var declinedControl = CrimeEmpire.Persistence.Session.PersistentSession.Start(Seed, Variant, Vincent);
            AdvanceToNextPause(declinedControl);
            ChooseByDescription(declinedControl, StartPersuade);
            AdvanceToNextPause(declinedControl);
            ChooseByDescription(declinedControl, DelegateToTommy);
            SettleLastOption(declinedControl);

            // Each loaded branch, continued to a real consequence, matches its own unsaved control
            // exactly: history, player-facing projection, and the operation's own
            // replay/future-decision-relevant state.
            AssertPersistentEquivalence(goldenLoaded, goldenControl);
            AssertPersistentEquivalence(declinedLoaded, declinedControl);

            // A real operation consequence was actually reached in both — not merely the immediate
            // DelegatedToId flag — and the two branches still diverge from each other at it.
            var goldenWorld = goldenLoaded.InnerSession.World;
            var declinedWorld = declinedLoaded.InnerSession.World;
            Assert.True(
                goldenWorld.TruthLog.Count > 1 && declinedWorld.TruthLog.Count > 1,
                "the settled continuation barely moved past the fork, so this proves nothing about a " +
                "real consequence");
            Assert.NotEqual(
                TraceWriter.Render(goldenWorld, Variant, false),
                TraceWriter.Render(declinedWorld, Variant, false));

            var goldenStrategy = goldenWorld.Get(Vincent).Execution.Strategy;
            Assert.True(goldenStrategy is null || goldenStrategy.DelegatedToId is null);
            Assert.Contains(Tommy, declinedWorld.Get(Vincent).Execution.DelegatedExecutorIds);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    // ================================================================= helpers — Section A

    private static PendingDecision RunToNextPause(SimulationSession session, DateTime horizon)
    {
        if (session.Status == SessionStatus.Ready) session.AdvanceTo(horizon);
        Assert.Equal(SessionStatus.AwaitingChoice, session.Status);
        return session.Pending!;
    }

    /// <summary>
    /// Reaches the fork this milestone is about: Vincent's first pause is the start decision, so this
    /// answers it with the Persuade start (matching the accepted baseline's own first choice) and
    /// advances once more to the pause where continue and delegate are offered together.
    /// </summary>
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

    /// <summary>
    /// A real, visible, deterministic player policy — the last offered option — matching
    /// <c>PlayerSessionTests.Settle</c> exactly rather than <see cref="SimulationSession.ResolveAutomatically"/>'s
    /// hidden score, so a stepping-pattern comparison exercises the same kind of policy a person
    /// actually uses.
    /// </summary>
    private static void Settle(SimulationSession session)
    {
        while (session.Status == SessionStatus.AwaitingChoice)
            session.Choose(session.Pending!.Options[^1].Id);
    }

    /// <summary>The operation's own replay/future-decision-relevant identity — owner, delegate,
    /// method, step, and target — collapsed to one comparable string, or "none" if it has
    /// completed.</summary>
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

    private static IEnumerable<object> ValueGraph(object? node)
    {
        switch (node)
        {
            case null:
                yield break;
            case string s:
                yield return s;
                yield break;
            case double or int or long or bool or DateTime:
                yield return node;
                yield break;
            case System.Collections.IEnumerable seq:
                foreach (var item in seq)
                foreach (var v in ValueGraph(item))
                    yield return v;
                yield break;
            default:
                var type = node.GetType();
                if (type.IsEnum) { yield return node; yield break; }
                foreach (var property in type.GetProperties(
                             System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    if (property.GetIndexParameters().Length > 0) continue;
                    foreach (var v in ValueGraph(property.GetValue(node)))
                        yield return v;
                }
                yield break;
        }
    }

    // ================================================================= helpers — Section B

    /// <summary>
    /// Opens a real <see cref="StrategyKind.SecureTribute"/> case against Bellini's grocery through the
    /// production <see cref="Commit.Apply"/> path, then delegates it through the same path when
    /// <paramref name="executor"/> differs from <paramref name="owner"/> — mirroring
    /// <c>InvestigationTests.OpenDelegatedCase</c> exactly, scoped to this strategy kind.
    /// </summary>
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

    /// <summary>Drives <paramref name="steps"/> real <see cref="Strategies.Advance"/> calls, exactly
    /// mirroring <c>InvestigationTests.RunToCompletion</c>'s event-construction pattern.</summary>
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

    /// <summary>Everything still queued, taken off through the queue's own ordering — mirrors
    /// <c>InvestigationTests.Drain</c> exactly, independently copied per this project's practice of not
    /// sharing test helpers across milestone-specific files.</summary>
    private static List<ScheduledEvent> Drain(World world)
    {
        var drained = new List<ScheduledEvent>();
        while (world.Queue.Next(world.Now.AddYears(1)) is { } ev) drained.Add(ev);
        return drained;
    }

    private static StrategyInstance OpenInvestigation(World world, Character kane, Claim lead)
    {
        var ctx = Context(world, kane);
        var candidate = new Candidate($"investigate:{lead}", ActionKind.StartStrategy, "test",
            $"open an investigation into events at {lead.Subject}")
        {
            TargetId = lead.Subject,
            Strategy = StrategyKind.InvestigateIncident,
            Domain = Cast.Harbour,
            AboutIncident = lead,
        };
        Commit.Apply(world, kane, candidate, ctx.Agenda, ctx, new List<string>());
        return kane.Execution.Strategy!;
    }

    private static void RunInvestigationToCompletion(World world, Character kane, StrategyInstance s)
    {
        for (int i = 0; i < Strategies.InvestigateSteps.Length; i++)
        {
            if (s.PendingStepEventId is null) return;
            Strategies.Advance(world, kane, new ScheduledEvent
            {
                Id = s.PendingStepEventId!.Value,
                Time = world.Now,
                Kind = EventKind.StrategyStep,
                OwnerId = kane.Id,
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
            ReportsSent: Array.Empty<Report>(),
            RequestsMade: Array.Empty<InformationRequest>(),
            VisibleTargets: Array.Empty<string>());

    // ================================================================= helpers — Section C

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

    /// <summary>
    /// Continues far enough past the fork to reach a real operation consequence, using the same real,
    /// visible, deterministic "last offered option" policy <see cref="Settle(SimulationSession)"/> uses
    /// for <see cref="SimulationSession"/> — <see cref="CrimeEmpire.Persistence.Session.PersistentSession"/>
    /// has no autonomous-resolution path, so this is the closest equivalent that still exercises only
    /// the session's real public mutators (<c>AdvanceDays</c>/<c>Choose</c>), exactly as a save file
    /// can actually replay. 90 days comfortably covers every consequence any accepted trace at this
    /// seed reaches (collection lands by 1 April, a few weeks in).
    /// </summary>
    private static void SettleLastOption(CrimeEmpire.Persistence.Session.PersistentSession session)
    {
        session.AdvanceDays(90);
        while (session.Status == SessionStatus.AwaitingChoice)
            session.Choose(session.Pending!.Options[^1].Id);
    }

    /// <summary>Full equivalence between a loaded continuation and its unsaved control: history, the
    /// Vincent-facing projection, and the operation's own replay/future-decision-relevant state.</summary>
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
