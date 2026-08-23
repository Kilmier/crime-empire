using CrimeSim.Session;

namespace CrimeEmpire.Persistence;

/// <summary>
/// What "the same build" means for ruling 8's same-build-only compatibility rule.
///
/// <c>ModuleVersionId</c> is produced by the compiler from a deterministic build (the default since
/// the .NET SDK this project targets, and unset by anything in <c>Directory.Build.props</c>):
/// identical source and references reliably produce an identical MVID, and a behavioural change to
/// <c>CrimeEmpire.Simulation</c> reliably produces a different one. That makes it a same-build
/// fingerprint nobody has to remember to bump, unlike a hand-maintained version string, and one that
/// needs no build-time tooling (no <c>git</c> invocation), unlike a source-control revision id.
/// </summary>
public static class SimulationBuild
{
    /// <summary>
    /// The current process's build of the simulation, read off the one type every save already
    /// depends on. Not cached beyond the CLR's own module handle, which does not change within a
    /// process.
    /// </summary>
    public static string CurrentId
        => typeof(SimulationSession).Assembly.ManifestModule.ModuleVersionId.ToString("N");
}
