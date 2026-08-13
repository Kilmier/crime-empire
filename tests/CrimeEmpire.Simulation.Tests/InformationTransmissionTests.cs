using CrimeSim.Decision;
using CrimeSim.Domain;
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

        // An omission says less; it never says the opposite. This is the whole difference between
        // the two, and it is checkable from the record alone — unlike "everything he asserted he
        // still believes", which fails honestly, because beliefs move on after the report is sent.
        foreach (var partial in world.Reports.Where(r => r.Candor == ReportCandor.Partial))
        {
            Assert.DoesNotContain(partial.Asserted, a => a.AssertedStance == Stance.Rejects);
            Assert.NotEmpty(partial.Withheld);
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
                Array.Empty<ReportedClaim>(), Array.Empty<Claim>(), "reported"),
        };

        Assert.True(
            Generators.HasSomethingToReport(alreadyReported, cognition.Records, "salvatore"),
            "being contradicted since he last spoke is something to report");
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

        var spoke = new[]
        {
            new Report(1, "vincent", "salvatore", at.AddDays(1), ReportCandor.Candid,
                Array.Empty<ReportedClaim>(), Array.Empty<Claim>(), "reported"),
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

    private static World Run(string variant)
    {
        var world = Cast.Build(seed: 42, variant);
        Runner.Run(world, Cast.Start.AddDays(90));
        return world;
    }
}
