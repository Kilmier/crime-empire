using System.Globalization;
using CrimeSim.Scenario;
using CrimeSim.Sim;

namespace CrimeEmpire.Simulation.Tests;

public sealed class SimulationReplayTests
{
    [Fact]
    public void Identical_inputs_produce_identical_histories()
    {
        var first = Run(seed: 42, variant: "baseline", days: 90);
        var second = Run(seed: 42, variant: "baseline", days: 90);

        Assert.Equal(Snapshot(first), Snapshot(second));
    }

    [Fact]
    public void Pausing_and_resuming_does_not_change_the_history()
    {
        var uninterrupted = Run(seed: 42, variant: "baseline", days: 90);

        var resumed = Cast.Build(seed: 42, variant: "baseline");
        Runner.Run(resumed, Cast.Start.AddDays(30));
        Runner.Run(resumed, Cast.Start.AddDays(90));

        Assert.Equal(Snapshot(uninterrupted), Snapshot(resumed));
    }

    [Fact]
    public void Meaningful_character_variants_can_change_the_history()
    {
        var baseline = Run(seed: 42, variant: "baseline", days: 90);
        var cautiousVincent = Run(seed: 42, variant: "cautious-vincent", days: 90);

        Assert.NotEqual(Snapshot(baseline), Snapshot(cautiousVincent));
    }

    private static World Run(int seed, string variant, int days)
    {
        var world = Cast.Build(seed, variant);
        Runner.Run(world, Cast.Start.AddDays(days));
        return world;
    }

    private static string Snapshot(World world)
    {
        static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        var lines = new List<string>
        {
            $"now|{world.Now:O}",
            $"queue|{world.Queue.Count}",
        };

        lines.AddRange(world.TruthLog.Select(e =>
            $"truth|{e.Id}|{e.At:O}|{e.Kind}|{e.ActorId}|{e.TargetId}|{e.Summary}"));

        lines.AddRange(world.Decisions.Select(d =>
            $"decision|{d.Id}|{d.At:O}|{d.ActorId}|{d.TriggerEventId}|{d.TriggerKind}|" +
            $"{d.Agenda.Kind}|{d.Agenda.Domain}|{d.Chosen?.Candidate.Id}|" +
            $"{Number(d.Chosen?.Total ?? 0)}|{d.Outcome}"));

        // Report content and candour are simulation state, so determinism has to cover them —
        // otherwise a run could pass the replay test while quietly composing different accounts.
        lines.AddRange(world.Reports.Select(r =>
            $"report|{r.Id}|{r.At:O}|{r.SenderId}|{r.RecipientId}|{r.Candor}|" +
            string.Join(",", r.Asserted.Select(a => $"{a.Claim}:{a.AssertedStance}:{Number(a.AssertedConfidence)}")) +
            "|" + string.Join(",", r.Withheld.Select(w => w.ToString()))));

        foreach (var business in world.Businesses.Values.OrderBy(b => b.Id, StringComparer.Ordinal))
            lines.Add($"business|{business.Id}|{Number(business.MonthlyRevenue)}|" +
                      $"{business.PayingTribute}|{Number(business.Resistance)}|{business.Damaged}");

        foreach (var character in world.Characters.Values.OrderBy(c => c.Id, StringComparer.Ordinal))
        {
            lines.Add($"character|{character.Id}|{character.Tier}|{character.DecisionCount}|" +
                      $"{character.Execution.Strategy?.Kind}|{character.Execution.Strategy?.StepIndex}");

            lines.AddRange(character.Cognition.Records.Select(r =>
                $"knowledge|{character.Id}|{r.Claim.Kind}|{r.Claim.Subject}|{r.Claim.Object}|" +
                $"{r.Stance}|{Number(r.Confidence)}|{r.SourceKind}|{r.SourceId}|{r.AcquiredAt:O}|" +
                $"{r.ReconsideredAt:O}|{r.Contested}"));

            lines.AddRange(character.Cognition.Testimony.Select(t =>
                $"testimony|{character.Id}|{t.SenderId}|{t.Claim}|{t.AssertedStance}|" +
                $"{Number(t.AssertedConfidence)}|{t.At:O}"));
        }

        return string.Join('\n', lines);
    }
}
