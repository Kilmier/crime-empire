using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Scenario;
using CrimeSim.Sim;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>
/// Milestone 008 — relationship readers and the executable schema.
///
/// The milestone's question was why a relationship movement was worth so little at the decision that
/// read it: the coefficients, or the shape. The answer turned out to be the shape, in two places, and
/// these tests pin both along with the instrument that found them.
///
/// <b>One.</b> Grievance was subtracted inside the clamp that produced <c>Loyalty</c>, so once the sum
/// floored at zero, further grievance was free and further trust was worthless. Milestone 008 moved it
/// out and made it its own named contribution at every reader, at the same <c>0.50</c> coefficient. No
/// coefficient in this milestone was tuned.
///
/// <b>Two.</b> The only way to ask how much of a score came from a relationship was to filter
/// components by the name "relationship effects", and across the five variants at seed 42 that name
/// covered 168 components of which 61 — 36% — read no relationship state at all. A label is not a
/// derivation. Components now carry the facet they were derived from, set where the value is computed.
/// </summary>
public sealed class RelationshipReaderTests
{
    private static readonly DateTime At = Cast.Start;

    private static World Run(string variant, int seed = 42)
    {
        var world = Cast.Build(seed, variant);
        Runner.Run(world, Cast.Start.AddDays(90));
        return world;
    }

    // ================================================================ the vocabulary is closed

    /// <summary>
    /// Ruling 6: the executable vocabulary is Trust, Fear, Obligation and relationship-keyed
    /// Grievances, and each is retained only because a decision reads it. This is that condition,
    /// asserted rather than argued — the rule that removed <c>Affection</c> in milestone 006, applied
    /// to the whole list.
    ///
    /// Measured across all five variants of a natural run. A dimension that stops being read by
    /// anything fails here, and the honest response is to remove it rather than to invent a reader.
    /// </summary>
    [Fact]
    public void Every_retained_relationship_dimension_is_read_by_some_decision()
    {
        var seen = RelationshipFacet.None;

        foreach (var variant in Variants.All)
            foreach (var d in Run(variant).Decisions)
                foreach (var s in d.Scored)
                    foreach (var c in s.Components)
                        seen |= c.Reads;

        Assert.True(seen.HasFlag(RelationshipFacet.Trust), "no decision read Trust");
        Assert.True(seen.HasFlag(RelationshipFacet.Fear), "no decision read Fear");
        Assert.True(seen.HasFlag(RelationshipFacet.Obligation), "no decision read Obligation");
        Assert.True(seen.HasFlag(RelationshipFacet.Grievance), "no decision read Grievance");
    }

    /// <summary>
    /// The precheck finding, pinned so it cannot come back.
    ///
    /// <c>SeekCorroboration</c>'s "going behind X" carries the name "relationship effects" and is
    /// <c>-0.45 * proud</c>. It reads no relationship state, so it must be tagged <c>None</c> and must
    /// not appear in the relationship channel. Two production tests and this milestone's whole
    /// diagnostic were built to aggregate on that name before this was measured.
    /// </summary>
    [Fact]
    public void Going_behind_somebody_reads_pride_and_not_a_relationship()
    {
        var world = Cast.Build(42, "baseline");
        var vincent = world.Get("vincent");

        // A claim he was told by somebody other than the man he is about to ask.
        var claim = new Claim(ClaimKind.BusinessRefusesTribute, Cast.Grocery);
        vincent.Cognition.Learn(claim, Stance.Believes, 0.5, SourceKind.Report, "salvatore", At);

        var candidate = new Candidate(
            "corroborate:tommy", ActionKind.SeekCorroboration, "test", "ask tommy")
        { TargetId = "tommy", AboutClaim = claim, Domain = Cast.Harbour };

        var breakdown = Score(vincent, candidate, world);

        var goingBehind = breakdown.Components.Single(c => c.Explanation.StartsWith("going behind"));
        Assert.Equal("relationship effects", goingBehind.Name);
        Assert.Equal(RelationshipFacet.None, goingBehind.Reads);
        Assert.Equal(0.0, goingBehind.RelationshipShare, 9);

        // And therefore it is absent from the channel, and the channel is silent on this candidate.
        Assert.DoesNotContain(breakdown.RelationshipComponents(),
            c => c.Explanation.StartsWith("going behind"));
        Assert.Equal(0.0, breakdown.RelationshipNet(), 9);
    }

    // ================================================================ grievance is unbundled

    /// <summary>
    /// Ruling 3. Grievance is out of the clamped loyalty sum and is its own named contribution.
    ///
    /// The test that matters is the saturating case, because that is what the clamp was hiding. A man
    /// whose bond is worth less than his grievance used to read as loyalty exactly zero, with the
    /// remainder of the grievance discarded — so a bitter subordinate and an indifferent one scored
    /// identically, and any further trust he gained was worth nothing until it exceeded the grudge.
    /// </summary>
    [Fact]
    public void Grievance_is_not_clamped_away_against_the_bond()
    {
        var world = Cast.Build(42, "baseline");
        var tommy = world.Get("tommy");

        // Bond worth less than the grievance: under the old rule this clamped to exactly zero.
        Relations.Establish(tommy, "vincent", trust: 0.10, obligation: 0.05);
        Relations.RaiseGrievance(tommy, new Grievance("vincent", "he leaves me holding it", 0.60, At));

        var loyalty = Utility.Loyalty(tommy.View, tommy.Psychology, "vincent");

        // The parts survive separately, which is the whole of ruling 3.
        Assert.Equal(0.10, loyalty.Trust, 9);
        Assert.Equal(0.05, loyalty.Obligation, 9);
        Assert.Equal(tommy.Psychology[Drive.Belonging], loyalty.Belonging, 9);
        Assert.Equal(0.60, loyalty.Grievance, 9);

        // The bond is positive and the grievance is a separate negative offset, not folded in.
        Assert.True(loyalty.Value > 0,
            "the bond floored to zero, which is the clamp this ruling removed");
        Assert.Equal(-0.50 * 0.60, loyalty.GrievanceOffset, 9);

        // The coefficient is preserved exactly. Milestone 008 was forbidden to tune it.
        Assert.Equal(0.50, Utility.LoyaltyReading.GrievanceWeight, 9);
    }

    /// <summary>
    /// And at a reader: two components, separately named and separately tagged, at that reader's own
    /// coefficient. Before this, one number arrived and nothing downstream could take it apart.
    /// </summary>
    [Fact]
    public void A_reader_reports_the_bond_and_the_grievance_as_two_components()
    {
        var world = Cast.Build(42, "baseline");
        var tommy = world.Get("tommy");
        Relations.Establish(tommy, "vincent", trust: 0.10, obligation: 0.05);
        Relations.RaiseGrievance(tommy, new Grievance("vincent", "he leaves me holding it", 0.60, At));

        var candidate = new Candidate("report:vincent", ActionKind.ReportToSuperior, "test", "report in")
        { TargetId = "vincent", Domain = Cast.Harbour };

        var breakdown = Score(tommy, candidate, world);
        var channel = breakdown.RelationshipComponents().ToList();

        var bond = channel.Single(c => c.Reads.HasFlag(RelationshipFacet.Trust));
        var grievance = channel.Single(c => c.Reads == RelationshipFacet.Grievance);

        Assert.NotEqual(bond.Explanation, grievance.Explanation);

        var loyalty = Utility.Loyalty(tommy.View, tommy.Psychology, "vincent");
        Assert.Equal(0.7 * loyalty.Value, bond.Value, 9);
        Assert.Equal(0.7 * loyalty.GrievanceOffset, grievance.Value, 9);

        // The grievance is entirely relational; the bond is not, because Belonging is a drive.
        Assert.Equal(grievance.Value, grievance.RelationshipShare, 9);
        Assert.Equal(0.7 * loyalty.BareValue, bond.RelationshipFreeValue, 9);
    }

    // ================================================================ the two report considerations

    /// <summary>
    /// Ruling 2. A partial report carries two legitimate relationship considerations — the standing
    /// reporting buys, and what shading it costs — and they remain separately identifiable at their
    /// existing coefficients. This is not a defect to be merged; it is two things that were merely
    /// indistinguishable once summed.
    ///
    /// The test also pins the cancellation itself, because that is the finding: gross is materially
    /// larger than net, which is exactly the information the old aggregate destroyed.
    /// </summary>
    [Fact]
    public void A_partial_report_keeps_both_considerations_and_they_largely_cancel()
    {
        var world = Cast.Build(42, "baseline");
        var vincent = world.Get("vincent");

        var suppressed = new Claim(ClaimKind.PersonBreachedPolicy, "vincent", "no-violence-harbour");
        vincent.Cognition.Learn(suppressed, Stance.Knows, 1.0, SourceKind.Participant, "vincent", At);

        var candidate = new Candidate("report:salvatore", ActionKind.ReportToSuperior, "test", "report in")
        {
            TargetId = "salvatore",
            Domain = Cast.Harbour,
            Candor = ReportCandor.Partial,
            Suppressed = new[] { new SuppressedClaim(suppressed, PriorDisclosureState.NeverAddressed) },
        };

        var breakdown = Score(vincent, candidate, world);
        var loyalty = Utility.Loyalty(vincent.View, vincent.Psychology, "salvatore");

        var standing = breakdown.Components.Single(
            c => c.Explanation == "reporting maintains standing with his superior");
        var candorCost = breakdown.Components.Single(
            c => c.Explanation == "it is not what he owes the man");

        // Both coefficients unchanged by milestone 008.
        Assert.Equal(0.7 * loyalty.Value, standing.Value, 9);
        Assert.Equal(-0.5 * loyalty.Value, candorCost.Value, 9);

        // The cancellation the old aggregate hid: they very nearly annihilate.
        Assert.True(breakdown.RelationshipGross() > 2 * Math.Abs(breakdown.RelationshipNet()),
            $"gross {breakdown.RelationshipGross():0.0000} against net " +
            $"{breakdown.RelationshipNet():0.0000} — if these are close the pair has stopped " +
            "cancelling, which would be a real change and needs recording rather than passing");
    }

    /// <summary>
    /// The cutoff must not be applied to the diagnostic.
    ///
    /// On the decision milestone 007's finding was measured on, both halves of that pair are under
    /// <c>Significant()</c>'s 0.15, so the human-readable reason list correctly prints neither and the
    /// channel must still report both. A cutoff that hides a cancelling pair hides the cancellation.
    /// </summary>
    [Fact]
    public void The_diagnostic_reports_components_the_reason_list_drops()
    {
        var world = Run("baseline");
        var vincent = world.Get("vincent");

        var conflict = world.AccountConflicts
            .First(c => c.ListenerId == "vincent" && c.Conflict.SpeakerId == "salvatore");

        var partials = world.Decisions
            .Where(d => d.ActorId == "vincent" && d.At >= conflict.At)
            .SelectMany(d => d.Scored)
            .Where(s => s.Candidate.Kind == ActionKind.ReportToSuperior
                        && s.Candidate.TargetId == "salvatore"
                        && s.Candidate.Candor == ReportCandor.Partial)
            .ToList();

        Assert.NotEmpty(partials);

        // The general property: on every one of these, the channel reports relationship components
        // that the human-readable reason list drops.
        Assert.All(partials, s =>
        {
            var channel = s.RelationshipComponents().ToList();
            Assert.NotEmpty(channel);

            var shown = s.Significant().Where(c => c.Name == "relationship effects").ToList();
            Assert.True(channel.Count > shown.Count,
                $"the reason list showed all {channel.Count} relationship components, so the cutoff " +
                "is currently hiding nothing and this test has stopped covering what it claims");
        });

        // And the sharp case, which is the one milestone 007's central finding was measured on: by
        // the last of them, trust has fallen far enough that every component is under the threshold
        // and the reason list prints no relationship line at all — for the candidate whose
        // relationship contribution was the number being reported.
        var last = partials[^1];
        Assert.All(last.RelationshipComponents(), c => Assert.True(Math.Abs(c.Value) < 0.15,
            $"component \"{c.Explanation}\" is {c.Value:0.0000}; if these have grown past the cutoff " +
            "the finding this test records has changed and needs restating"));
        Assert.DoesNotContain(last.Significant(), c => c.Name == "relationship effects");
        Assert.NotEmpty(last.RelationshipComponents());
    }

    // ================================================================ the counterfactual

    /// <summary>
    /// Ruling 8's counterfactual is like-for-like: it reuses this breakdown's own noise draw.
    ///
    /// Re-scoring with a fresh draw would introduce up to ±0.05 of difference, which is larger than
    /// the effect being measured on every report candidate in the scenario — the measurement would
    /// have been dominated by its own instrument. It also cannot be done by zeroing the relationship,
    /// because milestone 006 deliberately made relationship state unwritable from outside
    /// <c>Relations</c>.
    /// </summary>
    [Fact]
    public void The_counterfactual_is_the_same_score_without_the_relationship_share()
    {
        var world = Cast.Build(42, "baseline");
        var marco = world.Get("marco");
        Relations.Frighten(marco, "tommy", 0.5);

        var candidate = new Candidate("concede:tommy", ActionKind.Concede, "test", "pay up")
        { TargetId = "tommy", Domain = Cast.Harbour };

        var breakdown = Score(marco, candidate, world);

        Assert.Equal(breakdown.Total - breakdown.RelationshipNet(),
                     breakdown.TotalWithoutRelationships(), 9);

        // Fear is wholly relational, so removing the channel removes all of it.
        var fear = breakdown.Components.Single(c => c.Reads == RelationshipFacet.Fear);
        Assert.Equal(1.6 * 0.5, fear.Value, 9);
        Assert.Equal(fear.Value, fear.RelationshipShare, 9);
        Assert.Equal(0.0, fear.RelationshipFreeValue, 9);
    }

    /// <summary>
    /// Ruling 9, measured in both directions and reported as found.
    ///
    /// The channel is load-bearing: in every variant of a natural run, removing relationship state
    /// changes which candidate wins at least once. That is a stronger result than milestone 007's
    /// figure suggested, and the reason is that 007 could only see the trust-to-report path, which is
    /// the one place in the model where two loyalty reads nearly cancel. Fear, obligation and
    /// grievance were never small.
    ///
    /// A zero here would have been the honest result. It is asserted at a floor of one rather than at
    /// a measured count so that the test states the claim — "the channel decides something" — instead
    /// of pinning an arithmetic accident that any future tuning would have to come and edit.
    /// </summary>
    [Fact]
    public void Removing_the_relationship_channel_changes_at_least_one_choice_in_every_variant()
    {
        foreach (var variant in Variants.All)
        {
            var world = Run(variant);

            int decided = world.Decisions.Count(d =>
                d.Scored.Count > 1
                && !ReferenceEquals(
                    d.Scored.OrderByDescending(s => s.TotalWithoutRelationships())
                            .ThenBy(s => s.Candidate.Id, StringComparer.Ordinal).First(),
                    d.Scored[0]));

            Assert.True(decided >= 1,
                $"{variant}: the relationship channel decided no winner at all. That is an honest " +
                "possible result and it needs recording in the archive rather than failing quietly.");
        }
    }

    // ================================================================ helper

    private static ScoreBreakdown Score(Character actor, Candidate candidate, World world)
    {
        var perceived = Salience.Perceive(actor, world.Now);
        var agenda = new Agenda(
            AgendaKind.DischargeResponsibility, "keep the harbour earning", "test", Cast.Harbour);
        var rng = Rng.ForOccasion(world.Seed, "test|fixed");
        return Utility.Score(candidate, actor.View, actor.Psychology, perceived, agenda, rng);
    }
}
