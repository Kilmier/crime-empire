namespace CrimeSim.Domain;

public enum StrategyKind
{
    SecureTribute,
    ConcealIncident,
    InvestigateIncident,
}

/// <summary>How a tribute strategy is being pursued. The parameter, not a separate strategy.</summary>
public enum CoercionMethod
{
    Persuade,
    Threaten,
    Force,
}

/// <summary>A promise, order, or ongoing assignment. Supplies continuity and a cost for abandoning.</summary>
public sealed record Commitment(
    string Id,
    string Description,
    string? ToWhomId,
    DateTime Since,
    double Weight);

/// <summary>A running instance of a parameterised strategy.</summary>
public sealed class StrategyInstance
{
    /// <summary>
    /// Together with <see cref="LocalSequence"/>, the immutable identity of this instance for its
    /// entire life — unaffected by scheduling, by any other character's activity, or by delegation.
    /// Not the same question as who is currently executing it; see <see cref="DelegatedToId"/>.
    /// </summary>
    public required string OwnerId { get; init; }

    /// <summary>
    /// Assigned once from <see cref="Character.StrategyCount"/> at construction, exactly the
    /// DecisionCount pattern one level down. Never reused, never rewound — unlike StepIndex, which
    /// AlterStrategy legitimately rewinds to re-run a step under a new method. Occasion keys are
    /// built from (OwnerId, LocalSequence, advance ordinal), never from StepIndex, precisely because
    /// StepIndex can repeat and this must not.
    /// </summary>
    public required int LocalSequence { get; init; }

    public required StrategyKind Kind { get; init; }
    public required string Domain { get; init; }
    public string? TargetId { get; set; }
    public CoercionMethod Method { get; set; } = CoercionMethod.Persuade;
    public int StepIndex { get; set; }
    public required DateTime StartedAt { get; init; }
    public DateTime Deadline { get; set; }
    public long? AssignmentId { get; init; }
    public long? SourceEventId { get; init; }

    /// <summary>
    /// The ordinal the next delivered StrategyStep must carry to be accepted. Incremented exactly
    /// once, by Strategies.Advance, after a delivered event validates against it — never rewound,
    /// never touched anywhere else. This is what an occasion key is keyed to instead of any global
    /// scheduling identifier.
    /// </summary>
    public int NextAdvanceOrdinal { get; set; }

    /// <summary>Set when execution was handed to a subordinate. Control is transferred; outcome is not.</summary>
    public string? DelegatedToId { get; set; }

    /// <summary>
    /// The policy this course of action knowingly breaches, if any. Recorded on the instance so the
    /// breach survives past the decision that made it — consequences arrive later than choices.
    /// </summary>
    public string? BreachedPolicyId { get; set; }

    /// <summary>The scheduled step event, so abandoning can cancel it with a reason.</summary>
    public long? PendingStepEventId { get; set; }

    /// <summary>
    /// How many times this approach has visibly failed. Commitment supplies continuity, but
    /// evidence has to be able to erode it — otherwise a character repeats a losing method forever
    /// and reads as stubborn rather than motivated.
    /// </summary>
    public int FailedAttempts { get; set; }

    /// <summary>Whether the current method has already been brought to bear on the target.</summary>
    public bool PressureApplied { get; set; }

    public string Label => TargetId is null
        ? $"{Kind}({Domain})"
        : $"{Kind}({Domain}, target={TargetId}, method={Method})";
}

public sealed class ExecutionState
{
    /// <summary>What the character has chosen to pursue, in plain words, for the trace.</summary>
    public string? Intention { get; set; }

    public StrategyInstance? Strategy { get; set; }
    public List<Commitment> Commitments { get; } = new();

    /// <summary>Conditions that should wake this character early. Recorded on the decision that set them.</summary>
    public List<string> ReconsiderationTriggers { get; } = new();

    public DateTime? NextReview { get; set; }

    public double CommitmentWeight => Commitments.Sum(c => c.Weight);

    /// <summary>
    /// Incidents this character has already attempted to conceal, whether the attempt is still
    /// running or has since completed. MVP rule, not a permanent design commitment: one attempt per
    /// incident, enforced at Decision/Filters.cs's redundancy stage. See docs/CURRENT_MILESTONE.md.
    /// The eventual shape is likely incident-relative state about evidence and exposure in the world
    /// rather than a private per-character tally — a man who cleans up badly and learns the traces
    /// are still there has a real reason to go again.
    /// </summary>
    public List<Claim> AttemptedConcealments { get; } = new();
}
