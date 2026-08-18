namespace CrimeSim.Domain;

public enum Tier
{
    Active,
    Supporting,
    Background,
}

public sealed class Character
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string RoleTitle { get; init; }

    /// <summary>
    /// How player-facing prose refers to this person. See <see cref="Domain.Pronouns"/>.
    ///
    /// Defaulted rather than required, and that is a considered trade. Making it required would force
    /// every construction site to state it, which is the stricter rule — but the default it replaces
    /// is the same "he" that every surface hardcoded, so a forgotten one is no worse than the
    /// position before this existed, while a required field would be one more thing for a test
    /// fixture to get wrong for no behavioural gain. The scenario cast states all six explicitly.
    /// </summary>
    public Pronouns Pronouns { get; init; } = Pronouns.He;

    public required Capabilities Capabilities { get; init; }

    /// <summary>
    /// RESTRICTED: read only by Decision/Salience.cs and Decision/Utility.cs. See Psychology.
    /// Settable only so scenario variants can swap a personality before the run starts; nothing in
    /// the simulation loop writes to it.
    /// </summary>
    public required Psychology Psychology { get; set; }

    public Cognition Cognition { get; } = new();
    public SocialState Social { get; } = new();
    public Motivations Motivations { get; } = new();
    public ExecutionState Execution { get; } = new();

    public Tier Tier { get; set; } = Tier.Active;

    /// <summary>Monotonic per-character counter used to seed this character's decision RNG stream.</summary>
    public int DecisionCount { get; set; }

    /// <summary>
    /// Monotonic per-character counter assigning each new strategy instance its local identity —
    /// exactly the DecisionCount pattern, one level down. See StrategyInstance.LocalSequence.
    /// </summary>
    public int StrategyCount { get; set; }

    /// <summary>Set for characters who are not organisation members (e.g. the detective, the shopkeeper).</summary>
    public bool IsOrgMember => Social.OrganizationId is not null;

    public CharacterView View => new(this);

    public override string ToString() => $"{Name} ({RoleTitle})";
}

/// <summary>
/// The projection of a character that candidate generators are allowed to see.
///
/// It deliberately exposes no Psychology and no Cognition. Traits are unavailable so a generator
/// cannot branch on them to emit an action; raw beliefs are unavailable so a generator must go
/// through PerceivedSituation, which is the same belief-limited view the scorer uses. Both
/// omissions are the point of this type — do not add those properties.
/// </summary>
public sealed class CharacterView
{
    private readonly Character _c;
    internal CharacterView(Character c) => _c = c;

    public string Id => _c.Id;
    public string Name => _c.Name;
    public string RoleTitle => _c.RoleTitle;
    public Pronouns Pronouns => _c.Pronouns;
    public Capabilities Capabilities => _c.Capabilities;
    public SocialState Social => _c.Social;
    public Motivations Motivations => _c.Motivations;
    public ExecutionState Execution => _c.Execution;
    public Tier Tier => _c.Tier;
    public bool IsOrgMember => _c.IsOrgMember;

    public override string ToString() => _c.ToString();
}
