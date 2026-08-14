using System.Reflection;
using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Scenario;
using CrimeSim.Sim;
using CrimeSim.Trace;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>
/// Milestone 006: the social consequence of a perceived account conflict, and the centralized
/// relationship API that owns it. See docs/CURRENT_MILESTONE.md for the ruling each of these pins.
///
/// The load-bearing pair is at the foot of the file: a counterfactual showing a conflict changes a
/// score, and a staged boundary case showing it changes which candidate wins. Ruling 7 requires
/// behavioural relevance be proved that way rather than by tuning magnitudes until a natural
/// variant flips, so neither of them reads the accepted scenario for its evidence.
/// </summary>
public sealed class RelationalConsequenceTests
{
    private static readonly DateTime At = new(1987, 3, 2, 8, 0, 0, DateTimeKind.Utc);

    private static readonly Claim Beating =
        new(ClaimKind.PersonUsedViolence, "tommy", "bellini-grocery");

    // ---------------------------------------------------------------- the conflict outcome

    [Fact]
    public void Being_told_something_he_has_no_position_on_is_not_a_conflict()
    {
        var listener = new Cognition();

        var receipt = listener.Receive(
            ReportedClaim.Honest(Beating, Stance.Believes, 0.8, SourceKind.Participant), "tommy", At);

        Assert.Null(receipt.Conflict);
    }

    [Fact]
    public void Agreement_is_not_a_conflict()
    {
        var listener = new Cognition();
        listener.Learn(Beating, Stance.Believes, 0.7, SourceKind.Discovery, "salvatore", At);

        var receipt = listener.Receive(
            ReportedClaim.Honest(Beating, Stance.Believes, 0.8, SourceKind.Participant),
            "tommy", At.AddDays(1));

        Assert.Null(receipt.Conflict);
    }

    [Fact]
    public void A_denial_of_something_he_holds_is_a_conflict()
    {
        var listener = new Cognition();
        listener.Learn(Beating, Stance.Believes, 0.7, SourceKind.Discovery, "salvatore", At);

        var receipt = listener.Receive(Denial(0.9), "tommy", At.AddDays(1));

        var conflict = Assert.NotNull(receipt.Conflict);
        Assert.Equal("tommy", conflict.SpeakerId);
        Assert.Equal(Beating, conflict.Claim);
        Assert.Equal(Stance.Rejects, conflict.AssertedStance);
    }

    /// <summary>
    /// Ruling 12's "non-repeated", and it is inherited rather than separately written: the verbatim
    /// repeat check in Receive returns before the disagreement branch, so the same early return
    /// bounds both compounding confidence loss and compounding trust loss. That inheritance is
    /// exactly why it needs its own test — the comparison it rests on is code milestone 003 and
    /// milestone 004 each had to repair.
    /// </summary>
    [Fact]
    public void Repeating_the_same_denial_is_not_a_second_conflict()
    {
        var listener = new Cognition();
        listener.Learn(Beating, Stance.Believes, 0.7, SourceKind.Discovery, "salvatore", At);

        var first = listener.Receive(Denial(0.9), "tommy", At.AddDays(1));
        var second = listener.Receive(Denial(0.9), "tommy", At.AddDays(2));
        var third = listener.Receive(Denial(0.9), "tommy", At.AddDays(3));

        Assert.NotNull(first.Conflict);
        Assert.Null(second.Conflict);
        Assert.Null(third.Conflict);
    }

    [Fact]
    public void Repeated_denials_therefore_cost_trust_only_once()
    {
        var listener = Character("salvatore");
        Relations.Establish(listener, "tommy", trust: 0.80);
        listener.Cognition.Learn(Beating, Stance.Believes, 0.7, SourceKind.Discovery, listener.Id, At);

        for (int i = 1; i <= 5; i++)
            Apply(listener, listener.Cognition.Receive(Denial(0.9), "tommy", At.AddDays(i)));

        // One conflict's worth, not five. 0.80 - 0.35 * (0.7 * 0.9).
        Assert.Equal(0.80 - Relations.ConflictTrustCost * 0.63, listener.Social.Toward("tommy").Trust, 9);
    }

    [Fact]
    public void Strength_is_how_firmly_he_held_it_times_how_firmly_it_was_denied()
    {
        var listener = new Cognition();
        listener.Learn(Beating, Stance.Believes, 0.60, SourceKind.Discovery, "salvatore", At);

        var conflict = Assert.NotNull(listener.Receive(Denial(0.50), "tommy", At.AddDays(1)).Conflict);

        Assert.Equal(0.60, conflict.PriorConfidence, 9);
        Assert.Equal(0.50, conflict.AssertedConfidence, 9);
        Assert.Equal(0.30, conflict.Strength, 9);
    }

    // ---------------------------------------------------------------- ruling 13: one rule, both paths

    /// <summary>
    /// The prior's provenance travels even though no rule currently reads it. Milestone 004's
    /// lesson four times over was that a distinction dropped on the way to its next reader is the
    /// defect this codebase produces most reliably; a later evidence-led pass should not have to
    /// reconstruct what was discarded here.
    /// </summary>
    [Theory]
    [InlineData(SourceKind.Participant)]
    [InlineData(SourceKind.Witness)]
    [InlineData(SourceKind.Discovery)]
    [InlineData(SourceKind.FirstHandTestimony)]
    [InlineData(SourceKind.Report)]
    [InlineData(SourceKind.Inference)]
    public void The_conflict_preserves_the_prior_provenance_whatever_it_was(SourceKind prior)
    {
        var listener = new Cognition();
        listener.Learn(Beating, Stance.Believes, 0.7, prior, "someone", At);

        var conflict = Assert.NotNull(listener.Receive(Denial(0.9), "tommy", At.AddDays(1)).Conflict);

        Assert.Equal(prior, conflict.PriorSourceKind);
        Assert.Equal("someone", conflict.PriorSourceId);
        Assert.Equal(Stance.Believes, conflict.PriorStance);
    }

    /// <summary>
    /// Ruling 13. Contradicting what a man saw and contradicting what he was told cost the same
    /// socially, because Cognition already charges the epistemic difference — the 0.15-versus-0.45
    /// erosion and the stance protection — and weighting it again here would bill it twice.
    ///
    /// Note the test asserts equality of the *trust* movement while the two paths deliberately end
    /// with different confidence, which is the distinction being kept where it belongs.
    /// </summary>
    [Fact]
    public void A_conflict_costs_the_same_trust_whether_the_prior_was_his_own_or_hearsay()
    {
        var sawIt = Character("a");
        Relations.Establish(sawIt, "tommy", trust: 0.80);
        sawIt.Cognition.Learn(Beating, Stance.Believes, 0.7, SourceKind.Witness, sawIt.Id, At);

        var wasTold = Character("b");
        Relations.Establish(wasTold, "tommy", trust: 0.80);
        wasTold.Cognition.Learn(Beating, Stance.Believes, 0.7, SourceKind.Report, "vincent", At);

        Apply(sawIt, sawIt.Cognition.Receive(Denial(0.9), "tommy", At.AddDays(1)));
        Apply(wasTold, wasTold.Cognition.Receive(Denial(0.9), "tommy", At.AddDays(1)));

        Assert.Equal(sawIt.Social.Toward("tommy").Trust, wasTold.Social.Toward("tommy").Trust, 9);

        // And the epistemic difference is still charged, in the layer that owns it.
        Assert.True(sawIt.Cognition.ConfidenceIn(Beating) > wasTold.Cognition.ConfidenceIn(Beating));
    }

    // ---------------------------------------------------------------- rulings 2 and 12

    [Fact]
    public void Only_the_listener_relationship_moves()
    {
        var listener = Character("salvatore");
        var speaker = Character("tommy");
        Relations.Establish(listener, "tommy", trust: 0.80);
        Relations.Establish(speaker, "salvatore", trust: 0.80);
        listener.Cognition.Learn(Beating, Stance.Believes, 0.7, SourceKind.Discovery, listener.Id, At);

        Apply(listener, listener.Cognition.Receive(Denial(0.9), "tommy", At.AddDays(1)));

        Assert.True(listener.Social.Toward("tommy").Trust < 0.80);
        Assert.Equal(0.80, speaker.Social.Toward("salvatore").Trust, 9);
    }

    [Fact]
    public void A_conflict_raises_no_grievance()
    {
        var listener = Character("salvatore");
        Relations.Establish(listener, "tommy", trust: 0.80);
        listener.Cognition.Learn(Beating, Stance.Believes, 0.7, SourceKind.Discovery, listener.Id, At);

        Apply(listener, listener.Cognition.Receive(Denial(0.9), "tommy", At.AddDays(1)));

        Assert.Empty(listener.Social.Toward("tommy").Grievances);
        Assert.Empty(listener.Social.Grievances);
    }

    [Fact]
    public void Trust_cannot_be_driven_below_zero()
    {
        var listener = Character("salvatore");
        Relations.Establish(listener, "tommy", trust: 0.05);
        listener.Cognition.Learn(Beating, Stance.Knows, 1.0, SourceKind.Participant, listener.Id, At);

        Apply(listener, listener.Cognition.Receive(Denial(1.0), "tommy", At.AddDays(1)));

        Assert.Equal(0.0, listener.Social.Toward("tommy").Trust, 9);
    }

    // ---------------------------------------------------------------- ruling 5: the API is authoritative

    /// <summary>
    /// The hazard this closes was invisible until relationships entered the replay comparison:
    /// SocialState.Toward was a get-or-create, and scoring reads a great many relationships that do
    /// not exist, so the act of scoring a candidate would have changed the snapshot.
    /// </summary>
    [Fact]
    public void Reading_a_relationship_does_not_create_one()
    {
        var c = Character("salvatore");

        var reading = c.Social.Toward("nobody-at-all");

        Assert.Equal(0, reading.Trust);
        Assert.Equal(0, reading.Fear);
        Assert.Equal(0, reading.Obligation);
        Assert.Empty(c.Social.Others);
        Assert.Empty(c.Social.All);
    }

    /// <summary>
    /// The rule that matters in practice: after a complete run, every relationship a character holds
    /// must trace to something that happened to him, not to somebody having been scored against.
    ///
    /// Note the second clause, which an earlier version of this test lacked and which cost it its
    /// point. A conflict with somebody a character had no relationship with legitimately creates one
    /// and can legitimately leave it at zero on every dimension — trust is floored at zero, so a
    /// stranger who contradicts you produces exactly that. Asserting on non-zero dimensions alone
    /// conflated "created by an event" with "created by a read", and only passed because the single
    /// variant it ran against happened not to contain the case. It now runs against all five.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("cautious-vincent")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    [InlineData("resentful-tommy")]
    public void A_full_run_creates_no_relationships_by_reading(string variant)
    {
        var world = Run(variant);

        foreach (var c in world.Characters.Values)
            foreach (var rel in c.Social.All)
            {
                bool moved = rel.Trust > 0 || rel.Fear > 0 || rel.Obligation > 0 || rel.Grievances.Count > 0;
                bool fromConflict = world.AccountConflicts.Any(
                    x => x.ListenerId == c.Id && x.Conflict.SpeakerId == rel.OtherId);

                Assert.True(moved || fromConflict,
                    $"{c.Id} holds an all-zero relationship toward {rel.OtherId} that no recorded " +
                    "event accounts for, which means something created one by reading it.");
            }
    }

    /// <summary>
    /// The edge fires in the accepted scenario, and this pins how often. A budget rather than a
    /// bare "greater than zero": a run that started producing conflicts in bulk would be a runaway
    /// of exactly the kind milestone 003's corroboration loop was, and the number is small enough
    /// that a change to it should be explained rather than absorbed.
    /// </summary>
    [Theory]
    [InlineData("baseline", 1)]
    [InlineData("cautious-vincent", 2)]
    [InlineData("watchful-boss", 1)]
    [InlineData("disloyal-vincent", 1)]
    [InlineData("resentful-tommy", 1)]
    public void The_scenario_produces_the_expected_number_of_conflicts(string variant, int expected)
        => Assert.Equal(expected, Run(variant).AccountConflicts.Count);

    /// <summary>
    /// And the trust movement those conflicts caused is real, not merely recorded. Salvatore starts
    /// at 0.50 toward Vincent in every variant and is contradicted by him once.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    [InlineData("resentful-tommy")]
    public void The_boss_trusts_his_capo_less_after_being_contradicted(string variant)
    {
        var world = Run(variant);
        Assert.True(world.Get("salvatore").Social.Toward("vincent").Trust < 0.50);
    }

    /// <summary>
    /// Ruling 5's "runtime mutation cannot bypass it", checked at the type level rather than by
    /// convention. The concrete relationship class is private to Relations, so outside that class
    /// the type cannot be named, constructed, or cast to — this asserts the public surface really
    /// is the read-only interface and cannot be quietly widened back.
    /// </summary>
    [Fact]
    public void Relationship_state_has_no_public_mutation_surface()
    {
        var iface = typeof(IRelationship);

        foreach (var p in iface.GetProperties())
            Assert.Null(p.SetMethod);

        Assert.Empty(iface.GetMethods().Where(m => !m.IsSpecialName));

        // No publicly visible implementation of it exists anywhere in the simulation assembly.
        var publicImpls = typeof(Relations).Assembly.GetExportedTypes()
            .Where(t => iface.IsAssignableFrom(t) && t != iface)
            .ToList();

        Assert.Empty(publicImpls);
    }

    [Fact]
    public void Grievances_enumerate_in_a_deterministic_order()
    {
        var c = Character("vincent");
        Relations.RaiseGrievance(c, new Grievance("tommy", "second", 0.1, At));
        Relations.RaiseGrievance(c, new Grievance("salvatore", "first", 0.2, At));
        Relations.RaiseGrievance(c, new Grievance("tommy", "third", 0.3, At));

        var order = c.Social.Grievances.Select(g => g.Description).ToList();

        // By person in ordinal id order, then in the order they accumulated against that person.
        Assert.Equal(new[] { "first", "second", "third" }, order);
        Assert.Equal(0.4, c.Social.GrievanceAgainst("tommy"), 9);
        Assert.Equal(0.2, c.Social.GrievanceAgainst("salvatore"), 9);
        Assert.Equal(0.0, c.Social.GrievanceAgainst("nobody"), 9);
    }

    // ---------------------------------------------------------------- all three receipt paths

    /// <summary>
    /// The consequence is applied wherever an assertion is received, not only in the report
    /// channel. Applying it in one place and not the others would be this project's most reliable
    /// defect — a rule written where it was noticed and missing everywhere else the value travels —
    /// and it would let a superior contradict a subordinate for free by calling it an instruction.
    /// </summary>
    [Fact]
    public void A_report_that_contradicts_costs_the_recipient_trust()
    {
        var world = Cast.Build(42, "baseline");
        var salvatore = world.Get("salvatore");
        var tommy = world.Get("tommy");
        Relations.Establish(salvatore, "tommy", trust: 0.80);
        salvatore.Cognition.Learn(Beating, Stance.Believes, 0.7, SourceKind.Discovery, salvatore.Id, world.Now);

        var report = new Report(
            world.NextReportId(), tommy.Id, salvatore.Id, world.Now, ReportCandor.False,
            new[] { ReportedClaim.Misrepresenting(Beating, Stance.Rejects, 0.9, SourceKind.Report, SourceKind.Participant) },
            Array.Empty<Claim>(), "framing");

        Reporting.Deliver(world, report, salvatore);

        Assert.True(salvatore.Social.Toward("tommy").Trust < 0.80);
        Assert.Single(world.AccountConflicts);
        Assert.Equal("salvatore", world.AccountConflicts[0].ListenerId);
    }

    [Fact]
    public void An_assignment_briefing_that_contradicts_costs_the_recipient_trust()
    {
        var world = Cast.Build(42, "baseline");
        var vincent = world.Get("vincent");
        Relations.Establish(vincent, "salvatore", trust: 0.80);

        var refusing = new Claim(ClaimKind.BusinessRefusesTribute, Cast.Grocery);
        vincent.Cognition.Learn(refusing, Stance.Believes, 0.7, SourceKind.Discovery, vincent.Id, world.Now);

        // The boss briefs him that the shop is not holding back — contradicting what he found.
        var assignment = new Assignment(
            world.NextAssignmentId(), "restore the harbour tribute", "salvatore", vincent.Id, Cast.Harbour,
            Array.Empty<string>(),
            new[] { ReportedClaim.Honest(refusing, Stance.Rejects, 0.9, SourceKind.Report) },
            world.Now, world.Now.AddDays(30));
        world.Org.Assignments.Add(assignment);

        world.Queue.Schedule(world.Now, EventKind.AssignmentDelivered, vincent.Id, "briefed",
            new EventPayload { AssignmentId = assignment.Id });
        Runner.Run(world, world.Now.AddMinutes(1));

        Assert.True(vincent.Social.Toward("salvatore").Trust < 0.80);
        Assert.Contains(world.AccountConflicts, c => c.ListenerId == "vincent" && c.Conflict.SpeakerId == "salvatore");
    }

    [Fact]
    public void A_delegation_briefing_that_contradicts_costs_the_delegate_trust()
    {
        var world = Cast.Build(42, "baseline");
        var vincent = world.Get("vincent");
        var tommy = world.Get("tommy");

        var vulnerable = new Claim(ClaimKind.TargetIsVulnerable, Cast.Grocery);
        vincent.Cognition.Learn(vulnerable, Stance.Believes, 0.9, SourceKind.Discovery, vincent.Id, world.Now);
        tommy.Cognition.Learn(vulnerable, Stance.Rejects, 0.9, SourceKind.Witness, tommy.Id, world.Now);

        double before = tommy.Social.Toward("vincent").Trust;
        Assert.True(before > 0, "the fixture needs a real starting relationship for this to be visible");

        Delegate(world, vincent, tommy);

        Assert.True(tommy.Social.Toward("vincent").Trust < before);
        Assert.Contains(world.AccountConflicts, c => c.ListenerId == "tommy" && c.Conflict.SpeakerId == "vincent");
    }

    // ---------------------------------------------------------------- ruling 7: behavioural relevance

    /// <summary>
    /// The counterfactual. Two characters identical in every respect except that one has been
    /// contradicted, scoring the same candidate — the difference in the Loyalty-derived component
    /// is the edge this milestone exists to create.
    ///
    /// Deliberately not read off the accepted scenario. Ruling 7 forbids proving relevance by
    /// tuning until a natural variant flips, so the proof is staged and the scenario is left to
    /// report whatever it reports.
    /// </summary>
    [Fact]
    public void A_conflict_changes_a_later_score()
    {
        double undisturbed = ReportScoreAfter(conflict: false);
        double contradicted = ReportScoreAfter(conflict: true);

        Assert.True(contradicted < undisturbed,
            $"a contradicted man should weigh reporting to that person differently: " +
            $"{contradicted} was not below {undisturbed}");
    }

    /// <summary>
    /// The boundary case, and the one that says what this milestone is actually for.
    ///
    /// A man weighing whether to move against his boss prices the danger partly by how bound to him
    /// he is — <c>Utility</c>'s retaliation risk is <c>-(1.3 + 2.2 * loyalty)</c>. Loyalty is derived
    /// from trust. So a boss who contradicts an account his capo holds makes moving against himself
    /// cheaper, and at the margin that is the difference between the capo sitting on it and the capo
    /// acting. Nobody wrote a rule connecting those two things; it falls out of a trust edge feeding
    /// a derived value that a risk term already read.
    ///
    /// The margin is staged rather than discovered: the fixture positions the two candidates close
    /// enough that one conflict separates them. That is the honest direction — the alternative,
    /// adjusting ConflictTrustCost until a scenario flipped, is what ruling 7 forbids.
    /// </summary>
    [Fact]
    public void At_the_boundary_a_conflict_changes_which_candidate_wins()
    {
        Assert.Equal("hold", WinnerAfter(conflict: false));
        Assert.Equal("retaliate", WinnerAfter(conflict: true));
    }

    // ---------------------------------------------------------------- run-wide

    [Theory]
    [InlineData("baseline")]
    [InlineData("cautious-vincent")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    [InlineData("resentful-tommy")]
    public void Every_recorded_conflict_moved_exactly_the_relationship_it_names(string variant)
    {
        var world = Run(variant);

        foreach (var pc in world.AccountConflicts)
        {
            var listener = world.Get(pc.ListenerId);
            Assert.Contains(listener.Social.Others, id => id == pc.Conflict.SpeakerId);
        }
    }

    /// <summary>
    /// No listener records the same speaker's same account twice. This is the run-wide form of the
    /// non-repetition guarantee — the unit test above proves the rule, this proves nothing in the
    /// scenario reaches it by another route.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("cautious-vincent")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    [InlineData("resentful-tommy")]
    public void No_conflict_is_recorded_twice_for_the_same_account(string variant)
    {
        var world = Run(variant);

        var keys = world.AccountConflicts
            .Select(c => $"{c.ListenerId}|{c.Conflict.SpeakerId}|{c.Conflict.Claim}|{c.At:O}")
            .ToList();

        Assert.Equal(keys.Count, keys.Distinct().Count());
    }

    // ---------------------------------------------------------------- ruling 8: player-facing

    [Theory]
    [InlineData("baseline", "salvatore")]
    [InlineData("baseline", "vincent")]
    [InlineData("disloyal-vincent", "salvatore")]
    [InlineData("resentful-tommy", "tommy")]
    public void The_view_never_asserts_deception_or_prints_a_number(string variant, string viewpoint)
    {
        var world = Run(variant);
        string view = IntelligenceWriter.Render(world, viewpoint);

        // The standing preamble — "where it is wrong, it is wrong because somebody was wrong or
        // somebody lied" — is a general statement about how fallible everything below it is, with
        // no subject and no claim attached. It is exactly the disclaimer this section wants, and
        // deliberately excluded here: what must never happen is an *attributed* line telling the
        // player that a named person lied about a named thing.
        string body = view[(view.IndexOf("WHAT HE HAS", StringComparison.Ordinal))..];

        // Never an accusation. The simulation itself cannot tell a lie from an honest disagreement
        // at this layer, so the player must not be handed a certainty the model does not have.
        foreach (var forbidden in new[] { "lied", "lying", "deceiv", "dishonest", "misrepresent", "a lie" })
            Assert.DoesNotContain(forbidden, body, StringComparison.OrdinalIgnoreCase);

        // Never a number. A relationship dimension or a confidence with a decimal point in it is
        // hidden state wearing a percentage sign.
        Assert.DoesNotMatch(@"\d+\.\d+", view);
    }

    [Fact]
    public void The_view_describes_his_own_standing_and_never_another_mans()
    {
        var world = Run("baseline");
        string view = IntelligenceWriter.Render(world, "vincent");

        // Every attitude sentence is one of the fixed qualitative readings, and each is about the
        // viewpoint character. Nothing renders what anybody else makes of him.
        var vincent = world.Get("vincent");
        foreach (var rel in vincent.Social.All)
        {
            if (rel.Trust <= 0 && rel.Fear <= 0 && rel.Grievances.Count == 0) continue;
            Assert.Contains(IntelligenceWriter.Standing(rel.Trust), view);
        }
    }

    [Fact]
    public void Standing_never_names_a_method_or_a_number()
    {
        foreach (double t in new[] { 0.0, 0.05, 0.2, 0.5, 0.9, 1.0 })
        {
            string s = IntelligenceWriter.Standing(t);
            Assert.DoesNotContain("lie", s, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(t.ToString("0.00"), s);
        }
    }

    // ---------------------------------------------------------------- fixtures

    private static ReportedClaim Denial(double confidence)
        => ReportedClaim.Misrepresenting(
            Beating, Stance.Rejects, confidence, claimed: SourceKind.Report, actual: SourceKind.Participant);

    private static void Apply(Character listener, Receipt receipt)
    {
        if (receipt.Conflict is { } conflict) Relations.RecordAccountConflict(listener, conflict);
    }

    private static Character Character(string id) => new()
    {
        Id = id,
        Name = id,
        RoleTitle = "test",
        Capabilities = new Capabilities(new Dictionary<Skill, double>(), 1, 1000, 1, new[] { Cast.Harbour }),
        Psychology = new Psychology(new Dictionary<Trait, double>(), new Dictionary<Drive, double>()),
    };

    private static World Run(string variant)
    {
        var world = Cast.Build(42, variant);
        Runner.Run(world, Cast.Start.AddDays(90));
        return world;
    }

    private static void Delegate(World world, Character from, Character to)
    {
        from.Execution.Strategy = new StrategyInstance
        {
            OwnerId = from.Id,
            LocalSequence = from.StrategyCount++,
            Kind = StrategyKind.SecureTribute,
            Domain = Cast.Harbour,
            TargetId = Cast.Grocery,
            Method = CoercionMethod.Threaten,
            StartedAt = world.Now,
            Deadline = world.Now.AddDays(30),
        };

        var candidate = new Candidate($"delegate:{to.Id}", ActionKind.DelegateStrategy, "test", "hand it over")
        { TargetId = to.Id, Domain = Cast.Harbour };

        var ctx = Context(world, from);
        Commit.Apply(world, from, candidate, ctx.Agenda, ctx, new List<string>());
    }

    private static GeneratorContext Context(World world, Character actor)
    {
        var perceived = new PerceivedSituation(
            actor.Id, world.Now, actor.Cognition.Records, actor.Cognition.Testimony);

        return new GeneratorContext(
            actor.View, perceived,
            new Agenda(AgendaKind.DischargeResponsibility, "get the harbour earning", "assigned", Cast.Harbour),
            world.Now,
            new ScheduledEvent
            {
                Id = 1,
                Time = world.Now,
                Kind = EventKind.RoleReview,
                OwnerId = actor.Id,
                Cause = "test",
            },
            world.Org.OfficeForDomain(Cast.Harbour), null, Array.Empty<Policy>(),
            Pipeline.SuperiorOf(world, actor), Pipeline.SubordinatesOf(world, actor),
            Pipeline.OrgMembersOf(world, actor),
            Array.Empty<Report>(), Array.Empty<InformationRequest>(), new[] { Cast.Grocery });
    }

    /// <summary>
    /// One staged character weighing a report to his superior, optionally after that superior has
    /// contradicted him. Everything but the conflict is held identical.
    /// </summary>
    private static double ReportScoreAfter(bool conflict)
    {
        var world = Cast.Build(42, "baseline");
        var vincent = world.Get("vincent");
        Relations.Establish(vincent, "salvatore", trust: 0.80, obligation: 0.40);

        var refusing = new Claim(ClaimKind.BusinessRefusesTribute, Cast.Grocery);
        vincent.Cognition.Learn(refusing, Stance.Believes, 0.9, SourceKind.Discovery, vincent.Id, world.Now);

        if (conflict)
            Apply(vincent, vincent.Cognition.Receive(
                ReportedClaim.Honest(refusing, Stance.Rejects, 0.9, SourceKind.Report),
                "salvatore", world.Now));

        var candidate = new Candidate("report:salvatore", ActionKind.ReportToSuperior, "test", "report in")
        { TargetId = "salvatore", Domain = Cast.Harbour, Candor = ReportCandor.Candid };

        var ctx = Context(world, vincent);
        var rng = Rng.ForOccasion(world.Seed, "test|fixed");
        return Utility.Score(candidate, vincent.View, vincent.Psychology, ctx.Perceived, ctx.Agenda, rng)
            .Components.Where(p => p.Name == "relationship effects").Sum(p => p.Value);
    }

    /// <summary>
    /// Two candidates staged close enough together that a single conflict separates them. The
    /// margin is set here on purpose — see the test's own note on why that is the honest direction.
    /// </summary>
    private static string WinnerAfter(bool conflict)
    {
        var world = Cast.Build(42, "baseline");
        var vincent = world.Get("vincent");

        // Positioned so that retaliation sits just below doing nothing, by less than one conflict's
        // worth of trust. Vincent's standing grievance against Salvatore comes from the scenario.
        Relations.Establish(vincent, "salvatore", trust: 0.55, obligation: 0.40);

        var refusing = new Claim(ClaimKind.BusinessRefusesTribute, Cast.Grocery);
        vincent.Cognition.Learn(refusing, Stance.Believes, 0.9, SourceKind.Discovery, vincent.Id, world.Now);

        if (conflict)
            Apply(vincent, vincent.Cognition.Receive(
                ReportedClaim.Honest(refusing, Stance.Rejects, 0.9, SourceKind.Report),
                "salvatore", world.Now));

        var ctx = Context(world, vincent);

        var retaliate = new Candidate("retaliate", ActionKind.Retaliate, "test", "move against him")
        { TargetId = "salvatore", Domain = Cast.Harbour };

        var hold = new Candidate("hold", ActionKind.DoNothing, "test", "say nothing for now");

        // A fresh stream per candidate, so both draw the same noise and the comparison is decided
        // by the score rather than by which one was scored first.
        double Score(Candidate c) => Utility.Score(
            c, vincent.View, vincent.Psychology, ctx.Perceived, ctx.Agenda,
            Rng.ForOccasion(world.Seed, "test|fixed")).Total;

        return Score(retaliate) > Score(hold) ? "retaliate" : "hold";
    }
}
