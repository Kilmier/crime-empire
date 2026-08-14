using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Scenario;
using CrimeSim.Sim;
using CrimeSim.Trace;

namespace CrimeEmpire.Simulation.Tests;

/// <summary>
/// Knowledge has to travel. Nobody acquires anything by rank, by shared employer, or by having
/// ordered it — only by an information-bearing act or a discoverable trace.
/// </summary>
public sealed class KnowledgeRoutingTests
{
    private const string Viewpoint = "salvatore";

    private static World Run(string variant)
    {
        var world = Cast.Build(seed: 42, variant);
        Runner.Run(world, Cast.Start.AddDays(90));
        return world;
    }

    /// <summary>
    /// The defect this milestone's correction exists for. Ordering a beating told Vincent it had
    /// been carried out, sourced to Tommy, before Tommy had said a word — a report that never left
    /// anyone's mouth, and a player-facing account naming a source that had not spoken.
    ///
    /// He may hold that he gave the order. What he may not hold, without a channel, is that it was
    /// done.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("cautious-vincent")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    public void Ordering_something_does_not_tell_you_it_was_carried_out(string variant)
    {
        var world = Run(variant);

        foreach (var who in world.Characters.Values)
        {
            foreach (var held in who.Cognition.Records.Where(r => r.SourceKind == SourceKind.FirstHandTestimony))
            {
                // First-hand testimony can only come from somebody having testified. Every such
                // record must correspond to an account actually logged from that speaker.
                Assert.True(
                    who.Cognition.Testimony.Any(t => t.Claim.Equals(held.Claim) && t.SenderId == held.SourceId),
                    $"[{variant}] {who.Id} holds {held.Claim} as first-hand testimony from "
                    + $"{held.SourceId}, but {held.SourceId} never gave him an account of it");
            }
        }
    }

    /// <summary>
    /// Nothing a character holds may have arrived without an event behind it. Every belief is
    /// either his own act, something he saw, something he found, something he reasoned to, or
    /// something a named source actually said to him.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("disloyal-vincent")]
    public void No_belief_arrives_without_an_information_bearing_event(string variant)
    {
        var world = Run(variant);

        foreach (var who in world.Characters.Values)
        foreach (var held in who.Cognition.Records)
        {
            if (held.SourceKind.IsSelfAcquired() || held.SourceKind == SourceKind.Inference)
            {
                Assert.Equal(who.Id, held.SourceId);
                continue;
            }

            // Sources outside the cast are scenario fixtures — the bookkeeper who told Salvatore
            // the takings were short is not a character and cannot testify. The guarantee under
            // test is about the simulation's own routing: if it names a *character* as the source,
            // that character has to have actually spoken.
            if (world.Find(held.SourceId) is null) continue;

            Assert.True(
                who.Cognition.Testimony.Any(t => t.Claim.Equals(held.Claim) && t.SenderId == held.SourceId),
                $"[{variant}] {who.Id} holds {held.Claim} from {held.SourceId} with no account on record");
        }
    }

    /// <summary>
    /// Rank is not a channel. A superior does not inherit what his subordinates know, and members
    /// of one organisation do not pool what they hold.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("disloyal-vincent")]
    public void Hierarchy_and_shared_membership_transfer_nothing(string variant)
    {
        var world = Run(variant);
        var org = world.Org;

        foreach (var who in world.Characters.Values.Where(c => c.Social.OrganizationId == org.Id))
        foreach (var held in who.Cognition.Records.Where(r => r.IsHeld))
        {
            bool ownDoing = held.SourceKind.IsSelfAcquired() || held.SourceKind == SourceKind.Inference;
            bool wasTold = who.Cognition.Testimony.Any(t => t.Claim.Equals(held.Claim));

            Assert.True(ownDoing || wasTold,
                $"[{variant}] {who.Id} holds {held.Claim} without acquiring it or being told — "
                + "the only remaining explanation is that the organisation handed it to him");
        }
    }

    /// <summary>
    /// A participant's own account arrives as first-hand testimony; a man passing on what he was
    /// told, or what he found, or what he worked out, is giving a report.
    /// </summary>
    [Fact]
    public void Provenance_survives_transmission_and_hearsay_stays_hearsay()
    {
        var at = Cast.Start;
        var claim = new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery, 1);

        foreach (var (basis, expected) in new[]
        {
            (SourceKind.Participant, SourceKind.FirstHandTestimony),
            (SourceKind.Witness, SourceKind.FirstHandTestimony),
            (SourceKind.Discovery, SourceKind.Report),
            (SourceKind.Inference, SourceKind.Report),
            (SourceKind.Report, SourceKind.Report),
            (SourceKind.FirstHandTestimony, SourceKind.Report),
            (SourceKind.Rumor, SourceKind.Report),
        })
        {
            var listener = new Cognition();
            listener.Receive(new ReportedClaim(claim, Stance.Believes, 0.7, basis), "tommy", at);

            Assert.Equal(expected, listener.Find(claim)!.SourceKind);

            // Whatever it lands as, the speaker's own basis is preserved verbatim in the log.
            Assert.Equal(basis, listener.Testimony.Single().SpeakerBasis);
        }
    }

    /// <summary>
    /// Repeating first-hand testimony makes it hearsay. Being told by the man who was there is not
    /// the same as being told by a man who was told by the man who was there, and the chain must
    /// not launder itself back into first-hand at every hop.
    /// </summary>
    [Fact]
    public void Relayed_testimony_does_not_stay_first_hand()
    {
        var at = Cast.Start;
        var claim = new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery, 1);

        var vincent = new Cognition();
        vincent.Receive(new ReportedClaim(claim, Stance.Believes, 0.8, SourceKind.Participant), "tommy", at);
        Assert.Equal(SourceKind.FirstHandTestimony, vincent.Find(claim)!.SourceKind);

        var salvatore = new Cognition();
        salvatore.Receive(
            new ReportedClaim(claim, Stance.Believes, 0.7, vincent.Find(claim)!.SourceKind),
            "vincent", at.AddDays(1));

        Assert.Equal(SourceKind.Report, salvatore.Find(claim)!.SourceKind);
    }

    /// <summary>
    /// Discovery is a reading of a trace, and a reading can be wrong. It must be displaceable by a
    /// firmer account and contestable by a contrary one — where doing it or seeing it is not.
    /// </summary>
    [Fact]
    public void A_weak_discovery_can_be_displaced_and_contested()
    {
        var at = Cast.Start;
        var claim = new Claim(ClaimKind.PersonUsedViolence, "tommy", Cast.Grocery, 1);

        // Displaced: a firmer later record wins, where an unmediated one would have won regardless.
        var found = new Cognition();
        found.Learn(claim, Stance.Believes, 0.4, SourceKind.Discovery, "self", at);
        found.Learn(claim, Stance.Rejects, 0.9, SourceKind.Report, "vincent", at.AddDays(1));
        Assert.Equal(SourceKind.Report, found.Find(claim)?.SourceKind ?? SourceKind.Report);
        Assert.False(found.Holds(claim));

        // And a weak Discovery does not displace a firmer prior record.
        var firmFirst = new Cognition();
        firmFirst.Learn(claim, Stance.Believes, 0.9, SourceKind.Report, "vincent", at);
        firmFirst.Learn(claim, Stance.Rejects, 0.2, SourceKind.Discovery, "self", at.AddDays(1));
        Assert.True(firmFirst.Holds(claim), "a faint reading must not evict a firm account");

        // Contested: contradiction erodes it at the ordinary rate and can take its stance.
        var contested = new Cognition();
        contested.Learn(claim, Stance.Believes, 0.5, SourceKind.Discovery, "self", at);
        contested.Receive(new ReportedClaim(claim, Stance.Rejects, 0.9, SourceKind.Participant), "tommy", at.AddDays(1));
        Assert.False(contested.Find(claim)!.IsHeld,
            "an interpreted trace must be arguable, unlike something he did or saw");

        // The contrast: what he saw survives the same denial.
        var saw = new Cognition();
        saw.Learn(claim, Stance.Believes, 0.5, SourceKind.Witness, "self", at);
        saw.Receive(new ReportedClaim(claim, Stance.Rejects, 0.9, SourceKind.Participant), "tommy", at.AddDays(1));
        Assert.True(saw.Find(claim)!.IsHeld);
    }

    /// <summary>
    /// The player-facing account may never invent a source. Every attribution naming another
    /// character must correspond to that character having actually given an account.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("cautious-vincent")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    public void The_player_account_never_invents_a_source(string variant)
    {
        var world = Run(variant);

        foreach (var who in world.Characters.Values)
        {
            string view = IntelligenceWriter.Render(world, who.Id);

            foreach (var other in world.Characters.Values.Where(c => c.Id != who.Id))
            {
                bool everSpoke = who.Cognition.Testimony.Any(t => t.SenderId == other.Id);
                if (everSpoke) continue;

                foreach (var phrase in new[]
                {
                    $"{other.Name} told him",
                    $"{other.Name} was in it and told him so",
                })
                {
                    Assert.DoesNotContain(phrase, view, StringComparison.Ordinal);
                }
            }
        }
    }

    /// <summary>
    /// Determinism and pause/resume equivalence, over the state this correction touches: every
    /// record's provenance and source, and every testimony entry including the speaker's basis.
    /// </summary>
    [Theory]
    [InlineData("baseline")]
    [InlineData("cautious-vincent")]
    [InlineData("watchful-boss")]
    [InlineData("disloyal-vincent")]
    public void Provenance_and_testimony_survive_pausing(string variant)
    {
        static string Shape(World w) => string.Join('\n', w.Characters.Values
            .OrderBy(c => c.Id, StringComparer.Ordinal)
            .SelectMany(c => c.Cognition.Records
                .Select(r => $"know|{c.Id}|{r.Claim}|{r.Stance}|{r.SourceKind}|{r.SourceId}")
                .Concat(c.Cognition.Testimony
                    .Select(t => $"said|{c.Id}|{t.SenderId}|{t.Claim}|{t.AssertedStance}|{t.SpeakerBasis}"))));

        var straight = Run(variant);

        var resumed = Cast.Build(seed: 42, variant);
        Runner.Run(resumed, Cast.Start.AddDays(17));
        Runner.Run(resumed, Cast.Start.AddDays(48));
        Runner.Run(resumed, Cast.Start.AddDays(90));

        Assert.Equal(Shape(straight), Shape(resumed));

        var again = Run(variant);
        Assert.Equal(Shape(straight), Shape(again));
    }
}
