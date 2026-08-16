namespace CrimeSim.Scenario;

using CrimeSim.Domain;
using CrimeSim.Org;
using CrimeSim.Sim;

/// <summary>
/// Configuration-only variations on the same scenario.
///
/// These exist to falsify the model rather than to demonstrate it. If all three produce the same
/// history, the traits are decoration and the decision model is not doing the work it claims to.
/// No variant changes a line of behaviour code — only trait values, a policy strength, and the
/// social facts around Vincent.
/// </summary>
public static class Variants
{
    public static readonly string[] All =
        { "baseline", "cautious-vincent", "watchful-boss", "disloyal-vincent", "resentful-tommy" };

    public static string Describe(string variant) => variant switch
    {
        "cautious-vincent" => "Vincent is careful rather than aggressive; nothing else changes.",
        "watchful-boss" => "The rule is firmer and Vincent owes Salvatore more; his traits are untouched.",
        "disloyal-vincent" => "Vincent owes Salvatore nothing and resents him; his traits are untouched.",
        "resentful-tommy" => "Tommy owes Vincent nothing and resents him; Vincent still trusts Tommy.",
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
        }
    }
}
