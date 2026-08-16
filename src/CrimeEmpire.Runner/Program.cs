using System.Security.Cryptography;
using System.Text;
using CrimeSim.Scenario;
using CrimeSim.Sim;
using CrimeSim.Decision;
using CrimeSim.Trace;

int seed = 42;
int days = 90;
string variant = "baseline";
bool verify = false;
bool full = false;
bool compare = false;
string? viewpoint = null;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--seed" when i + 1 < args.Length: seed = int.Parse(args[++i]); break;
        case "--days" when i + 1 < args.Length: days = int.Parse(args[++i]); break;
        case "--variant" when i + 1 < args.Length: variant = args[++i]; break;
        case "--viewpoint" when i + 1 < args.Length: viewpoint = args[++i]; break;
        case "--verify": verify = true; break;
        case "--compare": compare = true; break;
        case "--full": full = true; break;
        case "--help":
            Console.WriteLine("""
                CrimeSim — behavioural spike

                  --seed N        world seed (default 42)
                  --days N        in-game days to run (default 90)
                  --variant NAME  baseline | cautious-vincent | watchful-boss | disloyal-vincent
                                  | resentful-tommy
                  --viewpoint ID  show only what that character knows, as the player would see
                                  it (e.g. salvatore) — no truth log, no decision traces
                  --full          also show options that never occurred to the character
                  --verify        run the same seed twice and compare trace hashes
                  --compare       run all variants at one seed and summarise the differences
                """);
            return 0;
    }
}

string RunOnce(string v)
{
    var world = Cast.Build(seed, v);
    Runner.Run(world, Cast.Start.AddDays(days));
    return TraceWriter.Render(world, v, full);
}

static string Hash(string s)
    => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)))[..16];

if (verify)
{
    string a = RunOnce(variant);
    string b = RunOnce(variant);
    Console.WriteLine($"run A hash: {Hash(a)}");
    Console.WriteLine($"run B hash: {Hash(b)}");
    Console.WriteLine(a == b
        ? "DETERMINISTIC — identical histories from identical inputs."
        : "NON-DETERMINISTIC — same seed produced different histories. This is a bug.");
    return a == b ? 0 : 1;
}

if (compare)
{
    Console.WriteLine($"Variant comparison at seed {seed}, {days} days\n");
    var hashes = new List<string>();

    // What each configuration actually *did*, from structured decision fields. Kept separate from
    // the trace hash because they answer different questions and the difference has been misreported:
    // the hash covers wording and scores as well, so two variants that chose the identical action at
    // every decision were being counted as distinct histories.
    var behaviour = new List<string>();

    foreach (var v in Variants.All)
    {
        var world = Cast.Build(seed, v);
        Runner.Run(world, Cast.Start.AddDays(days));
        string text = TraceWriter.Render(world, v, false);
        hashes.Add(Hash(text));

        string actions = string.Join('\n', world.Decisions.Select(d => d.ChosenActionSignature()));
        behaviour.Add(actions);

        var violence = world.TruthLog.Where(e => e.Kind == "violence").ToList();
        var grocery = world.Businesses[Cast.Grocery];
        var bakery = world.Businesses[Cast.Bakery];
        var vincent = world.Get("vincent");
        var kane = world.Get("kane");

        Console.WriteLine($"  {v,-18} {Variants.Describe(v)}");
        Console.WriteLine($"  {"",-18} decisions:  {world.Decisions.Count}");
        Console.WriteLine($"  {"",-18} violence:   {(violence.Count == 0 ? "none" : $"{violence.Count} incident(s)")}");
        bool breached = violence.Count > 0 || vincent.Execution.Strategy?.BreachedPolicyId is not null;
        Console.WriteLine($"  {"",-18} policy:     {(breached ? "breached" : "held")}");
        Console.WriteLine($"  {"",-18} grocery:    {(grocery.PayingTribute ? "paying" : "not paying")}, resistance {grocery.Resistance:0.00}");
        Console.WriteLine($"  {"",-18} bakery:     {(bakery.PayingTribute ? "paying" : "not paying")}, resistance {bakery.Resistance:0.00}");
        Console.WriteLine($"  {"",-18} conflicts:  {world.AccountConflicts.Count} perceived");
        Console.WriteLine($"  {"",-18} Kane knows: {kane.Cognition.Records.Count(r => r.IsHeld)} things");
        Console.WriteLine($"  {"",-18} trace:      {Hash(text)}");
        Console.WriteLine($"  {"",-18} actions:    {Hash(actions)}");
        Console.WriteLine();
    }

    int distinctTraces = hashes.Distinct().Count();
    int distinctBehaviour = behaviour.Distinct().Count();

    Console.WriteLine($"{hashes.Count} configurations · {distinctTraces} distinct traces · " +
                      $"{distinctBehaviour} distinct chosen-action sequences.");

    if (distinctTraces < hashes.Count)
        Console.WriteLine("WARNING — variants produced identical traces. Traits are not doing any work.");
    else if (distinctBehaviour < hashes.Count)
        Console.WriteLine(
            $"NOTE — {hashes.Count - distinctBehaviour} configuration(s) chose the same actions as " +
            "another throughout, differing only in scores, wording or seeded state. The trace count " +
            "above is the weaker claim of the two; read the action count for behaviour.");
    return 0;
}

if (viewpoint is not null)
{
    // Deliberately not printed alongside the developer trace. Seeing them side by side would make
    // it far too easy to read the answer out of the truth log and believe the player could have
    // worked it out.
    var world = Cast.Build(seed, variant);
    Runner.Run(world, Cast.Start.AddDays(days));

    if (world.Find(viewpoint) is null)
    {
        Console.Error.WriteLine($"no such character: {viewpoint}");
        Console.Error.WriteLine($"try one of: {string.Join(", ", world.Characters.Keys.OrderBy(k => k, StringComparer.Ordinal))}");
        return 1;
    }

    Console.WriteLine(IntelligenceWriter.Render(world, viewpoint));
    return 0;
}

Console.WriteLine(RunOnce(variant));
return 0;
