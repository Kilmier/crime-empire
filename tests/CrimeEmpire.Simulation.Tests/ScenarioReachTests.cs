using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Scenario;
using CrimeSim.Sim;
using CrimeSim.Trace;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>
/// Milestone 007. Three things had to become true together for the mechanisms built in 004–006 to
/// show up in a natural scenario, and each of the three is pinned here.
///
/// First, concealment stops being re-priced as a fresh gain every time it is repeated: what a report
/// buys is the protection it did not already have. Second, being re-told something you have since
/// disproved counts as a disagreement rather than as somebody clearing his throat. Third, the fixture
/// has enough room for a second organisational review, which is what puts a briefing in front of a
/// capo who has personally watched the claim in it become false.
///
/// The distinction the milestone turns on, and which its tests must keep separate:
/// <b>decision-relevant</b> means the trust the conflict moved contributes a non-zero, named
/// component to a later decision's score, and that contribution vanishes if the conflict costs
/// nothing. <b>Choice-changing</b> would mean it flips a winner. Only the first is claimed, and
/// <see cref="The_conflict_changes_what_a_later_decision_is_scored_on"/> is the test that establishes
/// it. Nothing here was tuned to make anything win.
/// </summary>
public sealed class ScenarioReachTests
{
    private static World Run(string variant) => Run(variant, 42);

    private static World Run(string variant, int seed)
    {
        var world = Cast.Build(seed, variant);
        Runner.Run(world, Cast.Start.AddDays(90));
        return world;
    }

    // ================================================================ D1 — prior disclosure state

    private static readonly Claim Breach =
        new(ClaimKind.PersonBreachedPolicy, "vincent", "no-violence-harbour", 11);

    private static Report Sent(
        long id,
        string recipient,
        DateTime at,
        IEnumerable<ReportedClaim>? asserted = null,
        IEnumerable<Claim>? withheld = null)
        => new(id, "vincent", recipient, at, ReportCandor.Partial,
               (asserted ?? Array.Empty<ReportedClaim>()).ToList(),
               (withheld ?? Array.Empty<Claim>()).ToList(),
               "test");

    private static PriorDisclosureState Prior(params Report[] sent)
        => Reporting.PriorDisclosure(sent, "salvatore", Breach);

    /// <summary>
    /// Nothing said is nothing said — including a claim the three-item cap dropped, which appears in
    /// neither list because he decided nothing about it. Cap-omission is not concealment and must not
    /// be paid for as though it were, which is the same distinction
    /// <see cref="Reporting.LastAddressed"/> already draws for eligibility.
    /// </summary>
    [Fact]
    public void A_claim_he_has_never_addressed_to_this_man_is_never_addressed()
    {
        Assert.Equal(PriorDisclosureState.NeverAddressed, Prior());

        // A report to him that simply did not reach this claim.
        Assert.Equal(PriorDisclosureState.NeverAddressed,
            Prior(Sent(1, "salvatore", Cast.Start,
                asserted: new[] { ReportedClaim.Honest(
                    new Claim(ClaimKind.TributeCollected, Cast.Grocery),
                    Stance.Believes, 0.8, SourceKind.Participant) })));
    }

    [Fact]
    public void Keeping_it_back_is_recorded_as_having_kept_it_back()
        => Assert.Equal(PriorDisclosureState.Withheld,
            Prior(Sent(1, "salvatore", Cast.Start, withheld: new[] { Breach })));

    [Fact]
    public void Telling_him_is_recorded_as_having_told_him()
        => Assert.Equal(PriorDisclosureState.DisclosedAffirmatively,
            Prior(Sent(1, "salvatore", Cast.Start,
                asserted: new[] { ReportedClaim.Honest(Breach, Stance.Believes, 0.8, SourceKind.Participant) })));

    /// <summary>
    /// A candid rejection and a deceptive denial reach the same state, because the recipient ends up
    /// holding the same thing: this man has told him it is not so.
    ///
    /// Detected from the asserted stance rather than from <see cref="Report.Candor"/>, and this is the
    /// test that pins the difference. Keying on candour would have caught the liar and let the sincere
    /// retraction go on buying protection it had already spent.
    /// </summary>
    [Fact]
    public void A_candid_rejection_and_a_deceptive_denial_reach_the_same_state()
    {
        // Sincere: he has come to reject it and says so. The report's candour is Partial here, so a
        // rule reading Candor could not distinguish this from an ordinary omission.
        var honest = Sent(1, "salvatore", Cast.Start,
            asserted: new[] { ReportedClaim.Honest(Breach, Stance.Rejects, 0.8, SourceKind.Participant) });

        // The deceptive shape: Compose writes a denial into both lists, because he suppressed his
        // real position and asserted its opposite.
        var lie = Sent(2, "salvatore", Cast.Start,
            asserted: new[] { ReportedClaim.Misrepresenting(
                Breach, Stance.Rejects, 0.8, SourceKind.Report, SourceKind.Participant) },
            withheld: new[] { Breach });

        Assert.Equal(PriorDisclosureState.Denied, Prior(honest));
        Assert.Equal(PriorDisclosureState.Denied, Prior(lie));
    }

    /// <summary>
    /// The most recent treatment is the one that counts, because the question is what this recipient
    /// currently has from him. Denial is therefore not absorbing: a man who denied something and then
    /// came clean has given his recipient a new position, and burying it again has to overturn one.
    ///
    /// Pinned rather than left to intuition, because milestone 006 found the obvious expectation
    /// wrong on the affirm → deny → affirm sequence and this is its mirror image.
    /// </summary>
    [Fact]
    public void Deny_then_affirm_leaves_him_holding_the_affirmation()
    {
        var denied = Sent(1, "salvatore", Cast.Start,
            asserted: new[] { ReportedClaim.Honest(Breach, Stance.Rejects, 0.8, SourceKind.Participant) });
        var affirmed = Sent(2, "salvatore", Cast.Start.AddDays(1),
            asserted: new[] { ReportedClaim.Honest(Breach, Stance.Believes, 0.8, SourceKind.Participant) });

        Assert.Equal(PriorDisclosureState.DisclosedAffirmatively, Prior(denied, affirmed));

        // And order, not argument order, decides it.
        Assert.Equal(PriorDisclosureState.DisclosedAffirmatively, Prior(affirmed, denied));
        Assert.Equal(PriorDisclosureState.Denied,
            Prior(affirmed, Sent(3, "salvatore", Cast.Start.AddDays(2),
                asserted: new[] { ReportedClaim.Honest(Breach, Stance.Rejects, 0.8, SourceKind.Participant) })));
    }

    /// <summary>
    /// Two reports in the same instant break on report id, which is allocated from world state and is
    /// monotonic. Without a total order "most recent" would depend on enumeration order, which the
    /// determinism rules forbid relying on.
    /// </summary>
    [Fact]
    public void Two_reports_in_one_instant_break_the_tie_on_report_id()
    {
        var earlier = Sent(1, "salvatore", Cast.Start, withheld: new[] { Breach });
        var later = Sent(2, "salvatore", Cast.Start,
            asserted: new[] { ReportedClaim.Honest(Breach, Stance.Believes, 0.8, SourceKind.Participant) });

        Assert.Equal(PriorDisclosureState.DisclosedAffirmatively, Prior(earlier, later));
        Assert.Equal(PriorDisclosureState.DisclosedAffirmatively, Prior(later, earlier));
    }

    /// <summary>What one man has heard says nothing about what another has heard.</summary>
    [Fact]
    public void Keeping_it_from_one_man_settles_nothing_toward_another()
    {
        var toTommy = Sent(1, "tommy", Cast.Start, withheld: new[] { Breach });

        Assert.Equal(PriorDisclosureState.Withheld,
            Reporting.PriorDisclosure(new[] { toTommy }, "tommy", Breach));
        Assert.Equal(PriorDisclosureState.NeverAddressed,
            Reporting.PriorDisclosure(new[] { toTommy }, "salvatore", Breach));
    }

    // ================================================================ D1 — what it is worth

    /// <summary>
    /// The whole table, priced through the production scorer.
    ///
    /// Stakes are exactly 1.0: the claim is his own act, held at full confidence, and Participant is
    /// exempt from the Suspicious discount — so these figures are the coefficients themselves and a
    /// change to any of them shows up here directly.
    ///
    /// Read the first and last rows together. A first denial still scores 1.9, exactly as before this
    /// milestone; a repeat of what he has already denied scores nothing. Nothing was retuned — 1.9 was
    /// separated into the 1.5 that silence buys and the 0.4 a denial adds on top of it.
    /// </summary>
    [Theory]
    [InlineData(ReportCandor.Partial, PriorDisclosureState.NeverAddressed, 1.5)]
    [InlineData(ReportCandor.Partial, PriorDisclosureState.Withheld, 0.0)]
    [InlineData(ReportCandor.Partial, PriorDisclosureState.DisclosedAffirmatively, 0.0)]
    [InlineData(ReportCandor.Partial, PriorDisclosureState.Denied, 0.0)]
    [InlineData(ReportCandor.False, PriorDisclosureState.NeverAddressed, 1.9)]
    [InlineData(ReportCandor.False, PriorDisclosureState.Withheld, 0.4)]
    [InlineData(ReportCandor.False, PriorDisclosureState.DisclosedAffirmatively, 0.4)]
    [InlineData(ReportCandor.False, PriorDisclosureState.Denied, 0.0)]
    public void Concealment_is_worth_only_the_protection_it_newly_buys(
        ReportCandor candor, PriorDisclosureState prior, double expected)
        => Assert.Equal(expected, SelfProtectionOf(candor, new SuppressedClaim(Breach, prior)), 6);

    /// <summary>
    /// Each claim's value is completed before the maximum is taken, rather than the omission and
    /// premium halves being maximised separately and added.
    ///
    /// The two differ exactly when the claim with the most at stake is not the claim with the most to
    /// gain, which is what this stages: a heavy secret he has already kept back, and a slight one he
    /// has never mentioned. Per claim, the answer is the slight one's full denial value, 1.9 × 0.4.
    /// Added maxima would take the omission half from the slight claim and the premium half from the
    /// heavy one and report 1.0 — a figure no single act of concealment buys him.
    /// </summary>
    [Fact]
    public void Protection_is_completed_per_claim_before_the_maximum_is_taken()
    {
        var slight = new Claim(ClaimKind.PersonUsedViolence, "vincent", Cast.Grocery, 12);

        double value = SelfProtectionOf(
            ReportCandor.False,
            new SuppressedClaim(Breach, PriorDisclosureState.Withheld),
            new SuppressedClaim(slight, PriorDisclosureState.NeverAddressed));

        Assert.Equal(1.9 * 0.4, value, 6);
        Assert.NotEqual(1.5 * 0.4 + 0.4 * 1.0, value, 6);
    }

    /// <summary>
    /// Staged scoring of one report candidate, returning only what its concealment was worth.
    /// Everything else about the candidate is held constant across the cases above.
    /// </summary>
    private static double SelfProtectionOf(ReportCandor candor, params SuppressedClaim[] suppressed)
    {
        var world = Cast.Build(42, "baseline");
        var vincent = world.Get("vincent");

        // Full confidence, and exempt from the Suspicious discount, so perceived stakes are exactly
        // what is written here rather than a discounted figure the assertions would have to mirror.
        vincent.Cognition.Learn(Breach, Stance.Knows, 1.0, SourceKind.Participant, vincent.Id, world.Now);
        vincent.Cognition.Learn(
            new Claim(ClaimKind.PersonUsedViolence, "vincent", Cast.Grocery, 12),
            Stance.Believes, 0.4, SourceKind.Participant, vincent.Id, world.Now);

        var candidate = new Candidate("report:salvatore", ActionKind.ReportToSuperior, "test", "report in")
        {
            TargetId = "salvatore",
            Domain = Cast.Harbour,
            Candor = candor,
            Suppressed = suppressed,
        };

        var ctx = Context(world, vincent, Array.Empty<Report>());

        return Utility.Score(
                candidate, vincent.View, vincent.Psychology, ctx.Perceived, ctx.Agenda,
                Rng.ForOccasion(world.Seed, "test|fixed"))
            .Components.Where(p => p.Name == "self-protection")
            .Sum(p => p.Value);
    }

    /// <summary>
    /// Report eligibility and marginal concealment value answer different questions, and this is the
    /// test that keeps them apart.
    ///
    /// A sender whose own position has moved since he last spoke is entitled to report again —
    /// <see cref="Reporting.NeedsConveying"/> says so, and that is right: he has something new to say.
    /// What his change of mind cannot do is make his recipient un-hear the silence he already bought.
    /// Deriving concealment value from belief timestamps would have refunded protection on every
    /// reconsideration, which is the defect this milestone exists to remove, inverted.
    /// </summary>
    [Fact]
    public void Reconsidering_a_belief_does_not_restore_protection_already_spent()
    {
        var world = Cast.Build(42, "baseline");
        var vincent = world.Get("vincent");

        vincent.Cognition.Learn(Breach, Stance.Knows, 1.0, SourceKind.Participant, vincent.Id, world.Now);

        var kept = Sent(1, "salvatore", world.Now, withheld: new[] { Breach });
        var sent = new[] { kept };

        // He revisits it a week later. The record's reconsideration stamp moves; nothing he told
        // anybody changes.
        var later = world.Now.AddDays(7);
        var revisited = vincent.Cognition.Learn(
            Breach, Stance.Knows, 1.0, SourceKind.Participant, vincent.Id, later);

        Assert.True(revisited.ReconsideredAt > kept.At, "the fixture must actually move the stamp");

        // Eligibility: yes, he may raise it again.
        Assert.True(Reporting.NeedsConveying(sent, "salvatore", revisited));

        // Value: no, the silence is already his.
        Assert.Equal(PriorDisclosureState.Withheld, Reporting.PriorDisclosure(sent, "salvatore", Breach));

        // And end to end through the generator, which is what actually assembles the candidate.
        var ctx = Context(world, vincent, sent);
        var partial = Generators.GenerateAll(ctx)
            .Single(c => c.Kind == ActionKind.ReportToSuperior && c.Candor == ReportCandor.Partial);

        Assert.Equal(
            PriorDisclosureState.Withheld,
            partial.Suppressed.Single(s => s.Claim.Equals(Breach)).Prior);
    }

    // ================================================================ D2 — repetition

    /// <summary>
    /// The three-account sequence the refined invariant describes, in one test because the three
    /// clauses only mean anything together.
    ///
    /// Identical words are inert while he has not moved; once he has independently moved, the same
    /// words are a fresh disagreement — but exactly one of them, because the conflict itself stamps
    /// the record and the next repetition finds nothing new to react to.
    /// </summary>
    [Fact]
    public void Repetition_is_inert_until_he_moves_and_then_inert_again()
    {
        var claim = new Claim(ClaimKind.BusinessRefusesTribute, Cast.Grocery);
        var account = ReportedClaim.Honest(claim, Stance.Believes, 0.75, SourceKind.Report);

        var cognition = new Cognition();
        var t0 = Cast.Start;

        // He is told it, and holds it. Nothing to disagree with yet.
        Assert.Null(cognition.Receive(account, "salvatore", t0).Conflict);

        // Told again, word for word, having not moved. Still nothing.
        Assert.Null(cognition.Receive(account, "salvatore", t0.AddDays(7)).Conflict);

        // He finds out for himself that it is not so.
        cognition.Learn(claim, Stance.Rejects, 0.9, SourceKind.Discovery, "vincent", t0.AddDays(14));

        // The same words now meet a man who holds the opposite. That is a disagreement, whoever put
        // him where he was and however unchanged the speaker's account is.
        var afterMoving = cognition.Receive(account, "salvatore", t0.AddDays(21));
        Assert.NotNull(afterMoving.Conflict);
        Assert.Equal("salvatore", afterMoving.Conflict!.Value.SpeakerId);

        // And once more, with nothing having moved in between. Inert again.
        Assert.Null(cognition.Receive(account, "salvatore", t0.AddDays(28)).Conflict);
    }

    /// <summary>
    /// The property above holds in the accepted run and is not merely stageable: Salvatore issues
    /// three identical briefings about the grocery, and they do not all count.
    /// </summary>
    [Fact]
    public void The_accepted_run_shows_a_repeated_briefing_counting_once_per_movement()
    {
        var world = Run("baseline");
        var vincent = world.Get("vincent");
        var refusing = new Claim(ClaimKind.BusinessRefusesTribute, Cast.Grocery);

        var briefings = vincent.Cognition.AccountsOf(refusing)
            .Where(t => t.SenderId == "salvatore")
            .ToList();

        var conflicts = world.AccountConflicts
            .Where(c => c.ListenerId == "vincent"
                        && c.Conflict.SpeakerId == "salvatore"
                        && c.Conflict.Claim.Equals(refusing))
            .ToList();

        Assert.True(briefings.Count > conflicts.Count,
            $"the boss said it {briefings.Count} time(s) and it counted {conflicts.Count} time(s) — " +
            "if those are equal, repetition is compounding after all");
        Assert.NotEmpty(conflicts);
    }

    // ================================================================ D3 — the fixture's reach

    /// <summary>
    /// Business ordering is determinism-relevant, not a naming coincidence.
    /// <c>FromResponsibility</c> falls back to the first visible target when the character believes
    /// no particular business is holding out, so which one sorts first decides what he considers.
    /// </summary>
    [Fact]
    public void The_grocery_is_the_first_visible_target_in_the_harbour()
    {
        var world = Cast.Build(42, "baseline");
        Assert.Equal(
            new[] { Cast.Grocery, Cast.Bakery },
            world.BusinessesIn(Cast.Harbour).Select(b => b.Id).ToArray());
    }

    /// <summary>
    /// The boss's books are short by two shops and his account of why names one. That asymmetry is
    /// deliberate: the organisational condition is objective, his explanation of it is not, and the
    /// gap between them is what leaves his capo the room to go and ask his own man instead of being
    /// handed a second errand.
    /// </summary>
    [Fact]
    public void The_boss_does_not_know_about_the_second_shop()
    {
        var world = Cast.Build(42, "baseline");
        var salvatore = world.Get("salvatore");

        Assert.False(world.Businesses[Cast.Bakery].PayingTribute);
        Assert.True(salvatore.Cognition.Holds(new Claim(ClaimKind.BusinessRefusesTribute, Cast.Grocery)));
        Assert.Null(salvatore.Cognition.Find(new Claim(ClaimKind.BusinessRefusesTribute, Cast.Bakery)));
    }

    /// <summary>
    /// The first collection cycle is unchanged by the second shop's existence: same target, same
    /// escalation, same delegation. The fixture gained room without gaining a different opening.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    [InlineData("resentful-tommy")]
    public void The_first_collection_cycle_still_runs_on_the_grocery(string variant)
    {
        var world = Run(variant);

        var firstStrategy = world.Decisions
            .Where(d => d.Chosen?.Candidate.Kind == ActionKind.StartStrategy
                        && d.Chosen.Candidate.Strategy == StrategyKind.SecureTribute)
            .Select(d => d.Chosen!.Candidate.TargetId)
            .FirstOrDefault();

        Assert.Equal(Cast.Grocery, firstStrategy);
        Assert.True(world.Businesses[Cast.Grocery].PayingTribute);
    }

    /// <summary>
    /// The second organisational review happens, which is the whole reason the second shop is there.
    /// Without a business still short, the condition falls below the review threshold on collection
    /// and the last third of the run is a boss deciding nothing.
    /// </summary>
    [Fact]
    public void The_shortfall_survives_the_first_collection_and_produces_a_second_briefing()
    {
        var world = Run("baseline");

        var briefings = world.TruthLog.Where(e => e.Kind == "assignment").ToList();
        Assert.True(briefings.Count >= 2,
            $"only {briefings.Count} assignment(s) issued — the organisational condition died with " +
            "the first collection and the scenario has no second act");
    }

    // ================================================================ the milestone's own claims

    /// <summary>
    /// The delegator's question wins in play. It has existed since milestone 006's first correction
    /// and had never been chosen in any variant, losing every time to a report that was being paid
    /// afresh for concealing what it had already concealed.
    ///
    /// Note what is asserted: that it was chosen, by the generator that offers it, against the man he
    /// sent. Not that it was chosen at a particular moment or with a particular score — nothing was
    /// tuned to make it win, and pinning the margin would invite exactly that.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    [InlineData("resentful-tommy")]
    public void The_delegator_puts_his_question_to_the_man_he_sent(string variant)
    {
        var world = Run(variant);

        Assert.Contains(world.Decisions, d =>
            d.ActorId == "vincent"
            && d.Chosen?.Candidate.Kind == ActionKind.SeekCorroboration
            && d.Chosen.Candidate.Generator == "FromDelegation"
            && d.Chosen.Candidate.TargetId == "tommy");
    }

    /// <summary>
    /// And the man answers. This is the first delegator-to-executor exchange the accepted scenario
    /// has ever produced — milestone 006 could prove the path only through a staged test.
    /// </summary>
    [Fact]
    public void And_the_executor_gives_his_delegator_an_account_of_it()
    {
        var world = Run("baseline");

        var question = world.Requests.First(q => q.AskerId == "vincent" && q.AskedId == "tommy");
        var reply = world.Reports.FirstOrDefault(r =>
            r.SenderId == "tommy" && r.RecipientId == "vincent"
            && r.AnsweringClaim is { } about && about.Equals(question.About));

        Assert.NotNull(reply);
    }

    /// <summary>
    /// <b>The milestone's success bar.</b> A perceived account conflict moved a relationship, and a
    /// decision taken afterwards is scored differently because of it.
    ///
    /// Two arms, both through the production scorer. The live arm takes the real post-conflict world
    /// and reads the component off a candidate that was actually weighed. The counterfactual arm puts
    /// the relationship back where it started — nothing else changes, the same beliefs, the same
    /// candidate, the same everything — and scores it again. The delta is computed by
    /// <see cref="Utility"/>, never re-implemented here, which is what stops this being a test that
    /// asserts against a copy of the rule it is checking.
    ///
    /// This is decision-relevance, and it is deliberately all that is claimed. Whether the difference
    /// is large enough to change which candidate wins is a separate question, measured by
    /// <see cref="The_relationship_change_is_not_large_enough_to_change_a_choice"/> and answered no.
    /// </summary>
    [Fact]
    public void The_conflict_changes_what_a_later_decision_is_scored_on()
    {
        var world = Run("baseline");
        var vincent = world.Get("vincent");

        var conflict = world.AccountConflicts
            .First(c => c.ListenerId == "vincent" && c.Conflict.SpeakerId == "salvatore");

        // A candidate actually weighed after the conflict, whose score reads the relationship it
        // moved. Reports to his boss price standing off loyalty, and loyalty derives from trust.
        var later = world.Decisions
            .Where(d => d.ActorId == "vincent" && d.At >= conflict.At)
            .SelectMany(d => d.Scored)
            .First(s => s.Candidate.Kind == ActionKind.ReportToSuperior
                        && s.Candidate.TargetId == "salvatore");

        double afterTrust = vincent.Social.Toward("salvatore").Trust;
        Assert.True(afterTrust < 0.45, $"the conflict did not move trust: it stands at {afterTrust:0.000}");

        var perceived = Salience.Perceive(vincent, world.Now);
        var agenda = new Agenda(AgendaKind.DischargeResponsibility, "keep the harbour earning", "test", Cast.Harbour);
        var rng = Rng.ForOccasion(world.Seed, "test|fixed");

        // Milestone 008: read through the facet, not the component name.
        //
        // This used to sum `p.Name == "relationship effects"`, and that was measurably the wrong
        // question. Of the 168 components carrying that name across the five variants, 61 read no
        // relationship state at all — `SeekCorroboration`'s "going behind X" is `-0.45 * proud`. It
        // happens not to appear on a `ReportToSuperior` candidate, so this particular test was
        // getting a right answer from a wrong rule, which is the least durable kind. The name also
        // folded in the Belonging share of loyalty, which is a drive rather than anything owed to
        // Salvatore, so the figure it reported was never purely relational.
        double Relationship() => Utility
            .Score(later.Candidate, vincent.View, vincent.Psychology, perceived, agenda, rng)
            .RelationshipNet();

        double withConflict = Relationship();
        Assert.NotEqual(0.0, withConflict, 6);

        // Undo only the trust the conflict cost him, leaving obligation, fear and grievances alone.
        var rel = vincent.Social.Toward("salvatore");
        Relations.Establish(vincent, "salvatore", trust: 0.45, obligation: rel.Obligation, fear: rel.Fear);

        double withoutConflict = Relationship();

        Assert.NotEqual(withConflict, withoutConflict, 6);
        Assert.True(withConflict < withoutConflict,
            $"being contradicted should lower what the relationship is worth to him, " +
            $"but the component went from {withoutConflict:0.000} to {withConflict:0.000}");
    }

    /// <summary>
    /// And the honest other half of it. The trust movement is real and reaches a score; it is nowhere
    /// near large enough to flip a winner in this scenario, and nothing was tuned to make it so.
    ///
    /// Recorded as an assertion rather than as prose because it is a measurement the next tuning pass
    /// will want, and because a later change that did make the term choice-changing should have to
    /// come here and say so deliberately.
    /// </summary>
    [Fact]
    public void The_relationship_change_is_not_large_enough_to_change_a_choice()
    {
        var world = Run("baseline");
        var vincent = world.Get("vincent");

        var conflict = world.AccountConflicts
            .First(c => c.ListenerId == "vincent" && c.Conflict.SpeakerId == "salvatore");

        var decision = world.Decisions
            .First(d => d.ActorId == "vincent" && d.At >= conflict.At && d.Scored.Count > 1);

        double winner = decision.Scored[0].Total;
        double runnerUp = decision.Scored[1].Total;

        // The whole trust movement is worth about a tenth of a point through loyalty; the margins it
        // would have to close are an order of magnitude wider.
        Assert.True(winner - runnerUp > 0.5,
            $"the margin at {decision.At:yyyy-MM-dd} is {winner - runnerUp:0.000}, which is close " +
            "enough that the relationship term may now be choice-changing — that would be a real " +
            "result, and it needs recording rather than passing silently");
    }

    // ================================================================ D4 — honest distinctness

    private static string Actions(World world)
        => string.Join('\n', world.Decisions.Select(d => d.ChosenActionSignature()));

    /// <summary>
    /// The behavioural digest is built from structured decision fields, not from rendered text.
    ///
    /// <b>This test moved seeds in milestone 008, and why is the point of it.</b> It used to run at
    /// seed 42, where `resentful-tommy` rendered differently from `baseline` and chose the identical
    /// action at every decision — the case milestone 007 recorded as an honest convergence. Milestone
    /// 008 broke that convergence at seed 42 by unbundling grievance from the clamped loyalty: Tommy
    /// resents Vincent, the pair floored to zero under the old clamp, and once it stopped flooring,
    /// what he holds against the man began taking the good out of reporting to him. See
    /// <see cref="Resentment_now_reaches_a_chosen_action_at_seed_42"/>, which pins the new outcome.
    ///
    /// The property being tested here is unchanged and still needs a witness: a pair that renders
    /// differently while choosing identically, so that a digest taken from rendered text would call
    /// them distinct and this one does not. Seed 1 is such a pair, as are 7 and 99. Moving the seed
    /// keeps the property pinned; deleting the test because its old witness stopped qualifying would
    /// have quietly retired a guarantee.
    /// </summary>
    [Fact]
    public void Behavioural_distinctness_is_read_from_decisions_not_from_rendered_text()
    {
        var baseline = Run("baseline", seed: 1);
        var resentful = Run("resentful-tommy", seed: 1);

        Assert.NotEqual(
            TraceWriter.Render(baseline, "baseline", false),
            TraceWriter.Render(resentful, "resentful-tommy", false));

        Assert.Equal(Actions(baseline), Actions(resentful));
    }

    /// <summary>
    /// Milestone 008's behavioural result, pinned where it happens.
    ///
    /// At seed 42 `resentful-tommy` now chooses differently from `baseline`, at exactly one decision:
    /// on 9 April Tommy conceals the incident himself instead of reporting it to Vincent. Nobody
    /// wrote a rule connecting resentment to concealment. It falls out of the grievance he holds
    /// against Vincent no longer being clamped away, which takes 0.21 out of what reporting to that
    /// particular man is worth and lets a concealment candidate that was always there win by 0.03.
    ///
    /// <b>Recorded with its fragility.</b> The winning margin is smaller than the ±0.05 per-candidate
    /// noise, and the divergence holds at seeds 42 and 31337 but not at 1, 7, 99 or 2024. It is a
    /// real choice change at this seed and it is not a robust one, and the archive says so. Nothing
    /// was tuned to produce it — milestone 008 changed no coefficient.
    /// </summary>
    [Fact]
    public void Resentment_now_reaches_a_chosen_action_at_seed_42()
    {
        var baseline = Run("baseline");
        var resentful = Run("resentful-tommy");

        Assert.NotEqual(Actions(baseline), Actions(resentful));

        // The histories fork at one decision and stay forked — eleven of the thirty-eight pairs
        // differ, which is what a fork looks like when compared position by position, not eleven
        // independent changes. What is pinned is the fork point.
        var (before, after) = baseline.Decisions
            .Zip(resentful.Decisions, (b, r) => (b, r))
            .First(p => p.b.ChosenActionSignature() != p.r.ChosenActionSignature());
        Assert.Equal("tommy", after.ActorId);
        Assert.Equal(ActionKind.ReportToSuperior, before.Chosen!.Candidate.Kind);
        Assert.Equal(ActionKind.StartStrategy, after.Chosen!.Candidate.Kind);
        Assert.Equal(StrategyKind.ConcealIncident, after.Chosen!.Candidate.Strategy);

        // And the relationship channel is what decided it: without relationship state the report
        // he actually declined would have won instead.
        var counterfactualWinner = after.Scored
            .OrderByDescending(s => s.TotalWithoutRelationships())
            .ThenBy(s => s.Candidate.Id, StringComparer.Ordinal)
            .First();
        Assert.Equal(ActionKind.ReportToSuperior, counterfactualWinner.Candidate.Kind);
    }

    /// <summary>
    /// The four configurations that do differ behaviourally still do. Kept alongside the test above
    /// so the honest non-result cannot quietly become "nothing distinguishes anything".
    /// </summary>
    [Fact]
    public void The_remaining_configurations_still_choose_differently_from_each_other()
    {
        var distinct = new[] { "baseline", "cautious-vincent", "watchful-boss", "disloyal-vincent" }
            .Select(v => Actions(Run(v)))
            .Distinct()
            .Count();

        Assert.Equal(4, distinct);
    }

    // ================================================================ helpers

    private static GeneratorContext Context(World world, Character actor, IReadOnlyList<Report> sent)
    {
        var perceived = Salience.Perceive(actor, world.Now);

        return new GeneratorContext(
            actor.View,
            perceived,
            new Agenda(AgendaKind.DischargeResponsibility, "keep the harbour earning", "test", Cast.Harbour),
            world.Now,
            new ScheduledEvent
            {
                Id = 1,
                Time = world.Now,
                Kind = EventKind.RoleReview,
                OwnerId = actor.Id,
                Cause = "test",
            },
            world.Org.OfficeForDomain(Cast.Harbour),
            null,
            Array.Empty<Policy>(),
            Pipeline.SuperiorOf(world, actor),
            Pipeline.SubordinatesOf(world, actor),
            Pipeline.OrgMembersOf(world, actor),
            sent,
            Array.Empty<InformationRequest>(),
            new[] { Cast.Grocery });
    }
}
