using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Scenario;
using CrimeSim.Sim;
using CrimeSim.Strategy;
using CrimeSim.Trace;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>
/// Milestone 007. Three things had to become true together for the mechanisms built in 004–006 to
/// show up in a natural scenario. M028's correction preserves the second assignment, collection,
/// and conflict-to-later-score bars in the unmodified seed-42 fixture.
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
    /// <summary>
    /// Found by search over this file's unmodified production scenario (<see cref="Cast.Build"/> /
    /// <see cref="Runner.Run"/>, nothing staged), 2026-09-09: the first seed at which Vincent's own
    /// owner's-carve-out discovery roll on Tommy's violence lands in every one of baseline,
    /// watchful-boss, disloyal-vincent and resentful-tommy together. Seed 42's own history no longer
    /// reaches this — see the <see cref="Rng.ForOccasion"/> correction note on
    /// <see cref="The_delegator_puts_his_question_to_the_man_he_sent"/> — so the handful of tests
    /// whose purpose is proving this exchange is reachable at all, rather than pinning seed 42's own
    /// history, use this seed instead.
    /// </summary>
    private const int AltSeedWhereVincentAsksTommy = 199;

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
                Rng.ForOccasion(world.Seed, "test|fixed"), ctx.CurrentExecution)
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

    /// <summary>The unmodified fixture supplies repeated briefings, not a staged replacement.</summary>
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
            new[] { Cast.Grocery, Cast.Bakery, Cast.Tailor },
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

    /// <summary>Measure the first selected target, without treating a start as proof of collection.</summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    [InlineData("resentful-tommy")]
    public void The_first_target_is_measured_without_promising_collection(string variant)
    {
        var world = Run(variant);

        var firstStrategy = world.Decisions
            .Where(d => d.Chosen?.Candidate.Kind == ActionKind.StartStrategy
                        && d.Chosen.Candidate.Strategy == StrategyKind.SecureTribute)
            .Select(d => d.Chosen!.Candidate.TargetId)
            .FirstOrDefault();

        // M028: the choice is not a promise of collection; parallel work can remain blocked.
        Assert.Equal(Cast.Tailor, firstStrategy);
    }

    /// <summary>Both opening operations complete, allowing another genuine assignment.</summary>
    [Fact]
    public void The_shortfall_produces_a_second_assignment_after_the_opening_work_completes()
    {
        var world = Run("baseline");

        var briefings = world.TruthLog.Where(e => e.Kind == "assignment").ToList();
        Assert.True(briefings.Count >= 2);
        Assert.True(world.Businesses[Cast.Grocery].PayingTribute);
        Assert.True(world.Businesses[Cast.Tailor].PayingTribute);
        Assert.True(world.Org.Condition(OrgCondition.RevenueLoss) >= Organization.SignificantRevenueLoss);
    }

    // ================================================================ the milestone's own claims
    //
    // Retired 2026-09-11 by milestone 024's sixth correction, not relocated: "The_delegator_puts_his_
    // question_to_the_man_he_sent" and "And_the_executor_gives_his_delegator_an_account_of_it" both
    // pinned Vincent's own discovery roll on a delegate's violence naturally landing at seed 199,
    // producing a natural ask-and-answer exchange. That chain depended on a delegate autonomously
    // reaching Force, which is now structurally impossible for any delegate in this cast — every
    // eligible delegate (Tommy, Angelo in capable-angelo) has Capabilities.Crew below Force's
    // RequiredCrew=2, and Filters.Apply's capability stage removes the candidate before scoring, for
    // any seed. There is therefore no seed at which either claim can be honestly reproduced as a
    // natural-autonomous-emergence proof; retracting rather than moving them again is the same
    // treatment "Resentment_no_longer_reaches_a_chosen_action_at_seed_42" got when its own claim
    // stopped holding. The underlying exchange — a delegator putting a direct question to the man he
    // sent, and being answered — is preserved instead through honestly staged production-path proofs
    // in CausalFeedbackTests.cs's "pending vs. declined" section and ControlledAutonomousParityTests.cs,
    // where only the originating incident is staged and the exchange itself runs unstaged.

    /// <summary>A natural conflict moves trust, which a later real candidate actually reads.</summary>
    [Fact]
    public void The_conflict_changes_what_a_later_decision_is_scored_on()
    {
        var world = Cast.Build(42, "baseline");
        var vincent = world.Get("vincent");
        double? cost = null;
        bool witnessed = false;
        for (int guard = 0; guard < 10000; guard++)
        {
            double before = vincent.Social.Toward("salvatore").Trust;
            int conflicts = world.AccountConflicts.Count;
            var step = Runner.Step(world, Cast.Start.AddDays(90), "vincent");
            if (world.AccountConflicts.Skip(conflicts).Any(c => c.ListenerId == "vincent"
                && c.Conflict.SpeakerId == "salvatore"))
                cost = before - vincent.Social.Toward("salvatore").Trust;
            if (step.Status == StepStatus.Exhausted) break;
            if (step.Awaiting is not { } prepared) continue;
            if (cost is > 0)
            {
                var rel = vincent.Social.Toward("salvatore");
                double actualTrust = rel.Trust;
                foreach (var candidate in prepared.Available)
                {
                    double Score() => Utility.Score(candidate, vincent.View, vincent.Psychology,
                        prepared.Perceived, prepared.Agenda, Rng.ForOccasion(world.Seed, "test|fixed"),
                        prepared.Context.CurrentExecution).RelationshipNet();
                    double actual = Score();
                    Relations.Establish(vincent, "salvatore", trust: actualTrust + cost.Value,
                        obligation: rel.Obligation, fear: rel.Fear);
                    double withoutConflict = Score();
                    Relations.Establish(vincent, "salvatore", trust: actualTrust,
                        obligation: rel.Obligation, fear: rel.Fear);
                    if (Math.Abs(actual - withoutConflict) > 0.000001) witnessed = true;
                }
            }
            CommissioningTestDriver.Resolve(prepared, null);
        }
        Assert.True(cost is > 0, "No natural conflict cost Vincent trust.");
        Assert.True(witnessed, "No later available candidate read the trust that conflict moved.");
    }

    /// <summary>Re-rank actual current-run decisions without relationship terms; at least one winner changes.</summary>
    [Fact]
    public void Relationship_components_change_at_least_one_natural_choice()
    {
        // Measure actual re-ranking, not a margin or an invented replacement.
        var world = Run("baseline");
        Assert.Contains(world.Decisions, d => d.Scored.Count > 1 &&
            !ReferenceEquals(d.Scored[0], d.Scored.OrderByDescending(s => s.TotalWithoutRelationships())
                .ThenBy(s => s.Candidate.Id, StringComparer.Ordinal).First()));
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
    /// what he holds against the man began taking the good out of reporting to him.
    ///
    /// <b>2026-09-09 — seed 42 converged again</b>, this time for an unrelated reason (the
    /// <see cref="Rng.ForOccasion"/> correction, not a milestone 008 regression); see
    /// <see cref="Resentment_no_longer_reaches_a_chosen_action_at_seed_42"/>, which now records that
    /// honestly rather than the divergence it used to pin.
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
    /// Milestone 008's behavioural result at seed 42 — retracted here, not deleted, by the
    /// <see cref="Rng.ForOccasion"/> correction of 2026-09-09.
    ///
    /// <b>The old claim.</b> At seed 42, `resentful-tommy` used to choose differently from `baseline`
    /// at exactly one decision: on 9 April Tommy concealed the incident himself instead of reporting
    /// it to Vincent, because grievance (once milestone 008 stopped clamping it away) took 0.21 out of
    /// what reporting to a resented man was worth and let a concealment candidate that was always
    /// there win by a fragile 0.03 margin.
    ///
    /// <b>That claim is now false, and is not being relocated to another seed</b> — per the scope of
    /// this correction, a test whose name and claim are specifically about seed 42 stays about seed
    /// 42 and reports what is actually true there now, rather than quietly moving to wherever the old
    /// result can still be reproduced. Confirmed directly: <c>Actions(baseline)</c> and
    /// <c>Actions(resentful-tommy)</c> are now byte-identical at seed 42, not merely close.
    ///
    /// <b>The mechanism did not regress.</b> Milestone 008's rule — unclamped grievance taking value
    /// out of reporting to a resented man — is untouched; nothing in this correction's authorized
    /// scope touched traits, coefficients, or <c>Utility.Score</c>. What moved is the causal history
    /// upstream of 9 April: the corrected finalizer redistributes which of the milestone 022
    /// observation opportunities land at this seed (see <c>StreetTalkTests.cs</c> and
    /// <c>docs/milestones/022-the-street-talks.md</c>'s correction section), which changes what Tommy
    /// and Vincent each believe by the time this decision is reached, in both configurations, before
    /// the fragile ±0.03 margin from milestone 008 ever gets a chance to matter. This is the same
    /// already-traced convergence Matt accepted as an honest outcome when authorizing this correction,
    /// not a new finding raised here.
    /// </summary>
    [Fact]
    public void Resentment_no_longer_reaches_a_chosen_action_at_seed_42()
    {
        var baseline = Run("baseline");
        var resentful = Run("resentful-tommy");

        Assert.Equal(Actions(baseline), Actions(resentful));
    }

    /// <summary>
    /// Three of these four configurations still differ behaviourally. Baseline and watchful-boss now
    /// converge in chosen actions after the invalid second grocery cycle is removed, while their full
    /// traces remain distinct; cautious-vincent and disloyal-vincent retain distinct action histories.
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
            Acquaintance.KnownTo(world, actor),
            sent,
            Array.Empty<InformationRequest>(),
            new[] { Cast.Grocery },
            Pipeline.SubordinatesOf(world, actor).Where(id => Pipeline.AvailableToExecute(world, id)).ToList(),
            Strategies.CurrentExecution(world, actor));
    }
}
