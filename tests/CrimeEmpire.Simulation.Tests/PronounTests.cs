using System.Text.RegularExpressions;
using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Scenario;
using CrimeSim.Session;
using CrimeSim.Sim;
using CrimeSim.Trace;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>
/// Milestone 011, item 5. A character is described as themselves on every player-facing surface.
///
/// Every such surface said "he". Det. Iris Kane is the only woman in the cast, and her own
/// intelligence view opened <c>WHAT HE HAS</c> and told her that "everything here is something
/// <i>he</i> saw or was told" — from milestone 003, when the view was built, to milestone 010. No
/// test caught it because no test had ever asserted that a character is described as themselves,
/// which is the whole content of this file.
///
/// The developer trace is deliberately still masculine throughout and is not checked here. It is a
/// debugging tool that <c>SIMULATION_ARCHITECTURE.md</c> separates from player-facing accounts by
/// name, and it stays on the carried-forward list rather than being quietly included.
/// </summary>
public sealed class PronounTests
{
    private static readonly Regex Masculine = new(@"\b(he|him|his|He|Him|His)\b", RegexOptions.Compiled);
    private static readonly Regex Feminine = new(@"\b(she|her|hers|She|Her|Hers)\b", RegexOptions.Compiled);

    /// <summary>
    /// The whole point, end to end, through the production renderer over a natural run. Her own view
    /// speaks of her as "she" and never as "he"; the men's views are the mirror image.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("cautious-vincent")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    [InlineData("resentful-tommy")]
    public void Every_viewpoint_is_described_as_themselves(string variant)
    {
        var world = Cast.Build(seed: 42, variant);
        Runner.Run(world, Cast.Start.AddDays(90));

        foreach (var who in world.Characters.Values.OrderBy(c => c.Id, StringComparer.Ordinal))
        {
            string view = IntelligenceWriter.Render(PlayerView.Build(world, who.Id, world.Now));
            bool isShe = ReferenceEquals(who.Pronouns, Pronouns.She);

            // Not vacuous: the view has to be talking about somebody in the first place.
            Assert.Matches(isShe ? Feminine : Masculine, view);
            Assert.DoesNotMatch(isShe ? Masculine : Feminine, view);
        }
    }

    /// <summary>
    /// And the same over the session boundary, which is what the Godot interface reads. The occasion
    /// and every option are built from a controlled character's own pronouns.
    /// </summary>
    [Fact]
    public void A_pending_decision_speaks_of_its_actor_as_themselves()
    {
        string HerText(string controlled)
        {
            var session = SimulationSession.Start(42, "baseline", controlled, controlled);
            session.AdvanceTo(Cast.Start.AddDays(90));
            var pending = session.Pending;
            return pending is null
                ? ""
                : string.Join('\n', new[] { pending.Occasion, pending.Focus }
                    .Concat(pending.Options.Select(o => o.Description))
                    .Where(s => !string.IsNullOrEmpty(s)));
        }

        string kane = HerText("kane");
        Assert.NotEqual("", kane);
        Assert.DoesNotMatch(Masculine, kane);

        string vincent = HerText("vincent");
        Assert.NotEqual("", vincent);
        Assert.DoesNotMatch(Feminine, vincent);
    }

    /// <summary>
    /// The snapshot carries how to refer to the people in it, not only their names — otherwise a
    /// renderer would have to guess, which is the position everything was in before.
    /// </summary>
    [Fact]
    public void The_boundary_carries_pronouns_for_the_viewpoint_and_for_the_people_in_it()
    {
        var world = Cast.Build(seed: 42, "baseline");
        Runner.Run(world, Cast.Start.AddDays(90));

        var vincentsView = PlayerView.Build(world, "vincent", world.Now);
        Assert.Equal(Pronouns.He, vincentsView.ViewpointPronouns);

        var kanesView = PlayerView.Build(world, "kane", world.Now);
        Assert.Equal(Pronouns.She, kanesView.ViewpointPronouns);

        foreach (var who in world.Characters.Values.OrderBy(c => c.Id, StringComparer.Ordinal))
            foreach (var attitude in PlayerView.Build(world, who.Id, world.Now).Attitudes)
                Assert.Equal(world.Get(attitude.PersonId).Pronouns, attitude.PersonPronouns);

        // **The loop above cannot fail, and saying so is the point.** No viewpoint in the accepted
        // scenario holds a scored relationship with the one woman in it: the attitude list filters
        // to non-zero trust, fear or grievance, and Kane's only relationship is the all-zero one
        // `Relations.Meet` records when she puts her question to Tommy. So a boundary that returned
        // "he" for everybody passed every assertion above, which the mutation check duly showed.
        // Staged below, on the one path the fixture cannot reach.
        Assert.DoesNotContain(
            world.Characters.Values.SelectMany(c => PlayerView.Build(world, c.Id, world.Now).Attitudes),
            a => ReferenceEquals(a.PersonPronouns, Pronouns.She));

        Relations.Establish(world.Get("tommy"), "kane", trust: 0.4);
        var withKane = PlayerView.Build(world, "tommy", world.Now).Attitudes
            .Single(a => a.PersonId == "kane");

        // "he takes her as he finds her" — Tommy is the subject and Kane is the object, so both
        // forms belong in the sentence and only the object's is under test here.
        Assert.Equal(Pronouns.She, withKane.PersonPronouns);
        Assert.Equal("he takes her as he finds her", withKane.Standing);
    }

    /// <summary>
    /// Verb agreement is real and is not faked. A pronoun set that produced "they has nothing to
    /// decide" would be worse than the defect it replaced, which is why <see cref="Pronouns.They"/>
    /// is usable rather than decorative even though nobody in the current cast takes it.
    /// </summary>
    [Fact]
    public void Plural_pronouns_agree_with_their_verbs()
    {
        Assert.Equal("has", Pronouns.He.Verb("has", "have"));
        Assert.Equal("has", Pronouns.She.Verb("has", "have"));
        Assert.Equal("have", Pronouns.They.Verb("has", "have"));

        Assert.Equal("He", Pronouns.He.Subject_);
        Assert.Equal("She", Pronouns.She.Subject_);
        Assert.Equal("They", Pronouns.They.Subject_);
    }

    /// <summary>
    /// Every phrase the narrator can produce, driven through it for each pronoun set, so a branch
    /// nobody exercises in the accepted scenario cannot keep a hardcoded "he". Enumerated from the
    /// production enums rather than from a list written here.
    /// </summary>
    [Fact]
    public void No_narrated_phrase_hardcodes_a_pronoun()
    {
        var world = Cast.Build(seed: 1, "baseline");
        var said = new List<string>();

        foreach (var source in Enum.GetValues<SourceKind>())
        {
            var record = new InformationRecord(
                new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery, 7),
                Stance.Believes, 0.5, source, "vincent", world.Now);
            said.Add(PlayerNarration.Attribute(record, id => id, Pronouns.She));
            if (PlayerNarration.OwnBasis(record, Pronouns.She) is { } basis) said.Add(basis);
        }

        foreach (double trust in new[] { 0.0, 0.1, 0.2, 0.5, 0.9 })
            said.Add(PlayerNarration.Standing(trust, Pronouns.She, Pronouns.She));

        foreach (double fear in new[] { 0.3, 0.9 })
            said.Add(PlayerNarration.Wariness(fear, Pronouns.She, Pronouns.She)!);

        Assert.NotEmpty(said);
        foreach (string phrase in said)
            Assert.DoesNotMatch(Masculine, phrase);
    }
}
