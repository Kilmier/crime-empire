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

    // ---------------------------------------------------------------- the state machine
    //
    // Cognition changes are reviewed as state-machine changes, not list updates, and the transitions
    // below are the ones the ledger's checklist names. Each asserts both halves — how many conflicts
    // were emitted and where trust ended up — because the two can disagree: an emission that applied
    // no consequence and a consequence applied without an emission are different defects, and a test
    // that checked only one would miss the other.

    /// <summary>
    /// Recantation. A man tells the listener something the listener had no position on, and then
    /// takes it back.
    ///
    /// The first move is not a conflict — there was nothing to contradict. The second is, and that
    /// is the point worth pinning: by then the listener holds the claim, and it makes no difference
    /// that he holds it on this same man's earlier say-so. Being told the opposite of your current
    /// position is a conflict whoever put you there, which is what keeps the rule about the
    /// listener's state rather than about the speaker's history.
    ///
    /// An earlier version of this test was named and documented as though neither move counted,
    /// while its body asserted exactly what the body below asserts. The implementation was right and
    /// the description was wrong.
    /// </summary>
    [Fact]
    public void A_source_taking_back_what_he_said_contradicts_the_belief_he_created()
    {
        var listener = Character("salvatore");
        Relations.Establish(listener, "tommy", trust: 0.80);

        var conflicts = new List<AccountConflict>();
        Receive(listener, conflicts, Affirm(0.8), "tommy", At);          // news — nothing to contradict
        Receive(listener, conflicts, Denial(0.9), "tommy", At.AddDays(1)); // takes it back — a conflict

        var single = Assert.Single(conflicts);
        Assert.Equal(Stance.Rejects, single.AssertedStance);

        // And the prior it names is the one his own earlier account created.
        Assert.Equal(SourceKind.FirstHandTestimony, single.PriorSourceKind);
        Assert.Equal("tommy", single.PriorSourceId);

        Assert.Equal(0.80 - Relations.ConflictTrustCost * single.Strength,
            listener.Social.Toward("tommy").Trust, 9);
    }

    /// <summary>
    /// Affirm → deny → affirm from one source. Milestone 003 had to repair this comparison twice, so
    /// it gets its own accounting here: each genuine reversal is one conflict, and the man coming
    /// back round is not a free pass — coming back is itself a reversal of the denial the listener
    /// had by then adopted, if he adopted it.
    /// </summary>
    [Fact]
    public void Affirm_deny_affirm_emits_one_conflict_per_genuine_reversal()
    {
        var listener = Character("salvatore");
        Relations.Establish(listener, "tommy", trust: 0.90);
        listener.Cognition.Learn(Beating, Stance.Believes, 0.7, SourceKind.Discovery, listener.Id, At);

        var conflicts = new List<AccountConflict>();
        Receive(listener, conflicts, Affirm(0.8), "tommy", At.AddDays(1));   // agrees — not a conflict
        Receive(listener, conflicts, Denial(0.9), "tommy", At.AddDays(2));   // reverses — a conflict
        Receive(listener, conflicts, Affirm(0.8), "tommy", At.AddDays(3));   // comes back round

        // **One**, and the reason is the point of the test. A firmly held position eroded by the
        // denial without being displaced by it, so the listener still held the claim when Tommy came
        // back round — and a man agreeing with what you already think is not contradicting you,
        // however much he has been swinging about. The companion test below covers the case where
        // the denial does displace, and there the return trip *is* a second conflict.
        var single = Assert.Single(conflicts);
        Assert.Equal("tommy", single.SpeakerId);
        Assert.Equal(Stance.Rejects, single.AssertedStance);

        Assert.Equal(0.90 - Relations.ConflictTrustCost * single.Strength,
            listener.Social.Toward("tommy").Trust, 9);

        // The belief survived, and the disagreement stays on the record regardless.
        Assert.True(listener.Cognition.Holds(Beating));
        Assert.True(listener.Cognition.IsContested(Beating));
    }

    /// <summary>
    /// The same sequence against a position weak enough that the denial displaces it. Now the
    /// listener has followed Tommy off the claim, so Tommy coming back round contradicts what the
    /// listener holds by then — two reversals, two conflicts, and trust paying for both.
    /// </summary>
    [Fact]
    public void When_the_denial_displaces_the_belief_coming_back_round_is_a_second_conflict()
    {
        var listener = Character("salvatore");
        Relations.Establish(listener, "tommy", trust: 0.90);
        listener.Cognition.Learn(Beating, Stance.Believes, 0.4, SourceKind.Report, "someone", At);

        var conflicts = new List<AccountConflict>();
        Receive(listener, conflicts, Denial(0.9), "tommy", At.AddDays(1));
        Assert.False(listener.Cognition.Holds(Beating));      // the denial took

        Receive(listener, conflicts, Affirm(0.8), "tommy", At.AddDays(2));

        Assert.Equal(2, conflicts.Count);
        Assert.Equal(Stance.Rejects, conflicts[0].AssertedStance);
        Assert.Equal(Stance.Believes, conflicts[1].AssertedStance);

        double expected = 0.90;
        foreach (var c in conflicts) expected -= Relations.ConflictTrustCost * c.Strength;
        Assert.Equal(Math.Max(0, expected), listener.Social.Toward("tommy").Trust, 9);
    }

    /// <summary>
    /// The same sequence with the repeats interleaved: only the moves that change direction count,
    /// and restating a position already given costs nothing further.
    /// </summary>
    [Fact]
    public void Restating_a_position_between_reversals_adds_no_conflict()
    {
        var listener = Character("salvatore");
        Relations.Establish(listener, "tommy", trust: 0.90);
        listener.Cognition.Learn(Beating, Stance.Believes, 0.7, SourceKind.Discovery, listener.Id, At);

        var conflicts = new List<AccountConflict>();
        Receive(listener, conflicts, Denial(0.9), "tommy", At.AddDays(1));
        Receive(listener, conflicts, Denial(0.9), "tommy", At.AddDays(2));
        Receive(listener, conflicts, Denial(0.9), "tommy", At.AddDays(3));
        double afterRepeats = listener.Social.Toward("tommy").Trust;

        Receive(listener, conflicts, Affirm(0.8), "tommy", At.AddDays(4));

        Assert.Equal(1, conflicts.Count(c => c.AssertedStance == Stance.Rejects));
        Assert.Equal(0.90 - Relations.ConflictTrustCost * conflicts[0].Strength, afterRepeats, 9);
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
    [InlineData("baseline", 2)]
    [InlineData("cautious-vincent", 3)]
    [InlineData("watchful-boss", 2)]
    [InlineData("disloyal-vincent", 2)]
    [InlineData("resentful-tommy", 2)]
    public void The_scenario_produces_the_expected_number_of_conflicts(string variant, int expected)
        => Assert.Equal(expected, Run(variant).AccountConflicts.Count);

    /// <summary>
    /// And the trust movement those conflicts caused is real, not merely recorded.
    ///
    /// <b>The direction changed in milestone 007, and the change is the point rather than an
    /// accident.</b> This test previously asserted that Salvatore ends below his starting 0.50 toward
    /// Vincent, which held because Vincent filed four concealing reports and his rejection of
    /// <c>BusinessRefusesTribute</c> reached the page on the second. He no longer files that second
    /// report: it existed only because withholding the same claim was being paid for afresh every
    /// time. So the boss is no longer contradicted, and the capo is — twice, by assignment briefings
    /// that re-assert a claim he has personally watched become false.
    ///
    /// Vincent is also the character who has decisions that read the relationship, which is what the
    /// milestone was for. The staged boss-side cases above (<c>A_denial_of_something_he_holds_is_a_conflict</c>,
    /// <c>Only_the_listener_relationship_moves</c>, and the delegation and assignment path tests)
    /// keep that direction covered at unit level, so retargeting this one loses no rule.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("cautious-vincent")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    [InlineData("resentful-tommy")]
    public void The_capo_trusts_his_boss_less_after_being_contradicted(string variant)
    {
        var world = Run(variant);
        var vincent = world.Get("vincent");

        double started = variant switch
        {
            "watchful-boss" => 0.70,
            "disloyal-vincent" => 0.05,
            _ => 0.45,
        };

        Assert.Contains(world.AccountConflicts,
            c => c.ListenerId == "vincent" && c.Conflict.SpeakerId == "salvatore");
        Assert.True(vincent.Social.Toward("salvatore").Trust < started,
            $"[{variant}] vincent's trust in salvatore is " +
            $"{vincent.Social.Toward("salvatore").Trust:0.000}, not below its starting {started:0.00}");
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

        Assert.DoesNotContain(iface.GetMethods(), m => !m.IsSpecialName);

        // No publicly visible implementation of it exists anywhere in the simulation assembly.
        var publicImpls = typeof(Relations).Assembly.GetExportedTypes()
            .Where(t => iface.IsAssignableFrom(t) && t != iface)
            .ToList();

        Assert.Empty(publicImpls);
    }

    /// <summary>
    /// `IReadOnlyList&lt;T&gt;` is an interface, not a guarantee. Returning the backing list as one
    /// let any caller cast it straight back and add to it, so the read-only surface was read-only by
    /// politeness — the exact bypass this asserts is now closed.
    /// </summary>
    [Fact]
    public void The_grievance_collection_cannot_be_cast_back_to_something_mutable()
    {
        var c = Character("vincent");
        Relations.RaiseGrievance(c, new Grievance("tommy", "one", 0.3, At));

        var exposed = c.Social.Toward("tommy").Grievances;

        // The backing list must not be reachable by a cast. Note that the wrapper does still
        // implement IList<Grievance> — that is unavoidable and harmless, because every mutating
        // member on it throws rather than quietly succeeding, which is the property that matters.
        Assert.Null(exposed as List<Grievance>);
        var asList = Assert.IsAssignableFrom<IList<Grievance>>(exposed);
        Assert.Throws<NotSupportedException>(() => asList.Add(new Grievance("tommy", "smuggled in", 9.0, At)));
        Assert.Throws<NotSupportedException>(() => asList.Clear());
        Assert.Throws<NotSupportedException>(() => asList[0] = new Grievance("tommy", "swapped", 9.0, At));
        Assert.Single(c.Social.Toward("tommy").Grievances);
        Assert.Equal(0.3, c.Social.GrievanceAgainst("tommy"), 9);
    }

    /// <summary>
    /// The absent reading is handed to every caller who asks about somebody unknown. If one of them
    /// could write to it, the contamination would be invisible and would follow the character
    /// around — so the mutation guard refuses it outright rather than silently discarding the write.
    /// </summary>
    [Fact]
    public void The_absent_relationship_reading_cannot_be_contaminated()
    {
        var c = Character("vincent");
        var absent = c.Social.Toward("a-stranger");

        Assert.Null(absent.Grievances as List<Grievance>);
        Assert.Throws<NotSupportedException>(
            () => ((IList<Grievance>)absent.Grievances).Add(new Grievance("a-stranger", "x", 1.0, At)));

        // And a later absent read is unaffected by anything attempted against an earlier one.
        Assert.Empty(c.Social.Toward("a-stranger").Grievances);
        Assert.Equal(0, c.Social.Toward("a-stranger").Trust);
        Assert.Empty(c.Social.Others);
    }

    /// <summary>
    /// Finding 2: an absent reading must still say who it is about. A shared sentinel reported
    /// `OtherId = ""`, so every absent read claimed to be about nobody — wrong in itself and
    /// misleading to anything that logged or grouped by it.
    /// </summary>
    [Fact]
    public void An_absent_reading_names_the_person_it_was_asked_about()
    {
        var c = Character("vincent");

        Assert.Equal("a-stranger", c.Social.Toward("a-stranger").OtherId);
        Assert.Equal("someone-else", c.Social.Toward("someone-else").OtherId);
        Assert.Empty(c.Social.Others);
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

    // ---------------------------------------------------------------- the delegator's account path

    /// <summary>
    /// The delegator's question exists and reaches the man he sent.
    ///
    /// Generated from <see cref="Generators"/> itself rather than hand-built, because the thing
    /// under test is that the option occurs to him at all — before this generator existed, the only
    /// character who ever put a question was whoever happened to hold a shaky second-hand belief,
    /// and a delegator holding first-hand traces of his own man's work was specifically excluded.
    /// </summary>
    [Fact]
    public void A_delegator_can_put_it_to_the_man_he_sent()
    {
        var (world, vincent, _) = Delegated();

        var candidates = Generators.GenerateAll(Context(world, vincent));

        var ask = Assert.Single(candidates, c =>
            c.Kind == ActionKind.SeekCorroboration && c.TargetId == "tommy"
            && c.AboutClaim is { } a && a.Equals(Beating));

        Assert.Equal("FromDelegation", ask.Generator);
    }

    /// <summary>
    /// The standing to ask outlives the operation. The first version of this generator read the live
    /// strategy's DelegatedToId, so the question existed only while the work was running — which is
    /// exactly the window in which a man is too busy to ask, and it had evaporated by the time he
    /// was free.
    /// </summary>
    [Fact]
    public void The_standing_to_ask_survives_the_strategy_finishing()
    {
        var (world, vincent, _) = Delegated();
        vincent.Execution.Strategy = null;

        var candidates = Generators.GenerateAll(Context(world, vincent));

        Assert.Contains(candidates, c =>
            c.Kind == ActionKind.SeekCorroboration && c.TargetId == "tommy");
    }

    /// <summary>
    /// **The end-to-end path, and the proof finding 3 asks for.** An executor contradicts his
    /// delegator and the delegator's trust in him falls — through production code the whole way:
    /// the generator offers the question, `Commit` records the request and schedules the executor's
    /// wake, `Runner` delivers it, the executor's own `Pipeline` deliberation picks what to say,
    /// `Reporting` composes and delivers it, `Cognition.Receive` recognises the contradiction, and
    /// `Relations` applies the consequence. Nothing here hand-builds a conflict.
    ///
    /// One thing is staged and it is not a coefficient: this Tommy does not believe anybody saw him.
    /// In the accepted scenario he does — `ResolveViolence` leaves him inferring witnesses — and
    /// <see cref="Utility"/> prices a denial almost entirely on that belief, so he conceals instead
    /// of denying and no contradiction reaches Vincent. That is the model working: a man who thinks
    /// the street watched him do it does not tell his capo it never happened. Removing that belief
    /// here is setting up the case where a denial is the rational move, not tuning one into
    /// existence — and the denial still has to win its own utility competition, which is asserted
    /// below rather than assumed.
    /// </summary>
    [Fact]
    public void An_executor_who_denies_it_costs_himself_his_delegators_trust()
    {
        var (world, vincent, tommy) = Delegated();

        // He has no reason to think he was seen, so a denial is not priced out of the running.
        Assert.False(tommy.Cognition.Holds(new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, "tommy")));

        double before = vincent.Social.Toward("tommy").Trust;
        Assert.True(before > 0, "the fixture needs a real relationship for the movement to be visible");

        // Vincent puts the question — the only forced step, and it is the choice to ask, not what
        // comes back. Everything downstream is the simulation's.
        var ask = Generators.GenerateAll(Context(world, vincent))
            .Single(c => c.Kind == ActionKind.SeekCorroboration && c.TargetId == "tommy");
        var ctx = Context(world, vincent);
        Commit.Apply(world, vincent, ask, ctx.Agenda, ctx, new List<string>());

        Runner.Run(world, world.Now.AddDays(5));

        // Tommy decided for himself to deny it, and it reached Vincent as a denial.
        var denial = Assert.Single(world.Reports,
            r => r.SenderId == "tommy" && r.RecipientId == "vincent" && r.Candor == ReportCandor.False);
        Assert.Contains(denial.Asserted, a => a.Claim.Equals(Beating) && a.AssertedStance == Stance.Rejects);

        // The delegator registered it as a conflict and it cost the executor his trust.
        var conflict = Assert.Single(world.AccountConflicts,
            c => c.ListenerId == "vincent" && c.Conflict.SpeakerId == "tommy");
        Assert.Equal(Beating, conflict.Conflict.Claim);
        Assert.True(vincent.Social.Toward("tommy").Trust < before);

        // Directional, still: Tommy's own view of Vincent is untouched by having lied to him.
        Assert.Equal(0.10, tommy.Social.Toward("vincent").Trust, 9);
    }

    // ---------------------------------------------------------------- the question is scored on its subject

    /// <summary>
    /// The uncertainty behind a question is the uncertainty about the thing being asked, and nothing
    /// else. The scorer used to scan every testimonial belief the actor held and price the question
    /// off the weakest one, so an unrelated thin rumour sitting in his head made him keen to ask
    /// about something he was perfectly confident of.
    /// </summary>
    [Fact]
    public void An_unrelated_weak_testimony_does_not_change_the_delegator_question_score()
    {
        double without = DelegatorQuestionScore(unrelatedTestimonyConfidence: null);
        double withThinRumour = DelegatorQuestionScore(unrelatedTestimonyConfidence: 0.05);
        double withFirmRumour = DelegatorQuestionScore(unrelatedTestimonyConfidence: 0.95);

        Assert.Equal(without, withThinRumour, 9);
        Assert.Equal(without, withFirmRumour, 9);
    }

    /// <summary>The confidence that *does* move it is the one attached to the claim being asked about.</summary>
    [Fact]
    public void The_confidence_of_the_asked_claim_moves_the_question_score()
    {
        double thin = DelegatorQuestionScore(unrelatedTestimonyConfidence: null, askedConfidence: 0.20);
        double solid = DelegatorQuestionScore(unrelatedTestimonyConfidence: null, askedConfidence: 0.90);

        Assert.True(thin > solid,
            $"a question about something he is unsure of should be worth more: {thin} was not above {solid}");
        Assert.Equal(1.5 * (0.90 - 0.20), thin - solid, 9);
    }

    /// <summary>
    /// And the trace has to say something true. Asking the man named in a claim you found yourself
    /// is not going behind anybody, so that explanation must not appear — while asking a third party
    /// about something you were told is, and must.
    /// </summary>
    [Fact]
    public void The_trace_only_claims_he_is_going_behind_somebody_when_there_is_somebody_to_go_behind()
    {
        var selfAcquired = DelegatorQuestionBreakdown(SourceKind.Discovery, sourceId: "vincent");
        Assert.DoesNotContain(selfAcquired.Components, c => c.Explanation.Contains("going behind"));
        Assert.Contains(selfAcquired.Components, c => c.Name == "uncertainty");

        var wasTold = DelegatorQuestionBreakdown(SourceKind.Report, sourceId: "salvatore");
        var behind = Assert.Single(wasTold.Components, c => c.Explanation.Contains("going behind"));
        Assert.Contains("salvatore", behind.Explanation);

        // Nor when the man he would be going behind is the man he is asking — putting it back to the
        // person who told you is not going around him.
        var askingTheSource = DelegatorQuestionBreakdown(SourceKind.FirstHandTestimony, sourceId: "tommy");
        Assert.DoesNotContain(askingTheSource.Components, c => c.Explanation.Contains("going behind"));
    }

    /// <summary>
    /// Finding 3's audit. The two generators can propose the same question, and when they do it is
    /// one act: the same target, the same claim, offered twice with different wording would spend
    /// two of a bounded six slots saying the same thing.
    /// </summary>
    [Fact]
    public void The_same_question_from_two_generators_is_offered_once()
    {
        // Make the two questions coincide: the claim about Tommy that Vincent would audit is also
        // the shakiest thing anybody has told him, so both generators reach for it.
        var (world, vincent, _) = Delegated(
            vincentConfidence: 0.10, vincentBasis: SourceKind.Report, vincentSourceId: "salvatore");

        var candidates = Generators.GenerateAll(Context(world, vincent));

        var asking = candidates
            .Where(c => c.Kind == ActionKind.SeekCorroboration
                        && c.TargetId == "tommy"
                        && c.AboutClaim is { } a && a.Equals(Beating))
            .ToList();

        Assert.Single(asking);

        // Kept deterministically, and both generators still exist for the cases that do differ.
        Assert.Equal("FromRelationship", asking[0].Generator);
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

    private static ReportedClaim Affirm(double confidence)
        => ReportedClaim.Honest(Beating, Stance.Believes, confidence, SourceKind.Participant);

    /// <summary>Delivers an account and applies whatever consequence it carried, recording the conflict.</summary>
    private static void Receive(
        Character listener, List<AccountConflict> log, ReportedClaim said, string senderId, DateTime at)
    {
        var receipt = listener.Cognition.Receive(said, senderId, at);
        if (receipt.Conflict is not { } conflict) return;
        log.Add(conflict);
        Relations.RecordAccountConflict(listener, conflict);
    }

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

    /// <summary>
    /// A world in which Vincent has delegated the shakedown to Tommy, Tommy has done it, and Vincent
    /// has since come across the traces himself. The state the accepted scenario actually reaches by
    /// late March, staged directly so the exchange under test does not depend on fifty days of
    /// unrelated causation landing the same way.
    /// </summary>
    /// <summary>
    /// As <see cref="Delegated()"/>, but with the delegator's record of the beating set exactly as
    /// asked.
    ///
    /// Parameterised rather than overwritten afterwards, because <see cref="Cognition.Learn"/>
    /// deliberately refuses to lower an existing confidence unless the new basis overrides — so a
    /// test that seeded 0.6 and then "set" 0.2 silently kept 0.6 and asserted against a fixture it
    /// did not have. Two of these tests were written that way and passed for the wrong reason until
    /// the numbers were checked.
    /// </summary>
    private static (World World, Character Vincent, Character Tommy) Delegated(
        double vincentConfidence, SourceKind vincentBasis, string vincentSourceId = "vincent")
    {
        var staged = Delegated(seedVincentBelief: false);
        staged.Vincent.Cognition.Learn(
            Beating, Stance.Believes, vincentConfidence, vincentBasis, vincentSourceId, staged.World.Now);
        return staged;
    }

    private static (World World, Character Vincent, Character Tommy) Delegated(
        bool seedVincentBelief = true)
    {
        var world = Cast.Build(42, "resentful-tommy");
        var vincent = world.Get("vincent");
        var tommy = world.Get("tommy");

        vincent.Execution.Strategy = new StrategyInstance
        {
            OwnerId = vincent.Id,
            LocalSequence = vincent.StrategyCount++,
            Kind = StrategyKind.SecureTribute,
            Domain = Cast.Harbour,
            TargetId = Cast.Grocery,
            Method = CoercionMethod.Force,
            StartedAt = world.Now,
            Deadline = world.Now.AddDays(30),
            DelegatedToId = tommy.Id,
        };
        vincent.Execution.RecordDelegation(tommy.Id);

        // He did it, and he knows he did. She found the traces afterwards, which is what Discovery
        // means and why it does not put her at the scene.
        tommy.Cognition.Learn(Beating, Stance.Knows, 1.0, SourceKind.Participant, tommy.Id, world.Now);
        if (seedVincentBelief)
            vincent.Cognition.Learn(Beating, Stance.Believes, 0.6, SourceKind.Discovery, vincent.Id, world.Now);

        return (world, vincent, tommy);
    }

    /// <summary>
    /// Scores the delegator's question about the beating, optionally with an unrelated testimonial
    /// belief of a given confidence also in the actor's head. Everything but the named variable is
    /// held identical, including the noise stream.
    /// </summary>
    private static double DelegatorQuestionScore(
        double? unrelatedTestimonyConfidence, double askedConfidence = 0.60)
        => DelegatorQuestionBreakdown(
            SourceKind.Discovery, "vincent", askedConfidence, unrelatedTestimonyConfidence).Total;

    private static ScoreBreakdown DelegatorQuestionBreakdown(
        SourceKind askedBasis,
        string sourceId,
        double askedConfidence = 0.60,
        double? unrelatedTestimonyConfidence = null)
    {
        var (world, vincent, _) = Delegated(askedConfidence, askedBasis, sourceId);

        if (unrelatedTestimonyConfidence is { } c)
            vincent.Cognition.Learn(
                new Claim(ClaimKind.PoliceInvestigating, "kane"),
                Stance.Believes, c, SourceKind.Report, "salvatore", world.Now);

        var ask = new Candidate("ask", ActionKind.SeekCorroboration, "test", "put it to him")
        { TargetId = "tommy", Domain = Cast.Harbour, AboutClaim = Beating };

        var ctx = Context(world, vincent);
        return Utility.Score(ask, vincent.View, vincent.Psychology, ctx.Perceived, ctx.Agenda,
            Rng.ForOccasion(world.Seed, "test|fixed"));
    }

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
