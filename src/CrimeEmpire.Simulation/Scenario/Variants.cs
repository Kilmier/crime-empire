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
    public static readonly string[] All = { "baseline", "cautious-vincent", "watchful-boss", "disloyal-vincent" };

    public static string Describe(string variant) => variant switch
    {
        "cautious-vincent" => "Vincent is careful rather than aggressive; nothing else changes.",
        "watchful-boss" => "The rule is firmer and Vincent owes Salvatore more; his traits are untouched.",
        "disloyal-vincent" => "Vincent owes Salvatore nothing and resents him; his traits are untouched.",
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
                vincent.Social.Toward("salvatore").Obligation = 0.0;
                vincent.Social.Toward("salvatore").Trust = 0.05;
                vincent.Social.Grievances.Add(
                    new Grievance("salvatore", "he has taken the harbour's earnings and given nothing back", 0.75, world.Now));
                vincent.Motivations.AddPressure(PressureKind.Resentment, 0.35);
                break;
            }

            case "watchful-boss":
            {
                // Same man, different position: the rule carries more weight and he owes more.
                var policy = world.Org.Policies[0];
                world.Org.Policies[0] = policy with { Strength = 0.90 };

                vincent.Social.Toward("salvatore").Obligation = 0.80;
                vincent.Social.Toward("salvatore").Trust = 0.70;
                vincent.Social.Grievances.RemoveAll(g => g.AgainstId == "salvatore");
                vincent.Motivations.Pressures[PressureKind.Resentment] = 0.0;
                break;
            }
        }
    }
}
