namespace CrimeSim.Scenario;

using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Sim;

/// <summary>
/// The harbour scenario: one organisation, one contested district, five people.
///
/// Deliberately small. The question this spike answers is whether the decision pipeline produces
/// motivated behaviour, and a bigger cast makes it harder to tell whether an odd trace came from
/// the pipeline or from the scenario.
///
/// Nothing here scripts an escalation. Vincent is given aggression, pride, a revenue problem and a
/// grievance; whether he breaks his boss's rule is a scoring outcome, and the variants exist to
/// check that it is genuinely contingent on those inputs.
/// </summary>
public static class Cast
{
    public const string OrgId = "greco-family";
    public const string Harbour = "harbour";
    public const string Grocery = "bellini-grocery";

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

        var kane = new Character
        {
            Id = "kane",
            Name = "Det. Iris Kane",
            RoleTitle = "detective",
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

        foreach (var c in new[] { salvatore, vincent, tommy, marco, kane })
            world.Characters[c.Id] = c;

        // ---------------------------------------------------------------- affiliations
        salvatore.Social.OrganizationId = OrgId;
        vincent.Social.OrganizationId = OrgId;
        tommy.Social.OrganizationId = OrgId;

        vincent.Social.Toward("salvatore").Trust = 0.45;
        vincent.Social.Toward("salvatore").Obligation = 0.35;
        vincent.Social.Toward("tommy").Trust = 0.70;

        tommy.Social.Toward("vincent").Trust = 0.80;
        tommy.Social.Toward("vincent").Obligation = 0.70;
        tommy.Social.Toward("salvatore").Trust = 0.30;
        tommy.Social.Toward("salvatore").Obligation = 0.40;

        salvatore.Social.Toward("vincent").Trust = 0.50;
        salvatore.Social.Toward("vincent").Obligation = 0.20;

        // He was passed over. This is a motive, not an instruction to betray anyone.
        vincent.Social.Grievances.Add(
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
