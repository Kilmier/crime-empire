using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Scenario;
using CrimeSim.Sim;
using CrimeSim.Trace;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>
/// The information loop from INFORMATION_AND_LEGIBILITY.md: direct observation, one report
/// channel, a report that holds something back, a source that contradicts, and a player-facing
/// account limited to one character's information.
///
/// The load-bearing test here is <see cref="Player_view_never_shows_what_the_viewpoint_character_does_not_hold"/>.
/// The rest can be read off a trace by eye; a leak cannot, because a leaked fact looks exactly
/// like a fact the player was entitled to.
/// </summary>
public sealed class InformationTransmissionTests
{
    private const string Viewpoint = "salvatore";

    /// <summary>
    /// Every claim anyone holds that the viewpoint character does not hold must be absent from
    /// his view — checked against the renderer's own wording, not a copy of it.
    ///
    /// This is the executable form of the doc's success criterion that sources never communicate
    /// facts unavailable to them. It runs across every variant because a leak that only appears
    /// in one configuration is still a leak.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("cautious-vincent")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    public void Player_view_never_shows_what_the_viewpoint_character_does_not_hold(string variant)
    {
        var world = Run(variant);
        var salvatore = world.Get(Viewpoint);
        string view = IntelligenceWriter.Render(world, Viewpoint);

        string Name(string id) =>
            world.Find(id)?.Name ?? world.Businesses.GetValueOrDefault(id)?.Name ?? id;

        // Everything anyone in the world holds, plus everything ever asserted to anyone.
        var everyClaim = world.Characters.Values
            .SelectMany(c => c.Cognition.Records.Select(r => r.Claim))
            .Concat(world.Reports.SelectMany(r => r.Asserted.Select(a => a.Claim)))
            .Concat(world.Reports.SelectMany(r => r.Withheld))
            .Distinct()
            .ToList();

        Assert.NotEmpty(everyClaim);

        foreach (var claim in everyClaim)
        {
            if (salvatore.Cognition.Find(claim) is { IsHeld: true }) continue;

            string wording = IntelligenceWriter.Describe(claim, Name);
            Assert.False(
                view.Contains(wording, StringComparison.Ordinal),
                $"[{variant}] the player view states \"{wording}\", which {salvatore.Name} does not hold");
        }
    }

    /// <summary>
    /// The view must not carry the developer-only side of the record.
    ///
    /// Note what is deliberately *not* asserted here: that the view avoids the wording of truth-log
    /// entries. It legitimately overlaps, because a fact the viewpoint character genuinely observed
    /// reads much the same either way — "Tommy Nardo put hands on Bellini's grocery" is both what
    /// happened and what Salvatore saw. Whether he was entitled to it is
    /// <see cref="Player_view_never_shows_what_the_viewpoint_character_does_not_hold"/>'s job.
    /// What this test guards is the material that is hidden regardless of entitlement: a report's
    /// framing states whether its sender was being straight, which is the one thing the player
    /// must work out rather than be told.
    /// </summary>
    [Fact]
    public void Player_view_does_not_carry_developer_only_material()
    {
        var world = Run("disloyal-vincent");
        string view = IntelligenceWriter.Render(world, Viewpoint);

        Assert.NotEmpty(world.Reports);
        foreach (var report in world.Reports)
        {
            Assert.DoesNotContain(report.Framing, view, StringComparison.Ordinal);
            Assert.DoesNotContain(report.Candor.ToString(), view, StringComparison.Ordinal);
        }

        // Utility components and raw scores are the hidden state the doc names explicitly.
        Assert.DoesNotContain("utility", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("perceived goal progress", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("self-protection", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("withheld", view, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// A report carries only what its sender holds. Anything withheld is genuinely absent from
    /// what the recipient was told, rather than passed along at lower confidence.
    /// </summary>
    [Fact]
    public void A_report_conveys_only_what_its_sender_holds_and_omits_what_it_withholds()
    {
        var world = Run("baseline");
        Assert.NotEmpty(world.Reports);

        foreach (var report in world.Reports)
        {
            var sender = world.Get(report.SenderId);

            foreach (var withheld in report.Withheld)
            {
                // He kept back something he actually had — otherwise "withheld" is meaningless.
                Assert.True(
                    sender.Cognition.Find(withheld) is not null,
                    $"{report.SenderId} withheld {withheld}, which he never held");

                // And it was not smuggled through as an affirmation anyway.
                Assert.DoesNotContain(
                    report.Asserted,
                    a => a.Claim.Equals(withheld) && a.AssertedStance is Stance.Knows or Stance.Believes or Stance.Suspects);
            }
        }

        Assert.Contains(world.Reports, r => r.Withheld.Count > 0);
    }

    /// <summary>
    /// The concealment costs the recipient that route to the fact.
    ///
    /// Deliberately not asserted: that he ends up not holding it at all. He may well reach it
    /// another way — by inference, or from somebody else — and after the review that separated
    /// observable violence from the hidden question of who authorised it, inference is exactly
    /// how the boss is *meant* to get there. What must hold is narrower and actually about the
    /// report: the man who kept it back never affirmed it to him.
    /// </summary>
    [Fact]
    public void An_incomplete_report_never_conveys_what_it_withheld()
    {
        var world = Run("baseline");
        var salvatore = world.Get(Viewpoint);

        Assert.Contains(world.TruthLog, e => e.Kind == "violence");

        var concealing = world.Reports
            .Where(r => r.RecipientId == Viewpoint && r.Withheld.Count > 0)
            .ToList();

        Assert.NotEmpty(concealing);

        foreach (var report in concealing)
        foreach (var withheld in report.Withheld)
        {
            Assert.DoesNotContain(
                salvatore.Cognition.AccountsOf(withheld),
                t => t.SenderId == report.SenderId && t.Affirms);

            // And if he does hold it, it is on some other footing than that man's word.
            if (salvatore.Cognition.Find(withheld) is { IsHeld: true } held)
                Assert.NotEqual(report.SenderId, held.SourceId);
        }
    }

    /// <summary>
    /// Who authorised violence is never observable. It may only be reasoned to or reported —
    /// observing a wrecked shopfront cannot reveal whose decision it was.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    public void Authorship_of_a_breach_is_never_acquired_by_observation(string variant)
    {
        var world = Run(variant);

        foreach (var character in world.Characters.Values)
        foreach (var breach in character.Cognition.Records.Where(r => r.Claim.Kind == ClaimKind.PersonBreachedPolicy))
        {
            // The one exception is the man who did it: he does not deduce his own decision.
            if (breach.Claim.Subject == character.Id) continue;

            Assert.True(
                breach.SourceKind is SourceKind.Inference or SourceKind.Report,
                $"[{variant}] {character.Id} holds {breach.Claim} via {breach.SourceKind}; " +
                "authorship must be inferred or reported, never observed");
        }
    }

    /// <summary>
    /// The boss can still get there — otherwise the fix to finding 1 would simply have deleted the
    /// behaviour rather than routing it correctly.
    /// </summary>
    [Fact]
    public void A_breach_can_still_be_reasoned_to_from_facts_the_character_holds()
    {
        var world = Run("baseline");
        var salvatore = world.Get(Viewpoint);

        var reasoned = salvatore.Cognition.Records
            .Where(r => r.Claim.Kind == ClaimKind.PersonBreachedPolicy && r.SourceKind == SourceKind.Inference)
            .ToList();

        Assert.NotEmpty(reasoned);

        foreach (var r in reasoned)
        {
            Assert.Equal(Viewpoint, r.SourceId);
            Assert.Equal(Stance.Suspects, r.Stance);

            // It rests on something he actually has: violence he holds, and the rule itself.
            Assert.Contains(
                salvatore.Cognition.OfKind(ClaimKind.PersonUsedViolence),
                v => v.Claim.EventId == r.Claim.EventId);
            Assert.True(salvatore.Cognition.Holds(new Claim(ClaimKind.PolicyIssued, "greco-family", r.Claim.Object)));
        }
    }

    /// <summary>
    /// Candour records what the sender was trying to do, not whether he happened to be right.
    ///
    /// A denial asserts the opposite of what the sender holds; an omission asserts nothing untrue
    /// and simply says less. Keeping those distinguishable in the developer record is the doc's
    /// open question about telling lies apart from sincere false belief.
    /// </summary>
    [Fact]
    public void Candour_distinguishes_lying_from_merely_leaving_things_out()
    {
        var world = Run("disloyal-vincent");

        var denial = world.Reports.FirstOrDefault(r => r.Candor == ReportCandor.False);
        Assert.NotNull(denial);

        var liar = world.Get(denial!.SenderId);
        foreach (var withheld in denial.Withheld)
        {
            // He holds it, and told the other man the opposite. That gap is what makes it a lie.
            Assert.True(liar.Cognition.Find(withheld) is { IsHeld: true });
            Assert.Contains(denial.Asserted, a => a.Claim.Equals(withheld) && a.AssertedStance == Stance.Rejects);
        }

        // An omission says less about the awkward thing; it never says the opposite of it.
        //
        // Note this is scoped to the withheld claims rather than banning Rejects outright. A
        // report may legitimately carry a sincere rejection — "that business is not holding out
        // any more", "I was wrong about that" — and conflating an honest retraction with a lie is
        // exactly the confusion Candor exists to prevent. What marks the lie is denying something
        // he is simultaneously recorded as keeping back.
        foreach (var partial in world.Reports.Where(r => r.Candor == ReportCandor.Partial))
        {
            Assert.NotEmpty(partial.Withheld);
            foreach (var w in partial.Withheld)
                Assert.DoesNotContain(partial.Asserted, a => a.Claim.Equals(w));
        }
    }

    /// <summary>
    /// A source contradicting him leaves the disagreement standing and attributable, rather than
    /// one account quietly overwriting the other.
    /// </summary>
    [Fact]
    public void A_contradicting_source_leaves_a_conflict_that_is_still_attributable()
    {
        var world = Run("disloyal-vincent");
        var salvatore = world.Get(Viewpoint);

        var contested = salvatore.Cognition.Testimony
            .Select(t => t.Claim)
            .Distinct()
            .Where(salvatore.Cognition.IsContested)
            .ToList();

        Assert.NotEmpty(contested);

        foreach (var claim in contested)
        {
            var accounts = salvatore.Cognition.AccountsOf(claim).ToList();
            Assert.NotEmpty(accounts);

            // Every account still names who gave it. A conflict nobody can be held to is not
            // usable evidence about anything.
            Assert.All(accounts, a => Assert.False(string.IsNullOrEmpty(a.SenderId)));

            var own = salvatore.Cognition.Find(claim);
            bool disagreesWithHim = own is not null && accounts.Any(a => a.Affirms != own.IsHeld);
            bool sourcesDisagree = accounts.Any(a => a.Affirms) && accounts.Any(a => !a.Affirms);
            Assert.True(disagreesWithHim || sourcesDisagree);
        }

        // And the player is shown it as a disagreement, not handed a resolution.
        string view = IntelligenceWriter.Render(world, Viewpoint);
        Assert.Contains("ACCOUNTS THAT DO NOT AGREE", view, StringComparison.Ordinal);
        Assert.Contains("contradicted", view, StringComparison.Ordinal);
    }

    /// <summary>
    /// Being told something does not overwrite what he saw. A character who could be talked out of
    /// his own eyes by one assertion would make deception free.
    /// </summary>
    [Fact]
    public void Being_contradicted_shakes_a_directly_observed_belief_without_erasing_it()
    {
        var cognition = new Cognition();
        var claim = new Claim(ClaimKind.PersonUsedViolence, "vincent", "shop", 1);
        var at = new DateTime(1987, 3, 2, 0, 0, 0, DateTimeKind.Utc);

        cognition.Learn(claim, Stance.Knows, 1.0, SourceKind.Direct, "self", at);
        cognition.Receive(new ReportedClaim(claim, Stance.Rejects, 0.8), "vincent", at.AddDays(1));

        var after = cognition.Find(claim);
        Assert.NotNull(after);
        Assert.True(after!.IsHeld, "a single denial should not overturn direct observation");
        Assert.True(after.Confidence < 1.0, "but it should cost him some certainty");
        Assert.True(cognition.IsContested(claim));

        // The acquisition time is when he saw it, not when he was argued with about it.
        Assert.Equal(at, after.AcquiredAt);
        Assert.Equal(at.AddDays(1), after.ReconsideredAt);
    }

    /// <summary>Two independent voices saying the same thing are worth more than one repeating.</summary>
    [Fact]
    public void Corroboration_counts_only_when_it_comes_from_someone_new()
    {
        var claim = new Claim(ClaimKind.BusinessRefusesTribute, "shop");
        var at = new DateTime(1987, 3, 2, 0, 0, 0, DateTimeKind.Utc);

        var repeated = new Cognition();
        repeated.Receive(new ReportedClaim(claim, Stance.Believes, 0.5), "tommy", at);
        repeated.Receive(new ReportedClaim(claim, Stance.Believes, 0.5), "tommy", at.AddDays(1));

        var independent = new Cognition();
        independent.Receive(new ReportedClaim(claim, Stance.Believes, 0.5), "tommy", at);
        independent.Receive(new ReportedClaim(claim, Stance.Believes, 0.5), "vincent", at.AddDays(1));

        Assert.True(
            independent.ConfidenceIn(claim) > repeated.ConfidenceIn(claim),
            "a second source should be worth more than the first source saying it twice");
    }

    /// <summary>
    /// No name reaches the page that the viewpoint character has no way to know.
    ///
    /// This is the identity counterpart of the claim-level leak test, and it needs to exist
    /// separately: the "who has not reported" section named people without stating any claim about
    /// them, so a check that only looked at claim wording passed while the org roster was being
    /// enumerated straight out of world state. Vincent is the sharper case — a capo should not be
    /// handed the boss's full membership list.
    /// </summary>
    [Theory]
    [InlineData("salvatore", "baseline")]
    [InlineData("salvatore", "disloyal-vincent")]
    [InlineData("vincent", "baseline")]
    [InlineData("tommy", "baseline")]
    [InlineData("marco", "baseline")]
    public void Player_view_names_only_people_the_viewpoint_character_could_know(string viewpoint, string variant)
    {
        var world = Run(variant);
        var who = world.Get(viewpoint);
        string view = IntelligenceWriter.Render(world, viewpoint);

        var knowable = IntelligenceWriter.KnownPeople(world, who).ToHashSet(StringComparer.Ordinal);

        foreach (var other in world.Characters.Values)
        {
            if (other.Id == who.Id || knowable.Contains(other.Id)) continue;

            Assert.False(
                view.Contains(other.Name, StringComparison.Ordinal),
                $"[{viewpoint}/{variant}] the view names {other.Name}, who is not among the people " +
                $"{who.Name} has any way to know about");
        }
    }

    /// <summary>
    /// A member of the organisation the viewpoint character has never heard of must not be named.
    ///
    /// The scenario cast cannot show this on its own — Salvatore happens to know all four of the
    /// others, so enumerating the roster and enumerating what he knows produce the same names, and
    /// a test over the stock cast passes either way. That coincidence is exactly why the leak went
    /// unnoticed. This introduces a member nobody has any claim, relationship or account
    /// mentioning, so the two sources of names can finally disagree.
    /// </summary>
    [Fact]
    public void An_unknown_member_of_the_organisation_is_never_named()
    {
        var world = Cast.Build(seed: 42, "baseline");

        var unknown = new Character
        {
            Id = "zzz-unknown",
            Name = "Enzo Fantini",
            RoleTitle = "soldier",
            Capabilities = new Capabilities(authority: 1, districts: new[] { Cast.Harbour }),
            Psychology = new Psychology(),
        };
        unknown.Social.OrganizationId = Cast.OrgId;
        world.Characters[unknown.Id] = unknown;

        Runner.Run(world, Cast.Start.AddDays(90));

        var salvatore = world.Get(Viewpoint);

        // Nothing in his head refers to this man.
        Assert.DoesNotContain(unknown.Id, IntelligenceWriter.KnownPeople(world, salvatore));

        string view = IntelligenceWriter.Render(world, Viewpoint);
        Assert.DoesNotContain(unknown.Name, view, StringComparison.Ordinal);
        Assert.DoesNotContain(unknown.Id, view, StringComparison.Ordinal);
    }

    /// <summary>
    /// Provenance wording must not invent facts the record does not contain — in particular it
    /// must not place the viewpoint character at a scene merely because a claim reached him
    /// unmediated. Direct means unmediated, not present.
    /// </summary>
    [Theory]
    [InlineData("salvatore")]
    [InlineData("vincent")]
    [InlineData("tommy")]
    public void Player_view_does_not_invent_attendance(string viewpoint)
    {
        var world = Run("baseline");
        string view = IntelligenceWriter.Render(world, viewpoint);

        Assert.DoesNotContain("was there", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("was present", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("watched", view, StringComparison.OrdinalIgnoreCase);

        // Nor the self-sourced form of the same invention. Vincent holds that he went outside the
        // rule because he decided to; nobody watched that happen, himself included.
        Assert.DoesNotContain("saw it himself", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("saw for himself", view, StringComparison.OrdinalIgnoreCase);

        // And not through the confidence label either — see the dedicated test below.
        Assert.DoesNotContain("personally witnessed", view, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The confidence label must not smuggle a provenance claim back in.
    ///
    /// "Personally witnessed" was emitted purely on confidence >= 0.9, with no reference to how
    /// the claim was acquired. Vincent is the case that shows why that is wrong: he holds that he
    /// went outside his boss's rule at full confidence because he *decided* it, and the view
    /// therefore told the player he had witnessed something he never saw. Confidence and
    /// provenance are different axes and a number cannot establish a method.
    /// </summary>
    [Theory]
    [InlineData("vincent", "baseline")]
    [InlineData("vincent", "disloyal-vincent")]
    [InlineData("salvatore", "baseline")]
    [InlineData("tommy", "baseline")]
    public void Player_view_never_claims_witnessing_from_confidence_alone(string viewpoint, string variant)
    {
        var world = Run(variant);
        string view = IntelligenceWriter.Render(world, viewpoint);

        Assert.DoesNotContain("personally witnessed", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("witnessed", view, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Vincent's own breach specifically: he holds it at full confidence, so it exercises the top
    /// of the label range, and he authored it rather than observing it. This pins the exact line
    /// the finding named.
    /// </summary>
    [Fact]
    public void Vincents_own_breach_is_never_rendered_as_personally_witnessed()
    {
        var world = Run("baseline");
        var vincent = world.Get("vincent");

        var ownBreach = vincent.Cognition.Records.Single(r =>
            r.Claim.Kind == ClaimKind.PersonBreachedPolicy && r.Claim.Subject == vincent.Id);
        Assert.True(ownBreach.Confidence >= 0.9, "the case only bites at the top of the label range");
        Assert.DoesNotContain("witness", ownBreach.ConfidenceLabel, StringComparison.OrdinalIgnoreCase);

        string view = IntelligenceWriter.Render(world, "vincent");
        Assert.DoesNotContain("personally witnessed", view, StringComparison.Ordinal);
    }

    /// <summary>
    /// Specifically: the man who ordered a breach must not be described as having seen it. This
    /// pins the exact case that motivated the wording change, so a future rewrite of the
    /// attribution phrasing cannot quietly reintroduce it.
    /// </summary>
    [Fact]
    public void The_author_of_a_breach_is_not_described_as_having_observed_it()
    {
        var world = Run("baseline");
        var vincent = world.Get("vincent");

        var ownBreach = vincent.Cognition.Records
            .FirstOrDefault(r => r.Claim.Kind == ClaimKind.PersonBreachedPolicy
                                 && r.Claim.Subject == vincent.Id
                                 && r.IsHeld);
        Assert.NotNull(ownBreach);
        Assert.Equal(SourceKind.Direct, ownBreach!.SourceKind);

        string view = IntelligenceWriter.Render(world, "vincent");
        string wording = IntelligenceWriter.Describe(ownBreach.Claim, id =>
            world.Find(id)?.Name ?? world.Businesses.GetValueOrDefault(id)?.Name ?? id);

        Assert.Contains(wording, view, StringComparison.Ordinal);

        // The line that follows it says how he has it, and must not say he watched it.
        int at = view.IndexOf(wording, StringComparison.Ordinal);
        string provenance = view[at..].Split('\n').Skip(1).FirstOrDefault() ?? "";
        Assert.DoesNotContain("saw", provenance, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The same voice repeating itself is the same evidence, not more of it — however the belief
    /// was first acquired, and however many times he says it.
    /// </summary>
    [Fact]
    public void One_source_cannot_corroborate_itself_by_repetition()
    {
        var claim = new Claim(ClaimKind.BusinessRefusesTribute, "shop");
        var at = new DateTime(1987, 3, 2, 0, 0, 0, DateTimeKind.Utc);

        // Acquired by observation, then talked at repeatedly by one man. The record still names
        // the observer as its source, which is what the old check compared against.
        var observed = new Cognition();
        observed.Learn(claim, Stance.Believes, 0.5, SourceKind.Direct, "self", at);
        double afterObserving = observed.ConfidenceIn(claim);

        for (int i = 1; i <= 5; i++)
            observed.Receive(new ReportedClaim(claim, Stance.Believes, 0.9), "tommy", at.AddDays(i));

        double afterOneVoice = observed.ConfidenceIn(claim);

        // One new voice may move him once. Five repetitions must not move him five times.
        var chorus = new Cognition();
        chorus.Learn(claim, Stance.Believes, 0.5, SourceKind.Direct, "self", at);
        chorus.Receive(new ReportedClaim(claim, Stance.Believes, 0.9), "tommy", at.AddDays(1));
        double afterFirst = chorus.ConfidenceIn(claim);

        Assert.True(afterOneVoice > afterObserving, "the first new source should count for something");
        Assert.Equal(afterFirst, afterOneVoice);
    }

    /// <summary>Repeating a denial does not wear a belief down any further either.</summary>
    [Fact]
    public void One_source_cannot_erode_a_belief_twice_by_repeating_a_denial()
    {
        var claim = new Claim(ClaimKind.PersonUsedViolence, "vincent", "shop", 1);
        var at = new DateTime(1987, 3, 2, 0, 0, 0, DateTimeKind.Utc);

        var cognition = new Cognition();
        cognition.Learn(claim, Stance.Knows, 1.0, SourceKind.Direct, "self", at);

        cognition.Receive(new ReportedClaim(claim, Stance.Rejects, 0.8), "vincent", at.AddDays(1));
        double afterFirstDenial = cognition.ConfidenceIn(claim);

        for (int i = 2; i <= 6; i++)
            cognition.Receive(new ReportedClaim(claim, Stance.Rejects, 0.8), "vincent", at.AddDays(i));

        Assert.Equal(afterFirstDenial, cognition.ConfidenceIn(claim));
    }

    /// <summary>
    /// A disagreement stays on the record even after the belief it disturbed has been talked all
    /// the way down — that is precisely the case where the deception worked, and the one where
    /// re-deriving contestedness from the current stance would report nothing wrong.
    /// </summary>
    [Fact]
    public void A_contradiction_survives_the_belief_it_overturned()
    {
        var claim = new Claim(ClaimKind.PersonBreachedPolicy, "vincent", "no-violence-harbour", 1);
        var at = new DateTime(1987, 3, 2, 0, 0, 0, DateTimeKind.Utc);

        var cognition = new Cognition();
        // Reasoned to, not seen — the shape the boss now actually holds.
        cognition.Learn(claim, Stance.Suspects, 0.4, SourceKind.Inference, "salvatore", at);
        cognition.Receive(new ReportedClaim(claim, Stance.Rejects, 0.95), "vincent", at.AddDays(1));

        var after = cognition.Find(claim);
        Assert.NotNull(after);
        Assert.False(after!.IsHeld, "a thin suspicion should give way to a confident denial");
        Assert.True(cognition.IsContested(claim), "but the disagreement still happened");
        Assert.True(after.Contested);
    }

    /// <summary>
    /// Being contradicted counts as having something to report, even though no new claim was
    /// acquired. Checking acquisition alone left a character sitting on an account he had since
    /// learned was disputed.
    /// </summary>
    [Fact]
    public void Reconsideration_counts_as_something_new_to_report()
    {
        var claim = new Claim(ClaimKind.BusinessRefusesTribute, "shop");
        var acquired = new DateTime(1987, 3, 2, 0, 0, 0, DateTimeKind.Utc);
        var disputed = acquired.AddDays(10);

        var cognition = new Cognition();
        cognition.Learn(claim, Stance.Believes, 0.6, SourceKind.Report, "tommy", acquired);

        var before = cognition.Find(claim)!;
        Assert.Equal(acquired, before.ReconsideredAt);

        cognition.Receive(new ReportedClaim(claim, Stance.Rejects, 0.7), "vincent", disputed);

        var after = cognition.Find(claim)!;
        Assert.Equal(acquired, after.AcquiredAt);
        Assert.Equal(disputed, after.ReconsideredAt);

        // Which is what report eligibility keys off: acquisition alone would still read as March.
        Assert.True(after.ReconsideredAt > after.AcquiredAt);

        // And the rule itself agrees. He reported in between acquiring the claim and being
        // contradicted about it, so an acquisition-only test says he has nothing to add — while
        // in fact the account he gave has since been disputed.
        var alreadyReported = new[]
        {
            new Report(1, "vincent", "salvatore", acquired.AddDays(1), ReportCandor.Candid,
                new[] { new ReportedClaim(claim, Stance.Believes, 0.45) },
                Array.Empty<Claim>(), "reported"),
        };

        Assert.True(
            Generators.HasSomethingToReport(alreadyReported, cognition.Records, "salvatore"),
            "being contradicted since he conveyed it is something to report");
    }

    /// <summary>
    /// The other half of the same rule: with nothing changed since he last spoke, he has nothing
    /// to say. Without this, reporting re-arms every time anyone repeats themselves and two
    /// characters file accounts at each other until the run ends.
    /// </summary>
    [Fact]
    public void Nothing_changed_since_he_last_spoke_means_nothing_to_report()
    {
        var claim = new Claim(ClaimKind.BusinessRefusesTribute, "shop");
        var at = new DateTime(1987, 3, 2, 0, 0, 0, DateTimeKind.Utc);

        var cognition = new Cognition();
        cognition.Learn(claim, Stance.Believes, 0.6, SourceKind.Report, "tommy", at);

        // He actually conveyed this claim. An empty report would convey nothing and would
        // correctly leave the matter outstanding — eligibility is per claim, not per timestamp.
        var spoke = new[]
        {
            new Report(1, "vincent", "salvatore", at.AddDays(1), ReportCandor.Candid,
                new[] { new ReportedClaim(claim, Stance.Believes, 0.45) },
                Array.Empty<Claim>(), "reported"),
        };

        Assert.False(Generators.HasSomethingToReport(spoke, cognition.Records, "salvatore"));

        // Tommy saying the same thing again is not a development.
        cognition.Receive(new ReportedClaim(claim, Stance.Believes, 0.6), "tommy", at.AddDays(5));
        Assert.False(
            Generators.HasSomethingToReport(spoke, cognition.Records, "salvatore"),
            "a source repeating himself must not re-arm reporting");

        // A genuinely new voice is.
        cognition.Receive(new ReportedClaim(claim, Stance.Believes, 0.6), "marco", at.AddDays(6));
        Assert.True(Generators.HasSomethingToReport(spoke, cognition.Records, "salvatore"));
    }

    /// <summary>
    /// And the converse: hearing the same man say the same thing is not a development, so it must
    /// not keep re-arming report eligibility. This is what stops two characters reporting to each
    /// other until the run ends.
    /// </summary>
    [Fact]
    public void Hearing_the_same_account_again_is_not_a_development()
    {
        var claim = new Claim(ClaimKind.BusinessRefusesTribute, "shop");
        var at = new DateTime(1987, 3, 2, 0, 0, 0, DateTimeKind.Utc);

        var cognition = new Cognition();
        cognition.Receive(new ReportedClaim(claim, Stance.Believes, 0.6), "tommy", at);
        var first = cognition.Find(claim)!;

        cognition.Receive(new ReportedClaim(claim, Stance.Believes, 0.6), "tommy", at.AddDays(5));
        var second = cognition.Find(claim)!;

        Assert.Equal(first.ReconsideredAt, second.ReconsideredAt);
        Assert.Equal(first.Confidence, second.Confidence);

        // The account is still filed, even though it moved nothing.
        Assert.Equal(2, cognition.AccountsOf(claim).Count());
    }

    /// <summary>
    /// A retraction survives the whole channel: eligible to report, composed into the message with
    /// the direction he actually holds, and delivered as a denial the recipient can act on.
    ///
    /// Tested end to end through Compose and Deliver rather than against the eligibility helper
    /// alone, because the three stages failed independently — eligibility filtered to held
    /// beliefs, composition filtered to held beliefs, and composition then hardcoded
    /// <see cref="Stance.Believes"/> on everything it did include. Any one of those left standing
    /// makes "I was wrong about that" unsayable, and a test of one stage cannot see the others.
    /// </summary>
    [Fact]
    public void A_retraction_can_be_reported_composed_and_delivered()
    {
        var world = Cast.Build(seed: 42, "baseline");
        var vincent = world.Get("vincent");
        var salvatore = world.Get(Viewpoint);
        var claim = new Claim(ClaimKind.BusinessRefusesTribute, Cast.Grocery);
        var t0 = Cast.Start;

        // Salvatore opens the scenario believing the grocery is holding out.
        Assert.True(salvatore.Cognition.Holds(claim));

        // Vincent believed it too, then found out otherwise.
        vincent.Cognition.Learn(claim, Stance.Believes, 0.8, SourceKind.Report, Viewpoint, t0);
        vincent.Cognition.Learn(claim, Stance.Rejects, 0.9, SourceKind.Direct, vincent.Id, t0.AddDays(5));

        var changed = vincent.Cognition.Find(claim)!;
        Assert.False(changed.IsHeld);

        // 1. Eligibility sees it, even though he no longer holds it. He conveyed the original
        //    affirmation, so this is specifically the "said, and has since changed" case rather
        //    than the trivial "never mentioned it" one.
        var alreadyReported = new[]
        {
            new Report(1, vincent.Id, Viewpoint, t0.AddDays(1), ReportCandor.Candid,
                new[] { new ReportedClaim(claim, Stance.Believes, 0.6) },
                Array.Empty<Claim>(), "reported"),
        };
        Assert.True(
            Generators.HasSomethingToReport(alreadyReported, vincent.Cognition.Records, Viewpoint),
            "a belief he has since rejected is something to report");

        // 2. Composition carries it, in the direction he actually holds it.
        world.Now = t0.AddDays(6);
        var candidate = new Candidate("report:salvatore", ActionKind.ReportToSuperior, "test", "report in")
        {
            TargetId = Viewpoint,
            Candor = ReportCandor.Candid,
        };
        var report = Reporting.Compose(
            world, vincent, salvatore, candidate, Salience.Perceive(vincent, world.Now));

        var conveyed = report.Asserted.SingleOrDefault(a => a.Claim.Equals(claim));
        Assert.NotEqual(default, conveyed);
        Assert.Equal(Stance.Rejects, conveyed.AssertedStance);
        Assert.Empty(report.Withheld);

        // 3. Delivery lands it as a denial, not as another affirmation.
        Reporting.Deliver(world, report, salvatore);

        Assert.Contains(
            salvatore.Cognition.AccountsOf(claim),
            t => t.SenderId == vincent.Id && !t.Affirms);
        Assert.True(salvatore.Cognition.IsContested(claim));
        Assert.True(salvatore.Cognition.ConfidenceIn(claim) < 0.75,
            "being told it is not so should cost him confidence");
    }

    /// <summary>
    /// A source changing its story is new information; a source repeating itself is not.
    ///
    /// Collapsing the two was the price of the earlier fix against self-corroboration: blocking
    /// every further account from a familiar sender also blocked recantation, which would leave a
    /// witness permanently unable to take anything back.
    /// </summary>
    [Fact]
    public void A_source_changing_its_story_is_not_treated_as_repetition()
    {
        var claim = new Claim(ClaimKind.PersonUsedViolence, "tommy", "shop", 1);
        var at = new DateTime(1987, 3, 2, 0, 0, 0, DateTimeKind.Utc);

        var cognition = new Cognition();
        cognition.Receive(new ReportedClaim(claim, Stance.Believes, 0.8), "vincent", at);
        var afterFirst = cognition.Find(claim)!;

        // Same man, same story: nothing moves, not even the reconsideration stamp.
        cognition.Receive(new ReportedClaim(claim, Stance.Believes, 0.8), "vincent", at.AddDays(1));
        var afterRepeat = cognition.Find(claim)!;
        Assert.Equal(afterFirst.Confidence, afterRepeat.Confidence);
        Assert.Equal(afterFirst.ReconsideredAt, afterRepeat.ReconsideredAt);

        // Same man, opposite story: that is a recantation and it must land.
        cognition.Receive(new ReportedClaim(claim, Stance.Rejects, 0.9), "vincent", at.AddDays(2));
        var afterRecant = cognition.Find(claim)!;

        Assert.True(afterRecant.Confidence < afterRepeat.Confidence, "taking it back should cost the belief");
        Assert.Equal(at.AddDays(2), afterRecant.ReconsideredAt);
        Assert.True(cognition.IsContested(claim));

        // Three accounts on file, all attributable — the retraction does not erase what he said before.
        var accounts = cognition.AccountsOf(claim).ToList();
        Assert.Equal(3, accounts.Count);
        Assert.Equal(2, accounts.Count(a => a.Affirms));
        Assert.Single(accounts, a => !a.Affirms);

        // And having been recanted at is itself something he can pass on — he had already told
        // Kane the original version, so this is a correction to something on the record.
        var spoke = new[]
        {
            new Report(1, "salvatore", "kane", at.AddDays(1), ReportCandor.Candid,
                new[] { new ReportedClaim(claim, Stance.Believes, 0.6) },
                Array.Empty<Claim>(), "reported"),
        };
        Assert.True(Generators.HasSomethingToReport(spoke, cognition.Records, "kane"));
    }

    /// <summary>
    /// And the recantation must not then compound: having denied it once, denying it again is the
    /// same account a second time.
    /// </summary>
    [Fact]
    public void A_recantation_does_not_compound_when_repeated()
    {
        var claim = new Claim(ClaimKind.PersonUsedViolence, "tommy", "shop", 1);
        var at = new DateTime(1987, 3, 2, 0, 0, 0, DateTimeKind.Utc);

        var cognition = new Cognition();
        cognition.Receive(new ReportedClaim(claim, Stance.Believes, 0.8), "vincent", at);
        cognition.Receive(new ReportedClaim(claim, Stance.Rejects, 0.9), "vincent", at.AddDays(1));
        var afterRecant = cognition.Find(claim)!;

        for (int i = 2; i <= 5; i++)
            cognition.Receive(new ReportedClaim(claim, Stance.Rejects, 0.9), "vincent", at.AddDays(i));

        var afterRepeats = cognition.Find(claim)!;
        Assert.Equal(afterRecant.Confidence, afterRepeats.Confidence);
        Assert.Equal(afterRecant.ReconsideredAt, afterRepeats.ReconsideredAt);
    }

    /// <summary>
    /// Asking is spent when the question is put, not when it is answered — but what is spent is
    /// the question, not the relationship.
    ///
    /// Filtering only on replies meant an unanswered request left no trace at all, so the asker
    /// put it again on every wake. The reply cannot be the terminating condition, because giving
    /// one is the other man's decision and he is entitled to say nothing. Scoping the record to a
    /// pair of people rather than to a subject then went too far the other way: one enquiry barred
    /// a character from ever asking that man about anything again.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("cautious-vincent")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    public void Nobody_asks_the_same_person_about_the_same_thing_twice(string variant)
    {
        var world = Run(variant);

        var duplicates = world.Requests
            .GroupBy(r => (r.AskerId, r.AskedId, r.About))
            .Where(g => g.Count() > 1)
            .ToList();

        Assert.Empty(duplicates);
    }

    /// <summary>
    /// Asking about one thing must not close the channel. Two different questions to the same
    /// person are two questions, and the second must remain possible.
    /// </summary>
    [Fact]
    public void A_request_spends_the_question_and_not_the_relationship()
    {
        var t0 = Cast.Start;
        var asked = new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery, 1);
        var different = new Claim(ClaimKind.PoliceInvestigating, "tommy");

        var made = new[] { new InformationRequest(1, "salvatore", "tommy", asked, t0) };

        // Exercised through the generator's own rule, not a copy of it written in the test.
        Assert.False(Generators.CanAsk(made, "tommy", asked),
            "the question he already put is spent");
        Assert.True(Generators.CanAsk(made, "tommy", different),
            "a different question to the same man must stay open");
        Assert.True(Generators.CanAsk(made, "vincent", asked),
            "and the same question to a different man must stay open");
    }

    /// <summary>
    /// The same account, to the same person, twice, with nothing having changed in between.
    ///
    /// This is what a partial report degenerated into: withholding a claim left it looking
    /// permanently unsaid, so it counted as news forever and re-armed reporting every week —
    /// thirteen identical accounts from Tommy through to the end of May. Deciding to keep
    /// something back is a decision about it, and it stands until his position moves.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("cautious-vincent")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    public void No_two_reports_between_the_same_pair_are_identical(string variant)
    {
        var world = Run(variant);

        static string Content(Report r) =>
            $"{r.Candor}|" +
            string.Join(",", r.Asserted.Select(a => $"{a.Claim}:{a.AssertedStance}")) +
            "|" + string.Join(",", r.Withheld.Select(w => w.ToString()));

        var repeats = world.Reports
            .GroupBy(r => (r.SenderId, r.RecipientId, Content: Content(r)))
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key.SenderId}->{g.Key.RecipientId} x{g.Count()}: {g.Key.Content}")
            .ToList();

        Assert.True(repeats.Count == 0,
            $"[{variant}] the same account was filed more than once with nothing changed:\n" +
            string.Join("\n", repeats));
    }

    /// <summary>
    /// The three cases a report can leave a claim in, kept apart. Told, deliberately kept back,
    /// and never reached — only the last is still outstanding on its own.
    /// </summary>
    [Fact]
    public void Withholding_a_claim_is_a_decision_about_it_not_a_silence()
    {
        var at = new DateTime(1987, 3, 2, 0, 0, 0, DateTimeKind.Utc);
        var told = new Claim(ClaimKind.BusinessRefusesTribute, "shop");
        var kept = new Claim(ClaimKind.PersonUsedViolence, "vincent", "shop", 1);
        var unreached = new Claim(ClaimKind.PoliceInvestigating, "vincent");

        var cognition = new Cognition();
        foreach (var c in new[] { told, kept, unreached })
            cognition.Learn(c, Stance.Believes, 0.8, SourceKind.Direct, "self", at);

        var sent = new[]
        {
            new Report(1, "vincent", "salvatore", at.AddDays(1), ReportCandor.Partial,
                new[] { new ReportedClaim(told, Stance.Believes, 0.6) },
                new[] { kept }, "reported"),
        };

        Assert.False(Reporting.NeedsConveying(sent, "salvatore", cognition.Find(told)!));
        Assert.False(Reporting.NeedsConveying(sent, "salvatore", cognition.Find(kept)!),
            "a claim he decided to keep back is not permanently outstanding");
        Assert.True(Reporting.NeedsConveying(sent, "salvatore", cognition.Find(unreached)!),
            "a claim the length cap cut is still outstanding");

        Assert.False(Generators.HasSomethingToReport(sent, new[] { cognition.Find(kept)! }, "salvatore"));

        // Until his position on it moves — then it is worth raising after all.
        cognition.Receive(new ReportedClaim(kept, Stance.Rejects, 0.9), "tommy", at.AddDays(5));
        Assert.True(Reporting.NeedsConveying(sent, "salvatore", cognition.Find(kept)!),
            "being contradicted about it since is a reason to raise it");
    }

    /// <summary>
    /// A behavioural budget. Not a performance test — a runaway exchange is a *correctness*
    /// failure that happens to show up as a number, and it went unnoticed because nothing asserted
    /// the scenario stays a scenario. Five people over ninety days should not produce hundreds of
    /// deliberations, and the disloyal path is the one that actually ran away.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("cautious-vincent")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    public void The_scenario_stays_within_a_sane_behavioural_budget(string variant)
    {
        var world = Run(variant);

        Assert.True(world.Decisions.Count < 100,
            $"[{variant}] {world.Decisions.Count} decisions in 90 days — an exchange is looping");

        // Reports are bounded too, and separately: the decision budget alone did not catch
        // thirteen identical partial accounts, because filing one is a single cheap decision.
        Assert.True(world.Reports.Count < 25,
            $"[{variant}] {world.Reports.Count} reports in 90 days — the channel is repeating");

        // Requests are bounded by the questions there are to ask, not by how often anyone is
        // woken. Deliberately not "one per ordered pair" — that was the over-tight version that
        // shut the channel between two people permanently.
        Assert.Equal(
            world.Requests.Count,
            world.Requests.Select(r => (r.AskerId, r.AskedId, r.About)).Distinct().Count());
    }

    /// <summary>
    /// Pausing and resuming must not change the information history either — including the
    /// runaway path, which the existing replay test never covered because it only ran baseline.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("disloyal-vincent")]
    public void Pausing_does_not_change_reports_requests_or_testimony(string variant)
    {
        var straight = Run(variant);

        var resumed = Cast.Build(seed: 42, variant);
        Runner.Run(resumed, Cast.Start.AddDays(20));
        Runner.Run(resumed, Cast.Start.AddDays(55));
        Runner.Run(resumed, Cast.Start.AddDays(90));

        Assert.Equal(Channel(straight), Channel(resumed));
    }

    /// <summary>The report channel's state, flattened for comparison.</summary>
    private static string Channel(World world)
    {
        var lines = world.Reports.Select(r =>
            $"report|{r.Id}|{r.At:O}|{r.SenderId}|{r.RecipientId}|{r.Candor}|" +
            string.Join(",", r.Asserted.Select(a => $"{a.Claim}:{a.AssertedStance}")) +
            "|" + string.Join(",", r.Withheld));

        lines = lines.Concat(world.Requests.Select(q =>
            $"request|{q.Id}|{q.At:O}|{q.AskerId}|{q.AskedId}"));

        lines = lines.Concat(world.Characters.Values
            .OrderBy(c => c.Id, StringComparer.Ordinal)
            .SelectMany(c => c.Cognition.Testimony.Select(t =>
                $"testimony|{c.Id}|{t.SenderId}|{t.Claim}|{t.AssertedStance}|{t.At:O}")));

        return string.Join('\n', lines);
    }

    /// <summary>
    /// A position squeezed out by the length cap stays outstanding instead of being marked
    /// delivered. Eligibility is per claim; a report that never mentioned something has not
    /// covered it, however many other things it said.
    /// </summary>
    [Fact]
    public void A_position_crowded_out_of_a_report_is_not_treated_as_delivered()
    {
        var at = new DateTime(1987, 3, 2, 0, 0, 0, DateTimeKind.Utc);
        var told = new Claim(ClaimKind.BusinessRefusesTribute, "shop");
        var untold = new Claim(ClaimKind.PersonUsedViolence, "tommy", "shop", 1);

        var cognition = new Cognition();
        cognition.Learn(told, Stance.Believes, 0.9, SourceKind.Direct, "self", at);
        cognition.Learn(untold, Stance.Believes, 0.5, SourceKind.Direct, "self", at);

        // He reported, but only got one of them out.
        var sent = new[]
        {
            new Report(1, "vincent", "salvatore", at.AddDays(1), ReportCandor.Candid,
                new[] { new ReportedClaim(told, Stance.Believes, 0.7) },
                Array.Empty<Claim>(), "reported"),
        };

        Assert.True(
            Generators.HasSomethingToReport(sent, cognition.Records, "salvatore"),
            "the claim he never mentioned is still outstanding");

        Assert.False(Reporting.NeedsConveying(sent, "salvatore", cognition.Find(told)!));
        Assert.True(Reporting.NeedsConveying(sent, "salvatore", cognition.Find(untold)!));
    }

    /// <summary>
    /// Composition leads with what is news to this recipient, so a retraction cannot be crowded
    /// out by standing beliefs the recipient already has.
    /// </summary>
    [Fact]
    public void A_retraction_outranks_positions_the_recipient_already_has()
    {
        var world = Cast.Build(seed: 42, "baseline");
        var vincent = world.Get("vincent");
        var salvatore = world.Get(Viewpoint);
        var t0 = Cast.Start;

        // Enough standing beliefs to fill the report on their own.
        var filler = new[]
        {
            new Claim(ClaimKind.TargetIsVulnerable, Cast.Grocery),
            new Claim(ClaimKind.TributeCollected, Cast.Grocery),
            new Claim(ClaimKind.PoliceInvestigating, "tommy"),
            new Claim(ClaimKind.WitnessSawIncident, Cast.Grocery, "tommy", 1),
        };
        foreach (var f in filler)
            vincent.Cognition.Learn(f, Stance.Believes, 0.95, SourceKind.Direct, vincent.Id, t0);

        // And one thing he has taken back, held at lower confidence than any of the filler.
        var retracted = new Claim(ClaimKind.BusinessRefusesTribute, Cast.Grocery);
        vincent.Cognition.Learn(retracted, Stance.Believes, 0.8, SourceKind.Report, Viewpoint, t0);
        vincent.Cognition.Learn(retracted, Stance.Rejects, 0.3, SourceKind.Direct, vincent.Id, t0.AddDays(5));

        // He already told the boss all the filler, and the original affirmation.
        world.Reports.Add(new Report(
            world.NextReportId(), vincent.Id, Viewpoint, t0.AddDays(1), ReportCandor.Candid,
            filler.Select(f => new ReportedClaim(f, Stance.Believes, 0.7))
                  .Append(new ReportedClaim(retracted, Stance.Believes, 0.6))
                  .ToList(),
            Array.Empty<Claim>(), "reported"));

        world.Now = t0.AddDays(6);
        var candidate = new Candidate("report:salvatore", ActionKind.ReportToSuperior, "test", "report in")
        {
            TargetId = Viewpoint,
            Candor = ReportCandor.Candid,
        };
        var report = Reporting.Compose(
            world, vincent, salvatore, candidate, Salience.Perceive(vincent, world.Now));

        Assert.Contains(report.Asserted, a => a.Claim.Equals(retracted) && a.AssertedStance == Stance.Rejects);
    }

    private static World Run(string variant)
    {
        var world = Cast.Build(seed: 42, variant);
        Runner.Run(world, Cast.Start.AddDays(90));
        return world;
    }
}
