namespace CrimeSim.Sim;

public enum EventKind
{
    /// <summary>Leadership reviews organisational conditions and sets priorities/policies.</summary>
    OrgReview,

    /// <summary>An assignment reaches its recipient. Triggers interpretation.</summary>
    AssignmentDelivered,

    /// <summary>Periodic role/agenda review for an office holder.</summary>
    RoleReview,

    /// <summary>The next step of an active strategy comes due.</summary>
    StrategyStep,

    /// <summary>A strategy reached its completion condition.</summary>
    StrategyComplete,

    /// <summary>A strategy became blocked or invalid.</summary>
    StrategyBlocked,

    /// <summary>Something happened in the world that a character may react to.</summary>
    Incident,

    /// <summary>A pressure crossed a meaningful threshold.</summary>
    PressureThreshold,

    /// <summary>A character had the opportunity to observe a trace.</summary>
    ObservationOpportunity,

    /// <summary>Slow-moving world state advances (revenue, pressure decay).</summary>
    WorldTick,
}

/// <summary>
/// A trace is a discoverable artefact left by an event. Step 1 records them but nothing consumes
/// them yet; step 1b (observation, reports, player-facing log) is what reads this field. It exists
/// now so that legibility is an addition rather than a retrofit.
/// </summary>
public sealed record Trace(
    string Kind,
    string Description,
    string? DistrictId,
    double Discoverability);

public sealed class EventPayload
{
    public string? TargetId { get; init; }
    public long? AssignmentId { get; init; }
    public long? RelatedEventId { get; init; }
    public Domain.StrategyKind? Strategy { get; init; }
    public int StepIndex { get; init; }
    public string? Note { get; init; }

    /// <summary>Claims an observer would acquire if they notice this. Used by ObservationOpportunity.</summary>
    public IReadOnlyList<Domain.Claim> Claims { get; init; } = Array.Empty<Domain.Claim>();

    public double Discoverability { get; init; }

    public static readonly EventPayload None = new();
}

public sealed class ScheduledEvent
{
    public required long Id { get; init; }
    public required DateTime Time { get; init; }
    public required EventKind Kind { get; init; }

    /// <summary>The character this event wakes, or null for world-owned events.</summary>
    public required string? OwnerId { get; init; }

    /// <summary>Human-readable reason this event exists, used verbatim as the decision trigger.</summary>
    public required string Cause { get; init; }

    public EventPayload Payload { get; init; } = EventPayload.None;

    /// <summary>Populated by resolution; consumed in step 1b. See <see cref="Trace"/>.</summary>
    public List<Trace> Traces { get; } = new();

    public override string ToString() => $"[{Id}] {Time:yyyy-MM-dd HH:mm} {Kind} owner={OwnerId ?? "world"} :: {Cause}";
}
