using CrimeSim.Domain;
using CrimeSim.Scenario;
using CrimeSim.Sim;
using CrimeSim.Trace;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>
/// The information loop from INFORMATION_AND_LEGIBILITY.md: direct observation, one report
/// channel, a report that holds something back, a source that contradicts, and a player-facing
/// account limited to one character's information.
///
/// The load-bearing test here is <see cref="Player_view_never_shows_what_the_viewpoint_character_does_not_hold"/>.
/// The rest can be read off a trace by eye; a leak cannot, because a leaked fact looks exactly
/// like a fact the player was entitled to.
/// </summary>
public sealed class InformationTransmissionTests
{
    private const string Viewpoint = "salvatore";

    /// <summary>
    /// Every claim anyone holds that the viewpoint character does not hold must be absent from
    /// his view — checked against the renderer's own wording, not a copy of it.
    ///
    /// This is the executable form of the doc's success criterion that sources never communicate
    /// facts unavailable to them. It runs across every variant because a leak that only appears
    /// in one configuration is still a leak.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("cautious-vincent")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    public void Player_view_never_shows_what_the_viewpoint_character_does_not_hold(string variant)
    {
        var world = Run(variant);
        var salvatore = world.Get(Viewpoint);
        string view = IntelligenceWriter.Render(world, Viewpoint);

        string Name(string id) =>
            world.Find(id)?.Name ?? world.Businesses.GetValueOrDefault(id)?.Name ?? id;

        // Everything anyone in the world holds, plus everything ever asserted to anyone.
        var everyClaim = world.Characters.Values
            .SelectMany(c => c.Cognition.Records.Select(r => r.Claim))
            .Concat(world.Reports.SelectMany(r => r.Asserted.Select(a => a.Claim)))
            .Concat(world.Reports.SelectMany(r => r.Withheld))
            .Distinct()
            .ToList();

        Assert.NotEmpty(everyClaim);

        foreach (var claim in everyClaim)
        {
            if (salvatore.Cognition.Find(claim) is { IsHeld: true }) continue;

            string wording = IntelligenceWriter.Describe(claim, Name);
            Assert.False(
                view.Contains(wording, StringComparison.Ordinal),
                $"[{variant}] the player view states \"{wording}\", which {salvatore.Name} does not hold");
        }
    }

    /// <summary>
    /// The view must not carry the developer-only side of the record.
    ///
    /// Note what is deliberately *not* asserted here: that the view avoids the wording of truth-log
    /// entries. It legitimately overlaps, because a fact the viewpoint character genuinely observed
    /// reads much the same either way — "Tommy Nardo put hands on Bellini's grocery" is both what
    /// happened and what Salvatore saw. Whether he was entitled to it is
    /// <see cref="Player_view_never_shows_what_the_viewpoint_character_does_not_hold"/>'s job.
    /// What this test guards is the material that is hidden regardless of entitlement: a report's
    /// framing states whether its sender was being straight, which is the one thing the player
    /// must work out rather than be told.
    /// </summary>
    [Fact]
    public void Player_view_does_not_carry_developer_only_material()
    {
        var world = Run("disloyal-vincent");
        string view = IntelligenceWriter.Render(world, Viewpoint);

        Assert.NotEmpty(world.Reports);
        foreach (var report in world.Reports)
        {
            Assert.DoesNotContain(report.Framing, view, StringComparison.Ordinal);
            Assert.DoesNotContain(report.Candor.ToString(), view, StringComparison.Ordinal);
        }

        // Utility components and raw scores are the hidden state the doc names explicitly.
        Assert.DoesNotContain("utility", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("perceived goal progress", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("self-protection", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("withheld", view, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// A report carries only what its sender holds. Anything withheld is genuinely absent from
    /// what the recipient was told, rather than passed along at lower confidence.
    /// </summary>
    [Fact]
    public void A_report_conveys_only_what_its_sender_holds_and_omits_what_it_withholds()
    {
        var world = Run("baseline");
        Assert.NotEmpty(world.Reports);

        foreach (var report in world.Reports)
        {
            var sender = world.Get(report.SenderId);

            foreach (var withheld in report.Withheld)
            {
                // He kept back something he actually had — otherwise "withheld" is meaningless.
                Assert.True(
                    sender.Cognition.Find(withheld) is not null,
                    $"{report.SenderId} withheld {withheld}, which he never held");

                // And it was not smuggled through as an affirmation anyway.
                Assert.DoesNotContain(
                    report.Asserted,
                    a => a.Claim.Equals(withheld) && a.AssertedStance is Stance.Knows or Stance.Believes or Stance.Suspects);
            }
        }

        Assert.Contains(world.Reports, r => r.Withheld.Count > 0);
    }

    /// <summary>
    /// The deception actually costs the recipient the truth: the world log records the breach,
    /// and the boss does not end up holding it.
    /// </summary>
    [Fact]
    public void An_incomplete_report_leaves_the_recipient_without_what_was_withheld()
    {
        var world = Run("baseline");
        var salvatore = world.Get(Viewpoint);

        Assert.Contains(world.TruthLog, e => e.Kind == "violence");

        var concealed = world.Reports
            .Where(r => r.RecipientId == Viewpoint && r.Withheld.Count > 0)
            .SelectMany(r => r.Withheld)
            .ToList();

        Assert.NotEmpty(concealed);

        // At least one thing kept from him is something he never got from any other route either.
        Assert.Contains(concealed, c => salvatore.Cognition.Find(c) is null or { IsHeld: false });
    }

    /// <summary>
    /// Candour records what the sender was trying to do, not whether he happened to be right.
    ///
    /// A denial asserts the opposite of what the sender holds; an omission asserts nothing untrue
    /// and simply says less. Keeping those distinguishable in the developer record is the doc's
    /// open question about telling lies apart from sincere false belief.
    /// </summary>
    [Fact]
    public void Candour_distinguishes_lying_from_merely_leaving_things_out()
    {
        var world = Run("disloyal-vincent");

        var denial = world.Reports.FirstOrDefault(r => r.Candor == ReportCandor.False);
        Assert.NotNull(denial);

        var liar = world.Get(denial!.SenderId);
        foreach (var withheld in denial.Withheld)
        {
            // He holds it, and told the other man the opposite. That gap is what makes it a lie.
            Assert.True(liar.Cognition.Find(withheld) is { IsHeld: true });
            Assert.Contains(denial.Asserted, a => a.Claim.Equals(withheld) && a.AssertedStance == Stance.Rejects);
        }

        // An omission says less; it never says the opposite. This is the whole difference between
        // the two, and it is checkable from the record alone — unlike "everything he asserted he
        // still believes", which fails honestly, because beliefs move on after the report is sent.
        foreach (var partial in world.Reports.Where(r => r.Candor == ReportCandor.Partial))
        {
            Assert.DoesNotContain(partial.Asserted, a => a.AssertedStance == Stance.Rejects);
            Assert.NotEmpty(partial.Withheld);
        }
    }

    /// <summary>
    /// A source contradicting him leaves the disagreement standing and attributable, rather than
    /// one account quietly overwriting the other.
    /// </summary>
    [Fact]
    public void A_contradicting_source_leaves_a_conflict_that_is_still_attributable()
    {
        var world = Run("disloyal-vincent");
        var salvatore = world.Get(Viewpoint);

        var contested = salvatore.Cognition.Testimony
            .Select(t => t.Claim)
            .Distinct()
            .Where(salvatore.Cognition.IsContested)
            .ToList();

        Assert.NotEmpty(contested);

        foreach (var claim in contested)
        {
            var accounts = salvatore.Cognition.AccountsOf(claim).ToList();
            Assert.NotEmpty(accounts);

            // Every account still names who gave it. A conflict nobody can be held to is not
            // usable evidence about anything.
            Assert.All(accounts, a => Assert.False(string.IsNullOrEmpty(a.SenderId)));

            var own = salvatore.Cognition.Find(claim);
            bool disagreesWithHim = own is not null && accounts.Any(a => a.Affirms != own.IsHeld);
            bool sourcesDisagree = accounts.Any(a => a.Affirms) && accounts.Any(a => !a.Affirms);
            Assert.True(disagreesWithHim || sourcesDisagree);
        }

        // And the player is shown it as a disagreement, not handed a resolution.
        string view = IntelligenceWriter.Render(world, Viewpoint);
        Assert.Contains("ACCOUNTS THAT DO NOT AGREE", view, StringComparison.Ordinal);
        Assert.Contains("contradicted", view, StringComparison.Ordinal);
    }

    /// <summary>
    /// Being told something does not overwrite what he saw. A character who could be talked out of
    /// his own eyes by one assertion would make deception free.
    /// </summary>
    [Fact]
    public void Being_contradicted_shakes_a_directly_observed_belief_without_erasing_it()
    {
        var cognition = new Cognition();
        var claim = new Claim(ClaimKind.PersonUsedViolence, "vincent", "shop", 1);
        var at = new DateTime(1987, 3, 2, 0, 0, 0, DateTimeKind.Utc);

        cognition.Learn(claim, Stance.Knows, 1.0, SourceKind.Direct, "self", at);
        cognition.Receive(new ReportedClaim(claim, Stance.Rejects, 0.8), "vincent", at.AddDays(1));

        var after = cognition.Find(claim);
        Assert.NotNull(after);
        Assert.True(after!.IsHeld, "a single denial should not overturn direct observation");
        Assert.True(after.Confidence < 1.0, "but it should cost him some certainty");
        Assert.True(cognition.IsContested(claim));

        // The acquisition time is when he saw it, not when he was argued with about it.
        Assert.Equal(at, after.AcquiredAt);
        Assert.Equal(at.AddDays(1), after.ReconsideredAt);
    }

    /// <summary>Two independent voices saying the same thing are worth more than one repeating.</summary>
    [Fact]
    public void Corroboration_counts_only_when_it_comes_from_someone_new()
    {
        var claim = new Claim(ClaimKind.BusinessRefusesTribute, "shop");
        var at = new DateTime(1987, 3, 2, 0, 0, 0, DateTimeKind.Utc);

        var repeated = new Cognition();
        repeated.Receive(new ReportedClaim(claim, Stance.Believes, 0.5), "tommy", at);
        repeated.Receive(new ReportedClaim(claim, Stance.Believes, 0.5), "tommy", at.AddDays(1));

        var independent = new Cognition();
        independent.Receive(new ReportedClaim(claim, Stance.Believes, 0.5), "tommy", at);
        independent.Receive(new ReportedClaim(claim, Stance.Believes, 0.5), "vincent", at.AddDays(1));

        Assert.True(
            independent.ConfidenceIn(claim) > repeated.ConfidenceIn(claim),
            "a second source should be worth more than the first source saying it twice");
    }

    private static World Run(string variant)
    {
        var world = Cast.Build(seed: 42, variant);
        Runner.Run(world, Cast.Start.AddDays(90));
        return world;
    }
}
