using CrimeSim.Domain;
using CrimeSim.Scenario;
using CrimeSim.Session;
using CrimeSim.Sim;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>
/// Milestone 024 — The Operation Reads. A player saw a decision prompt when his character paused and
/// nothing between pauses: he gave an order and the order disappeared. `ExecutionState.Strategy` held
/// what he had running the whole time — kind, target, method, who it was handed to, when it started,
/// how many attempts had come back empty — and none of it reached `PlayerSnapshot`.
///
/// <b>The milestone is one branch, and it is an information rule rather than a display preference.</b>
/// Progress is reported for work he is doing himself, because the step he has reached and the
/// refusals he has met are his own experience. It is withheld for work he handed to somebody, because
/// `StepIndex` on a delegated instance is the executor's state and milestones 017 and 022 both settled
/// that the man who ordered a job learns whether it was carried out "through a report or a discovery
/// roll like anyone else". Handing it over through a panel instead of through a belief makes it no
/// less a leak.
/// </summary>
public sealed class OperationReadsTests
{
    private const int Seed = 42;

    // ================================================================= the boundary

    /// <summary>
    /// The milestone's central claim, proved by difference rather than by inspection: two worlds
    /// identical but for how far a **delegated** operation has advanced produce byte-identical
    /// player-facing text, and the same difference on **own** work does not.
    ///
    /// Asserting only "delegated progress is null" would pass against an implementation that never
    /// showed progress at all. The own-work half is what makes the delegated half mean something.
    /// </summary>
    [Fact]
    public void How_far_a_delegate_has_got_never_reaches_the_player_but_his_own_progress_does()
    {
        var earlyDelegated = Operating(delegated: true, stepIndex: 1);
        var lateDelegated = Operating(delegated: true, stepIndex: 3);

        Assert.Equal(Render(earlyDelegated), Render(lateDelegated));
        Assert.Null(Snapshot(earlyDelegated).Operation!.Progress);

        var earlyOwn = Operating(delegated: false, stepIndex: 1);
        var lateOwn = Operating(delegated: false, stepIndex: 3);

        Assert.NotEqual(Render(earlyOwn), Render(lateOwn));
        Assert.NotNull(Snapshot(earlyOwn).Operation!.Progress);
    }

    /// <summary>
    /// The same rule from the other side: what he ordered, and who has it, are his own acts and are
    /// shown for both. Withholding those too would be a different and wrong milestone — a man does
    /// not forget who he sent.
    /// </summary>
    [Fact]
    public void What_he_ordered_and_who_is_carrying_it_are_always_his_to_know()
    {
        var op = Snapshot(Operating(delegated: true, stepIndex: 2)).Operation;

        Assert.NotNull(op);
        Assert.Contains("Bellini's grocery", op!.Description, StringComparison.Ordinal);
        Assert.Equal("Tommy Nardo", op.ExecutorName);
        Assert.Equal(Cast.Start, op.Since);
    }

    /// <summary>Nothing running, nothing claimed. Null rather than an empty-looking operation.</summary>
    [Fact]
    public void A_man_with_nothing_running_has_no_operation()
    {
        var world = Cast.Build(Seed, "baseline");
        Assert.Null(PlayerView.Build(world, "vincent", world.Now).Operation);
    }

    // ================================================================= what it is allowed to say

    /// <summary>
    /// No developer vocabulary reaches the panel. `StrategyInstance.Label` renders as
    /// `SecureTribute(harbour, target=bellini-grocery, method=Persuade)` and once reached the player
    /// as a decision's focus — milestone 009's first correction. The wording here comes from
    /// <c>PlayerOption.Work</c>, the shared phrasing that exists so there are not two of them.
    ///
    /// And no digits, for the same reason a standing never carries one: the step is named and the
    /// empty attempts are counted in words.
    /// </summary>
    [Fact]
    public void The_operation_never_speaks_in_developer_terms_or_numbers()
    {
        foreach (bool delegated in new[] { true, false })
        {
            var op = Snapshot(Operating(delegated, stepIndex: 2)).Operation!;
            string text = $"{op.Description} {op.ExecutorName} {op.Progress}";

            Assert.DoesNotContain("SecureTribute", text, StringComparison.Ordinal);
            Assert.DoesNotContain("bellini-grocery", text, StringComparison.Ordinal);
            Assert.DoesNotContain("tommy", text, StringComparison.Ordinal);
            Assert.DoesNotContain("StepIndex", text, StringComparison.Ordinal);
            Assert.All("0123456789", d => Assert.DoesNotContain(d.ToString(), text, StringComparison.Ordinal));
        }
    }

    /// <summary>
    /// Empty attempts are his own experience and are said in words. Staged across the range because
    /// the wording changes shape at one, two and more, and a count that read "3" would be the model's
    /// arithmetic rather than something that happened to him.
    /// </summary>
    [Theory]
    [InlineData(0, "made his demand")]
    [InlineData(1, "once it went nowhere")]
    [InlineData(2, "twice it went nowhere")]
    [InlineData(5, "keeps going nowhere")]
    public void Refusals_on_his_own_work_are_counted_in_words(int failures, string expected)
    {
        var world = Operating(delegated: false, stepIndex: 2);
        world.Get("vincent").Execution.Strategy!.FailedAttempts = failures;

        Assert.Contains(expected, Snapshot(world).Operation!.Progress!, StringComparison.Ordinal);
    }

    // ================================================================= natural, and both surfaces

    /// <summary>
    /// It populates in an unmodified run, read through the real projection — and the delegated case
    /// is the one the accepted fixture actually produces, which is the case the boundary protects.
    /// </summary>
    [Fact]
    public void The_panel_is_populated_during_a_natural_run()
    {
        var world = Cast.Build(Seed, "baseline");
        Runner.Run(world, Cast.Start.AddDays(20));

        var op = PlayerView.Build(world, "vincent", world.Now).Operation;

        Assert.NotNull(op);
        Assert.Equal("Tommy Nardo", op!.ExecutorName);
        Assert.Null(op.Progress);
    }

    // ================================================================= helpers

    /// <summary>
    /// A world with one operation running, optionally handed to Tommy, advanced to a given step.
    /// Built by assignment rather than by driving the pipeline, because the point is to vary
    /// <c>StepIndex</c> alone while holding everything else identical — which a real run cannot do.
    /// </summary>
    private static World Operating(bool delegated, int stepIndex)
    {
        var world = Cast.Build(Seed, "baseline");
        var vincent = world.Get("vincent");

        vincent.Execution.Strategy = new StrategyInstance
        {
            OwnerId = vincent.Id,
            LocalSequence = vincent.StrategyCount++,
            Kind = StrategyKind.SecureTribute,
            Domain = Cast.Harbour,
            TargetId = Cast.Grocery,
            Method = CoercionMethod.Persuade,
            StartedAt = Cast.Start,
            Deadline = Cast.Start.AddDays(30),
            StepIndex = stepIndex,
            DelegatedToId = delegated ? "tommy" : null,
        };

        return world;
    }

    private static PlayerSnapshot Snapshot(World world)
        => PlayerView.Build(world, "vincent", world.Now);

    /// <summary>Everything about the operation the player can see, as one string to diff.</summary>
    private static string Render(World world)
    {
        var op = Snapshot(world).Operation;
        return op is null ? "" : $"{op.Description}|{op.ExecutorName}|{op.Since:O}|{op.Progress}";
    }
}
