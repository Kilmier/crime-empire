namespace CrimeSim.Domain;

public sealed record Grievance(string AgainstId, string Description, double Severity, DateTime At);

/// <summary>
/// One directed relationship. Loyalty is deliberately not a field here — it is derived in
/// Decision/Utility.cs from trust, obligation and the Belonging drive, because "loyal" collapses
/// distinctions (attachment vs obligation vs fear of consequence) that should behave differently.
/// </summary>
public sealed class Relationship
{
    public required string OtherId { get; init; }
    public double Trust { get; set; }
    public double Affection { get; set; }
    public double Fear { get; set; }

    /// <summary>Sense of owing this person — favours, patronage, formal subordination.</summary>
    public double Obligation { get; set; }
}

public sealed class SocialState
{
    private readonly Dictionary<string, Relationship> _relationships = new();

    public List<Grievance> Grievances { get; } = new();
    public string? OrganizationId { get; set; }

    public IReadOnlyDictionary<string, Relationship> Relationships => _relationships;

    public Relationship Toward(string otherId)
    {
        if (!_relationships.TryGetValue(otherId, out var r))
        {
            r = new Relationship { OtherId = otherId };
            _relationships[otherId] = r;
        }
        return r;
    }

    public double GrievanceAgainst(string otherId)
        => Grievances.Where(g => g.AgainstId == otherId).Sum(g => g.Severity);
}
