using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Scenario;
using CrimeSim.Sim;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>Explicit conflict fixture, not a claim that the M028 natural run repeats stale briefings.</summary>
internal static class AccountScenario
{
    internal static void ContradictVincent(World world)
    {
        var claim = new Claim(ClaimKind.BusinessRefusesTribute, Cast.Grocery);
        var vincent = world.Get("vincent");
        vincent.Cognition.Learn(claim, Stance.Rejects, 0.9, SourceKind.Discovery, vincent.Id, world.Now);
        var report = new Report(world.NextReportId(), "salvatore", "vincent", world.Now,
            ReportCandor.Candid, new[] { ReportedClaim.Honest(claim, Stance.Believes, 0.75, SourceKind.Report) },
            Array.Empty<Claim>(), "staged stale briefing");
        Reporting.Deliver(world, report, vincent);
    }
}
