using CrimeEmpire.Persistence.Session;
using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Scenario;
using CrimeSim.Session;
using CrimeSim.Sim;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>
/// Milestone 018: every deliberate choice must leave a player-visible causal thread — immediate
/// acknowledgement, unresolved status while pending, and perspective-limited resolution once the
/// consequence becomes known.
///
/// The two required natural proofs both use the accepted seed-42 fixture, unmodified:
///
///  - <b>Salvatore asks Vincent</b> — <c>"cautious-vincent"</c>, Salvatore controlled and viewpoint.
///    Observed directly before writing these tests (not guessed): the ask becomes available on
///    1987-04-03, and Vincent's own answer — which happens to contradict what Salvatore already held
///    from "the books" — reaches him naturally within a few days, through the ordinary report
///    channel, with no staging and no tuning. Both halves of the milestone's own instruction are
///    therefore provable from one unmodified run: the same seed shows the request genuinely unresolved
///    immediately after being asked, and genuinely resolved once Vincent actually answers.
///  - <b>Marco and the tribute demand</b> — <c>"baseline"</c>, Marco controlled and viewpoint. The
///    first demand lands 1987-03-08, from Vincent directly, before any escalation — a clean case for
///    the "plain demand" phrasing. A later demand, after refusal and delegation, carries a genuine
///    held <c>PersonUsedViolence</c> claim naming the (by-then-different) demander, proving the
///    "force already used" phrasing is reachable too.
/// </summary>
public sealed class CausalFeedbackTests
{
    private const int Seed = 42;
    private const string CautiousVincent = "cautious-vincent";
    private const string Baseline = "baseline";
    private const string Salvatore = "salvatore";
    private const string Marco = "marco";

    private const string AskVincent =
        "ask Vincent Russo for his own account of whether Bellini's grocery is holding back what it owes";

    // ================================================================= proof A: Salvatore asks Vincent

    /// <summary>
    /// Falsifiers 1-3: selecting the request creates a correctly-attributed acknowledgement, and it
    /// stays unresolved — not merely un-rendered, genuinely absent from <c>AwaitingAnswers</c> — even
    /// though <c>Commit.Apply</c> has already scheduled Vincent's own wake to answer it. Reached by
    /// single-stepping to the choice (never a fast-forward horizon, which would carry across the
    /// <c>Choose</c> call and run straight past the point this test is examining — the same discipline
    /// <c>Game.cs</c>'s own self-tests use "Next event" for).
    /// </summary>
    [Fact]
    public void Asking_vincent_immediately_acknowledges_and_leaves_the_request_genuinely_unresolved()
    {
        var session = SimulationSession.Start(Seed, CautiousVincent, Salvatore);
        var pending = AdvanceToPause(session);
        Assert.Equal(Salvatore, pending.ActorId);

        var askOption = pending.Options.Single(o => o.Description == AskVincent);
        session.Choose(askOption.Id);

        var snapshot = session.Snapshot();

        Assert.NotNull(snapshot.LastAction);
        Assert.Equal(AskVincent, snapshot.LastAction!.Description);

        var request = Assert.Single(snapshot.AwaitingAnswers);
        Assert.Equal("vincent", request.AskedId);
        Assert.Equal("Vincent Russo", request.AskedName);
        Assert.Equal("Bellini's grocery is holding back what it owes", request.Statement);
        Assert.Equal(session.Date, request.AskedAt);
        // Genuinely Pending, not merely "not yet observed to be Declined" — Vincent has not yet had
        // his own triggered deliberation on this at all, which is a different fact from "he had it
        // and chose not to answer" (see the Declined tests below).
        Assert.Equal(RequestDisposition.Pending, request.Disposition);

        // Silence remains silence at this exact moment: nothing has yet reached Salvatore attributing
        // an account to Vincent on this subject, even though the wake that will let Vincent answer is
        // already sitting in the event queue.
        Assert.DoesNotContain(snapshot.Disagreements, d => d.Accounts.Any(a => a.SourceName == "Vincent Russo"));
        Assert.DoesNotContain(snapshot.Known, b => b.Attribution.Contains("Vincent", StringComparison.Ordinal));
        Assert.DoesNotContain(snapshot.Recent, b => b.Attribution.Contains("Vincent", StringComparison.Ordinal));
    }

    /// <summary>
    /// Falsifier 4: once Vincent's answer actually arrives — through the ordinary report channel, no
    /// staging — the request drops out of <c>AwaitingAnswers</c> and his account is attributed to him
    /// by name, preserving the disagreement the answer itself created (it contradicts what "the books"
    /// told Salvatore).
    /// </summary>
    [Fact]
    public void A_delivered_answer_resolves_the_request_and_attributes_the_account_to_vincent()
    {
        var session = SimulationSession.Start(Seed, CautiousVincent, Salvatore);
        var pending = AdvanceToPause(session);
        session.Choose(pending.Options.Single(o => o.Description == AskVincent).Id);

        // Vincent is not controlled here, so his own wake resolves through the ordinary pipeline —
        // nothing staged, nothing forced. Three days is comfortably past the observed ~2-day delivery.
        session.AdvanceDays(3);

        var snapshot = session.Snapshot();
        Assert.DoesNotContain(snapshot.AwaitingAnswers, r => r.AskedId == "vincent");
        Assert.Contains(snapshot.Disagreements, d => d.Accounts.Any(a => a.SourceName == "Vincent Russo"));
    }

    /// <summary>
    /// Actor neutrality (staged proof 5): the identical decision, resolved once through
    /// <see cref="SimulationSession.Choose"/> and once through the autonomous
    /// <see cref="SimulationSession.ResolveAutomatically"/> path, renders the same
    /// <c>LastAction</c> text — guarding specifically against a future branch that renders
    /// differently depending on whether a person made the choice.
    /// </summary>
    [Fact]
    public void Player_chosen_and_autonomously_resolved_decisions_render_identical_last_action_text()
    {
        var autoResolved = SimulationSession.Start(Seed, Baseline, "vincent");
        AdvanceToPause(autoResolved);
        autoResolved.ResolveAutomatically();
        string autoDescription = autoResolved.Snapshot().LastAction!.Description;

        var playerChosen = SimulationSession.Start(Seed, Baseline, "vincent");
        var pending = AdvanceToPause(playerChosen);
        var matching = pending.Options.Single(o => o.Description == autoDescription);
        playerChosen.Choose(matching.Id);

        Assert.Equal(autoDescription, playerChosen.Snapshot().LastAction!.Description);
    }

    /// <summary>Staged proof 6: an unresolved request survives a save/load cycle, and resolves
    /// identically on the reloaded session — replay-reconstructed state, not a copy.</summary>
    [Fact]
    public void Save_load_preserves_an_unresolved_request_and_its_later_resolution()
    {
        string path = Path.Combine(Path.GetTempPath(), $"ce-018-request-{Guid.NewGuid():N}.db");
        try
        {
            var original = PersistentSession.Start(Seed, CautiousVincent, Salvatore, Salvatore);
            for (int guard = 0; guard < 5000 && original.Status != SessionStatus.AwaitingChoice; guard++)
                original.StepEvent();
            Assert.Equal(SessionStatus.AwaitingChoice, original.Status);
            original.Choose(original.Pending!.Options.Single(o => o.Description == AskVincent).Id);

            var beforeSave = original.Snapshot();
            var pendingRequest = Assert.Single(beforeSave.AwaitingAnswers);

            original.Save(path);
            var loaded = PersistentSession.Load(path);

            var afterLoad = loaded.Snapshot();
            var reloadedRequest = Assert.Single(afterLoad.AwaitingAnswers);
            Assert.Equal(pendingRequest.AskedName, reloadedRequest.AskedName);
            Assert.Equal(pendingRequest.AskedAt, reloadedRequest.AskedAt);
            Assert.Equal(pendingRequest.Statement, reloadedRequest.Statement);

            loaded.AdvanceDays(3);
            var resolved = loaded.Snapshot();
            Assert.DoesNotContain(resolved.AwaitingAnswers, r => r.AskedId == "vincent");
            Assert.Contains(resolved.Disagreements, d => d.Accounts.Any(a => a.SourceName == "Vincent Russo"));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    /// <summary>Refreshing (repeated <c>Snapshot()</c> calls with no state change in between) neither
    /// duplicates nor clears the unresolved request.</summary>
    [Fact]
    public void Repeated_snapshots_neither_duplicate_nor_clear_an_unresolved_request()
    {
        var session = SimulationSession.Start(Seed, CautiousVincent, Salvatore);
        var pending = AdvanceToPause(session);
        session.Choose(pending.Options.Single(o => o.Description == AskVincent).Id);

        var first = session.Snapshot();
        var second = session.Snapshot();
        var third = session.Snapshot();

        Assert.Single(first.AwaitingAnswers);
        Assert.Single(second.AwaitingAnswers);
        Assert.Single(third.AwaitingAnswers);
    }

    /// <summary>Deterministic reruns of the identical seed/variant/choice produce byte-identical
    /// projected text.</summary>
    [Fact]
    public void Identical_reruns_produce_identical_projected_text()
    {
        PlayerSnapshot Run()
        {
            var session = SimulationSession.Start(Seed, CautiousVincent, Salvatore);
            var pending = AdvanceToPause(session);
            session.Choose(pending.Options.Single(o => o.Description == AskVincent).Id);
            session.AdvanceDays(3);
            return session.Snapshot();
        }

        var a = Run();
        var b = Run();

        Assert.Equal(a.AwaitingAnswers.Count, b.AwaitingAnswers.Count);
        Assert.Equal(a.LastAction?.Description, b.LastAction?.Description);
        Assert.Equal(
            a.Disagreements.Select(d => d.Statement),
            b.Disagreements.Select(d => d.Statement));
    }

    // ================================================================= pending vs. declined (correction)

    /// <summary>
    /// The corrected disposition itself: Vincent's natural, unmodified delegation audit
    /// (<c>ScenarioReachTests.The_delegator_puts_his_question_to_the_man_he_sent</c>) reaches Tommy's
    /// own "asked-to-account" pause on 1987-04-04 — his very first pause when controlled, per direct
    /// observation. "let it lie" is always offered there (`Generators.FromTrigger`'s floor candidate).
    /// Choosing it, rather than any of the account-giving candidates, is Tommy's own decision to stay
    /// silent — the same real pause a person would face, not a staged shortcut. From Vincent's own
    /// viewpoint, the request must now read Declined, never Pending (he has had his answer — silence)
    /// and never Answered (nothing was asserted to Vincent).
    /// </summary>
    [Fact]
    public void Declining_to_answer_leaves_the_request_marked_declined_not_pending()
    {
        var session = SimulationSession.Start(Seed, Baseline, controlledCharacterId: "tommy", viewpointCharacterId: "vincent");
        var pending = AdvanceToPause(session);
        Assert.Equal("tommy", pending.ActorId);

        session.Choose(pending.Options.Single(o => o.Description == "let it lie").Id);

        var snapshot = session.Snapshot();
        var request = Assert.Single(snapshot.AwaitingAnswers);
        Assert.Equal("tommy", request.AskedId);
        Assert.Equal(RequestDisposition.Declined, request.Disposition);
    }

    /// <summary>Save/load for the declined state, mirroring the pending case's own proof — the
    /// disposition is re-derived from replayed `World.Decisions`/`World.Requests` on load, not
    /// stored, so it must come back identically.</summary>
    [Fact]
    public void Save_load_preserves_a_declined_requests_disposition()
    {
        string path = Path.Combine(Path.GetTempPath(), $"ce-018-declined-{Guid.NewGuid():N}.db");
        try
        {
            var original = PersistentSession.Start(Seed, Baseline, "tommy", "vincent");
            for (int guard = 0; guard < 5000 && original.Status != SessionStatus.AwaitingChoice; guard++)
                original.StepEvent();
            Assert.Equal(SessionStatus.AwaitingChoice, original.Status);
            original.Choose(original.Pending!.Options.Single(o => o.Description == "let it lie").Id);

            var beforeSave = original.Snapshot();
            var declined = Assert.Single(beforeSave.AwaitingAnswers);
            Assert.Equal(RequestDisposition.Declined, declined.Disposition);

            original.Save(path);
            var loaded = PersistentSession.Load(path);

            var reloaded = Assert.Single(loaded.Snapshot().AwaitingAnswers);
            Assert.Equal(RequestDisposition.Declined, reloaded.Disposition);
            Assert.Equal(declined.AskedName, reloaded.AskedName);
            Assert.Equal(declined.AskedAt, reloaded.AskedAt);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    /// <summary>
    /// Player/autonomous parity for the disposition mechanism itself, not only for
    /// <c>LastAction</c>. Reached, not staged: Tommy's own top-ranked preference at this exact pause
    /// (confirmed by reading <c>PreparedDecision.Scored[0]</c> directly before resolving) is a
    /// <em>Partial</em> report that withholds precisely the claim Vincent asked about — his own
    /// self-protective instinct, and a second, genuinely distinct flavour of decline alongside the
    /// "let it lie" case above: a real report is sent, but it deliberately omits the one thing that
    /// was asked, which is indistinguishable from silence on that specific question. Both sides of
    /// this comparison control Tommy — one resolves via <see cref="SimulationSession.ResolveAutomatically"/>,
    /// the other via a person explicitly choosing the identical rendered option — so this isolates
    /// exactly the variable the review asked about (did a person or the pipeline choose?) without
    /// also crossing into whether Tommy was controlled at all, which is a separate variable this
    /// fixture is not guaranteed to hold constant (see the milestone archive's correction section).
    /// </summary>
    [Fact]
    public void Request_disposition_computation_is_identical_whether_declining_was_autonomous_or_player_chosen()
    {
        const string partialWithholding =
            "tell Vincent Russo about whether Tommy Nardo put hands on Bellini's grocery, leaving out his own part";

        var autoResolved = SimulationSession.Start(Seed, Baseline, "tommy", "vincent");
        AdvanceToPause(autoResolved);
        autoResolved.ResolveAutomatically();

        var playerChosen = SimulationSession.Start(Seed, Baseline, "tommy", "vincent");
        var pending = AdvanceToPause(playerChosen);
        playerChosen.Choose(pending.Options.Single(o => o.Description == partialWithholding).Id);

        var autoRequest = Assert.Single(autoResolved.Snapshot().AwaitingAnswers);
        var playerRequest = Assert.Single(playerChosen.Snapshot().AwaitingAnswers);
        Assert.Equal(RequestDisposition.Declined, autoRequest.Disposition);
        Assert.Equal(RequestDisposition.Declined, playerRequest.Disposition);
    }

    // ================================================================= proof B: Marco and the demand

    /// <summary>Before Marco chooses, the panel identifies the demander — the plain-demand phrasing,
    /// since the first demand carries no violence claim yet.</summary>
    [Fact]
    public void Marcos_first_demand_panel_identifies_the_demander_before_he_chooses()
    {
        var session = SimulationSession.Start(Seed, Baseline, Marco);
        var pending = AdvanceToPause(session);

        Assert.Equal(Marco, pending.ActorId);
        Assert.Equal("Vincent Russo is demanding tribute from him", pending.Occasion);
    }

    /// <summary>Falsifier: each of Marco's three responses to the first demand gets an immediate
    /// acknowledgement.</summary>
    [Theory]
    [InlineData("pay what Vincent Russo is asking")]
    [InlineData("refuse Vincent Russo")]
    [InlineData("let it lie")]
    public void Each_of_marcos_responses_produces_an_immediate_acknowledgement(string choice)
    {
        var session = SimulationSession.Start(Seed, Baseline, Marco);
        var pending = AdvanceToPause(session);
        session.Choose(pending.Options.Single(o => o.Description == choice).Id);

        var snapshot = session.Snapshot();
        Assert.NotNull(snapshot.LastAction);
        Assert.Equal(choice, snapshot.LastAction!.Description);
    }

    /// <summary>Falsifier: refusing shows the known, already-produced consequence (the business is
    /// still unpaid) and invents no future retaliation — "they come back harder" is
    /// <c>Commit.Apply</c>'s own reconsideration-trigger text and is developer-only.</summary>
    [Fact]
    public void Refusing_shows_the_business_remains_unpaid_and_invents_no_future_consequence()
    {
        var session = SimulationSession.Start(Seed, Baseline, Marco);
        var pending = AdvanceToPause(session);
        session.Choose(pending.Options.Single(o => o.Description == "refuse Vincent Russo").Id);

        var snapshot = session.Snapshot();
        Assert.NotNull(snapshot.MyBusiness);
        Assert.Equal("Bellini's grocery", snapshot.MyBusiness!.Name);
        Assert.False(snapshot.MyBusiness.PayingTribute);
        Assert.DoesNotContain("harder", snapshot.LastAction!.Description, StringComparison.Ordinal);
    }

    /// <summary>Conceding shows the known consequence in the other direction.</summary>
    [Fact]
    public void Conceding_shows_the_business_now_paying()
    {
        var session = SimulationSession.Start(Seed, Baseline, Marco);
        var pending = AdvanceToPause(session);
        session.Choose(pending.Options.Single(o => o.Description == "pay what Vincent Russo is asking").Id);

        var snapshot = session.Snapshot();
        Assert.NotNull(snapshot.MyBusiness);
        Assert.True(snapshot.MyBusiness!.PayingTribute);
    }

    /// <summary><see cref="PlayerBusinessStatus"/> can never carry <c>Business.Resistance</c> — it is
    /// not merely unpopulated on this path, the type itself has no such member.</summary>
    [Fact]
    public void Player_business_status_has_no_resistance_member()
    {
        var properties = typeof(PlayerBusinessStatus).GetProperties().Select(p => p.Name).ToArray();
        Assert.DoesNotContain("Resistance", properties);
        Assert.Equal(new[] { "Id", "Name", "PayingTribute" }, properties);
    }

    /// <summary>
    /// The force-already-used phrasing is real and reachable: after a refusal, an escalation to
    /// force, and a further refusal, the owner holds a genuine <c>PersonUsedViolence</c> claim naming
    /// whoever is currently demanding <em>against the business this demand names</em>, and the
    /// occasion says so. Threaten alone — proven by staging <see cref="PlayerOccasion.For"/> directly
    /// against a world where only <c>Relations.Frighten</c> has run — must not be asserted as a
    /// distinct fact, since nothing structural distinguishes it from a plain demand.
    /// </summary>
    [Fact]
    public void A_demand_after_force_names_it_and_a_demand_after_only_a_threat_does_not()
    {
        var world = Cast.Build(Seed, Baseline);
        var marco = world.Get(Marco);
        var tommy = world.Get("tommy");

        // Force actually used and witnessed, exactly as Strategies.ResolveViolence records it.
        var violence = new Claim(ClaimKind.PersonUsedViolence, tommy.Id, Cast.Grocery, 999);
        marco.Cognition.Learn(violence, Stance.Knows, 1.0, SourceKind.Witness, marco.Id, world.Now);

        var groceryClaim = new Claim(ClaimKind.BusinessRefusesTribute, Cast.Grocery);
        var forcedTrigger = new ScheduledEvent
        {
            Id = 1, Time = world.Now, Kind = EventKind.Incident, OwnerId = marco.Id,
            Cause = "staged: an authored cause that must never reach a player",
            Payload = new EventPayload { Note = "tribute-demanded", TargetId = tommy.Id, AboutClaim = groceryClaim },
        };
        Assert.Equal(
            "Tommy Nardo has already used force over this",
            PlayerOccasion.For(forcedTrigger, marco, id => world.Find(id)?.Name ?? id));

        // Threaten actually happened here — Relations.Frighten has already run, exactly as
        // Strategies.cs's Threaten branch does — but it leaves no claim, only a relationship
        // dimension this occasion has no reader for. Structurally identical to a plain demand.
        var vincent = world.Get("vincent");
        Relations.Frighten(marco, vincent.Id, 0.35);
        var threatenedTrigger = new ScheduledEvent
        {
            Id = 2, Time = world.Now, Kind = EventKind.Incident, OwnerId = marco.Id,
            Cause = "staged: an authored cause that must never reach a player",
            Payload = new EventPayload { Note = "tribute-demanded", TargetId = vincent.Id, AboutClaim = groceryClaim },
        };
        Assert.Equal(
            "Vincent Russo is demanding tribute from him",
            PlayerOccasion.For(threatenedTrigger, marco, id => world.Find(id)?.Name ?? id));
    }

    /// <summary>
    /// Correction: the same demander's violence against a <em>different</em> business must not read
    /// as "over this". Nunzio holds a genuine <c>PersonUsedViolence(tommy -&gt; bellini-grocery)</c>
    /// claim — real force, just at a business that is not his own — while Tommy is currently demanding
    /// tribute from Nunzio's own bakery, carried through <c>AboutClaim</c> exactly as production code
    /// sets it. This isolates the business match specifically: the demander matches
    /// (<c>Claim.Subject</c>) but the business does not (<c>Claim.Object</c>), so the phrasing must
    /// stay a plain demand.
    /// </summary>
    [Fact]
    public void The_same_demanders_violence_at_a_different_business_does_not_read_as_over_this()
    {
        var world = Cast.Build(Seed, Baseline);
        var nunzio = world.Get("nunzio");
        var tommy = world.Get("tommy");

        // Tommy used force at the grocery — a real claim, just about the wrong business for this demand.
        var violenceAtGrocery = new Claim(ClaimKind.PersonUsedViolence, tommy.Id, Cast.Grocery, 999);
        nunzio.Cognition.Learn(violenceAtGrocery, Stance.Knows, 1.0, SourceKind.Witness, nunzio.Id, world.Now);

        // The current demand is about the bakery, carried through AboutClaim exactly as production
        // code sets it.
        var bakeryTrigger = new ScheduledEvent
        {
            Id = 3, Time = world.Now, Kind = EventKind.Incident, OwnerId = nunzio.Id,
            Cause = "staged: an authored cause that must never reach a player",
            Payload = new EventPayload
            {
                TargetId = tommy.Id,
                Note = "tribute-demanded",
                AboutClaim = new Claim(ClaimKind.BusinessRefusesTribute, Cast.Bakery),
            },
        };

        Assert.Equal(
            "Tommy Nardo is demanding tribute from him",
            PlayerOccasion.For(bakeryTrigger, nunzio, id => world.Find(id)?.Name ?? id));
    }

    // ================================================================= relationship movement

    /// <summary>Mutation guard: trust movement is shown only to the character whose own outward
    /// relationship moved, never to anybody else — staged directly against a hand-built
    /// <see cref="AccountConflict"/> so the filter is proven independent of scenario timing.</summary>
    [Fact]
    public void Trust_movement_is_shown_only_to_the_listener_whose_own_relationship_moved()
    {
        var world = Cast.Build(Seed, Baseline);
        var conflict = new AccountConflict(
            new Claim(ClaimKind.BusinessRefusesTribute, Cast.Grocery),
            "tommy", Stance.Rejects, 0.8, SourceKind.Report,
            Stance.Believes, 0.5, SourceKind.Report, "the-books");
        world.AccountConflicts.Add(new PerceivedConflict("vincent", conflict, world.Now));

        var vincentSnapshot = PlayerView.Build(world, "vincent", world.Now);
        var salvatoreSnapshot = PlayerView.Build(world, Salvatore, world.Now);

        var movement = Assert.Single(vincentSnapshot.RecentTrustMovements);
        Assert.Equal("tommy", movement.PersonId);
        Assert.False(movement.Warmed);
        Assert.Empty(salvatoreSnapshot.RecentTrustMovements);
    }

    /// <summary>The mirror image: a fresh agreement warms trust, shown only to its own listener.</summary>
    [Fact]
    public void Trust_agreement_warms_and_is_also_listener_scoped()
    {
        var world = Cast.Build(Seed, Baseline);
        var agreement = new AccountAgreement(
            new Claim(ClaimKind.BusinessRefusesTribute, Cast.Grocery),
            "tommy", Stance.Believes, 0.7, SourceKind.Report,
            Stance.Believes, 0.5, SourceKind.Report, "the-books");
        world.AccountAgreements.Add(new PerceivedAgreement("vincent", agreement, world.Now));

        var vincentSnapshot = PlayerView.Build(world, "vincent", world.Now);
        var salvatoreSnapshot = PlayerView.Build(world, Salvatore, world.Now);

        var movement = Assert.Single(vincentSnapshot.RecentTrustMovements);
        Assert.Equal("tommy", movement.PersonId);
        Assert.True(movement.Warmed);
        Assert.Empty(salvatoreSnapshot.RecentTrustMovements);
    }

    // ================================================================= action-kind audit (correction)

    /// <summary>
    /// Every player-selectable <see cref="ActionKind"/> reachable through the existing six-character
    /// scenarios, audited through the shared rendering <c>LastAction</c> itself calls
    /// (<see cref="PlayerOption.Describe"/>) — never a hand-constructed candidate, so "reachable"
    /// means what it says: every <see cref="DecisionRecord.Chosen"/> across all five variants' full
    /// 90-day autonomous runs.
    ///
    /// <b>Reachable, confirmed by this test:</b> <c>StartStrategy</c>, <c>ContinueStrategy</c>,
    /// <c>AlterStrategy</c>, <c>DelegateStrategy</c>, <c>ReportToSuperior</c>, <c>SeekApproval</c>,
    /// <c>SeekCorroboration</c>, <c>Retaliate</c>, <c>Concede</c>, <c>Refuse</c>, <c>DoNothing</c> — 11
    /// of 14. <b>Not reached by any variant at seed 42 across 90 days, recorded honestly rather than
    /// forced:</b> <c>AbandonStrategy</c>, <c>PostponeStrategy</c>, <c>RequestHelp</c>. This matches
    /// `ROADMAP.md`'s existing "apparently-dead lines" finding; manufacturing a path to force any of
    /// the three would be exactly the kind of result this milestone forbids.
    /// </summary>
    [Fact]
    public void Every_reachable_action_kind_renders_through_the_shared_last_action_projection()
    {
        var reachedKinds = new HashSet<ActionKind>();

        foreach (var variant in Variants.All)
        {
            var session = SimulationSession.Start(Seed, variant, controlledCharacterId: null, viewpointCharacterId: Salvatore);
            session.AdvanceTo(Cast.Start.AddDays(90));

            foreach (var decision in session.World.Decisions)
            {
                if (decision.Chosen is not { Candidate: var candidate }) continue;
                reachedKinds.Add(candidate.Kind);

                var actor = session.World.Get(decision.ActorId);
                string Name(string id) => session.World.Find(id)?.Name
                    ?? session.World.Businesses.GetValueOrDefault(id)?.Name ?? id;
                Pronouns PronounsOf(string id) => session.World.Find(id)?.Pronouns ?? Pronouns.He;

                string description = PlayerOption.Describe(candidate, Name, actor.Pronouns, PronounsOf);
                Assert.False(string.IsNullOrWhiteSpace(description),
                    $"{candidate.Kind} (variant {variant}, {decision.At:yyyy-MM-dd}) rendered an empty description");
            }
        }

        foreach (var expected in new[]
        {
            ActionKind.StartStrategy, ActionKind.ContinueStrategy, ActionKind.AlterStrategy,
            ActionKind.DelegateStrategy, ActionKind.ReportToSuperior, ActionKind.SeekApproval,
            ActionKind.SeekCorroboration, ActionKind.Retaliate, ActionKind.Concede,
            ActionKind.Refuse, ActionKind.DoNothing,
        })
            Assert.Contains(expected, reachedKinds);

        // Recorded, not asserted as a requirement to force: these three do not fire in any of the
        // five variants across a full 90-day run at seed 42. If a future milestone makes one
        // reachable, this assertion should move to the "expected" list above rather than being
        // silently dropped.
        foreach (var unreached in new[] { ActionKind.AbandonStrategy, ActionKind.PostponeStrategy, ActionKind.RequestHelp })
            Assert.DoesNotContain(unreached, reachedKinds);
    }

    // ================================================================= developer-truth exclusion

    /// <summary>
    /// Mutation guard: when nothing was open to a character — <c>Chosen</c> is null and
    /// <c>DecisionRecord.Outcome</c> is the developer-only literal "nothing was open to him" — no
    /// <c>LastAction</c> is shown at all, rather than that literal string leaking through.
    /// </summary>
    [Fact]
    public void When_nothing_was_open_no_last_action_is_shown()
    {
        var world = Cast.Build(Seed, Baseline);
        var salvatore = world.Get(Salvatore);

        world.Decisions.Add(new DecisionRecord(
            world.NextDecisionId(), world.Now, salvatore.Id, salvatore.Name,
            0, EventKind.RoleReview, "staged: an authored cause that must never reach a player",
            new Agenda(AgendaKind.RespondToTrigger, "staged agenda", "staged reason"),
            Array.Empty<InformationRecord>(), Array.Empty<Candidate>(), Array.Empty<Rejection>(),
            Array.Empty<ScoreBreakdown>(), null, "nothing was open to him",
            Array.Empty<string>(), Array.Empty<string>()));

        var snapshot = PlayerView.Build(world, salvatore.Id, world.Now);
        Assert.Null(snapshot.LastAction);
    }

    // ================================================================= mutation-guarded resolution

    /// <summary>Mutation guard: stale testimony from before the request was made must not resolve
    /// it — the <c>&gt;= r.At</c> comparison is load-bearing, not incidental.</summary>
    [Fact]
    public void Testimony_from_before_the_request_was_made_does_not_resolve_it()
    {
        var world = Cast.Build(Seed, Baseline);
        var salvatore = world.Get(Salvatore);
        var about = new Claim(ClaimKind.BusinessRefusesTribute, Cast.Grocery);

        // An account from Vincent, well before he is ever asked anything.
        salvatore.Cognition.Receive(
            ReportedClaim.Honest(about, Stance.Rejects, 0.8, SourceKind.Report), "vincent", world.Now);

        // The request itself is made after that stale account. No decision exists yet with this
        // wake id, so this must read Pending, not Declined.
        world.Requests.Add(new InformationRequest(1, salvatore.Id, "vincent", about, world.Now.AddDays(1), WakeEventId: 999));

        var snapshot = PlayerView.Build(world, salvatore.Id, world.Now.AddDays(1));
        var request = Assert.Single(snapshot.AwaitingAnswers);
        Assert.Equal(RequestDisposition.Pending, request.Disposition);
    }

    /// <summary>Mutation guard: an account from somebody other than the person actually asked must not
    /// resolve the request — the sender filter is load-bearing.</summary>
    [Fact]
    public void An_account_from_a_third_party_does_not_resolve_a_request_addressed_to_someone_else()
    {
        var world = Cast.Build(Seed, Baseline);
        var salvatore = world.Get(Salvatore);
        var about = new Claim(ClaimKind.BusinessRefusesTribute, Cast.Grocery);

        world.Requests.Add(new InformationRequest(1, salvatore.Id, "vincent", about, world.Now, WakeEventId: 999));

        // Tommy, not Vincent, happens to volunteer a matching account afterward.
        salvatore.Cognition.Receive(
            ReportedClaim.Honest(about, Stance.Rejects, 0.8, SourceKind.Report),
            "tommy", world.Now.AddDays(1));

        var snapshot = PlayerView.Build(world, salvatore.Id, world.Now.AddDays(1));
        var request = Assert.Single(snapshot.AwaitingAnswers);
        Assert.Equal(RequestDisposition.Pending, request.Disposition);
    }

    /// <summary>Mutation guard: an account of a different claim from the right person must not resolve
    /// a request about a different subject — claim equality, not claim kind, is load-bearing.</summary>
    [Fact]
    public void An_account_of_a_different_claim_does_not_resolve_the_request()
    {
        var world = Cast.Build(Seed, Baseline);
        var salvatore = world.Get(Salvatore);
        var about = new Claim(ClaimKind.BusinessRefusesTribute, Cast.Grocery);
        var somethingElse = new Claim(ClaimKind.BusinessRefusesTribute, Cast.Bakery);

        world.Requests.Add(new InformationRequest(1, salvatore.Id, "vincent", about, world.Now, WakeEventId: 999));

        salvatore.Cognition.Receive(
            ReportedClaim.Honest(somethingElse, Stance.Rejects, 0.8, SourceKind.Report),
            "vincent", world.Now.AddDays(1));

        var snapshot = PlayerView.Build(world, salvatore.Id, world.Now.AddDays(1));
        var request = Assert.Single(snapshot.AwaitingAnswers);
        Assert.Equal(RequestDisposition.Pending, request.Disposition);
    }

    // ================================================================= helpers

    /// <summary>Steps single events until a decision is waiting — never a fast-forward horizon, which
    /// would carry across a subsequent <c>Choose</c> and run straight past the point under test.</summary>
    private static PendingDecision AdvanceToPause(SimulationSession session)
    {
        for (int guard = 0; guard < 5000 && session.Status != SessionStatus.AwaitingChoice; guard++)
            session.StepEvent();
        Assert.Equal(SessionStatus.AwaitingChoice, session.Status);
        return session.Pending!;
    }
}
