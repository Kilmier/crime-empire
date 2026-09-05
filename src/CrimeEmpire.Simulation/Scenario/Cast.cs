namespace CrimeSim.Scenario;

using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Sim;

/// <summary>
/// The harbour scenario: one organisation, one contested district, six people, two businesses.
///
/// Deliberately small. The question this spike answers is whether the decision pipeline produces
/// motivated behaviour, and a bigger cast makes it harder to tell whether an odd trace came from
/// the pipeline or from the scenario.
///
/// Nothing here scripts an escalation. Vincent is given aggression, pride, a revenue problem and a
/// grievance; whether he breaks his boss's rule is a scoring outcome, and the variants exist to
/// check that it is genuinely contingent on those inputs.
///
/// <b>Why there are two businesses (milestone 007).</b> With one, the whole run had exactly one
/// collection cycle: the grocery paid, <c>RevenueLoss</c> dropped half a point, nothing was left to
/// tick it back over <see cref="Org.Organization"/>'s review threshold, and the last third of the
/// simulation was a boss choosing to do nothing eleven times. Three consecutive milestones had ended
/// with a correct mechanism the scenario could not demonstrate, and the reason was structural rather
/// than any mechanism's fault — one line of causation has nowhere to put a second event. A second
/// contested business keeps the organisational condition alive, which produces a second assignment,
/// a second briefing, and a second delegation, and those are where relationships get read.
///
/// The second shop needs an owner because <c>AdvanceTribute</c> resolves a demand through the
/// owner's own decision rather than a roll on his behalf. Sharing Marco would have been worse than
/// a sixth character: <c>Commit</c>'s concede and refuse paths find a business by owner and would
/// have had him answering for the wrong shop.
/// </summary>
public static class Cast
{
    public const string OrgId = "greco-family";
    public const string Harbour = "harbour";
    public const string Grocery = "bellini-grocery";

    /// <summary>
    /// The second contested business, and the one nobody has told the boss about.
    ///
    /// Sorts after <see cref="Grocery"/>, and that is determinism-relevant rather than incidental:
    /// <c>World.BusinessesIn</c> orders by id, and <c>FromResponsibility</c> falls back to the first
    /// visible target when the character believes no particular business is holding out. Pinned by a
    /// test rather than left to a naming coincidence.
    /// </summary>
    public const string Bakery = "dorato-bakery";

    public static readonly DateTime Start = new(1987, 3, 2, 8, 0, 0, DateTimeKind.Utc);

    public static World Build(int seed, string variant)
    {
        var org = new Organization { Id = OrgId, Name = "the Greco family", BossId = "salvatore" };
        var world = new World { Seed = seed, Org = org, Now = Start };

        // ---------------------------------------------------------------- people
        var salvatore = new Character
        {
            Id = "salvatore",
            Name = "Salvatore Greco",
            RoleTitle = "boss",
            Pronouns = Pronouns.He,
            Capabilities = new Capabilities(
                new Dictionary<Skill, double>
                {
                    [Skill.Persuasion] = 0.70, [Skill.Coercion] = 0.40,
                    [Skill.Discretion] = 0.65, [Skill.Investigation] = 0.15,
                },
                crew: 3, cash: 40000, authority: 3, districts: new[] { Harbour }),
            Psychology = new Psychology(
                new Dictionary<Trait, double>
                {
                    [Trait.Cautious] = 0.70, [Trait.Proud] = 0.60,
                    [Trait.Aggressive] = 0.20, [Trait.Suspicious] = 0.45,
                },
                new Dictionary<Drive, double>
                {
                    [Drive.Security] = 0.80, [Drive.Status] = 0.70,
                    [Drive.Wealth] = 0.50, [Drive.Belonging] = 0.45,
                }),
        };

        var vincent = new Character
        {
            Id = "vincent",
            Name = "Vincent Russo",
            RoleTitle = "capo, harbour",
            Pronouns = Pronouns.He,
            Capabilities = new Capabilities(
                new Dictionary<Skill, double>
                {
                    [Skill.Coercion] = 0.75, [Skill.Persuasion] = 0.40,
                    [Skill.Discretion] = 0.35, [Skill.Investigation] = 0.10,
                },
                crew: 4, cash: 6000, authority: 2, districts: new[] { Harbour }),
            Psychology = new Psychology(
                new Dictionary<Trait, double>
                {
                    [Trait.Aggressive] = 0.80, [Trait.Proud] = 0.75,
                    [Trait.Cautious] = 0.15, [Trait.Suspicious] = 0.30,
                },
                new Dictionary<Drive, double>
                {
                    [Drive.Status] = 0.85, [Drive.Wealth] = 0.70,
                    [Drive.Security] = 0.30, [Drive.Belonging] = 0.35,
                }),
        };

        var tommy = new Character
        {
            Id = "tommy",
            Name = "Tommy Nardo",
            RoleTitle = "soldier",
            Pronouns = Pronouns.He,
            Capabilities = new Capabilities(
                new Dictionary<Skill, double>
                {
                    [Skill.Coercion] = 0.55, [Skill.Persuasion] = 0.25,
                    [Skill.Discretion] = 0.30, [Skill.Investigation] = 0.10,
                },
                crew: 1, cash: 900, authority: 1, districts: new[] { Harbour }),
            Psychology = new Psychology(
                new Dictionary<Trait, double>
                {
                    [Trait.Cautious] = 0.65, [Trait.Aggressive] = 0.35,
                    [Trait.Proud] = 0.20, [Trait.Suspicious] = 0.25,
                },
                new Dictionary<Drive, double>
                {
                    [Drive.Belonging] = 0.80, [Drive.Security] = 0.60,
                    [Drive.Wealth] = 0.40, [Drive.Status] = 0.25,
                }),
        };

        var marco = new Character
        {
            Id = "marco",
            Name = "Marco Bellini",
            RoleTitle = "grocer",
            Pronouns = Pronouns.He,
            Capabilities = new Capabilities(
                new Dictionary<Skill, double> { [Skill.Persuasion] = 0.45, [Skill.Discretion] = 0.30 },
                crew: 0, cash: 3000, authority: 0, districts: new[] { Harbour }),
            Psychology = new Psychology(
                new Dictionary<Trait, double> { [Trait.Cautious] = 0.60, [Trait.Proud] = 0.55, [Trait.Suspicious] = 0.40 },
                new Dictionary<Drive, double>
                {
                    // He leans toward keeping his money. Fear of Vincent is what moves him, and
                    // fear is a relationship that has to be built, not a starting stat.
                    [Drive.Wealth] = 0.75, [Drive.Security] = 0.50,
                    [Drive.Status] = 0.35, [Drive.Belonging] = 0.35,
                }),
        };

        var nunzio = new Character
        {
            Id = "nunzio",
            Name = "Nunzio Dorato",
            RoleTitle = "baker",
            Pronouns = Pronouns.He,
            Capabilities = new Capabilities(
                new Dictionary<Skill, double> { [Skill.Persuasion] = 0.35, [Skill.Discretion] = 0.40 },
                crew: 0, cash: 2600, authority: 0, districts: new[] { Harbour }),
            Psychology = new Psychology(
                // Softer than Marco on pride and harder on security: two shopkeepers who fold at
                // different points make the second cycle a second experiment rather than a replay.
                // Nothing here is tuned toward an outcome — the collection has to win its own
                // scoring competition against his wealth drive exactly as the first one did.
                new Dictionary<Trait, double> { [Trait.Cautious] = 0.70, [Trait.Proud] = 0.35, [Trait.Suspicious] = 0.45 },
                new Dictionary<Drive, double>
                {
                    [Drive.Wealth] = 0.65, [Drive.Security] = 0.65,
                    [Drive.Status] = 0.25, [Drive.Belonging] = 0.40,
                }),
        };

        var kane = new Character
        {
            Id = "kane",
            Name = "Det. Iris Kane",
            RoleTitle = "detective",
            Pronouns = Pronouns.She,
            Capabilities = new Capabilities(
                new Dictionary<Skill, double>
                {
                    [Skill.Investigation] = 0.70, [Skill.Persuasion] = 0.50,
                    [Skill.Discretion] = 0.45, [Skill.Coercion] = 0.20,
                },
                crew: 2, cash: 0, authority: 0, districts: new[] { Harbour }),
            Psychology = new Psychology(
                new Dictionary<Trait, double>
                {
                    [Trait.Suspicious] = 0.70, [Trait.Cautious] = 0.50, [Trait.Proud] = 0.40,
                },
                new Dictionary<Drive, double>
                {
                    [Drive.Status] = 0.60, [Drive.Security] = 0.40, [Drive.Belonging] = 0.30,
                }),
        };

        foreach (var c in new[] { salvatore, vincent, tommy, marco, nunzio, kane })
            world.Characters[c.Id] = c;

        // ---------------------------------------------------------------- affiliations
        salvatore.Social.OrganizationId = OrgId;
        vincent.Social.OrganizationId = OrgId;
        tommy.Social.OrganizationId = OrgId;

        // Starting relationships go through Relations like every later change, so there is one door
        // rather than two. A separate seeding path that could set dimensions directly would be the
        // obvious place for ad-hoc mutation to creep back in unnoticed.
        Relations.Establish(vincent, "salvatore", trust: 0.45, obligation: 0.35);
        Relations.Establish(vincent, "tommy", trust: 0.70);

        Relations.Establish(tommy, "vincent", trust: 0.80, obligation: 0.70);
        Relations.Establish(tommy, "salvatore", trust: 0.30, obligation: 0.40);

        Relations.Establish(salvatore, "vincent", trust: 0.50, obligation: 0.20);

        // He was passed over. This is a motive, not an instruction to betray anyone.
        Relations.RaiseGrievance(vincent,
            new Grievance("salvatore", "the harbour was handed to me only after it stopped earning", 0.35, Start));
        vincent.Motivations.AddPressure(PressureKind.RevenueShortfall, 0.50);
        vincent.Motivations.AddPressure(PressureKind.Resentment, 0.30);
        vincent.Motivations.Ambition = new Ambition("a-underboss", "be made underboss", Drive.Status);

        kane.Motivations.Responsibilities.Add(
            new Responsibility("r-harbour-cases", "clear cases in the harbour district", Harbour));
        salvatore.Motivations.Responsibilities.Add(
            new Responsibility("r-family", "keep the family earning and quiet", Harbour));

        // ---------------------------------------------------------------- places
        world.Businesses[Grocery] = new Business
        {
            Id = Grocery,
            Name = "Bellini's grocery",
            DistrictId = Harbour,
            OwnerId = "marco",
            MonthlyRevenue = 4200,
            PayingTribute = false,
            Resistance = 0.55,
        };

        world.Businesses[Bakery] = new Business
        {
            Id = Bakery,
            Name = "Dorato's bakery",
            DistrictId = Harbour,
            OwnerId = "nunzio",
            MonthlyRevenue = 3100,
            PayingTribute = false,
            Resistance = 0.50,
        };

        // ---------------------------------------------------------------- institution
        org.Offices.Add(new Office { Title = "capo, harbour", Domain = Harbour, Authority = 2, HolderId = "vincent" });
        org.Conditions[OrgCondition.RevenueLoss] = 0.55;

        var policy = new Policy(
            "no-violence-harbour",
            "no public violence in the harbour",
            PolicyKind.NoPublicViolence,
            Harbour,
            0.60);
        org.Policies.Add(policy);

        // The boss knows his own rule and knows the money is short. Nobody else knows either yet.
        // He set the rule, so he holds it as its author rather than as something he found out.
        salvatore.Cognition.Learn(policy.AwarenessClaim(OrgId), Stance.Knows, 1.0,
            SourceKind.Participant, "salvatore", Start);
        // A scenario fixture, not something the simulation routed: he starts the run already
        // believing this, on the word of a bookkeeper nobody models. "the books" is a source
        // outside the cast, which is what makes it a report rather than something he found — and
        // what makes it corroboratable, since a thing you were told is a thing worth checking.
        //
        // NOTE WHAT HE IS NOT TOLD. The bakery is also refusing, and nobody has mentioned it to him.
        // The organisational condition below is objective — the family's takings really are short by
        // both shops — while his account of *why* names only one of them. That is the truth/knowledge
        // distinction the whole project rests on, applied to an organisation's own books, and it is
        // the fixture's most productive asymmetry: he goes on telling his capo the grocery will not
        // pay after his capo has personally watched it start paying, because the shortfall he can see
        // has a cause he cannot.
        //
        // It was first written the other way, with both shops in his head, and that is worth
        // recording because the difference was not cosmetic. Knowing about the bakery handed Vincent
        // a fresh collection job on the same wake where he would otherwise have gone to ask his own
        // man for an account — so the delegator's question, freed by milestone 007's scoring fix, was
        // immediately crowded out by a second errand. Partial knowledge is not a workaround here; it
        // is the thing that leaves him room to think.
        salvatore.Cognition.Learn(new Claim(ClaimKind.BusinessRefusesTribute, Grocery),
            Stance.Believes, 0.75, SourceKind.Report, "the books", Start);
        salvatore.Cognition.Learn(new Claim(ClaimKind.TargetIsVulnerable, Grocery),
            Stance.Suspects, 0.45, SourceKind.Inference, "salvatore", Start);

        Variants.Apply(world, variant);

        // ---------------------------------------------------------------- opening events
        world.Queue.Schedule(Start, EventKind.WorldTick, null, "the simulation begins");
        world.Queue.Schedule(Start.AddHours(1), EventKind.OrgReview, "salvatore", "the month's takings came in short");

        return world;
    }
}
