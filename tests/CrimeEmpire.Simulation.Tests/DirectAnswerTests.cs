using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Scenario;
using CrimeSim.Sim;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>
/// Answering a direct question, and the three ways that path went wrong: a sincere denial filed as
/// deception, a question inverting the chain of command, and an answer whose supporting belief was
/// missing from the decision trace.
/// </summary>
public sealed class DirectAnswerTests
{
    private static readonly Claim Beating =
        new(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery, 1);

    private static World Run(string variant)
    {
        var world = Cast.Build(seed: 42, variant);
        Runner.Run(world, Cast.Start.AddDays(90));
        return world;
    }

    /// <summary>Wakes a character with a direct question about one claim and returns his decision.</summary>
    private static DecisionRecord AskAbout(World world, Character who, string askerId, Claim about)
    {
        var trigger = world.Queue.Schedule(
            Cast.Start.AddDays(1), EventKind.RoleReview, who.Id,
            $"{askerId} asked him directly what happened",
            new EventPayload { TargetId = askerId, Note = "asked-to-account", AboutClaim = about });

        world.Now = trigger.Time;
        return Pipeline.Deliberate(world, who, trigger);
    }

    /// <summary>
    /// A man who does not believe he did it, saying he did not do it, is being honest.
    ///
    /// The old rule offered a False report whenever the question named the respondent, without
    /// asking whether he held the thing being denied — so a sincere rejection was filed as a lie.
    /// That inverts the one distinction ReportCandor exists to draw, and inverts it in the
    /// direction that makes an innocent man look guilty in the developer record.
    /// </summary>
    [Fact]
    public void A_sincere_rejection_is_never_offered_as_a_false_report()
    {
        var world = Cast.Build(seed: 42, "baseline");
        var tommy = world.Get("tommy");

        // He has come to reject it — sincerely, and about himself.
        tommy.Cognition.Learn(Beating, Stance.Rejects, 0.9, SourceKind.Participant, tommy.Id, Cast.Start);

        var decision = AskAbout(world, tommy, "salvatore", Beating);

        Assert.DoesNotContain(decision.Generated, c => c.Candor == ReportCandor.False);
        Assert.DoesNotContain(decision.Generated, c => c.Candor == ReportCandor.Partial);

        Assert.Contains(decision.Generated,
            c => c.AnsweringClaim == Beating && c.Candor == ReportCandor.Candid);
    }

    /// <summary>
    /// The contrast, so the test above cannot pass by never generating anything. The same question
    /// to a man who does hold it against himself still offers him the chance to lie.
    /// </summary>
    [Fact]
    public void A_man_who_holds_it_against_himself_can_still_deny_it()
    {
        var world = Cast.Build(seed: 42, "baseline");
        var tommy = world.Get("tommy");

        tommy.Cognition.Learn(Beating, Stance.Knows, 1.0, SourceKind.Participant, tommy.Id, Cast.Start);

        var decision = AskAbout(world, tommy, "salvatore", Beating);

        Assert.Contains(decision.Generated, c => c.Candor == ReportCandor.False);
    }

    /// <summary>
    /// Being asked a question redirects who a man answers to. It does not put the asker above him.
    ///
    /// The old rule reused the recipient of the answer as the policy authority, so a boss with
    /// nobody above him fell back to whoever had just questioned him — and Salvatore asked his own
    /// soldier for permission to relax Salvatore's own policy.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("cautious-vincent")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    public void Permission_is_never_sought_from_someone_lower_in_the_chain(string variant)
    {
        var world = Run(variant);

        var inverted = world.Decisions
            .SelectMany(d => d.Generated
                .Where(c => c.Kind == ActionKind.SeekApproval)
                .Select(c => (Decision: d, Candidate: c)))
            .Where(x =>
            {
                var actor = world.Get(x.Decision.ActorId);
                var target = world.Find(x.Candidate.TargetId ?? "");
                return target is null
                       || target.Capabilities.Authority <= actor.Capabilities.Authority;
            })
            .Select(x => $"{x.Decision.ActorId} -> {x.Candidate.TargetId}: {x.Candidate.Description}")
            .ToList();

        Assert.True(inverted.Count == 0,
            $"[{variant}] permission asked of somebody not above the asker:\n"
            + string.Join("\n", inverted));
    }

    /// <summary>
    /// The trace has to show what the answer rested on. Reading the position off the raw belief
    /// list did not mark it consulted, so a man answered a question about a claim while the record
    /// of what he knew omitted that very claim.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("disloyal-vincent")]
    public void An_answer_records_the_belief_it_was_drawn_from(string variant)
    {
        var world = Run(variant);
        int answers = 0;

        foreach (var decision in world.Decisions)
        {
            if (decision.Chosen?.Candidate.AnsweringClaim is not { } answered) continue;
            answers++;

            Assert.True(decision.BeliefsUsed.Any(b => b.Claim.Equals(answered)),
                $"[{variant}] {decision.ActorId} answered about {answered} on {decision.At:d MMM} "
                + "without that position appearing in what he knew");
        }

        Assert.True(answers > 0, $"[{variant}] no answer was ever given, so nothing was proved");
    }

    /// <summary>
    /// The invariant at the place the report is actually built, not merely at the place candidates
    /// are offered.
    ///
    /// A candidate marked False whose suppressed claim the sender does not hold used to fall
    /// through to the honest branch and come back stamped `ReportCandor.False` with lying framing:
    /// content and label disagreeing, and a sincere man recorded as a liar by a field nothing had
    /// checked. Composing must refuse it outright rather than quietly relabel it, so a future
    /// caller cannot reintroduce the state by building a candidate by hand.
    /// </summary>
    [Fact]
    public void Composing_refuses_a_false_report_that_would_deny_nothing()
    {
        var world = Cast.Build(seed: 42, "baseline");
        var tommy = world.Get("tommy");
        var salvatore = world.Get("salvatore");

        // He sincerely does not believe he did it.
        tommy.Cognition.Learn(Beating, Stance.Rejects, 0.9, SourceKind.Participant, tommy.Id, Cast.Start);
        var perceived = Salience.Perceive(tommy, Cast.Start);

        var inconsistent = new Candidate(
            "handmade:false", ActionKind.ReportToSuperior, "test", "deny something he does not hold")
        {
            TargetId = salvatore.Id,
            Candor = ReportCandor.False,
            Suppressed = new[] { Beating },
            AnsweringClaim = Beating,
        };

        var thrown = Assert.Throws<ArgumentException>(() =>
            Reporting.Compose(world, tommy, salvatore, inconsistent, perceived));

        Assert.Contains("candour is False", thrown.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(world.Reports);
    }

    /// <summary>
    /// The positive control. The same candidate against a man who does hold it composes normally,
    /// and what comes back is a denial — so the guard above rejects inconsistency rather than
    /// rejecting lying.
    /// </summary>
    [Fact]
    public void Composing_still_produces_a_denial_when_there_is_something_to_deny()
    {
        var world = Cast.Build(seed: 42, "baseline");
        var tommy = world.Get("tommy");
        var salvatore = world.Get("salvatore");

        tommy.Cognition.Learn(Beating, Stance.Knows, 1.0, SourceKind.Participant, tommy.Id, Cast.Start);
        var perceived = Salience.Perceive(tommy, Cast.Start);

        var lie = new Candidate(
            "handmade:false", ActionKind.ReportToSuperior, "test", "deny it")
        {
            TargetId = salvatore.Id,
            Candor = ReportCandor.False,
            Suppressed = new[] { Beating },
            AnsweringClaim = Beating,
        };

        var report = Reporting.Compose(world, tommy, salvatore, lie, perceived);

        Assert.Equal(ReportCandor.False, report.Candor);
        Assert.Contains(report.Asserted, a => a.Claim.Equals(Beating) && a.AssertedStance == Stance.Rejects);
    }

    /// <summary>
    /// Every report the simulation actually produces satisfies the same invariant: a false one
    /// always carries a denial. The guard is a backstop; this is the guarantee in play.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("disloyal-vincent")]
    public void No_false_report_is_ever_filed_without_a_denial_in_it(string variant)
    {
        var world = Run(variant);

        foreach (var report in world.Reports.Where(r => r.Candor == ReportCandor.False))
            Assert.True(report.Asserted.Any(a => a.AssertedStance == Stance.Rejects),
                $"[{variant}] report {report.Id} from {report.SenderId} is marked false but denies nothing");
    }

    /// <summary>
    /// And the same guarantee for a position he rejects, which is the case the accessor exists for:
    /// a denial is drawn from a real record and must appear in the trace like any other.
    /// </summary>
    [Fact]
    public void A_rejected_position_is_recorded_as_consulted_when_it_is_answered_from()
    {
        var world = Cast.Build(seed: 42, "baseline");
        var tommy = world.Get("tommy");

        tommy.Cognition.Learn(Beating, Stance.Rejects, 0.9, SourceKind.Participant, tommy.Id, Cast.Start);

        var decision = AskAbout(world, tommy, "salvatore", Beating);

        Assert.Contains(decision.BeliefsUsed, b => b.Claim.Equals(Beating));
    }
}
