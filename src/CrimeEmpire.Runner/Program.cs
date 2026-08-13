using System.Security.Cryptography;
using System.Text;
using CrimeSim.Scenario;
using CrimeSim.Sim;
using CrimeSim.Trace;

int seed = 42;
int days = 90;
string variant = "baseline";
bool verify = false;
bool full = false;
bool compare = false;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--seed" when i + 1 < args.Length: seed = int.Parse(args[++i]); break;
        case "--days" when i + 1 < args.Length: days = int.Parse(args[++i]); break;
        case "--variant" when i + 1 < args.Length: variant = args[++i]; break;
        case "--verify": verify = true; break;
        case "--compare": compare = true; break;
        case "--full": full = true; break;
        case "--help":
            Console.WriteLine("""
                CrimeSim — behavioural spike

                  --seed N        world seed (default 42)
                  --days N        in-game days to run (default 90)
                  --variant NAME  baseline | cautious-vincent | watchful-boss
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
    foreach (var v in Variants.All)
    {
        var world = Cast.Build(seed, v);
        Runner.Run(world, Cast.Start.AddDays(days));
        string text = TraceWriter.Render(world, v, false);
        hashes.Add(Hash(text));

        var violence = world.TruthLog.Where(e => e.Kind == "violence").ToList();
        var grocery = world.Businesses[Cast.Grocery];
        var vincent = world.Get("vincent");
        var kane = world.Get("kane");

        Console.WriteLine($"  {v,-18} {Variants.Describe(v)}");
        Console.WriteLine($"  {"",-18} decisions:  {world.Decisions.Count}");
        Console.WriteLine($"  {"",-18} violence:   {(violence.Count == 0 ? "none" : $"{violence.Count} incident(s)")}");
        bool breached = violence.Count > 0 || vincent.Execution.Strategy?.BreachedPolicyId is not null;
        Console.WriteLine($"  {"",-18} policy:     {(breached ? "breached" : "held")}");
        Console.WriteLine($"  {"",-18} grocery:    {(grocery.PayingTribute ? "paying" : "not paying")}, resistance {grocery.Resistance:0.00}");
        Console.WriteLine($"  {"",-18} Kane knows: {kane.Cognition.Records.Count(r => r.IsHeld)} things");
        Console.WriteLine($"  {"",-18} hash:       {Hash(text)}");
        Console.WriteLine();
    }

    Console.WriteLine(hashes.Distinct().Count() == hashes.Count
        ? "Three configurations, three distinct histories."
        : "WARNING — variants converged on identical histories. Traits are not doing any work.");
    return 0;
}

Console.WriteLine(RunOnce(variant));
return 0;
