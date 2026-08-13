namespace CrimeSim.Sim;

using CrimeSim.Decision;
using CrimeSim.Domain;
using CrimeSim.Org;

public sealed class Business
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string DistrictId { get; init; }
    public required string OwnerId { get; init; }

    public double MonthlyRevenue { get; set; }
    public bool PayingTribute { get; set; }

    /// <summary>How hard the owner resists demands. Objective; characters only estimate it.</summary>
    public double Resistance { get; set; }

    public bool Damaged { get; set; }
}

/// <summary>Authoritative record of what actually happened. Never consulted by decision-making.</summary>
public sealed record WorldEvent(
    long Id,
    DateTime At,
    string Kind,
    string ActorId,
    string? TargetId,
    string Summary,
    IReadOnlyList<Trace> Traces);

/// <summary>
/// Objective world state.
///
/// Note what is absent: there is no global police-attention or heat scalar. Police interest exists
/// only as claims held by specific characters. That is deliberate — INFORMATION_AND_LEGIBILITY.md's
/// anti-heat-bar tests require that attention and case strength be separable, and the cheapest way
/// to guarantee that is to never create the variable in the first place.
/// </summary>
public sealed class World
{
    public required int Seed { get; init; }
    public DateTime Now { get; set; }
    public EventQueue Queue { get; } = new();

    public Dictionary<string, Character> Characters { get; } = new();
    public Dictionary<string, Business> Businesses { get; } = new();
    public required Organization Org { get; init; }

    public List<WorldEvent> TruthLog { get; } = new();
    public List<DecisionRecord> Decisions { get; } = new();

    private long _nextWorldEventId = 1;
    private long _nextAssignmentId = 1;

    public Character Get(string id) => Characters[id];
    public Character? Find(string id) => Characters.TryGetValue(id, out var c) ? c : null;

    public long NextAssignmentId() => _nextAssignmentId++;

    public WorldEvent Record(
        string kind,
        string actorId,
        string? targetId,
        string summary,
        params Trace[] traces)
    {
        var ev = new WorldEvent(_nextWorldEventId++, Now, kind, actorId, targetId, summary, traces);
        TruthLog.Add(ev);
        return ev;
    }

    public IEnumerable<Character> ActiveCharacters()
        => Characters.Values.Where(c => c.Tier == Tier.Active).OrderBy(c => c.Id, StringComparer.Ordinal);

    public IEnumerable<Business> BusinessesIn(string districtId)
        => Businesses.Values.Where(b => b.DistrictId == districtId).OrderBy(b => b.Id, StringComparer.Ordinal);
}
