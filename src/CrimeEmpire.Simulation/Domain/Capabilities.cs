namespace CrimeSim.Domain;

public enum Skill
{
    Coercion,
    Persuasion,
    Discretion,
    Investigation,
}

/// <summary>
/// Money this character actually received, kept on the same private capability state as the cash
/// balance it changed. Source and executor are ids until the player projection resolves names; the
/// receipt itself is never another character's state and carries no hidden business value.
/// </summary>
public sealed record CashReceipt(DateTime At, double Amount, string SourceId, string ExecutorId);

/// <summary>
/// Whether and how well a character can act. Capability gates candidates; it never creates desire.
/// </summary>
public sealed class Capabilities
{
    private readonly Dictionary<Skill, double> _skills;
    private readonly List<CashReceipt> _cashReceipts = new();

    public Capabilities(
        IReadOnlyDictionary<Skill, double>? skills = null,
        int crew = 0,
        double cash = 0,
        int authority = 0,
        IEnumerable<string>? districts = null)
    {
        _skills = skills is null ? new() : new(skills);
        Crew = crew;
        Cash = cash;
        Authority = authority;
        Districts = districts is null ? new HashSet<string>() : new HashSet<string>(districts);
    }

    /// <summary>Skill level in [0,1]. Absent skills read as 0.</summary>
    public double this[Skill s] => _skills.TryGetValue(s, out var v) ? v : 0.0;

    public int Crew { get; set; }
    public double Cash { get; set; }
    public IReadOnlyList<CashReceipt> CashReceipts => _cashReceipts;

    /// <summary>Apply and remember one owner-visible receipt as a single transaction.</summary>
    public void ReceiveCash(double amount, string sourceId, string executorId, DateTime at)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Cash += amount;
        _cashReceipts.Add(new CashReceipt(at, amount, sourceId, executorId));
    }

    /// <summary>Formal authority rank. Affects salience and social consequence, never possibility.</summary>
    public int Authority { get; }

    public HashSet<string> Districts { get; }

    public bool CanReach(string? districtId) => districtId is null || Districts.Contains(districtId);
}
