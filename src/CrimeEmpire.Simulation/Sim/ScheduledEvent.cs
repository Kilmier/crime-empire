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
    /// <summary>
    /// Who was carrying a strategy when it completed. Set on <see cref="EventKind.StrategyComplete"/>
    /// so a player-facing reader can distinguish work the owner did from work he knowingly handed
    /// to somebody else without retaining the whole completed strategy instance.
    /// </summary>
    public string? ExecutorId { get; init; }
    public long? AssignmentId { get; init; }
    public long? RelatedEventId { get; init; }
    public Domain.StrategyKind? Strategy { get; init; }
    public int StepIndex { get; init; }
    public string? Note { get; init; }

    /// <summary>Claims an observer would acquire if they notice this. Used by ObservationOpportunity.</summary>
    public IReadOnlyList<Domain.Claim> Claims { get; init; } = Array.Empty<Domain.Claim>();

    public double Discoverability { get; init; }

    /// <summary>
    /// How the claims above would be acquired if the observer notices them, and what they would be
    /// attributed to. Used by ObservationOpportunity; milestone 022.
    ///
    /// <b>Not every chance to notice is the same kind of noticing, and treating them alike was the
    /// defect.</b> A detective who went looking and found a wrecked shopfront established that
    /// herself. A man who works the same street and hears what happened did not: he was told, by
    /// nobody in particular. Before this the scheduler could say how likely somebody was to find
    /// out and not how, so both were filed as <see cref="Domain.SourceKind.Discovery"/> sourced to
    /// the observer — which gave street talk the resistance to contradiction of a thing you saw
    /// yourself, and made it ineligible for the corroboration it most obviously deserves. See
    /// <c>Provenance.cs</c> on why bundling acquisition categories is the mistake this repeats.
    ///
    /// <see cref="AttributedTo"/> null means the observer himself, which is what an own-reading
    /// category requires. A rumour names a place instead, per
    /// <c>INFORMATION_AND_LEGIBILITY.md</c>'s "a rumor attributed to a neighborhood or source" —
    /// never a person, because the whole point of talk going round is that there is nobody to go
    /// back to.
    /// </summary>
    public Domain.SourceKind AcquiredAs { get; init; } = Domain.SourceKind.Discovery;

    /// <summary>Who or what the acquired claims are attributed to. Null means the observer himself.</summary>
    public string? AttributedTo { get; init; }

    /// <summary>
    /// What the recipient of an <c>asked-to-account</c> event is being asked about.
    ///
    /// The request itself records a subject, but the event that wakes the respondent is what
    /// carries it to him. Without this the man being asked knows only who wanted a word, and
    /// answers with whatever was on his mind — which is not an answer to anything.
    /// </summary>
    public Domain.Claim? AboutClaim { get; init; }

    /// <summary>
    /// Together with StrategySequence and AdvanceOrdinal, the exact strategy-instance identity a
    /// StrategyStep event was scheduled for. Strategies.Advance validates all three, plus that this
    /// event is the instance's own PendingStepEventId and that the awakened character is who the
    /// step was addressed to — a delivery that fails any of the five throws rather than advancing
    /// whatever instance happens to be running now.
    /// </summary>
    public string? StrategyOwnerId { get; init; }
    public int? StrategySequence { get; init; }
    public int? AdvanceOrdinal { get; init; }

    /// <summary>
    /// The RNG occasion key for an ObservationOpportunity, built at the scheduling site from causally
    /// local identity (the strategy advance and trace kind that produced the opportunity, plus the
    /// observer) — never reconstructed at dispatch from this event's own Id or any other global
    /// identifier.
    /// </summary>
    public string? OccasionKey { get; init; }

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
