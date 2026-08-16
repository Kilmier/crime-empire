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

    /// <summary>Ask someone other than the usual reporter for their account of the same thing.</summary>
    SeekCorroboration,

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

    /// <summary>
    /// How honest this report would be. Set only on <see cref="ActionKind.ReportToSuperior"/>.
    ///
    /// Candour is a candidate rather than a switch inside the reporting code because that is the
    /// only way it can be a *choice*: it occurs to the character or does not (salience), and it
    /// wins or loses against the alternatives on its merits (utility). Deciding it inside Commit
    /// would put behaviour where nothing can weigh it.
    /// </summary>
    public ReportCandor? Candor { get; init; }

    /// <summary>
    /// Claims a deceptive report would suppress or deny, each carrying what this recipient has
    /// already been given about it. Populated with Candor.
    ///
    /// The prior state is on the claim rather than in a second list, so a scorer cannot pair the
    /// wrong two together, and so it cannot be dropped on the way from the generator that knows the
    /// sender's message history to the scorer that prices what a further concealment is worth.
    /// </summary>
    public IReadOnlyList<SuppressedClaim> Suppressed { get; init; } = Array.Empty<SuppressedClaim>();

    /// <summary>
    /// What a <see cref="ActionKind.SeekCorroboration"/> is about. A question has a subject; a
    /// request recorded without one becomes a permanent bar on ever asking that person anything
    /// again.
    /// </summary>
    public Claim? AboutClaim { get; init; }

    /// <summary>
    /// The question this report is an answer to, when it is one.
    ///
    /// A man asked about one thing and a man filing his weekly account are doing different acts.
    /// Set, <see cref="Org.Reporting.Compose"/> restricts the report to this claim, so an answer
    /// addresses what was asked instead of whatever three things happened to be most newsworthy.
    /// </summary>
    public Claim? AnsweringClaim { get; init; }

    /// <summary>Claims the character must actually hold for this to be conceivable.</summary>
    public IReadOnlyList<Claim> RequiredKnowledge { get; init; } = Array.Empty<Claim>();

    /// <summary>
    /// The incident a ConcealIncident candidate is about. Set only there. Filters' redundancy stage
    /// uses it to enforce the one-attempt-per-incident MVP rule regardless of whether the earlier
    /// attempt is still running or has already completed — a state (Kind, TargetId) match alone
    /// cannot express "already tried and finished", since a finished strategy is no longer running.
    /// </summary>
    public Claim? AboutIncident { get; init; }

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
    /// <summary>
    /// He is already doing this, or has already tried once, and trying again is not new. Runs before
    /// Salience so a redundant duplicate cannot rank into the bounded candidate set and crowd out a
    /// genuinely different option.
    /// </summary>
    Redundancy,

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
