using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Scenario;
using CrimeSim.Session;
using CrimeSim.Sim;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>
/// Milestone 012: a boss who cannot attribute a shortfall may come to suspect it has another cause,
/// and the mark-selection defect that meant nobody was ever offered a second business to check.
///
/// The two findings are independent and each half is tested on its own terms before the natural-run
/// tests at the bottom show them working together: "Answered during implementation" in
/// <c>docs/CURRENT_MILESTONE.md</c> is explicit that the inference gives a reason to look and the
/// generator fix gives him something to look at, and either alone leaves the bakery untouched.
/// </summary>
public sealed class ShortfallAttributionTests
{
    private static readonly Claim Attributed = new(ClaimKind.BusinessRefusesTribute, Cast.Grocery);
    private static readonly Claim Gap = new(ClaimKind.UnattributedShortfall, Cast.Harbour);

    // ================================================================ the inference itself

    /// <summary>
    /// The headline case, driven through the real contradiction path rather than a hand-built
    /// <c>Contested</c> flag: <see cref="Cognition.Receive"/> is what actually sets it, and a test
    /// that set the flag directly would not be exercising the same fact the production code reads.
    /// </summary>
    [Fact]
    public void He_suspects_a_gap_once_his_attribution_is_contradicted_and_the_condition_is_live()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var salvatore = world.Get("salvatore");
        var vincent = world.Get("vincent");

        // Sanity: the fixture starts him uncontradicted and the condition already live.
        Assert.False(salvatore.Cognition.IsContested(Attributed));
        Assert.True(world.Org.Condition(OrgCondition.RevenueLoss) >= Organization.SignificantRevenueLoss);

        var receipt = salvatore.Cognition.Receive(
            ReportedClaim.Honest(Attributed, Stance.Rejects, 0.9, SourceKind.Participant), vincent.Id, world.Now);
        Assert.NotNull(receipt.Conflict);
        Assert.True(salvatore.Cognition.IsContested(Attributed));

        Inference.Reconsider(world, salvatore, world.Now);

        var held = salvatore.Cognition.Find(Gap);
        Assert.NotNull(held);
        Assert.Equal(Stance.Suspects, held!.Stance);
        Assert.Equal(SourceKind.Inference, held.SourceKind);
        Assert.Equal(salvatore.Id, held.SourceId);
    }

    /// <summary>
    /// Ruling 2's test requirement in the implementation plan: it does not fire while he is
    /// uncontradicted. The fixture's starting state is exactly this case.
    /// </summary>
    [Fact]
    public void He_does_not_suspect_a_gap_while_uncontradicted()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var salvatore = world.Get("salvatore");

        Inference.Reconsider(world, salvatore, world.Now);

        Assert.Null(salvatore.Cognition.Find(Gap));
    }

    /// <summary>Ruling 5's gate: a condition that has fallen quiet gives him nothing to doubt.</summary>
    [Fact]
    public void He_does_not_suspect_a_gap_while_the_condition_has_gone_quiet()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var salvatore = world.Get("salvatore");
        var vincent = world.Get("vincent");

        salvatore.Cognition.Receive(
            ReportedClaim.Honest(Attributed, Stance.Rejects, 0.9, SourceKind.Participant), vincent.Id, world.Now);
        Assert.True(salvatore.Cognition.IsContested(Attributed));

        world.Org.Conditions[OrgCondition.RevenueLoss] = Organization.SignificantRevenueLoss - 0.05;

        Inference.Reconsider(world, salvatore, world.Now);

        Assert.Null(salvatore.Cognition.Find(Gap));
    }

    /// <summary>
    /// Ruling 2: he ends up suspecting that <em>something</em> is refusing, never which business —
    /// checked against both shops by name, not merely against the claim's own subject.
    /// </summary>
    [Fact]
    public void The_suspicion_names_the_domain_and_never_a_shop()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var salvatore = world.Get("salvatore");
        var vincent = world.Get("vincent");

        salvatore.Cognition.Receive(
            ReportedClaim.Honest(Attributed, Stance.Rejects, 0.9, SourceKind.Participant), vincent.Id, world.Now);
        Inference.Reconsider(world, salvatore, world.Now);

        var held = salvatore.Cognition.Find(Gap);
        Assert.NotNull(held);
        Assert.Equal(Cast.Harbour, held!.Claim.Subject);
        Assert.DoesNotContain(Cast.Grocery, held.Claim.ToString());
        Assert.DoesNotContain(Cast.Bakery, held.Claim.ToString());
    }

    /// <summary>
    /// Ruling 3: a defeasible conclusion, revisable exactly like the existing policy-breach inference
    /// — through <see cref="Cognition.Revise"/>, which refuses anything that is not the holder's own
    /// reading.
    /// </summary>
    [Fact]
    public void The_suspicion_is_his_own_reading_and_he_can_think_better_of_it()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var salvatore = world.Get("salvatore");
        var vincent = world.Get("vincent");

        salvatore.Cognition.Receive(
            ReportedClaim.Honest(Attributed, Stance.Rejects, 0.9, SourceKind.Participant), vincent.Id, world.Now);
        Inference.Reconsider(world, salvatore, world.Now);

        var revised = salvatore.Cognition.Revise(Gap, 0.05, salvatore.Id, world.Now);
        Assert.NotNull(revised);
        Assert.Equal(0.05, revised!.Confidence, 6);
    }

    /// <summary>
    /// Only the organisation's leadership tracks the condition it is drawn from — a capo does not
    /// independently arrive at the same suspicion just because he is in the same organisation.
    /// </summary>
    [Fact]
    public void Only_the_boss_forms_the_suspicion()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var vincent = world.Get("vincent");

        // Stage the same contradiction shape against Vincent directly, so the only thing standing
        // between him and the suspicion is the boss-only gate.
        var contested = new Claim(ClaimKind.BusinessRefusesTribute, Cast.Bakery);
        vincent.Cognition.Learn(contested, Stance.Believes, 0.7, SourceKind.Report, "salvatore", world.Now);
        vincent.Cognition.Receive(
            ReportedClaim.Honest(contested, Stance.Rejects, 0.9, SourceKind.Participant), "tommy", world.Now);
        Assert.True(vincent.Cognition.IsContested(contested));

        Inference.Reconsider(world, vincent, world.Now);

        Assert.Null(vincent.Cognition.Find(Gap));
    }

    /// <summary>
    /// Re-deriving the same conclusion on every wake must not read as fresh content — the same
    /// re-arming guard the existing policy-breach inference already relies on.
    /// </summary>
    [Fact]
    public void Reconsidering_again_does_not_move_the_reconsideration_stamp_without_new_grounds()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var salvatore = world.Get("salvatore");
        var vincent = world.Get("vincent");

        salvatore.Cognition.Receive(
            ReportedClaim.Honest(Attributed, Stance.Rejects, 0.9, SourceKind.Participant), vincent.Id, world.Now);
        Inference.Reconsider(world, salvatore, world.Now);
        var first = salvatore.Cognition.Find(Gap)!;

        Inference.Reconsider(world, salvatore, world.Now.AddDays(1));
        var second = salvatore.Cognition.Find(Gap)!;

        Assert.Equal(first.AcquiredAt, second.AcquiredAt);
        Assert.Equal(first.Confidence, second.Confidence, 6);

        // The claim this test is named for, checked directly rather than through two proxies that
        // both happen to hold when it does. AcquiredAt and Confidence stay put whether or not the
        // stamp moved — Learn's override branch preserves AcquiredAt on any update, and an identical
        // re-derivation obviously reproduces the same Confidence — so neither could have caught a
        // guard that let the second call reach Learn again on the world.Now.AddDays(1) call: only
        // ReconsideredAt would move, and nothing above was reading it.
        Assert.Equal(first.ReconsideredAt, second.ReconsideredAt);
        Assert.Null(second.LastReconsideredAt);
    }

    // ================================================================ mark selection

    /// <summary>
    /// Unchanged behaviour: a named refuser still requires exactly that claim, exactly as before this
    /// milestone.
    /// </summary>
    [Fact]
    public void A_named_refuser_is_still_the_ordinary_route()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var vincent = world.Get("vincent");
        vincent.Cognition.Learn(Attributed, Stance.Believes, 0.75, SourceKind.Report, "salvatore", world.Now);

        var candidates = Generators.GenerateAll(Context(world, vincent))
            .Where(c => c.Strategy == StrategyKind.SecureTribute && c.Kind == ActionKind.StartStrategy)
            .ToList();

        Assert.NotEmpty(candidates);
        Assert.All(candidates, c => Assert.Equal(Cast.Grocery, c.TargetId));
        Assert.All(candidates, c => Assert.Equal(new[] { Attributed }, c.RequiredKnowledge));
    }

    /// <summary>
    /// The defect itself, pinned directly: with no named refuser and no suspicion of a gap, nothing
    /// is offered for any business — not even the dead, always-rejected candidate the fallback used
    /// to produce. Reverting the fix should make this fail by finding a candidate here.
    /// </summary>
    [Fact]
    public void With_no_named_refuser_and_no_suspicion_nothing_is_proposed()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var vincent = world.Get("vincent");

        var candidates = Generators.GenerateAll(Context(world, vincent))
            .Where(c => c.Strategy == StrategyKind.SecureTribute && c.Kind == ActionKind.StartStrategy);

        Assert.Empty(candidates);
    }

    /// <summary>
    /// The fix. Holding the gap claim — never a business claim — is what makes the second business
    /// occur to him at all, and the requirement on the resulting candidate names the gap rather than
    /// inventing a fact about the shop he has not established.
    /// </summary>
    [Fact]
    public void A_suspected_gap_offers_the_first_business_he_has_not_ruled_out()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var vincent = world.Get("vincent");
        vincent.Cognition.Learn(Gap, Stance.Suspects, 0.3, SourceKind.Report, "salvatore", world.Now);

        var candidates = Generators.GenerateAll(Context(world, vincent))
            .Where(c => c.Strategy == StrategyKind.SecureTribute && c.Kind == ActionKind.StartStrategy)
            .ToList();

        Assert.NotEmpty(candidates);
        // Neither business is ruled out yet, so the alphabetically first is the one visible-target
        // ordering picks — pinned by the same determinism concern Cast.Bakery's own doc comment names.
        Assert.All(candidates, c => Assert.Equal(Cast.Grocery, c.TargetId));
        Assert.All(candidates, c => Assert.Equal(new[] { Gap }, c.RequiredKnowledge));
    }

    /// <summary>
    /// The case the milestone was for: the grocery is already resolved, so the suspicion turns up the
    /// bakery — the business that, before this milestone, appeared zero times in the full decision
    /// trace, rejected candidates included.
    /// </summary>
    [Fact]
    public void A_suspected_gap_skips_a_business_already_concluded_to_be_paying()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var vincent = world.Get("vincent");
        vincent.Cognition.Learn(Gap, Stance.Suspects, 0.3, SourceKind.Report, "salvatore", world.Now);
        vincent.Cognition.Learn(new Claim(ClaimKind.BusinessRefusesTribute, Cast.Grocery),
            Stance.Rejects, 0.9, SourceKind.Participant, vincent.Id, world.Now);

        var candidates = Generators.GenerateAll(Context(world, vincent))
            .Where(c => c.Strategy == StrategyKind.SecureTribute && c.Kind == ActionKind.StartStrategy)
            .ToList();

        Assert.NotEmpty(candidates);
        Assert.All(candidates, c => Assert.Equal(Cast.Bakery, c.TargetId));
        Assert.All(candidates, c => Assert.Equal(new[] { Gap }, c.RequiredKnowledge));
    }

    /// <summary>Once every visible business is ruled out, there is nothing left to check.</summary>
    [Fact]
    public void A_suspected_gap_proposes_nothing_once_every_business_is_ruled_out()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var vincent = world.Get("vincent");
        vincent.Cognition.Learn(Gap, Stance.Suspects, 0.3, SourceKind.Report, "salvatore", world.Now);
        vincent.Cognition.Learn(new Claim(ClaimKind.BusinessRefusesTribute, Cast.Grocery),
            Stance.Rejects, 0.9, SourceKind.Participant, vincent.Id, world.Now);
        vincent.Cognition.Learn(new Claim(ClaimKind.BusinessRefusesTribute, Cast.Bakery),
            Stance.Rejects, 0.9, SourceKind.Participant, vincent.Id, world.Now);

        var candidates = Generators.GenerateAll(Context(world, vincent))
            .Where(c => c.Strategy == StrategyKind.SecureTribute && c.Kind == ActionKind.StartStrategy);

        Assert.Empty(candidates);
    }

    // ================================================================ the two findings together

    /// <summary>
    /// The route from suspicion to a named target: the existing assignment-disclosure channel, not a
    /// fact invented to close the gap. A fresh assignment carries the boss's own stance and confidence
    /// on the shortfall — never firmed up the way a named refusal is — and the capo comes to hold it
    /// through the same <see cref="Cognition.Receive"/> path any other briefing uses.
    /// </summary>
    [Fact]
    public void The_assignment_channel_carries_the_boss_suspicion_to_the_capo()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var salvatore = world.Get("salvatore");
        var vincent = world.Get("vincent");

        salvatore.Cognition.Receive(
            ReportedClaim.Honest(Attributed, Stance.Rejects, 0.9, SourceKind.Participant), vincent.Id, world.Now);
        Inference.Reconsider(world, salvatore, world.Now);
        Assert.NotNull(salvatore.Cognition.Find(Gap));

        // Drive the real scheduler through to the leadership review Cast.Build already queues,
        // rather than reaching for the institutional step directly. Terminates deterministically:
        // the condition is live and Vincent holds the office, so the first OrgReview creates it.
        for (int i = 0; i < 10 && world.Org.Assignments.Count == 0; i++)
            Runner.Step(world, world.Now.AddYears(1), controlledCharacterId: null);

        var assignment = world.Org.Assignments.Single();
        Assert.Contains(assignment.Disclosed, d => d.Claim.Equals(Gap) && d.AssertedStance == Stance.Suspects);
    }

    /// <summary>
    /// Ruling 6, checked by driving a session rather than argued by construction: whatever becomes
    /// available to the capo is available to a player controlling him through the same candidate set.
    /// <c>watchful-boss</c> is one of the two variants at seed 42 where the attribution is actually
    /// contradicted before day 90 — see the milestone archive for the measured figures.
    ///
    /// Resolved automatically at every pause rather than by picking the first option: <c>Available</c>
    /// is deliberately sorted by candidate id rather than by rank (milestone 009, ruling 5), so
    /// "always take the first one offered" drives an entirely different, lower-ranked history and
    /// would not reach the bakery at all. <see cref="SimulationSession.ResolveAutomatically"/> commits
    /// to whichever option the character himself would have preferred — the same run this variant's
    /// batch figures in the archive are measured from — while this test still records what was
    /// actually offered at each pause, which is the fact ruling 6 asks to have checked.
    /// </summary>
    [Fact]
    public void A_player_controlling_the_capo_is_offered_the_bakery_once_he_suspects_a_gap()
    {
        var session = SimulationSession.Start(42, "watchful-boss", "vincent", "vincent");
        var offered = new List<string>();

        session.AdvanceTo(Cast.Start.AddDays(90));
        while (session.Status == SessionStatus.AwaitingChoice)
        {
            offered.AddRange(session.Pending!.Options.Select(o => o.Description));
            session.ResolveAutomatically();
        }

        Assert.Contains(offered, o => o.Contains("Dorato's bakery", StringComparison.Ordinal));
    }

    // ================================================================ helpers

    private static GeneratorContext Context(World world, Character actor)
    {
        var org = world.Org;
        var office = org.OfficeFor(actor.Id);
        return new GeneratorContext(
            actor.View,
            Salience.Perceive(actor, world.Now),
            new Agenda(AgendaKind.DischargeResponsibility, "restore the harbour tribute", "test", Cast.Harbour),
            world.Now,
            new ScheduledEvent
            {
                Id = 0,
                Time = world.Now,
                Kind = EventKind.RoleReview,
                OwnerId = actor.Id,
                Cause = "test",
            },
            MyOffice: office,
            MyAssignment: null,
            KnownPolicies: Array.Empty<Policy>(),
            SuperiorId: null,
            SubordinateIds: Array.Empty<string>(),
            OrgMemberIds: Array.Empty<string>(),
            AcquaintedIds: Array.Empty<string>(),
            ReportsSent: Array.Empty<Report>(),
            RequestsMade: Array.Empty<InformationRequest>(),
            VisibleTargets: world.BusinessesIn(Cast.Harbour).Select(b => b.Id).ToList());
    }
}
