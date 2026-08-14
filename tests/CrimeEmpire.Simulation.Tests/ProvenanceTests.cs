using CrimeSim.Domain;
using CrimeSim.Scenario;
using CrimeSim.Sim;
using CrimeSim.Trace;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>
/// Provenance records how a claim was acquired, and each category carries a different guarantee.
/// These tests hold those guarantees apart — particularly the one that motivated the milestone:
/// a man who ordered something knows he ordered it, and a man who saw the result does not.
/// </summary>
public sealed class ProvenanceTests
{
    private static World Run(string variant)
    {
        var world = Cast.Build(seed: 42, variant);
        Runner.Run(world, Cast.Start.AddDays(90));
        return world;
    }

    private static string Name(World world, string id)
        => world.Find(id)?.Name ?? world.Businesses.GetValueOrDefault(id)?.Name ?? id;

    /// <summary>
    /// The pair the vocabulary exists for. Vincent ordered the beating and holds that fact as its
    /// author; Marco was beaten and saw who did it, and learns nothing about who sent him.
    ///
    /// Authorship is not a visible property of an event. If witnessing could deliver it there
    /// would be nothing left for a capo to conceal, and the concealment the previous milestone
    /// models would be unfalsifiable in the other direction.
    /// </summary>
    [Fact]
    public void The_author_holds_his_own_order_and_a_witness_does_not_learn_it()
    {
        var world = Run("baseline");
        var vincent = world.Get("vincent");
        var marco = world.Get("marco");

        var authored = vincent.Cognition.Records.FirstOrDefault(r =>
            r.Claim.Kind == ClaimKind.PersonBreachedPolicy && r.Claim.Subject == vincent.Id);

        Assert.NotNull(authored);
        Assert.Equal(SourceKind.Participant, authored!.SourceKind);

        var seen = marco.Cognition.Records.FirstOrDefault(r =>
            r.Claim.Kind == ClaimKind.PersonUsedViolence);

        Assert.NotNull(seen);
        Assert.Equal(SourceKind.Witness, seen!.SourceKind);

        Assert.False(
            marco.Cognition.Records.Any(r => r.Claim.Kind == ClaimKind.PersonBreachedPolicy),
            "the man it was done to must not learn who authorised it merely by being there");
    }

    /// <summary>
    /// Witnessing is only ever produced by being there. Everyone else who comes to hold what
    /// happened got it some other way — found afterwards, or told — and no route that runs after
    /// the event may hand out a category that asserts presence at it.
    ///
    /// Note the assertion is not "Salvatore holds it as Discovery". In `baseline` he is told by
    /// Vincent before any discovery roll reaches him, so his record is a Report, and that is
    /// correct. The guarantee under test is the negative one.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("disloyal-vincent")]
    public void Nothing_acquired_after_the_event_is_recorded_as_witnessing(string variant)
    {
        var world = Run(variant);

        var witnesses = world.Characters.Values
            .SelectMany(c => c.Cognition.Records
                .Where(r => r.SourceKind == SourceKind.Witness)
                .Select(r => (Holder: c.Id, r.Claim)))
            .ToList();

        // Only the man it was done to. Nobody acquires Witness through the discovery roll or a
        // report, however confident either leaves them.
        Assert.All(witnesses, w => Assert.Equal("marco", w.Holder));

        // And the discovery path really does produce Discovery, or the negative above is vacuous.
        Assert.Contains(world.Characters.Values,
            c => c.Cognition.Records.Any(r => r.SourceKind == SourceKind.Discovery));
    }

    /// <summary>
    /// Testimony behaves as testimony, however close to the event its source stood. A participant's
    /// own account erodes at the ordinary rate and can lose its stance; what a man established
    /// himself resists and holds.
    /// </summary>
    [Fact]
    public void First_hand_testimony_is_not_protected_the_way_observation_is()
    {
        var at = Cast.Start;
        var claim = new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery, 1);

        var sawIt = new Cognition();
        sawIt.Learn(claim, Stance.Believes, 0.5, SourceKind.Witness, "self", at);

        var wasToldByTommy = new Cognition();
        wasToldByTommy.Learn(claim, Stance.Believes, 0.5, SourceKind.FirstHandTestimony, "tommy", at);

        var denial = new ReportedClaim(claim, Stance.Rejects, 0.9);
        sawIt.Receive(denial, "vincent", at.AddDays(1));
        wasToldByTommy.Receive(denial, "vincent", at.AddDays(1));

        Assert.True(sawIt.ConfidenceIn(claim) > wasToldByTommy.Find(claim)!.Confidence,
            "an account should give way faster than his own eyes");

        Assert.True(sawIt.Find(claim)!.IsHeld,
            "one denial must not talk a man out of what he saw");
        Assert.False(wasToldByTommy.Find(claim)!.IsHeld,
            "but it can talk him out of what he was told, which is the point of the category");
    }

    /// <summary>
    /// Being certain is not the same as having seen. A claim held beyond doubt that was never
    /// witnessed must never be rendered as though it were.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("disloyal-vincent")]
    public void Certainty_is_never_rendered_as_sight(string variant)
    {
        var world = Run(variant);

        foreach (var who in world.Characters.Values)
        {
            // The confidence vocabulary must stay silent about method entirely.
            foreach (var record in who.Cognition.Records)
                Assert.DoesNotContain("witness", record.ConfidenceLabel, StringComparison.OrdinalIgnoreCase);

            bool sawAnything = who.Cognition.Records.Any(r => r.SourceKind == SourceKind.Witness);
            if (sawAnything) continue;

            // Nobody who witnessed nothing may be described in terms of sight.
            string view = IntelligenceWriter.Render(world, who.Id);
            Assert.DoesNotContain("he saw it himself", view, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Transmission does not rewrite acquisition. Being told something makes it a report, and
    /// being told something you already established yourself does not turn your own knowledge
    /// into somebody else's account — or the reverse.
    /// </summary>
    [Fact]
    public void Reporting_never_upgrades_or_downgrades_how_a_claim_was_acquired()
    {
        var at = Cast.Start;
        var claim = new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery, 1);

        // Told something new: it is a report, not an observation, however sure the sender sounded.
        var fresh = new Cognition();
        fresh.Receive(new ReportedClaim(claim, Stance.Knows, 1.0), "tommy", at);
        Assert.Equal(SourceKind.Report, fresh.Find(claim)!.SourceKind);
        Assert.False(fresh.Find(claim)!.SourceKind.IsSelfAcquired());

        // Told something he already had first-hand: still his own, corroborated.
        var already = new Cognition();
        already.Learn(claim, Stance.Believes, 0.6, SourceKind.Participant, "self", at);
        already.Receive(new ReportedClaim(claim, Stance.Believes, 0.8), "tommy", at.AddDays(1));
        Assert.Equal(SourceKind.Participant, already.Find(claim)!.SourceKind);
    }

    /// <summary>
    /// The structural invariant behind the whole category. If a record says he established it
    /// himself, the record has to name him as its source; anything else is an account, and an
    /// account that claims to be unmediated is how presence gets invented.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("cautious-vincent")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    public void Unmediated_records_are_always_self_sourced(string variant)
    {
        var world = Run(variant);

        var wrong = world.Characters.Values
            .SelectMany(c => c.Cognition.Records
                .Where(r => r.SourceKind.IsSelfAcquired() && r.SourceId != c.Id)
                .Select(r => $"{c.Id} holds {r.Claim} as {r.SourceKind} sourced to {r.SourceId}"))
            .ToList();

        Assert.True(wrong.Count == 0,
            $"[{variant}] a record claims unmediated acquisition from somebody else:\n"
            + string.Join("\n", wrong));
    }

    /// <summary>
    /// Provenance steers future decisions — it decides what resists contradiction and what is
    /// worth seeking a second account of — so it has to be in the comparison that proves a paused
    /// run and an uninterrupted one are the same run.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("disloyal-vincent")]
    public void Provenance_survives_pausing_and_resuming(string variant)
    {
        static string Provenances(World w) => string.Join('\n', w.Characters.Values
            .OrderBy(c => c.Id, StringComparer.Ordinal)
            .SelectMany(c => c.Cognition.Records.Select(r =>
                $"{c.Id}|{r.Claim}|{r.SourceKind}|{r.SourceId}")));

        var straight = Run(variant);

        var resumed = Cast.Build(seed: 42, variant);
        Runner.Run(resumed, Cast.Start.AddDays(20));
        Runner.Run(resumed, Cast.Start.AddDays(55));
        Runner.Run(resumed, Cast.Start.AddDays(90));

        Assert.Equal(Provenances(straight), Provenances(resumed));
        Assert.Contains("Participant", Provenances(straight));
        Assert.Contains("Discovery", Provenances(straight));
    }
}
