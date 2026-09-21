using CrimeSim.Decision;
using CrimeSim.Session;
using CrimeEmpire.Persistence.Session;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>Existing personal-operation proofs now explicitly answer the new executor and confirm questions.</summary>
internal static class CommissioningTestDriver
{
    public static void ChooseAndConfirm(this SimulationSession session, string token)
    {
        session.Choose(token);
        Finish(() => session.Pending, session.Choose);
    }

    public static void ChooseAndConfirm(this PersistentSession session, string token)
    {
        session.Choose(token);
        Finish(() => session.Pending, session.Choose);
    }

    private static void Finish(Func<PendingDecision?> pending, Action<string> choose)
    {
        if (pending()?.Commissioning is null) return;
        // Some historical last-option policies now choose commissioning while personally busy.
        // Prefer self when available, otherwise the first retained executor; never choose Back.
        choose((pending()!.Options.FirstOrDefault(o => o.Description == "Do it yourself")
            ?? pending()!.Options.First(o => o.Description.StartsWith("Assign "))).Id);
        choose(pending()!.Options.Single(o => o.Description == "Confirm operation").Id);
    }

    public static DecisionRecord Resolve(PreparedDecision prepared, string? chosenCandidateId)
    {
        if (chosenCandidateId is { } id && prepared.Available.FirstOrDefault(c => c.Id == id) is { } leaf
            && Commissioning.IsOperation(leaf))
        {
            var executor = Commissioning.Prepare(prepared, leaf).Available.Single(c => c.Kind == ActionKind.StartStrategy);
            return Pipeline.Resolve(prepared, id, executor.Id);
        }
        return Pipeline.Resolve(prepared, chosenCandidateId);
    }
}
