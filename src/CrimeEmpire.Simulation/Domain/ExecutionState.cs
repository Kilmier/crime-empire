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

    /// <summary>
    /// The method the owner last chose while the operation was his to direct. It remains his own
    /// remembered order after delegation even if the executor later changes the live
    /// <see cref="Method"/> privately. Null only on legacy or deliberately hand-built fixtures that
    /// never passed through a start decision; player projection then falls back without exposing a
    /// delegate-side change.
    /// </summary>
    public CoercionMethod? OwnerOrderedMethod { get; set; }
    public string? CommissionedExecutorId { get; init; }
    public IReadOnlyList<ReportedClaim> InitialBriefing { get; init; } = Array.Empty<ReportedClaim>();
    public int StepIndex { get; set; }
    public required DateTime StartedAt { get; init; }
    public DateTime Deadline { get; set; }
    public long? AssignmentId { get; init; }

    /// <summary>
    /// The incident this instance is about, as the truth-log id of the event that produced it.
    ///
    /// Set from <c>Candidate.AboutIncident</c> at commitment: a concealment is about the incident it
    /// is covering up, and an investigation is about the incident its lead belongs to, and neither's
    /// steps can act on an incident the instance cannot name. Populated for
    /// <see cref="StrategyKind.ConcealIncident"/> since milestone 010 and for
    /// <see cref="StrategyKind.InvestigateIncident"/> since milestone 011 — both read it throughout
    /// their own <c>Strategies.cs</c> advance methods rather than falling back on <c>TargetId</c>,
    /// which names an address rather than an incident. Null when the strategy is about no particular
    /// incident, and also when the incident claim carries no event id — an incident nobody can
    /// identify is not one a step may act on, and treating the default 0 as a key would collapse
    /// every unidentified claim into a single shared incident, which is the same scan defect one
    /// level down.
    ///
    /// Declared since milestone 001 and set by nothing at all until milestone 010.
    /// </summary>
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

    /// <summary>
    /// Who chose the currently operative prohibited method — the decision, never who owns the
    /// instance and never who happens to be carrying it out. Set by whichever <c>StartStrategy</c>
    /// or <c>AlterStrategy</c> commit first introduces the breach, and updated only when a later
    /// <c>AlterStrategy</c> genuinely changes which prohibited method is operative (the method
    /// actually moves). A later delegation, a different executor, or a repeated/no-op
    /// <c>AlterStrategy</c> that leaves the operative method unchanged must not rewrite who made the
    /// original choice. Null whenever <see cref="BreachedPolicyId"/> is null.
    ///
    /// Ownership alone establishes neither this nor <c>PersonUsedViolence</c>'s own attribution
    /// (which is already correctly the executor at the moment violence resolves, since that claim's
    /// subject has always been whoever is running the step). This field exists because the breach
    /// is different: it is a standing decision made once, not a recurring act, and the owner who
    /// chose a prohibited method and only then delegated its execution must not be let off the hook
    /// by the man he sent, nor must that man be credited with a decision he never made.
    /// </summary>
    public string? PolicyBreachDecisionMakerId { get; set; }

    /// <summary>The scheduled step event, so abandoning can cancel it with a reason.</summary>
    public long? PendingStepEventId { get; set; }
    public long? PendingReviewEventId { get; set; }

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

public sealed record OperationLearning(string OwnerId, int Sequence, InformationRecord Position);
public sealed record OperationAccount(string OwnerId, int Sequence, string SenderId, DateTime At,
    Claim Claim, Stance Stance);

public sealed class ExecutionState
{
    // Attribution follows information acquired during an actual operation, not address/time guesses.
    public Dictionary<Claim, OperationLearning> OperationLearning { get; } = new();
    public List<OperationAccount> OperationAccounts { get; } = new();
    /// <summary>What the character has chosen to pursue, in plain words, for the trace.</summary>
    public string? Intention { get; set; }

    /// <summary>Active work commissioned by this actor, in creation order.</summary>
    public List<StrategyInstance> Operations { get; } = new();

    /// <summary>The owned operation this actor executes personally, not supervised work.</summary>
    public StrategyInstance? Strategy
    {
        get => Operations.SingleOrDefault(s => s.DelegatedToId is null);
        set
        {
            if (Strategy is { } previous) Operations.Remove(previous);
            if (value is not null) Operations.Add(value);
        }
    }
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

    /// <summary>
    /// Everyone who has ever executed delegated work for this character, in the order they were
    /// first handed something. Never removed.
    ///
    /// Deliberately outliving the strategy instance. Being owed an account of work you ordered does
    /// not stop being true when the operation finishes — and the first version of the delegator's
    /// question generator read <see cref="StrategyInstance.DelegatedToId"/> instead, which meant the
    /// standing to ask existed only while the strategy was live. That is exactly the window in which
    /// a man is too busy to ask: the question was offered only in competition with carrying on, lost
    /// four-to-nothing every time, and had evaporated by the time he was free. The same principle
    /// the architecture states for demotion applies here — do not discard state a later consequence
    /// depends on.
    /// </summary>
    public List<string> DelegatedExecutorIds { get; } = new();

    /// <summary>Records a delegation, ignoring a repeat of one already held.</summary>
    public void RecordDelegation(string executorId)
    {
        if (!DelegatedExecutorIds.Contains(executorId)) DelegatedExecutorIds.Add(executorId);
    }
}
