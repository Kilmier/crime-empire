using CrimeSim.Domain;
using CrimeSim.Scenario;
using CrimeSim.Session;
using Xunit;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>Astra's accepted historical-audit finding L3.</summary>
public sealed class ProjectedClaimCollisionTests
{
    /// <summary>
    /// Two contested domain claims differ only by their incident ids. The player boundary must
    /// continue to omit those truth-log ids while preserving both projected disagreements and all
    /// of their source accounts for the renderer to coalesce safely.
    /// </summary>
    [Fact]
    public void Two_incidents_with_one_visible_predicate_preserve_each_position_basis_and_account()
    {
        var world = Cast.Build(seed: 42, variant: "baseline");
        var salvatore = world.Get("salvatore");
        DateTime firstAt = Cast.Start.AddDays(1);
        DateTime secondAt = Cast.Start.AddDays(4);

        var first = new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery, EventId: 101);
        var second = new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery, EventId: 202);

        salvatore.Cognition.Learn(
            first, Stance.Believes, 0.8, SourceKind.Discovery, salvatore.Id, firstAt);
        salvatore.Cognition.Receive(
            new ReportedClaim(first, Stance.Rejects, 0.9), "vincent", firstAt.AddHours(1));

        salvatore.Cognition.Learn(
            second, Stance.Rejects, 0.8, SourceKind.Inference, salvatore.Id, secondAt);
        salvatore.Cognition.Receive(
            new ReportedClaim(second, Stance.Believes, 0.9), "kane", secondAt.AddHours(1));

        var snapshot = PlayerView.Build(world, salvatore.Id, secondAt.AddDays(1));
        var visible = new PlayerClaim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery);
        var projected = snapshot.Disagreements.Where(d => d.Claim == visible).ToList();

        Assert.Equal(2, projected.Count);
        Assert.All(projected, d => Assert.Equal(visible, d.Claim));
        Assert.Contains(projected, d => d.OwnPositionHeld && d.OwnBasis == "what he found out");
        Assert.Contains(projected, d => !d.OwnPositionHeld && d.OwnBasis == "what he worked out");
        Assert.Contains(projected.SelectMany(d => d.Accounts), a => a.SourceId == "vincent" && !a.Affirms);
        Assert.Contains(projected.SelectMany(d => d.Accounts), a => a.SourceId == "kane" && a.Affirms);
    }
}
