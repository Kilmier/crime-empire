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
                // though it does. Tommy never gives Vincent an account at all: the only character
                // who puts the question is Salvatore, and being asked redirects the answer to the
                // asker, so the soldier's account goes to the boss and never to the capo who sent
                // him. That is structural, not a matter of degree — no configuration of trust,
                // obligation or grievance changes who asks. Fixing it means changing who seeks
                // corroboration from whom, which is behaviour code and a milestone of its own.
                //
                // It is kept because the directional asymmetry above is worth having as a fixture
                // and becomes live the moment a delegator-to-executor question path exists. Note
                // its limitation honestly when reading --compare: it currently produces the same
                // decisions as baseline.
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
