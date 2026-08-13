namespace CrimeSim.Decision;

using CrimeSim.Domain;

public enum ActionKind
{
    ContinueStrategy,
    AlterStrategy,
    DelegateStrategy,
    PostponeStrategy,
    AbandonStrategy,
    StartStrategy,
    ReportToSuperior,
    SeekApproval,
    RequestHelp,
    Retaliate,
    Concede,
    Refuse,
    DoNothing,
}

/// <summary>
/// One possibility that occurred to a character. Requirements are declared on the candidate rather
/// than checked inside each generator, so Filters can reject uniformly and — importantly — record
/// *which* requirement failed. "Rejected because he did not know" is the single most load-bearing
/// line in a decision trace.
/// </summary>
public sealed record Candidate(
    string Id,
    ActionKind Kind,
    string Generator,
    string Description)
{
    public string? TargetId { get; init; }
    public StrategyKind? Strategy { get; init; }
    public CoercionMethod? Method { get; init; }
    public string? Domain { get; init; }

    /// <summary>Claims the character must actually hold for this to be conceivable.</summary>
    public IReadOnlyList<Claim> RequiredKnowledge { get; init; } = Array.Empty<Claim>();

    public Skill? RequiredSkill { get; init; }
    public double RequiredSkillLevel { get; init; }
    public int RequiredCrew { get; init; }
    public int RequiredAuthority { get; init; }

    /// <summary>
    /// Set when this candidate would breach a policy. Only ever populated from policies the
    /// character actually knows about — an unknown policy cannot deter anyone.
    /// </summary>
    public string? BreachesPolicyId { get; init; }
    public double BreachesPolicyStrength { get; init; }
    public string? PolicyIssuerId { get; init; }

    public override string ToString() => Description;
}

public enum RejectionStage
{
    /// <summary>Did not occur to the character at all. Trait- and circumstance-driven.</summary>
    Salience,

    /// <summary>The character lacks a fact the option depends on.</summary>
    Knowledge,

    /// <summary>Insufficient skill, crew, or money.</summary>
    Capability,

    /// <summary>No access, or no standing to act.</summary>
    Access,
}

public sealed record Rejection(Candidate Candidate, RejectionStage Stage, string Reason);
