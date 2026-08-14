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

    /// <summary>
    /// The snapshot has to name what each request was about.
    ///
    /// This cannot be mutation-checked the usual way — the snapshot is the comparator, so deleting
    /// a field from it makes the comparison blinder without failing anything. Asserting the content
    /// of the snapshot string is the only check that fails when the subject is dropped.
    /// </summary>
    [Fact]
    public void The_snapshot_names_the_subject_of_every_request()
    {
        var world = Run(seed: 42, variant: "disloyal-vincent", days: 90);
        Assert.NotEmpty(world.Requests);

        string snapshot = Snapshot(world);
        foreach (var request in world.Requests)
            Assert.True(snapshot.Contains(request.About.ToString(), StringComparison.Ordinal),
                $"the replay snapshot does not mention {request.About}, so a run that asked a " +
                "different question would compare equal");
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

    /// <summary>
    /// Milestone 005's load-bearing property: occasion keys are causally local, not derived from
    /// ScheduledEvent.Id, so a perturbation that only ever consumes an event id and is immediately
    /// cancelled must not change anything downstream. Before the fix, this shifted every later id
    /// and re-rolled every subsequent observation and strategy-step outcome; a cancelled event is
    /// the smallest perturbation that still consumes an id, which is what makes it the right proof.
    ///
    /// Compared with <see cref="BehavioralSnapshot"/>, not <see cref="Snapshot"/>. The full snapshot
    /// is the wrong tool here: it deliberately bakes in ScheduledEvent.Id (DecisionRecord's own
    /// trigger id) and lets Claim.ToString() print a WorldEvent-derived EventId inside report and
    /// candidate text, both of which are *expected* to shift when an extra event is scheduled — that
    /// shift is not a behavioural difference, and asserting against it would fail this test even
    /// when the fix is working.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("cautious-vincent")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    public void An_unrelated_cancelled_event_does_not_change_the_history(string variant)
    {
        var unperturbed = Run(seed: 42, variant: variant, days: 90);

        var perturbed = Cast.Build(seed: 42, variant);
        var noise = perturbed.Queue.Schedule(Cast.Start.AddHours(1), EventKind.WorldTick, null, "test: noise");
        perturbed.Queue.Cancel(noise.Id, "test: causally inert perturbation");
        Runner.Run(perturbed, Cast.Start.AddDays(90));

        Assert.Equal(BehavioralSnapshot(unperturbed), BehavioralSnapshot(perturbed));
    }

    /// <summary>
    /// The same property from the truth-log side: WorldEvent.Id is a global counter too, and
    /// nothing keyed to it should exist any more. An extra recorded entry with no causal role must
    /// not move any subsequent roll. See <see cref="BehavioralSnapshot"/> for why the comparison
    /// deliberately does not use the full <see cref="Snapshot"/>.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("cautious-vincent")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    public void An_unrelated_truth_log_entry_does_not_change_the_history(string variant)
    {
        var unperturbed = Run(seed: 42, variant: variant, days: 90);

        var perturbed = Cast.Build(seed: 42, variant);
        perturbed.Record("test-noise", "salvatore", null, "an entry with no causal role");
        Runner.Run(perturbed, Cast.Start.AddDays(90));

        Assert.Equal(BehavioralSnapshot(unperturbed), BehavioralSnapshot(perturbed));
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
            string.Join(",", r.Asserted.Select(a =>
                $"{a.Claim}:{a.AssertedStance}:{Number(a.AssertedConfidence)}" +
                $":{a.ClaimedBasis}:{a.ActualBasis}")) +
            "|" + string.Join(",", r.Withheld.Select(w => w.ToString()))));

        // Requests too. They gate future candidate generation, so a run that made different
        // requests would go on to make different decisions — state that steers behaviour has to be
        // in the canonical comparison, not only in the focused pause/resume test.
        //
        // Note this line cannot be mutation-checked: the snapshot is the comparator, so deleting a
        // field from it makes the comparison blinder without making anything fail. The independent
        // assurance is that asking is also recorded in the truth log, which the runner's --verify
        // hash covers by a different route.
        lines.AddRange(world.Requests.Select(q =>
            $"request|{q.Id}|{q.At:O}|{q.AskerId}|{q.AskedId}|{q.About}"));

        foreach (var business in world.Businesses.Values.OrderBy(b => b.Id, StringComparer.Ordinal))
            lines.Add($"business|{business.Id}|{Number(business.MonthlyRevenue)}|" +
                      $"{business.PayingTribute}|{Number(business.Resistance)}|{business.Damaged}");

        foreach (var character in world.Characters.Values.OrderBy(c => c.Id, StringComparer.Ordinal))
        {
            // StrategyCount, LocalSequence, NextAdvanceOrdinal and PendingStepEventId are milestone
            // 005's occasion identity — a run that assigned a different instance sequence, drew a
            // different advance ordinal, or left a different step pending would go on to draw
            // different occasion keys, so this state has to be in the canonical comparison rather
            // than only in a dedicated test. AttemptedConcealments is the redundancy rule's own
            // state: a run that attempted a different set of incidents would reach different
            // candidates on every later wake.
            lines.Add($"character|{character.Id}|{character.Tier}|{character.DecisionCount}|" +
                      $"{character.StrategyCount}|{character.Execution.Strategy?.Kind}|" +
                      $"{character.Execution.Strategy?.OwnerId}|{character.Execution.Strategy?.LocalSequence}|" +
                      $"{character.Execution.Strategy?.StepIndex}|{character.Execution.Strategy?.NextAdvanceOrdinal}|" +
                      $"{character.Execution.Strategy?.PendingStepEventId}|" +
                      string.Join(",", character.Execution.AttemptedConcealments.Select(a => a.ToString())));

            // Relationship state, milestone 006. Trust now moves during a run — a perceived account
            // conflict costs the listener trust in the speaker — and Utility.Loyalty reads trust,
            // obligation and grievance while Concede/Refuse read fear, so all of it steers later
            // decisions and belongs in the canonical comparison rather than only in a dedicated
            // test. Marco's fear was already moving before this milestone and was already outside
            // this comparator, which is the gap this line closes.
            //
            // Enumerated through SocialState.All, which is ordered by id: dictionary order is an
            // implementation detail the determinism rules forbid depending on.
            foreach (var rel in character.Social.All)
                lines.Add($"relationship|{character.Id}|{rel.OtherId}|{Number(rel.Trust)}|" +
                          $"{Number(rel.Fear)}|{Number(rel.Obligation)}|" +
                          string.Join(",", rel.Grievances.Select(g =>
                              $"{g.Description}:{Number(g.Severity)}:{g.At:O}")));

            lines.AddRange(character.Cognition.Records.Select(r =>
                $"knowledge|{character.Id}|{r.Claim.Kind}|{r.Claim.Subject}|{r.Claim.Object}|" +
                $"{r.Stance}|{Number(r.Confidence)}|{r.SourceKind}|{r.SourceId}|{r.AcquiredAt:O}|" +
                $"{r.ReconsideredAt:O}|{r.Contested}"));

            // Claimed basis, not actual: this is the listener's log, and what the speaker really
            // had never enters it. The distinction is decision-relevant — it decides whether the
            // belief is first-hand testimony — so a run that changed it must fail this comparison.
            lines.AddRange(character.Cognition.Testimony.Select(t =>
                $"testimony|{character.Id}|{t.SenderId}|{t.Claim}|{t.AssertedStance}|" +
                $"{Number(t.AssertedConfidence)}|{t.ClaimedBasis}|{t.At:O}"));
        }

        return string.Join('\n', lines);
    }

    /// <summary>
    /// A comparator deliberately narrower than <see cref="Snapshot"/>: it omits every field that is
    /// itself a monotonic counter derived from ScheduledEvent.Id, WorldEvent.Id, Report.Id or
    /// Request.Id — and every free-text field, such as Candidate.Id or DecisionRecord.Outcome, that
    /// can embed one indirectly through Claim.ToString() printing a nonzero EventId. Those ids
    /// legitimately shift when an extra event is scheduled or an extra truth-log entry is recorded;
    /// that shift is exactly what the insertion-stability tests above must not mistake for a
    /// behavioural difference. What must not shift is which candidate was chosen, in what shape, and
    /// what the world and every character's knowledge end up looking like.
    /// </summary>
    private static string BehavioralSnapshot(World world)
    {
        static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);

        var lines = new List<string>();

        foreach (var d in world.Decisions)
            lines.Add($"decision|{d.ActorId}|{d.TriggerKind}|{d.Agenda.Kind}|{d.Agenda.Domain}|" +
                      $"{d.Chosen?.Candidate.Kind}|{d.Chosen?.Candidate.Strategy}|{d.Chosen?.Candidate.Method}|" +
                      $"{d.Chosen?.Candidate.TargetId}|{d.Chosen?.Candidate.Candor}");

        foreach (var business in world.Businesses.Values.OrderBy(b => b.Id, StringComparer.Ordinal))
            lines.Add($"business|{business.Id}|{Number(business.MonthlyRevenue)}|" +
                      $"{business.PayingTribute}|{Number(business.Resistance)}|{business.Damaged}");

        foreach (var character in world.Characters.Values.OrderBy(c => c.Id, StringComparer.Ordinal))
        {
            lines.Add($"character|{character.Id}|{character.Tier}|{character.DecisionCount}|" +
                      $"{character.StrategyCount}|{character.Execution.Strategy?.Kind}|" +
                      $"{character.Execution.Strategy?.TargetId}|{character.Execution.Strategy?.StepIndex}|" +
                      $"{character.Execution.Strategy?.NextAdvanceOrdinal}|" +
                      string.Join(",", character.Execution.AttemptedConcealments.Select(a =>
                          $"{a.Kind}:{a.Subject}:{a.Object}")));

            // As above, minus the grievance timestamp: a DateTime is not derived from any global
            // counter, but it is free text as far as this comparator is concerned and the narrower
            // one keeps to what a perturbation could not legitimately move.
            foreach (var rel in character.Social.All)
                lines.Add($"relationship|{character.Id}|{rel.OtherId}|{Number(rel.Trust)}|" +
                          $"{Number(rel.Fear)}|{Number(rel.Obligation)}|" +
                          string.Join(",", rel.Grievances.Select(g => Number(g.Severity))));

            lines.AddRange(character.Cognition.Records.Select(r =>
                $"knowledge|{character.Id}|{r.Claim.Kind}|{r.Claim.Subject}|{r.Claim.Object}|" +
                $"{r.Stance}|{Number(r.Confidence)}|{r.SourceKind}|{r.SourceId}"));
        }

        return string.Join('\n', lines);
    }
}
