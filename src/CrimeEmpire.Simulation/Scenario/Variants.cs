namespace CrimeSim.Scenario;

using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Sim;

/// <summary>
/// Configuration-only variations on the same scenario.
///
/// These exist to falsify the model rather than to demonstrate it. If all three produce the same
/// history, the traits are decoration and the decision model is not doing the work it claims to.
/// Almost every variant changes no line of behaviour code — only trait values, a policy strength,
/// and the social facts around Vincent. <c>capable-angelo</c> is the one deliberate exception,
/// the same kind milestone 007 needed for adding <c>nunzio</c> to <c>Cast.Build</c>: it adds a
/// second organisational subordinate, authorized by Matt's own milestone-020 scope text (see
/// <c>docs/CURRENT_MILESTONE.md</c>, "Ruling — the seventh character") and bounded to this one
/// variant so every other variant's <see cref="World"/> is exactly what it always was.
/// </summary>
public static class Variants
{
    public static readonly string[] All =
    {
        "baseline", "cautious-vincent", "watchful-boss", "disloyal-vincent", "resentful-tommy",
        "capable-angelo",
    };

    public static string Describe(string variant) => variant switch
    {
        "cautious-vincent" => "Vincent is careful rather than aggressive; nothing else changes.",
        "watchful-boss" => "The rule is firmer and Vincent owes Salvatore more; his traits are untouched.",
        "disloyal-vincent" => "Vincent owes Salvatore nothing and resents him; his traits are untouched.",
        "resentful-tommy" => "Tommy owes Vincent nothing and resents him; Vincent still trusts Tommy.",
        "capable-angelo" => "Vincent gains a second man, harder-hitting than Tommy but far less trusted; nothing else changes.",
        _ => "Vincent as written: aggressive, proud, short of money, carrying a grudge.",
    };

    public static void Apply(World world, string variant)
    {
        var vincent = world.Get("vincent");

        switch (variant)
        {
            case "cautious-vincent":
                vincent.Psychology = new Psychology(
                    new Dictionary<Trait, double>
                    {
                        [Trait.Aggressive] = 0.20, [Trait.Cautious] = 0.70,
                        [Trait.Proud] = 0.30, [Trait.Suspicious] = 0.30,
                    },
                    new Dictionary<Drive, double>
                    {
                        [Drive.Status] = 0.85, [Drive.Wealth] = 0.70,
                        [Drive.Security] = 0.30, [Drive.Belonging] = 0.35,
                    });
                break;

            case "disloyal-vincent":
            {
                // The mirror of watchful-boss, and the reason it exists: what a man is willing to
                // say to his superior should turn on what he owes him, not on his temperament.
                // Vincent's traits are untouched here — only the bond is cut. If this produced the
                // same account as the baseline, the reporting model would be decorative.
                Relations.Establish(vincent, "salvatore", trust: 0.05, obligation: 0.0);
                Relations.RaiseGrievance(vincent,
                    new Grievance("salvatore", "he has taken the harbour's earnings and given nothing back", 0.75, world.Now));
                vincent.Motivations.AddPressure(PressureKind.Resentment, 0.35);
                break;
            }

            case "watchful-boss":
            {
                // Same man, different position: the rule carries more weight and he owes more.
                var policy = world.Org.Policies[0];
                world.Org.Policies[0] = policy with { Strength = 0.90 };

                Relations.Establish(vincent, "salvatore", trust: 0.70, obligation: 0.80);
                Relations.ClearGrievancesAgainst(vincent, "salvatore");
                vincent.Motivations.Pressures[PressureKind.Resentment] = 0.0;
                break;
            }

            case "resentful-tommy":
            {
                // The same lever as disloyal-vincent, one rung further down the chain. The asymmetry
                // is the point rather than an oversight: Vincent's relationship toward Tommy is
                // untouched, so he still trusts him enough to hand him the job. Only Tommy's side of
                // the pair is cut. Relationships are directional, and a man can be trusted by
                // somebody he has stopped caring about.
                //
                // WHAT THIS DOES NOT DO, and the name reflects it. It was added to make an executor
                // deny his own act to his delegator — milestone 004's central distinction, still
                // provable only in unit tests. It does not achieve that, and it is not named as
                // though it does.
                //
                // The reason recorded here was itself wrong twice over and is corrected rather than
                // quietly deleted. It said Tommy never gives Vincent an account at all, and that
                // fixing it would need a milestone of its own. Both are false. Tommy volunteers
                // accounts to Vincent — withholding asserts nothing, which is why no *contradiction*
                // followed — and milestone 006's `Generators.FromDelegation` gave a delegator the
                // standing to ask, which milestone 007's scoring correction then let him actually
                // use: Vincent puts the question in play, on 6 April in the accepted run.
                //
                // What still does not happen is the denial. Tommy answers, and answers honestly,
                // because he believes the street saw him and `Utility` prices a denial almost
                // entirely on that belief. Cutting his side of the pair does not change it: at
                // loyalty zero the denial still loses by a wide margin. So this variant continues to
                // make the same decisions as baseline, and `--compare` now says so in its own right
                // rather than leaving a trace hash to imply otherwise. It is kept, untuned and
                // uncut, because the asymmetry is a real fixture and manufacturing distinctness
                // would be inventing a result.
                var tommy = world.Get("tommy");
                Relations.Establish(tommy, "vincent", trust: 0.10, obligation: 0.05);
                Relations.RaiseGrievance(tommy,
                    new Grievance("vincent", "he sends me to do the things he will not be seen doing", 0.60, world.Now));
                tommy.Motivations.AddPressure(PressureKind.Resentment, 0.30);
                break;
            }

            case "capable-angelo":
            {
                // Milestone 020. A second organisational subordinate under Vincent, so
                // Generators.FromRelationship's delegation choice is genuinely between two known
                // people rather than a single default. Added here, not in Cast.Build, so every
                // other variant's World is byte-for-byte what it always was.
                var angelo = new Character
                {
                    Id = "angelo",
                    Name = "Angelo Conti",
                    RoleTitle = "soldier",
                    Pronouns = Pronouns.He,
                    Capabilities = new Capabilities(
                        new Dictionary<Skill, double>
                        {
                            // Harder-hitting than both Tommy (0.55) and Vincent (0.75) — the
                            // actual tension the variant exists to pose: the man Vincent trusts
                            // less is the man better suited to the job.
                            [Skill.Coercion] = 0.80, [Skill.Persuasion] = 0.20,
                            [Skill.Discretion] = 0.25, [Skill.Investigation] = 0.10,
                        },
                        crew: 1, cash: 700, authority: 1, districts: new[] { Cast.Harbour }),
                    Psychology = new Psychology(
                        // Kept broadly parallel to Tommy's shape rather than a clone or a foil,
                        // so the variant isolates trust and capability instead of adding a third
                        // variable nobody asked to measure.
                        new Dictionary<Trait, double>
                        {
                            [Trait.Aggressive] = 0.45, [Trait.Cautious] = 0.55,
                            [Trait.Proud] = 0.30, [Trait.Suspicious] = 0.30,
                        },
                        new Dictionary<Drive, double>
                        {
                            [Drive.Belonging] = 0.70, [Drive.Security] = 0.55,
                            [Drive.Wealth] = 0.45, [Drive.Status] = 0.35,
                        }),
                };
                world.Characters[angelo.Id] = angelo;
                angelo.Social.OrganizationId = Cast.OrgId;

                // Deliberately below Vincent's trust in Tommy (0.70): the less-trusted,
                // more-capable option, which is the whole point of the variant. Angelo's own
                // relationship toward Vincent and Salvatore is set up the same shape Tommy's is,
                // so he behaves like a real cast member once delegated to rather than a
                // degenerate edge case. Salvatore's relationship toward Angelo is left
                // unestablished — reads as zero, the same as Salvatore's toward Tommy, which is
                // likewise never set from that side.
                Relations.Establish(vincent, "angelo", trust: 0.35, obligation: 0.10);
                Relations.Establish(angelo, "vincent", trust: 0.65, obligation: 0.55);
                Relations.Establish(angelo, "salvatore", trust: 0.25, obligation: 0.30);
                break;
            }
        }
    }
}
