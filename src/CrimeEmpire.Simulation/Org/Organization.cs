namespace CrimeSim.Org;

using CrimeSim.Domain;

public enum OrgCondition
{
    RevenueLoss,
    TerritorialPressure,
    LeadershipInstability,
    LegalExposure,
}

/// <summary>A desired organisational outcome. Leadership sets a small number at a time.</summary>
public sealed record Priority(string Id, string Description, string Domain, double Weight);

public enum PolicyKind
{
    /// <summary>Avoid public violence in a district.</summary>
    NoPublicViolence,

    /// <summary>Keep a specific business unharmed.</summary>
    ProtectBusiness,
}

/// <summary>
/// A boundary or preference set by leadership.
///
/// Policies change salience and anticipated social consequence. They are NOT hard filters — a
/// character can breach one, and whether they do is a scoring outcome. Making policy mechanically
/// binding would delete authority violation from the simulation, which is one of the behaviours
/// this spike exists to observe.
/// </summary>
public sealed record Policy(string Id, string Description, PolicyKind Kind, string Domain, double Strength)
{
    /// <summary>The claim a character holds when they have been told this policy exists.</summary>
    public Claim AwarenessClaim(string orgId) => new(ClaimKind.PolicyIssued, orgId, Id);
}

/// <summary>A bounded area of responsibility that translates priorities into one person's remit.</summary>
public sealed class Office
{
    public required string Title { get; init; }
    public required string Domain { get; init; }
    public required int Authority { get; init; }
    public string? HolderId { get; set; }
}

/// <summary>
/// An objective handed from an issuer to a recipient. The recipient interprets it through their own
/// beliefs and motives — they may comply, delay, exceed authority, or conceal. Nothing here forces
/// execution; the assignment only creates a responsibility, a commitment and a deadline.
/// </summary>
public sealed record Assignment(
    long Id,
    string Objective,
    string IssuerId,
    string RecipientId,
    string Domain,
    IReadOnlyList<string> Constraints,
    IReadOnlyList<Claim> Disclosed,
    DateTime IssuedAt,
    DateTime Deadline);

public sealed class Organization
{
    public required string Id { get; init; }
    public required string Name { get; init; }

    public Dictionary<OrgCondition, double> Conditions { get; } = new();
    public List<Priority> Priorities { get; } = new();
    public List<Policy> Policies { get; } = new();
    public List<Office> Offices { get; } = new();
    public List<Assignment> Assignments { get; } = new();

    public string? BossId { get; set; }

    public double Condition(OrgCondition c) => Conditions.TryGetValue(c, out var v) ? v : 0.0;

    public void AdjustCondition(OrgCondition c, double delta)
        => Conditions[c] = Math.Clamp(Condition(c) + delta, 0.0, 1.0);

    public Office? OfficeFor(string characterId)
        => Offices.FirstOrDefault(o => o.HolderId == characterId);

    public Office? OfficeForDomain(string domain)
        => Offices.FirstOrDefault(o => o.Domain == domain);

    public IEnumerable<Policy> PoliciesForDomain(string domain)
        => Policies.Where(p => p.Domain == domain);

    public Policy? PolicyById(string id) => Policies.FirstOrDefault(p => p.Id == id);
}
