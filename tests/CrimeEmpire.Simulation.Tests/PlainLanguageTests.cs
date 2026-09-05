using CrimeSim.Domain;
using CrimeSim.Scenario;
using CrimeSim.Session;
using CrimeSim.Sim;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>
/// Milestone 025: the words the player reads, and the voice they are read in.
///
/// The rewording itself has no test beyond a person reading the screen — that is the milestone's
/// stated verification. What can be pinned is the structure under the words: the certainty phrase
/// changes at exactly the bands the domain's own label uses, it is omitted where it would be
/// redundant, an act of the reader's own carries no source line, a rule reaches the player as its
/// description rather than its id, a ledger is not a person, and the session speaks as "you" to the
/// character the player controls and about "him" to everybody else.
/// </summary>
public sealed class PlainLanguageTests
{
    private const int Seed = 42;

    private static readonly Claim Grocery = new(ClaimKind.BusinessRefusesTribute, Cast.Grocery);

    private static InformationRecord Record(double confidence, SourceKind kind = SourceKind.Report, string source = "salvatore")
        => new(Grocery, Stance.Believes, confidence, kind, source, Cast.Start);

    // ================================================================= certainty

    /// <summary>
    /// The plain phrase and the developer label draw the same five bands. The label reaches the
    /// trace, so it is not reused; this walks the whole range and pins that the two change at
    /// exactly the same points, which is the only way two copies of a threshold stay one.
    /// </summary>
    [Fact]
    public void Certainty_bands_change_exactly_where_the_domain_label_does()
    {
        string? previousLabel = null, previousPhrase = null;
        var phrases = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i <= 1000; i++)
        {
            double c = i / 1000.0;
            var r = Record(c);
            string label = r.ConfidenceLabel;
            string phrase = PlayerNarration.Certainty(r, contested: false, Pronouns.He)!;
            phrases.Add(phrase);

            if (previousLabel is not null)
                Assert.Equal(previousLabel != label, previousPhrase != phrase);

            previousLabel = label;
            previousPhrase = phrase;
        }

        Assert.Equal(5, phrases.Count);
        Assert.All(phrases, p => Assert.DoesNotMatch(@"\d", p));
    }

    /// <summary>"You saw it yourself; you are certain of it" says one thing twice.</summary>
    [Theory]
    [InlineData(SourceKind.Witness)]
    [InlineData(SourceKind.Participant)]
    public void Certainty_is_omitted_for_what_he_saw_or_did_himself(SourceKind kind)
        => Assert.Null(PlayerNarration.Certainty(Record(1.0, kind, "vincent"), contested: false, Pronouns.He));

    [Theory]
    [InlineData(SourceKind.Report)]
    [InlineData(SourceKind.Discovery)]
    [InlineData(SourceKind.Inference)]
    [InlineData(SourceKind.FirstHandTestimony)]
    [InlineData(SourceKind.Rumor)]
    public void Certainty_is_stated_for_everything_he_did_not_see_or_do(SourceKind kind)
        => Assert.NotNull(PlayerNarration.Certainty(Record(0.8, kind), contested: false, Pronouns.He));

    /// <summary>Somebody having told him otherwise replaces how sure he was, whatever the source.</summary>
    [Fact]
    public void A_disputed_belief_says_so_instead_of_how_sure_he_is()
        => Assert.Equal("disputed", PlayerNarration.Certainty(Record(1.0, SourceKind.Witness), contested: true, Pronouns.He));

    // ================================================================= attribution

    /// <summary>
    /// "You broke the rule; you had a hand in it yourself" names the author twice. The source line
    /// goes; the sentence keeps him. Anybody else's act he was party to keeps the line, because
    /// having had a hand in it is information.
    /// </summary>
    [Fact]
    public void An_act_of_his_own_carries_no_source_line_and_reads_as_you()
    {
        var world = Cast.Build(Seed, "baseline");
        var vincent = world.Get("vincent");
        var breach = new Claim(ClaimKind.PersonBreachedPolicy, "vincent", "no-violence-harbour");
        var tommysViolence = new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery);
        vincent.Cognition.Learn(breach, Stance.Knows, 1.0, SourceKind.Participant, "vincent", Cast.Start);
        vincent.Cognition.Learn(tommysViolence, Stance.Knows, 1.0, SourceKind.Participant, "vincent", Cast.Start);

        var asYou = PlayerView.Build(world, "vincent", world.Now, PlayerView.You);
        var own = asYou.Known.Single(b => b.Claim.Matches(breach));
        Assert.Equal("you broke the rule: no public violence in the harbour", own.Statement);
        Assert.Null(own.Attribution);
        Assert.Null(own.Certainty);

        var partyTo = asYou.Known.Single(b => b.Claim.Matches(tommysViolence));
        Assert.Equal("Tommy Nardo got violent at Bellini's grocery", partyTo.Statement);
        Assert.Equal("you had a hand in it yourself", partyTo.Attribution);

        // Watched rather than played: the name stays and so does the line.
        var asHim = PlayerView.Build(world, "vincent", world.Now);
        Assert.Equal("Vincent Russo broke the rule: no public violence in the harbour",
            asHim.Known.Single(b => b.Claim.Matches(breach)).Statement);
    }

    /// <summary>The boss holds his own rule as its author, and a rule is not something one "had a hand in".</summary>
    [Fact]
    public void The_boss_set_his_own_rule_himself()
    {
        var world = Cast.Build(Seed, "baseline");
        var rule = PlayerView.Build(world, "salvatore", world.Now).Known
            .Single(b => b.Claim.Kind == ClaimKind.PolicyIssued);

        Assert.Equal("the outfit's rule: no public violence in the harbour", rule.Statement);
        Assert.Equal("he set it himself", rule.Attribution);
        Assert.Null(rule.Certainty);
    }

    /// <summary>
    /// "the rule 'no-violence-harbour' stands" was a developer id in the player's hands. The
    /// description is prose a person wrote and the character was told; that is what crosses.
    /// </summary>
    [Fact]
    public void A_rule_reaches_the_player_as_its_description_never_its_id()
    {
        var session = SimulationSession.Start(Seed, "baseline", "vincent");
        AdvanceToPause(session);

        string everything = string.Join('\n',
            session.Snapshot().Known.Select(b => b.Statement)
                .Concat(session.Pending!.Options.Select(o => o.Description)));

        Assert.Contains("no public violence in the harbour", everything, StringComparison.Ordinal);
        Assert.DoesNotContain("no-violence-harbour", everything, StringComparison.Ordinal);
    }

    /// <summary>"the books told him" read as though a Mr. Books had. A source that is not a person says so.</summary>
    [Fact]
    public void A_source_that_is_not_a_person_says_so_rather_than_telling_him()
    {
        var world = Cast.Build(Seed, "baseline");
        var fromTheBooks = PlayerView.Build(world, "salvatore", world.Now).Known
            .Single(b => b.Claim.Matches(Grocery));

        Assert.Equal("the books say so", fromTheBooks.Attribution);
    }

    /// <summary>Finding the aftermath is not seeing the act, and the phrase says nothing about what was found.</summary>
    [Fact]
    public void Discovery_claims_no_sight()
    {
        string phrase = PlayerNarration.Attribute(Record(0.6, SourceKind.Discovery, "vincent"), id => id, Pronouns.He);
        Assert.Equal("he found out for himself", phrase);
        Assert.DoesNotContain("saw", phrase, StringComparison.OrdinalIgnoreCase);
    }

    // ================================================================= voice

    /// <summary>
    /// The second person is a pronoun set and nothing else: the same fields, worded for the reader.
    /// The session chooses it exactly when the viewpoint is the character under control.
    /// </summary>
    [Fact]
    public void The_session_speaks_as_you_only_to_the_character_the_player_controls()
    {
        var played = SimulationSession.Start(Seed, "baseline", "vincent");
        var watched = SimulationSession.Start(Seed, "baseline", controlledCharacterId: null, viewpointCharacterId: "vincent");
        var developer = SimulationSession.Start(Seed, "baseline", "vincent", "salvatore");
        foreach (var s in new[] { played, watched, developer }) s.AdvanceDays(7);

        Assert.Equal(PlayerView.You, played.Snapshot().ViewpointPronouns);
        Assert.Equal(Pronouns.He, watched.Snapshot().ViewpointPronouns);
        Assert.Equal(Pronouns.He, developer.Snapshot().ViewpointPronouns);

        Assert.All(played.Snapshot().Attitudes, a => Assert.StartsWith("you ", a.Standing));
        Assert.All(watched.Snapshot().Attitudes, a => Assert.StartsWith("he ", a.Standing));
    }

    /// <summary>A pause is always the controlled character's own, so the decision is put as "you".</summary>
    [Fact]
    public void A_decision_is_put_to_the_player_as_you()
    {
        var session = SimulationSession.Start(Seed, "baseline", "vincent");
        AdvanceToPause(session);

        Assert.Equal(PlayerView.You, session.Pending!.ActorPronouns);
        Assert.Equal("you have just been given a job", session.Pending.Occasion);
    }

    /// <summary>"you has nothing to decide" would be worse than the defect it replaced.</summary>
    [Fact]
    public void The_second_person_agrees_with_its_verbs()
    {
        Assert.Equal("have", PlayerView.You.Verb("has", "have"));
        Assert.Equal("are", PlayerView.You.Verb("is", "are"));
        Assert.Equal("You", PlayerView.You.Subject_);
        Assert.Equal("yourself", PlayerView.You.Reflexive);
        Assert.True(PlayerView.IsSecondPerson(PlayerView.You));
        Assert.False(PlayerView.IsSecondPerson(Pronouns.They));
    }

    // ================================================================= fixtures

    private static void AdvanceToPause(SimulationSession session)
    {
        for (int guard = 0; guard < 5000 && session.Status != SessionStatus.AwaitingChoice; guard++)
            session.StepEvent();
        Assert.Equal(SessionStatus.AwaitingChoice, session.Status);
    }
}
