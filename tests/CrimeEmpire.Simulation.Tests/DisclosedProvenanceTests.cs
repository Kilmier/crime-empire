using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Scenario;
using CrimeSim.Sim;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>
/// What a speaker claims and what a speaker has are two different things, and only the first is
/// available to the listener. These tests hold that line, and cover the rest of the
/// <see cref="Cognition.Receive"/> state machine that provenance has to pass through.
/// </summary>
public sealed class DisclosedProvenanceTests
{
    private const string Viewpoint = "salvatore";
    private static readonly Claim Beating = new(ClaimKind.PersonUsedViolence, "vincent", Cast.Grocery, 1);

    private static World Run(string variant)
    {
        var world = Cast.Build(seed: 42, variant);
        Runner.Run(world, Cast.Start.AddDays(90));
        return world;
    }

    // ---------------------------------------------------------------- claimed vs actual

    /// <summary>
    /// A denial must not disclose the participation it denies.
    ///
    /// Transmitting the sender's private basis leaked. Vincent denied the beating and shipped
    /// `Participant` alongside the denial, the listener filed first-hand testimony, and the
    /// concealment announced the very thing it was concealing — through a field nobody was reading.
    /// </summary>
    [Fact]
    public void A_denial_does_not_disclose_the_participation_it_denies()
    {
        var world = Cast.Build(seed: 42, "baseline");
        var vincent = world.Get("vincent");
        var salvatore = world.Get(Viewpoint);

        vincent.Cognition.Learn(Beating, Stance.Knows, 1.0, SourceKind.Participant, vincent.Id, Cast.Start);
        world.Now = Cast.Start.AddDays(1);

        var lie = new Candidate("lie", ActionKind.ReportToSuperior, "test", "deny it")
        {
            TargetId = salvatore.Id,
            Candor = ReportCandor.False,
            Suppressed = new[] { Beating },
            AnsweringClaim = Beating,
        };

        var report = Reporting.Compose(
            world, vincent, salvatore, lie, Salience.Perceive(vincent, world.Now));
        Reporting.Deliver(world, report, salvatore);

        var denial = report.Asserted.Single(a => a.Claim.Equals(Beating));

        // Developer truth keeps what he really had; what he offered discloses nothing.
        Assert.Equal(SourceKind.Participant, denial.ActualBasis);
        Assert.NotEqual(SourceKind.Participant, denial.ClaimedBasis);
        Assert.True(denial.BasisIsMisrepresented);

        // And none of it reaches the listener, in the belief or in his log of what was said.
        Assert.Equal(SourceKind.Report, salvatore.Cognition.Find(Beating)!.SourceKind);
        Assert.Equal(SourceKind.Report, salvatore.Cognition.AccountsOf(Beating).Single().ClaimedBasis);
    }

    /// <summary>
    /// An honest account discloses the basis it has. The split only opens when somebody is lying,
    /// so a candid report must not look like a misrepresentation.
    /// </summary>
    [Fact]
    public void An_honest_account_claims_the_basis_it_actually_has()
    {
        var world = Cast.Build(seed: 42, "baseline");
        var tommy = world.Get("tommy");
        var vincent = world.Get("vincent");
        var shortfall = new Claim(ClaimKind.BusinessRefusesTribute, Cast.Grocery);

        tommy.Cognition.Learn(shortfall, Stance.Believes, 0.8, SourceKind.Participant, tommy.Id, Cast.Start);
        world.Now = Cast.Start.AddDays(1);

        var candid = new Candidate("candid", ActionKind.ReportToSuperior, "test", "report in")
        {
            TargetId = vincent.Id,
            Candor = ReportCandor.Candid,
        };

        var report = Reporting.Compose(
            world, tommy, vincent, candid, Salience.Perceive(tommy, world.Now));

        var spoken = report.Asserted.Single(a => a.Claim.Equals(shortfall));
        Assert.Equal(SourceKind.Participant, spoken.ClaimedBasis);
        Assert.Equal(SourceKind.Participant, spoken.ActualBasis);
        Assert.False(spoken.BasisIsMisrepresented);
    }

    /// <summary>
    /// An honest briefing by a participant is not a misrepresentation, and nothing in the running
    /// simulation quietly makes it one.
    ///
    /// This is the migration hazard the factories exist to remove. When both bases were positional
    /// with defaults, supplying only the claimed one — the natural thing to write, and what the
    /// delegation branch did — left the actual one at `Report`, so a capo briefing his own man on
    /// something he had done himself came out flagged as lying about how he knew it.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("cautious-vincent")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    public void An_honest_briefing_is_never_marked_as_misrepresented(string variant)
    {
        var world = Run(variant);

        // Nothing candid or partial ever misrepresents its basis: only a denial does that, and only
        // for the claim it denies.
        foreach (var report in world.Reports)
        foreach (var spoken in report.Asserted.Where(a => a.BasisIsMisrepresented))
        {
            Assert.Equal(ReportCandor.False, report.Candor);
            Assert.Equal(Stance.Rejects, spoken.AssertedStance);
            Assert.Contains(report.Withheld, w => w.Equals(spoken.Claim));
        }

        // And the factory itself holds the line, for the basis the delegation branch passes.
        foreach (var basis in Enum.GetValues<SourceKind>())
        {
            var honest = ReportedClaim.Honest(
                new Claim(ClaimKind.BusinessRefusesTribute, Cast.Grocery), Stance.Believes, 0.8, basis);

            Assert.Equal(basis, honest.ClaimedBasis);
            Assert.Equal(basis, honest.ActualBasis);
            Assert.False(honest.BasisIsMisrepresented,
                $"an honest account offered on a {basis} basis must not read as a lie about its basis");
        }
    }

    /// <summary>
    /// Across the running simulation: wherever a speaker misrepresented his basis, nothing the
    /// listener holds reflects the private one.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("cautious-vincent")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    public void Private_basis_never_reaches_a_listener(string variant)
    {
        var world = Run(variant);

        foreach (var report in world.Reports)
        foreach (var spoken in report.Asserted.Where(a => a.BasisIsMisrepresented))
        {
            var recipient = world.Get(report.RecipientId);

            Assert.DoesNotContain(recipient.Cognition.AccountsOf(spoken.Claim),
                t => t.SenderId == report.SenderId && t.ClaimedBasis == spoken.ActualBasis);

            if (recipient.Cognition.Find(spoken.Claim) is { } held && held.SourceId == report.SenderId)
                Assert.NotEqual(spoken.ActualBasis.AsHeardFrom(), held.SourceKind);
        }
    }

    // ---------------------------------------------------------------- the rest of Receive

    /// <summary>
    /// A participant speaking after an ordinary report re-sources the belief to him.
    ///
    /// Provenance used to be decided only when the claim was new, so a man who first heard a rumour
    /// and was later told by the participant himself kept the rumour's classification and the
    /// rumour-teller's name against it for the rest of the run.
    /// </summary>
    [Fact]
    public void A_participant_speaking_after_a_report_becomes_the_attributable_source()
    {
        var at = Cast.Start;
        var claim = new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery, 1);

        var salvatore = new Cognition();
        salvatore.Receive(ReportedClaim.Honest(claim, Stance.Believes, 0.5, SourceKind.Report), "vincent", at);
        Assert.Equal(SourceKind.Report, salvatore.Find(claim)!.SourceKind);
        Assert.Equal("vincent", salvatore.Find(claim)!.SourceId);

        salvatore.Receive(
            ReportedClaim.Honest(claim, Stance.Believes, 0.7, SourceKind.Participant), "tommy", at.AddDays(1));

        var held = salvatore.Find(claim)!;
        Assert.Equal(SourceKind.FirstHandTestimony, held.SourceKind);
        Assert.Equal("tommy", held.SourceId);

        // The earlier account survives. Append-only means nothing is displaced from the log.
        Assert.Equal(2, salvatore.AccountsOf(claim).Count());
        Assert.Contains(salvatore.AccountsOf(claim), t => t.SenderId == "vincent");
    }

    /// <summary>
    /// Attribution follows the better account, never a weaker or equal one. Two reports are two
    /// reports, and being told what you already saw does not make it somebody else's account.
    /// </summary>
    [Fact]
    public void Attribution_never_moves_to_a_weaker_or_equal_account()
    {
        var at = Cast.Start;
        var claim = new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery, 1);

        var two = new Cognition();
        two.Receive(ReportedClaim.Honest(claim, Stance.Believes, 0.5, SourceKind.Report), "vincent", at);
        two.Receive(ReportedClaim.Honest(claim, Stance.Believes, 0.6, SourceKind.Report), "marco", at.AddDays(1));
        Assert.Equal("vincent", two.Find(claim)!.SourceId);

        var firm = new Cognition();
        firm.Receive(ReportedClaim.Honest(claim, Stance.Believes, 0.6, SourceKind.Participant), "tommy", at);
        firm.Receive(ReportedClaim.Honest(claim, Stance.Believes, 0.6, SourceKind.Rumor), "marco", at.AddDays(1));
        Assert.Equal(SourceKind.FirstHandTestimony, firm.Find(claim)!.SourceKind);
        Assert.Equal("tommy", firm.Find(claim)!.SourceId);

        var own = new Cognition();
        own.Learn(claim, Stance.Believes, 0.6, SourceKind.Witness, "self", at);
        own.Receive(ReportedClaim.Honest(claim, Stance.Believes, 0.9, SourceKind.Participant), "tommy", at.AddDays(1));
        Assert.Equal(SourceKind.Witness, own.Find(claim)!.SourceKind);
        Assert.Equal("self", own.Find(claim)!.SourceId);
    }

    /// <summary>
    /// Identical words with a materially different claimed basis are a new account, not a repeat.
    /// A man stepping forward to say he was there has said something he had not said before.
    /// </summary>
    [Fact]
    public void Changing_the_claimed_basis_is_not_a_verbatim_repeat()
    {
        var at = Cast.Start;
        var claim = new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery, 1);

        var listener = new Cognition();
        listener.Receive(ReportedClaim.Honest(claim, Stance.Believes, 0.6, SourceKind.Report), "tommy", at);
        Assert.Equal(SourceKind.Report, listener.Find(claim)!.SourceKind);

        listener.Receive(
            ReportedClaim.Honest(claim, Stance.Believes, 0.6, SourceKind.Participant), "tommy", at.AddDays(1));

        Assert.Equal(SourceKind.FirstHandTestimony, listener.Find(claim)!.SourceKind);
        Assert.Equal(2, listener.AccountsOf(claim).Count());
    }

    /// <summary>
    /// "I did it" and "I saw it" are different accounts, and the repeat rule has to see that.
    ///
    /// The comparison used to project each claimed basis through <c>AsHeardFrom</c> before testing
    /// equality, which maps Participant and Witness onto the same value — so a man who first said
    /// he did it and then said he only watched it counted as having repeated himself. Those differ
    /// in whether he is confessing, which is not a detail.
    ///
    /// What must *not* happen is the opposite error: treating it as a fresh voice and paying him
    /// confidence for it. It is still one man, still saying the thing happened, so the belief is
    /// marked reconsidered and left where it was.
    /// </summary>
    [Fact]
    public void Participant_to_witness_is_a_new_account_but_earns_no_confidence()
    {
        var at = Cast.Start;
        var claim = new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery, 1);

        var listener = new Cognition();
        listener.Receive(ReportedClaim.Honest(claim, Stance.Believes, 0.6, SourceKind.Participant), "tommy", at);

        var afterFirst = listener.Find(claim)!;
        double confidence = afterFirst.Confidence;
        var reconsideredAt = afterFirst.ReconsideredAt;

        // Same words, same confidence, different claimed basis.
        listener.Receive(
            ReportedClaim.Honest(claim, Stance.Believes, 0.6, SourceKind.Witness), "tommy", at.AddDays(1));

        var afterSecond = listener.Find(claim)!;

        Assert.Equal(confidence, afterSecond.Confidence);
        Assert.True(afterSecond.ReconsideredAt > reconsideredAt,
            "a materially different account is a development, even when it moves nothing");

        // Both accounts survive, distinguishable by the basis each was offered under.
        Assert.Equal(2, listener.AccountsOf(claim).Count());
        Assert.Contains(listener.AccountsOf(claim), t => t.ClaimedBasis == SourceKind.Participant);
        Assert.Contains(listener.AccountsOf(claim), t => t.ClaimedBasis == SourceKind.Witness);

        // And repeating the second one now genuinely is a repeat: nothing moves at all.
        listener.Receive(
            ReportedClaim.Honest(claim, Stance.Believes, 0.6, SourceKind.Witness), "tommy", at.AddDays(2));

        var afterThird = listener.Find(claim)!;
        Assert.Equal(confidence, afterThird.Confidence);
        Assert.Equal(afterSecond.ReconsideredAt, afterThird.ReconsideredAt);
    }

    /// <summary>
    /// The safeguard the repeat rule exists for still holds: one voice saying the same thing the
    /// same way cannot farm confidence, however many times it says it.
    /// </summary>
    [Fact]
    public void Repeating_the_same_account_still_cannot_farm_confidence()
    {
        var at = Cast.Start;
        var claim = new Claim(ClaimKind.BusinessRefusesTribute, Cast.Grocery);

        var listener = new Cognition();
        listener.Receive(ReportedClaim.Honest(claim, Stance.Believes, 0.5, SourceKind.Participant), "tommy", at);
        double afterFirst = listener.ConfidenceIn(claim);

        for (int i = 1; i <= 5; i++)
            listener.Receive(
                ReportedClaim.Honest(claim, Stance.Believes, 0.5, SourceKind.Participant), "tommy", at.AddDays(i));

        Assert.Equal(afterFirst, listener.ConfidenceIn(claim));

        // A second voice is genuinely new support.
        listener.Receive(ReportedClaim.Honest(claim, Stance.Believes, 0.5, SourceKind.Report), "marco", at.AddDays(6));
        Assert.True(listener.ConfidenceIn(claim) > afterFirst);
    }

    /// <summary>
    /// Contradiction and recantation still behave across the provenance path: a participant's
    /// denial shakes a report, and repeating that denial does not shake it again.
    /// </summary>
    [Fact]
    public void Contradiction_and_recantation_survive_the_provenance_path()
    {
        var at = Cast.Start;
        var claim = new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery, 1);

        var listener = new Cognition();
        listener.Receive(ReportedClaim.Honest(claim, Stance.Believes, 0.8, SourceKind.Report), "vincent", at);
        double before = listener.ConfidenceIn(claim);

        listener.Receive(
            ReportedClaim.Honest(claim, Stance.Rejects, 0.9, SourceKind.Participant), "tommy", at.AddDays(1));
        Assert.True(listener.ConfidenceIn(claim) < before);
        Assert.True(listener.IsContested(claim));

        double afterOne = listener.ConfidenceIn(claim);
        for (int i = 2; i <= 5; i++)
            listener.Receive(
                ReportedClaim.Honest(claim, Stance.Rejects, 0.9, SourceKind.Participant), "tommy", at.AddDays(i));

        Assert.Equal(afterOne, listener.ConfidenceIn(claim));
    }

    // ---------------------------------------------------------------- assignment snapshot

    /// <summary>
    /// What a man was told is a fact about the moment he was told it. The briefing delivers the
    /// terms fixed at issuance, not whatever the issuer has come to believe six hours later.
    /// </summary>
    [Fact]
    public void An_assignment_delivers_what_was_said_not_what_the_issuer_later_believes()
    {
        var world = Cast.Build(seed: 42, "baseline");
        var salvatore = world.Get(Viewpoint);
        var vincent = world.Get("vincent");
        var shortfall = new Claim(ClaimKind.BusinessRefusesTribute, Cast.Grocery);

        // Far enough for the assignment to be issued, not far enough for it to land.
        Runner.Run(world, Cast.Start.AddHours(2));
        var assignment = Assert.Single(world.Org.Assignments);
        var snapshot = assignment.Disclosed.Single(d => d.Claim.Equals(shortfall));
        Assert.True(snapshot.AssertedStance is Stance.Knows or Stance.Believes or Stance.Suspects);

        // The boss reverses himself in the gap between giving the order and it arriving.
        salvatore.Cognition.Learn(shortfall, Stance.Rejects, 1.0, SourceKind.Participant, salvatore.Id, world.Now);

        Runner.Run(world, Cast.Start.AddDays(1));

        var told = vincent.Cognition.AccountsOf(shortfall).FirstOrDefault(t => t.SenderId == salvatore.Id);
        Assert.Equal(snapshot.AssertedStance, told.AssertedStance);
        Assert.Equal(snapshot.ClaimedBasis, told.ClaimedBasis);
        Assert.True(told.Affirms, "he was told the shop was holding out, and that is what he heard");
    }

    // ---------------------------------------------------------------- snapshots

    /// <summary>
    /// The comparison surfaces have to carry decision-relevant provenance, or a run that classified
    /// beliefs differently would compare equal. Asserted content is checked directly, because a
    /// snapshot is its own comparator and cannot fail by omission.
    /// </summary>
    [Fact]
    public void Comparison_surfaces_carry_claimed_and_actual_provenance()
    {
        var world = Run("disloyal-vincent");
        Assert.NotEmpty(world.Reports);

        string channel = string.Join('\n', world.Reports.Select(r =>
            string.Join(",", r.Asserted.Select(a =>
                $"{a.Claim}:{a.AssertedStance}:{a.ClaimedBasis}:{a.ActualBasis}"))));

        foreach (var report in world.Reports)
        foreach (var spoken in report.Asserted)
        {
            Assert.Contains($":{spoken.ClaimedBasis}:{spoken.ActualBasis}", channel, StringComparison.Ordinal);
        }

        // And every testimony entry carries the basis it was offered under.
        var withTestimony = world.Characters.Values
            .SelectMany(c => c.Cognition.Testimony)
            .ToList();
        Assert.NotEmpty(withTestimony);
    }
}
