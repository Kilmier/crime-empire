namespace CrimeSim.Domain;

public enum PressureKind
{
    /// <summary>Assigned revenue is not arriving.</summary>
    RevenueShortfall,

    /// <summary>Believed police or prosecutorial interest.</summary>
    LegalExposure,

    /// <summary>Accumulated resentment toward the organisation or a superior.</summary>
    Resentment,

    /// <summary>Fear of a specific person or consequence.</summary>
    Fear,

    /// <summary>The organisation itself looks unstable.</summary>
    OrganizationalInstability,
}

public sealed record Ambition(string Id, string Description, Drive Serves);

/// <summary>A role-based duty that repeatedly demands attention.</summary>
public sealed record Responsibility(string Id, string Description, string Domain);

public sealed class Motivations
{
    public Ambition? Ambition { get; set; }
    public List<Responsibility> Responsibilities { get; } = new();
    public Dictionary<PressureKind, double> Pressures { get; } = new();

    /// <summary>Short-horizon concerns pushed by the reactive layer. Cleared once addressed.</summary>
    public List<string> ImmediateNeeds { get; } = new();

    public double Pressure(PressureKind kind) => Pressures.TryGetValue(kind, out var v) ? v : 0.0;

    public void AddPressure(PressureKind kind, double delta)
        => Pressures[kind] = Math.Clamp(Pressure(kind) + delta, 0.0, 1.0);

    public (PressureKind Kind, double Value)? Dominant()
    {
        if (Pressures.Count == 0) return null;
        var top = Pressures.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key).First();
        return top.Value <= 0.01 ? null : (top.Key, top.Value);
    }
}
